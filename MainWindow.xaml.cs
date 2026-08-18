using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace SillyPutty;

public partial class MainWindow : Window
{
    private string _currentDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    private readonly List<string> _history = [];
    private int _historyIndex;
    private readonly DispatcherTimer _refreshTimer;
    private SessionMode _mode = SessionMode.PowerShell;
    private PlatformKind _detectedPlatform = PlatformKind.Windows;
    private bool _initializing = true;

    private sealed record Tool(string Label, string Command, bool NeedsTarget = false, string Hint = "");
    private sealed record FileEntry(string Name, string FullPath, string Kind, bool IsDirectory);
    private enum SessionMode { PowerShell, Kali, Ssh }
    private enum PlatformKind { Default, Windows, Linux, MacOS }

    public MainWindow()
    {
        InitializeComponent();
        RebuildTools();
        _initializing = false;
        RefreshFiles();
        Append("SillyPutty ready. Select a session and connect, or run local PowerShell commands.\n");
        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _refreshTimer.Tick += (_, _) => RefreshFiles(false);
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
    }

    private void PlatformSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    { if (!_initializing) RebuildTools(); }

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

    private async void Connect_Click(object sender, RoutedEventArgs e)
    {
        _mode = SessionType.SelectedIndex switch { 1 => SessionMode.Kali, 2 => SessionMode.Ssh, _ => SessionMode.PowerShell };
        if (_mode == SessionMode.Ssh && !Regex.IsMatch(HostBox.Text.Trim(), @"^[\w.-]+@[\w.-]+(?::\d+)?$"))
        { MessageBox.Show("Enter an SSH destination like user@host.", "SSH destination required"); return; }
        StatusText.Text = "Detecting platform…";
        _detectedPlatform = await DetectPlatformAsync();
        if (PlatformSelector.SelectedIndex == 0) RebuildTools();
        StatusDot.Fill = new SolidColorBrush(Color.FromRgb(56, 217, 150));
        StatusText.Text = _mode switch { SessionMode.Kali => "Kali / WSL", SessionMode.Ssh => $"SSH {HostBox.Text.Trim()}", _ => "Local PowerShell" };
        PromptText.Text = _mode switch { SessionMode.Kali => "kali›", SessionMode.Ssh => "ssh›", _ => "PS›" };
        Append($"\n[session] {StatusText.Text}\n");
        if (_mode == SessionMode.Kali) await ExecuteAsync("pwd");
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
            psi.ArgumentList.Add(HostBox.Text.Trim());
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

    private void Disconnect_Click(object sender, RoutedEventArgs e)
    { _mode = SessionMode.PowerShell; _detectedPlatform = PlatformKind.Windows; if (PlatformSelector.SelectedIndex == 0) RebuildTools(); StatusText.Text = "Local PowerShell"; PromptText.Text = "PS›"; StatusDot.Fill = Brushes.Goldenrod; Append("\n[session] disconnected\n"); }

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
        if (TryChangeDirectory(command)) return;
        Append($"\n{PromptText.Text} {command}\n");
        CommandBox.IsEnabled = false;
        try
        {
            var psi = BuildProcess(command);
            using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            process.OutputDataReceived += (_, a) => { if (a.Data != null) Dispatcher.Invoke(() => Append(a.Data + "\n")); };
            process.ErrorDataReceived += (_, a) => { if (a.Data != null) Dispatcher.Invoke(() => Append(a.Data + "\n")); };
            process.Start(); process.BeginOutputReadLine(); process.BeginErrorReadLine();
            await process.WaitForExitAsync();
            Append($"[exit {process.ExitCode}]\n");
        }
        catch (Exception ex) { Append($"[error] {ex.Message}\n"); }
        finally { CommandBox.IsEnabled = true; CommandBox.Focus(); RefreshFiles(); }
    }

    private ProcessStartInfo BuildProcess(string command)
    {
        var psi = new ProcessStartInfo { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true, WorkingDirectory = _currentDirectory, StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8 };
        if (_mode == SessionMode.Kali) { psi.FileName = "wsl.exe"; psi.ArgumentList.Add("-d"); psi.ArgumentList.Add("kali-linux"); psi.ArgumentList.Add("--"); psi.ArgumentList.Add("bash"); psi.ArgumentList.Add("-lc"); psi.ArgumentList.Add(command); }
        else if (_mode == SessionMode.Ssh) { psi.FileName = "ssh.exe"; psi.ArgumentList.Add(HostBox.Text.Trim()); psi.ArgumentList.Add(command); }
        else { psi.FileName = "powershell.exe"; psi.ArgumentList.Add("-NoLogo"); psi.ArgumentList.Add("-NoProfile"); psi.ArgumentList.Add("-Command"); psi.ArgumentList.Add(command); }
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

    private void FileList_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (FileList.SelectedItem is not FileEntry item) return;
        if (item.IsDirectory) { _currentDirectory = item.FullPath; Append($"\nPS› cd \"{item.FullPath}\"\n"); RefreshFiles(); }
        else Process.Start(new ProcessStartInfo(item.FullPath) { UseShellExecute = true });
    }
    private void Refresh_Click(object sender, RoutedEventArgs e) => RefreshFiles();
    private void Clear_Click(object sender, RoutedEventArgs e) => TerminalOutput.Clear();
    private void Help_Click(object sender, RoutedEventArgs e) => new HelpWindow { Owner = this }.ShowDialog();
    private void Append(string text) { TerminalOutput.AppendText(text); TerminalOutput.ScrollToEnd(); }
}
