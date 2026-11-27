// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.Projections;
using MatPlotLibNet.Geo.Series;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Geo.Tests;

/// <summary>The maps that draw each region by a value (a choropleth, a cartogram, a contiguous cartogram, the cells and the bins of
/// H3) are one kind of map with one look: the outline wears the theme's neutral grey and is 0.3 wide, the label names the verb.
/// What differs between them is a value of the verb: the regions it draws, whether they are coloured by their values or in the one
/// colour the verb chose, whether its coordinates are already projected, and where in the drawing order it stands.</summary>
public class GeoRegionMapTests
{
    private static readonly IReadOnlyDictionary<string, double> Values = new Dictionary<string, double> { ["FRA"] = 1, ["DEU"] = 2, ["ESP"] = 4 };
    private static readonly IReadOnlyDictionary<string, double> Cells = new Dictionary<string, double> { ["8928308280fffff"] = 1 };

    /// <summary>Verb, label, drawn by value, raw projected, z-order.</summary>
    public static TheoryData<string, string, bool, bool, int> Verbs => new()
    {
        { "choropleth", "Choropleth", true, false, -1 },
        { "choropleth by property", "Choropleth", true, false, -1 },
        { "cartogram", "Cartogram", false, true, -1 },
        { "contiguous cartogram", "Contiguous cartogram", false, true, -1 },
        { "h3 cells", "H3 cells", true, false, 0 },
        { "h3 bins", "H3 cells", true, false, 0 },
    };

    private static Figure Map(string verb) => Plt.Create().AddSubPlot(1, 1, 1, ax =>
    {
        ax.WithProjection(GeoProjection.PlateCarree);
        switch (verb)
        {
            case "choropleth": ax.Choropleth(Values); break;
            case "choropleth by property": ax.Choropleth("POP_EST"); break;
            case "cartogram": ax.Cartogram(Values); break;
            case "contiguous cartogram": ax.ContiguousCartogram(Values); break;
            case "h3 cells": ax.H3Cells(Cells); break;
            default: ax.H3Bins([37.7749], [-122.4194], 5); break;
        }
    }).Build();

    private static GeoPolygonSeries Series(string verb) => Assert.Single(Map(verb).SubPlots[0].Series.OfType<GeoPolygonSeries>());

    [Theory]
    [MemberData(nameof(Verbs))]
    public void EveryMapOfRegions_WearsTheNeutralOutline_ThirtyHundredthsWide(string verb, string label, bool byValue, bool projected, int zOrder)
    {
        var series = Series(verb);

        Assert.Null(series.StrokeColor);
        Assert.Contains("\"wearsNeutral\":true", Map(verb).ToJson());
        Assert.Equal(0.3, series.StrokeWidth);
        Assert.Equal(label, series.Label);
        Assert.Equal(byValue, series.Values is not null);
        Assert.Equal(projected, series.IsRawProjected);
        Assert.Equal(zOrder, series.ZOrder);
    }

    [Theory]
    [InlineData("cartogram")]
    [InlineData("contiguous cartogram")]
    public void ACartogram_NamesNoFill_TheVerbChoseTheOneItIsFilledIn(string verb)
    {
        Assert.Null(Series(verb).Color);
        Assert.Contains("\"verbColor\":\"" + Colors.Tab10Blue.ToHex() + "\"", Map(verb).ToJson());
    }

    [Theory]
    [InlineData("choropleth")]
    [InlineData("choropleth by property")]
    [InlineData("h3 cells")]
    [InlineData("h3 bins")]
    public void AMapColouredByValues_NamesNoFill(string verb) =>
        Assert.Null(Series(verb).Color);
}
