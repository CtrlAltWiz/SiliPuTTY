# SiliPuTTY capability status

This file distinguishes implemented behavior from configuration UI and planned work. A setting is not considered supported until its transport or terminal engine applies it.

## Implemented

- Independent tabbed persistent local PowerShell, Kali/WSL, and SSH process sessions
- Reconnect, interrupt, disconnect, masked secret entry, SSH host-key warnings, and output logging
- Named session profiles that never store passwords
- Windows, Linux, macOS, and generic network-appliance command palettes
- Persistent categorized defaults for session, logging, terminal, keyboard, bell, window, connection, proxy, SSH, and serial settings
- Saved host, SSH port, username, identity file, compression, agent forwarding, X11 forwarding, wrapping, and terminal colors applied to new tabs
- Private RFC 1918 `/24` discovery with bounded concurrency
- Hostname resolution, ARP/MAC lookup, common-service checks, cancellation, and CSV export
- Optional TShark interface detection, filtered PCAPNG capture, capture stop, and Wireshark/PCAP launching
- PuTTY, OpenSSH, TShark, and Wireshark capability detection
- Validated, explicitly enabled, data-only plugin manifests for pre-existing command-line tools
- Crash diagnostics, dependency-free smoke tests, Windows CI, portable packaging, installer scaffolding, and optional code-signing hook

## Partially implemented

- SSH requests a remote TTY and remains persistent, but the local display is still a TextBox rather than a ConPTY/VT renderer. ANSI control sequences and full-screen programs are not rendered correctly.
- Configuration categories persist all displayed values, but only the settings listed above currently affect sessions.
- Packet decoding and display filters are delegated to TShark/Wireshark when installed.
- MAC vendor names require a vendor database and are not yet resolved.

## Planned before claiming PuTTY parity

- ConPTY-backed VT terminal rendering and resize events
- Raw, Telnet, Rlogin, and Serial transports
- Named session profile load/save/delete
- Session logging modes and collision behavior
- Keyboard/application keypad modes, bell behavior, cursor appearance, selection, and ANSI palette
- SOCKS/HTTP/Telnet proxy routing and environment variables
- SSH port forwarding, jump hosts, host-key management, cipher policy, and connection sharing
- PSCP/PSFTP file transfer surface
- Wake-on-LAN and optional RDP launch for discovered devices
- Packet list/detail/byte panes, saved display filters, coloring rules, stream following, and protocol statistics

Remote shutdown is intentionally excluded from one-click discovery actions because it is destructive.
