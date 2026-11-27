// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Tests.TestFixtures;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Tests.Rendering;

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
        return [.. svg.Each("rect").Where(rect => !rect.Ancestors().Any(group => (string?)group.Attribute("class") == "legend") && rect.Attribute("fill")?.Value.StartsWith('#') == true)
            .Select(rect => (X: rect.Number("x"), Y: rect.Number("y"), W: rect.Number("width"), H: rect.Number("height"), Fill: (string)rect.Attribute("fill")!))
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

    /// <summary>The refusal, word for word: the type, the parameter and the message every verb that takes a
    /// <see cref="BarGroup"/> gives.</summary>
    [Fact]
    public void AGroupThatDoesNotFitTheCategories_IsRefusedWithTheOneMessage()
    {
        var wrong = new BarGroup[] { new("north", [3.0, 4.0, 5.0]), new("south", [3.0, 4.0]) };

        var error = Assert.Throws<ArgumentException>(() =>
            Plt.Create().AddSubPlot(1, 1, 1, ax => ax.GroupedBar(Quarters, wrong)).Build());

        Assert.Equal("groups", error.ParamName);
        Assert.Equal(2.RefusalAgainst(3, "groups", "group 'south'"), error.Message);
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

    /// <summary>Grouped horizontal bars lie along X: each as long as its value, all from the same x (the value 0), the
    /// groups side by side along Y inside their category's slot, and the category named on the Y axis level with the
    /// middle of its group. Before, the values were scaled along Y and the categories along X.</summary>
    [Fact]
    public void Horizontal_BarsScaleTheirValuesAlongX_AndTheCategoriesAreNamedAlongY()
    {
        string svg = Plt.Create().WithSize(600, 400)
            .AddSubPlot(1, 1, 1, ax => ax.GroupedBar(Quarters, Regions,
                s => s.Orientation = BarOrientation.Horizontal))
            .ToSvg();

        var bars = Bars(svg);   // north Q1..Q3, south Q1..Q3, east Q1..Q3
        var plot = svg.PlotArea();

        Assert.Equal(9, bars.Length);
        Assert.Single(bars.Select(b => Math.Round(b.X, 6)).Distinct());
        Assert.Equal(3 * bars[6].W, bars[0].W, SvgMarkupExtensions.WrittenPrecision);               // north Q1 (3) against east Q1 (1)
        Assert.Equal(5.0 / 3 * bars[8].W, bars[2].W, SvgMarkupExtensions.WrittenPrecision);         // north Q3 (5) against east Q3 (3)
        Assert.All(bars, b => Assert.InRange(b.X + b.W, plot.X, plot.X + plot.Width + 1e-6));
        for (int q = 0; q < 3; q++)
        {
            var middle = bars[3 + q];                              // south: the middle group, centred on the slot
            var name = Assert.Single(Rendering.SeriesRenderers.BarSeriesRendererTests.Texts(svg, Quarters[q]));
            Assert.Equal(middle.Y + middle.H / 2 + 0.35 * MatPlotLibNet.Styling.Theme.MatplotlibV2.TickFont().Size, name.Y, SvgMarkupExtensions.WrittenPrecision);   // centred on the slot
            Assert.True(name.X < plot.X, $"{Quarters[q]} stands left of the Y spine");
        }
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
