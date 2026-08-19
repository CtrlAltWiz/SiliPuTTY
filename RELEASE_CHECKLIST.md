# Release checklist

- [x] Release configuration builds with zero warnings
- [x] Dependency-free smoke tests pass
- [x] Self-contained `win-x64` package builds and launches
- [x] GitHub Actions build/test/publish workflow exists
- [x] Tag-driven GitHub Releases publish a portable ZIP, installer, and SHA-256 checksums
- [x] Crash diagnostics write to `%LOCALAPPDATA%\SiliPuTTY\CrashLogs`
- [x] Capability matrix distinguishes active and planned settings
- [x] Plugin manifests require validation and explicit enablement
- [ ] Complete ConPTY/VT renderer validation for interactive full-screen programs
- [ ] Install Wireshark/TShark/Npcap and validate capture start/stop with representative PCAPNG files
- [ ] Confirm the automated installer install/launch/uninstall smoke test passes on a tagged clean Windows runner
- [ ] Obtain a trusted code-signing certificate and sign/timestamp the executable and installer
- [x] MIT license is included and the repository is public
- [ ] Complete accessibility, DPI, keyboard-only, and screen-reader testing
- [ ] Run a limited private beta and triage crash logs before public release
- [ ] Field-test Cisco, Aruba, Fortinet, Palo Alto, Juniper, and generic appliance profiles on authorized hardware
