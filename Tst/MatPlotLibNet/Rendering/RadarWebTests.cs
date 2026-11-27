// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Xml.Linq;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Under the matplotlib themes the web of a radar chart is laid as matplotlib's polar axes lays its rings: at the ticks of the
/// radial axis' locator, labelled, in the theme's grid, with the frame in the spine's style; under the library's own themes the rings
/// stay at fifths of the axis, unlabelled. matplotlib 3.11.2, printed: <c>ax = fig.add_subplot(projection="polar"); ax.set_ylim(0, 9.3)</c>
/// on a 5 x 4 inch figure has <c>get_yticks()</c> 2, 4, 6, 8 (and 10, which is beyond the view) in both styles, labels '2' ... '10' at
/// <c>get_rlabel_position()</c> 22.5, the rings and spokes wear <c>grid.color</c> / <c>grid.linewidth</c> / <c>grid.linestyle</c>
/// (default: '#b0b0b0', 0.8, '-'; classic: 'k', 0.5, ':'), and <c>spines['polar']</c> is the frame (0.8 and 1.0 points, black). The
/// shape of the web, a polygon with its first spoke at the top, is the chart's own under every theme: matplotlib has no radar axes,
/// and its own gallery recipe for one (<c>frame='polygon'</c>, <c>set_theta_zero_location('N')</c>) is that shape.</summary>
public sealed class RadarWebTests
{
    private static readonly string[] Categories = ["Speed", "Power", "Range", "Accuracy", "Cost"];

    // The outer ring (the largest value) is 9.3, so the ticks 2, 4, 6 and 8 stand inside it and the next one, 10, does not.
    private static readonly double[] Values = [9.3, 5, 6, 7, 3];

    private static XDocument Svg(Theme theme, double[]? values = null) =>
        (Plt.Create().WithSize(500, 400).WithTheme(theme).AddSubPlot(1, 1, 1, ax => ax.Radar(Categories, values ?? Values)).Build().ToSvg()).Xml();

    /// <summary>The centre and the outer radius of the web: every spoke is a line from the centre out to the outermost ring.</summary>
    private static (double Cx, double Cy, double Radius) Web(XDocument svg)
    {
        var spokes = svg.Each("line").Where(l => l.Number("x1") == svg.Each("line").First().Number("x1") && l.Number("y1") == svg.Each("line").First().Number("y1")).ToList();
        Assert.Equal(5, spokes.Count);
        var first = spokes[0];
        return (first.Number("x1"), first.Number("y1"), Math.Sqrt(Math.Pow(first.Number("x2") - first.Number("x1"), 2) + Math.Pow(first.Number("y2") - first.Number("y1"), 2)));
    }

    /// <summary>How far each ring stands from the centre: the vertical distance of its first vertex, the one at the top.</summary>
    private static double[] Radii(IEnumerable<XElement> rings, double cy) =>
        [.. rings.Select(r => cy - double.Parse(((string)r.Attribute("points")!).Split(' ')[0].Split(',')[1], CultureInfo.InvariantCulture)).Order()];

    private static List<XElement> Rings(XDocument svg, string stroke) =>
        [.. svg.Each("polygon").Where(p => (string?)p.Attribute("fill") == "none" && (string?)p.Attribute("stroke") == stroke)];

    [Fact]
    public void V2_TheRingsStandAtTheTicksOfTheRadialLocator()
    {
        var svg = Svg(Theme.MatplotlibV2);
        var (_, cy, radius) = Web(svg);

        var rings = Radii(Rings(svg, Theme.MatplotlibV2.DefaultGrid.Color.ToHex()), cy);

        Assert.Equal(4, rings.Length);
        double[] ticks = [2, 4, 6, 8];
        for (int i = 0; i < ticks.Length; i++) Assert.Equal(radius * ticks[i] / 9.3, rings[i], 0.011);
    }

    [Fact]
    public void V2_TheRingsAreLabelledOnTheRayPastTheFirstSpoke()
    {
        var svg = Svg(Theme.MatplotlibV2);
        var (cx, cy, radius) = Web(svg);

        var labels = svg.Each("text").Where(t => t.Value is "2" or "4" or "6" or "8" or "10").ToList();

        Assert.Equal(["2", "4", "6", "8"], labels.OrderByDescending(t => t.Number("y")).Select(t => t.Value));
        // The ray turns 22.5 degrees from the first spoke the way the categories run (clockwise from the top), and crosses the polygon
        // ring of circumradius rho on the edge between the first two vertices (72 degrees apart): at rho cos(36) / cos(22.5 - 36). The label
        // stands 3 pixels right of that point and 3 above it.
        double edge = Math.PI / 5, ray = 22.5 * Math.PI / 180 - Math.PI / 2;
        foreach (var label in labels)
        {
            double rho = radius * double.Parse(label.Value, CultureInfo.InvariantCulture) / 9.3;
            double reach = rho * Math.Cos(edge) / Math.Cos(22.5 * Math.PI / 180 - edge);
            Assert.Equal(cx + reach * Math.Cos(ray) + 3, label.Number("x"), 0.011);
            Assert.Equal(cy + reach * Math.Sin(ray) - 3, label.Number("y"), 0.011);
        }
    }

