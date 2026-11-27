// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.GeoJson;
using MatPlotLibNet.Geo.Projections;
using MatPlotLibNet.Geo.Series;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering.ColorBarRendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Rendering.ColorBarRendering;

/// <summary>A colour bar is resolved once over the series it reads (<see cref="ResolvedColorBarExtensions.Resolve"/>): the series that
/// backs it, its map, its range, its normaliser, the steps of its gradient and its lines. The axes' bar, the figure's bar and the room
/// the layout keeps for a bar read that one value.</summary>
public class ResolvedColorBarTests
{
    private static readonly double[,] Grid = { { 1, 2 }, { 3, 8 } };

    [Fact]
    public void WithNoSeriesThatProvidesOne_TheBarTakesItsOwnLimitsAndTheThemesMap()
    {
        var bar = new ColorBar { Min = 2, Max = 9 }.Resolve([new LineSeries([0.0, 1], [0.0, 1])], Theme.MatplotlibClassic);

        Assert.Null(bar.Provider);
        Assert.Same(ColorMaps.Jet, bar.Map);
        Assert.Equal(new MinMaxRange(2, 9), bar.Range);
        Assert.Same(LinearNormalizer.Instance, bar.Normalizer);
        Assert.Null(bar.Lines);
    }

    [Fact]
    public void TheFirstSeriesThatProvidesOneWithValues_BacksTheBar()
    {
        var empty = new GeoPolygonSeries(new List<GeoFeature>(), GeoProjection.PlateCarree);
        var heatmap = new HeatmapSeries(Grid) { ColorMap = ColorMaps.Plasma, Normalizer = new LogNormalizer() };

        var bar = new ColorBar().Resolve([empty, heatmap], Theme.Default);

        Assert.Same(heatmap, bar.Provider);
        Assert.Same(ColorMaps.Plasma, bar.Map);
        Assert.Equal(heatmap.ColorBarRangeUnder(Theme.Default), bar.Range);
        Assert.IsType<LogNormalizer>(bar.Normalizer);
    }

    [Fact]
    public void TheBarsOwnMap_WinsOverItsProviders()
    {
        var bar = new ColorBar { ColorMap = ColorMaps.Inferno }.Resolve([new HeatmapSeries(Grid) { ColorMap = ColorMaps.Plasma }], Theme.Default);

        Assert.Same(ColorMaps.Inferno, bar.Map);
    }

    [Theory]
    [InlineData("Default")]
    [InlineData("MatplotlibV2")]
    public void TheSteps_AreTheThemesForTheMap(string themeName)
    {
        var theme = themeName.AsTheme();

        var bar = new ColorBar().Resolve([new HeatmapSeries(Grid)], theme);

        Assert.Equal(ColorBarStrip.StepsFor(bar.Map, theme), bar.Steps);
    }

    [Fact]
    public void AContourOfLines_GivesItsLines()
    {
        var contour = new ContourSeries([0.0, 1, 2], [0.0, 1, 2], new double[,] { { 0, 1, 2 }, { 1, 2, 3 }, { 2, 3, 4 } });

        var bar = new ColorBar().Resolve([contour], Theme.MatplotlibV2);

        Assert.NotNull(bar.Lines);
        Assert.Equal(contour.ColorBarLinesUnder(Theme.MatplotlibV2)!.Value.Levels, bar.Lines!.Value.Levels);
    }
}
