// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The coloured cells of an image: the colour range the values are coloured over (the image's colour scale), and the grid of rectangles an image
/// series (scaled to its plot) and a figure image (one pixel for each cell) both draw. Held at the class's own granularity,
/// every branch, so the series renderer and the figure draw one copy of it.</summary>
public class ImageCellsTests
{
    private static readonly LinearColorMap RedToBlue = new("test", [Colors.Red, Colors.Blue]);

    /// <summary>The cells of an image of <paramref name="data"/> as a series draws them: its colour scale under the library's own
    /// theme (the range by <see cref="ColorMapping.RangeOf(double[,], double?, double?)"/>) and its opacity.</summary>
    private static ImageCells Of(double[,] data, IColorMap? map = null, INormalizer? normalizer = null, double? vMin = null, double? vMax = null, double alpha = 1.0)
    {
        var mapping = new ColorMapping(map ?? RedToBlue, normalizer ?? LinearNormalizer.Instance, Theme.Default);
        return new ImageCells(mapping.Over(mapping.RangeOf(data, vMin, vMax)), alpha);
    }

    private static RecordingRenderContext Drawn(ImageCells cells, double[,] pixels, Rect bounds, bool skipTransparentCells = false)
    {
        var ctx = new RecordingRenderContext();
        cells.Draw(ctx, pixels, bounds, skipTransparentCells);
        return ctx;
    }

    private static List<Color> Fills(RecordingRenderContext ctx) => [.. ctx.Of<DrawRectangleCall>().Select(c => c.Fill!.Value)];

    // ── Of: the colour range ───────────────────────────────────────────────

    [Fact]
    public void Of_TakesTheRange_OfTheValues()
    {
        var cells = Of(new double[,] { { 3, 1 }, { 4, 2 } });

        Assert.Equal((1.0, 4.0), (cells.Scale.Min, cells.Scale.Max));
    }

    [Fact]
    public void Of_LeavesAMissingValueAndAnInfinity_OutOfTheRange()
    {
        var cells = Of(new double[,] { { double.NaN, 2 }, { double.PositiveInfinity, 6 }, { double.NegativeInfinity, 4 } });

        Assert.Equal((2.0, 6.0), (cells.Scale.Min, cells.Scale.Max));
    }

    [Fact]
    public void Of_TheLimitsGiven_AreTheRange_EvenOutsideTheValues()
    {
        var cells = Of(new double[,] { { 1, 2 } }, vMin: -5, vMax: 20);

        Assert.Equal((-5.0, 20.0), (cells.Scale.Min, cells.Scale.Max));
    }

    [Fact]
    public void Of_OnlyTheLowerLimitGiven_TakesTheUpperEndFromTheValues()
    {
        var cells = Of(new double[,] { { 1, 2 }, { 3, 4 } }, vMin: 0);

        Assert.Equal((0.0, 4.0), (cells.Scale.Min, cells.Scale.Max));
    }

    [Fact]
    public void Of_OnlyTheUpperLimitGiven_TakesTheLowerEndFromTheValues()
    {
        var cells = Of(new double[,] { { 1, 2 }, { 3, 4 } }, vMax: 9);

        Assert.Equal((1.0, 9.0), (cells.Scale.Min, cells.Scale.Max));
    }

    [Fact]
    public void Of_AnEmptyRange_IsOneWide_SoNoNormalizerDividesByZero()
    {
        Assert.Equal((5.0, 6.0), (Of(new double[,] { { 5, 5 }, { 5, 5 } }).Scale.Min, Of(new double[,] { { 5, 5 }, { 5, 5 } }).Scale.Max));
        var limited = Of(new double[,] { { 1, 9 } }, vMin: 3, vMax: 3);
        Assert.Equal((3.0, 4.0), (limited.Scale.Min, limited.Scale.Max));
    }

    [Fact]
    public void Of_WithNoValueThatCanBePlaced_IsTheUnitRange()
    {
        var cells = Of(new double[,] { { double.NaN, double.PositiveInfinity } });

        Assert.Equal((0.0, 1.0), (cells.Scale.Min, cells.Scale.Max));
    }

