using SillyPutty;

var failures = new List<string>();
void Check(bool condition, string name) { if (!condition) failures.Add(name); }

var valid = new PluginManifest
{
    Id = "example.test", Name = "Example", Version = "1.0.0", Publisher = "Tests", Capabilities = ["session-command"],
    Tools = [new PluginTool { Label = "Echo", Commands = new() { ["Windows"] = "Write-Output ok" } }]
};
Check(PluginManager.Validate(valid) == null, "valid plugin manifest");
valid.Id = "BAD ID"; Check(PluginManager.Validate(valid) != null, "invalid plugin id rejected");
valid.Id = "example.test"; valid.Capabilities = ["process-injection"]; Check(PluginManager.Validate(valid) != null, "unknown capability rejected");
valid.Capabilities = ["session-command"]; valid.Tools[0].Commands = new() { ["Solaris"] = "uname" }; Check(PluginManager.Validate(valid) != null, "unknown platform rejected");
Check(typeof(App).Assembly.GetName().Version?.Major == 0, "assembly version present");
Check(NetworkPolicy.TryParsePrivate24("192.168.10.0/24", out var prefix) && prefix == "192.168.10", "private /24 accepted");
Check(NetworkPolicy.TryParsePrivate24("10.20.30.55/24", out _), "private 10/8 /24 accepted");
Check(!NetworkPolicy.TryParsePrivate24("8.8.8.0/24", out _), "public network rejected");
Check(!NetworkPolicy.TryParsePrivate24("192.168.1.0/16", out _), "non-/24 rejected");

if (failures.Count > 0) { Console.Error.WriteLine("FAILED: " + string.Join(", ", failures)); return 1; }
Console.WriteLine("All SillyPutty smoke tests passed."); return 0;
