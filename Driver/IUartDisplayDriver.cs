using System.Drawing;
using System.IO.Compression;

namespace WeActLCD.Driver
{

    public enum LZCompressionLevel
    {
        Auto,
        Yes,
        No
    }

    public interface IUartDisplayDriver: IDisposable
    {
        (int width, int height) Resolution { get; }

        static abstract ushort ConvertToRGB565(Color color);
        void Connect();
        DeviceInfo ReadInfo();
        void Fill(Color color);
        int SetBitmap(Bitmap bmp, LZCompressionLevel useFastLZCompression = LZCompressionLevel.Auto);
        void SetBrightness(byte level, ushort duration = 0);
        void SetOrientation(DisplayOrientation orientation);
        void WaitForQueueToEmpty();
    }
}