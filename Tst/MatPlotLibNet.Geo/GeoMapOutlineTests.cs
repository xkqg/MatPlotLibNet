// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.Projections;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Geo.Tests;

/// <summary>A map has the outline of its projection, as cartopy's: the ocean fills the area inside it, not the rectangle of the
/// bounds, and under the matplotlib themes the axes' background is that area and its edge is drawn as the map's spine
/// (cartopy 0.26.0 on matplotlib 3.11.2, printed: <c>ax.spines['geo']</c> linewidth 0.8 under the default style and 1.0 under
/// classic, edge colour black, zorder 2.5; <c>ax.patch</c> the outline, white, the figure's colour outside it). The outline of a
/// pseudo-cylindrical projection is its two outer meridians from pole to pole, 91 points each and the first again
/// (<c>_WarpedRectangularProjection</c>); of an orthographic or azimuthal map a circle (<c>_ellipse_boundary</c>, 61 points, 91
/// for a stereographic map); of every other projection the rectangle of its bounds.</summary>
[Collection("ChartSerializerGlobalState")]
public class GeoMapOutlineTests
{
    private static Figure Map(Theme theme, IGeoProjection projection, Color? ocean = null) =>
        Plt.Create().WithSize(800, 600).WithTheme(theme)
            .AddSubPlot(1, 1, 1, ax => ax.WithProjection(projection).Ocean(ocean ?? Color.FromHex("#1A3A5C")).WithLegend(visible: false))
            .Build();

    /// <summary>The vertices of the ocean: the path filled in its colour.</summary>
    private static (double X, double Y)[] Ocean(string svg) =>
        [.. svg.Elements("path").First(path => path["d"] is not null && path["fill"] == "#1A3A5C").PathPoints().Select(point => (point.X, point.Y))];

    /// <summary>The polygons of the picture that are the outline of the map: the ones of 183 vertices.</summary>
    private static SvgElement[] Outlines(string svg) =>
        [.. svg.Elements("polygon").Where(polygon => polygon["points"] is not null && polygon.Points().Length == 183)];

    /// <summary>Measured before the change: the ocean was the rectangle of the bounds, its top edge running the plot's whole width.</summary>
    [Fact]
    public void TheOcean_FillsTheRobinsonOutline_NotTheRectangle()
    {
        var figure = Map(Theme.MatplotlibV2, GeoProjection.Robinson);
        var plot = figure.FirstPlotArea();

        var ocean = Ocean(figure.ToSvg());

        Assert.Equal(183, ocean.Length);
        var top = ocean.Where(p => Math.Abs(p.Y - plot.Y) < 0.01).ToArray();
        // Robinson's pole line is 0.5322 of its equator, so the top edge runs over the middle 53 % of the plot.
        Assert.Equal(plot.X + plot.Width * (1 - 0.5322) / 2, top.Min(p => p.X), 1);
        Assert.Equal(plot.X, ocean.Min(p => p.X), 2);
    }

    /// <summary>cartopy clips every artist of a map to the axes' patch, which is the map's outline (printed: the clip path of a
    /// coastline is <c>ax.patch</c>'s path), so under the matplotlib themes the map is cut off at its outline, not at the rectangle of
    /// the plot: against matplotlib's Robinson pictures RMS v2 10.43 -> 10.42, classic 8.65 -> 8.63, where the rectangle drew v2 10.44.</summary>
    [Fact]
    public void UnderTheMatplotlibThemes_TheMapIsCutOffAtItsOutline()
    {
        string svg = Map(Theme.MatplotlibV2, GeoProjection.Robinson).ToSvg();

        var clip = svg.Each("clipPath").FirstOrDefault(element => element.Children("path").Any());
        Assert.True(clip is not null, "the map is cut off at a rectangle");
        string id = (string)clip!.Attribute("id")!;
        Assert.Equal(183, SvgElement.Of(clip.Children("path").First()).PathPoints().Length);
        int ocean = svg.IndexOf("fill=\"#1A3A5C\"", StringComparison.Ordinal);
        int group = svg.LastIndexOf("<g clip-path=\"url(#" + id + ")\">", ocean, StringComparison.Ordinal);
        Assert.True(group > svg.IndexOf("<clipPath id=\"" + id + "\"", StringComparison.Ordinal) && svg.IndexOf("</g>", group, StringComparison.Ordinal) > ocean, "the ocean is not inside the map's clip");

        Assert.DoesNotContain(Map(Theme.Default, GeoProjection.Robinson).ToSvg().Each("clipPath"), element => element.Children("path").Any());
    }

