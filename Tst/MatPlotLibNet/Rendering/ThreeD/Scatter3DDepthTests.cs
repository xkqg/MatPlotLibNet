// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Xml.Linq;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;

namespace MatPlotLibNet.Tests.Rendering.ThreeD;

/// <summary>How a 3-D scatter shows depth, read from matplotlib 3.11.2 with <c>inspect</c> and measured. <c>Path3DCollection.do_3d_projection</c>
/// sorts the markers by their projected z and keeps their sizes (<c>self._sizes = self._sizes3d[z_markers_idx]</c>, no perspective
/// factor), so every marker is one size; the depth is shown by <c>depthshade</c> (<c>rcParams['axes3d.depthshade']</c>: True in the
/// default style, False in the classic one; <c>depthshade_minalpha</c> 0.3 in both), which scales each marker's alpha by <c>_zalpha</c>:
/// <c>clip(1 - (z - min(z)) / data_scale, min_alpha, 1)</c> over the projected z of the markers, <c>data_scale</c> being the root of the
/// summed squares of the ranges of the projected x, y and z (<c>_get_data_scale</c>). The edge follows the face (<c>edgecolors='face'</c>).
/// Measured, <c>scatter([1, 2, 3], [1, 3, 2], [1, 3, 2])</c> on axes limited to 0..4 viewed from 30 / -60 degrees: <c>proj_transform</c>
/// gives x (-0.04058544, 0.0147167, 0.02611107), y (-0.02473502, 0.0318626, -0.00753762), z (-0.99827641, -0.98896247, -1.01305553), the
/// scale is 0.09073142, and the opacities, farthest first, 0.73445737, 0.83711127, 1.0; the classic style draws one opacity. The library
/// scaled each marker by its depth (half the size at the farthest) under every theme, and shaded none; its own themes keep that.</summary>
public class Scatter3DDepthTests
{
    private const double Pt = 100.0 / 72.0;

    private static readonly double[] X = [1.0, 2.0, 3.0];
    private static readonly double[] Y = [1.0, 3.0, 2.0];
    private static readonly double[] Z = [1.0, 3.0, 2.0];

    private static List<XElement> Circles(Theme theme)
    {
        var svg = Plt.Create().WithSize(400, 300).WithTheme(theme)
            .AddSubPlot(1, 1, 1, ax => ax.SetXLim(0, 4).SetYLim(0, 4).SetZLim(0, 4).WithCamera(30, -60).Scatter3D(X, Y, Z)).Build().ToSvg().Xml();
        var face = theme.CycleColors[0].ToHex();
        return [.. svg.Each("circle").Where(e => (string?)e.Attribute("fill") == face)];
    }

    private static double Number(XElement e, string attribute, double resting = 1) =>
        e.Attribute(attribute) is { } a ? double.Parse(a.Value, CultureInfo.InvariantCulture) : resting;

    // ── the matplotlib themes: one size, the default style fades by depth ────────────────────────────────────────

    [Fact]
    public void V2_EveryMarkerIsOneSize_AndTheFarthestFadesAsDepthshadeFadesIt()
    {
        var circles = Circles(Theme.MatplotlibV2);

        Assert.Equal(3, circles.Count);
        Assert.All(circles, c => Assert.Equal(Math.Sqrt(20) * Pt / 2, Number(c, "r"), 1));
        // Farthest first, as the painter draws them.
        Assert.Equal([0.73445737, 0.83711127, 1.0], circles.Select(c => Number(c, "fill-opacity")), new Within(0.005));
    }

    [Fact]
    public void V2_TheEdgeFollowsTheFace()
    {
        var circles = Circles(Theme.MatplotlibV2);

        Assert.All(circles, c => Assert.Equal(Number(c, "fill-opacity"), Number(c, "stroke-opacity"), 6));
    }

