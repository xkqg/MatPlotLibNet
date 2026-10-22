// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary><c>ContourSeries.LevelValues</c> and <c>ContourfSeries.LevelValues</c> are the levels a contour draws. Before
/// they were carried in JSON and read by nobody, so a figure that set <c>[1, 5, 9]</c> drew the even levels of
/// <c>Levels</c> all the same. Two grids with a closed-form iso-line make the levels checkable without a stored picture:
/// a ramp, whose iso-line of level L is the vertical line x = L / 2, and a cone, whose iso-line of level L is the
/// diamond |x - 3| + |y - 3| = (12 - L) / 2. The pictures are compared in-process, never against stored bytes.</summary>
public class ContourExplicitLevelRenderTests
{
    private const double NaN = double.NaN;
    private static readonly Rect Bounds = new(0, 0, 400, 300);

    private static DataTransform Linear() => new(0, 10, 0, 10, Bounds);

    /// <summary>z = 2x over x = 0..5, four rows: values 0..10, and the iso-line of level L is the line x = L / 2.</summary>
    private static readonly double[] RampX = [0, 1, 2, 3, 4, 5];
    private static readonly double[] RampY = [0, 1, 2, 3];

    private static double[,] Ramp()
    {
        var z = new double[RampY.Length, RampX.Length];
        for (int r = 0; r < RampY.Length; r++)
            for (int c = 0; c < RampX.Length; c++)
                z[r, c] = 2.0 * c;
        return z;
    }

    /// <summary>z = 12 - 2(|x - 3| + |y - 3|) over 0..6 in both: values 0..12, and the iso-line of level L is the
    /// diamond |x - 3| + |y - 3| = (12 - L) / 2, a closed ring when that is below 3. The nodes hold even numbers, so an
    /// odd level crosses every edge at its midpoint.</summary>
    private static readonly double[] ConeAxis = [0, 1, 2, 3, 4, 5, 6];

    private static double[,] Cone()
    {
        var z = new double[ConeAxis.Length, ConeAxis.Length];
        for (int r = 0; r < ConeAxis.Length; r++)
            for (int c = 0; c < ConeAxis.Length; c++)
                z[r, c] = 12.0 - 2.0 * (Math.Abs(c - 3.0) + Math.Abs(r - 3.0));
        return z;
    }

    private static RecordingRenderContext Render(Action<SeriesRenderContext> draw)
    {
        var ctx = new RecordingRenderContext();
        draw(new SeriesRenderContext(Linear(), ctx, Colors.Tab10Blue, new RenderArea(Bounds, ctx)));
        return ctx;
    }

    private static RecordingRenderContext RenderContour(double[]? levelValues, int levels = 10, bool labels = false) =>
        Render(c => new ContourSeriesRenderer(c).Render(
            new ContourSeries(RampX, RampY, Ramp()) { Levels = levels, LevelValues = levelValues, ShowLabels = labels }));

    private static RecordingRenderContext RenderContourf(double[]? levelValues, int levels = 10) =>
        Render(c => new ContourfSeriesRenderer(c).Render(
            new ContourfSeries(ConeAxis, ConeAxis, Cone()) { Levels = levels, LevelValues = levelValues }));

    private static T Arg<T>(CallRecord call, string name) =>
        (T)call.Args!.GetType().GetProperty(name)!.GetValue(call.Args)!;

    /// <summary>The polylines drawn, each as the data points it runs through.</summary>
    private static List<Point[]> Lines(RecordingRenderContext ctx)
    {
        var transform = Linear();
        return [.. ctx.OfKind("DrawLines").Select(call => Arg<Point[]>(call, "points").Select(transform.PixelToData).ToArray())];
    }

    /// <summary>The x every point of a ramp line stands at: the iso-line of a ramp is vertical.</summary>
    private static double RampLineX(Point[] line)
    {
        double x = line[0].X;
        Assert.All(line, p => Assert.Equal(x, p.X, 3));
        return x;
    }

    private static List<string> Signature(RecordingRenderContext ctx) =>
        [.. ctx.Calls.Select(c => c.Kind + "(" + string.Join("; ", c.Args?.GetType().GetProperties().Select(p => Describe(p.GetValue(c.Args))) ?? []) + ")")];

    private static string Describe(object? value) => value switch
    {
        Point[] points => string.Join(" ", points),
        _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "",
    };

    // ---- ContourSeries: the lines --------------------------------------------------------------------------------------

