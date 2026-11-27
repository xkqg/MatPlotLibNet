// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>
/// Verifies SVG output of <see cref="PlanarBar3DSeries"/> rendering.
/// Ships as a v1.1.4 bonus series with zero test coverage until now.
/// </summary>
public class PlanarBar3DSeriesRenderTests
{
    private static readonly double[] X = [0.0, 1.0, 2.0];
    private static readonly double[] Y = [0.0, 0.0, 1.0];
    private static readonly double[] Z = [2.0, 3.5, 1.5];

    /// <summary>A planar bar is matplotlib's <c>Axes3D.bar</c>: a <c>Patch3D</c> made from the 2-D bar patch (printed with 3.11.2:
    /// <c>type(ax.bar(..., zs=1, zdir='y')[0])</c> is <c>Patch3D</c>, <c>get_joinstyle()</c> 'miter'), so its face keeps the mitre of a
    /// patch under matplotlib's themes, where a collection (a <c>bar3d</c>, a surface) is joined round.</summary>
    [Fact]
    public void APlanarBarsFace_IsAPatch_MitredUnderMatplotlibsThemes()
    {
        string svg = Plt.Create().WithTheme(Theme.MatplotlibV2)
            .AddSubPlot(1, 1, 1, ax => ax.PlanarBar3D(X, Y, Z, s => s.Color = Colors.Red))
            .ToSvg();

        var faces = svg.AllElements().Where(element => element.Name is "polygon" or "path" && element["fill"] == "#FF0000").ToList();
        Assert.Equal(X.Length, faces.Count);
        Assert.All(faces, face => Assert.Null(face["stroke-linejoin"]));
    }

    [Fact]
    public void PlanarBar3D_SvgContainsPolygons()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.PlanarBar3D(X, Y, Z))
            .ToSvg();
        Assert.True(svg.Contains("<polygon") || svg.Contains("<path"),
            "Expected SVG to contain filled polygon or path elements for bar faces");
    }

    /// <summary>A planar bar series drawn off a 3-D axes (no scene shared with other series) is still one scene: its bars are drawn
    /// the farthest first, as on a 3-D axes, not in the order they were given. The camera at rest looks from the side of negative Y,
    /// so the bar at Y = 3 stands behind the one at Y = 0 and is drawn first, although it was given second.</summary>
    [Fact]
    public void OffAThreeDAxes_TheBarsAreDrawnBackToFront()
    {
        var front = Color.FromHex("#FF0000");
        var back = Color.FromHex("#0000FF");
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.PlanarBar3D([0.0, 0.0], [0.0, 3.0], [1.0, 1.0], s => s.Colors = [front, back]))
            .Build();
        figure.SubPlots[0].CoordinateSystem = CoordinateSystem.Cartesian;

        string svg = figure.ToSvg();
        int frontAt = svg.IndexOf($"fill=\"{front.ToHex()}\"", StringComparison.Ordinal);
        int backAt = svg.IndexOf($"fill=\"{back.ToHex()}\"", StringComparison.Ordinal);

        Assert.True(frontAt > 0 && backAt > 0, "both bars are drawn");
        Assert.True(backAt < frontAt, "the bar behind is drawn before the bar in front");
    }
}
