// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.ColorBarRendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The range matplotlib's <c>LogNorm</c> colours over when it finds the range itself: <c>LogNorm.autoscale_None</c> takes the
/// smallest value above 0 as <c>vmin</c> (3.11.2, printed: <c>LogNorm.autoscale_None([0, 0.5, 2, 8])</c> gives vmin 0.5, vmax 8) and a
/// value of 0 or below is masked. Every series that colours through a map, and the colour bar that reads the same range, takes it
/// under the matplotlib themes; the library's own themes keep the range of every value, over which the documented formula of the
/// <see cref="LogNormalizer"/> is taken.</summary>
public sealed class LogNormRangeTests
{
    private static readonly Rect Bounds = new(0, 0, 400, 300);

    private static RecordingRenderContext Render(Theme theme, Action<SeriesRenderContext> draw)
    {
        var ctx = new RecordingRenderContext();
        draw(new SeriesRenderContext(new DataTransform(0, 4, 0, 4, Bounds), ctx, Colors.Tab10Blue, new RenderArea(Bounds, ctx)) { Theme = theme });
        return ctx;
    }

    private static Color Viridis(double fraction) => ColorMaps.Viridis.GetColor(fraction);

    private static List<Color> Fills(RecordingRenderContext ctx, params Type[] kinds) =>
        [.. ctx.Calls.OfType<ShapeCall>().Where(c => kinds.Contains(c.GetType())).Select(c => c.Fill).OfType<Color>()];

    private static readonly double[,] WithZero = { { 0, 2, 20, 200 } };

    /// <summary>The colours of 0, 2, 20 and 200 over 2 to 200: the map's missing colour (a masked value), then the map at 0, 1/2 and 1.</summary>
    private static readonly Color[] OverThePositiveRange = [Colors.Transparent, Viridis(0), Viridis(0.5), Viridis(1)];

    // ---- the cells ----------------------------------------------------------------------------------------------------------

    [Fact]
    public void AHeatmapWithAZero_IsColouredOverTheValuesAboveZero_UnderV2()
    {
        var series = new HeatmapSeries(WithZero) { Normalizer = new LogNormalizer(), ColorMap = ColorMaps.Viridis };

        var ctx = Render(Theme.MatplotlibV2, c => new HeatmapSeriesRenderer(c).Render(series));

        Assert.Equal(OverThePositiveRange, Fills(ctx, typeof(DrawRectangleCall)));
    }

    public static TheoryData<double[,], double> TheDocumentedFormula => new()
    {
        { new double[,] { { 2, 20, 200 } }, Math.Log(19) / Math.Log(199) },
        { WithZero, Math.Log(3) / Math.Log(201) },
    };

    [Theory]
    [MemberData(nameof(TheDocumentedFormula))]
    public void AHeatmap_KeepsTheRangeOfEveryValue_UnderTheLibrarysOwnTheme(double[,] cells, double fraction)
    {
        var series = new HeatmapSeries(cells) { Normalizer = new LogNormalizer(), ColorMap = ColorMaps.Viridis };

        var ctx = Render(Theme.Default, c => new HeatmapSeriesRenderer(c).Render(series));

        Assert.Equal(Viridis(fraction), Fills(ctx, typeof(DrawRectangleCall))[1]);
    }

    [Fact]
    public void APcolormeshWithAZero_IsColouredOverTheValuesAboveZero_UnderV2()
    {
        var series = new PcolormeshSeries(new Vec([0.0, 1.0, 2.0, 3.0, 4.0]), new Vec([0.0, 1.0]), WithZero)
            { Normalizer = new LogNormalizer(), ColorMap = ColorMaps.Viridis };

        var ctx = Render(Theme.MatplotlibV2, c => new PcolormeshSeriesRenderer(c).Render(series));

        Assert.Equal(OverThePositiveRange, Fills(ctx, typeof(DrawRectangleCall)));
    }

    /// <summary>A histogram of two dimensions has bins of no point, whose count is 0: under a log norm they are masked and the
    /// colours run from the smallest count above 0 to the largest (matplotlib's <c>hist2d(norm=LogNorm())</c>).</summary>
    [Fact]
    public void AHistogram2D_ColoursItsBinsOverTheCountsAboveZero_UnderV2()
    {
        var series = new Histogram2DSeries([0.1, 0.1, 0.1, 0.9], [0.1, 0.1, 0.1, 0.9], 2, 2)
            { Normalizer = new LogNormalizer(), ColorMap = ColorMaps.Viridis };

        var ctx = Render(Theme.MatplotlibV2, c => new Histogram2DSeriesRenderer(c).Render(series));

        var fills = Fills(ctx, typeof(DrawRectangleCall));
        Assert.Equal(1, fills.Count(c => c == Viridis(0)));
        Assert.Equal(1, fills.Count(c => c == Viridis(1)));
        Assert.Equal(2, fills.Count(c => c == Colors.Transparent));
    }

