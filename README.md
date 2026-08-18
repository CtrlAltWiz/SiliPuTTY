# SillyPutty

A native Windows terminal dashboard inspired by PuTTY, with local PowerShell, Kali Linux via WSL, SSH command execution, one-click Kali tools, and a live synchronized file browser.

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
