# SillyPutty

A native Windows terminal dashboard inspired by PuTTY, with local PowerShell, Kali Linux via WSL, SSH command execution, one-click Kali tools, and a live synchronized file browser.

The SillyPutty pixel-art mark is bundled as the executable, window, and in-app brand icon.

The **Configure** window organizes persistent defaults in a PuTTY-style category tree. The **Network Center** provides authorized private-LAN discovery, common-service checks, ARP/MAC collection, CSV export, and optional TShark/Wireshark packet capture integration.

Version 0.2 adds persistent session processes, reconnect/interrupt controls, masked secret entry, host-key warnings, named profiles, session logging, crash diagnostics, and a validated manifest-based plugin mode. See [FEATURES.md](FEATURES.md) for exact capability status and [docs/PLUGINS.md](docs/PLUGINS.md) for plugin authoring.

Open multiple independent sessions in tabs. Each tab preserves its own connection, platform, command history, output, target, and current folder. Use **New Session** or `Ctrl+T`; close the active tab with `Ctrl+W`; switch with `Ctrl+Tab`.

SillyPutty automatically detects Windows, Linux, or macOS when connecting and rebuilds its session-tool palette with platform-appropriate commands. Unknown SSH platforms fall back to a neutral Default appliance profile for switches, routers, and firewalls. The Platform menu can override detection at any time.

## Run

```powershell
dotnet run --project .\SillyPutty.csproj
```

For Kali mode, install Kali under WSL with the distro name `kali-linux`. SSH mode uses the Windows OpenSSH client. The tool buttons run commands in the selected session; tools must already be installed in that environment.

## Notes

- Double-click folders to change the local terminal directory; double-click files to open them with Windows.
- Enter commands at the bottom of the terminal. Use Up/Down for history.
- Select **Help** in the top bar for an in-app usage guide.
- Commands execute one at a time, so full-screen interactive programs are not supported in this prototype.
- Only run security tools against systems you own or have explicit permission to test.
