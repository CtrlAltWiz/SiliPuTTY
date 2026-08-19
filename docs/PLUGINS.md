# SiliPuTTY plugins

Plugin mode integrates command-line tools already installed on a user's system. Plugins are data-only JSON manifests: they cannot load DLLs, inject UI code, install software, request elevation, or store passwords.

Place manifests in `%LOCALAPPDATA%\SiliPuTTY\Plugins`, then enable them under **Configure → Plugins**. New session tabs load enabled tools after settings are saved.

```json
{
  "id": "example.diagnostics",
  "name": "Example Diagnostics",
  "version": "1.0.0",
  "publisher": "Example Publisher",
  "minimumAppVersion": "0.2.0",
  "description": "Adds a cross-platform DNS tool.",
  "capabilities": ["session-command", "network-access"],
  "tools": [{
    "label": "Resolve target",
    "needsTarget": true,
    "targetHint": "hostname",
    "commands": {
      "Windows": "Resolve-DnsName {target}",
      "Linux": "dig {target}",
      "MacOS": "dig {target}",
      "Default": "ping {target}"
    }
  }]
}
```

Allowed capabilities are `session-command`, `network-access`, `file-read`, and `file-write`. Commands run inside the active session. Target values use the same restricted-character validation as built-in tools. Capabilities are permission disclosures in version 0.2, not an operating-system sandbox; command-line tools retain the permissions of the active session.

Only install manifests from publishers you trust. Enabling a plugin authorizes its declared commands to run when you click its buttons.
