using System;
using System.Numerics;
using OpenTabletDriver.Plugin;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Tablet.Touch;

namespace WacomHidPlugin
{
    public class WacomHidPenReportParser : IReportParser<IDeviceReport>
    {
        // private int lastBattery = -1;

        public IDeviceReport Parse(byte[] report)
        {
            // Report ID 12 is multitouch, ID 6 is the pen
            if (report[0] == 0x0C && report.Length >= 40)
                return new WacomHidTouchReport(report);

            if (report.Length < 18 || report[0] != 0x06)
                return new DeviceReport(report);

            // bit 5 of the button byte is "In Range"
            if ((report[1] & 0x20) == 0)
                return new OutOfRangeReport(report);

            // Byte 15 is declared as HID "Battery Strength", but it jumps between 0 and a power of two
            // (128 / 255) independent of pen state, so its meaning is unconfirmed (maybe hover distance).
            // if (report[15] != lastBattery)
            // {
            //     lastBattery = report[15];
            //     Log.Write("WacomHid", $"Pen battery strength (raw byte): {lastBattery}", LogLevel.Debug);
            // }

            return new WacomHidPenReport(report);
        }
    }

    public struct WacomHidPenReport : ITabletReport, ITiltReport, IEraserReport, IToolReport
    {
        public WacomHidPenReport(byte[] report)
        {
            Raw = report;

            Position = new Vector2(BitConverter.ToUInt16(report, 2), BitConverter.ToUInt16(report, 4));
            Pressure = BitConverter.ToUInt16(report, 6);

            // Button byte: 0 tip, 1 barrel, 2 eraser, 3 invert, 4 secondary barrel, 5 in range
            PenButtons = new bool[]
            {
                (report[1] & 0x02) != 0,
                (report[1] & 0x10) != 0
            };
            Eraser = (report[1] & 0x0C) != 0;

            // Bytes 8-9 identify the tool type, bytes 10-13 are the pen's own serial number
            RawToolID = BitConverter.ToUInt16(report, 8);
            Serial = BitConverter.ToUInt32(report, 10);
            Tool = Eraser ? ToolType.Eraser : ToolType.Pen;

            // Tilt is signed degrees, -90..90
            Tilt = new Vector2((sbyte)report[16], (sbyte)report[17]);
        }

        public byte[] Raw { set; get; }
        public Vector2 Position { set; get; }
        public uint Pressure { set; get; }
        public bool[] PenButtons { set; get; }
        public Vector2 Tilt { set; get; }
        public bool Eraser { set; get; }
        public ulong Serial { set; get; }
        public uint RawToolID { set; get; }
        public ToolType Tool { set; get; }
    }

    public struct WacomHidTouchReport : ITouchReport
    {
        private const int MaxContacts = 5;

        public WacomHidTouchReport(byte[] report)
        {
            Raw = report;

            // 5 contact slots of 7 bytes from offset 3: flags, contact id (16 bit), X, Y.
            // Flags bit 0 is Tip Switch; a slot without it is empty or lifting.
            Touches = new TouchPoint[MaxContacts];
            for (int i = 0; i < MaxContacts; i++)
            {
                int o = 3 + i * 7;
                if ((report[o] & 0x01) == 0)
                    continue;

                Touches[i] = new TouchPoint
                {
                    TouchID = (byte)BitConverter.ToUInt16(report, o + 1),
                    Position = new Vector2(BitConverter.ToUInt16(report, o + 3), BitConverter.ToUInt16(report, o + 5))
                };
            }
        }

        public byte[] Raw { set; get; }
        public TouchPoint[] Touches { set; get; }
    }
}
