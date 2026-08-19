using System.IO;
using System.Text.Json;

namespace SiliPuTTY;

internal static class AppDataPaths
{
    public static readonly string Root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SiliPuTTY");
    private static readonly string LegacyRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SillyPutty");

    public static string FileWithLegacyFallback(string name)
    {
        var current = Path.Combine(Root, name); var legacy = Path.Combine(LegacyRoot, name);
        if (!File.Exists(current) && File.Exists(legacy))
        {
            Directory.CreateDirectory(Root); File.Copy(legacy, current);
        }
        return current;
    }

    public static string DirectoryWithLegacyMigration(string name)
    {
        var current = Path.Combine(Root, name); var legacy = Path.Combine(LegacyRoot, name);
        if (!Directory.Exists(current) && Directory.Exists(legacy))
        {
            Directory.CreateDirectory(current);
            foreach (var file in Directory.EnumerateFiles(legacy)) File.Copy(file, Path.Combine(current, Path.GetFileName(file)), false);
        }
        return current;
    }
}

public sealed class AppSettings
{
    public string DefaultHost { get; set; } = "user@host";
    public int Port { get; set; } = 22;
    public string ConnectionType { get; set; } = "SSH";
    public string CloseOnExit { get; set; } = "Only on clean exit";
    public string LoggingMode { get; set; } = "None";
    public string LogFilePattern { get; set; } = "SiliPuTTY-&Y&M&D-&T.log";
    public string ExistingLogAction { get; set; } = "Ask";
    public bool FlushLogs { get; set; } = true;
    public bool IncludeLogHeader { get; set; } = true;
    public bool AutoWrap { get; set; } = true;
    public string TerminalId { get; set; } = "xterm-256color";
    public string BackspaceKey { get; set; } = "Control-H";
    public string FunctionKeys { get; set; } = "Xterm R6";
    public string BellAction { get; set; } = "Visual flash";
    public bool AllowRemoteTitle { get; set; }
    public int Columns { get; set; } = 100;
    public int Rows { get; set; } = 32;
    public int ScrollbackLines { get; set; } = 10000;
    public string CursorShape { get; set; } = "Block";
    public bool CursorBlinks { get; set; } = true;
    public string CharacterSet { get; set; } = "UTF-8";
    public string ForegroundColor { get; set; } = "#E8F1F8";
    public string BackgroundColor { get; set; } = "#070B0F";
    public string AutoLoginUsername { get; set; } = "";
    public int KeepAliveSeconds { get; set; } = 30;
    public string ProxyType { get; set; } = "None";
    public string ProxyHost { get; set; } = "";
    public int ProxyPort { get; set; } = 8080;
    public bool SshCompression { get; set; }
    public bool AgentForwarding { get; set; }
    public bool X11Forwarding { get; set; }
    public string PrivateKeyPath { get; set; } = "";
    public string SerialLine { get; set; } = "COM1";
    public int SerialSpeed { get; set; } = 9600;
    public int DataBits { get; set; } = 8;
    public string StopBits { get; set; } = "1";
    public string Parity { get; set; } = "None";
    public string FlowControl { get; set; } = "None";
    public List<string> EnabledPluginIds { get; set; } = [];
}

public static class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static readonly string Folder = AppDataPaths.Root;
    private static readonly string FilePath = AppDataPaths.FileWithLegacyFallback("settings.json");
    public static AppSettings Current { get; private set; } = Load();

    private static AppSettings Load()
    {
        try { var settings = File.Exists(FilePath) ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new() : new(); settings.EnabledPluginIds ??= []; return settings; }
        catch { return new(); }
    }

    public static AppSettings Snapshot() => JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(Current, JsonOptions), JsonOptions) ?? new();

    public static void Save(AppSettings settings)
    {
        Directory.CreateDirectory(Folder);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(settings, JsonOptions));
        Current = settings;
    }
}

public sealed class SessionProfile
{
    public string Name { get; set; } = "New Session";
    public string Host { get; set; } = "user@host";
    public int Port { get; set; } = 22;
    public string ConnectionType { get; set; } = "SSH";
    public string Username { get; set; } = "";
    public string PrivateKeyPath { get; set; } = "";
}

public static class ProfileStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static readonly string Folder = AppDataPaths.Root;
    private static readonly string FilePath = AppDataPaths.FileWithLegacyFallback("profiles.json");
    public static IReadOnlyList<SessionProfile> Load()
    {
        try { return File.Exists(FilePath) ? JsonSerializer.Deserialize<List<SessionProfile>>(File.ReadAllText(FilePath)) ?? [] : []; }
        catch { return []; }
    }
    public static void Save(SessionProfile profile)
    {
        var profiles = Load().ToList(); profiles.RemoveAll(p => p.Name.Equals(profile.Name, StringComparison.OrdinalIgnoreCase)); profiles.Add(profile);
        Directory.CreateDirectory(Folder); File.WriteAllText(FilePath, JsonSerializer.Serialize(profiles.OrderBy(p => p.Name), JsonOptions));
    }
    public static void Delete(string name)
    {
        var profiles = Load().Where(p => !p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).ToList();
        Directory.CreateDirectory(Folder); File.WriteAllText(FilePath, JsonSerializer.Serialize(profiles, JsonOptions));
    }
}