    [Fact]
    public void TheOcean_OfAGlobe_IsItsCircle()
    {
        var figure = Map(Theme.MatplotlibV2, GeoProjection.Orthographic);
        var plot = figure.FirstPlotArea();

        var ocean = Ocean(figure.ToSvg());

        Assert.Equal(61, ocean.Length);
        foreach (var (x, y) in ocean)
            Assert.Equal(plot.Width / 2, Math.Sqrt(Math.Pow(x - plot.Center.X, 2) + Math.Pow(y - plot.Center.Y, 2)), 1);
    }

    [Theory]
    [InlineData("PlateCarree", 5)]
    [InlineData("Mercator", 5)]
    [InlineData("Mollweide", 183)]
    [InlineData("Sinusoidal", 183)]
    [InlineData("EqualEarth", 183)]
    [InlineData("NaturalEarth", 183)]
    [InlineData("AzimuthalEquidistant", 61)]
    [InlineData("Stereographic", 91)]
    [InlineData("LambertConformal", 5)]
    public void TheOcean_HasTheOutlineOfItsProjection(string name, int vertices)
    {
        var projection = typeof(GeoProjection).GetProperty(name)!.GetValue(null) as IGeoProjection;

        Assert.Equal(vertices, Ocean(Map(Theme.MatplotlibV2, projection!).ToSvg()).Length);
    }

    /// <summary>The outline under the matplotlib themes: the background the axes is painted in is the map's area, and its edge is
    /// drawn over the map in black as wide as the theme's spines. Measured before the change: no outline, a rectangle of
    /// background.</summary>
    [Theory]
    [InlineData("v2", "1.11")]       // 0.8 pt at 100 dpi
    [InlineData("classic", "1.39")]  // 1.0 pt
    public void UnderTheMatplotlibThemes_TheMapHasItsOutlineAsSpine_AndAsBackground(string style, string spineWidth)
    {
        var theme = style.AsTheme();
        string svg = Map(theme, GeoProjection.Robinson).ToSvg();

        var outlines = Outlines(svg);

        Assert.Equal(2, outlines.Length);
        Assert.Equal(theme.AxesBackground.ToHex(), outlines[0]["fill"]);
        Assert.Equal("none", outlines[1]["fill"]);
        Assert.Equal("#000000", outlines[1]["stroke"]);
        Assert.Equal(spineWidth, outlines[1]["stroke-width"]);
        Assert.True(svg.LastIndexOf("points=\"" + outlines[1]["points"] + "\"", StringComparison.Ordinal) > svg.IndexOf("fill=\"#1A3A5C\"", StringComparison.Ordinal),
            "the outline is drawn over the map");
    }

    /// <summary>The library's own themes keep their look: no outline, the rectangle of background.</summary>
    [Fact]
    public void UnderTheLibrarysThemes_TheMapHasNoOutline()
    {
        string svg = Map(Theme.Default, GeoProjection.Robinson).ToSvg();

        Assert.Empty(Outlines(svg));
    }

    /// <summary>The ocean comes back from JSON as it was drawn.</summary>
    [Fact]
    public void TheOcean_ComesBackFromJson_AsTheSamePicture()
    {
        var figure = Map(Theme.MatplotlibV2, GeoProjection.Robinson);

        Assert.Equal(figure.ToSvg(), figure.RoundTripped().ToSvg());
    }

    /// <summary>A projection the package does not know has the rectangle of its bounds as its outline.</summary>
    [Fact]
    public void AProjectionOfYourOwn_HasTheRectangleOfItsBounds()
    {
        Assert.Equal(5, Ocean(Map(Theme.MatplotlibV2, new TestGeoProjection(new GeoBounds(-180, 180, -180, 180))).ToSvg()).Length);
    }
}
