<img alt="Static Badge" src="https://img.shields.io/badge/Build-Alpha-blue%3Flogo%3Dgithub?logo=github&color=lightgreen"> <img alt="Static Badge" src="https://img.shields.io/badge/Network-Tools-blue"> <img alt="Static Badge" src="https://img.shields.io/badge/License-MIT-blue%3Flogo%3Dgithub">

# What on Earth is SiliPuTTY?! <img width="32" height="32" alt="siliputty" src="https://github.com/user-attachments/assets/061a9ceb-814b-4603-81f9-06ab142f60c7" />

<img width="1241" height="757" alt="image" src="https://github.com/user-attachments/assets/7297ccd8-fe62-42e4-9bc4-d6d7be10fa3a" />


<img width="18" height="18" alt="siliputty" src="https://github.com/user-attachments/assets/061a9ceb-814b-4603-81f9-06ab142f60c7" /> This app is like PuTTY’s quirky cousin for Windows terminals, packing local PowerShell, Kali Linux through WSL, SSH command magic, one-click Kali gadget access, and a live file browser that’s always in sync.

<img width="18" height="18" alt="siliputty" src="https://github.com/user-attachments/assets/061a9ceb-814b-4603-81f9-06ab142f60c7" /> The app spots whether you're on Windows, Linux, or macOS as soon as you connect and then whips up a session-tool palette packed with commands that actually make sense for your platform. If it stumbles upon an unfamiliar SSH platform, it just shrugs and defaults to a generic appliance profile for switches, routers, and firewalls. And if you’re feeling rebellious, the Platform menu lets you override its guesswork whenever you want.

> **Project status:** `0.3.3-alpha` — ready for controlled testing, but not yet recommended as the sole access path to production systems. See [capability status](FEATURES.md) and the [release checklist](RELEASE_CHECKLIST.md).

## Version information

| | |
|---|---|
| Current version | `0.3.3-alpha` |
| Release channel | Alpha pre-release |
| Platform | Windows x64; self-contained .NET 10 build |
| Notable update | Quiet remote browsing and Windows File Explorer integration |

This version stops background SSH file polling from echoing encoded listing commands into the terminal. Remote listings now refresh on connect, navigation, or demand, and a new right-click action opens local paths or accessible Windows administrative-share paths in File Explorer.

## Highlights

- Multiple independent PowerShell, Kali/WSL, and SSH session tabs
- Persistent processes with reconnect, interrupt, and disconnect controls
- Masked secret entry that avoids terminal history and session logs
- SSH host-key warnings and configurable identity files
- Windows, Linux, macOS, Cisco, Aruba, Fortinet, Palo Alto, Juniper, and generic appliance tool palettes
- Named session profiles that never store passwords
- Live local file browser plus remote folder navigation over an authenticated SSH session
- PuTTY-style categorized configuration with persistent defaults
- Authorized RFC 1918 `/24` discovery, hostname/MAC checks, common-service detection, cancellation, and CSV export
- Optional Wireshark/TShark/Npcap capture and PCAP integration
- Dynamic backend detection and installation recommendations
- Explicitly enabled, validated JSON plugins for pre-existing command-line tools
- Crash diagnostics, automated tests, CI, packaging, and installer scaffolding

## Requirements

SiliPuTTY runs on Windows. The self-contained package includes the required .NET runtime.

Runtime integrations are detected automatically:

| Integration | Purpose | Requirement |
|---|---|---|
| Windows OpenSSH | Persistent SSH sessions | Recommended |
| Kali Linux under WSL | Kali session mode | Optional; distro name `kali-linux` |
| PuTTY suite | Compatibility and file-transfer tooling | Optional |
| Nmap | Advanced discovery commands | Optional |
| Wireshark and TShark | Packet capture and PCAP analysis | Optional |
| Npcap | Live packet-capture driver | Optional |

SiliPuTTY does not silently download, install, or elevate external tools. Check **Network → Capabilities** after installing them.

## Getting started

### Download for Windows

Download SiliPuTTY `v0.3.3-alpha` directly:

