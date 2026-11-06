# OpenTabletDriver plugin for the Wacom `056A:5229` pen digitizer

[OpenTabletDriver](https://opentabletdriver.net) report parser and configuration for the built-in
"Pen and multitouch sensor" of Lenovo ThinkPad X1 Yoga Gen 5 (probably other generations too).
The device is a standard USB HID digitizer (Wacom, VID `056A`, PID `5229`), and OpenTabletDriver
has no configuration for it.

Developed and tested on a Hackintosh (macOS Ventura 13.7.8) with OpenTabletDriver 0.6.8 . See [thinkpad-x1-yoga-5-hack](https://github.com/AlexFullmoon/thinkpad-x1-yoga-5-hack)
for the whole setup (including patched VoodooI2CHID, which is needed to avoid duplicate pen input).
Not tested and probably not needed on other systems.

What works: position, pressure (0–4095), two pen buttons, eraser, tilt (with a pen that reports it),
tool type and serial number.

## Installation

1. Copy `WacomHidPlugin.dll` (build it, see below) to `Plugins/WacomHidPlugin/`
   in the OpenTabletDriver user folder (`~/Library/Application Support/OpenTabletDriver` on macOS).
2. Copy [`configs/Wacom-056A-5229.json`](configs/Wacom-056A-5229.json) to `Configurations/Wacom/`
   in the same folder.
3. Restart OpenTabletDriver. The log should contain `Found tablet 'Wacom Integrated Pen Display (056A:5229)'`.

[`configs/Wacom-056A-5229-Debug.json`](configs/Wacom-056A-5229-Debug.json) is the same configuration
with the built-in `PassthroughReportParser`: it needs no plugin, and Tablet Debugger shows raw reports.
Both files have the same device name, so use only one of them at a time.

## Building

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download) (the installed OpenTabletDriver runs on .NET 10)
and the OpenTabletDriver installation itself: the project references its `OpenTabletDriver.Plugin.dll`,
so the plugin always matches the API of the version you run.

```sh
dotnet build -c Release
# bin/Release/net10.0/WacomHidPlugin.dll
```

The default location of OpenTabletDriver is `/Applications/OpenTabletDriver.app/Contents/MacOS`.
For anything else pass it explicitly:

```sh
dotnet build -c Release -p:OtdDir=/path/to/opentabletdriver
```

After updating OpenTabletDriver the plugin might stop loading (log message "Unable to find plugin matching name
'WacomHidPlugin…'"). Rebuild it against the new version; if the API changed, the code needs adjustments.

## Report format

Determined from the HID report descriptor and verified with Tablet Debugger recordings.
The device has a single HID interface, its largest input report is 59 bytes (that is the value of
`InputReportLength`, as reported by macOS; other systems may differ).

Pen report, Report ID `0x06`, 18 bytes:

| Byte  | Meaning                                                                          |
| ----- | -------------------------------------------------------------------------------- |
| 0     | Report ID (6)                                                                    |
| 1     | Bits: 0 tip, 1 barrel button, 2 eraser, 3 invert, 4 second barrel button, 5 in range |
| 2–3   | X, 0–30931 (100 lines per mm, 309.31 mm)                                          |
| 4–5   | Y, 0–17399 (173.99 mm)                                                           |
| 6–7   | Pressure, 0–4095                                                                 |
| 8–9   | Tool ID (`0x0011` for the bundled pen)                                           |
| 10–13 | Tool serial number                                                               |
| 14    | Vendor data, always 0 in recordings                                              |
| 15    | Declared as "battery strength", see below                                        |
| 16–17 | Tilt X and Y, signed degrees                                                     |

Touch report, Report ID `0x0C`, 40 bytes: contact count at byte 2, then 5 contact slots of 7 bytes
from byte 3 (flags with bit 0 = tip, contact ID, X, Y; X up to 12372, Y up to 6960), scan time in the last 2 bytes.
The parser also converts these to `ITouchReport`, but OpenTabletDriver cannot pass touch input to the system,
so it is only visible in Tablet Debugger.

## Caveats

- **Do not** add the generic Wacom `FeatureInitReport` (`AgI=`, i.e. feature report 2 = 2) that most Wacom
  configurations use. It switches the digitizer to a legacy mode with a different product ID (`056A:0094`) and no
  usable HID reports: OpenTabletDriver does not detect the tablet, and the pen stops working in macOS too.
  A reboot or shutdown should reset it.
- Byte 15 jumps between 0 and 128 (bundled pen) or 255 (other pen), independently of the pen state. Its meaning
  is not confirmed (battery level? hover distance?). The parser ignores it, there is a commented-out
  logging snippet in the source.
- The bundled pen does not report tilt. Another pen worked, tilt up to ±65° was seen.
- The configuration size (309.31 × 173.99 mm) is calculated from the descriptor, not measured.

## License

Not chosen yet.
