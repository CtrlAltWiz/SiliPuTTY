# SillyPutty

![SillyPutty icon](Assets/sillyputty.png)

SillyPutty is a native Windows terminal and authorized network-diagnostics dashboard inspired by PuTTY. It combines persistent tabbed sessions, platform-aware tools, live file navigation, private-LAN discovery, optional packet-capture integration, and a safe manifest-based plugin system in one focused interface.

> **Project status:** `0.2.0-alpha` — ready for controlled testing, but not yet recommended as the sole access path to production systems. See [capability status](FEATURES.md) and the [release checklist](RELEASE_CHECKLIST.md).

## Highlights

- Multiple independent PowerShell, Kali/WSL, and SSH session tabs
- Persistent processes with reconnect, interrupt, and disconnect controls
- Masked secret entry that avoids terminal history and session logs
- SSH host-key warnings and configurable identity files
- Windows, Linux, macOS, and generic network-appliance tool palettes
- Named session profiles that never store passwords
- Live local file browser synchronized with directory changes
- PuTTY-style categorized configuration with persistent defaults
- Authorized RFC 1918 `/24` discovery, hostname/MAC checks, common-service detection, cancellation, and CSV export
- Optional Wireshark/TShark/Npcap capture and PCAP integration
- Dynamic backend detection and installation recommendations
- Explicitly enabled, validated JSON plugins for pre-existing command-line tools
- Crash diagnostics, automated tests, CI, packaging, and installer scaffolding

## Requirements

SillyPutty runs on Windows. The self-contained package includes the required .NET runtime.

Runtime integrations are detected automatically:

| Integration | Purpose | Requirement |
|---|---|---|
| Windows OpenSSH | Persistent SSH sessions | Recommended |
| Kali Linux under WSL | Kali session mode | Optional; distro name `kali-linux` |
| PuTTY suite | Compatibility and file-transfer tooling | Optional |
| Nmap | Advanced discovery commands | Optional |
| Wireshark and TShark | Packet capture and PCAP analysis | Optional |
| Npcap | Live packet-capture driver | Optional |

SillyPutty does not silently download, install, or elevate external tools. Check **Network → Capabilities** after installing them.

## Getting started

### Download for Windows

Download the newest build from the [GitHub Releases page](https://github.com/CtrlAltWiz/SillyPutty/releases). Do not use GitHub's **Source code** downloads unless you intend to build the application yourself.

- **Setup EXE** — recommended for most users; installs SillyPutty and creates Start Menu shortcuts.
- **Portable ZIP** — extract the archive, then run `SillyPutty.exe`; no .NET installation is required.

Early alpha, beta, and release-candidate builds appear under **Pre-releases**. Windows may show an unknown-publisher warning until release binaries are code-signed.

### Run from source

Install the .NET 10 SDK, clone the repository, and run:

```powershell
dotnet run --project .\SillyPutty.csproj
```

### Build

```powershell
dotnet build .\SillyPutty.csproj -c Release
dotnet run --project .\tests\SillyPutty.Tests\SillyPutty.Tests.csproj -c Release
```

### Create a self-contained package

```powershell
.\scripts\package.ps1
```

The packaging script supports an optional trusted code-signing certificate thumbprint:

```powershell
.\scripts\package.ps1 -CertificateThumbprint "YOUR_CERTIFICATE_THUMBPRINT"
```

## Basic use

1. Open a session tab and choose PowerShell, Kali/WSL, or SSH.
2. For SSH, enter `host`, `user@host`, or `user@host:port`, then select **Connect**.
3. Use **Secret** for passwords and passphrases so they are not displayed, logged, or saved in history.
4. Enter an authorized target before running tools that require one.
5. Use **Configure** for defaults and named profiles.
6. Use **Network** for private-LAN discovery, capture integration, and backend status.

Keyboard shortcuts:

- `Ctrl+T` — new session tab
- `Ctrl+W` — close active tab
- `Ctrl+Tab` / `Ctrl+Shift+Tab` — switch tabs
- `Up` / `Down` — command history

## Plugins

Plugin mode integrates command-line tools already installed on a user's system. Plugins are JSON manifests, require explicit enablement, and cannot inject DLL or WPF code into SillyPutty. See the [plugin authoring guide](docs/PLUGINS.md).

Plugin capability declarations are disclosures, not an operating-system sandbox. Only enable plugins from publishers you trust.

## Security and responsible use

Use network discovery, packet capture, and security tools only on systems and networks you own or have explicit permission to test. Discovery is restricted to private RFC 1918 IPv4 `/24` networks.

Passwords are not stored in profiles. Private-key configuration stores a filesystem path, not key contents. Sensitive vulnerabilities should be reported privately as described in [SECURITY.md](SECURITY.md).

## Current limitations

The SSH process requests a remote TTY, but the local display is not yet a complete ConPTY/VT renderer. ANSI control sequences and full-screen interactive programs may not render correctly. Raw, Telnet, Rlogin, Serial, proxy routing, port forwarding, and integrated PSCP/PSFTP surfaces remain planned.

The full implemented/partial/planned matrix is maintained in [FEATURES.md](FEATURES.md).

## License

SillyPutty is free and open-source software licensed under the [MIT License](LICENSE).

Copyright © 2026 CtrlAltWiz.

External applications—including PuTTY, Nmap, Wireshark, TShark, and Npcap—are separate projects governed by their own licenses and are not relicensed or automatically distributed by SillyPutty.
