# ScreenResy

Change screen resolution, refresh rate and scale from a small window or from the command line.

This is a small utility I wrote to quickly change display resolution and scale.

Typically I run my monitor at 4096x2160 with 150% scale. However, while coding WinForms apps on a large monitor, it helps to write code in 100% scale. While coding I prefer to run my monitor at 3000 x 1700 with 100% scale.

This app allows me to quickly swap between my two preferred resolutions and scale:

```
ScreenResy -x 3000 -y 1700 -d 100
ScreenResy -x 4096 -y 2160 -d 150
```

Put each line in a shortcut and switching is one click or one hotkey.

## Download

Get the latest version from the [Releases](https://github.com/bommerts/ScreenResy/releases) page. There are two downloads; both are a single portable `ScreenResy.exe` with nothing to install.

| Download | Size | Needs |
| --- | --- | --- |
| `ScreenResy-<version>-win-x64-self-contained.zip` | about 44 MB | Nothing. Pick this one if you are unsure. |
| `ScreenResy-<version>-win-x64.zip` | about 0.1 MB | The [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0). |

If the small version does nothing when you start it, the runtime is missing. Start it from a terminal to see the message, or use the self-contained version.

Works on Windows 10 and Windows 11.

## The window

Start `ScreenResy.exe` with no arguments. Pick a display, a resolution, a refresh rate and a scale, then press **Apply**. The lists only show what the selected display supports.

After a resolution or refresh rate change, ScreenResy asks whether to keep it. If you don't answer within 15 seconds it puts the previous settings back, so picking a mode your monitor can't show never leaves you with a blank screen.

## The command line

```
ScreenResy [-x <width> -y <height>] [-r <hz>] [-d <percent>] [-m <display>]
ScreenResy --list
```

| Option | Meaning |
| --- | --- |
| `-x`, `--width <pixels>` | Horizontal resolution. Use together with `-y`. |
| `-y`, `--height <pixels>` | Vertical resolution. Use together with `-x`. |
| `-r`, `--refresh <hz>` | Refresh rate. Without it, Windows picks one. |
| `-d`, `--scale <percent>` | Display scale: 100, 125, 150, 175, 200, 225, 250, 300, 350, 400, 450 or 500. |
| `-m`, `--monitor <number>` | Display to change (see `--list`). Default: the primary display. |
| `-l`, `--list` | Show each display, its current settings and the modes it supports. |
| `-h`, `--help` | Show help. |
| `-v`, `--version` | Show the version. |

Any combination works: resolution only, scale only, or everything at once.

```
ScreenResy -x 1920 -y 1080 -d 125
ScreenResy -x 2560 -y 1440 -r 60 -m 2
ScreenResy -d 150
```

Changes made from the command line are saved, so they survive a restart. Nothing is changed if the display doesn't support what you asked for; ScreenResy says why and sets an exit code a script can check:

| Exit code | Meaning |
| --- | --- |
| 0 | Success |
| 1 | Invalid arguments |
| 2 | Windows refused the change |

## Good to know

- **Scale** uses the same undocumented Windows request the Settings app uses, because Windows has no public API for it. It has worked unchanged since Windows 10, but a future Windows version could break it. Resolution and refresh rate use documented APIs.
- The highest scale a display allows depends on its resolution, so ScreenResy sets the resolution first and the scale second.
- **Before Windows 11 24H2**, a console window flashes briefly when you open the ScreenResy window from Explorer. Newer versions of Windows don't show it.
- In a terminal, `ScreenResy` with no arguments opens the window and the terminal waits until you close it.

## What's new in 2.0

- Lists of supported resolutions, refresh rates and scales instead of free-text boxes, showing the current settings.
- Keep-or-revert confirmation after a mode change.
- Multiple displays: choose one in the window or with `-m`.
- Refresh rate can be set (`-r`).
- `--list`, `--help` and `--version`.
- Resolution, scale and refresh rate can each be set alone. In 1.01, giving only some of them made the change and then reported "Invalid arguments".
- Clear error messages and exit codes. 1.01 reported success even when Windows refused the change, and crashed on non-numeric input in the window.
- A scale Windows doesn't offer (say 110) is refused. 1.01 silently set 100% instead.
- No longer sends an Enter keystroke to whichever window has the focus after a command-line run.
- Resolution changes from the command line now survive a restart, as scale changes always did.
- Sharp at every display scale.
- Runs on .NET 10. 1.01 needed .NET 7, which is no longer supported.

## Building

Needs the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```
dotnet build -c Release
```

The release downloads are made with:

```
dotnet publish ScreenResy/ScreenResy.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:DebugType=none
dotnet publish ScreenResy/ScreenResy.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none
```

## License

[GPL-3.0](LICENSE)
