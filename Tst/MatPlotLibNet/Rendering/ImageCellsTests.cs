// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The coloured cells of an image: the colour range the values are coloured over, and the grid of rectangles an image
/// series (scaled to its plot) and a figure image (one pixel for each cell) both draw. Held at the class's own granularity,
/// every branch, so the series renderer and the figure draw one copy of it.</summary>
public class ImageCellsTests
{
    private static readonly LinearColorMap RedToBlue = new("test", [Colors.Red, Colors.Blue]);

    private static ImageCells Of(double[,] data, IColorMap? map = null, INormalizer? normalizer = null, double? vMin = null, double? vMax = null, double alpha = 1.0) =>
        ImageCells.Of(data, map ?? RedToBlue, normalizer ?? LinearNormalizer.Instance, vMin, vMax, alpha);

    private static RecordingRenderContext Drawn(ImageCells cells, double[,] pixels, Rect bounds, bool skipTransparentCells = false)
    {
        var ctx = new RecordingRenderContext();
        cells.Draw(ctx, pixels, bounds, skipTransparentCells);
        return ctx;
    }

    private static dynamic Args(CallRecord call) => call.Args!;

    private static List<Rect> Rects(RecordingRenderContext ctx) => [.. ctx.OfKind("DrawRectangle").Select(c => (Rect)Args(c).rect)];

    private static List<Color> Fills(RecordingRenderContext ctx) => [.. ctx.OfKind("DrawRectangle").Select(c => (Color)Args(c).fill)];

    // ── Of: the colour range ───────────────────────────────────────────────

    [Fact]
    public void Of_TakesTheRange_OfTheValues()
    {
        var cells = Of(new double[,] { { 3, 1 }, { 4, 2 } });

        Assert.Equal((1.0, 4.0), (cells.Min, cells.Max));
    }

    [Fact]
    public void Of_LeavesAMissingValueAndAnInfinity_OutOfTheRange()
    {
        var cells = Of(new double[,] { { double.NaN, 2 }, { double.PositiveInfinity, 6 }, { double.NegativeInfinity, 4 } });

        Assert.Equal((2.0, 6.0), (cells.Min, cells.Max));
    }

    [Fact]
    public void Of_TheLimitsGiven_AreTheRange_EvenOutsideTheValues()
    {
        var cells = Of(new double[,] { { 1, 2 } }, vMin: -5, vMax: 20);

        Assert.Equal((-5.0, 20.0), (cells.Min, cells.Max));
    }

    [Fact]
    public void Of_OnlyTheLowerLimitGiven_TakesTheUpperEndFromTheValues()
    {
        var cells = Of(new double[,] { { 1, 2 }, { 3, 4 } }, vMin: 0);

        Assert.Equal((0.0, 4.0), (cells.Min, cells.Max));
    }

    [Fact]
    public void Of_OnlyTheUpperLimitGiven_TakesTheLowerEndFromTheValues()
    {
        var cells = Of(new double[,] { { 1, 2 }, { 3, 4 } }, vMax: 9);

        Assert.Equal((1.0, 9.0), (cells.Min, cells.Max));
    }

    [Fact]
    public void Of_AnEmptyRange_IsOneWide_SoNoNormalizerDividesByZero()
    {
        Assert.Equal((5.0, 6.0), (Of(new double[,] { { 5, 5 }, { 5, 5 } }).Min, Of(new double[,] { { 5, 5 }, { 5, 5 } }).Max));
        var limited = Of(new double[,] { { 1, 9 } }, vMin: 3, vMax: 3);
        Assert.Equal((3.0, 4.0), (limited.Min, limited.Max));
    }

    [Fact]
    public void Of_WithNoValueThatCanBePlaced_IsTheUnitRange()
    {
        var cells = Of(new double[,] { { double.NaN, double.PositiveInfinity } });

        Assert.Equal((0.0, 1.0), (cells.Min, cells.Max));
    }

    [Fact]
    public void Of_KeepsTheMapTheNormalizerAndTheAlpha()
    {
        var normalizer = new LogNormalizer();

        var cells = Of(new double[,] { { 1, 2 } }, RedToBlue, normalizer, alpha: 0.4);

        Assert.Same(RedToBlue, cells.ColorMap);
        Assert.Same(normalizer, cells.Normalizer);
        Assert.Equal(0.4, cells.Alpha);
    }

