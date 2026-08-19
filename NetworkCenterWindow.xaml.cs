using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;

namespace SiliPuTTY;

public partial class NetworkCenterWindow : Window
{
    public ObservableCollection<DeviceResult> Devices { get; } = [];
    private CancellationTokenSource? _scanCancellation;
    private Process? _captureProcess;
    private string? _tsharkPath;
    private string? _wiresharkPath;
    private readonly Dictionary<int, string> _serviceNames = new() { [22] = "SSH", [80] = "HTTP", [443] = "HTTPS", [445] = "SMB", [3389] = "RDP", [5985] = "WinRM" };

    public sealed record DeviceResult(string Address, string Hostname, string Mac, string Services, string Latency);

    public NetworkCenterWindow()
    {
        InitializeComponent(); DataContext = this;
        SubnetBox.Text = GuessPrivateSubnet();
        DetectCapabilities();
    }

    private static string GuessPrivateSubnet()
    {
        var address = NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .SelectMany(n => n.GetIPProperties().UnicastAddresses)
            .Select(a => a.Address).FirstOrDefault(NetworkPolicy.IsPrivateIpv4);
        if (address == null) return "192.168.1.0/24";
        var b = address.GetAddressBytes(); return $"{b[0]}.{b[1]}.{b[2]}.0/24";
    }

    private async void Scan_Click(object sender, RoutedEventArgs e)
    {
        if (!NetworkPolicy.TryParsePrivate24(SubnetBox.Text.Trim(), out var prefix))
        { MessageBox.Show("Enter an authorized private IPv4 /24 such as 192.168.1.0/24.", "Private /24 required", MessageBoxButton.OK, MessageBoxImage.Information); return; }
        Devices.Clear(); ScanProgress.Value = 0; ScanButton.IsEnabled = false; CancelScanButton.IsEnabled = true;
        _scanCancellation = new CancellationTokenSource(); var token = _scanCancellation.Token;
        ScanStatus.Text = $"Scanning {prefix}.0/24…";
        try
        {
            using var gate = new SemaphoreSlim(32);
            var results = new List<DeviceResult>();
            var tasks = Enumerable.Range(1, 254).Select(async last =>
            {
                await gate.WaitAsync(token);
                try
                {
                    var address = $"{prefix}.{last}";
                    using var ping = new Ping();
                    var reply = await ping.SendPingAsync(address, TimeSpan.FromMilliseconds(500), cancellationToken: token);
                    if (reply.Status != IPStatus.Success) return;
                    var hostname = await ResolveHostnameAsync(address, token);
                    var services = await FindServicesAsync(address, token);
                    lock (results) results.Add(new(address, hostname, "", services, $"{reply.RoundtripTime} ms"));
                }
                catch (OperationCanceledException) { }
                catch { }
                finally { gate.Release(); Dispatcher.Invoke(() => ScanProgress.Value++); }
            });
            await Task.WhenAll(tasks);
            var arp = await ReadArpTableAsync();
            foreach (var item in results.OrderBy(r => IPAddress.Parse(r.Address).GetAddressBytes()[3]))
                Devices.Add(item with { Mac = arp.GetValueOrDefault(item.Address, "—") });
            ScanStatus.Text = $"Found {Devices.Count} responding device{(Devices.Count == 1 ? "" : "s")}.";
        }
        catch (OperationCanceledException) { ScanStatus.Text = $"Scan cancelled; {Devices.Count} results retained."; }
        finally { ScanButton.IsEnabled = true; CancelScanButton.IsEnabled = false; _scanCancellation?.Dispose(); _scanCancellation = null; }
    }

    private static async Task<string> ResolveHostnameAsync(string address, CancellationToken token)
    { try { return (await Dns.GetHostEntryAsync(address, token)).HostName; } catch { return "—"; } }

    private async Task<string> FindServicesAsync(string address, CancellationToken token)
    {
        var open = new List<string>();
        foreach (var port in _serviceNames.Keys)
        {
            using var client = new TcpClient();
            try { await client.ConnectAsync(address, port, token).AsTask().WaitAsync(TimeSpan.FromMilliseconds(250), token); open.Add($"{_serviceNames[port]}:{port}"); }
            catch { }
        }
        return open.Count == 0 ? "—" : string.Join(", ", open);
    }

