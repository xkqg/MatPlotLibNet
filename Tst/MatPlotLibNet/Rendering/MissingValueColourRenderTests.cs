// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Every renderer that colours a data value through a colormap draws a missing value (NaN) in the map's bad
/// colour, and leaves it out of the range the other values are coloured over. Before, a NaN cell came out in whatever
/// an index computed from NaN gave (transparent black on net10.0, an IndexOutOfRangeException on net8.0), and a
/// renderer that took its colour range with <c>Math.Min</c> or LINQ <c>Min()</c> coloured every value over NaN, so
/// one missing value blanked the whole image.</summary>
public class MissingValueColourRenderTests
{
    private const string BadHex = "#13579B";
    private const string LowHex = "#000080";
    private const string HighHex = "#FFFF00";

    private static LinearColorMap MapWithBadColour() =>
        new("missing-value-test", [Color.FromHex(LowHex), Color.FromHex(HighHex)]) { BadColor = Color.FromHex(BadHex) };

    /// <summary>Data with its low end 0, its high end 1 and one NaN, so the two end stops must appear when the range
    /// ignores the NaN.</summary>
    private static readonly double[,] Cells = { { 0, double.NaN }, { 0.5, 1 } };

    private static string Svg(string shape, IColorMap map, INormalizer? normalizer = null) => shape switch
    {
        "heatmap" => Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Heatmap(Cells, s => { s.ColorMap = map; s.Normalizer = normalizer; })).ToSvg(),
        "pcolormesh" => Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Pcolormesh([0, 1, 2], [0, 1, 2], Cells, s => { s.ColorMap = map; s.Normalizer = normalizer; })).ToSvg(),
        "image" => Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Image(Cells, s => { s.ColorMap = map; s.Normalizer = normalizer; })).ToSvg(),
        "polar-heatmap" => Plt.Create().AddSubPlot(1, 1, 1, ax => ax.PolarHeatmap(Cells, 2, 2, s => { s.ColorMap = map; s.Normalizer = normalizer; })).ToSvg(),
        "scatter" => Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Scatter([0, 1, 2], [0, 1, 2], s =>
        {
            s.C = [0, double.NaN, 1]; s.ColorMap = map; s.Normalizer = normalizer;
        })).ToSvg(),
        // Three explicit triangles: one all at 0, one all at 1, one with a missing corner.
        "tripcolor" => Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Tripcolor(
            [0, 1, 0, 2, 3, 2, 4, 5, 4], [0, 0, 1, 0, 0, 1, 0, 0, 1], [0, 0, 0, 1, 1, 1, double.NaN, 0.5, 0.5], s =>
            {
                s.Triangles = [0, 1, 2, 3, 4, 5, 6, 7, 8]; s.ColorMap = map; s.Normalizer = normalizer;
            })).ToSvg(),
        _ => throw new ArgumentException(shape, nameof(shape)),
    };

    /// <summary>The renderers that colour a data value through a colormap.</summary>
    public static TheoryData<string> Shapes { get; } = new("heatmap", "pcolormesh", "image", "polar-heatmap", "scatter", "tripcolor");

    [Theory]
    [MemberData(nameof(Shapes))]
    public void AMissingValue_IsDrawnInTheColourMapsBadColour(string shape)
    {
        string svg = Svg(shape, MapWithBadColour());

        Assert.True(svg.Contains($"fill=\"{BadHex}\"", StringComparison.Ordinal), $"{shape}: no element is filled in the bad colour {BadHex}.");
    }

    [Theory]
    [MemberData(nameof(Shapes))]
    public void TheOtherValues_AreColouredOverARangeThatLeavesTheMissingValueOut(string shape)
    {
        string svg = Svg(shape, MapWithBadColour());

        Assert.True(svg.Contains($"fill=\"{LowHex}\"", StringComparison.Ordinal), $"{shape}: the lowest value is not drawn in the low end {LowHex}.");
        Assert.True(svg.Contains($"fill=\"{HighHex}\"", StringComparison.Ordinal), $"{shape}: the highest value is not drawn in the high end {HighHex}.");
    }

    /// <summary>The renderers that work out their own colour range from the values.</summary>
    public static TheoryData<string> SelfRangingShapes { get; } = new("image", "scatter");

    [Theory]
    [MemberData(nameof(SelfRangingShapes))]
    public void WhenEveryValueIsMissing_EverythingIsDrawnInTheBadColour(string shape)
    {
        var map = MapWithBadColour();
        string svg = shape == "image"
            ? Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Image(new[,] { { double.NaN, double.NaN } }, s => s.ColorMap = map)).ToSvg()
            : Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Scatter([0, 1], [0, 1], s => { s.C = [double.NaN, double.NaN]; s.ColorMap = map; })).ToSvg();

        Assert.Contains($"fill=\"{BadHex}\"", svg);
        Assert.DoesNotContain($"fill=\"{LowHex}\"", svg);
        Assert.DoesNotContain($"fill=\"{HighHex}\"", svg);
    }

    /// <summary>A colour bar shows the range the cells are coloured over: the drawable values only.</summary>
    [Fact]
    public void TheColourBarOfAnImageOrATriangleMesh_SpansTheDrawableValuesOnly()
    {
        Assert.Equal(new(0, 1), new ImageSeries(Cells).GetColorBarRange());
        Assert.Equal(new(0, 1), new TripcolorSeries(new([0.0, 1, 0]), new([0.0, 0, 1]), new([0.0, double.NaN, 1])).GetColorBarRange());
        Assert.Equal(new(2, 5), new TripcolorSeries(new([0.0, 1, 0]), new([0.0, 0, 1]), new([2.0, double.NaN, 5])).GetColorBarRange());
    }

    [Fact]
    public void TheColourBarOfAnImageOrATriangleMesh_WithNothingDrawable_KeepsItsDefaultRange()
    {
        Assert.Equal(new(0, 1), new ImageSeries(new[,] { { double.NaN } }).GetColorBarRange());
        Assert.Equal(new(0, 1), new TripcolorSeries(new([0.0]), new([0.0]), new([double.NaN])).GetColorBarRange());
    }

    [Fact]
    public void AHeatmapUnderASymLogNormalizer_DrawsAMissingCell_InsteadOfThrowing()
    {
        var thrown = Record.Exception(() => Svg("heatmap", MapWithBadColour(), new SymLogNormalizer()));

        Assert.Null(thrown);
    }

    [Fact]
    public void AHeatmapUnderABoundaryNormalizer_DrawsAMissingCell_InTheBadColour_NotInTheLastBin()
    {
        string svg = Svg("heatmap", MapWithBadColour(), new BoundaryNormalizer([0, 0.25, 0.5, 0.75, 1]));

        Assert.Contains($"fill=\"{BadHex}\"", svg);
    }

    /// <summary>A cell label states the cell's value; a missing cell has none, so it gets no label (seaborn's
    /// annotated heatmap leaves a masked cell blank).</summary>
    [Fact]
    public void AHeatmap_DoesNotLabelAMissingCell()
    {
        var ctx = new RecordingRenderContext();
        var bounds = new Rect(0, 0, 200, 200);
        var context = new SeriesRenderContext(new DataTransform(0, 2, 0, 2, bounds), ctx, Colors.Tab10Blue, new RenderArea(bounds, ctx));

        new HeatmapSeriesRenderer(context).Render(new HeatmapSeries(Cells) { ShowLabels = true });

        var labels = ctx.OfKind("DrawText").Select(c => (string)c.Args!.GetType().GetProperty("text")!.GetValue(c.Args)!).ToList();
        Assert.Equal(3, labels.Count);
        Assert.DoesNotContain(labels, l => l.Contains("NaN", StringComparison.Ordinal));
    }
}