    [Fact]
    public void AContour_WithExplicitLevels_DrawsExactlyThoseLevels()
    {
        var lines = Lines(RenderContour([1, 5, 9]));

        Assert.Equal([0.5, 2.5, 4.5], lines.Select(RampLineX).Select(x => Math.Round(x, 3)));
    }

    [Fact]
    public void AContour_WithExplicitLevels_IgnoresTheLevelCount()
    {
        Assert.Equal(Signature(RenderContour([1, 5, 9], levels: 3)), Signature(RenderContour([1, 5, 9], levels: 20)));
        Assert.Equal(3, Lines(RenderContour([1, 5, 9], levels: 20)).Count);
    }

    /// <summary>The levels are the finite values, ascending, each once: a repeated level would stroke the same line
    /// twice, and a level that is not a number has no place on any grid.</summary>
    [Fact]
    public void AContour_TakesTheExplicitLevelsSortedDistinctAndFinite()
    {
        var expected = Signature(RenderContour([1, 5, 9]));

        Assert.Equal(expected, Signature(RenderContour([9, NaN, 1, 5, 5, double.PositiveInfinity, 1, double.NegativeInfinity])));
    }

    [Fact]
    public void AContour_WithALevelOutsideTheDataRange_DrawsNothingForIt()
    {
        var lines = Lines(RenderContour([-3, 1, 12, 1000]));

        Assert.Equal(0.5, RampLineX(Assert.Single(lines)), 3);
    }

    [Theory]
    [InlineData(new[] { NaN })]
    [InlineData(new[] { double.PositiveInfinity, double.NegativeInfinity })]
    public void AContour_WhoseExplicitLevelsCannotBeDrawn_DrawsNothing_ItDoesNotFallBackToTheCount(double[] levels) =>
        Assert.Empty(RenderContour(levels).Calls);

    /// <summary>No explicit level — unset, or an empty list — is the even levels of <c>Levels</c>, as before.</summary>
    [Fact]
    public void AContour_WithoutExplicitLevels_KeepsTheEvenLevelsOfTheCount()
    {
        var lines = Lines(RenderContour(null, levels: 4));

        Assert.Equal([1.0, 2.0, 3.0, 4.0], lines.Select(RampLineX).Select(x => Math.Round(x, 3)));
        Assert.Equal(Signature(RenderContour(null, levels: 4)), Signature(RenderContour([], levels: 4)));
        Assert.Equal(10, Lines(RenderContour(null)).Count);
    }

    [Fact]
    public void AContour_LabelsEachExplicitLevel()
    {
        var texts = RenderContour([9, 1, 5], labels: true).OfKind("DrawText").Select(c => Arg<string>(c, "text"));

        Assert.Equal(["1", "5", "9"], texts);
    }

    // ---- ContourfSeries: the bands and the lines over them ---------------------------------------------------------------

    private static double DiamondRadius(Point p) => Math.Abs(p.X - 3) + Math.Abs(p.Y - 3);

    [Fact]
    public void AFilledContour_WithExplicitLevels_DrawsItsLinesAtExactlyThoseLevels()
    {
        var lines = Lines(RenderContourf([7, 9, 11]));

        Assert.Equal(3, lines.Count);
        double[] radii = [2.5, 1.5, 0.5];
        for (int i = 0; i < lines.Count; i++)
            Assert.All(lines[i], p => Assert.Equal(radii[i], DiamondRadius(p), 3));
    }

    /// <summary>A level the grid never reaches crosses no cell, so it adds no line; it still bounds a band, and the colour
    /// scale runs over the levels, the outer ones included.</summary>
    [Fact]
    public void AFilledContour_WithALevelOutsideTheGridRange_AddsNoLineForIt()
    {
        var ctx = RenderContourf([-5, 7, 9, 50]);

        var lines = Lines(ctx);
        Assert.Equal(2, lines.Count);
        Assert.All(lines[0], p => Assert.Equal(2.5, DiamondRadius(p), 3));
        Assert.All(lines[1], p => Assert.Equal(1.5, DiamondRadius(p), 3));
        Assert.Equal(ColorMaps.Viridis.ColorOf(1.0, LinearNormalizer.Instance, -5, 50), Arg<Color?>(Assert.Single(ctx.OfKind("DrawRectangle")), "fill"));
    }