    [Fact]
    public void AnImage_TakesTheSmallestValueAboveZeroAsItsLowEnd_UnderALogNorm()
    {
        var range = new ColorMapping(ColorMaps.Viridis, LogNorm.Instance, Theme.MatplotlibV2).RangeOf(WithZero);

        Assert.Equal((2.0, 200.0), (range.Min, range.Max));
    }

    [Fact]
    public void AnImage_KeepsTheLimitsAnAuthorSet_UnderALogNorm()
    {
        var range = new ColorMapping(ColorMaps.Viridis, LogNorm.Instance, Theme.MatplotlibV2).RangeOf(WithZero, 5, 50);

        Assert.Equal((5.0, 50.0), (range.Min, range.Max));
    }

    [Fact]
    public void AScatterColouredByAValueOfZero_IsColouredOverTheValuesAboveZero_UnderV2()
    {
        var series = new ScatterSeries([1.0, 2.0, 3.0, 3.5], [1.0, 2.0, 3.0, 3.5])
            { C = [0.0, 2.0, 20.0, 200.0], ColorMap = ColorMaps.Viridis, Normalizer = new LogNormalizer() };

        var ctx = Render(Theme.MatplotlibV2, c => new ScatterSeriesRenderer(c).Render(series));

        var fills = ctx.Fills();
        Assert.Contains(Viridis(0.5), fills);   // 20 over 2 to 200; the documented formula over 0 to 200 gives it 0.574
        Assert.Contains(Colors.Transparent, fills);   // the value of 0 is masked
    }

