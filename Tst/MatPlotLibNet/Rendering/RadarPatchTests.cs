// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Xml.Linq;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A radar chart stands on the patch of its axes, filled with the axes' colour, as matplotlib's radar does. matplotlib 3.11.2:
/// a radar chart is a polar axes (<c>subplot_kw={"projection": "polar"}</c>, and the gallery's recipe with <c>frame='polygon'</c> gives it
/// <c>_gen_axes_patch</c> of a <c>RegularPolygon</c>), and <c>Axes.draw</c> draws the axes patch unless <c>set_axis_off()</c> was called;
/// a polar axes' patch is filled with <c>axes.facecolor</c> (white in both styles: the classic style's figure is grey, <c>figure.facecolor:
/// 0.75</c>, so the disc stands out). A radar draws no axes of the library's, and the classic theme leaves such an axes unpainted
/// (<c>AxesFrameLook.NoBackgroundWithoutAxes</c>), which took the white from behind the web and left the figure's grey there, where the polar axes
/// of the same figure (<c>PolarAxesRenderer</c>, one rule for the axes' patch) keeps its white disc. The web's outer ring now carries the
/// axes' colour under a theme that leaves the axes unpainted; every other theme paints the axes' rectangle, white or its own, behind the
/// web as it always did, and draws the same marks.</summary>
public sealed class RadarPatchTests
{
    private static readonly string[] Categories = ["Speed", "Power", "Range", "Accuracy", "Cost"];
    private static readonly double[] Values = [0.8, 0.6, 0.7, 0.9, 0.5];

    private static XDocument Svg(Theme theme) =>
        Plt.Create().WithSize(500, 400).WithTheme(theme).AddSubPlot(1, 1, 1, ax => ax.Radar(Categories, Values)).Build().ToSvg().Xml();

    private static List<XElement> Polygons(XDocument svg) => [.. svg.Each("polygon")];

    private static List<XElement> Filled(XDocument svg, Color fill) => [.. Polygons(svg).Where(e => (string?)e.Attribute("fill") == fill.ToHex())];

    private static (double Width, double Height) Extent(XElement polygon)
    {
        var points = ((string)polygon.Attribute("points")!).Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Split(',')).Select(p => (X: double.Parse(p[0], CultureInfo.InvariantCulture), Y: double.Parse(p[1], CultureInfo.InvariantCulture))).ToArray();
        return (points.Max(p => p.X) - points.Min(p => p.X), points.Max(p => p.Y) - points.Min(p => p.Y));
    }

    [Fact]
    public void Classic_TheOuterRingOfTheWeb_IsFilledWithTheAxesColour()
    {
        var svg = Svg(Theme.MatplotlibClassic);

        var patch = Assert.Single(Filled(svg, Theme.MatplotlibClassic.AxesBackground));
        // It is the polygon of the outermost of the five rings: no other polygon of the chart is wider, and the ring drawn over it is as wide.
        var widest = Polygons(svg).Max(p => Extent(p).Width);
        Assert.Equal(widest, Extent(patch).Width, 6);
        Assert.Contains(Polygons(svg), p => p != patch && (string?)p.Attribute("fill") == "none" && Math.Abs(Extent(p).Width - widest) < 1e-6);
    }

    [Fact]
    public void Classic_ThePatchStandsBehindTheRingsAndTheData()
    {
        var polygons = Polygons(Svg(Theme.MatplotlibClassic));

        // The patch comes first and the data polygon is drawn after the rings; the rings (dotted, as the classic grid is) are polylines, and
        // the frame is the polygon drawn over the data: the axes' patch stands behind everything.
        var patch = Assert.Single(polygons, p => (string?)p.Attribute("fill") == Theme.MatplotlibClassic.AxesBackground.ToHex());
        Assert.Equal(0, polygons.IndexOf(patch));
        Assert.Equal(3, polygons.Count);
    }

    [Fact]
    public void V2_TheWebHasNoPatchOfItsOwn_TheAxesRectangleIsBehindIt()
    {
        var svg = Svg(Theme.MatplotlibV2);

        Assert.Empty(Filled(svg, Theme.MatplotlibV2.AxesBackground));
        Assert.Equal(6, Polygons(svg).Count);
    }

    [Fact]
    public void TheLibrarysOwnTheme_HasNoPatchOfItsOwnEither()
    {
        var svg = Svg(Theme.Default);

        Assert.Empty(Filled(svg, Theme.Default.AxesBackground));
        Assert.Equal(6, Polygons(svg).Count);
    }
}
