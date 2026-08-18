# Release checklist

- [x] Release configuration builds with zero warnings
- [x] Dependency-free smoke tests pass
- [x] Self-contained `win-x64` package builds and launches
- [x] GitHub Actions build/test/publish workflow exists
- [x] Crash diagnostics write to `%LOCALAPPDATA%\SillyPutty\CrashLogs`
- [x] Capability matrix distinguishes active and planned settings
- [x] Plugin manifests require validation and explicit enablement
- [ ] Complete ConPTY/VT renderer validation for interactive full-screen programs
- [ ] Install Wireshark/TShark/Npcap and validate capture start/stop with representative PCAPNG files
- [ ] Install Inno Setup and compile/test the installer on a clean Windows VM
- [ ] Obtain a trusted code-signing certificate and sign/timestamp the executable and installer
- [ ] Choose and add an open-source license before making the repository public
- [ ] Complete accessibility, DPI, keyboard-only, and screen-reader testing
- [ ] Run a limited private beta and triage crash logs before public release
