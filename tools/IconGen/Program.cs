using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Svg;

// Usage: IconGen <input.svg> <output.ico> [preview-dir]
if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: IconGen <input.svg> <output.ico> [preview-dir]");
    return 1;
}

var svgPath = Path.GetFullPath(args[0]);
var icoPath = Path.GetFullPath(args[1]);
var previewDir = args.Length >= 3 ? Path.GetFullPath(args[2]) : null;
var sizes = new[] { 16, 24, 32, 48, 64, 128, 256 };

// Leave ~10% margin so Lucide strokes don't look edge-clipped as a tray/app icon.
const float ContentFraction = 0.88f;

var bitmaps = new List<Bitmap>();
foreach (var size in sizes)
    bitmaps.Add(RenderIcon(svgPath, size, ContentFraction));

WriteMultiSizeIco(icoPath, bitmaps);
Console.WriteLine($"Wrote {icoPath} ({string.Join(", ", sizes)} px)");

if (previewDir is not null)
{
    Directory.CreateDirectory(previewDir);
    foreach (var size in new[] { 32, 64, 256 })
    {
        using var bmp = RenderIcon(svgPath, size, ContentFraction, background: Color.FromArgb(255, 230, 230, 230));
        var path = Path.Combine(previewDir, $"preview-{size}.png");
        bmp.Save(path, ImageFormat.Png);
        Console.WriteLine($"Preview: {path}");
    }
}

foreach (var b in bitmaps) b.Dispose();
return 0;

static Bitmap RenderIcon(string svgPath, int size, float contentFraction, Color? background = null)
{
    var doc = SvgDocument.Open(svgPath);

    // Keep Lucide's design viewBox; Svg.Draw(width,height) scales the whole document to that raster size.
    if (doc.ViewBox.Width <= 0 || doc.ViewBox.Height <= 0)
        doc.ViewBox = new SvgViewBox(0, 0, 24, 24);

    doc.AspectRatio = new SvgAspectRatio(SvgPreserveAspectRatio.xMidYMid, defer: false, slice: false);

    var content = Math.Max(1, (int)Math.Round(size * contentFraction));
    // Draw() returns a bitmap scaled to the requested pixel size (fixes "tiny top-left" bug).
    using var drawn = doc.Draw(content, content);

    var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
    using (var g = Graphics.FromImage(bmp))
    {
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.CompositingMode = CompositingMode.SourceCopy;
        g.Clear(background ?? Color.Transparent);

        var x = (size - content) / 2;
        var y = (size - content) / 2;
        g.CompositingMode = CompositingMode.SourceOver;
        g.DrawImage(drawn, x, y, content, content);
    }

    return bmp;
}

static void WriteMultiSizeIco(string path, IReadOnlyList<Bitmap> images)
{
    using var fs = File.Create(path);
    using var bw = new BinaryWriter(fs);

    bw.Write((ushort)0);
    bw.Write((ushort)1);
    bw.Write((ushort)images.Count);

    var pngs = new List<byte[]>(images.Count);
    foreach (var bmp in images)
    {
        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        pngs.Add(ms.ToArray());
    }

    const int headerSize = 6;
    const int entrySize = 16;
    var dataOffset = headerSize + entrySize * images.Count;

    for (var i = 0; i < images.Count; i++)
    {
        var bmp = images[i];
        var w = bmp.Width >= 256 ? 0 : bmp.Width;
        var h = bmp.Height >= 256 ? 0 : bmp.Height;
        bw.Write((byte)w);
        bw.Write((byte)h);
        bw.Write((byte)0);
        bw.Write((byte)0);
        bw.Write((ushort)1);
        bw.Write((ushort)32);
        bw.Write(pngs[i].Length);
        bw.Write(dataOffset);
        dataOffset += pngs[i].Length;
    }

    foreach (var png in pngs)
        bw.Write(png);
}
