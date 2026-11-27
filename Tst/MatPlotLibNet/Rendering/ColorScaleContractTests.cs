// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.ColorBarRendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;
using MatPlotLibNet.Tests;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The colour scale of a kind that maps its values to colours (its map, its normaliser, the range its colours run over) is
/// worked out in one place, <c>ColorScaledExtensions.ColorScaleUnder</c>, from the values the kind says its colours run over
/// (<see cref="IColorScaled.ColourValuesOver"/>): its renderer, its colour bar and a model that colours a part read that one scale.
/// A range of no width (every value the same) is opened one wide from its low end under every theme (<c>ColorMapping.Opened</c>), so
/// every value takes the map's first colour, as matplotlib draws a norm of no width (<c>Normalize.__call__</c>: <c>if vmin == vmax:
/// result.fill(0)</c>; python, 3.11.2, no colour bar: an <c>imshow</c> of zeros, a <c>hexbin</c> of one count, a <c>scatter</c> of one
/// value, each in the map's first colour), and its colour bar names that range.</summary>
public class ColorScaleContractTests
{
    private static readonly LinearColorMap RedToBlue = new("test", [Colors.Red, Colors.Blue]);

    private static double[,] Grid(int rows, int cols, double value)
    {
        var grid = new double[rows, cols];
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                grid[r, c] = value;
        return grid;
    }

    private static TreeNode Tree(double colourValue) => new()
    {
        Label = "root",
        ColorValue = colourValue,
        Children = [new TreeNode { Label = "a", Value = 1, ColorValue = colourValue }, new TreeNode { Label = "b", Value = 2, ColorValue = colourValue }],
    };

    /// <summary>Each kind that maps its values to colours, made with every value it colours the same: the series and that value.</summary>
    private static readonly Dictionary<string, (Func<IColorScaled> Make, double Value)> Kinds = new()
    {
        ["heatmap"] = (() => new HeatmapSeries(Grid(2, 2, 5)) { ColorMap = RedToBlue }, 5),
        ["clustermap"] = (() => new ClustermapSeries(Grid(2, 2, 5)) { ColorMap = RedToBlue }, 5),
        ["pcolormesh"] = (() => new PcolormeshSeries(new([0.0, 1, 2]), new([0.0, 1, 2]), Grid(2, 2, 5)) { ColorMap = RedToBlue }, 5),
        ["histogram2d"] = (() => new Histogram2DSeries([0.25, 0.75, 0.25, 0.75], [0.25, 0.25, 0.75, 0.75], 2, 2) { ColorMap = RedToBlue }, 1),
        ["image"] = (() => new ImageSeries(Grid(2, 2, 5)) { ColorMap = RedToBlue }, 5),
        ["polarheatmap"] = (() => new PolarHeatmapSeries(Grid(2, 2, 5), 2, 2) { ColorMap = RedToBlue }, 5),
        ["spectrogram"] = (() => new SpectrogramSeries(new(new double[512])) { ColorMap = RedToBlue }, 0),
        ["tripcolor"] = (() => new TripcolorSeries(new([0.0, 1, 0, 1]), new([0.0, 0, 1, 1]), new([5.0, 5, 5, 5])) { ColorMap = RedToBlue }, 5),
        ["scatter"] = (() => new ScatterSeries([0.0, 1, 2], [0.0, 1, 2]) { C = [5.0, 5, 5], ColorMap = RedToBlue }, 5),
        ["scatter3d"] = (() => new Scatter3DSeries(new([0.0, 1, 2]), new([0.0, 1, 2]), new([5.0, 5, 5])) { ColorMap = RedToBlue }, 5),
        ["trisurf"] = (() => new Trisurf3DSeries(new([0.0, 1, 0]), new([0.0, 0, 1]), new([5.0, 5, 5])) { ColorMap = RedToBlue }, 5),
        ["surface"] = (() => new SurfaceSeries([0.0, 1, 2], [0.0, 1, 2], Grid(3, 3, 5)) { ColorMap = RedToBlue }, 5),
        ["treemap"] = (() => new TreemapSeries(Tree(5)) { ColorMap = RedToBlue }, 5),
        ["icicle"] = (() => new IcicleSeries(Tree(5)) { ColorMap = RedToBlue }, 5),
    };

    public static TheoryData<string, string> KindsUnderEachTheme { get; } = Rows();

    private static TheoryData<string, string> Rows()
    {
        var rows = new TheoryData<string, string>();
        foreach (var kind in Kinds.Keys)
        {
            rows.Add(kind, "Default");
            rows.Add(kind, "MatplotlibV2");
        }
        return rows;
    }

    /// <summary>The range a range of no width at <paramref name="value"/> is opened to, under every theme.</summary>
    private static MinMaxRange Opened(double value) => new(value, value + 1);

    /// <summary>Every kind of the library and its packages that names a colour map and a normaliser states its colour scale, so no
    /// kind colours its values past the one owner; a contour, whose colours run over its levels, states them through its levels
    /// (<see cref="IThemedColorBar"/>, <c>ContourLevels</c>) instead. The hexbin and the dendrogram are held by their own tests (the
    /// hexbin's range starts from its least count, <c>HexbinColourRangeTests</c>; a dendrogram colours its clusters, not values).</summary>
    [Fact]
    public void EveryKindThatMapsItsValuesToColours_StatesItsColourScale()
    {
        var mapped = new[] { typeof(HeatmapSeries).Assembly, typeof(MatPlotLibNet.Geo.Series.GeoPolygonSeries).Assembly }
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type is { IsClass: true, IsAbstract: false }
                && typeof(IColormappable).IsAssignableFrom(type) && typeof(INormalizable).IsAssignableFrom(type))
            .ToList();

