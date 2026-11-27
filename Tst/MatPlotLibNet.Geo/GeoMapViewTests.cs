// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.Data;
using MatPlotLibNet.Geo.GeoJson;
using MatPlotLibNet.Geo.Projections;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Geo.Tests;

/// <summary>A map is drawn through the limits of its axes, as every other series is: set them narrower (a zoom) or move them (a pan)
/// and the land, the coastlines and the ocean move with them, so a mark placed on the map by another series stays on the place it
/// marks. The map used to fill the plot from the edges of its projection whatever the limits were: a point at 10 east moved with
/// the limits and the country it stands in did not (measured with the limits set to 60 to 100 east and 5 to 35 north on a PlateCarree
/// map: the point at an Indian coastline vertex moved to the plot's left side and the coastline stayed where it was).</summary>
public class GeoMapViewTests
{

    /// <summary>Whether a group clipped to the plot holds, as its first mark, an element of the name given (and of the fill, when one is given).</summary>
    private static bool ClipsAsFirst(string svg, string element, string? fill = null) =>
        svg.Each("g").Any(group => group.Attribute("clip-path") is not null && group.Elements().FirstOrDefault() is { } first && first.Is(element)
            && (fill is null || (string?)first.Attribute("fill") == fill));

    /// <summary>A vertex of the Indian outline in the window 70 to 88 east and 10 to 30 north, where a zoom to 60-100 east and 5-35
    /// north shows it.</summary>
    private static GeoPosition IndianVertex() =>
        NaturalEarth110m.Countries().Single(c => c.Properties["ADM0_A3"] == "IND").Geometry.Polygons
            .SelectMany(p => p.Exterior).First(p => p.Lon is > 70 and < 88 && p.Lat is > 10 and < 30);

    private static string Map(IGeoProjection projection, Action<AxesBuilder> limits, Theme? theme = null)
    {
        var vertex = IndianVertex();
        return Plt.Create().WithSize(600, 400).WithTheme(theme ?? Theme.Default).AddSubPlot(1, 1, 1, ax =>
        {
            ax.WithProjection(projection).Land(Color.FromHex("#90EE90")).Points([vertex.Lat], [vertex.Lon], s => s.Color = Colors.Red);
            limits(ax);
        }).Build().ToSvg();
    }

    /// <summary>How far the nearest vertex of the land is from the centre of the one red mark, in pixels.</summary>
    private static double LandVertexFromTheMark(string svg)
    {
        var mark = Assert.Single(MapSvg.Circles(svg), c => c.R < 10);
        return svg.Elements("path").Where(path => path["fill"] == "#90EE90" && path["d"] is not null).SelectMany(path => path.PathPoints())
            .Min(point => double.Hypot(point.X - mark.X, point.Y - mark.Y));
    }

    [Fact]
    public void AtTheLimitsOfTheMap_AVertexOfTheLand_IsWhereAPointAtItsPositionIsDrawn() =>
        Assert.True(LandVertexFromTheMark(Map(GeoProjection.PlateCarree, _ => { })) < 0.02);

    [Fact]
    public void WhenTheLimitsAreSetNarrower_TheLandIsZoomedWithThem()
    {
        string svg = Map(GeoProjection.PlateCarree, ax => { ax.SetXLim(60, 100); ax.SetYLim(5, 35); });

        Assert.True(LandVertexFromTheMark(svg) < 0.02, $"the land vertex is {LandVertexFromTheMark(svg)} px from the point placed at it");
    }

    [Fact]
    public void WhenTheLimitsAreMoved_TheLandIsPannedWithThem()
    {
        string svg = Map(GeoProjection.PlateCarree, ax => { ax.SetXLim(40, 130); ax.SetYLim(-5, 40); });

        Assert.True(LandVertexFromTheMark(svg) < 0.02, $"the land vertex is {LandVertexFromTheMark(svg)} px from the point placed at it");
    }

    [Fact]
    public void WhenTheLimitsAreSetNarrower_AGlobeIsZoomedWithThem()
    {
        // an orthographic map is in degrees of arc: the vertex (about 77 east, 22 north) stands at x = cos(lat) sin(dlon), in 57 degrees
        var globe = GeoProjection.OrthographicAt(20, 70);
        string svg = Map(globe, ax => { ax.SetXLim(-15, 25); ax.SetYLim(-15, 25); });

        Assert.True(LandVertexFromTheMark(svg) < 0.02, $"the land vertex is {LandVertexFromTheMark(svg)} px from the point placed at it");
    }

    [Fact]
    public void ALandThatTheLimitsCutOff_IsCutByThePlot_NotDrawnOutsideIt()
    {
        string svg = Map(GeoProjection.PlateCarree, ax => { ax.SetXLim(60, 100); ax.SetYLim(5, 35); });

        // the land is drawn inside the series' clip: a group clipped to the plot holds it
        Assert.True(ClipsAsFirst(svg, "path", "#90EE90"));
    }

    [Fact]
    public void AMapThatTheLimitsHaveZoomedInto_HasItsBackgroundAndItsEdgeCutByThePlot()
    {
        // under a theme that frames a map as cartopy does its background is the area inside the map's outline, and the outline is
        // its spine: zoomed into the middle of a globe both are far larger than the plot and are cut by it
        var globe = GeoProjection.OrthographicAt(0, 0);
        string svg = Plt.Create().WithSize(600, 600).WithTheme(Theme.MatplotlibV2).AddSubPlot(1, 1, 1, ax =>
        {
            ax.WithProjection(globe).Ocean(globe, Color.FromHex("#87CEEB"));
            ax.SetXLim(-10, 10);
            ax.SetYLim(-10, 10);
        }).Build().ToSvg();

        Assert.True(ClipsAsFirst(svg, "polygon", "#FFFFFF"));
        Assert.True(ClipsAsFirst(svg, "polygon", "none"));
    }

    [Fact]
    public void AMapAtItsOwnLimits_NeedsNoClipForItsBackgroundOrItsEdge()
    {
        var globe = GeoProjection.OrthographicAt(0, 0);
        string svg = Plt.Create().WithSize(600, 600).WithTheme(Theme.MatplotlibV2).AddSubPlot(1, 1, 1, ax =>
            ax.WithProjection(globe).Ocean(globe, Color.FromHex("#87CEEB"))).Build().ToSvg();

        Assert.False(ClipsAsFirst(svg, "polygon"));
    }
}
