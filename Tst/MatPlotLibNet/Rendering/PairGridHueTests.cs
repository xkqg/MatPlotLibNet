// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>How a pair grid reads its hue: whether the groups it was given can colour the samples at all
/// (<c>HasValidHue</c>), and the colour each group takes (<c>HueColors()</c>).</summary>
public class PairGridHueTests
{
    private static PairGridSeries Grid(params double[][] variables) => new(variables);

    // ── HasValidHue ──────────────────────────────────────────────────────────

    [Fact]
    public void HasValidHue_WithoutGroups_IsFalse() =>
        Assert.False(Grid([1.0, 2.0]).HasValidHue);

    [Fact]
    public void HasValidHue_WithAnEmptyGroupArray_IsFalse() =>
        Assert.False(new PairGridSeries([[1.0, 2.0]]) { HueGroups = [] }.HasValidHue);

    [Fact]
    public void HasValidHue_WithOneGroupPerSample_IsTrue() =>
        Assert.True(new PairGridSeries([[1.0, 2.0, 3.0], [4.0, 5.0, 6.0]]) { HueGroups = [0, 1, 0] }.HasValidHue);

    [Fact]
    public void HasValidHue_WhenTheGroupsAreNotAsLongAsTheSamples_IsFalse() =>
        Assert.False(new PairGridSeries([[1.0, 2.0, 3.0]]) { HueGroups = [0, 1] }.HasValidHue);

    // ── HueColors ────────────────────────────────────────────────────────────

    [Fact]
    public void HueColors_HoldsOneColourPerDistinctGroup()
    {
        var series = new PairGridSeries([[1.0, 2.0, 3.0, 4.0]]) { HueGroups = [2, 0, 2, 1] };

        var colors = series.HueColors(Theme.Default);

        Assert.Equal([0, 1, 2], colors.Keys.Order());
    }

    [Fact]
    public void HueColors_WithoutAPalette_TakesTheCycleOfTheTheme()
    {
        var series = new PairGridSeries([[1.0, 2.0]]) { HueGroups = [0, 3] };

        var colors = series.HueColors(Theme.Default);

        Assert.Equal(QualitativeColorMaps.Tab10.GetColor(0.05), colors[0]);
        Assert.Equal(QualitativeColorMaps.Tab10.GetColor(0.35), colors[3]);
    }

    [Fact]
    public void HueColorOf_WithoutAPalette_WrapsAfterTheCycleLength_AndForNegativeGroups()
    {
        var series = Grid([1.0]);

        Assert.Equal(series.HueColorOf(3, Theme.Default), series.HueColorOf(13, Theme.Default));
        Assert.Equal(series.HueColorOf(9, Theme.Default), series.HueColorOf(-1, Theme.Default));
    }

    [Fact]
    public void HueColorOf_WithAPalette_TakesThePaletteInTurn_AndWrapsIt()
    {
        var series = new PairGridSeries([[1.0]]) { HuePalette = [Colors.Red, Colors.Green, Colors.Blue] };

        Assert.Equal(Colors.Red, series.HueColorOf(0, Theme.Default));
        Assert.Equal(Colors.Blue, series.HueColorOf(2, Theme.Default));
        Assert.Equal(Colors.Red, series.HueColorOf(3, Theme.Default));
        Assert.Equal(Colors.Blue, series.HueColorOf(-1, Theme.Default));
    }

    [Fact]
    public void HueColorOf_WithAnEmptyPalette_FallsBackToTheCycleOfTheTheme()
    {
        var series = new PairGridSeries([[1.0]]) { HuePalette = [] };

        Assert.Equal(Grid([1.0]).HueColorOf(4, Theme.Default), series.HueColorOf(4, Theme.Default));
    }

    // ── SamplesByHue ─────────────────────────────────────────────────────────

    [Fact]
    public void SamplesByHue_AreOnePerGroup_SmallestGroupFirst_EachInSampleOrder()
    {
        var series = new PairGridSeries([[1.0, 2, 3, 4, 5], [6.0, 7, 8, 9, 10]]) { HueGroups = [3, -2, 3, 0, -2] };

        var slices = series.SamplesByHue(1);

        Assert.Equal(new[] { -2, 0, 3 }, slices.Select(s => s.Group));
        Assert.Equal(new[] { 7.0, 10 }, slices[0].Data);
        Assert.Equal(new[] { 9.0 }, slices[1].Data);
        Assert.Equal(new[] { 6.0, 8 }, slices[2].Data);
    }
}
