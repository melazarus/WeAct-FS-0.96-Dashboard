//https://github.com/WeActStudio/WeActStudio.SystemMonitor/blob/main/library/lcd/lcd_comm_weact_b.py

using System.Runtime.CompilerServices;

namespace WeActLCD.Driver
{
    public class Command
    {
        public bool RequestResponse {get; set;} 
        public List<byte[]> Data { get; set; } = new List<byte[]>();
        public string? Response { get; set; }
        public bool Processed { get; set; } = false;

        public Command(byte[] data)
        {
            Data.Add((byte[])data.Clone());
            RequestResponse = false;
        }

        public static Command Fill(ushort color, int width, int height)
        {
            return new Command([(byte)CommandCode.CMD_FULL,
                0,0,0,0,(byte)(width),0,(byte)(height),0,
                (byte)(color & 0xFF),(byte)(color >> 8),
                (byte)CommandCode.CMD_END,
            ]);
        }

        public static Command SetOrientation(DisplayOrientation orientation)
        {
            return new Command([(byte)CommandCode.CMD_SET_ORIENTATION, (byte)orientation, (byte)CommandCode.CMD_END]);
        }

        public static Command GetSystemVersion()
        {
            var command = new Command([(byte)CommandCode.CMD_SYSTEM_VERSION | (byte)CommandCode.CMD_READ, (byte)CommandCode.CMD_END]);
            command.RequestResponse = true;
            return command;
        }

        public static Command SetBrightness(byte level, ushort duration)
        {
            return new Command([(byte)CommandCode.CMD_SET_BRIGHTNESS, level, (byte)(duration & 0xFF), (byte)(duration >> 8 & 0xFF), (byte)CommandCode.CMD_END]);
        }

        public static Command SetRawBitmap(byte[] rawBitmap)
        {
            var command = new Command([(byte)CommandCode.CMD_SET_BITMAP, 0, 0, 0, 0, 159, 0, 79, 0, (byte)CommandCode.CMD_END]);

            for (int i = 0; i < 160; i++)
            {
                var chunk = new byte[160];
                Array.Copy(rawBitmap, 160 * i, chunk, 0, 160);
                var clone = (byte[])chunk.Clone();
                command.Data.Add(clone);
            }

            return command;
        }

        //public static Command SetCompressedBitmap(byte[] rawBitmap)
        //{

        //}


        internal void WaitForResponse()
        {
            while (!Processed) { Thread.Sleep(1); } //todo async
        }
    }
}

