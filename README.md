# What on Earth is SillyPutty?!

SillyPutty is like PuTTY’s quirky cousin for Windows terminals, packing local PowerShell, Kali Linux through WSL, SSH command magic, one-click Kali gadget access, and a live file browser that’s always in sync.

The app spots whether you're on Windows, Linux, or macOS as soon as you connect and then whips up a session-tool palette packed with commands that actually make sense for your platform. If it stumbles upon an unfamiliar SSH platform, it just shrugs and defaults to a generic appliance profile for switches, routers, and firewalls. And if you’re feeling rebellious, the Platform menu lets you override its guesswork whenever you want.

## Run

```powershell
dotnet run --project .\SillyPutty.csproj
```

To use Kali mode, set up Kali on WSL with the distro named `kali-linux`. SSH mode relies on Windows' OpenSSH client. The tool buttons execute commands in the session you’ve picked; just make sure the tools are already installed there.

## Notes

- Double-click on folders to switch the local terminal directory; double-click files to open them with Windows.
- Type commands at the bottom of the terminal. Use the Up/Down arrows to browse command history.
- Click **Help** in the top bar to access the in-app usage guide.
- Commands run one after another, so full-screen interactive programs aren't supported in this prototype.
- Only use security tools on systems you own or have explicit permission to test.
