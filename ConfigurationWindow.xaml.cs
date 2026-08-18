using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SillyPutty;

public partial class ConfigurationWindow : Window
{
    private readonly AppSettings _settings = SettingsStore.Snapshot();
    private readonly List<Action> _collectors = [];

    public ConfigurationWindow() { InitializeComponent(); ShowCategory("Session"); }

    private void CategoryTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    { if (e.NewValue is TreeViewItem item && item.Tag is string tag) ShowCategory(tag); }

    private void ShowCategory(string category)
    {
        foreach (var collect in _collectors) collect();
        SettingsPanel.Children.Clear(); _collectors.Clear();
        SectionTitle(category);
        switch (category)
        {
            case "Session":
                Info("Active now: SSH and local PowerShell. Kali/WSL is selected from the session tab. Raw, Telnet, Rlogin, and Serial transports are planned and are not selectable in the active session UI yet.");
                TextSetting("Host name or IP", _settings.DefaultHost, v => _settings.DefaultHost = v);
                NumberSetting("Port", _settings.Port, v => _settings.Port = v, 1, 65535);
                ChoiceSetting("Connection type", _settings.ConnectionType, ["SSH", "Raw", "Telnet", "Rlogin", "Serial"], v => _settings.ConnectionType = v);
                ChoiceSetting("Close window on exit", _settings.CloseOnExit, ["Always", "Never", "Only on clean exit"], v => _settings.CloseOnExit = v); break;
            case "Saved": ShowSavedProfiles(); break;
            case "Logging":
                Info("Printable/all-output logging is active. SSH-packet and raw logging currently fall back to session output and are labeled in the terminal.");
                ChoiceSetting("Logging mode", _settings.LoggingMode, ["None", "Printable output", "All session output", "SSH packets", "Raw data"], v => _settings.LoggingMode = v);
                TextSetting("Log file name pattern", _settings.LogFilePattern, v => _settings.LogFilePattern = v);
                ChoiceSetting("If the log exists", _settings.ExistingLogAction, ["Ask", "Append", "Overwrite"], v => _settings.ExistingLogAction = v);
                CheckSetting("Flush log file frequently", _settings.FlushLogs, v => _settings.FlushLogs = v);
                CheckSetting("Include header lines", _settings.IncludeLogHeader, v => _settings.IncludeLogHeader = v); break;
            case "Terminal": Info("Auto-wrap is active for new tabs. Terminal-ID negotiation requires the planned ConPTY/VT renderer."); CheckSetting("Auto-wrap mode", _settings.AutoWrap, v => _settings.AutoWrap = v); TextSetting("Terminal identification string", _settings.TerminalId, v => _settings.TerminalId = v); break;
            case "Keyboard": Info("Saved for the planned VT keyboard mapper; not active in the current TextBox renderer."); ChoiceSetting("Backspace key", _settings.BackspaceKey, ["Control-H", "Control-?"], v => _settings.BackspaceKey = v); ChoiceSetting("Function keys and keypad", _settings.FunctionKeys, ["Xterm R6", "Linux", "VT100+", "SCO"], v => _settings.FunctionKeys = v); break;
            case "Bell": Info("Saved for the planned VT renderer; not active yet."); ChoiceSetting("Terminal bell", _settings.BellAction, ["None", "Visual flash", "Audio beep", "System sound"], v => _settings.BellAction = v); break;
            case "Features": Info("Saved for the planned VT renderer; not active yet."); CheckSetting("Allow remote-controlled window title", _settings.AllowRemoteTitle, v => _settings.AllowRemoteTitle = v); break;
            case "Window": Info("Saved as terminal-layout preferences; scrollback is not yet hard-limited by the current renderer."); NumberSetting("Columns", _settings.Columns, v => _settings.Columns = v, 40, 400); NumberSetting("Rows", _settings.Rows, v => _settings.Rows = v, 10, 200); NumberSetting("Scrollback lines", _settings.ScrollbackLines, v => _settings.ScrollbackLines = v, 100, 1000000); break;
            case "Appearance": Info("Saved for the planned VT renderer; not active yet."); ChoiceSetting("Cursor shape", _settings.CursorShape, ["Block", "Underline", "Vertical line"], v => _settings.CursorShape = v); CheckSetting("Cursor blinks", _settings.CursorBlinks, v => _settings.CursorBlinks = v); break;
            case "Translation": Info("UTF-8 is active. Additional character-set translation is planned."); ChoiceSetting("Character set", _settings.CharacterSet, ["UTF-8", "ISO-8859-1", "Windows-1252"], v => _settings.CharacterSet = v); break;
            case "Colours": TextSetting("Terminal foreground", _settings.ForegroundColor, v => _settings.ForegroundColor = v); TextSetting("Terminal background", _settings.BackgroundColor, v => _settings.BackgroundColor = v); break;
            case "Connection": NumberSetting("Seconds between keepalives (0 disables)", _settings.KeepAliveSeconds, v => _settings.KeepAliveSeconds = v, 0, 3600); break;
            case "Data": TextSetting("Auto-login username", _settings.AutoLoginUsername, v => _settings.AutoLoginUsername = v); break;
            case "Proxy": Info("Proxy routing is persisted but not active in the current OpenSSH backend yet."); ChoiceSetting("Proxy type", _settings.ProxyType, ["None", "SOCKS 5", "SOCKS 4", "HTTP", "Telnet"], v => _settings.ProxyType = v); TextSetting("Proxy host", _settings.ProxyHost, v => _settings.ProxyHost = v); NumberSetting("Proxy port", _settings.ProxyPort, v => _settings.ProxyPort = v, 1, 65535); break;
            case "SSH": CheckSetting("Enable compression", _settings.SshCompression, v => _settings.SshCompression = v); CheckSetting("Allow agent forwarding", _settings.AgentForwarding, v => _settings.AgentForwarding = v); CheckSetting("Enable X11 forwarding", _settings.X11Forwarding, v => _settings.X11Forwarding = v); TextSetting("Private key file", _settings.PrivateKeyPath, v => _settings.PrivateKeyPath = v); break;
            case "Serial": Info("Serial transport is planned; these values are saved but not active yet."); TextSetting("Serial line", _settings.SerialLine, v => _settings.SerialLine = v); NumberSetting("Speed (baud)", _settings.SerialSpeed, v => _settings.SerialSpeed = v, 50, 4000000); NumberSetting("Data bits", _settings.DataBits, v => _settings.DataBits = v, 5, 8); ChoiceSetting("Stop bits", _settings.StopBits, ["1", "1.5", "2"], v => _settings.StopBits = v); ChoiceSetting("Parity", _settings.Parity, ["None", "Odd", "Even", "Mark", "Space"], v => _settings.Parity = v); ChoiceSetting("Flow control", _settings.FlowControl, ["None", "XON/XOFF", "RTS/CTS", "DSR/DTR"], v => _settings.FlowControl = v); break;
            case "Plugins": ShowPlugins(); break;
        }
    }

