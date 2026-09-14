<h1 align="center"> VIBECODED BLOXSTRAP FORK!</h1>

<p align="center">
  <img src="Bloxstrap/Microstrap.ico" width="112" alt="Microstrap logo">
</p>

<h1 align="center">Microstrap</h1>

<p align="center">The best free, lightweight Roblox bootstrapper.</p>

Microstrap is a sleek, open-source replacement for the standard Roblox bootstrapper. It keeps your launch experience quick and clean while giving you thoughtful control over settings, integrations, mods, and engine options.

> **Version 0.1.0** — Microstrap is an early release. Expect the core experience to stay lightweight while features continue to grow.

> **🟧 WARNING:** ONLY DOWNLOAD MICROSTRAP FROM THIS REPO OR FROM **microstraplabs.freebuff.app**. ANY OTHER SITE MAY BE FALSE AND CONTAIN MALWARE.

## Highlights

- Fast, simple Roblox and Roblox Studio launching
- A polished Windows interface with dark and light themes
- Discord Rich Presence and activity integrations
- Optional mod support for compatible Roblox content
- Graphics and engine settings in one place
- No paid features, subscriptions, or unnecessary bloat

## Installing

Download the latest Microstrap release from the [GitHub releases page](https://github.com/microstraplabs/microstrap/releases/latest), then run the installer. Microstrap is currently supported on Windows PCs with the .NET 6 Desktop Runtime.

## Building

Microstrap uses WPF and the maintained WPF UI project included in this repository. Open `Bloxstrap.sln` in Visual Studio or build it with the .NET SDK:

```powershell
dotnet build Bloxstrap.sln
```

The source project keeps its historical `Bloxstrap` folder and namespaces to preserve compatibility with the existing codebase, while the shipped application, assembly, shortcuts, and UI are branded Microstrap.

## License

See [LICENSE](LICENSE) for the project license.
