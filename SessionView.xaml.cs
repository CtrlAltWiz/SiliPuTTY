using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace SiliPuTTY;

public partial class SessionView : UserControl
{
    private string _currentDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    private readonly List<string> _history = [];
    private int _historyIndex;
    private readonly DispatcherTimer _refreshTimer;
    private SessionMode _mode = SessionMode.PowerShell;
    private PlatformKind _detectedPlatform = PlatformKind.Windows;
    private bool _initializing = true;
    private Process? _sessionProcess;
    private StreamWriter? _logWriter;
    private bool _intentionalDisconnect;
    private bool _hostKeyWarningShown;
    private bool _connectionInProgress;
    private string? _remoteListingToken;
    private bool _capturingRemoteListing;
    private bool _remoteBrowsingReady;
    private string _remoteDirectory = "";
    private readonly List<string> _remoteListingLines = [];
    private int _sessionPort;
    private string _sessionUsername = "";
    private string _sessionPrivateKeyPath = "";

    private sealed record Tool(string Label, string Command, bool NeedsTarget = false, string Hint = "");
    private sealed record FileEntry(string Name, string FullPath, string Kind, bool IsDirectory);
    private enum SessionMode { PowerShell, Kali, Ssh }
    private enum PlatformKind { Default, Windows, Linux, MacOS }

    public event Action<string>? TitleSuggested;

    public SessionView()
    {
        InitializeComponent();
        var defaults = SettingsStore.Current;
        HostBox.Text = defaults.DefaultHost;
        _sessionPort = defaults.Port; _sessionUsername = defaults.AutoLoginUsername; _sessionPrivateKeyPath = defaults.PrivateKeyPath;
        TerminalOutput.Foreground = ParseBrush(defaults.ForegroundColor, Color.FromRgb(232, 241, 248));
        TerminalOutput.Background = ParseBrush(defaults.BackgroundColor, Color.FromRgb(7, 11, 15));
        TerminalOutput.TextWrapping = defaults.AutoWrap ? TextWrapping.Wrap : TextWrapping.NoWrap;
        SessionType.SelectedIndex = defaults.ConnectionType == "SSH" ? 2 : 0;
        ProfileBox.ItemsSource = new[] { "Default" }.Concat(ProfileStore.Load().Select(p => p.Name)).ToArray(); ProfileBox.SelectedIndex = 0;
        RebuildTools();
        _initializing = false;
        RefreshFiles();
        Append("SiliPuTTY ready. Select a session and connect, or run local PowerShell commands.\n");
        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _refreshTimer.Tick += (_, _) =>
        {
            // Polling a remote interactive TTY echoes the encoded listing command into the
            // terminal. Remote listings refresh explicitly on connect/navigation/↻ instead.
            if (_mode != SessionMode.Ssh) RefreshFiles(false);
        };
        _refreshTimer.Start();
        CommandBox.Focus();
    }

    private PlatformKind ActivePlatform => PlatformSelector.SelectedIndex switch
    {
        1 => PlatformKind.Default, 2 => PlatformKind.Windows, 3 => PlatformKind.Linux, 4 => PlatformKind.MacOS, _ => _detectedPlatform
    };

