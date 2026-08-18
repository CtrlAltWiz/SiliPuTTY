# What on Earth is SillyPutty?! <img width="32" height="32" alt="sillyputty" src="https://github.com/user-attachments/assets/061a9ceb-814b-4603-81f9-06ab142f60c7" />

<img width="18" height="18" alt="sillyputty" src="https://github.com/user-attachments/assets/061a9ceb-814b-4603-81f9-06ab142f60c7" /> SillyPutty is like PuTTY’s quirky cousin for Windows terminals, packing local PowerShell, Kali Linux through WSL, SSH command magic, one-click Kali gadget access, and a live file browser that’s always in sync.

<img width="18" height="18" alt="sillyputty" src="https://github.com/user-attachments/assets/061a9ceb-814b-4603-81f9-06ab142f60c7" /> The app spots whether you're on Windows, Linux, or macOS as soon as you connect and then whips up a session-tool palette packed with commands that actually make sense for your platform. If it stumbles upon an unfamiliar SSH platform, it just shrugs and defaults to a generic appliance profile for switches, routers, and firewalls. And if you’re feeling rebellious, the Platform menu lets you override its guesswork whenever you want.

## Run

```powershell
dotnet run --project .\SillyPutty.csproj
```

<img width="18" height="18" alt="sillyputty" src="https://github.com/user-attachments/assets/061a9ceb-814b-4603-81f9-06ab142f60c7" /> To use Kali mode, set up Kali on WSL with the distro named `kali-linux`. SSH mode relies on Windows' OpenSSH client. The tool buttons execute commands in the session you’ve picked; just make sure the tools are already installed there.

## Notes

<img width="18" height="18" alt="sillyputty" src="https://github.com/user-attachments/assets/061a9ceb-814b-4603-81f9-06ab142f60c7" /> Double-click on folders to switch the local terminal directory; double-click files to open them with Windows.

<img width="18" height="18" alt="sillyputty" src="https://github.com/user-attachments/assets/061a9ceb-814b-4603-81f9-06ab142f60c7" /> Type commands at the bottom of the terminal. Use the Up/Down arrows to browse command history.

<img width="18" height="18" alt="sillyputty" src="https://github.com/user-attachments/assets/061a9ceb-814b-4603-81f9-06ab142f60c7" /> Click **Help** in the top bar to access the in-app usage guide.

<img width="18" height="18" alt="sillyputty" src="https://github.com/user-attachments/assets/061a9ceb-814b-4603-81f9-06ab142f60c7" /> Commands run one after another, so full-screen interactive programs aren't supported in this prototype.

<img width="18" height="18" alt="sillyputty" src="https://github.com/user-attachments/assets/061a9ceb-814b-4603-81f9-06ab142f60c7" /> Only use security tools on systems you own or have explicit permission to test.
