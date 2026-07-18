using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Svg;

var svgPath = args[0];
var outDir = args[1];
Directory.CreateDirectory(outDir);
var doc = SvgDocument.Open(svgPath);
Console.WriteLine($"Width={doc.Width} Height={doc.Height}");
Console.WriteLine($"ViewBox={doc.ViewBox.MinX},{doc.ViewBox.MinY} {doc.ViewBox.Width}x{doc.ViewBox.Height}");
Console.WriteLine($"Bounds={doc.Bounds}");
foreach (var size in new[] { 32, 64, 256 })
{
    var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
    using (var g = Graphics.FromImage(bmp))
    {
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.Clear(Color.FromArgb(255, 240, 240, 240)); // light bg to see extents
        doc.Draw(g, new SizeF(size, size));
    }
    var path = Path.Combine(outDir, $"preview-{size}.png");
    bmp.Save(path, ImageFormat.Png);
    bmp.Dispose();
    Console.WriteLine($"Wrote {path}");
}
// Also try with padding inset
foreach (var size in new[] { 32, 64, 256 })
{
    var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
    var pad = size / 8f;
    using (var g = Graphics.FromImage(bmp))
    {
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.Clear(Color.FromArgb(255, 240, 240, 240));
        doc.Draw(g, new RectangleF(pad, pad, size - 2*pad, size - 2*pad));
    }
    var path = Path.Combine(outDir, $"preview-pad-{size}.png");
    bmp.Save(path, ImageFormat.Png);
    bmp.Dispose();
    Console.WriteLine($"Wrote {path}");
}
