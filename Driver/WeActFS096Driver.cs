//using DotFastLZ.Compression;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

//https://github.com/WeActStudio/WeActStudio.SystemMonitor/blob/main/library/lcd/lcd_comm_weact_b.py

namespace WeActLCD.Driver
{
    public class WeActFS096Driver : IUartDisplayDriver
    {
        private string? _portName;
        private SerialPort? _serialPort;
        private DeviceInfo _deviceInfo;
        private DisplayOrientation _currentOrientation = DisplayOrientation.Portrait;
        private byte _currentBrightness = 255;

        private BlockingCollection<Command> _command_queue = new BlockingCollection<Command>(new ConcurrentQueue<Command>(), 100);
        private CancellationTokenSource _cts;
        private Task _portWriter;

        public (int width, int height) Resolution { get; } = (160, 80);
        //todo: getwidth get heigt based on orientation
        public string BusReportedDeviceName { get => "Display FS 0.96 Inch"; }

        public WeActFS096Driver(string portName)
        {
            _portName = portName;
        }

        public void Connect()
        {
            if (_portName == null) throw new ArgumentNullException("portName should not be null");
            _serialPort = new(_portName, 115200);
            _serialPort.Open();
            _cts = new CancellationTokenSource();
            _portWriter = Task.Run(CommandProcessor, _cts.Token);  
        }

        public void CommandProcessor()
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                if (!_serialPort.IsOpen)
                {
                    try { _serialPort.Open(); } catch (FileNotFoundException e) { }
                    
                    /* TODO restore orientation and brightness (and last image?) */
                    //SetOrientation(_currentOrientation);
                    //SetBrightness(100); // same here
                    Thread.Sleep(100); 
                    continue;
                }
                                
