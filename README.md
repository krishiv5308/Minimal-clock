Minimal Clock

Minimal clock is a customizable clock for your Windows desktop. Choose from 18 transparent designs that let your wallpaper show through and 12 illustrated clock cards.

## Features

- Browse 30 themes in a searchable library with live previews.
- Customize colors, typeface, size, opacity, and date and time formats.
- Save a different look for each theme.
- Drag the clock to position it. It remembers where you left it.
- The widget refreshes once per minute and updates when Windows wakes.

## Getting started

Download `MinimalDateClock.exe` from Releases and open it. Right-click the clock and choose **Choose theme** to browse designs or **Customize this theme** to edit the current one.

To launch Orbit at sign-in, put a shortcut to the executable in your Windows Startup folder. You can open that folder by pressing Win+R and entering `shell:startup`.

## Build

From the repository root on Windows, run:

    .\work\BuildClock.ps1

The build script produces `work\MinimalDateClock.exe`. To build and run the UI checks, use `.\work\BuildClock.ps1 -Test`.