    private void RebuildTools()
    {
        if (ToolsPanel == null) return;
        ToolsPanel.Children.Clear();
        DetectedPlatformText.Text = ActivePlatform switch { PlatformKind.MacOS => "macOS", _ => ActivePlatform.ToString() };
        Tool[] tools = ActivePlatform switch
        {
            PlatformKind.Default =>
            [
                new("Show version", "show version"), new("System info", "show system"),
                new("Interfaces", "show interfaces"), new("IP summary", "show ip interface brief"),
                new("Routes", "show ip route"), new("ARP table", "show arp"),
                new("Neighbors", "show neighbors"), new("Inventory", "show inventory"),
                new("Running config", "show running-config"), new("Disable paging", "terminal length 0"),
                new("Ping", "ping {target}", true, "host or IP"),
                new("Trace route", "traceroute {target}", true, "host or IP")
            ],
            PlatformKind.Windows =>
            [
                new("System info", "$PSVersionTable; Get-ComputerInfo | Select-Object WindowsProductName,WindowsVersion,OsArchitecture"),
                new("Interfaces", "Get-NetIPConfiguration | Format-List InterfaceAlias,IPv4Address,IPv6Address,DNSServer"),
                new("Routes", "Get-NetRoute | Sort-Object RouteMetric | Format-Table -AutoSize"),
                new("Listening ports", "Get-NetTCPConnection -State Listen | Sort-Object LocalPort | Format-Table -AutoSize"),
                new("DNS lookup", "Resolve-DnsName {target}", true, "domain"), new("Ping ×4", "Test-Connection {target} -Count 4", true, "host or IP"),
                new("Trace route", "Test-NetConnection {target} -TraceRoute", true, "host or IP"),
                new("Processes", "Get-Process | Sort-Object CPU -Descending | Select-Object -First 25"),
                new("Services", "Get-Service | Sort-Object Status,DisplayName | Format-Table -AutoSize"),
                new("Nmap quick", "nmap -T3 --top-ports 100 {target}", true, "authorized host"),
                new("Nmap services", "nmap -sV -T3 {target}", true, "authorized host"),
                new("Installed tools", "Get-Command nmap,nikto,gobuster,whois -ErrorAction SilentlyContinue | Select-Object Name,Source")
            ],
            PlatformKind.MacOS =>
            [
                new("System info", "uname -a; sw_vers"), new("Interfaces", "ifconfig"), new("Routes", "netstat -rn"),
                new("Listening ports", "lsof -nP -iTCP -sTCP:LISTEN"), new("DNS lookup", "dig {target}", true, "domain"),
                new("Whois", "whois {target}", true, "domain or IP"), new("Ping ×4", "ping -c 4 {target}", true, "host or IP"),
                new("Trace route", "traceroute {target}", true, "host or IP"), new("Processes", "ps aux | head -26"),
                new("Nmap quick", "nmap -T3 --top-ports 100 {target}", true, "authorized host"),
                new("Nmap services", "nmap -sV -T3 {target}", true, "authorized host"),
                new("Installed tools", "for x in nmap nikto gobuster whois dig; do command -v $x || echo \"$x: not installed\"; done")
            ],
            _ =>
            [
                new("System info", "uname -a; cat /etc/os-release 2>/dev/null | head"), new("Interfaces", "ip addr"), new("Routes", "ip route"),
                new("Listening ports", "ss -tulpn"), new("DNS lookup", "dig {target}", true, "domain"),
                new("Whois", "whois {target}", true, "domain or IP"), new("Ping ×4", "ping -c 4 {target}", true, "host or IP"),
                new("Trace route", "traceroute {target}", true, "host or IP"), new("Processes", "ps aux --sort=-%cpu | head -26"),
                new("Nmap quick", "nmap -T3 --top-ports 100 {target}", true, "authorized host"),
                new("Nmap services", "nmap -sV -T3 {target}", true, "authorized host"),
                new("Nikto web", "nikto -h {target}", true, "authorized URL"),
                new("Gobuster dirs", "gobuster dir -u {target} -w /usr/share/wordlists/dirb/common.txt", true, "authorized URL"),
                new("Installed tools", "for x in nmap nikto gobuster whois dig; do command -v $x || echo \"$x: not installed\"; done")
            ]
        };
        foreach (var tool in tools)
        {
            var button = new Button { Content = tool.Label, ToolTip = tool.NeedsTarget ? $"Runs: {tool.Command}\nTarget: {tool.Hint}" : $"Runs: {tool.Command}", Tag = tool, MinWidth = 112, Height = 40 };
            button.Click += Tool_Click;
            ToolsPanel.Children.Add(button);
        }
        var enabledPlugins = PluginManager.LoadAll().Where(r => r.Manifest != null && SettingsStore.Current.EnabledPluginIds.Contains(r.Manifest.Id, StringComparer.OrdinalIgnoreCase)).Select(r => r.Manifest!);
        foreach (var plugin in enabledPlugins)
        {
            if (!plugin.Capabilities.Contains("session-command", StringComparer.OrdinalIgnoreCase)) continue;
            foreach (var pluginTool in plugin.Tools)
            {
                if (!pluginTool.Commands.TryGetValue(ActivePlatform.ToString(), out var command) && !pluginTool.Commands.TryGetValue("Default", out command)) continue;
                var tool = new Tool($"{plugin.Name}: {pluginTool.Label}", command, pluginTool.NeedsTarget, pluginTool.TargetHint);
                var button = new Button { Content = tool.Label, ToolTip = $"Plugin: {plugin.Publisher}\nRuns: {tool.Command}\nCapabilities: {string.Join(", ", plugin.Capabilities)}", Tag = tool, MinWidth = 112, Height = 40 };
                button.Click += Tool_Click; ToolsPanel.Children.Add(button);
            }
        }
    }

    private void PlatformSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    { if (!_initializing) RebuildTools(); }

