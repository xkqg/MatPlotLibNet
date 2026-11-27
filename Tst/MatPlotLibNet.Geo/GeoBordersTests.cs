// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.Data;
using MatPlotLibNet.Geo.Projections;
using MatPlotLibNet.Geo.Series;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Geo.Tests;

/// <summary>Borders are the boundaries between countries on land, as cartopy's <c>cfeature.BORDERS</c> is: Natural Earth's
/// <c>admin_0_boundary_lines_land</c> at 110m (cartopy 0.26.0: category 'cultural', scale '110m', 331 records, 329 LineString and
/// 2 MultiLineString). A coast is the coastlines' to draw: measured before the change, <c>Borders</c> outlined every country, coasts
/// included, so the grey of the borders lay over the white of the coastlines all round every continent.</summary>
[Collection("ChartSerializerGlobalState")]
public class GeoBordersTests
{
    private static GeoPolygonSeries Borders() =>
        (GeoPolygonSeries)Plt.Create().AddSubPlot(1, 1, 1, ax => ax.WithProjection(GeoProjection.PlateCarree).Borders())
            .Build().SubPlots[0].Series.Single();

    [Fact]
    public void TheBuiltInBoundaryLines_AreNaturalEarthsLandBoundaries()
    {
        var lines = NaturalEarth110m.BoundaryLines();

        Assert.Equal(331, lines.Count);
        Assert.All(lines, line => Assert.False(line.Geometry.Kind.IsArea));
        Assert.Same(lines, NaturalEarth110m.BoundaryLines());
    }

    [Fact]
    public void Borders_DrawTheLandBoundaries()
    {
        Assert.Same(NaturalEarth110m.BoundaryLines(), Borders().Features);
    }

    /// <summary>Australia has no border on land, so no border runs along its coast.</summary>
    [Fact]
    public void NoBorder_RunsAlongACoast()
    {
        var positions = Borders().Features.SelectMany(f => f.Geometry.PositionRings).SelectMany(ring => ring);

        Assert.DoesNotContain(positions, p => p.Lon is > 112 and < 155 && p.Lat is > -45 and < -10);
    }

    [Fact]
    public void TheBorders_AreWrittenAsTheirSourceId_AndComeBackAsTheSamePicture()
    {
        var figure = Plt.Create().WithSize(600, 330).WithTheme(Theme.MatplotlibV2)
            .AddSubPlot(1, 1, 1, ax => ax.WithProjection(GeoProjection.Robinson).Borders(Colors.Gray)).Build();

        string json = figure.ToJson();

        Assert.Contains("\"source\":\"ne_110m_admin_0_boundary_lines_land\"", json);
        Assert.Equal(figure.ToSvg(), new ChartSerializer().FromJson(json).ToSvg());
    }
}
