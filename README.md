# DDR Mini Light

**English** | [한국어](README.ko.md) | [日本語](README.ja.md)

A Windows tray app that lights up the arrow LEDs of the **DDR Classic Mini** 1P/2P pads (USB VID `0x33DD` / PID `0x0014`) to match your input when the pads are connected to a PC.

> This is an unofficial fan-made tool. It is not affiliated with or endorsed by KONAMI or the hardware manufacturer.

## Features

- Lights up the LED of each pressed arrow (modes: **Light pressed buttons** / **Always on** / **Off**)
- Supports multiple pads at once, with automatic USB connect/disconnect detection
- 1P/2P assignment: click **Assign 1P/2P by pressing** and press any button on the 1P pad, or use the 1P/2P buttons on each pad card
  - Assigned pads are shown in 1P → 2P order. The **Identify** button blinks that pad's LEDs
- Shows each pad's input (4 arrows + the bottom Menu Left / Cancel / Select / Menu Right buttons) and LED state in real time
- Stays in the system tray: closing the window hides it to the tray. The tray menu changes the mode, runs an LED test, or exits
- Optional start with Windows (starts hidden in the tray)
- Single instance: launching it again opens the existing window
- Turns all LEDs off on exit
- English, Korean, and Japanese UI. Follows the Windows display language by default; change it under **Options → Language** (the app restarts to apply it)

## Download

Get the latest build from [Releases](https://github.com/blurtic2/ddr-mini-light/releases).

| File | Description |
|---|---|
| `DDRMiniLight-vX.Y.Z-win-x64.zip` | Small executable. Requires the [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0). |
| `DDRMiniLight-vX.Y.Z-win-x64-selfcontained.zip` | Includes the runtime. Runs without installing anything. |

Unzip and run `DDRMiniLight.exe`. Settings are saved to `%AppData%\DDRMiniLight\settings.json`.

- Windows 11 puts new tray icons in the hidden icons area (^) of the taskbar. Drag the icon onto the taskbar to keep it visible.
- The 1P and 2P pads cannot be told apart by hardware, so they are identified by the USB port they are plugged into. If you move a pad to another port, assign it again.

## Project structure

```
DDRMiniLight.sln
├─ src/
│  ├─ DDRMiniLight.Core/          HID communication library (no UI)
│  │  ├─ DdrMiniProtocol.cs       Input/LED report format (bit positions)
│  │  ├─ DdrMiniDevice.cs         One pad: reader thread, LED output
│  │  ├─ DeviceManager.cs         Connect/disconnect detection, device list, LED test
│  │  ├─ DeviceLogEventArgs.cs    Device events for the log (the app turns them into text)
│  │  ├─ PadSideMap.cs            Device path → 1P/2P assignment (swap, auto-assign)
│  │  ├─ PadSide.cs               1P / 2P / unassigned
│  │  ├─ PadButtons.cs            Button/LED flags
│  │  └─ LightMode.cs             LED mode
│  └─ DDRMiniLight.App/           WinForms tray app (DDRMiniLight.exe)
│     ├─ Program.cs               Entry point, single instance
│     ├─ TrayApplicationContext.cs  Tray icon/menu, app lifetime, settings
│     ├─ MainForm.cs              Main window
│     ├─ Controls/PadView.cs      Pad drawing
│     ├─ Controls/DeviceCard.cs   Per-pad card
│     ├─ Strings.cs               UI text (Korean / English / Japanese)
│     ├─ AppSettings.cs           %AppData%\DDRMiniLight\settings.json
│     ├─ StartupRegistration.cs   Start with Windows (HKCU\...\Run)
│     └─ AppIcons.cs              App/tray icons (drawn at runtime)
└─ tests/
   └─ DDRMiniLight.Core.Tests/    Protocol and 1P/2P assignment unit tests (xUnit)
```

## Building

Requires the .NET 8 SDK.

```bash
dotnet build -c Release
dotnet test
```

### Building an executable for local use

Creates a single executable (`DDRMiniLight.exe`, about 0.5 MB, requires the .NET 8 Desktop Runtime) in the `output` folder at the repository root.

```bash
dotnet publish src/DDRMiniLight.App -p:PublishProfile=Local
```

In Visual Studio, right-click the project → **Publish** → **Local** does the same.

- Run `output\DDRMiniLight.exe` for everyday use, and enable "start with Windows" from that executable.
- Exit the app from the tray before publishing again. The executable is locked while it runs.
- If the app is running in the tray, debugging from Visual Studio only brings up the existing window and exits, because of the single-instance check.

## Protocol notes

Indexes include the report ID byte (`[0]`).

| Button | Input bit | HID button # | LED output byte |
|--------|-----------|--------------|-----------------|
| ←          | `[3]` bit5 | 6  | `[3]` |
| ↓          | `[3]` bit4 | 5  | `[2]` |
| ↑          | `[4]` bit2 | 11 | `[4]` |
| →          | `[4]` bit3 | 12 | `[1]` |
| Menu Left  | `[3]` bit6 | 7  | none |
| Cancel     | `[3]` bit1 | 2  | none |
| Select     | `[3]` bit0 | 1  | none |
| Menu Right | `[3]` bit7 | 8  | none |

LED output report: `[0] = 0x05` (report ID). The other bytes are 0 (off) or 1 (on).

Per the report descriptor, input report ID 1 is `[1]` X axis, `[2]` Y axis, `[3..4]` 16 buttons.
The 1P and 2P pads use the same bits.

### Telling 1P and 2P apart

The 1P and 2P pads have the same descriptor, version, and product name, and no serial number, so the hardware cannot tell them apart.
The app uses the HID device path, which differs per USB port, as the key and saves the user's 1P/2P assignment in the settings file.
The assignment is kept as long as each pad stays in the same port.
