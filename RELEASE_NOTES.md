<!-- Release notes for the next tagged release. The workflow fills in {{VERSION}} and the SHA-256 checksums. Edit 'What's new' before tagging. -->
Free, open-source AirPlay screen mirroring for Windows. Mirror your iPhone or iPad to your PC with low latency, 60 FPS and GPU decoding.

## What's new in 1.4.1

- **New logo:** an open circle that flows into a lowercase "i". It's used for the window, taskbar, tray, installer and shortcuts.
- **Status light:** the status dot glows **green** when iStream is ready (it pulses while streaming), **orange** while starting or stopping, and **red** when the server is stopped.
- The sidebar no longer lists the keyboard shortcuts. *Ctrl+Alt+F* (fullscreen) and *Ctrl+Alt+R* (rotate) still work.
- First release built and published by GitHub Actions straight from the source code.
- If you have 1.4.0, it updates itself to this version.

## Also in 1.4

- **Automatic updates.** iStream checks GitHub for new versions (30 s after start, then every 6 hours), downloads them in the background and installs them when nothing is streaming and the window isn't in use. It never interrupts a stream. Afterwards it comes back the way it was (in the tray if it was hidden) and tells you it was updated.
- **Check for updates** button in *Settings → Updates*, plus *Update to x.y.z* in the tray menu when a new version is ready.
- Every downloaded installer is verified against its **SHA-256** before it runs. A mismatch is deleted and never run.
- Turn automatic updates off in *Settings → Updates*. The portable ZIP version only tells you about updates and opens the download page.
- Coming from 1.3.x or older? Update once by hand (those versions have no updater). From 1.4.0 on, updates install themselves.

## Also in 1.3

- **Runs in the background.** Closing the window now hides iStream to the tray, so your iPhone can mirror to the PC any time. Click the tray icon to open it; right-click for *Start/Stop server*, *Fullscreen* and *Quit iStream*. You can turn this off in *Settings → Keep running in the background*.
- **Quiet start at sign-in.** With *Launch at Windows sign-in* on, iStream starts hidden in the tray, ready to receive.
- Opening iStream again (Start menu, desktop shortcut) brings back the running copy instead of starting a second one.
- Changing the theme no longer scrolls the Settings page back to the top, and the scrollbar scrolls live while you drag it.

Tip: Windows 11 hides new tray icons under the **^** arrow next to the clock. Drag the iStream icon onto the taskbar to keep it visible.

## Download

| File | What it is |
|---|---|
| **[iStream-Setup-{{VERSION}}.exe](https://github.com/olehnykorovych/iStream-AirPlay/releases/download/v{{VERSION}}/iStream-Setup-{{VERSION}}.exe)** | **Installer (recommended).** Run it, click *Install*, done. No administrator rights; adds iStream to the Start menu and to *Settings → Apps* for uninstalling. |
| [iStream-v{{VERSION}}-win-x64.zip](https://github.com/olehnykorovych/iStream-AirPlay/releases/download/v{{VERSION}}/iStream-v{{VERSION}}-win-x64.zip) | Portable version. Unzip anywhere and run `iStream.exe`. |

## Highlights

- **Fast:** hardware H.264/H.265 decoding on the GPU (Direct3D 11 or 12), 60 FPS, low-latency mode
- **Up to 4K** (H.265), plus 1080p and 1440p presets
- **Rotate the picture** in 90° steps on the GPU, without reconnecting
- **Borderless fullscreen** on any monitor, optionally on connect
- **Settings never cut the stream:** engine changes wait until the phone disconnects, or apply them with one click
- **One-click firewall fix** when Windows blocks incoming connections
- **Always ready in the background** (tray icon), optionally from Windows sign-in
- **Light, gray and dark themes**: follows Windows, or pick one in *Settings → Appearance*
- **Six languages:** English, Polski, Deutsch, Español, Français, Українська
- Shortcuts: **Ctrl+Alt+F** fullscreen, **Ctrl+Alt+R** rotate

## Quick start

1. Run `iStream-Setup-{{VERSION}}.exe` and click **Install** (or unzip the portable version and run `iStream.exe`). If SmartScreen warns, click **More info → Run anyway** (the app is not code-signed).
2. If a banner says the firewall is blocking iStream, click **Fix firewall**.
3. On your iPhone: **Control Center → Screen Mirroring → iStream**. The phone and the PC must be on the same network.

## Requirements

Windows 10 or 11 (64-bit), a GPU with Direct3D 11, and the iPhone/iPad on the same Wi-Fi or LAN as the PC.

## Checksums (SHA-256)

```
{{SHA256_SETUP}}  iStream-Setup-{{VERSION}}.exe
{{SHA256_ZIP}}  iStream-v{{VERSION}}-win-x64.zip
```

Built on the open-source [UxPlay](https://github.com/FDH2/UxPlay) engine (Windows build by [leapbtw/uxplay-windows](https://github.com/leapbtw/uxplay-windows) 2.0.0.1736). Licensed under GPL-3.0. Third-party licenses are in `THIRD_PARTY_NOTICES.md`.

iStream is not affiliated with Apple. AirPlay, iPhone and iPad are trademarks of Apple Inc.
