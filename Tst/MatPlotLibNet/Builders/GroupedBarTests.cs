// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>
/// Several bars per category, in one call. The renderer has always placed multiple bar series side by side; what
/// was missing is the entry point that takes the groups, so the caller had to add one series per group and
/// remember that the order of the calls is the order of the bars. matplotlib 3.11 added `grouped_bar()` for the
/// same reason.
/// <para>The groups are an ordered LIST, never a dictionary. A dictionary has no order, and the order here is
/// the picture: it decides which bar sits where in every category, and which colour it takes from the cycle.</para>
/// </summary>
public class GroupedBarTests
{
    private static readonly string[] Quarters = ["Q1", "Q2", "Q3"];

    private static readonly BarGroup[] Regions =
    [
        new("north", [3.0, 4.0, 5.0]),
        new("south", [2.0, 3.0, 4.0]),
        new("east", [1.0, 2.0, 3.0]),
    ];

    /// <summary>The coloured rectangles: the bars themselves. The figure background and the plot area are
    /// rectangles too, and they are painted in the theme's background colour; the legend draws its own swatches
    /// further down the document, so only the part before it is read.</summary>
    private static (double X, double Y, double W, double H, string Fill)[] Bars(string svg)
    {
        int legend = svg.IndexOf("class=\"legend\"", StringComparison.Ordinal);
        string plot = legend < 0 ? svg : svg[..legend];

        return [.. Regex.Matches(plot,
                @"<rect x=""([0-9.eE+-]+)"" y=""([0-9.eE+-]+)"" width=""([0-9.eE+-]+)"" height=""([0-9.eE+-]+)"" fill=""(#[0-9A-Fa-f]{6})""")
            .Select(m => (
                X: double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture),
                Y: double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture),
                W: double.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture),
                H: double.Parse(m.Groups[4].Value, CultureInfo.InvariantCulture),
                Fill: m.Groups[5].Value))
            .Where(r => !string.Equals(r.Fill, "#FFFFFF", StringComparison.OrdinalIgnoreCase))];
    }

    [Fact]
    public void EveryGroup_BecomesItsOwnSeries_InTheOrderItWasGiven()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.GroupedBar(Quarters, Regions))
            .Build();

        var bars = figure.SubPlots[0].Series.OfType<BarSeries>().ToArray();

        Assert.Equal(3, bars.Length);
        Assert.Equal(["north", "south", "east"], bars.Select(b => b.Label));
        Assert.Equal([3.0, 4.0, 5.0], bars[0].Values);
        Assert.Equal(Quarters, bars[0].Categories);
    }

    [Fact]
    public void TheBars_SitSideBySideWithinTheirCategory()
    {
        string svg = Plt.Create().WithSize(600, 400)
            .AddSubPlot(1, 1, 1, ax => ax.GroupedBar(Quarters, Regions))
            .ToSvg();

        var bars = Bars(svg);

        Assert.Equal(9, bars.Length);                          // three groups across three categories
        Assert.Equal(9, bars.Select(b => Math.Round(b.X, 3)).Distinct().Count());
        Assert.Single(bars.Select(b => Math.Round(b.W, 3)).Distinct());   // one width for all of them
    }

    [Fact]
    public void TheCaller_ConfiguresEverySeriesAtOnce()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.GroupedBar(Quarters, Regions, s => s.Alpha = 0.5))
            .Build();

        Assert.All(figure.SubPlots[0].Series.OfType<BarSeries>(), b => Assert.Equal(0.5, b.Alpha));
    }

    [Fact]
    public void OneGroup_IsAPlainBarChart()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.GroupedBar(Quarters, [new BarGroup("north", [3.0, 4.0, 5.0])]))
            .Build();

        var bar = Assert.Single(figure.SubPlots[0].Series.OfType<BarSeries>());
        Assert.Equal("north", bar.Label);
    }

    [Fact]
    public void AGroupThatDoesNotFitTheCategories_IsRefusedByName()
    {
        var wrong = new BarGroup[] { new("north", [3.0, 4.0]) };

        var error = Assert.Throws<ArgumentException>(() =>
            Plt.Create().AddSubPlot(1, 1, 1, ax => ax.GroupedBar(Quarters, wrong)).Build());

        Assert.Contains("north", error.Message, StringComparison.Ordinal);
        Assert.Contains("3", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void NoGroupsAtAll_IsRefused()
    {
        Assert.Throws<ArgumentException>(() =>
            Plt.Create().AddSubPlot(1, 1, 1, ax => ax.GroupedBar(Quarters, [])).Build());
    }

    [Fact]
    public void Horizontal_GroupsToo()
    {
        // Until this, the side-by-side pass skipped horizontal bars entirely: two horizontal series over the
        // same categories were drawn exactly on top of each other, with the last one painted over the rest.
        string svg = Plt.Create().WithSize(600, 400)
            .AddSubPlot(1, 1, 1, ax => ax.GroupedBar(Quarters, Regions,
                s => s.Orientation = BarOrientation.Horizontal))
            .ToSvg();

        var bars = Bars(svg);

        Assert.Equal(9, bars.Length);
        Assert.Equal(9, bars.Select(r => Math.Round(r.Y, 3)).Distinct().Count());
        Assert.Single(bars.Select(r => Math.Round(r.H, 3)).Distinct());   // one height for all of them
    }

    [Fact]
    public void AMixOfOrientations_GroupsEachKindAmongItsOwn()
    {
        // Two verticals and two horizontals in one axes: each pair steps aside for its own kind, and a vertical
        // is never offset against a horizontal, which would be a meaningless comparison.
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Bar(Quarters, [1.0, 2.0, 3.0])
                .Bar(Quarters, [2.0, 3.0, 4.0])
                .Bar(Quarters, [3.0, 4.0, 5.0], s => s.Orientation = BarOrientation.Horizontal)
                .Bar(Quarters, [4.0, 5.0, 6.0], s => s.Orientation = BarOrientation.Horizontal))
            .Build();

        figure.ToSvg();

        var bars = figure.SubPlots[0].Series.OfType<BarSeries>().ToArray();
        var vertical = bars.Where(b => b.Orientation == BarOrientation.Vertical).ToArray();
        var horizontal = bars.Where(b => b.Orientation == BarOrientation.Horizontal).ToArray();

        Assert.Equal(2, vertical.Select(b => b.BarGroupOffset).Distinct().Count());
        Assert.Equal(2, horizontal.Select(b => b.BarGroupOffset).Distinct().Count());
        Assert.Equal(vertical.Select(b => b.BarGroupOffset), horizontal.Select(b => b.BarGroupOffset));
    }

    [Fact]
    public void NoCategories_IsRefusedByName()
    {
        Assert.Throws<ArgumentNullException>(() =>
            Plt.Create().AddSubPlot(1, 1, 1, ax => ax.GroupedBar(null!, Regions)).Build());
    }

    [Fact]
    public void NoGroupList_IsRefusedByName()
    {
        Assert.Throws<ArgumentNullException>(() =>
            Plt.Create().AddSubPlot(1, 1, 1, ax => ax.GroupedBar(Quarters, null!)).Build());
    }
}
