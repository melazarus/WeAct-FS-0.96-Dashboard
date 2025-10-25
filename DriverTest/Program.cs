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
Thread.Sleep(500);

Console.WriteLine("Landscape Fill Green");
_driver.Fill(Color.Green);
Thread.Sleep(500);

Console.WriteLine("Landscape Fill Blue");
_driver.Fill(Color.Blue);
Thread.Sleep(500);

Console.WriteLine("Brightness 0");
_driver.SetBrightness(0);
Thread.Sleep(500);

Console.WriteLine("Brightness 255");
_driver.SetBrightness(255);
Thread.Sleep(500);

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
_driver.Dispose();

void FpsTest(string label, string? background, bool compressed)
{
    _driver.SetOrientation(DisplayOrientation.Landscape);

    var b = new Bitmap(160, 80);
    var g = Graphics.FromImage(b);
    long bytesSent = 0;
    if (background is not null) g.DrawImageUnscaled(Image.FromFile(background), Point.Empty);
    _driver.WaitForQueueToEmpty();
    var reftime = DateTime.Now;
    for (int i = 0; i < 50; i++)
        bytesSent += _driver.SetBitmap(b, compressed ? LZCompressionLevel.Yes : LZCompressionLevel.No);
    _driver.WaitForQueueToEmpty();
    var t = DateTime.Now.Subtract(reftime).TotalSeconds;
    Console.WriteLine($"\tFPS: {50 / t}\tAverage bytes sent: {bytesSent / 50}");
}