        Assert.NotEmpty(mapped);
        Assert.All(mapped, type => Assert.True(typeof(IColorScaled).IsAssignableFrom(type) != typeof(IThemedColorBar).IsAssignableFrom(type),
            $"{type.Name} maps its values to colours past IColorScaled (or states both a colour scale and levels)."));
    }

    /// <summary>Each kind is held by the theory below, so no kind of the library escapes the rule of a range of no width. The figure image is
    /// held by its own tests (<c>FigImageTests</c>), the hexbin by <c>HexbinColourRangeTests</c>; a dendrogram and a sunburst colour by part,
    /// not by value.</summary>
    [Fact]
    public void TheTheory_HoldsEveryColourScaledKindOfTheLibrary()
    {
        var scaled = typeof(HeatmapSeries).Assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IColorScaled).IsAssignableFrom(type))
            .Select(type => type.Name).Order().ToList();

        var held = Kinds.Values.Select(kind => kind.Make().GetType().Name)
            .Append(nameof(FigureImage)).Append(nameof(DendrogramSeries)).Append(nameof(SunburstSeries)).Append(nameof(HexbinSeries))
            .Order().ToList();

        Assert.Equal(held.Except(scaled), []);
        Assert.Equal(scaled.Except(held), []);
    }

    /// <summary>A kind whose values are all one value is coloured over the range opened one wide, and its colour bar names the same
    /// range.</summary>
    [Theory]
    [MemberData(nameof(KindsUnderEachTheme))]
    public void ARangeOfNoWidth_IsOpenedOneWide_AndTheBarNamesIt(string kind, string themeName)
    {
        var (make, value) = Kinds[kind];
        var theme = themeName.AsTheme();
        var series = make();

        var scale = series.ColorScaleUnder(theme);

        Assert.Equal(Opened(value), new MinMaxRange(scale.Min, scale.Max));
        if (series is IColorBarDataProvider provider)
            Assert.Equal(Opened(value), provider.ColorBarRangeUnder(theme));
    }

    /// <summary>The cells of a kind whose values are all one value are drawn in the colour its scale gives that value, the map's first
    /// colour, under every theme; none takes the map's middle (the normaliser's answer for a range of no width).</summary>
    [Theory]
    [MemberData(nameof(KindsUnderEachTheme))]
    public void TheMarksOfAKindOfOneValue_AreDrawnInTheColourItsScaleGives(string kind, string themeName)
    {
        var (make, _) = Kinds[kind];
        var series = make();
        var figure = Plt.Create().WithTheme(themeName.AsTheme()).Build();
        figure.AddSubPlot().AddSeries((ISeries)series);
        var ctx = new RecordingRenderContext();

        new ChartRenderer().Render(figure, ctx);

        var expected = RedToBlue.GetColor(0);
        var other = RedToBlue.GetColor(0.5);
        var fills = ctx.Fills();
        Assert.DoesNotContain(fills, fill => Near(fill, other));
        // A silent signal under a theme that draws specgram's decibels (SpectrogramLook) has none: 10 log10 of no power is no number, so
        // every cell is the map's bad colour and none is drawn in a colour of the map.
        if (series is SpectrogramSeries && themeName.AsTheme().Looks.Spectrogram.LikeSpecgram)
            Assert.DoesNotContain(fills, fill => Near(fill, expected));
        else
            Assert.Contains(fills, fill => Near(fill, expected));
    }

    /// <summary>Whether two colours are one, to the rounding of a channel.</summary>
    private static bool Near(Color a, Color b) => Math.Abs(a.R - b.R) <= 1 && Math.Abs(a.G - b.G) <= 1 && Math.Abs(a.B - b.B) <= 1;

    /// <summary>The one rule: the drawable values, the given ends winning, and a range of no width opened one wide from its low end (an
    /// inverted pair of given ends is opened from its low end too: matplotlib refuses <c>vmin &gt; vmax</c>).</summary>
    [Theory]
    [InlineData(2.0, 7.0, null, null, 2.0, 7.0)]
    [InlineData(5.0, 5.0, null, null, 5.0, 6.0)]
    [InlineData(0.0, 0.0, null, null, 0.0, 1.0)]
    [InlineData(-5.0, -5.0, null, null, -5.0, -4.0)]
    [InlineData(2.0, 7.0, 0.0, null, 0.0, 7.0)]
    [InlineData(2.0, 7.0, null, 4.0, 2.0, 4.0)]
    [InlineData(2.0, 7.0, 4.0, 4.0, 4.0, 5.0)]
    [InlineData(2.0, 7.0, 9.0, 3.0, 9.0, 10.0)]
    public void TheRule_OpensTheRange(double lo, double hi, double? vMin, double? vMax, double min, double max) =>
        Assert.Equal(new MinMaxRange(min, max), ColorMapping.ColourRangeOf(new Range1D(lo, hi), vMin, vMax));

    /// <summary>With no value the range is 0 to 1, as matplotlib's colour bar takes a norm that has none; a given end still wins.</summary>
    [Fact]
    public void WithNoValue_TheRangeIsZeroToOne()
    {
        Assert.Equal(new MinMaxRange(0, 1), ColorMapping.ColourRangeOf(null, null, null));
        Assert.Equal(new MinMaxRange(5, 6), ColorMapping.ColourRangeOf(null, 5, null));
    }
}
