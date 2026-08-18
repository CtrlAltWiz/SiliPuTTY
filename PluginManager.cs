using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SillyPutty;

public sealed class PluginManifest
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Version { get; set; } = "1.0.0";
    public string Publisher { get; set; } = "Unknown";
    public string MinimumAppVersion { get; set; } = "0.2.0";
    public string Description { get; set; } = "";
    public List<string> Capabilities { get; set; } = [];
    public List<PluginTool> Tools { get; set; } = [];
    public string SourceFile { get; internal set; } = "";
}

public sealed class PluginTool
{
    public string Label { get; set; } = "";
    public Dictionary<string, string> Commands { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public bool NeedsTarget { get; set; }
    public string TargetHint { get; set; } = "target";
}

public sealed record PluginLoadResult(PluginManifest? Manifest, string? Error);

public static class PluginManager
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true, WriteIndented = true };
    private static readonly HashSet<string> AllowedCapabilities = new(StringComparer.OrdinalIgnoreCase) { "session-command", "network-access", "file-read", "file-write" };
    public static string PluginFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SillyPutty", "Plugins");

    public static IReadOnlyList<PluginLoadResult> LoadAll()
    {
        Directory.CreateDirectory(PluginFolder); var results = new List<PluginLoadResult>();
        foreach (var file in Directory.EnumerateFiles(PluginFolder, "*.json", SearchOption.TopDirectoryOnly))
        {
            try
            {
                var manifest = JsonSerializer.Deserialize<PluginManifest>(File.ReadAllText(file), JsonOptions) ?? throw new InvalidDataException("Manifest is empty.");
                manifest.SourceFile = file; var error = Validate(manifest); results.Add(error == null ? new(manifest, null) : new(null, $"{Path.GetFileName(file)}: {error}"));
            }
            catch (Exception ex) { results.Add(new(null, $"{Path.GetFileName(file)}: {ex.Message}")); }
        }
        return results;
    }

    public static string? Validate(PluginManifest manifest)
    {
        if (!Regex.IsMatch(manifest.Id, @"^[a-z0-9][a-z0-9.-]{2,63}$")) return "id must contain 3–64 lowercase letters, digits, dots, or hyphens.";
        if (string.IsNullOrWhiteSpace(manifest.Name) || manifest.Name.Length > 80) return "name is required and must be at most 80 characters.";
        if (!Version.TryParse(manifest.Version, out _)) return "version must be numeric, such as 1.0.0.";
        if (!Version.TryParse(manifest.MinimumAppVersion, out var minimum)) return "minimumAppVersion must be numeric, such as 0.2.0.";
        var current = typeof(PluginManager).Assembly.GetName().Version ?? new Version(0, 0); if (minimum > current) return $"requires SillyPutty {minimum} or later.";
        if (manifest.Capabilities.Any(c => !AllowedCapabilities.Contains(c))) return "manifest requests an unknown capability.";
        if (manifest.Tools.Count is < 1 or > 40) return "manifest must define 1–40 tools.";
        foreach (var tool in manifest.Tools)
        {
            if (string.IsNullOrWhiteSpace(tool.Label) || tool.Label.Length > 40) return "every tool requires a label of at most 40 characters.";
            if (tool.Commands.Count == 0 || tool.Commands.Any(c => c.Value.Length is < 1 or > 2000)) return $"{tool.Label} has an invalid command.";
            if (tool.Commands.Keys.Any(k => k is not ("Default" or "Windows" or "Linux" or "MacOS"))) return $"{tool.Label} contains an unsupported platform key.";
        }
        return null;
    }

    public static void CreateExample()
    {
        Directory.CreateDirectory(PluginFolder); var path = Path.Combine(PluginFolder, "example.disabled.json"); if (File.Exists(path)) return;
        var example = new PluginManifest
        {
            Id = "example.diagnostics", Name = "Example Diagnostics", Publisher = "Local example", Description = "Example manifest; rename to .json and enable it in Configuration.", Capabilities = ["session-command", "network-access"],
            Tools = [new PluginTool { Label = "Resolve target", NeedsTarget = true, TargetHint = "hostname", Commands = new() { ["Windows"] = "Resolve-DnsName {target}", ["Linux"] = "dig {target}", ["MacOS"] = "dig {target}", ["Default"] = "ping {target}" } }]
        };
        File.WriteAllText(path, JsonSerializer.Serialize(example, JsonOptions));
    }
}