    [Fact]
    public void Of_KeepsTheMapTheNormalizerAndTheAlpha()
    {
        var normalizer = new LogNormalizer();

        var cells = Of(new double[,] { { 1, 2 } }, RedToBlue, normalizer, alpha: 0.4);

        Assert.Same(RedToBlue, cells.Scale.ColorMap);
        Assert.Same(normalizer, cells.Scale.Normalizer);
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
            ctx.Rects());
    }

    [Fact]
    public void Draw_AtNativeSize_IsOneUnitForEachCell()
    {
        var pixels = new double[,] { { 0, 1, 2 }, { 3, 4, 5 } };

        var rects = Drawn(Of(pixels), pixels, new Rect(7, 9, 3, 2)).Rects();

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
        Assert.All(ctx.Of<DrawRectangleCall>(), call =>
        {
            Assert.Null(call.Stroke);
            Assert.Equal(0.0, call.StrokeThickness);
        });
    }

    [Fact]
    public void Draw_ColoursThePixelsByTheRangeOfTheValuesItWasMadeFrom_NotOfThePixels()
    {
        // The pixels are a resampled copy: the range stays that of the original values.
        var original = new double[,] { { 0, 10 } };
        var resampled = new double[,] { { 5, 5, 5, 5 } };

        var ctx = Drawn(Of(original), resampled, new Rect(0, 0, 4, 1));

        Assert.Equal(4, ctx.Rects().Count);
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

    [Fact]
    public void Draw_FromCornerToCorner_StandsTheFirstCellAtTheFirstCorner()
    {
        var pixels = new double[,] { { 0, 1 }, { 1, 0 } };

        var rects = DrawnBetween(Of(pixels), pixels, new Point(10, 20), new Point(30, 60)).Rects();

        Assert.Equal([new Rect(10, 20, 10, 20), new Rect(20, 20, 10, 20), new Rect(10, 40, 10, 20), new Rect(20, 40, 10, 20)], rects);
    }

    [Fact]
    public void Draw_FromCornerToCorner_AGridThatRunsUpAndLeft_IsDrawnTurned_EachCellOfPositiveSize()
    {
        // The last corner above and left of the first: an axis that runs up (a Y limit set low to high) puts the first row at the bottom.
        var pixels = new double[,] { { 0, 1 }, { 1, 0 } };

        var rects = DrawnBetween(Of(pixels), pixels, new Point(30, 60), new Point(10, 20)).Rects();

        Assert.Equal([new Rect(20, 40, 10, 20), new Rect(10, 40, 10, 20), new Rect(20, 20, 10, 20), new Rect(10, 20, 10, 20)], rects);
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(2, 0)]
    public void Draw_FromCornerToCorner_AGridWithNoRowsOrNoColumns_DrawsNothing(int rows, int columns)
    {
        var ctx = DrawnBetween(Of(new double[,] { { 1, 2 } }, alpha: 0.5), new double[rows, columns], new Point(0, 0), new Point(10, 10));

        Assert.Empty(ctx.Calls);
    }

    private static RecordingRenderContext DrawnBetween(ImageCells cells, double[,] pixels, Point first, Point last)
    {
        var ctx = new RecordingRenderContext();
        cells.Draw(ctx, pixels, first, last);
        return ctx;
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
        Assert.Equal(0.25, ((SetOpacityCall)calls[0]).Opacity);
        Assert.Equal(1.0, ((SetOpacityCall)calls[^1]).Opacity);
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(1.5)]
    public void Draw_AnImageThatIsNotTranslucent_NeverTouchesTheOpacity(double alpha)
    {
        var pixels = new double[,] { { 0, 1 } };

        var ctx = Drawn(Of(pixels, alpha: alpha), pixels, new Rect(0, 0, 2, 1));

        Assert.Equal(0, ctx.Count<SetOpacityCall>());
        Assert.Equal(2, ctx.Count<DrawRectangleCall>());
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

        Assert.Equal([new Rect(0, 0, 1, 1), new Rect(2, 0, 1, 1)], ctx.Rects());
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
        Assert.Equal([new Rect(1, 0, 1, 1)], ctx.Rects());
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

        // Every stage of a figure starts at full opacity; the image's own opacity is the one below full, reset right after.
        var kinds = ctx.Calls.Select(c => c.Kind).ToList();
        int open = ctx.Calls.FindIndex(c => c is SetOpacityCall k && k.Opacity < 1.0);
        int close = ctx.Calls.FindIndex(open + 1, c => c is SetOpacityCall);
        Assert.InRange(open, 0, close - 1);
        Assert.Equal(4, kinds.Skip(open).Take(close - open).Count(k => k == "DrawRectangle"));
    }

    // ── A surface that fills a grid of cells in one pass ───────────────────────────

    /// <summary>A surface that fills a grid of cells in one pass when <see cref="Fills"/> is set, and records what it was handed.</summary>
    private sealed class GridFillingContext(bool fills) : RecordingRenderContext, IPixelGridRenderContext
    {
        internal List<string> Kinds => [.. Calls.Select(c => c.Kind)];
        internal List<double> Opacities { get; } = [];
        internal List<(Color[] Colours, int Columns, Rect Area)> Grids { get; } = [];
        public bool FillsCellGrids => fills;
        public void FillCellGrid(ReadOnlySpan<Color> colours, int columns, Rect area) => Grids.Add((colours.ToArray(), columns, area));
        public PixelSegment OnPixelGrid(PixelSegment line, double thickness, bool projectingEnds) => line;
        public Rect OnPixelGrid(Rect area, double edgeThickness) => area;
        public void DrawRectangleOnPixelGrid(Rect area, ShapeStyle style) => DrawRectangle(area, style);
        public PixelSegment? MarkerOnPixelGrid(Point anchor, PixelSegment mark, double thickness) => null;
        public IReadOnlyList<Point> OnPixelGrid(IReadOnlyList<Point> line, double thickness) => line;
        public override void SetOpacity(double opacity) => Opacities.Add(opacity);
    }

    [Fact]
    public void ASurfaceThatFillsAGrid_IsHandedEveryCell_RowByRowFromTheTop_InOnePass()
    {
        var pixels = new double[,] { { 0, 1 }, { 1, 0 } };
        var ctx = new GridFillingContext(fills: true);

        Of(pixels).Draw(ctx, pixels, new Rect(10, 20, 4, 6));

        var fill = Assert.Single(ctx.Grids);
        Assert.Equal([Colors.Red, Colors.Blue, Colors.Blue, Colors.Red], fill.Colours);
        Assert.Equal((2, new Rect(10, 20, 4, 6)), (fill.Columns, fill.Area));
        Assert.DoesNotContain("DrawRectangle", ctx.Kinds);
    }

    [Fact]
    public void AGridLaidTheOtherWay_IsHandedAsItIsShown_FromItsFarEnd()
    {
        // The first cell's corner at the bottom right: the grid runs left and up, so its last column is shown first and its last row on top.
        var pixels = new double[,] { { 0, 1, 1 }, { 1, 1, 1 } };
        var ctx = new GridFillingContext(fills: true);

        Of(pixels).Draw(ctx, pixels, new Point(30, 40), new Point(0, 0));

        var fill = Assert.Single(ctx.Grids);
        Assert.Equal([Colors.Blue, Colors.Blue, Colors.Blue, Colors.Blue, Colors.Blue, Colors.Red], fill.Colours);
        Assert.Equal(new Rect(0, 0, 30, 40), fill.Area);
    }

    [Fact]
    public void ASurfaceThatFillsAGrid_DrawsItUnderTheImagesOpacity_ThenFullAgain()
    {
        var pixels = new double[,] { { 0, 1 } };
        var ctx = new GridFillingContext(fills: true);

        Of(pixels, alpha: 0.4).Draw(ctx, pixels, new Rect(0, 0, 2, 1));

        Assert.Equal([0.4, 1.0], ctx.Opacities);
        Assert.Single(ctx.Grids);
    }

    [Fact]
    public void ASurfaceWithAGridThatDoesNotFillCells_IsHandedARectangleForEachCell()
    {
        var pixels = new double[,] { { 0, 1 } };
        var ctx = new GridFillingContext(fills: false);

        Of(pixels).Draw(ctx, pixels, new Rect(0, 0, 2, 1));

        Assert.Empty(ctx.Grids);
        Assert.Equal(2, ctx.Kinds.Count(k => k == "DrawRectangle"));
    }
}