<img width="18" height="18" alt="siliputty" src="https://github.com/user-attachments/assets/061a9ceb-814b-4603-81f9-06ab142f60c7" /> **[Windows installer](https://github.com/CtrlAltWiz/SiliPuTTY/releases/download/v0.3.3-alpha/SiliPuTTY-0.3.3-alpha-Setup.exe)** — recommended for most users; installs SiliPuTTY and creates Start Menu shortcuts.

<img width="18" height="18" alt="siliputty" src="https://github.com/user-attachments/assets/061a9ceb-814b-4603-81f9-06ab142f60c7" /> **[Portable ZIP](https://github.com/CtrlAltWiz/SiliPuTTY/releases/download/v0.3.3-alpha/SiliPuTTY-0.3.3-alpha-win-x64.zip)** — extract the archive, then run `SiliPuTTY.exe`; no .NET installation is required.

<img width="18" height="18" alt="siliputty" src="https://github.com/user-attachments/assets/061a9ceb-814b-4603-81f9-06ab142f60c7" /> **[SHA-256 checksums](https://github.com/CtrlAltWiz/SiliPuTTY/releases/download/v0.3.3-alpha/SHA256SUMS.txt)** — use these to verify downloaded files.

See the [GitHub Releases page](https://github.com/CtrlAltWiz/SiliPuTTY/releases) for release notes and future versions. Do not use GitHub's **Source code** downloads unless you intend to build the application yourself.

Early alpha, beta, and release-candidate builds appear under **Pre-releases**. Windows may show an unknown-publisher warning until release binaries are code-signed.

### Run from source

Install the .NET 10 SDK, clone the repository, and run:

```powershell
dotnet run --project .\SiliPuTTY.csproj
```

<img width="18" height="18" alt="siliputty" src="https://github.com/user-attachments/assets/061a9ceb-814b-4603-81f9-06ab142f60c7" /> To use Kali mode, set up Kali on WSL with the distro named `kali-linux`. SSH mode relies on Windows' OpenSSH client. The tool buttons execute commands in the session you’ve picked; just make sure the tools are already installed there.

### Build

```powershell
dotnet build .\SiliPuTTY.csproj -c Release
dotnet run --project .\tests\SiliPuTTY.Tests\SiliPuTTY.Tests.csproj -c Release
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

<img width="18" height="18" alt="siliputty" src="https://github.com/user-attachments/assets/061a9ceb-814b-4603-81f9-06ab142f60c7" /> `Ctrl+T` — new session tab

<img width="18" height="18" alt="siliputty" src="https://github.com/user-attachments/assets/061a9ceb-814b-4603-81f9-06ab142f60c7" /> `Ctrl+W` — close active tab

<img width="18" height="18" alt="siliputty" src="https://github.com/user-attachments/assets/061a9ceb-814b-4603-81f9-06ab142f60c7" /> `Ctrl+Tab` / `Ctrl+Shift+Tab` — switch tabs

<img width="18" height="18" alt="siliputty" src="https://github.com/user-attachments/assets/061a9ceb-814b-4603-81f9-06ab142f60c7" /> `Up` / `Down` — command history

## Plugins

Plugin mode integrates command-line tools already installed on a user's system. Plugins are JSON manifests, require explicit enablement, and cannot inject DLL or WPF code into SiliPuTTY. See the [plugin authoring guide](docs/PLUGINS.md).

Plugin capability declarations are disclosures, not an operating-system sandbox. Only enable plugins from publishers you trust.

## Security and responsible use

Use network discovery, packet capture, and security tools only on systems and networks you own or have explicit permission to test. Discovery is restricted to private RFC 1918 IPv4 `/24` networks.

Passwords are not stored in profiles. Private-key configuration stores a filesystem path, not key contents. Sensitive vulnerabilities should be reported privately as described in [SECURITY.md](SECURITY.md).

## Current limitations

The SSH process requests a remote TTY, but the local display is not yet a complete ConPTY/VT renderer. ANSI control sequences and full-screen interactive programs may not render correctly. Appliance profiles provide practical command presets, but syntax can vary by product family and firmware. Raw, Telnet, Rlogin, Serial, proxy routing, port forwarding, and integrated PSCP/PSFTP surfaces remain planned.

The full implemented/partial/planned matrix is maintained in [FEATURES.md](FEATURES.md).

## License

SiliPuTTY is free and open-source software licensed under the [MIT License](LICENSE).

Copyright © 2026 CtrlAltWiz.

External applications—including PuTTY, Nmap, Wireshark, TShark, and Npcap—are separate projects governed by their own licenses and are not relicensed or automatically distributed by SiliPuTTY.

## Quick tips

<img width="18" height="18" alt="siliputty" src="https://github.com/user-attachments/assets/061a9ceb-814b-4603-81f9-06ab142f60c7" /> Double-click folders to switch directories. Right-click local items to open them in File Explorer. Windows SSH paths can open through an accessible administrative share; other remote paths remain SSH-only.

<img width="18" height="18" alt="siliputty" src="https://github.com/user-attachments/assets/061a9ceb-814b-4603-81f9-06ab142f60c7" /> Type commands at the bottom of the terminal. Use the Up/Down arrows to browse command history.

<img width="18" height="18" alt="siliputty" src="https://github.com/user-attachments/assets/061a9ceb-814b-4603-81f9-06ab142f60c7" /> Click **Help** in the top bar to access the in-app usage guide.

<img width="18" height="18" alt="siliputty" src="https://github.com/user-attachments/assets/061a9ceb-814b-4603-81f9-06ab142f60c7" /> Commands run one after another, so full-screen interactive programs aren't supported in this prototype.

<img width="18" height="18" alt="siliputty" src="https://github.com/user-attachments/assets/061a9ceb-814b-4603-81f9-06ab142f60c7" /> Only use security tools on systems you own or have explicit permission to test.
