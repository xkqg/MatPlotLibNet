// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;
using MatPlotLibNet.Tests.Packaging;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A series that colours its groups or its clusters or its assets and names no palette takes the colours of its theme's cycle,
/// as matplotlib's <c>C0</c>, <c>C1</c>... do (scipy's dendrogram names its clusters that way), not the ten colours of the standard
/// cycle under every theme. The theme's cycle is one colour map, <c>Theme.CycleMap</c>; a palette or a
/// colour map the series was given wins over it, and under the themes whose cycle is the standard one nothing moves.</summary>
public class QualitativeFallbackFollowsTheCycleTests
{
    private static TreeNode ThreeClusters => new()
    {
        Label = "root", Value = 3.0,
        Children =
        [
            new TreeNode { Label = "left", Value = 1.0, Children = [new TreeNode { Label = "A" }, new TreeNode { Label = "B" }] },
            new TreeNode
            {
                Label = "right", Value = 2.0,
                Children =
                [
                    new TreeNode { Label = "mid", Value = 1.0, Children = [new TreeNode { Label = "C" }, new TreeNode { Label = "D" }] },
                    new TreeNode { Label = "E" },
                ],
            },
        ],
    };

    private static string DendrogramSvg(Theme theme, IColorMap? given = null) =>
        Plt.Create().WithTheme(theme)
            .AddSubPlot(1, 1, 1, ax => ax.Dendrogram(ThreeClusters, s => { s.CutHeight = 1.5; s.ColorByCluster = true; s.ColorMap = given; }))
            .ToSvg();

    private static string RotationSvg(Theme theme, IColorMap? given = null)
    {
        double[] rising = [.. Enumerable.Range(0, 60).Select(i => 100.0 + i)];
        double[] flat = [.. Enumerable.Repeat(95.0, 60)];
        double[] bench = [.. Enumerable.Repeat(100.0, 60)];
        return Plt.Create().WithTheme(theme)
            .AddSubPlot(1, 1, 1, ax => ax.RelativeRotation([rising, flat], bench, ["ETH", "BNB"], s => s.ColorMap = given))
            .ToSvg();
    }

    private static bool Has(string svg, Color colour) => svg.Contains(colour.ToHex(), StringComparison.OrdinalIgnoreCase);

    [Theory]
    [MemberData(nameof(ThemeCases.Eight), MemberType = typeof(ThemeCases))]
    public void TheThemesCycleMap_ListsTheColoursItHandsOut(string name)
    {
        var theme = name.AsTheme();

        var map = theme.CycleMap;

        Assert.Equal(theme.CycleColors.Length, map.ColorCount);
        for (int i = 0; i < theme.CycleColors.Length; i++)
        {
            Assert.Equal(theme.SeriesSlot(i).CycleColor, map.GetColor((i + 0.5) / theme.CycleColors.Length));
        }
    }

    [Fact]
    public void TheCycleMapOfATheme_WithAPropCycler_ListsTheColoursOfTheCycler()
    {
        var cycler = new PropCyclerBuilder().WithColors(Colors.Red, Colors.Green, Colors.Blue).Build();
        var theme = Theme.CreateFrom(Theme.Default).WithPropCycler(cycler).Build();

        var map = theme.CycleMap;

        Assert.Equal(3, map.ColorCount);
        Assert.Equal(Colors.Green, map.GetColor(0.5));
    }

    [Theory]
    [MemberData(nameof(ThemeCases.Eight), MemberType = typeof(ThemeCases))]
    public void TheClustersOfADendrogramGivenNoColourMap_WearTheThemesCycle(string name)
    {
        var theme = name.AsTheme();
        var cycle = theme.CycleMap;
        var expected = Enumerable.Range(0, 3).Select(k => cycle.GetColor(k.ColormapFraction(3, 0.0))).ToArray();

        var svg = DendrogramSvg(theme);

        Assert.All(expected, colour => Assert.True(Has(svg, colour), $"{name}: cluster colour {colour.ToHex()} is not drawn"));
    }

    [Fact]
    public void TheClustersOfADendrogram_WearTheStandardCycle_UnderTheStandardThemes()
    {
        var expected = Enumerable.Range(0, 3).Select(k => QualitativeColorMaps.Tab10.GetColor(k.ColormapFraction(3, 0.0)));

        Assert.All(expected, colour => Assert.True(Has(DendrogramSvg(Theme.Default), colour)));
        Assert.All(expected, colour => Assert.True(Has(DendrogramSvg(Theme.MatplotlibV2), colour)));
    }