    // ── Draw: the grid ─────────────────────────────────────────────────────

    [Fact]
    public void Draw_FillsTheBounds_WithAGridOfEqualCells_RowByRow()
    {
        var pixels = new double[,] { { 0, 1, 0, 1 }, { 1, 0, 1, 0 } };
        var ctx = Drawn(Of(pixels), pixels, new Rect(100, 50, 40, 30));

        Assert.Equal(
            [new Rect(100, 50, 10, 15), new Rect(110, 50, 10, 15), new Rect(120, 50, 10, 15), new Rect(130, 50, 10, 15),
             new Rect(100, 65, 10, 15), new Rect(110, 65, 10, 15), new Rect(120, 65, 10, 15), new Rect(130, 65, 10, 15)],
            Rects(ctx));
    }

    [Fact]
    public void Draw_AtNativeSize_IsOneUnitForEachCell()
    {
        var pixels = new double[,] { { 0, 1, 2 }, { 3, 4, 5 } };

        var rects = Rects(Drawn(Of(pixels), pixels, new Rect(7, 9, 3, 2)));

        Assert.Equal(6, rects.Count);
        Assert.All(rects, rect => Assert.Equal((1.0, 1.0), (rect.Width, rect.Height)));
        Assert.Equal(new Rect(9, 10, 1, 1), rects[^1]);
    }

    [Fact]
    public void Draw_PaintsEachCell_InItsColour_WithoutAnOutline()
    {
        var pixels = new double[,] { { 0, 0.5, 1 } };
        var ctx = Drawn(Of(pixels), pixels, new Rect(0, 0, 3, 1));

        Assert.Equal([RedToBlue.GetColor(0), RedToBlue.GetColor(0.5), RedToBlue.GetColor(1)], Fills(ctx));
        Assert.All(ctx.OfKind("DrawRectangle"), call =>
        {
            Assert.Null(Args(call).stroke);
            Assert.Equal(0.0, (double)Args(call).strokeThickness);
        });
    }

    [Fact]
    public void Draw_ColoursThePixelsByTheRangeOfTheValuesItWasMadeFrom_NotOfThePixels()
    {
        // The pixels are a resampled copy: the range stays that of the original values.
        var original = new double[,] { { 0, 10 } };
        var resampled = new double[,] { { 5, 5, 5, 5 } };

        var ctx = Drawn(Of(original), resampled, new Rect(0, 0, 4, 1));

        Assert.Equal(4, Rects(ctx).Count);
        Assert.All(Fills(ctx), fill => Assert.Equal(RedToBlue.GetColor(0.5), fill));
    }

    [Fact]
    public void Draw_AppliesTheNormalizer()
    {
        var normalizer = new LogNormalizer();
        var pixels = new double[,] { { 1, 10, 100 } };

        var ctx = Drawn(Of(pixels, normalizer: normalizer), pixels, new Rect(0, 0, 3, 1));

        Assert.Equal([RedToBlue.GetColor(0), RedToBlue.GetColor(normalizer.Normalize(10, 1, 100)), RedToBlue.GetColor(1)], Fills(ctx));
    }

    [Theory]
    [InlineData(0, 3)]
    [InlineData(2, 0)]
    [InlineData(0, 0)]
    public void Draw_AGridWithNoRowsOrNoColumns_DrawsNothing_NotEvenTheOpacity(int rows, int columns)
    {
        var pixels = new double[rows, columns];

        var ctx = Drawn(Of(new double[,] { { 1, 2 } }, alpha: 0.5), pixels, new Rect(0, 0, 10, 10));

        Assert.Empty(ctx.Calls);
    }

    // ── Draw: the opacity ──────────────────────────────────────────────────

    [Fact]
    public void Draw_ATranslucentImage_SetsTheOpacityAroundItsCells_AndResetsIt()
    {
        var pixels = new double[,] { { 0, 1 } };

        var calls = Drawn(Of(pixels, alpha: 0.25), pixels, new Rect(0, 0, 2, 1)).Calls;

        Assert.Equal(["SetOpacity", "DrawRectangle", "DrawRectangle", "SetOpacity"], calls.Select(c => c.Kind));
        Assert.Equal(0.25, (double)Args(calls[0]).opacity);
        Assert.Equal(1.0, (double)Args(calls[^1]).opacity);
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(1.5)]
    public void Draw_AnImageThatIsNotTranslucent_NeverTouchesTheOpacity(double alpha)
    {
        var pixels = new double[,] { { 0, 1 } };

        var ctx = Drawn(Of(pixels, alpha: alpha), pixels, new Rect(0, 0, 2, 1));

        Assert.Equal(0, ctx.CountOf("SetOpacity"));
        Assert.Equal(2, ctx.CountOf("DrawRectangle"));
    }

