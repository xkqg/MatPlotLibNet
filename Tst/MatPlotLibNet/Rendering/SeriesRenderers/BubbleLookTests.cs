// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;
using MatPlotLibNet.Tests;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>A bubble chart is matplotlib's <c>scatter</c> with an array for <c>s</c>, so under a matplotlib theme it wears the look
/// a scatter wears where its author names nothing. matplotlib 3.11.2, <c>scatter([0, 10], [0, 10], s=[100, 400], alpha=0.5)</c>:
/// classic, face (0, 0, 1, 0.5), edge (0, 0, 0, 0.5), width 1, limits (-2, 12) on both axes, and the next <c>plot</c> still takes the
/// cycle's first colour ('b'); the default style, face C0 at 0.5, edge the same colour at 0.5, limits (-0.5, 10.5). The library's own
/// themes name no scatter look, and a bubble under them keeps the edge in its own colour that it was always drawn with.</summary>
public class BubbleLookTests
{
    private static readonly double[] X = [0.0, 10.0];
    private static readonly double[] Y = [0.0, 10.0];
    private static readonly double[] Sizes = [100.0, 400.0];

    /// <summary>What a marker is drawn with, as the SVG says it: where, how large, its fill and its edge.</summary>
    private readonly record struct Mark(string? Cx, string? Cy, string? R, string? Fill, string? FillOpacity,
        string? Stroke, string? StrokeOpacity, string? StrokeWidth);

    private static XDocument Svg(Theme theme, Action<AxesBuilder> axes) =>
        Plt.Create().WithSize(400, 300).WithTheme(theme).AddSubPlot(1, 1, 1, axes).Build().ToSvg().Xml();

    private static List<Mark> Marks(XDocument svg) =>
        [.. svg.Each("circle").Where(e => !e.Ancestors().Any(a => (string?)a.Attribute("class") == "legend"))
            .Select(e => new Mark((string?)e.Attribute("cx"), (string?)e.Attribute("cy"), (string?)e.Attribute("r"),
                (string?)e.Attribute("fill"), (string?)e.Attribute("fill-opacity"),
                (string?)e.Attribute("stroke"), (string?)e.Attribute("stroke-opacity"), (string?)e.Attribute("stroke-width")))];

    [Theory]
    [MemberData(nameof(ThemeCases.Matplotlib), MemberType = typeof(ThemeCases))]
    public void UnderAMatplotlibTheme_ABubbleIsDrawnAsAScatterOfTheSameSizesIs(string id)
    {
        var bubble = Marks(Svg(id.AsTheme(), ax => ax.Bubble(X, Y, Sizes, s => s.Alpha = 0.5)));
        var scatter = Marks(Svg(id.AsTheme(), ax => ax.AddSeries(new ScatterSeries(X, Y) { Sizes = Sizes, Alpha = 0.5 })));

        Assert.Equal(2, bubble.Count);
        Assert.Equal(scatter, bubble);
    }

    [Fact]
    public void Classic_ABubbleThatNamesNoColour_IsBlueWithABlackEdge_AndTakesNoColourOfTheCycle()
    {
        var svg = Svg(Theme.MatplotlibClassic, ax => ax
            .Bubble(X, Y, Sizes, s => s.Alpha = 0.5)
            .Plot([0.0, 10.0], [0.0, 10.0]));

        var mark = Marks(svg)[0];
        Assert.Equal(Colors.Blue.ToHex(), mark.Fill);
        Assert.Equal("#000000", mark.Stroke);

        var line = Assert.Single(svg.Each("polyline"), e => (string?)e.Attribute("stroke") == Theme.MatplotlibClassic.CycleColors[0].ToHex()
            && (string?)e.Attribute("fill") == "none");
        Assert.NotNull(line);
    }

    [Fact]
    public void Classic_ABubbleThatNamesAColour_IsInThatColour()
    {
        var mark = Marks(Svg(Theme.MatplotlibClassic, ax => ax.Bubble(X, Y, Sizes, s => { s.Color = Colors.Red; s.Alpha = 0.5; })))[0];

        Assert.Equal(Colors.Red.ToHex(), mark.Fill);
    }

    [Fact]
    public void ALibraryTheme_KeepsTheBubblesEdgeInItsOwnColour()
    {
        var mark = Marks(Svg(Theme.Default, ax => ax.Bubble(X, Y, Sizes, s => { s.Color = Colors.Red; s.Alpha = 0.5; })))[0];

        Assert.Equal(Colors.Red.ToHex(), mark.Fill);
        Assert.Equal(Colors.Red.ToHex(), mark.Stroke);
        Assert.Equal("1", mark.StrokeWidth);
    }
}
