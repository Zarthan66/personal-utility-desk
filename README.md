# Personal Utility Desk

Personal Utility Desk is a small collection of Windows utilities built for everyday situations where a focused tool is more useful than a larger application. It is a personal project and may grow as new needs arise.

## Current utilities

- **Video Downloader** — checks for `yt-dlp`, can install it through Windows Package Manager (`winget`), and lets you choose a save directory.
- **Shutdown Timer** — planned utility for scheduling a Windows shutdown when you may accidentally fall asleep before turning off your PC.

Some navigation items and features are still works in progress.

## Technology

- .NET 8
- WinUI 3 and Windows App SDK
- C#
- Windows 10 version 1809 or later

## Prerequisites

- Windows 10 version 1809 or later
- Visual Studio with .NET desktop development and Windows App SDK/WinUI support
- .NET 8 SDK
- `winget` if you want the application to install `yt-dlp` for you

## Build and run

1. Clone the repository.
2. Open `PrototypePT1.slnx` in Visual Studio.
3. Select an available Windows target such as `x64`.
4. Build and run the project.

The project can also be built from PowerShell with:

```powershell
dotnet build .\PrototypePT1.csproj -p:Platform=x64
```

## Notes

This project is maintained primarily for personal use. Features, interfaces, and implementation details may change as utilities are added or refined. The application currently targets Windows and is not intended to be cross-platform.

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.
