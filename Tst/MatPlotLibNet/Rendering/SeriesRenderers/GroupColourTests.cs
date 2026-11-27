// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>A strip plot and a swarm plot colour each of their groups from the cycle under the matplotlib themes, as seaborn 0.13 does
/// for a frame given as columns (<c>sns.stripplot(data=[a, b, c])</c>: the references' pictures, measured pixel by pixel, hold
/// tab:blue, tab:orange and tab:green dots in the default style and b, g, r in the classic one, in groups of equal ink). The library
/// drew every group in the one colour of the series' slot. seaborn 0.13.2 colours the groups from its palette's first colour whatever
/// the axes drew before, and asks the axes once, through a scout scatter (the patch cycle; none in classic mode); printed with python
/// (cycles_probe4): after two scatters and a line, the groups are C0, C1, C2 and the next scatter C3, the next line C1. A colour the
/// series names still colours them all. The library's own themes keep one colour.</summary>
public class GroupColourTests
{
    // Groups of 3, 4 and 5 dots, so a group is told from its dots' count.
    private static readonly double[][] Groups = [[1.0, 2.0, 3.0], [2.0, 3.0, 4.0, 5.0], [3.0, 4.0, 5.0, 6.0, 7.0]];

    private static XDocument Svg(Theme theme, Action<AxesBuilder> axes) =>
        Plt.Create().WithSize(500, 400).WithTheme(theme).AddSubPlot(1, 1, 1, axes).Build().ToSvg().Xml();

    private static int Dots(XDocument svg, Color fill) =>
        svg.Each("circle").Count(e => (string?)e.Attribute("fill") == fill.ToHex());

    private static void AddStrip(AxesBuilder ax, Color? color = null) =>
        ax.Stripplot(Groups, s => { if (color is { } c) s.Color = c; });

    private static void AddSwarm(AxesBuilder ax, Color? color = null) =>
        ax.Swarmplot(Groups, s => { if (color is { } c) s.Color = c; });

    // ── the matplotlib themes: one colour for each group, in the order of the cycle ──────────────────────────────

    /// <summary>Under the classic style the cycle is blue, green, red.</summary>
    [Theory]
    [InlineData("v2", "strip")]
    [InlineData("v2", "swarm")]
    [InlineData("classic", "strip")]
    [InlineData("classic", "swarm")]
    public void AStripOrSwarmPlot_ColoursEachGroupFromTheCycle(string theme, string kind)
    {
        var look = theme.AsTheme();
        var svg = Svg(look, ax => { if (kind == "strip") AddStrip(ax); else AddSwarm(ax); });

        var cycle = look.CycleColors;
        Assert.Equal(3, Dots(svg, cycle[0]));
        Assert.Equal(4, Dots(svg, cycle[1]));
        Assert.Equal(5, Dots(svg, cycle[2]));
    }

    [Fact]
    public void AColourTheSeriesNames_ColoursEveryGroup()
    {
        var strip = Svg(Theme.MatplotlibV2, ax => AddStrip(ax, Colors.Red));
        var swarm = Svg(Theme.MatplotlibV2, ax => AddSwarm(ax, Colors.Red));

        Assert.Equal(12, Dots(strip, Colors.Red));
        Assert.Equal(12, Dots(swarm, Colors.Red));
    }

    [Fact]
    public void TheSeriesAfterAStripPlot_ALineWearsTheFirstColour_AScatterTheSecond()
    {
        // The strip plot moved the patch cycle once and the lines cycle not at all.
        var axes = new Axes();
        axes.AddSeries(new StripplotSeries(Groups));
        axes.Plot([0.0, 1.0], [1.0, 2.0]);
        axes.Scatter([0.0, 1.0], [1.0, 2.0]);

        Assert.Equal([0, 0, 1], axes.ColorSlots(Theme.MatplotlibV2));
        var svg = Svg(Theme.MatplotlibV2, ax => { AddStrip(ax); ax.Plot([0.0, 1.0, 2.0], [1.0, 2.0, 3.0]); });
        Assert.Contains(svg.Descendants(), e => (string?)e.Attribute("stroke") == Theme.MatplotlibV2.CycleColors[0].ToHex() && (string?)e.Attribute("fill") == "none");
    }

    [Fact]
    public void AStripPlotThatNamesAColour_MovesNoCycle()
    {
        var axes = new Axes();
        axes.AddSeries(new StripplotSeries(Groups) { Color = Colors.Red });
        axes.Scatter([0.0, 1.0], [1.0, 2.0]);

        Assert.Equal([0, 0], axes.ColorSlots(Theme.MatplotlibV2));
    }

    // ── the library's own themes keep one colour ─────────────────────────────────────────────────────────────────

    [Fact]
    public void TheLibrarysOwnTheme_DrawsEveryGroupInOneColour_AndTheNextSeriesWearsTheNextSlot()
    {
        var svg = Svg(Theme.Default, ax => { AddStrip(ax); ax.Plot([0.0, 1.0, 2.0], [1.0, 2.0, 3.0]); });

        var cycle = Theme.Default.CycleColors;
        Assert.Equal(12, Dots(svg, cycle[0]));
        Assert.Contains(svg.Descendants(), e => (string?)e.Attribute("stroke") == cycle[1].ToHex() && (string?)e.Attribute("fill") == "none");
    }

    // ── the theme and the slot carry it ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TheColourSlotsOfAxes_MoveThePatchCycleOncePerStripOrSwarmPlot_AndOneSlotEachUnderTheLibrarysThemes()
    {
        var axes = new Axes();
        axes.AddSeries(new StripplotSeries(Groups));
        axes.AddSeries(new SwarmplotSeries(Groups));
        axes.AddSeries(new StripplotSeries(Groups) { Color = Colors.Red });
        axes.Plot([0.0, 1.0], [1.0, 2.0]);

        Assert.Equal([0, 1, 0, 0], axes.ColorSlots(Theme.MatplotlibV2));
        Assert.Equal([0, 0, 0, 0], axes.ColorSlots(Theme.MatplotlibClassic));
        Assert.Equal([0, 1, 2, 3], axes.ColorSlots(Theme.Default));
    }

    [Fact]
    public void AStripPlotWithNoGroups_StillMovesThePatchCycleOnce()
    {
        var axes = new Axes();
        axes.AddSeries(new StripplotSeries([]));
        axes.Scatter([0.0, 1.0], [1.0, 2.0]);

        Assert.Equal([0, 1], axes.ColorSlots(Theme.MatplotlibV2));
    }
}
