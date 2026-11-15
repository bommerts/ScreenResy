# ScreenResy

Change a Windows display's resolution, refresh rate and scale, from a small window or from the command line.

ScreenResy is a single 160 KB program written in C. There is nothing to install and nothing it depends on: download it and run it. It works on Windows 10 and Windows 11.

- **One step.** Set resolution, refresh rate and scale together in one command, so a shortcut or a hotkey can switch your whole setup.
- **Only what your display can do.** The window lists the modes and scales your display supports, and the command line refuses anything else and says why.
- **No blank screens.** After a mode change the window asks whether to keep it, and puts the old settings back by itself if you don't answer.
- **Any display.** Works on the primary display or any other one you pick.
- **Made for scripts.** Plain text output and exit codes a script can check.

## Why I wrote it

Typically I run my monitor at 4096x2160 with 150% scale. However, while coding WinForms apps on a large monitor, it helps to write code in 100% scale. While coding I prefer to run my monitor at 3000 x 1700 with 100% scale.

This app allows me to quickly swap between my two preferred resolutions and scale:

```
ScreenResy -x 3000 -y 1700 -d 100
ScreenResy -x 4096 -y 2160 -d 150
```

Put each line in a shortcut and switching is one click or one hotkey.

## Download

Get `ScreenResy.exe` from the [latest release](https://github.com/bommerts/ScreenResy/releases/latest). It is portable: put it anywhere and run it.

Works on Windows 10 and Windows 11 (64-bit).

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

## What's new

### 3.0

- Rewritten in C against the Windows API. ScreenResy no longer needs .NET: the download went from 44 MB to about 160 KB, and it starts several times faster.
- Everything else works as in 2.0.

### 2.0

- Lists of supported resolutions, refresh rates and scales instead of free-text boxes, showing the current settings.
- Keep-or-revert confirmation after a mode change.
- Multiple displays: choose one in the window or with `-m`.
- Refresh rate can be set (`-r`).
- `--list`, `--help` and `--version`.
- Resolution, scale and refresh rate can each be set alone. In 1.01, giving only some of them made the change and then reported "Invalid arguments".
- Clear error messages and exit codes. 1.01 reported success even when Windows refused the change, and crashed on non-numeric input in the window.
- A scale Windows doesn't offer (say 110) is refused. 1.01 silently set 100%.
- No longer sends an Enter keystroke to whichever window has the focus after a command-line run.
- Resolution changes from the command line now survive a restart, as scale changes always did.
- Sharp at every display scale.

## Building

ScreenResy is plain C (C11) and uses only the Windows API. Building it needs [CMake](https://cmake.org/download/) and Visual Studio with the "Desktop development with C++" workload.

```
cmake -S . -B build -A x64
cmake --build build --config Release
```

The result is `build/Release/ScreenResy.exe`. Visual Studio can also open the folder directly.

| File | Contents |
| --- | --- |
| `src/main.c` | Entry point: window with no arguments, command line otherwise. |
| `src/display.c` | Reads and changes resolution, refresh rate and scale. |
| `src/commandline.c` | The command line. |
| `src/window.c` | The window and the keep-or-revert dialog. |
| `src/ScreenResy.rc` | Dialog layouts, version information and the manifest. |
| `src/version.h` | The version number. |

## License

[GPL-3.0](LICENSE)
