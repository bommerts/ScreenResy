Programmatically change screen resolution and scale via GUI or commandline.

This is a small utility I wrote to quickly change display resolution and scale.

Typically I run my monitor at 4096x2160 with 150% scale. However, while coding WinForms apps on a large monitor, it helps to write code in 100% scale. While coding I prefer to run my monitor at 3000 x 1700 with 100% scale.

This app allows me to quickly swap between my two preferred resolutions and scale.

Tested on Windows 11 23H2 but should run on all versions of Windows 10 and 11.

Runs either in GUI mode or with commandline arguments.

Usage: ScreenResy.exe -x [width] -y [height] -d [dpi]

Example: ScreenResy.exe -x 1920 -y 1080 -d 125
