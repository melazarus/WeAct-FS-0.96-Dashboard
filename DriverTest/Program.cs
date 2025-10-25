using System.Diagnostics;
using System.Drawing;
using WeActLCD.Driver;

var testLogo = "Resources\\test-pattern-160x80.png";
var port = "com7";

Console.WriteLine("Running display tests");
Console.WriteLine("Connecting...");
using IUartDisplayDriver _driver = new WeActFS096Driver(port);
_driver.Connect();

Console.WriteLine("Reading Device Info");
Console.WriteLine(_driver.ReadInfo());

Console.WriteLine("Set Orientation Landscape");
_driver.SetOrientation(DisplayOrientation.Landscape);

Console.WriteLine("Set Brightness 255");
_driver.SetBrightness(255);

Console.WriteLine("Landscape Fill Red");
_driver.Fill(Color.Red);
Task.Delay(1000).Wait();

Console.WriteLine("Landscape Fill Green");
_driver.Fill(Color.Green);
Task.Delay(1000).Wait();

Console.WriteLine("Landscape Fill Blue");
_driver.Fill(Color.Blue);
Task.Delay(1000).Wait();

Console.WriteLine("Brightness 0");
_driver.SetBrightness(0);
Task.Delay(1000).Wait();

Console.WriteLine("Brightness 255");
_driver.SetBrightness(255);
Task.Delay(1000).Wait();

Console.WriteLine("FPS Test (Uncompressed, no BG)");
FpsTest("Uncompressed, No BG", null, false);

Console.WriteLine("FPS Test (Uncompressed, BG)");
FpsTest("Uncompressed, BG", testLogo, false);

//Console.WriteLine("FPS Test (Compressed, no BG)");
//FpsTest("Compressed, No BG", null, true);
//Console.WriteLine("FPS Test (Compressed, BG)");
//FpsTest("Compressed, BG", "TestPattern.png", true);

_driver.Fill(Color.Black);
_driver.WaitForQueueToEmpty();

void FpsTest(string label, string? background, bool compressed)
{
    using var image = new Bitmap(160, 80);
    using var g = Graphics.FromImage(image);

    if (background is not null)
    {
        using var backgroundImage = Image.FromFile(background);
        g.DrawImageUnscaled(backgroundImage, Point.Empty);
    }
    else
    {
        var location = g.MeasureString("FPS TEST", new Font("Arial", 20)).ToPointF();
        location.X = (_driver.Resolution.width - location.X) / 2;
        location.Y = (_driver.Resolution.height - location.Y) / 2;
        g.DrawString("FPS TEST", new Font("Arial", 20), new SolidBrush(Color.YellowGreen), location);
    }

    _driver.WaitForQueueToEmpty();
    long bytesSent = 0;
    var sw = Stopwatch.StartNew();
    for (int i = 0; i < 50; i++)
        bytesSent += _driver.SetBitmap(image, compressed ? LZCompressionLevel.Yes : LZCompressionLevel.No);
    _driver.WaitForQueueToEmpty();
    Console.WriteLine($"\tFPS: {(int)(50 / sw.Elapsed.TotalSeconds)}\tAverage bytes sent: {bytesSent / 50}");
}