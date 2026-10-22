// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;
using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>The figure's resolution reaches the PNG: a figure is laid out in units of one pixel at 96 dpi, and its
/// dpi says how many device pixels a unit takes. The SaveOptions resolution is the per-save override.</summary>
public class PngResolutionTests
{
    /// <summary>Save routes .png and .pdf to the Skia transforms only once the Skia assembly's module initializer has
    /// run, which happens when something of the assembly is first used; a test that saves a file before any other
    /// Skia call would otherwise write an SVG under a .png name, depending on the order the tests run in.</summary>
    static PngResolutionTests() =>
        System.Runtime.CompilerServices.RuntimeHelpers.RunModuleConstructor(typeof(FigureSkiaExtensions).Module.ModuleHandle);

    private static readonly SKColor BarBlue = new(31, 119, 180);

    private static Figure Chart(double width = 400, double height = 300, double dpi = 96) =>
        Plt.Create().WithSize(width, height).WithDpi(dpi)
            .AddSubPlot(1, 1, 1, ax => ax.Bar(["a", "b", "c"], [3.0, 5.0, 4.0]))
            .Build();

    private static string TempPng() => Path.Combine(Path.GetTempPath(), $"s9_{Guid.NewGuid():N}.png");

    private static byte[] SavedBytes(Figure figure, SaveOptions? options)
    {
        string path = TempPng();
        try
        {
            figure.Save(path, options);
            return File.ReadAllBytes(path);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    // ── the figure's own dpi ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void DefaultDpi_DrawsOnePixelPerUnit()
    {
        Assert.Equal(new PngSize(400, 300), Chart().ToPng().PngPixelSize());
    }

    [Fact]
    public void WithDpi192_DrawsTwoPixelsPerUnit()
    {
        Assert.Equal(new PngSize(800, 600), Chart(dpi: 192).ToPng().PngPixelSize());
    }

    [Fact]
    public void WithDpi48_DrawsHalfAPixelPerUnit()
    {
        Assert.Equal(new PngSize(200, 150), Chart(dpi: 48).ToPng().PngPixelSize());
    }

    [Fact]
    public void ScaledSize_IsRoundedToWholePixels()
    {
        // 500 * 150/96 = 781.25 and 350 * 150/96 = 546.875: a pixel count is a whole number, and 546.875 is nearer 547.
        Assert.Equal(new PngSize(781, 547), Chart(500, 350, dpi: 150).ToPng().PngPixelSize());
    }

    [Fact]
    public void ScaledPicture_FillsTheWholeBitmap()
    {
        // 781.25 units of picture on 781 pixels: if the picture were drawn at the exact scale it would be cut at the
        // right and bottom edge; if the bitmap were the larger size an edge of it would stay white. A dark background
        // shows both, so the corner pixels must carry it.
        var figure = Plt.Create().WithSize(500, 350).WithDpi(150).WithBackground(Colors.Black)
            .AddSubPlot(1, 1, 1, ax => ax.Bar(["a", "b"], [1.0, 2.0])).Build();

        using var bitmap = SKBitmap.Decode(figure.ToPng());

        Assert.Equal(new SKColor(0, 0, 0), bitmap.GetPixel(bitmap.Width - 1, bitmap.Height - 1));
        Assert.Equal(new SKColor(0, 0, 0), bitmap.GetPixel(bitmap.Width - 1, 0));
        Assert.Equal(new SKColor(0, 0, 0), bitmap.GetPixel(0, bitmap.Height - 1));
    }

    [Fact]
    public void ScaledPicture_IsTheSamePictureLarger()
    {
        PixelBox small = BoxOf(BarBlue, Chart().ToPng());
        PixelBox large = BoxOf(BarBlue, Chart(dpi: 192).ToPng());

        Assert.InRange(large.Left, small.Left * 2 - 2, small.Left * 2 + 2);
        Assert.InRange(large.Top, small.Top * 2 - 2, small.Top * 2 + 2);
        Assert.InRange(large.Right, small.Right * 2 - 2, small.Right * 2 + 2);
        Assert.InRange(large.Bottom, small.Bottom * 2 - 2, small.Bottom * 2 + 2);
    }

    [Fact]
    public void ScaledPicture_KeepsTheLayoutOfTheSmallOne()
    {
        // Layout is decided in units, so the scaled picture must hold the same bars: three of them.
        Assert.Equal(3, BarColumns(Chart().ToPng()));
        Assert.Equal(3, BarColumns(Chart(dpi: 192).ToPng()));
    }

    [Fact]
    public void TheScientificPaperTemplate_IsDrawnAtItsOwn150Dpi()
    {
        // 800 * 150/96 = 1250 and 600 * 150/96 = 937.5, which rounds up to 938.
        var figure = FigureTemplates.ScientificPaper(ax => ax.Plot([1.0, 2.0], [3.0, 4.0])).Build();

        Assert.Equal(new PngSize(1250, 938), figure.ToPng().PngPixelSize());
    }

    // ── the SaveOptions resolution ───────────────────────────────────────────────────────────────────

    [Fact]
    public void SaveOptionsDpi_ScalesAFigureThatLeavesItAtTheDefault()
    {
        var bytes = SavedBytes(Chart(), new SaveOptions { Dpi = 192 });

        Assert.Equal(new PngSize(800, 600), bytes.PngPixelSize());
    }

    [Fact]
    public void SaveOptionsDefaultDpi_LeavesTheFigureDpiInForce()
    {
        // new SaveOptions { Title = ... } says nothing about resolution: the figure's own WithDpi must survive it.
        var bytes = SavedBytes(Chart(dpi: 192), new SaveOptions { Title = "Revenue" });

        Assert.Equal(new PngSize(800, 600), bytes.PngPixelSize());
    }

    [Fact]
    public void SaveOptionsDpi_BeatsTheFigureDpi_WhenBothAreSet()
    {
        var bytes = SavedBytes(Chart(dpi: 300), new SaveOptions { Dpi = 192 });

        Assert.Equal(new PngSize(800, 600), bytes.PngPixelSize());
    }

    [Fact]
    public void SaveWithoutOptions_UsesTheFigureDpi()
    {
        Assert.Equal(new PngSize(800, 600), SavedBytes(Chart(dpi: 192), options: null).PngPixelSize());
    }

    // ── a resolution no picture can have ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0.0)]
    [InlineData(-96.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void FigureDpi_ThatIsNotPositiveAndFinite_IsRefused(double dpi)
    {
        var figure = Chart();
        figure.Dpi = dpi;

        var refusal = Assert.Throws<ArgumentOutOfRangeException>(() => figure.ToPng());

        Assert.Equal("dpi", refusal.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-300)]
    public void SaveOptionsDpi_ThatIsNotPositive_IsRefused(int dpi)
    {
        string path = TempPng();
        try
        {
            var refusal = Assert.Throws<ArgumentOutOfRangeException>(
                () => Chart().Save(path, new SaveOptions { Dpi = dpi }));

            Assert.Equal("dpi", refusal.ParamName);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void ADpiThatLeavesLessThanOnePixel_IsRefusedByName()
    {
        // 400 * 0.1 / 96 = 0.4 px wide.
        var figure = Chart(dpi: 0.1);

        var refusal = Assert.Throws<ArgumentException>(() => figure.ToPng());

        Assert.Contains("pixel", refusal.Message);
    }

    [Fact]
    public void ADpiThatOverflowsAPixelCount_IsRefusedByName()
    {
        var figure = Chart(dpi: 1e12);

        var refusal = Assert.Throws<ArgumentException>(() => figure.ToPng());

        Assert.Contains("pixel", refusal.Message);
    }

    [Fact]
    public void ARefusedSave_LeavesTheFileThatWasThere()
    {
        string path = TempPng();
        try
        {
            File.WriteAllText(path, "the report from last week");
            var figure = Chart();
            figure.Dpi = 0;

            Assert.Throws<ArgumentOutOfRangeException>(() => figure.Save(path));

            Assert.Equal("the report from last week", File.ReadAllText(path));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    // ── helpers ──────────────────────────────────────────────────────────────────────────────────────

    private readonly record struct PixelBox(int Left, int Top, int Right, int Bottom, int Count);

    /// <summary>The box around every pixel of exactly <paramref name="color"/>. The count is asserted, so a test that
    /// finds nothing cannot pass by comparing two empty boxes.</summary>
    private static PixelBox BoxOf(SKColor color, byte[] png)
    {
        using var bitmap = SKBitmap.Decode(png);
        int left = int.MaxValue, top = int.MaxValue, right = -1, bottom = -1, count = 0;
        for (int y = 0; y < bitmap.Height; y++)
            for (int x = 0; x < bitmap.Width; x++)
                if (bitmap.GetPixel(x, y) == color)
                {
                    left = Math.Min(left, x); right = Math.Max(right, x);
                    top = Math.Min(top, y); bottom = Math.Max(bottom, y);
                    count++;
                }

        Assert.True(count > 100, $"expected a block of {color} in the picture, found {count} pixels");
        return new PixelBox(left, top, right, bottom, count);
    }

    /// <summary>The number of separate runs of bar colour along the row through the middle of the lowest bar's body.</summary>
    private static int BarColumns(byte[] png)
    {
        using var bitmap = SKBitmap.Decode(png);
        PixelBox box = BoxOf(BarBlue, png);
        int y = box.Bottom - 3;
        int runs = 0;
        bool inside = false;
        for (int x = 0; x < bitmap.Width; x++)
        {
            bool bar = bitmap.GetPixel(x, y) == BarBlue;
            if (bar && !inside) runs++;
            inside = bar;
        }

        return runs;
    }
}