                Command? command = null;
                if (_command_queue.TryTake(out command))
                {
                    if (_serialPort.IsOpen)
                    {
                        try
                        {
                            if (command.RequestResponse) _serialPort.DiscardInBuffer();
                            foreach (var d in command.Data)
                            {
                                _serialPort.Write(d, 0, d.Length);
                            }

                            if (command.RequestResponse)
                            {
                                Thread.Sleep(100);
                                command.Response = _serialPort.ReadExisting();
                            }
                            command.Processed = true;
                        }
                        catch (IOException ex) { /* Silently fail */ }
                    }
                }
                else Thread.Sleep(1);
            }
        }

        public void Fill(Color color)
        {
            var color565 = ConvertToRGB565(color);
            var width = Resolution.width - 1; //todo use landscape to set this
            var height = Resolution.height - 1;
            var command = Command.Fill(color565, width, height);
            _command_queue.Add(command);
        }

        public void SetOrientation(DisplayOrientation orientation)
        {
            var command = Command.SetOrientation(orientation);
            _command_queue.Add(command);
        }

        public DeviceInfo ReadInfo()
        {
            var command = Command.GetSystemVersion();
            _command_queue.Add(command);

            command.WaitForResponse();

            string? message = command.Response;
            _deviceInfo = new DeviceInfo(
                message.Substring(1, 8),
                message.Substring(10, 6),
                message.Length == 19
                );
            return _deviceInfo;
        }

        public void SetBrightness(byte level, ushort duration = 0) //todo: duration messes up the Queue timeing
        {
            level = (byte)(Math.Clamp((int)level, 0, 100) * 255 / 100);
            var command = Command.SetBrightness(level, duration);
            _command_queue.Add(command);
        }

        public static ushort ConvertToRGB565(Color color)
        {
            // Clamp values to 0–255
            var red = Math.Clamp(color.R, (byte)0, (byte)255);
            var green = Math.Clamp(color.G, (byte)0, (byte)255);
            var blue = Math.Clamp(color.B, (byte)0, (byte)255);

            // Convert to RGB565
            ushort r = (ushort)(red >> 3 & 0x1F);      // 5 bits
            ushort g = (ushort)(green >> 2 & 0x3F);    // 6 bits
            ushort b = (ushort)(blue >> 3 & 0x1F);     // 5 bits

            return (ushort)(r << 11 | g << 5 | b);
        }

        public int SetBitmap(Bitmap bmp, LZCompressionLevel useFastLZCompression = LZCompressionLevel.Auto)
        {
            if (bmp.Width != Resolution.width || bmp.Height != Resolution.height)
                throw new ArgumentException("Invalid Resolution");

            var rect = new Rectangle(0, 0, Resolution.width, Resolution.height);
            BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadOnly, bmp.PixelFormat);
            int bpp = Image.GetPixelFormatSize(bmpData.PixelFormat) / 8;
            int stride = bmpData.Stride;
            byte[] pixels = new byte[stride * Resolution.height];
            Marshal.Copy(bmpData.Scan0, pixels, 0, pixels.Length);
            bmp.UnlockBits(bmpData);

            byte[] rgb565 = new byte[Resolution.width * Resolution.height * 2];

            Parallel.For(0, Resolution.height, y =>
            {
                for (int x = 0; x < Resolution.width; x++)
                {
                    int srcIndex = y * stride + x * bpp;
                    int dstIndex = (y * Resolution.width + x) * 2;
                    var r = (ushort)(pixels[srcIndex + 2] >> 3 & 0x1F);   // 5 bits
                    var g = (ushort)(pixels[srcIndex + 1] >> 2 & 0x3F);   // 6 bits
                    var b = (ushort)(pixels[srcIndex + 0] >> 3 & 0x1F);   // 5 bits
                    ushort p = (ushort)(r << 11 | g << 5 | b);
                    rgb565[dstIndex] = (byte)(p & 0xFF);
                    rgb565[dstIndex + 1] = (byte)(p >> 8);
                }
            });
            //if (useFastLZCompression == LZCompressionLevel.Yes) return SetBitmapFastLZ(rgb565);
            return SetBitmap(rgb565);
        }

        //// todo: this is very slow, optimize
        //public int SetBitmapFastLZ(byte[] rgb565)
        //{
        //    int bytesSent = 0;

        //    int chunkSize = 640*2*2;
        //    int chunkCount = rgb565.Length / chunkSize;
        //    var chunks = new byte[chunkCount][];

        //    Parallel.For(0, chunkCount, i =>
        //    {
        //        int currentChunkSize = Math.Min(chunkSize, rgb565.Length - (i*chunkSize));
        //        byte[] chunk = new byte[currentChunkSize];
        //        Array.Copy(rgb565, (i * chunkSize), chunk, 0, currentChunkSize);

        //        var estimateSize = FastLZ.EstimateCompressedSize(chunk.Length);
        //        byte[] comBuf = new byte[estimateSize];
        //        var comBufSize = FastLZ.CompressLevel(2, chunk, chunk.Length, comBuf);

        //        // verify whether to skip 4 bytes:
        //        int skip = 0; // or 0, depending on your FastLZ
        //        var compressedDataLength = comBufSize - skip;
        //        byte[] compressedData = new byte[compressedDataLength];
        //        Array.Copy(comBuf, skip, compressedData, 0, compressedDataLength);

        //        byte[] header = new byte[4];
        //        Array.Copy(BitConverter.GetBytes((ushort)chunk.Length), 0, header, 0, 2);
        //        Array.Copy(BitConverter.GetBytes((ushort)compressedDataLength), 0, header, 2, 2);

        //        byte[] chunkWithHeader = new byte[header.Length + compressedDataLength];
        //        Array.Copy(header, 0, chunkWithHeader, 0, header.Length);
        //        Array.Copy(compressedData, 0, chunkWithHeader, header.Length, compressedDataLength);

        //        chunks[i] = chunkWithHeader;
        //    });

        //    lock (_QWriteLock)
        //    {
        //        _tx_queue.Enqueue((query: false, data: [(byte)CommandCode.CMD_SET_BITMAP_WITH_FASTLZ, 0, 0, 0, 0, 159, 0, 79, 0, (byte)CommandCode.CMD_END]));
        //        foreach (var chunk in chunks)
        //        {
        //            _tx_queue.Enqueue((query: false, data: chunk));
        //            bytesSent += chunk.Length;
        //        }
        //    }

        //    return bytesSent;
        //}


        public int SetBitmap(byte[] rgb565)
        {
            var command = Command.SetRawBitmap(rgb565);
            _command_queue.Add(command);

            return rgb565.Count();
        }

        public void WaitForQueueToEmpty()
        {
            while (_command_queue.Count > 0) Thread.Sleep(1);
        }

        public void Dispose()
        {
            _cts.Cancel();
            while (!_portWriter.IsCompleted) Thread.Sleep(1);
            if (_serialPort != null)
            {
                if (_serialPort.IsOpen) _serialPort.Close();
                _serialPort.Dispose();
            }
        }
    }
}

