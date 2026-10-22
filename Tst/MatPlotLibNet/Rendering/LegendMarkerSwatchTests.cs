// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Xml.Linq;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A legend swatch draws the marker its series has. The marker drawing of the legend treated every marker that was
/// not a square as a circle, so a line, a step or a scatter with a triangle, a star or a cross showed a circle, and a line
/// whose marker is explicitly <see cref="MarkerStyle.None"/> still showed one. The swatch now draws through the one owner of
/// marker shapes (<c>MarkerRenderer</c>, which the plot itself draws with), so it shows the shape the plot shows, and
/// nothing when the series has no marker.</summary>
public class LegendMarkerSwatchTests
{
    private static readonly double[] X = [0.0, 1.0, 2.0, 3.0];
    private static readonly double[] Y = [1.0, 2.0, 1.5, 3.0];
    private static readonly string Red = Colors.Red.ToHex();

    public static TheoryData<string, MarkerStyle> EveryKindAndMarker()
    {
        var data = new TheoryData<string, MarkerStyle>();
        foreach (var kind in new[] { "line", "step", "scatter" })
            foreach (var marker in Enum.GetValues<MarkerStyle>())
                data.Add(kind, marker);
        return data;
    }

    private static XElement LegendOf(string kind, MarkerStyle marker)
    {
        Action<AxesBuilder> build = kind switch
        {
            "line" => ax => ax.Plot(X, Y, s => { s.Label = "series"; s.Color = Colors.Red; s.Marker = marker; }).WithLegend(),
            "step" => ax => ax.Step(X, Y, s => { s.Label = "series"; s.Color = Colors.Red; s.Marker = marker; }).WithLegend(),
            "scatter" => ax => ax.Scatter(X, Y, s => { s.Label = "series"; s.Color = Colors.Red; s.Marker = marker; }).WithLegend(),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Not a series with a marker."),
        };
        var doc = XDocument.Parse(Plt.Create().WithSize(500, 350).AddSubPlot(1, 1, 1, build).ToSvg());
        return Assert.Single(doc.Descendants(), g => g.Name.LocalName == "g" && (string?)g.Attribute("class") == "legend");
    }

    private static List<XElement> RedElements(XElement legend, string element, string attribute) =>
        legend.Descendants().Where(e => e.Name.LocalName == element && (string?)e.Attribute(attribute) == Red).ToList();

    private static (double X, double Y)[] Vertices(XElement polygon) =>
        ((string)polygon.Attribute("points")!).Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Split(','))
            .Select(p => (double.Parse(p[0], CultureInfo.InvariantCulture), double.Parse(p[1], CultureInfo.InvariantCulture)))
            .ToArray();

    /// <summary>How many sides a polygon marker has; the others are drawn as a circle, a square or two lines.</summary>
    private static int Corners(MarkerStyle marker) => marker switch
    {
        MarkerStyle.Triangle or MarkerStyle.TriangleDown or MarkerStyle.TriangleLeft or MarkerStyle.TriangleRight => 3,
        MarkerStyle.Diamond => 4,
        MarkerStyle.Pentagon => 5,
        MarkerStyle.Hexagon => 6,
        MarkerStyle.Star => 10,
        _ => 0,
    };

    [Theory]
    [MemberData(nameof(EveryKindAndMarker))]
    public void ALegendSwatch_DrawsTheMarkerTheSeriesHas_AndNothingElse(string kind, MarkerStyle marker)
    {
        var legend = LegendOf(kind, marker);

        // a line and a step keep their stroke in the swatch; a scatter is only its marker
        int swatchLine = kind == "scatter" ? 0 : 1;
        int circles = marker == MarkerStyle.Circle ? 1 : 0;
        int squares = marker == MarkerStyle.Square ? 1 : 0;
        int polygons = Corners(marker) > 0 ? 1 : 0;
        int crossLines = marker is MarkerStyle.Cross or MarkerStyle.Plus ? 2 : 0;

        Assert.Equal(circles, RedElements(legend, "circle", "fill").Count);
        Assert.Equal(squares, RedElements(legend, "rect", "fill").Count);
        Assert.Equal(polygons, RedElements(legend, "polygon", "fill").Count);
        Assert.Equal(swatchLine + crossLines, RedElements(legend, "line", "stroke").Count);
        if (polygons == 1)
            Assert.Equal(Corners(marker), Vertices(Assert.Single(RedElements(legend, "polygon", "fill"))).Length);
    }

    /// <summary>The four triangles point four ways; the apex is the one vertex alone on its side of the others.</summary>
    [Theory]
    [InlineData(MarkerStyle.Triangle, false, true)]
    [InlineData(MarkerStyle.TriangleDown, false, false)]
    [InlineData(MarkerStyle.TriangleLeft, true, true)]
    [InlineData(MarkerStyle.TriangleRight, true, false)]
    public void ATriangleSwatch_PointsTheWayItsMarkerDoes(MarkerStyle marker, bool alongX, bool apexAtTheLowSide)
    {
        var vertices = Vertices(Assert.Single(RedElements(LegendOf("line", marker), "polygon", "fill")));

        var coordinate = vertices.Select(v => alongX ? v.X : v.Y).ToArray();
        double mean = coordinate.Average();
        int low = coordinate.Count(c => c < mean);
        Assert.Equal(apexAtTheLowSide ? 1 : 2, low);
    }

    [Fact]
    public void ALineThatNamesNoMarkerAtAll_DrawsOnlyItsSwatchLine()
    {
        var doc = XDocument.Parse(Plt.Create().WithSize(500, 350)
            .AddSubPlot(1, 1, 1, ax => ax.Plot(X, Y, s => { s.Label = "series"; s.Color = Colors.Red; }).WithLegend()).ToSvg());
        var legend = Assert.Single(doc.Descendants(), g => g.Name.LocalName == "g" && (string?)g.Attribute("class") == "legend");

        Assert.Empty(RedElements(legend, "circle", "fill"));
        Assert.Empty(RedElements(legend, "polygon", "fill"));
        Assert.Single(RedElements(legend, "line", "stroke"));
    }

    /// <summary>An explicit <see cref="MarkerStyle.None"/> beats a cycler's marker, in the plot and in the legend.</summary>
    [Fact]
    public void ALineWhoseMarkerIsExplicitlyNone_DrawsNoMarkerInTheLegend_EvenUnderACyclerThatListsOne()
    {
        var withMarkers = Theme.CreateFrom(Theme.Default).WithPropCycler(new PropCyclerBuilder()
            .WithColors(Colors.Red).WithMarkerStyles(MarkerStyle.Square).Build()).Build();

        var doc = XDocument.Parse(Plt.Create().WithTheme(withMarkers).WithSize(500, 350).AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, Y, s => { s.Label = "marked"; }).Plot(X, Y, s => { s.Label = "bare"; s.Color = Colors.Blue; s.Marker = MarkerStyle.None; })
            .WithLegend()).ToSvg());
        var legend = Assert.Single(doc.Descendants(), g => g.Name.LocalName == "g" && (string?)g.Attribute("class") == "legend");

        // the first series takes the cycler's square, the second names none and gets none
        Assert.Single(RedElements(legend, "rect", "fill"));
        Assert.DoesNotContain(legend.Descendants(), e => (string?)e.Attribute("fill") == Colors.Blue.ToHex());
    }
}