    [Fact]
    public void Classic_EveryMarkerIsOneSize_AndNoneFades()
    {
        var circles = Circles(Theme.MatplotlibClassic);

        Assert.Equal(3, circles.Count);
        Assert.All(circles, c =>
        {
            Assert.Equal(Math.Sqrt(20) * Pt / 2, Number(c, "r"), 1);
            Assert.Equal(1, Number(c, "fill-opacity"));
        });
    }

    // ── the library's own themes keep their depth cue ────────────────────────────────────────────────────────────

    [Fact]
    public void TheLibrarysOwnTheme_ShrinksTheFarthestMarkerToHalf_AndFadesNone()
    {
        var radii = Circles(Theme.Default).Select(c => Number(c, "r")).ToArray();

        Assert.Equal(3, radii.Length);
        Assert.Equal(0.5, radii[0] / radii[^1], 2);   // the SVG writes a radius to a hundredth
        Assert.All(Circles(Theme.Default), c => Assert.Equal(1, Number(c, "fill-opacity")));
    }

    /// <summary>Markers that all stand at one depth have no farthest and nearest to shrink between: each is drawn halfway, at three
    /// quarters of its full size, where a lone marker, the farthest of its series, is drawn at half.</summary>
    [Fact]
    public void TheLibrarysOwnTheme_DrawsMarkersOfOneDepthAtThreeQuarters_AndALoneMarkerAtHalf()
    {
        double RadiusOf(double[] at) => Plt.Create().WithSize(400, 300).WithTheme(Theme.Default)
                .AddSubPlot(1, 1, 1, ax => ax.SetXLim(0, 4).SetYLim(0, 4).SetZLim(0, 4).Scatter3D(at, at, at)).Build().ToSvg().Each("circle").Where(e => (string?)e.Attribute("fill") == Theme.Default.CycleColors[0].ToHex())
            .Select(c => Number(c, "r")).Distinct().Single();

        Assert.Equal(1.5, RadiusOf([2.0, 2.0]) / RadiusOf([2.0]), 2);
    }

    // ── the projection the fade reads ────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AProjected3DPoint_IsMatplotlibsProjTransform()
    {
        var projection = new Projection3D(30, -60, new Rect(0, 0, 400, 300), 0, 4, 0, 4, 0, 4);

        var a = projection.ProjectedAt(1, 1, 1);
        var b = projection.ProjectedAt(2, 3, 3);
        var c = projection.ProjectedAt(3, 2, 2);

        Assert.Equal(-0.04058544, a.X, 7);
        Assert.Equal(-0.02473502, a.Y, 7);
        Assert.Equal(-0.99827641, a.Z, 7);
        Assert.Equal(0.0147167, b.X, 7);
        Assert.Equal(0.0318626, b.Y, 7);
        Assert.Equal(-0.98896247, b.Z, 7);
        Assert.Equal(0.02611107, c.X, 7);
        Assert.Equal(-0.00753762, c.Y, 7);
        Assert.Equal(-1.01305553, c.Z, 7);
    }

    // ── the fade itself ──────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TheScale_IsTheRootOfTheSummedSquaresOfTheProjectedRanges()
    {
        var shade = Scatter3DSeriesRenderer.DepthShade.Of([new(-0.04058544, -0.02473502, -0.99827641), new(0.0147167, 0.0318626, -0.98896247), new(0.02611107, -0.00753762, -1.01305553)]);

        Assert.Equal(0.09073142, shade.Scale, 7);
        Assert.Equal(-1.01305553, shade.Nearest, 7);
    }

    [Fact]
    public void TheNearestPointKeepsItsOpacity_AndTheFartherOnesFadeDownToTheLeast()
    {
        var shade = new Scatter3DSeriesRenderer.DepthShade(Nearest: -1, Scale: 0.1);

        Assert.Equal(1, shade.OpacityOf(-1, 0.3));
        Assert.Equal(0.5, shade.OpacityOf(-0.95, 0.3), 9);
        Assert.Equal(0.3, shade.OpacityOf(-0.5, 0.3));
    }