    // ── Draw: a cell with no colour ────────────────────────────────────────

    [Fact]
    public void Draw_AMissingValue_IsPaintedInTheMapsMissingColour_TransparentUnlessItNamesOne()
    {
        var pixels = new double[,] { { 0, double.NaN, 1 } };

        var plain = Drawn(Of(pixels), pixels, new Rect(0, 0, 3, 1));
        var bad = Color.FromHex("#13579B");
        var named = Drawn(Of(pixels, new LinearColorMap("bad", [Colors.Red, Colors.Blue]) { BadColor = bad }), pixels, new Rect(0, 0, 3, 1));

        Assert.Equal([Colors.Red, Colors.Transparent, Colors.Blue], Fills(plain));
        Assert.Equal([Colors.Red, bad, Colors.Blue], Fills(named));
    }

    [Fact]
    public void Draw_SkippingTransparentCells_LeavesAMissingValueWithNoBadColour_Out()
    {
        var pixels = new double[,] { { 0, double.NaN, 1 } };

        var ctx = Drawn(Of(pixels), pixels, new Rect(0, 0, 3, 1), skipTransparentCells: true);

        Assert.Equal([new Rect(0, 0, 1, 1), new Rect(2, 0, 1, 1)], Rects(ctx));
        Assert.Equal([Colors.Red, Colors.Blue], Fills(ctx));
    }

    [Fact]
    public void Draw_SkippingTransparentCells_StillPaintsAMissingValue_TheMapGivesAColour()
    {
        var pixels = new double[,] { { 0, double.NaN, 1 } };
        var bad = Color.FromHex("#13579B");

        var ctx = Drawn(Of(pixels, new LinearColorMap("bad", [Colors.Red, Colors.Blue]) { BadColor = bad }), pixels, new Rect(0, 0, 3, 1), skipTransparentCells: true);

        Assert.Equal([Colors.Red, bad, Colors.Blue], Fills(ctx));
    }

    [Fact]
    public void Draw_SkippingTransparentCells_LeavesOutAnyCellThatWouldBePaintedTransparent()
    {
        var map = new LinearColorMap("clear-low", [Colors.Transparent, Colors.Blue]);
        var pixels = new double[,] { { 0, 1 } };

        var ctx = Drawn(Of(pixels, map), pixels, new Rect(0, 0, 2, 1), skipTransparentCells: true);

        Assert.Equal([Colors.Blue], Fills(ctx));
        Assert.Equal([new Rect(1, 0, 1, 1)], Rects(ctx));
    }

    [Fact]
    public void Draw_NotSkippingTransparentCells_PaintsThemAll()
    {
        var map = new LinearColorMap("clear-low", [Colors.Transparent, Colors.Blue]);
        var pixels = new double[,] { { 0, 1 } };

        var ctx = Drawn(Of(pixels, map), pixels, new Rect(0, 0, 2, 1));

        Assert.Equal([Colors.Transparent, Colors.Blue], Fills(ctx));
    }

    // ── The picture an image series draws keeps its bytes ──────────────────

    [Fact]
    public void AnImageSeries_IsDrawnThroughTheCells_AsItAlwaysWas()
    {
        var ctx = new RecordingRenderContext();
        var figure = Plt.Create().WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax => ax.Image(new double[,] { { 0, 1 }, { 2, 3 } }, s => s.Alpha = 0.5))
            .Build();

        new ChartRenderer().Render(figure, ctx);

        var kinds = ctx.Calls.Select(c => c.Kind).ToList();
        int open = kinds.IndexOf("SetOpacity");
        int close = kinds.LastIndexOf("SetOpacity");
        Assert.InRange(open, 0, close - 1);
        Assert.Equal(4, kinds.Skip(open).Take(close - open).Count(k => k == "DrawRectangle"));
    }
}
