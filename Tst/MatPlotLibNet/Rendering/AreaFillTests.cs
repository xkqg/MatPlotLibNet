// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>How a fill between two curves is drawn under each theme. matplotlib 3.11.2, <c>Axes.fill_between</c> draws one
/// <c>PolyCollection</c> with no alpha and no line of its own: opaque, edged only where <c>patch.force_edgecolor</c> is on (the
/// classic style: <c>patch.edgecolor</c> black, <c>patch.linewidth</c> 1 pt), not in the default style. The library's own themes
/// keep their fill at 0.3 under a line along its top; an alpha, a line width or an edge colour given wins under every theme.</summary>
public sealed class AreaFillTests
{
    private static SvgElement[] Shapes(Theme theme, Action<AreaSeries>? configure = null) =>
        Shapes(Plt.Create().WithTheme(theme).WithSize(300, 200)
            .AddSubPlot(1, 1, 1, ax => ax.FillBetween([0.0, 1, 2], [1.0, 2, 1], configure: configure)).ToSvg());

    /// <summary>The filled shapes of the picture, then its lines drawn as a polyline.</summary>
    private static SvgElement[] Shapes(string svg) => [.. svg.Elements("polygon"), .. svg.Elements("polyline")];

    [Fact]
    public void V2_FillsOpaquely_WithNoLineAndNoEdge()
    {
        var shapes = Shapes(Theme.MatplotlibV2);

        var fill = Assert.Single(shapes);
        Assert.Equal("polygon", fill.Name);
        Assert.Null(fill["fill-opacity"]);
        Assert.Null(fill["stroke"]);
    }

    [Fact]
    public void Classic_FillsOpaquely_EdgedBlackOnePoint()
    {
        var fill = Assert.Single(Shapes(Theme.MatplotlibClassic));

        Assert.Equal("#0000FF", fill["fill"]);
        Assert.Null(fill["fill-opacity"]);
        Assert.Equal("#000000", fill["stroke"]);
        Assert.Equal("1.39", fill["stroke-width"]);
    }

    [Fact]
    public void TheLibrarysThemes_KeepATranslucentFillUnderALine()
    {
        var shapes = Shapes(Theme.Default);

        Assert.Equal(2, shapes.Length);
        Assert.NotNull(shapes[0]["fill-opacity"]);
        Assert.Equal("polyline", shapes[1].Name);
    }

    /// <summary>An alpha, a line width or an edge colour given wins under the matplotlib themes as under any.</summary>
    [Fact]
    public void AGivenAlphaLineOrEdge_Wins()
    {
        Assert.NotNull(Shapes(Theme.MatplotlibV2, s => s.Alpha = 0.3)[0]["fill-opacity"]);
        Assert.Contains(Shapes(Theme.MatplotlibV2, s => s.LineWidth = 2), shape => shape.Name == "polyline");
        Assert.Equal("#FF0000", Assert.Single(Shapes(Theme.MatplotlibV2, s => s.EdgeColor = Colors.Red))["stroke"]);
    }

    /// <summary>The JSON writes the alpha and the line width an area was given, its resting values included, and nothing when none
    /// was given.</summary>
    [Fact]
    public void AGivenAlphaAndWidth_AreWritten_TheRestingValuesIncluded()
    {
        var resting = new AreaSeries([0.0, 1], [1.0, 2]);
        var given = new AreaSeries([0.0, 1], [1.0, 2]) { Alpha = 0.3, LineWidth = 1.5 };

        Assert.Null(resting.ToSeriesDto().Alpha);
        Assert.Null(resting.ToSeriesDto().LineWidth);
        Assert.Equal(0.3, given.ToSeriesDto().Alpha);
        Assert.Equal(1.5, given.ToSeriesDto().LineWidth);
    }

    /// <summary>A fill a library verb lays keeps its translucent fill and its line under the default theme: the ridges of a
    /// ridgeline.</summary>
    [Fact]
    public void AFillAVerbLays_KeepsItsLook()
    {
        var shapes = Shapes(Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Ridgeline([new NamedSample("a", [1.0, 2, 2, 3, 4]), new NamedSample("b", [2.0, 3, 3, 4, 5])]))
            .ToSvg());

        Assert.Contains(shapes, shape => shape.Name == "polygon" && shape["fill-opacity"] is not null);
        Assert.Contains(shapes, shape => shape.Name == "polyline");
    }
}