    [Theory]
    [MemberData(nameof(ThemeCases.Eight), MemberType = typeof(ThemeCases))]
    public void ADendrogramGivenAColourMap_WearsItUnderEveryTheme(string name)
    {
        var svg = DendrogramSvg(name.AsTheme(), QualitativeColorMaps.Set1);

        Assert.True(Has(svg, QualitativeColorMaps.Set1.GetColor(0.0)));
        Assert.True(Has(svg, QualitativeColorMaps.Set1.GetColor(1.0)));
    }

    [Theory]
    [MemberData(nameof(ThemeCases.Eight), MemberType = typeof(ThemeCases))]
    public void TheAssetsOfARelativeRotationGivenNoColourMap_WearTheThemesCycle(string name)
    {
        var theme = name.AsTheme();
        var cycle = theme.CycleMap;

        var svg = RotationSvg(theme);

        Assert.True(Has(svg, cycle.GetColor(0.0)), $"{name}: first asset");
        Assert.True(Has(svg, cycle.GetColor(1.0)), $"{name}: last asset");
    }

    [Theory]
    [MemberData(nameof(ThemeCases.Eight), MemberType = typeof(ThemeCases))]
    public void ARelativeRotationGivenAColourMap_WearsItUnderEveryTheme(string name)
    {
        var svg = RotationSvg(name.AsTheme(), QualitativeColorMaps.Set1);

        Assert.True(Has(svg, QualitativeColorMaps.Set1.GetColor(0.0)));
        Assert.True(Has(svg, QualitativeColorMaps.Set1.GetColor(1.0)));
    }

    [Theory]
    [MemberData(nameof(ThemeCases.Eight), MemberType = typeof(ThemeCases))]
    public void TheHueGroupsOfAPairGridGivenNoPalette_WearTheThemesCycle_WrappingOverItsLength(string name)
    {
        var theme = name.AsTheme();
        var series = new PairGridSeries([[1.0]]);
        int n = theme.CycleColors.Length;

        for (int group = 0; group < n; group++)
        {
            Assert.Equal(theme.SeriesSlot(group).CycleColor, series.HueColorOf(group, theme));
        }

        Assert.Equal(series.HueColorOf(3, theme), series.HueColorOf(3 + n, theme));
        Assert.Equal(series.HueColorOf(n - 1, theme), series.HueColorOf(-1, theme));
    }

    [Fact]
    public void TheHueGroupsOfAPairGrid_GivenAPalette_WearThePalette_UnderAnyTheme()
    {
        var series = new PairGridSeries([[1.0]]) { HuePalette = [Colors.Red, Colors.Green] };

        Assert.Equal(Colors.Green, series.HueColorOf(1, Theme.ColorBlindSafe));
        Assert.Equal(Colors.Red, series.HueColorOf(2, Theme.MatplotlibClassic));
    }

    [Theory]
    [MemberData(nameof(ThemeCases.Eight), MemberType = typeof(ThemeCases))]
    public void APairGridDrawnInHueGroups_WearsTheThemesCycle(string name)
    {
        var theme = name.AsTheme();
        var svg = Plt.Create().WithTheme(theme)
            .AddSubPlot(1, 1, 1, ax => ax.PairGrid([[1.0, 2, 3, 4], [4.0, 3, 2, 1]], s => s.HueGroups = [0, 1, 0, 1]))
            .ToSvg();

        Assert.True(Has(svg, theme.SeriesSlot(0).CycleColor), $"{name}: group 0");
        Assert.True(Has(svg, theme.SeriesSlot(1).CycleColor), $"{name}: group 1");
    }

    [Fact]
    public void NoRenderer_NamesTheStandardCycleAsTheColourOfAPartOrAGroup()
    {
        var spellings = RepositoryTree.SourceFiles()
            .Where(file => file.Path.StartsWith("Src/MatPlotLibNet/Rendering/", StringComparison.Ordinal))
            .SelectMany(file => file.CodeLines.Select((line, i) => (file.Path, Line: i + 1, Text: line)))
            .Where(l => Regex.IsMatch(l.Text, @"QualitativeColorMaps\.Tab10\b|\bColors\.Tab10\w+|Tab10Cycle\("))
            .Select(l => $"{l.Path}:{l.Line}: {l.Text.Trim()}");

        Assert.Empty(spellings);
    }
}
