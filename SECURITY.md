# Security policy

SiliPuTTY is currently alpha software. Do not use it as the sole access path to production systems until its terminal and authentication workflows have been validated in your environment.

## Reporting a vulnerability

Do not open a public issue containing credentials, hostnames, packet captures, private keys, or exploit details. Use GitHub's private vulnerability reporting / Security Advisory feature for the repository owner.

## Credential handling

- Passwords are never stored in session profiles.
- Use **Secret** to send passwords or passphrases without adding them to terminal history or session logs.
- Private-key configuration stores only a filesystem path, not key contents.
- Plugin manifests are not an OS sandbox. Only enable plugins from trusted publishers.

## Network tools

Network discovery is restricted to RFC 1918 IPv4 `/24` ranges. Packet capture requires an external Wireshark/TShark/Npcap installation and may require elevated capture permissions. Use these features only on networks and systems you are authorized to inspect.