    private static async Task<Dictionary<string, string>> ReadArpTableAsync()
    {
        var result = new Dictionary<string, string>();
        try
        {
            var psi = new ProcessStartInfo("arp.exe", "-a") { UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true };
            using var p = Process.Start(psi)!; var text = await p.StandardOutput.ReadToEndAsync(); await p.WaitForExitAsync();
            foreach (Match m in Regex.Matches(text, @"(?m)^\s*(\d+\.\d+\.\d+\.\d+)\s+([0-9a-fA-F-]{17})\s+")) result[m.Groups[1].Value] = m.Groups[2].Value.ToUpperInvariant();
        }
        catch { }
        return result;
    }

    private void CancelScan_Click(object sender, RoutedEventArgs e) => _scanCancellation?.Cancel();

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        if (Devices.Count == 0) { MessageBox.Show("Run a scan before exporting."); return; }
        var dialog = new SaveFileDialog { Filter = "CSV files (*.csv)|*.csv", FileName = $"SiliPuTTY-scan-{DateTime.Now:yyyyMMdd-HHmm}.csv" };
        if (dialog.ShowDialog(this) != true) return;
        static string Q(string s) => $"\"{s.Replace("\"", "\"\"")}\"";
        var lines = new[] { "IP Address,Hostname,MAC Address,Open Services,Latency" }.Concat(Devices.Select(d => string.Join(',', Q(d.Address), Q(d.Hostname), Q(d.Mac), Q(d.Services), Q(d.Latency))));
        File.WriteAllLines(dialog.FileName, lines, Encoding.UTF8); ScanStatus.Text = $"Exported {Devices.Count} devices.";
    }

    private void DetectCapabilities()
    {
        _tsharkPath = FindExecutable("tshark.exe", @"C:\Program Files\Wireshark\tshark.exe");
        _wiresharkPath = FindExecutable("Wireshark.exe", @"C:\Program Files\Wireshark\Wireshark.exe");
        var putty = FindExecutable("putty.exe", @"C:\Program Files\PuTTY\putty.exe");
        var openSsh = FindExecutable("ssh.exe", @"C:\Windows\System32\OpenSSH\ssh.exe");
        var nmap = FindExecutable("nmap.exe", @"C:\Program Files (x86)\Nmap\nmap.exe", @"C:\Program Files\Nmap\nmap.exe");
        var npcap = File.Exists(@"C:\Windows\System32\Npcap\wpcap.dll") || File.Exists(@"C:\Windows\System32\Npcap\Packet.dll");
        CapabilityText.Text = $"OpenSSH: {(openSsh != null ? "Available" : "Not found")}\nPuTTY suite: {(putty != null ? "Available" : "Not found")}\nNmap: {(nmap != null ? "Available" : "Not installed")}\nTShark capture/decoder: {(_tsharkPath != null ? "Available" : "Not installed")}\nWireshark desktop: {(_wiresharkPath != null ? "Available" : "Not installed")}\nNpcap capture driver: {(npcap ? "Available" : "Not detected")}";
        var missing = new List<string>();
        if (openSsh == null) missing.Add("OpenSSH for SSH sessions");
        if (putty == null) missing.Add("the PuTTY suite for compatibility and file-transfer tools");
        if (nmap == null) missing.Add("Nmap for advanced network discovery");
        if (_wiresharkPath == null || _tsharkPath == null) missing.Add("Wireshark with TShark for capture analysis");
        if (!npcap) missing.Add("Npcap for live packet capture");
        RecommendationPanel.Visibility = missing.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        RecommendationText.Text = missing.Count == 0 ? "" : $"Recommended: install {string.Join(", ", missing)}. SiliPuTTY does not install or elevate external tools automatically. After installation, select Refresh detection.";
        LoadInterfaces();
    }

    private static string? FindExecutable(string name, params string[] knownPaths)
    {
        foreach (var path in knownPaths) if (File.Exists(path)) return path;
        var envPath = Environment.GetEnvironmentVariable("PATH") ?? "";
        return envPath.Split(Path.PathSeparator).Select(p => Path.Combine(p, name)).FirstOrDefault(File.Exists);
    }

    private void LoadInterfaces()
    {
        InterfaceBox.Items.Clear();
        if (_tsharkPath == null) { InterfaceBox.Items.Add("TShark not installed"); InterfaceBox.SelectedIndex = 0; StartCaptureButton.IsEnabled = false; CaptureStatus.Text = "Install Wireshark/TShark with Npcap to enable packet capture."; return; }
        try
        {
            var psi = new ProcessStartInfo(_tsharkPath, "-D") { UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true };
            using var p = Process.Start(psi)!; var output = p.StandardOutput.ReadToEnd(); p.WaitForExit();
            foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries)) InterfaceBox.Items.Add(line.Trim());
            if (InterfaceBox.Items.Count > 0) InterfaceBox.SelectedIndex = 0;
            CaptureStatus.Text = "Ready. Capture only traffic you are authorized to inspect.";
        }
        catch (Exception ex) { CaptureStatus.Text = $"Could not enumerate interfaces: {ex.Message}"; }
    }

    private async void StartCapture_Click(object sender, RoutedEventArgs e)
    {
        if (_tsharkPath == null || InterfaceBox.SelectedItem == null) return;
        var match = Regex.Match(InterfaceBox.SelectedItem.ToString()!, @"^(\d+)\."); if (!match.Success) return;
        var captures = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SiliPuTTY Captures"); Directory.CreateDirectory(captures);
        var outputFile = Path.Combine(captures, $"capture-{DateTime.Now:yyyyMMdd-HHmmss}.pcapng");
        if (!int.TryParse(CaptureDurationBox.Text, out var duration)) duration = 300; duration = Math.Clamp(duration, 10, 86400); CaptureDurationBox.Text = duration.ToString();
        var psi = new ProcessStartInfo(_tsharkPath) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        psi.ArgumentList.Add("-l"); psi.ArgumentList.Add("-i"); psi.ArgumentList.Add(match.Groups[1].Value); psi.ArgumentList.Add("-a"); psi.ArgumentList.Add($"duration:{duration}"); psi.ArgumentList.Add("-w"); psi.ArgumentList.Add(outputFile);
        if (!string.IsNullOrWhiteSpace(CaptureFilterBox.Text)) { psi.ArgumentList.Add("-f"); psi.ArgumentList.Add(CaptureFilterBox.Text.Trim()); }
        try
        {
            _captureProcess = new Process { StartInfo = psi, EnableRaisingEvents = true }; _captureProcess.Start();
            StartCaptureButton.IsEnabled = false; StopCaptureButton.IsEnabled = true; PacketOutput.AppendText($"Capturing to {outputFile}\n"); CaptureStatus.Text = "Capture running…";
            var errors = await _captureProcess.StandardError.ReadToEndAsync();
            if (!string.IsNullOrWhiteSpace(errors)) Dispatcher.Invoke(() => PacketOutput.AppendText(errors));
        }
        catch (Exception ex) { PacketOutput.AppendText($"Capture error: {ex.Message}\n"); StartCaptureButton.IsEnabled = true; StopCaptureButton.IsEnabled = false; }
    }

    private async void StopCapture_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_captureProcess is { HasExited: false } process)
            {
                CaptureStatus.Text = "Stopping capture…";
                var closeRequested = process.CloseMainWindow();
                if (closeRequested) { using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2)); try { await process.WaitForExitAsync(timeout.Token); } catch (OperationCanceledException) { } }
                if (!process.HasExited) { process.Kill(true); await process.WaitForExitAsync(); PacketOutput.AppendText("Capture backend required forced termination; validate the saved file before relying on it.\n"); }
            }
        }
        catch (Exception ex) { PacketOutput.AppendText($"Stop error: {ex.Message}\n"); }
        _captureProcess?.Dispose(); _captureProcess = null; StartCaptureButton.IsEnabled = _tsharkPath != null; StopCaptureButton.IsEnabled = false; CaptureStatus.Text = "Capture stopped.";
    }

    private void OpenPcap_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Packet captures (*.pcap;*.pcapng)|*.pcap;*.pcapng|All files (*.*)|*.*" }; if (dialog.ShowDialog(this) != true) return;
        if (_wiresharkPath != null) Process.Start(new ProcessStartInfo(_wiresharkPath, $"\"{dialog.FileName}\"") { UseShellExecute = true });
        else if (_tsharkPath != null) Process.Start(new ProcessStartInfo(_tsharkPath, $"-r \"{dialog.FileName}\"") { UseShellExecute = true });
        else MessageBox.Show("Install Wireshark or TShark to decode PCAP files.", "Decoder not installed");
    }

    private void RefreshCapabilities_Click(object sender, RoutedEventArgs e) => DetectCapabilities();
    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e) { _scanCancellation?.Cancel(); try { if (_captureProcess is { HasExited: false }) _captureProcess.Kill(true); } catch { } _captureProcess?.Dispose(); }
}