    [Fact]
    public void ADataScaleOfNothing_FadesNothing()
    {
        Assert.Equal(1, new Scatter3DSeriesRenderer.DepthShade(Nearest: -1, Scale: 0).OpacityOf(-0.5, 0.3));
        Assert.Equal(0, Scatter3DSeriesRenderer.DepthShade.Of([new(1, 2, 3)]).Scale);
    }

    [Fact]
    public void TheLeastOpacity_IsHeldBetween0And1()
    {
        var shade = new Scatter3DSeriesRenderer.DepthShade(Nearest: -1, Scale: 0.1);

        Assert.Equal(1, shade.OpacityOf(-0.5, 2));
        Assert.Equal(0, shade.OpacityOf(-0.5, -1));
    }

    [Fact]
    public void NoPoints_HaveNoScale()
    {
        Assert.Equal(0, Scatter3DSeriesRenderer.DepthShade.Of([]).Scale);
    }

    // ── the theme and the slot carry it ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TheDefaultStyleFadesToAThirdOfItsOpacity_TheClassicStyleDoesNotFade_AndTheJsonKeepsBoth()
    {
        Assert.Equal(0.3, Theme.MatplotlibV2.Looks.Scatter!.Value.DepthShade);
        Assert.Null(Theme.MatplotlibClassic.Looks.Scatter!.Value.DepthShade);
        Assert.Equal(0.3, Theme.MatplotlibV2.ThroughDto().Looks.Scatter!.Value.DepthShade);
        Assert.Null(Theme.MatplotlibClassic.ThroughDto().Looks.Scatter!.Value.DepthShade);
        Assert.Equal(0.3, Theme.MatplotlibV2.Cloned().Looks.Scatter!.Value.DepthShade);
    }

    [Fact]
    public void TheSlot_KeepsOneSizeUnderAThemeThatNamesAScatterLook_AndFadesToItsLeastOpacity()
    {
        var v2 = Theme.MatplotlibV2.SeriesSlot(0);
        var classic = Theme.MatplotlibClassic.SeriesSlot(0);
        var plain = Theme.Default.SeriesSlot(0);

        Assert.True(v2.Scatter3DKeepsOneSize);
        Assert.True(classic.Scatter3DKeepsOneSize);
        Assert.False(plain.Scatter3DKeepsOneSize);
        Assert.Equal(0.3, v2.Scatter3DLeastOpacity);
        Assert.Null(classic.Scatter3DLeastOpacity);
        Assert.Null(plain.Scatter3DLeastOpacity);
    }

    [Fact]
    public void AFadedStyle_FadesTheFill_AndTheEdgeThatFollowsIt()
    {
        var style = Theme.MatplotlibV2.SeriesSlot(0).ScatterStyleOf(Colors.Blue, 0.5);

        Assert.Equal(128, style.Fill!.Value.A);
        Assert.Equal(128, style.Stroke!.Value.A);
    }

    [Fact]
    public void AFadedStyle_FadesAnEdgeColourTheThemeNames_Once()
    {
        // Under a theme that names an edge colour (the classic style's black) the edge is that colour, faded on its own.
        var style = Theme.MatplotlibClassic.SeriesSlot(0).ScatterStyleOf(Colors.Blue, 0.5);

        Assert.Equal(128, style.Fill!.Value.A);
        Assert.Equal(Colors.Black.WithAlpha(128), style.Stroke);
    }

    [Fact]
    public void AStyleWithNoEdgeWidth_DrawsNoEdgeWhateverItsFade()
    {
        var style = Theme.Default.SeriesSlot(0).ScatterStyleOf(Colors.Blue, 0.5);

        Assert.Null(style.Stroke);
        Assert.Equal(128, style.Fill!.Value.A);
    }

    private sealed class Within(double tolerance) : IEqualityComparer<double>
    {
        public bool Equals(double x, double y) => Math.Abs(x - y) <= tolerance;
        public int GetHashCode(double obj) => 0;
    }
}
