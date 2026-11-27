// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.Projections;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests;

namespace MatPlotLibNet.Geo.Tests;

/// <summary>A map axes keeps the shape of its projection, as cartopy's <c>GeoAxes</c> does: one unit of Y as long as one unit of X
/// (cartopy 0.26.0 on matplotlib 3.11.2, a Robinson map in an 8 x 6 inch figure: <c>get_aspect()</c> 1.0, <c>get_adjustable()</c>
/// 'box', <c>get_anchor()</c> 'C', <c>get_position().bounds</c> (0.125, 0.233, 0.775, 0.524) under the default style), so the plot
/// shrinks to the projection's box, centred in its cell, and the limits are the projection's own (<c>set_global</c>), which no
/// theme rounds out. It holds wherever a map is drawn from: the verbs, the JSON, the layouts.</summary>
[Collection("ChartSerializerGlobalState")]
public class GeoMapAspectTests
{
    private static Figure Map(Theme theme, IGeoProjection projection, Action<AxesBuilder>? more = null, Func<FigureBuilder, FigureBuilder>? figure = null)
    {
        var builder = Plt.Create().WithSize(800, 600).WithTheme(theme)
            .AddSubPlot(1, 1, 1, ax => { ax.WithProjection(projection).Ocean(Colors.Blue); more?.Invoke(ax); });
        return (figure ?? (f => f))(builder).Build();
    }

    private static double BoundsRatio(IGeoProjection projection)
    {
        var b = projection.Bounds;
        return (b.YMax - b.YMin) / (b.XMax - b.XMin);
    }

    /// <summary>Measured before the change: the Robinson map filled the whole cell under v2, 620 by 462 pixels, where the projection
    /// is 0.507 as high as it is wide.</summary>
    [Theory]
    [MemberData(nameof(ThemeCases.MatplotlibAndDefault), MemberType = typeof(ThemeCases))]
    public void TheMap_HasTheShapeOfItsProjection_CentredInItsCell(string themeId)
    {
        var theme = themeId.AsTheme();
        var cell = Plt.Create().WithSize(800, 600).WithTheme(theme).AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1], [0.0, 1])).Build().FirstPlotArea();
        var figure = Map(theme, GeoProjection.Robinson);

        var plot = figure.FirstPlotArea();
        var frame = MapSvg.Frame(figure.ToSvg());

        Assert.Equal(BoundsRatio(GeoProjection.Robinson), plot.Height / plot.Width, 9);
        Assert.Equal(cell.Width, plot.Width, 9);
        Assert.Equal(cell.Y + cell.Height / 2, plot.Y + plot.Height / 2, 9);
        Assert.Equal(plot.Y, frame.Top, 2);
        Assert.Equal(plot.Y + plot.Height, frame.Bottom, 2);
    }

    /// <summary>cartopy's numbers: under the default style the axes stands at 0.125, 0.233, 0.775, 0.524 of an 8 x 6 inch figure.</summary>
    [Fact]
    public void UnderV2_TheRobinsonMap_StandsWhereCartopyPutsIt()
    {
        var plot = Map(Theme.MatplotlibV2, GeoProjection.Robinson).FirstPlotArea();

        Assert.Equal(0.125 * 800, plot.X, 6);
        Assert.Equal(0.775 * 800, plot.Width, 6);
        Assert.Equal((1 - 0.23295286454427272 - 0.5240942709114547) * 600, plot.Y, 1);
        Assert.Equal(0.5240942709114547 * 600, plot.Height, 1);
    }

    /// <summary>A map taller than its cell is as wide as its height allows, centred across.</summary>
    [Fact]
    public void AMapTallerThanItsCell_IsNarrowed_AndCentredAcross()
    {
        var cell = Plt.Create().WithSize(800, 600).WithTheme(Theme.MatplotlibV2).AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1], [0.0, 1])).Build().FirstPlotArea();
        var plot = Map(Theme.MatplotlibV2, GeoProjection.Orthographic).FirstPlotArea();

        Assert.Equal(plot.Width, plot.Height, 9);
        Assert.Equal(cell.Height, plot.Height, 9);
        Assert.Equal(cell.X + cell.Width / 2, plot.X + plot.Width / 2, 9);
    }

    /// <summary>Measured before the change: under the classic theme the axes was rounded out to -200 .. 200 and -100 .. 100, so a
    /// place at 90 E, 45 N stood at (549.5, 192) while the map drew that position at (565, 180).</summary>
    [Theory]
    [MemberData(nameof(ThemeCases.MatplotlibAndDefault), MemberType = typeof(ThemeCases))]
    public void APlaceMarkedOnTheMap_StandsWhereTheMapDrawsItsPosition(string themeId)
    {
        var figure = Map(themeId.AsTheme(), GeoProjection.PlateCarree, ax => ax.Points([45.0], [90.0]));
        string svg = figure.ToSvg();

        var (x, y) = MapSvg.PixelOf(svg, 90, 45);
        var circle = Assert.Single(MapSvg.Circles(svg));

        Assert.Equal(x, circle.X, 1);
        Assert.Equal(y, circle.Y, 1);
    }

    [Fact]
    public void AnAspectGivenToTheAxes_Wins()
    {
        var plot = Map(Theme.MatplotlibV2, GeoProjection.PlateCarree, ax => ax.SetAspect(2)).FirstPlotArea();

        Assert.Equal(2 * BoundsRatio(GeoProjection.PlateCarree), plot.Height / plot.Width, 9);
    }

    /// <summary>The shape is the map's, not a setting the verb writes: a map read from JSON, and one an author writes by hand with no
    /// aspect, keeps it.</summary>
    [Fact]
    public void AMapReadFromJson_KeepsTheShape_AndWritesNoAspect()
    {
        var figure = Map(Theme.MatplotlibV2, GeoProjection.Robinson);

        var read = figure.RoundTripped();

        Assert.DoesNotContain("\"aspect\"", figure.ToJson());
        Assert.Equal(figure.FirstPlotArea(), read.FirstPlotArea());
    }

    /// <summary>The picture of a map keeps its shape under a zoom too: its SVG root carries the lock the zoom script keeps the view
    /// isotropic by, read from the one aspect rule the layout fits the plot by (the map asks one). Measured before the change: the root
    /// read only an aspect given to the axes, so the eight maps the sample program draws had no lock, and a zoom stretched a projection
    /// whose plot the layout had shaped. <c>SetAspectAuto</c>, matplotlib's <c>set_aspect('auto')</c>, lets the map fill its cell and
    /// asks no lock.</summary>
    [Theory]
    [MemberData(nameof(ThemeCases.MatplotlibAndDefault), MemberType = typeof(ThemeCases))]
    public void AMapsPicture_LocksItsZoomToTheMapsShape(string themeId)
    {
        Assert.Contains("data-aspect-lock=\"true\"", Map(themeId.AsTheme(), GeoProjection.Robinson).ToSvg(), StringComparison.Ordinal);
        Assert.DoesNotContain("data-aspect-lock", Map(themeId.AsTheme(), GeoProjection.Robinson, ax => ax.SetAspectAuto()).ToSvg(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("tight")]
    [InlineData("constrained")]
    public void TheLayouts_KeepTheShape(string layout)
    {
        var figure = Map(Theme.MatplotlibV2, GeoProjection.Robinson, figure: f => layout == "tight" ? f.TightLayout() : f.ConstrainedLayout());

        var plot = figure.FirstPlotArea();

        Assert.Equal(BoundsRatio(GeoProjection.Robinson), plot.Height / plot.Width, 9);
    }
}