    [Fact]
    public void ATreemapWithAValueOfZero_IsColouredOverTheValuesAboveZero_UnderV2()
    {
        var root = new TreeNode
        {
            Label = "root",
            Children =
            [
                new TreeNode { Label = "a", Value = 1, ColorValue = 0 },
                new TreeNode { Label = "b", Value = 1, ColorValue = 2 },
                new TreeNode { Label = "c", Value = 1, ColorValue = 20 },
                new TreeNode { Label = "d", Value = 1, ColorValue = 200 },
            ],
        };
        var series = new TreemapSeries(root) { ColorMap = ColorMaps.Viridis, Normalizer = new LogNormalizer() };

        var ctx = Render(Theme.MatplotlibV2, c => new TreemapSeriesRenderer(c).Render(series));

        var fills = ctx.Of<DrawRectangleCall>().Select(c => c.Fill).ToList();
        Assert.Contains(Viridis(0.5), fills);
        // The value of 0 is masked: its colour is none, and a tile of no opacity is drawn with no face, as matplotlib's Patch.draw
        // passes no face for (0, 0, 0, 0).
        Assert.Single(fills, f => f is null);
        Assert.DoesNotContain(fills, f => f is { A: 0 });
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        // A 3-D surface is coloured over the means of its faces (norm.autoscale_None(avg_z)); under a log norm the smallest mean above 0
        // is the lowest colour and a face of mean 0 is masked.
        // the faces' means: 0, 2, 20 and 200
        new DrawnToken("ASurfaceWithAFaceOfMeanZero_IsColouredOverTheFacesAboveZero_UnderV2", () =>
        {
            double[] axis = [0, 1, 2];
            var z = new double[,] { { 0, 0, 4 }, { 0, 0, 4 }, { 40, 40, 756 } };
            return Plt.Create().With3DRotation().WithTheme(Theme.MatplotlibV2).WithSize(400, 300)
                .Surface(axis, axis, z, s => { s.ColorMap = ColorMaps.Viridis; s.Normalizer = new LogNormalizer(); }).Build().ToSvg();
        }, Viridis(0.5).ToHex()) { Comparison = StringComparison.OrdinalIgnoreCase },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    // ---- the hexagons -------------------------------------------------------------------------------------------------------

    private static HexbinSeries Hexagons() =>
        new([0.0, 0.1, 0.1, 1.0, 1.0, 1.0, 5.0], [0.0, 0.1, 0.1, 1.0, 1.0, 1.0, 5.0]) { MinCount = 0, GridSize = 4, ColorMap = ColorMaps.Viridis, Normalizer = new LogNormalizer() };

    [Fact]
    public void AHexbinWithNoMinimum_LeavesTheEmptyHexagonsOut_UnderV2_AndDrawsThemUnderTheLibrarysOwnTheme()
    {
        int v2 = Render(Theme.MatplotlibV2, c => new HexbinSeriesRenderer(c).Render(Hexagons())).Count<DrawPolygonCall>();
        int own = Render(Theme.Default, c => new HexbinSeriesRenderer(c).Render(Hexagons())).Count<DrawPolygonCall>();

        Assert.True(v2 < own, $"a hexagon of no points is masked: {v2} drawn against {own}");
        Assert.True(v2 >= 3);
    }

    [Fact]
    public void AHexbinsRange_StartsAtTheSmallestCountAboveZero_UnderALogNorm()
    {
        var series = Hexagons();

        Assert.Equal(0, series.ColorBarRangeUnder(Theme.Default).Min);
        Assert.Equal(1, series.ColorBarRangeUnder(Theme.MatplotlibV2).Min);
    }

    // ---- the range a colour bar names and the range the cells are coloured over ---------------------------------------------

    [Fact]
    public void TheBarsRangeOfAHeatmap_IsOverTheValuesAboveZero_UnderV2_AndOverEveryValueUnderTheLibrarysOwnTheme()
    {
        var series = new HeatmapSeries(WithZero) { Normalizer = new LogNormalizer() };

        Assert.Equal(new MinMaxRange(2, 200), series.ColorBarRangeUnder(Theme.MatplotlibV2));
        Assert.Equal(new MinMaxRange(2, 200), series.ColorBarRangeUnder(Theme.MatplotlibClassic));
        Assert.Equal(new MinMaxRange(0, 200), series.ColorBarRangeUnder(Theme.Default));
    }

    [Fact]
    public void ALinearSeries_KeepsEveryValueInItsRange_UnderEveryTheme()
    {
        var series = new HeatmapSeries(WithZero);

        Assert.Equal(new MinMaxRange(0, 200), series.ColorBarRangeUnder(Theme.MatplotlibV2));
    }

    [Fact]
    public void TheBarsRange_OfEveryOtherSeriesThatNamesOne_IsOverTheValuesAboveZero_UnderV2()
    {
        var normalizer = new LogNormalizer();
        double[] signal = [.. new double[256], .. Enumerable.Range(0, 768).Select(i => Math.Sin(i * 0.3) + 2)];
        IColorBarDataProvider[] series =
        [
            new TripcolorSeries(new Vec([0.0, 1.0, 0.5]), new Vec([0.0, 0.0, 1.0]), new Vec([0.0, 2.0, 200.0])) { Normalizer = normalizer },
            new PcolormeshSeries(new Vec([0.0, 1.0, 2.0, 3.0, 4.0]), new Vec([0.0, 1.0]), WithZero) { Normalizer = normalizer },
            new ImageSeries(WithZero) { Normalizer = normalizer },
            new PolarHeatmapSeries(WithZero, 1, 4) { Normalizer = normalizer },
            new ClustermapSeries(WithZero) { Normalizer = normalizer },
        ];

        foreach (var one in series)
            Assert.Equal(new MinMaxRange(2, 200), one.ColorBarRangeUnder(Theme.MatplotlibV2));

        var spectrogram = new SpectrogramSeries(new Vec(signal)) { SampleRate = 100, Normalizer = normalizer };
        // Under v2 its cells hold the decibels of the one-sided density of each window, as matplotlib's specgram draws them (SpectrogramLook):
        // the windows of silence have none, the bins of little power stand below 0 dB and the offset's above it.
        double[] cells = [.. spectrogram.CellsUnder(Theme.MatplotlibV2, spectrogram.Spectrum()).Cast<double>().Where(double.IsFinite)];
        double smallest = cells.Where(m => m > 0).Min();
        Assert.Contains(cells, m => m <= 0);
        Assert.Equal(smallest, spectrogram.ColorBarRangeUnder(Theme.MatplotlibV2).Min);
    }

    [Fact]
    public void TheMarkedColoursOfAHistogram2D_AreReadByTheBarOverTheCountsAboveZero_UnderV2()
    {
        var series = new Histogram2DSeries([0.1, 0.1, 0.1, 0.9], [0.1, 0.1, 0.1, 0.9], 2, 2) { Normalizer = new LogNormalizer() };

        Assert.Equal(new MinMaxRange(1, 3), series.ColorBarRangeUnder(Theme.MatplotlibV2));
        Assert.Equal(new MinMaxRange(0, 3), series.ColorBarRangeUnder(Theme.Default));
    }
}