    /// <summary>Two levels bound one band, three bound two: each band is painted over the one below it, and the colour
    /// scale runs from the first level to the last (7 to 11), whatever the grid holds (0 to 12).</summary>
    [Fact]
    public void AFilledContour_WithExplicitLevels_PaintsOneBandBetweenEachPairOfLevels()
    {
        var ctx = RenderContourf([7, 9, 11]);
        var cmap = ColorMaps.Viridis;
        var low = cmap.ColorOf(8.0, LinearNormalizer.Instance, 7, 11);
        var high = cmap.ColorOf(10.0, LinearNormalizer.Instance, 7, 11);

        // the lowest band under the whole plot, then the rings of each band: [7, 9) holds the rings at 7 and 9, [9, 11) those at 9 and 11
        Assert.Equal(low, Arg<Color?>(Assert.Single(ctx.OfKind("DrawRectangle")), "fill"));
        var polygons = ctx.OfKind("DrawPolygon").Select(c => Arg<Color?>(c, "fill")).ToList();
        Assert.Equal([low, low, high, high], polygons);
    }

    [Fact]
    public void AFilledContour_WithExplicitLevels_IgnoresTheLevelCount() =>
        Assert.Equal(Signature(RenderContourf([7, 9, 11], levels: 2)), Signature(RenderContourf([7, 9, 11], levels: 40)));

    [Fact]
    public void AFilledContour_TakesTheExplicitLevelsSortedDistinctAndFinite() =>
        Assert.Equal(Signature(RenderContourf([7, 9, 11])), Signature(RenderContourf([11, NaN, 7, 9, 9, double.PositiveInfinity])));

    /// <summary>A band needs two levels; fewer than two that can be drawn leave nothing to fill, and the count is not
    /// used in their place.</summary>
    [Theory]
    [InlineData(new[] { 5.0 })]
    [InlineData(new[] { NaN, 5.0 })]
    [InlineData(new[] { 5.0, 5.0 })]
    [InlineData(new[] { NaN })]
    public void AFilledContour_WithFewerThanTwoDrawableLevels_DrawsNothing(double[] levels) =>
        Assert.Empty(RenderContourf(levels).Calls);

    [Fact]
    public void AFilledContour_WithoutExplicitLevels_KeepsTheEvenBandsOfTheCount()
    {
        var ctx = RenderContourf(null, levels: 4);

        // levels 0, 4, 8 and 12 bound three bands
        Assert.Equal(13, ctx.CountOf("DrawLines"));
        Assert.Equal(21, ctx.CountOf("DrawPolygon"));
        Assert.Equal(1, ctx.CountOf("DrawRectangle"));
        Assert.Equal(Signature(ctx), Signature(RenderContourf([], levels: 4)));
    }

    [Fact]
    public void AFilledContour_ColourBarSpansTheExplicitLevels_AsTheColoursDo()
    {
        var series = new ContourfSeries(ConeAxis, ConeAxis, Cone()) { LevelValues = [11, NaN, 7, 9] };

        var (min, max) = series.GetColorBarRange();

        Assert.Equal(7, min);
        Assert.Equal(11, max);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(new double[0])]
    [InlineData(new[] { 5.0 })]
    [InlineData(new[] { NaN, 5.0 })]
    public void AFilledContour_ColourBarSpansTheGrid_WithoutTwoDrawableLevels(double[]? levels)
    {
        var series = new ContourfSeries(ConeAxis, ConeAxis, Cone()) { LevelValues = levels };

        var (min, max) = series.GetColorBarRange();

        Assert.Equal(0, min);
        Assert.Equal(12, max);
    }

    // ---- the public path -------------------------------------------------------------------------------------------------

    [Fact]
    public void ExplicitLevels_DrawTheSamePictureAfterAJsonRoundTrip()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 2, 1, ax => ax.Contour(RampX, RampY, Ramp(), s => s.LevelValues = [1, 5, 9]))
            .AddSubPlot(1, 2, 2, ax => ax.Contourf(ConeAxis, ConeAxis, Cone(), s => s.LevelValues = [7, 9, 11]))
            .Build();

        string drawn = figure.ToSvg();

        Assert.Equal(drawn, ChartServices.Serializer.FromJson(figure.ToJson()).ToSvg());
    }

    [Fact]
    public void ExplicitLevels_ChangeThePicture()
    {
        string Svg(double[]? levels) => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Contour(RampX, RampY, Ramp(), s => s.LevelValues = levels))
            .ToSvg();

        Assert.NotEqual(Svg(null), Svg([1, 5, 9]));
        Assert.Equal(Svg(null), Svg([]));
    }
}