    private void ProfileBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProfileBox.SelectedIndex <= 0 || ProfileBox.SelectedItem is not string name) return;
        var profile = ProfileStore.Load().FirstOrDefault(p => p.Name == name); if (profile == null) return;
        HostBox.Text = profile.Host; _sessionPort = profile.Port; _sessionUsername = profile.Username; _sessionPrivateKeyPath = profile.PrivateKeyPath;
        SessionType.SelectedIndex = profile.ConnectionType switch { "SSH" => 2, _ => 0 };
        TitleSuggested?.Invoke(profile.Name);
    }

    private async void Tool_Click(object sender, RoutedEventArgs e)
    {
        if (((Button)sender).Tag is not Tool tool) return;
        var target = TargetBox.Text.Trim();
        if (tool.NeedsTarget && string.IsNullOrWhiteSpace(target))
        {
            MessageBox.Show($"Enter a {tool.Hint} in the Target box first.", "Target required", MessageBoxButton.OK, MessageBoxImage.Information);
            TargetBox.Focus(); return;
        }
        if (tool.NeedsTarget && !IsSafeTarget(target))
        {
            MessageBox.Show("The target contains unsupported shell characters.", "Invalid target", MessageBoxButton.OK, MessageBoxImage.Warning); return;
        }
        await ExecuteAsync(tool.Command.Replace("{target}", target));
    }

    private static bool IsSafeTarget(string value) => Regex.IsMatch(value, @"^[a-zA-Z0-9.:/_?=&%#@+~-]+$");

    private async void Connect_Click(object sender, RoutedEventArgs e) => await ConnectSessionAsync();

    private async Task<bool> ConnectSessionAsync()
    {
        if (_connectionInProgress) return false;
        _connectionInProgress = true; ConnectButton.IsEnabled = false;
        try
        {
            await DisconnectCoreAsync(false);
            _mode = SessionType.SelectedIndex switch { 1 => SessionMode.Kali, 2 => SessionMode.Ssh, _ => SessionMode.PowerShell };
            if (_mode == SessionMode.Ssh && !Regex.IsMatch(HostBox.Text.Trim(), @"^(?:[\w.-]+@)?[\w.-]+(?::\d+)?$"))
            { MessageBox.Show("Enter an SSH destination like host or user@host.", "SSH destination required"); return false; }
            if (_mode == SessionMode.Ssh)
            {
                StatusText.Text = "Verifying host key…";
                if (!await EnsureSshHostKeyAsync()) { StatusText.Text = "Ready"; return false; }
            }
            StatusText.Text = "Detecting platform…";
            _detectedPlatform = await DetectPlatformAsync();
            if (PlatformSelector.SelectedIndex == 0) RebuildTools();
            var psi = BuildSessionProcess();
            _intentionalDisconnect = false; _hostKeyWarningShown = false;
            var sessionProcess = new Process { StartInfo = psi, EnableRaisingEvents = true };
            _sessionProcess = sessionProcess;
            sessionProcess.OutputDataReceived += (_, a) => { if (a.Data != null) Dispatcher.BeginInvoke(() => HandleSessionOutput(a.Data + "\n")); };
            sessionProcess.ErrorDataReceived += (_, a) => { if (a.Data != null) Dispatcher.BeginInvoke(() => HandleSessionOutput(a.Data + "\n")); };
            sessionProcess.Exited += (_, _) => Dispatcher.BeginInvoke(() => HandleSessionExited(sessionProcess));
            sessionProcess.Start(); sessionProcess.BeginOutputReadLine(); sessionProcess.BeginErrorReadLine();
            StartLogging();
            StatusDot.Fill = new SolidColorBrush(Color.FromRgb(56, 217, 150));
            StatusText.Text = _mode switch { SessionMode.Kali => "Kali / WSL", SessionMode.Ssh => $"SSH {HostBox.Text.Trim()}", _ => "Local PowerShell" };
            TitleSuggested?.Invoke(_mode switch { SessionMode.Kali => "Kali", SessionMode.Ssh => HostBox.Text.Trim(), _ => "PowerShell" });
            PromptText.Text = _mode switch { SessionMode.Kali => "kali›", SessionMode.Ssh => "ssh›", _ => "PS›" };
            Append($"\n[session] connected: {StatusText.Text}\n");
            if (_mode == SessionMode.Ssh)
            {
                FileHeading.Text = "REMOTE FILES"; FileList.ItemsSource = null;
                PathText.Text = "Complete SSH sign-in, then click ↻";
                Append("[auth] If SSH requires a password or key passphrase, use the secure authentication window that opens automatically. The 🔒 Secret button is for prompts inside an established session.\n");
                Append("[files] After SSH sign-in completes, click the file-browser refresh button to load the remote home directory.\n");
            }
            else { FileHeading.Text = "LOCAL FILES"; RefreshFiles(); }
            CommandBox.Focus(); return true;
        }
        catch (Exception ex)
        {
            Append($"[connect error] {ex.Message}\n"); StatusText.Text = "Connection failed";
            StatusDot.Fill = new SolidColorBrush(Color.FromRgb(176, 56, 217)); return false;
        }
        finally { _connectionInProgress = false; ConnectButton.IsEnabled = true; }
    }

    private async Task<bool> EnsureSshHostKeyAsync()
    {
        var destination = GetSshDestination(out var port);
        var host = destination.Contains('@') ? destination[(destination.LastIndexOf('@') + 1)..] : destination;
        var lookupHost = port == 22 ? host : $"[{host}]:{port}";
        try
        {
            var known = await RunUtilityAsync("ssh-keygen.exe", ["-F", lookupHost], TimeSpan.FromSeconds(5));
            if (known.ExitCode == 0 && !string.IsNullOrWhiteSpace(known.Output)) return true;

            var scanArgs = new List<string> { "-T", "5" };
            if (port != 22) { scanArgs.Add("-p"); scanArgs.Add(port.ToString()); }
            scanArgs.Add(host);
            var scan = await RunUtilityAsync("ssh-keyscan.exe", scanArgs, TimeSpan.FromSeconds(8));
            var keyLines = scan.Output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).Where(x => !x.StartsWith('#')).Distinct().ToList();
            if (keyLines.Count == 0) throw new InvalidOperationException("The host did not provide a key before the verification timeout.");
            var fingerprints = keyLines.Select(FormatHostKeyFingerprint).Where(x => x != null).Distinct();
            var message = $"This host is not yet trusted:\n\n{host}:{port}\n\nPresented fingerprints:\n{string.Join("\n", fingerprints)}\n\nCompare these fingerprints with the desktop or its administrator. Trust and save this host key?";
            if (MessageBox.Show(message, "Verify SSH host key", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return false;

            var sshFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ssh"); Directory.CreateDirectory(sshFolder);
            var knownHosts = Path.Combine(sshFolder, "known_hosts");
            await File.AppendAllTextAsync(knownHosts, string.Join(Environment.NewLine, keyLines) + Environment.NewLine, Encoding.UTF8);
            Append($"[security] Saved the explicitly approved host key for {lookupHost}.\n"); return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"SiliPuTTY could not verify the SSH host key.\n\n{ex.Message}", "SSH host-key verification failed", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    private static string? FormatHostKeyFingerprint(string line)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries); if (parts.Length < 3) return null;
        try
        {
            var hash = System.Security.Cryptography.SHA256.HashData(Convert.FromBase64String(parts[2]));
            return $"{parts[1]}  SHA256:{Convert.ToBase64String(hash).TrimEnd('=')}";
        }
        catch { return null; }
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunUtilityAsync(string fileName, IEnumerable<string> arguments, TimeSpan timeout)
    {
        var psi = new ProcessStartInfo(fileName) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        foreach (var argument in arguments) psi.ArgumentList.Add(argument);
        using var process = Process.Start(psi) ?? throw new InvalidOperationException($"Could not start {fileName}.");
        var outputTask = process.StandardOutput.ReadToEndAsync(); var errorTask = process.StandardError.ReadToEndAsync();
        using var cancellation = new CancellationTokenSource(timeout);
        try { await process.WaitForExitAsync(cancellation.Token); }
        catch (OperationCanceledException) { try { process.Kill(true); } catch { } throw new TimeoutException($"{fileName} timed out."); }
        return (process.ExitCode, await outputTask, await errorTask);
    }

    private async Task<PlatformKind> DetectPlatformAsync()
    {
        if (_mode == SessionMode.PowerShell) return PlatformKind.Windows;
        if (_mode == SessionMode.Kali) return PlatformKind.Linux;
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "ssh.exe", UseShellExecute = false, RedirectStandardOutput = true,
                RedirectStandardError = true, CreateNoWindow = true
            };
            psi.ArgumentList.Add("-o"); psi.ArgumentList.Add("BatchMode=yes");
            psi.ArgumentList.Add("-o"); psi.ArgumentList.Add("ConnectTimeout=6");
            psi.ArgumentList.Add("-o"); psi.ArgumentList.Add("StrictHostKeyChecking=yes");
            var destination = GetSshDestination(out var port);
            if (port != 22) { psi.ArgumentList.Add("-p"); psi.ArgumentList.Add(port.ToString()); }
            psi.ArgumentList.Add(destination);
            psi.ArgumentList.Add("uname -s 2>/dev/null || powershell -NoProfile -Command \"[System.Environment]::OSVersion.Platform\"");
            using var process = Process.Start(psi)!;
            var outputTask = process.StandardOutput.ReadToEndAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            await process.WaitForExitAsync(timeout.Token);
            var output = (await outputTask).Trim().ToLowerInvariant();
            if (output.Contains("darwin")) return PlatformKind.MacOS;
            if (output.Contains("linux")) return PlatformKind.Linux;
            if (output.Contains("win32") || output.Contains("windows")) return PlatformKind.Windows;
            Append("[detect] Remote platform is not Windows, Linux, or macOS; using the Default appliance profile.\n");
            return PlatformKind.Default;
        }
        catch { Append("[detect] Could not identify remote OS; using the Default appliance profile. Override with the Platform menu if needed.\n"); return PlatformKind.Default; }
    }

    private async void Disconnect_Click(object sender, RoutedEventArgs e) => await DisconnectCoreAsync(true);
    private async void Reconnect_Click(object sender, RoutedEventArgs e) { await DisconnectCoreAsync(false); await ConnectSessionAsync(); }
    private void Interrupt_Click(object sender, RoutedEventArgs e)
    {
        try { if (_sessionProcess is { HasExited: false }) { _sessionProcess.StandardInput.Write("\x03"); _sessionProcess.StandardInput.Flush(); Append("\n[session] interrupt sent\n"); } }
        catch (Exception ex) { Append($"[interrupt error] {ex.Message}\n"); }
    }

    private async Task DisconnectCoreAsync(bool announce)
    {
        var process = _sessionProcess; _sessionProcess = null; _intentionalDisconnect = true;
        if (process != null)
        {
            try
            {
                if (!process.HasExited)
                {
                    await process.StandardInput.WriteLineAsync("exit"); await process.StandardInput.FlushAsync();
                    using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(900));
                    try { await process.WaitForExitAsync(timeout.Token); } catch (OperationCanceledException) { process.Kill(true); await process.WaitForExitAsync(); }
                }
            }
            catch { try { if (!process.HasExited) process.Kill(true); } catch { } }
            finally { process.Dispose(); }
        }
        StopLogging();
        _remoteListingToken = null; _capturingRemoteListing = false; _remoteBrowsingReady = false; _remoteListingLines.Clear();
        StatusText.Text = "Ready"; StatusDot.Fill = new SolidColorBrush(Color.FromRgb(176, 56, 217));
        if (announce) Append("\n[session] disconnected\n");
    }

    private async void Run_Click(object sender, RoutedEventArgs e) => await RunCommandBoxAsync();
    private async void CommandBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) { e.Handled = true; await RunCommandBoxAsync(); }
        else if (e.Key == Key.Up && _history.Count > 0) { _historyIndex = Math.Max(0, _historyIndex - 1); CommandBox.Text = _history[_historyIndex]; CommandBox.CaretIndex = CommandBox.Text.Length; }
        else if (e.Key == Key.Down && _history.Count > 0) { _historyIndex = Math.Min(_history.Count, _historyIndex + 1); CommandBox.Text = _historyIndex == _history.Count ? "" : _history[_historyIndex]; CommandBox.CaretIndex = CommandBox.Text.Length; }
    }

    private async Task RunCommandBoxAsync()
    {
        var command = CommandBox.Text.Trim(); if (command.Length == 0) return;
        _history.Add(command); _historyIndex = _history.Count; CommandBox.Clear();
        await ExecuteAsync(command); CommandBox.Focus();
    }

    private async Task ExecuteAsync(string command)
    {
        var displayCommand = command;
        var changedDirectory = TryChangeDirectory(command);
        if (_sessionProcess is not { HasExited: false } && !await ConnectSessionAsync()) return;
        if (changedDirectory && _mode == SessionMode.PowerShell) command = $"Set-Location -LiteralPath '{_currentDirectory.Replace("'", "''")}'";
        if (_mode == SessionMode.Ssh && ActivePlatform == PlatformKind.Windows) command = BuildEncodedPowerShellCommand(command, true);
        Append($"\n{PromptText.Text} {displayCommand}\n"); Log($"> {displayCommand}\n");
        try
        {
            await _sessionProcess!.StandardInput.WriteLineAsync(command); await _sessionProcess.StandardInput.FlushAsync();
        }
        catch (Exception ex) { Append($"[error] {ex.Message}\n"); }
        finally
        {
            CommandBox.Focus();
            if (_mode != SessionMode.Ssh) await RefreshActiveFilesAsync(false);
        }
    }

    private ProcessStartInfo BuildSessionProcess()
    {
        var utf8WithoutBom = new UTF8Encoding(false);
        var psi = new ProcessStartInfo { UseShellExecute = false, RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true, WorkingDirectory = _currentDirectory, StandardInputEncoding = utf8WithoutBom, StandardOutputEncoding = utf8WithoutBom, StandardErrorEncoding = utf8WithoutBom };
        if (_mode == SessionMode.Kali) { psi.FileName = "wsl.exe"; psi.ArgumentList.Add("-d"); psi.ArgumentList.Add("kali-linux"); psi.ArgumentList.Add("--"); psi.ArgumentList.Add("bash"); psi.ArgumentList.Add("--noprofile"); psi.ArgumentList.Add("--norc"); }
        else if (_mode == SessionMode.Ssh)
        {
            psi.FileName = "ssh.exe";
            var destination = GetSshDestination(out var port);
            psi.ArgumentList.Add("-tt");
            if (port != 22) { psi.ArgumentList.Add("-p"); psi.ArgumentList.Add(port.ToString()); }
            if (SettingsStore.Current.SshCompression) psi.ArgumentList.Add("-C");
            if (SettingsStore.Current.AgentForwarding) psi.ArgumentList.Add("-A");
            if (SettingsStore.Current.X11Forwarding) psi.ArgumentList.Add("-X");
            if (!string.IsNullOrWhiteSpace(_sessionPrivateKeyPath)) { psi.ArgumentList.Add("-i"); psi.ArgumentList.Add(_sessionPrivateKeyPath); }
            psi.ArgumentList.Add("-o"); psi.ArgumentList.Add($"ServerAliveInterval={Math.Max(0, SettingsStore.Current.KeepAliveSeconds)}");
            psi.ArgumentList.Add("-o"); psi.ArgumentList.Add("StrictHostKeyChecking=yes");
            psi.ArgumentList.Add("-o"); psi.ArgumentList.Add("NumberOfPasswordPrompts=3");
            var executablePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrWhiteSpace(executablePath))
            {
                psi.Environment["SSH_ASKPASS"] = executablePath;
                psi.Environment["SSH_ASKPASS_REQUIRE"] = "force";
                psi.Environment["DISPLAY"] = "SiliPuTTY";
                psi.Environment["SILIPUTTY_ASKPASS"] = "1";
            }
            psi.ArgumentList.Add(destination);
        }
        else { psi.FileName = "powershell.exe"; psi.ArgumentList.Add("-NoLogo"); psi.ArgumentList.Add("-NoProfile"); psi.ArgumentList.Add("-NoExit"); }
        return psi;
    }

    private bool TryChangeDirectory(string command)
    {
        var match = Regex.Match(command, @"^\s*(?:cd|Set-Location)\s+(.+?)\s*$", RegexOptions.IgnoreCase); if (!match.Success) return false;
        if (_mode != SessionMode.PowerShell) { Append("\nFolder synchronization is available for the local session. Remote cd applies per command only.\n"); return false; }
        var raw = match.Groups[1].Value.Trim().Trim('"', '\'');
        var next = raw == "~" ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) : Path.GetFullPath(Path.IsPathRooted(raw) ? raw : Path.Combine(_currentDirectory, raw));
        if (!Directory.Exists(next)) { Append($"\ncd: directory not found: {next}\n"); return true; }
        _currentDirectory = next; Append($"\nPS› cd \"{next}\"\n"); RefreshFiles(); return true;
    }

    private void RefreshFiles(bool reportErrors = true)
    {
        try
        {
            var items = new List<FileEntry>();
            var parent = Directory.GetParent(_currentDirectory); if (parent != null) items.Add(new("..", parent.FullName, "Folder", true));
            items.AddRange(Directory.EnumerateDirectories(_currentDirectory).Select(p => new FileEntry(Path.GetFileName(p), p, "Folder", true)).OrderBy(x => x.Name));
            items.AddRange(Directory.EnumerateFiles(_currentDirectory).Select(p => new FileEntry(Path.GetFileName(p), p, Path.GetExtension(p).TrimStart('.').ToUpperInvariant(), false)).OrderBy(x => x.Name));
            FileList.ItemsSource = items; PathText.Text = _currentDirectory;
        }
        catch (Exception ex) { if (reportErrors) Append($"[files] {ex.Message}\n"); }
    }

    private async Task RefreshActiveFilesAsync(bool reportErrors = true)
    {
        if (_mode != SessionMode.Ssh) { RefreshFiles(reportErrors); return; }
        if (_sessionProcess is not { HasExited: false }) return;
        if (!reportErrors && !_remoteBrowsingReady) return;
        await RequestRemoteFilesAsync(reportErrors);
    }

    private async Task RequestRemoteFilesAsync(bool reportErrors)
    {
        if (_remoteListingToken != null || _sessionProcess is not { HasExited: false }) return;
        if (ActivePlatform == PlatformKind.Default)
        {
            PathText.Text = "Remote browsing is unavailable for generic appliances";
            if (reportErrors) Append("[files] Remote browsing requires a Windows, Linux, or macOS SSH host.\n");
            return;
        }

        var token = Guid.NewGuid().ToString("N");
        _remoteListingToken = token; _capturingRemoteListing = false; _remoteListingLines.Clear();
        var begin = $"__SP_FILES_BEGIN_{token}__"; var end = $"__SP_FILES_END_{token}__";
        var script = $"$p=(Get-Location).Path; Write-Output \"{begin}\"; Write-Output (\"P|\"+[Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($p))); Get-ChildItem -LiteralPath . -Force | ForEach-Object {{ $t=if($_.PSIsContainer){{\"D\"}}else{{\"F\"}}; Write-Output ($t+\"|\"+[Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($_.Name))) }}; Write-Output \"{end}\"";
        var command = ActivePlatform == PlatformKind.Windows
            ? BuildEncodedPowerShellCommand(script, true)
            : $"printf '%s\\n' '{begin}'; p=$(pwd | base64 | tr -d '\\r\\n'); printf 'P|%s\\n' \"$p\"; for f in .[!.]* ..?* *; do [ -e \"$f\" ] || continue; if [ -d \"$f\" ]; then t=D; else t=F; fi; n=$(printf '%s' \"${{f#./}}\" | base64 | tr -d '\\r\\n'); printf '%s|%s\\n' \"$t\" \"$n\"; done; printf '%s\\n' '{end}'";
        try
        {
            await _sessionProcess.StandardInput.WriteLineAsync(command); await _sessionProcess.StandardInput.FlushAsync();
            _ = ExpireRemoteListingAsync(token);
        }
        catch (Exception ex)
        {
            _remoteListingToken = null;
            if (reportErrors) Append($"[files] Could not request the remote directory: {ex.Message}\n");
        }
    }

    private async Task ExpireRemoteListingAsync(string token)
    {
        await Task.Delay(TimeSpan.FromSeconds(10));
        if (_remoteListingToken != token) return;
        _remoteListingToken = null; _capturingRemoteListing = false; _remoteListingLines.Clear();
        PathText.Text = "Remote listing timed out — click ↻ to retry";
        Append("[files] The remote directory listing timed out. Confirm SSH sign-in is complete and the selected platform matches the host.\n");
    }

    private bool TryHandleRemoteListingLine(string text)
    {
        var token = _remoteListingToken; if (token == null) return false;
        var line = text.TrimEnd('\r', '\n');
        var begin = $"__SP_FILES_BEGIN_{token}__"; var end = $"__SP_FILES_END_{token}__";
        if (line.Trim() == begin) { _capturingRemoteListing = true; _remoteListingLines.Clear(); return true; }
        if (line.Trim() == end && _capturingRemoteListing)
        {
            _capturingRemoteListing = false; _remoteListingToken = null;
            ApplyRemoteListing(); return true;
        }
        if (_capturingRemoteListing) { _remoteListingLines.Add(line); return true; }
        return line.Contains(token, StringComparison.Ordinal);
    }

    private void ApplyRemoteListing()
    {
        try
        {
            var entries = new List<FileEntry> { new("..", "..", "Folder", true) };
            string? path = null;
            foreach (var line in _remoteListingLines)
            {
                var separator = line.IndexOf('|'); if (separator != 1 || line.Length < 3) continue;
                var kind = line[0];
                string value; try { value = Encoding.UTF8.GetString(Convert.FromBase64String(line[2..])); } catch { continue; }
                if (kind == 'P') path = value;
                else if (kind is 'D' or 'F') entries.Add(new(value, value, kind == 'D' ? "Folder" : "File", kind == 'D'));
            }
            if (path == null) throw new InvalidDataException("The remote host returned an incomplete directory listing.");
            FileList.ItemsSource = entries.OrderByDescending(x => x.Name == "..").ThenByDescending(x => x.IsDirectory).ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList();
            PathText.Text = path; _remoteDirectory = path; _remoteBrowsingReady = true;
        }
        catch (Exception ex) { Append($"[files] {ex.Message}\n"); }
        finally { _remoteListingLines.Clear(); }
    }

    private async void FileList_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (FileList.SelectedItem is not FileEntry item) return;
        if (_mode == SessionMode.Ssh)
        {
            if (!item.IsDirectory) { Append($"[files] {item.Name} is remote. File download/open is not implemented yet.\n"); return; }
            if (_sessionProcess is not { HasExited: false }) return;
            if (ActivePlatform == PlatformKind.Windows)
            {
                _remoteDirectory = item.Name == ".."
                    ? Directory.GetParent(_remoteDirectory)?.FullName ?? _remoteDirectory
                    : Path.Combine(_remoteDirectory, item.Name);
                await RequestRemoteFilesAsync(true); return;
            }
            var escaped = item.FullPath.Replace("'", ActivePlatform == PlatformKind.Windows ? "''" : "'\\''");
            var command = $"cd -- '{escaped}'";
            try
            {
                await _sessionProcess.StandardInput.WriteLineAsync(command); await _sessionProcess.StandardInput.FlushAsync();
                await RequestRemoteFilesAsync(true);
            }
            catch (Exception ex) { Append($"[files] Could not change the remote directory: {ex.Message}\n"); }
            return;
        }
        if (item.IsDirectory) { _currentDirectory = item.FullPath; Append($"\nPS› cd \"{item.FullPath}\"\n"); RefreshFiles(); }
        else Process.Start(new ProcessStartInfo(item.FullPath) { UseShellExecute = true });
    }
    private async void Refresh_Click(object sender, RoutedEventArgs e) => await RefreshActiveFilesAsync();
    private void OpenInExplorer_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_mode != SessionMode.Ssh)
            {
                var localItem = FileList.SelectedItem as FileEntry;
                var localPath = localItem?.FullPath ?? _currentDirectory;
                OpenExplorerPath(localPath, localItem is { IsDirectory: false });
                return;
            }

            if (ActivePlatform != PlatformKind.Windows)
            {
                MessageBox.Show("Windows File Explorer cannot browse this SSH host directly. Remote Explorer access is currently available for Windows hosts through Windows file sharing.", "Open in File Explorer", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var remoteItem = FileList.SelectedItem as FileEntry;
            var remotePath = remoteItem == null ? _remoteDirectory
                : remoteItem.Name == ".." ? Directory.GetParent(_remoteDirectory)?.FullName ?? _remoteDirectory
                : Path.Combine(_remoteDirectory, remoteItem.Name);
            var driveMatch = Regex.Match(remotePath, @"^(?<drive>[A-Za-z]):\\(?<rest>.*)$");
            if (!driveMatch.Success) throw new InvalidOperationException("The selected remote path is not a Windows drive path.");

            var destination = GetSshDestination(out _);
            var host = destination.Contains('@') ? destination[(destination.LastIndexOf('@') + 1)..] : destination;
            var uncPath = $@"\\{host}\{driveMatch.Groups["drive"].Value}$\{driveMatch.Groups["rest"].Value}".TrimEnd('\\');
            OpenExplorerPath(uncPath, remoteItem is { IsDirectory: false });
            Append($"[files] Opening {uncPath} in File Explorer. Windows may request file-sharing credentials.\n");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not open the selected path in File Explorer.\n\n{ex.Message}", "Open in File Explorer", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
    private static void OpenExplorerPath(string path, bool selectFile)
    {
        var startInfo = new ProcessStartInfo("explorer.exe") { UseShellExecute = true };
        startInfo.ArgumentList.Add(selectFile ? $"/select,{path}" : path);
        Process.Start(startInfo);
    }
    private void Clear_Click(object sender, RoutedEventArgs e) => TerminalOutput.Clear();
    private void Help_Click(object sender, RoutedEventArgs e) => new HelpWindow { Owner = Window.GetWindow(this) }.ShowDialog();
    private async void SendSecret_Click(object sender, RoutedEventArgs e)
    {
        if (_sessionProcess is not { HasExited: false } && !await ConnectSessionAsync()) return;
        var prompt = new SecretPromptWindow { Owner = Window.GetWindow(this) };
        if (prompt.ShowDialog() != true) return;
        try { await _sessionProcess!.StandardInput.WriteLineAsync(prompt.Secret); await _sessionProcess.StandardInput.FlushAsync(); }
        catch (Exception ex) { Append($"[secret send error] {ex.Message}\n"); }
    }

    private void HandleSessionOutput(string text)
    {
        text = StripTerminalControlSequences(text);
        if (text.Length == 0) return;
        if (TryHandleRemoteListingLine(text)) return;
        Append(text); Log(text);
        var lower = text.ToLowerInvariant();
        if (_mode == SessionMode.Ssh && PlatformSelector.SelectedIndex == 0 && _detectedPlatform == PlatformKind.Default &&
            (lower.Contains("microsoft windows [version") || Regex.IsMatch(text, @"[a-z]:\\", RegexOptions.IgnoreCase)))
        {
            _detectedPlatform = PlatformKind.Windows; RebuildTools();
            Append("[detect] Windows command shell detected; Windows tools and remote file browsing are now active.\n");
            _ = InitializeWindowsRemoteShellAsync();
        }
        if (!_hostKeyWarningShown && (lower.Contains("authenticity of host") || lower.Contains("host key is not cached")))
        {
            _hostKeyWarningShown = true; StatusText.Text = "Verify host key";
            Append("[security] Verify the displayed host-key fingerprint with your administrator before accepting it.\n");
        }
        if (lower.Contains("remote host identification has changed") || lower.Contains("potential security breach"))
        {
            StatusText.Text = "Host key changed";
            MessageBox.Show("The remote host key changed. Stop and verify the fingerprint with the system owner before reconnecting.", "SSH host-key warning", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void HandleSessionExited(Process process)
    {
        if (!ReferenceEquals(_sessionProcess, process)) return;
        int? exitCode = null; try { exitCode = process.ExitCode; } catch { }
        _sessionProcess = null; StopLogging();
        StatusDot.Fill = new SolidColorBrush(Color.FromRgb(176, 56, 217));
        StatusText.Text = _intentionalDisconnect ? "Ready" : "Disconnected";
        if (!_intentionalDisconnect) Append($"\n[session] connection ended{(exitCode.HasValue ? $" (exit {exitCode})" : "")}\n");
        process.Dispose();
    }

    private void StartLogging()
    {
        StopLogging(); var settings = SettingsStore.Current; if (settings.LoggingMode == "None") return;
        try
        {
            var name = settings.LogFilePattern.Replace("&Y", DateTime.Now.ToString("yyyy")).Replace("&M", DateTime.Now.ToString("MM")).Replace("&D", DateTime.Now.ToString("dd")).Replace("&T", DateTime.Now.ToString("HHmmss"));
            foreach (var invalid in Path.GetInvalidFileNameChars()) name = name.Replace(invalid, '_');
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SiliPuTTY Logs"); Directory.CreateDirectory(folder);
            var path = Path.IsPathRooted(name) ? name : Path.Combine(folder, name);
            var append = settings.ExistingLogAction == "Append";
            if (File.Exists(path) && settings.ExistingLogAction == "Ask")
                append = MessageBox.Show($"Append to existing log?\n{path}\n\nChoose No to overwrite it.", "Existing session log", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
            _logWriter = new StreamWriter(path, append, Encoding.UTF8) { AutoFlush = settings.FlushLogs };
            if (settings.IncludeLogHeader) _logWriter.WriteLine($"--- SiliPuTTY {DateTimeOffset.Now:O} | {_mode} | {HostBox.Text.Trim()} ---");
            if (settings.LoggingMode is "SSH packets" or "Raw data") Append("[logging] Packet/raw logging is not available through the current backend; recording session output instead.\n");
        }
        catch (Exception ex) { Append($"[logging error] {ex.Message}\n"); }
    }

    private void Log(string text) { try { _logWriter?.Write(text); } catch { } }
    private void StopLogging() { try { _logWriter?.Flush(); _logWriter?.Dispose(); } catch { } _logWriter = null; }
    public void Shutdown()
    {
        _refreshTimer.Stop(); _intentionalDisconnect = true; StopLogging();
        try { if (_sessionProcess is { HasExited: false }) _sessionProcess.Kill(true); } catch { }
        _sessionProcess?.Dispose(); _sessionProcess = null;
    }
    private string GetSshDestination(out int port)
    {
        var destination = HostBox.Text.Trim(); port = _sessionPort;
        var portMatch = Regex.Match(destination, @"^(.*):(\d+)$");
        if (portMatch.Success) { destination = portMatch.Groups[1].Value; port = int.Parse(portMatch.Groups[2].Value); }
        if (!destination.Contains('@') && !string.IsNullOrWhiteSpace(_sessionUsername)) destination = $"{_sessionUsername}@{destination}";
        return destination;
    }
    private string BuildEncodedPowerShellCommand(string command, bool useRemoteDirectory)
    {
        if (useRemoteDirectory && !string.IsNullOrWhiteSpace(_remoteDirectory))
            command = $"Set-Location -LiteralPath '{_remoteDirectory.Replace("'", "''")}'; {command}";
        var encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(command));
        return $"powershell.exe -NoLogo -NoProfile -EncodedCommand {encoded}";
    }
    private async Task InitializeWindowsRemoteShellAsync()
    {
        try
        {
            if (_sessionProcess is not { HasExited: false }) return;
            await _sessionProcess.StandardInput.WriteLineAsync("@echo off"); await _sessionProcess.StandardInput.FlushAsync();
            await RequestRemoteFilesAsync(false);
        }
        catch (Exception ex) { Append($"[shell] Could not initialize the Windows remote shell: {ex.Message}\n"); }
    }
    private static string StripTerminalControlSequences(string text)
    {
        text = Regex.Replace(text, "\\x1B\\][^\\x07]*(?:\\x07|\\x1B\\\\)", "");
        text = Regex.Replace(text, "\\x1B\\[[0-?]*[ -/]*[@-~]", "");
        return text.Replace("\a", "");
    }
    private static Brush ParseBrush(string value, Color fallback)
    { try { return (Brush)new BrushConverter().ConvertFromString(value)!; } catch { return new SolidColorBrush(fallback); } }
    private void Append(string text) { TerminalOutput.AppendText(text); TerminalOutput.ScrollToEnd(); }
}