    [Fact]
    public void V2_TheRingsAndTheSpokesWearTheThemesGrid()
    {
        var theme = Theme.MatplotlibV2;
        var svg = Svg(theme);
        var grid = theme.DefaultGrid;

        var rings = Rings(svg, grid.Color.ToHex());
        var spokes = svg.Each("line").Where(l => (string?)l.Attribute("stroke") == grid.Color.ToHex()).ToList();

        Assert.Equal(4, rings.Count);
        Assert.Equal(5, spokes.Count);
        Assert.All(rings.Concat(spokes), e => Assert.Equal(grid.LineWidth, e.Number("stroke-width"), 2));
    }

    [Fact]
    public void V2_TheOuterPolygonIsTheFrame_DrawnInTheSpinesStyle()
    {
        var theme = Theme.MatplotlibV2;
        var svg = Svg(theme);
        var (_, cy, radius) = Web(svg);

        var frame = Assert.Single(Rings(svg, theme.ForegroundText.ToHex()));

        Assert.Equal(radius, Radii([frame], cy)[0], 0.011);
        Assert.Equal(theme.Looks.Frame.SpineWidth!.Value, frame.Number("stroke-width"), 2);
    }

    [Fact]
    public void V2_ARingOnTheOutermostValue_IsLabelledAndIsDrawnUnderTheFrame()
    {
        var svg = Svg(Theme.MatplotlibV2, [10, 5, 6, 7, 3]);
        var (_, cy, radius) = Web(svg);

        var grid = Radii(Rings(svg, Theme.MatplotlibV2.DefaultGrid.Color.ToHex()), cy);

        Assert.Equal(5, grid.Length);
        Assert.Equal(radius, grid[^1], 0.011);
        Assert.Contains(svg.Each("text"), t => t.Value == "10");
    }

    [Fact]
    public void Classic_TheRingsAreDottedAsTheGridIs_AndLabelled()
    {
        var theme = Theme.MatplotlibClassic;
        var svg = Svg(theme);
        var (_, cy, radius) = Web(svg);
        var grid = theme.DefaultGrid;

        var rings = svg.Each("polyline").Where(p => (string?)p.Attribute("stroke") == grid.Color.ToHex() && p.Attribute("stroke-dasharray") is not null).ToList();

        Assert.Equal(4, rings.Count);
        Assert.All(rings, p => Assert.Equal(grid.LineWidth, p.Number("stroke-width"), 2));
        double[] radii = [.. rings.Select(r => cy - double.Parse(((string)r.Attribute("points")!).Split(' ')[0].Split(',')[1], CultureInfo.InvariantCulture)).Order()];
        double[] ticks = [2, 4, 6, 8];
        for (int i = 0; i < ticks.Length; i++) Assert.Equal(radius * ticks[i] / 9.3, radii[i], 0.011);
        Assert.Equal(["2", "4", "6", "8"], svg.Each("text").Where(t => t.Value is "2" or "4" or "6" or "8").OrderByDescending(t => t.Number("y")).Select(t => t.Value));
    }

    [Fact]
    public void Classic_TheFrameStandsOnTheAxesPatch()
    {
        var theme = Theme.MatplotlibClassic;
        var svg = Svg(theme);
        var (_, cy, radius) = Web(svg);

        var patch = Assert.Single(svg.Each("polygon"), p => (string?)p.Attribute("fill") == theme.AxesBackground.ToHex());
        var frame = Assert.Single(Rings(svg, theme.ForegroundText.ToHex()));

        Assert.Equal(radius, Radii([patch], cy)[0], 0.011);
        Assert.Equal(radius, Radii([frame], cy)[0], 0.011);
        Assert.Equal(theme.Looks.Frame.SpineWidth!.Value, frame.Number("stroke-width"), 2);
    }