    private void SectionTitle(string text) => SettingsPanel.Children.Add(new TextBlock { Text = text, FontSize = 20, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(176, 56, 217)), Margin = new Thickness(0, 0, 0, 14) });
    private void Info(string text) => SettingsPanel.Children.Add(new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, Foreground = new SolidColorBrush(Color.FromRgb(86, 157, 229)) });
    private void TextSetting(string label, string value, Action<string> set) { Label(label); var box = new TextBox { Text = value, Margin = new Thickness(0, 0, 0, 12) }; SettingsPanel.Children.Add(box); _collectors.Add(() => set(box.Text.Trim())); }
    private void NumberSetting(string label, int value, Action<int> set, int min, int max) { Label(label); var box = new TextBox { Text = value.ToString(), Margin = new Thickness(0, 0, 0, 12) }; SettingsPanel.Children.Add(box); _collectors.Add(() => { if (int.TryParse(box.Text, out var n)) set(Math.Clamp(n, min, max)); }); }
    private void ChoiceSetting(string label, string value, string[] choices, Action<string> set) { Label(label); var box = new ComboBox { ItemsSource = choices, SelectedItem = value, Margin = new Thickness(0, 0, 0, 12), Padding = new Thickness(7) }; SettingsPanel.Children.Add(box); _collectors.Add(() => set(box.SelectedItem?.ToString() ?? value)); }
    private void CheckSetting(string label, bool value, Action<bool> set) { var box = new CheckBox { Content = label, IsChecked = value, Margin = new Thickness(0, 4, 0, 12) }; SettingsPanel.Children.Add(box); _collectors.Add(() => set(box.IsChecked == true)); }
    private void ShowSavedProfiles()
    {
        Info("Profiles store connection metadata only. Passwords and secrets are never saved.");
        Label("Profile name"); var nameBox = new TextBox { Margin = new Thickness(0, 5, 0, 10) }; SettingsPanel.Children.Add(nameBox);
        var list = new ListBox { ItemsSource = ProfileStore.Load().Select(p => p.Name), Height = 180, Foreground = new SolidColorBrush(Color.FromRgb(86, 157, 229)), Background = new SolidColorBrush(Color.FromRgb(11, 17, 23)), Margin = new Thickness(0, 0, 0, 10) }; SettingsPanel.Children.Add(list);
        list.SelectionChanged += (_, _) => { if (list.SelectedItem is string selected) nameBox.Text = selected; };
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        var save = new Button { Content = "Save current defaults", Background = new SolidColorBrush(Color.FromRgb(23, 107, 80)) };
        var delete = new Button { Content = "Delete selected" }; row.Children.Add(save); row.Children.Add(delete); SettingsPanel.Children.Add(row);
        save.Click += (_, _) =>
        {
            var name = nameBox.Text.Trim(); if (name.Length == 0) { MessageBox.Show("Enter a profile name."); return; }
            ProfileStore.Save(new SessionProfile { Name = name, Host = _settings.DefaultHost, Port = _settings.Port, ConnectionType = _settings.ConnectionType, Username = _settings.AutoLoginUsername, PrivateKeyPath = _settings.PrivateKeyPath });
            ShowCategory("Saved"); SaveStatus.Text = $"Saved profile {name}";
        };
        delete.Click += (_, _) => { if (list.SelectedItem is string selected) { ProfileStore.Delete(selected); ShowCategory("Saved"); SaveStatus.Text = $"Deleted profile {selected}"; } };
    }
    private void ShowPlugins()
    {
        Info("Plugins are JSON manifests that expose commands from tools already installed on your system. They run out of process and must be explicitly enabled. SillyPutty never downloads, installs, or elevates a plugin.");
        var results = PluginManager.LoadAll(); var enabled = _settings.EnabledPluginIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var result in results)
        {
            if (result.Manifest is { } plugin)
            {
                var check = new CheckBox { Content = $"{plugin.Name} {plugin.Version} — {plugin.Publisher}\n{plugin.Description}\nCapabilities: {string.Join(", ", plugin.Capabilities)}", IsChecked = enabled.Contains(plugin.Id), Margin = new Thickness(0, 8, 0, 8), Foreground = new SolidColorBrush(Color.FromRgb(86, 157, 229)) };
                SettingsPanel.Children.Add(check); _collectors.Add(() => { if (check.IsChecked == true && !_settings.EnabledPluginIds.Contains(plugin.Id, StringComparer.OrdinalIgnoreCase)) _settings.EnabledPluginIds.Add(plugin.Id); else if (check.IsChecked != true) _settings.EnabledPluginIds.RemoveAll(x => x.Equals(plugin.Id, StringComparison.OrdinalIgnoreCase)); });
            }
            else Info($"Invalid plugin: {result.Error}");
        }
        if (results.Count == 0) Info("No plugin manifests are installed.");
        var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 12, 0, 0) };
        var open = new Button { Content = "Open plugin folder" }; var example = new Button { Content = "Create example manifest" }; row.Children.Add(open); row.Children.Add(example); SettingsPanel.Children.Add(row);
        open.Click += (_, _) => { Directory.CreateDirectory(PluginManager.PluginFolder); Process.Start(new ProcessStartInfo(PluginManager.PluginFolder) { UseShellExecute = true }); };
        example.Click += (_, _) => { PluginManager.CreateExample(); ShowCategory("Plugins"); SaveStatus.Text = "Example created (disabled by filename)."; };
    }
    private void Label(string text) => SettingsPanel.Children.Add(new TextBlock { Text = text, Foreground = new SolidColorBrush(Color.FromRgb(86, 157, 229)), Margin = new Thickness(0, 0, 0, 5) });
    private void Save_Click(object sender, RoutedEventArgs e) { foreach (var collect in _collectors) collect(); SettingsStore.Save(_settings); SaveStatus.Text = "Settings saved"; }
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
