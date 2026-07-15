# Stay on Top

A tiny, dependency-free Windows utility that pins any window "always on top"
via a global hotkey or the system tray menu.

No installer. No .NET runtime to download separately. No third-party
libraries. It compiles with `csc.exe`, the C# compiler already built into
Windows 10 and 11.

## Features

- **Global hotkey — Ctrl+Alt+T** — pin/unpin whatever window is currently focused, from anywhere.
- **System tray icon** — right-click for the full menu; double-click to pin/unpin the active window.
- **Pinned windows list** — see everything currently pinned and unpin any of them directly from the tray menu.
- **Start with Windows** — one checkbox in the tray menu, no manual shortcut needed.
- **Pin state always matches reality** — "is this pinned?" is answered by asking Windows directly (the real topmost window style), not by a cache that can go stale.
- **~350 lines of plain C#**, split into small readable files, no external NuGet packages.

## Download & run

Grab `StayOnTop.exe` from the [Releases](../../releases) page (or the latest
[GitHub Actions build artifact](../../actions)) and run it. That's it — a
single `.exe`, nothing else to install.

## Build it yourself

```
git clone https://github.com/<your-username>/StayOnTop.git
cd StayOnTop
build.bat
```

`build.bat` finds Windows' built-in C# compiler and compiles everything in
`src\` into `StayOnTop.exe`. No Visual Studio, no .NET SDK install required.

## Usage

- Focus any window and press **Ctrl+Alt+T** → it's pinned on top of everything else. Press it again to unpin.
- Right-click the tray icon (bottom-right of the taskbar, possibly in the "^" hidden icons overflow) for:
  - A live list of pinned windows (click one to unpin it)
  - "Start with Windows" toggle
  - About / Exit

Note: pinning/unpinning is only done via the **Ctrl+Alt+T hotkey**, not from
the tray menu. This is intentional — clicking the tray icon itself forces
Windows to briefly hand foreground focus to the taskbar (a Windows
requirement for how tray icon menus behave), so by the time a tray menu item
would run, "the active window" would actually mean the taskbar itself, not
whatever you were just working in. The hotkey doesn't have this problem since
no click on the taskbar is involved.

## Run automatically at startup

Just tick **"Start with Windows"** in the tray menu — it adds itself to your
user's Startup Apps (`HKCU\...\Run`), the same list you'll see under
**Settings > Apps > Startup**. Untick it to remove it. No manual shortcut
copying needed.

## Customizing

Everything is in `src/`:
- `TrayApplicationContext.cs` — tray icon, menu, hotkey wiring
- `PinManager.cs` — pin/unpin logic
- `StartupHelper.cs` — Windows startup registry entry
- `NativeMethods.cs` — all Win32 P/Invoke declarations

To change the hotkey, edit `HotkeyVk` in `TrayApplicationContext.cs` (Win32
virtual-key code) and/or the `MOD_CONTROL | MOD_ALT` modifiers, then rebuild.

## License

MIT — see [LICENSE](LICENSE).