    // Two radars on one axes, the second ten times the first: matplotlib draws both on one polar axes, which has one radial axis, so
    // one set of rings (rgrids) and one scale, the largest value of the axes (printed, matplotlib 3.11.2, 5 x 4 inches: two filled
    // lines up to 3 and up to 30 on one polar axes give get_ylim() (0, 30) under the classic style and (0, 31.45) under the default
    // one, and one set of rings, 5, 10, ... 30 in view, each labelled once). The radar's outer ring is its largest value.
    private static XDocument TwoRadars(Theme theme) =>
        Plt.Create().WithSize(500, 400).WithTheme(theme)
            .AddSubPlot(1, 1, 1, ax => ax.Radar(Categories, [1, 2, 3, 2, 1]).Radar(Categories, [10, 20, 30, 20, 10])).Build().ToSvg().Xml();

    /// <summary>The polygons of the data, in drawing order: filled, neither the web's rings nor the axes' patch.</summary>
    private static List<XElement> DataPolygons(XDocument svg, Theme theme) =>
        [.. svg.Each("polygon").Where(p => (string?)p.Attribute("fill") is { } fill && fill != "none" && fill != theme.AxesBackground.ToHex())];

    /// <summary>How far the first vertex of a polygon, the one on the first spoke at the top, stands above the centre.</summary>
    private static double TopReach(XElement polygon, double cy) =>
        cy - double.Parse(((string)polygon.Attribute("points")!).Split(' ')[0].Split(',')[1], CultureInfo.InvariantCulture);

    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    public void TwoRadarsWithDifferentLargestValues_ShareOneWebAndOneScale_TheLargestOfTheAxes(string themeId)
    {
        var theme = themeId.AsTheme();
        var svg = TwoRadars(theme);

        var (_, cy, radius) = Web(svg);   // one web: five spokes, not ten

        var data = DataPolygons(svg, theme);
        Assert.Equal(2, data.Count);
        Assert.Equal(radius * 1 / 30.0, TopReach(data[0], cy), 0.011);
        Assert.Equal(radius * 10 / 30.0, TopReach(data[1], cy), 0.011);
        Assert.Single(Rings(svg, theme.ForegroundText.ToHex()));   // one frame
        var labels = svg.Each("text").Select(t => t.Value).Where(v => double.TryParse(v, CultureInfo.InvariantCulture, out _)).ToList();
        Assert.Equal(labels.Distinct(), labels);                    // each ring labelled once
        Assert.Contains("30", labels);
    }

    [Fact]
    public void Classic_TwoRadars_PaintTheAxesPatchOnce_BehindBothPolygons()
    {
        var theme = Theme.MatplotlibClassic;
        var svg = TwoRadars(theme);
        var polygons = svg.Each("polygon").ToList();

        var patch = Assert.Single(polygons, p => (string?)p.Attribute("fill") == theme.AxesBackground.ToHex());

        Assert.All(DataPolygons(svg, theme), data => Assert.True(polygons.IndexOf(patch) < polygons.IndexOf(data)));
    }

    [Fact]
    public void TheFrameOfTwoRadars_IsDrawnOverBothPolygons()
    {
        var theme = Theme.MatplotlibV2;
        var svg = TwoRadars(theme);
        var polygons = svg.Each("polygon").ToList();

        var frame = Assert.Single(Rings(svg, theme.ForegroundText.ToHex()));

        Assert.All(DataPolygons(svg, theme), data => Assert.True(polygons.IndexOf(data) < polygons.IndexOf(frame)));
    }

    [Fact]
    public void TheLibrarysOwnTheme_KeepsEachRadarOnItsOwnLargestValue()
    {
        var svg = TwoRadars(Theme.Default);
        var spokes = svg.Each("line").ToList();
        double cy = spokes[0].Number("y1");
        double radius = cy - spokes[0].Number("y2");

        var data = DataPolygons(svg, Theme.Default);

        Assert.Equal(radius * 1 / 3.0, TopReach(data[0], cy), 0.011);
        Assert.Equal(radius * 10 / 30.0, TopReach(data[1], cy), 0.011);
    }

    [Fact]
    public void TheLibrarysOwnTheme_KeepsTheRingsAtFifths_Unlabelled()
    {
        var svg = Svg(Theme.Default);
        var (_, cy, radius) = Web(svg);

        var rings = Radii(Rings(svg, Colors.GridGray.ToHex()), cy);

        Assert.Equal(5, rings.Length);
        for (int i = 0; i < 5; i++) Assert.Equal(radius * (i + 1) / 5.0, rings[i], 0.011);
        Assert.DoesNotContain(svg.Each("text"), t => t.Value is "2" or "4" or "6" or "8");
    }
}
