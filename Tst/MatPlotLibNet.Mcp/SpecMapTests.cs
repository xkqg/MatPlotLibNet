// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo;
using MatPlotLibNet.Geo.Projections;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;
using static MatPlotLibNet.Mcp.Tests.SpecFixtures;

namespace MatPlotLibNet.Mcp.Tests;

/// <summary>A map is a chart type of a spec: the server carries the Geo package, so an agent writes a <c>geo_polygon</c> series whose
/// package data names a projection and the features to draw (a built-in set or a GeoJSON FeatureCollection), and the reader takes
/// it as the figure JSON of a map is written. Package data that describes no map is refused with its place, never drawn as a blank
/// picture.</summary>
[Collection("ChartServicesGlobalState")]
public class SpecMapTests
{

    /// <summary>A spec of one subplot holding one map series whose package data is <paramref name="packageData"/>.</summary>
    private static ChartSpec Map(string packageData) => new($$"""
        {"width":640,"height":480,"subPlots":[{"series":[{"type":"geo_polygon","color":"tan","packageData":{{packageData}}}]}]}
        """);

    [Fact]
    public void AMapWrittenByHand_WithABuiltInSource_IsReadAsAMap()
    {
        var figure = Reader.Read(Map("""{"projection":{"name":"Robinson"},"source":"ne_110m_admin_0_countries"}"""));

        var series = Assert.Single(figure.SubPlots[0].Series);
        var dto = series.ToSeriesDto();
        Assert.Equal("geo_polygon", dto.Type);
        Assert.Equal("Robinson", dto.PackageData!.Value.GetProperty("projection").GetProperty("name").GetString());
        Assert.Equal("ne_110m_admin_0_countries", dto.PackageData!.Value.GetProperty("source").GetString());
    }

    /// <summary>The JSON a map figure writes is a spec the reader takes, and draws the same picture: the four layers on a Robinson
    /// map, and a choropleth with its colour bar on a parametric projection.</summary>
    [Theory]
    [InlineData("layers")]
    [InlineData("choropleth")]
    public void TheJsonAMapFigureWrites_IsASpec_ThatDrawsTheSamePicture(string which)
    {
        var figure = Plt.Create().WithSize(640, 360).AddSubPlot(1, 1, 1, ax =>
        {
            if (which == "layers")
            {
                ax.WithProjection(GeoProjection.Robinson)
                    .Ocean(Colors.Blue).Land(Colors.Green).Coastlines(Colors.White).Borders(Colors.Gray);
            }
            else
            {
                ax.WithProjection(GeoProjection.OrthographicAt(50, 10))
                    .Choropleth(new Dictionary<string, double> { ["FRA"] = 3, ["DEU"] = 7, ["NOR"] = 1 }, ColorMaps.Plasma)
                    .WithColorBar();
            }
        }).Build();

        var read = Reader.Read(new ChartSpec(figure.ToJson()));

        Assert.Equal(figure.ToSvg(), read.ToSvg());
    }

    /// <summary>A map written by hand names no aspect, and keeps its projection's shape: the Robinson world is 0.507 as high as it
    /// is wide, centred in its cell (cartopy's <c>GeoAxes</c>). Measured before the change: the map filled the cell, 0.70 as high.</summary>
    [Fact]
    public void AMapWrittenByHand_KeepsItsProjectionsShape()
    {
        var figure = Reader.Read(Map("""{"projection":{"name":"Robinson"},"source":"ne_110m_admin_0_countries"}"""));

        var plot = figure.FirstPlotArea();
        var bounds = GeoProjection.Robinson.Bounds;

        Assert.Equal((bounds.YMax - bounds.YMin) / (bounds.XMax - bounds.XMin), plot.Height / plot.Width, 9);
    }

    /// <summary>The tool an agent calls draws the map: render_chart returns a PNG of it.</summary>
    [Fact]
    public void RenderChart_DrawsAMap_AsAPng()
    {
        var rendering = new ChartRendering(Reader, new ChartSummarizer());

        var result = rendering.Render(Map("""{"projection":{"name":"EqualEarth"},"source":"ne_110m_admin_0_countries"}"""), ChartFormat.Png);

        Assert.Equal(0x89, result.Bytes[0]);
        Assert.Equal("geo_polygon", Assert.Single(result.Summary.Series).Discriminator);
    }

    [Theory]
    [InlineData("""{"projection":{"name":"Robinsn"},"source":"ne_110m_admin_0_countries"}""")]
    [InlineData("""{"projection":{"name":"Robinson"},"source":"ne_50m_admin_0_countries"}""")]
    [InlineData("""{"projection":{"name":"Orthographic","parameters":{"lat0":10}},"source":"ne_110m_coastline"}""")]
    [InlineData("""{"source":"ne_110m_coastline"}""")]
    public void PackageDataThatDescribesNoMap_IsRefusedWithItsPlace_AndWhatAMapHolds(string packageData)
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Reader.Read(Map(packageData)));

        Assert.Contains("$.subPlots[0].series[0]", refusal.Message);
        Assert.Contains("packageData", refusal.Message);
        Assert.Contains("Robinson", refusal.Message);
        Assert.Contains("ne_110m_coastline", refusal.Message);
    }

    /// <summary>The refusal says why the package data describes no map, in the package's words, before what a map holds.</summary>
    [Theory]
    [InlineData("""{"projection":{"name":"Robinsn"},"source":"ne_110m_admin_0_countries"}""", "its projection 'Robinsn' with no numbers is not one this package makes")]
    [InlineData("""{"projection":{"name":"Robinson"},"source":"ne_50m_admin_0_countries"}""", "its source 'ne_50m_admin_0_countries' is not a built-in set of this package")]
    public void ARefusedMap_SaysWhyItsDataDescribesNoMap(string packageData, string why)
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Reader.Read(Map(packageData)));

        Assert.Contains($"describes nothing the library can draw: {why}.", refusal.Message);
    }

    /// <summary>The package data is the package's own: a property of a feature is data, so a word under a <c>color</c> key of a
    /// feature's properties is kept as written and not read as a colour of the spec.</summary>
    [Fact]
    public void AFeaturePropertyNamedColor_IsKeptAsWritten_NotReadAsAColourOfTheSpec()
    {
        const string square = """
            {"projection":{"name":"PlateCarree"},"features":{"type":"FeatureCollection","features":[
              {"type":"Feature","properties":{"name":"A","color":"the colour of the flag"},
               "geometry":{"type":"Polygon","coordinates":[[[0,0],[10,0],[10,10],[0,10],[0,0]]]}}]}}
            """;

        var figure = Reader.Read(Map(square));

        var features = figure.SubPlots[0].Series[0].ToSeriesDto().PackageData!.Value.GetProperty("features").GetProperty("features");
        Assert.Equal("the colour of the flag", features[0].GetProperty("properties").GetProperty("color").GetString());
    }
}
