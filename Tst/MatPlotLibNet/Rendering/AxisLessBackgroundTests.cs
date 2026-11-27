// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>An axes that draws no axes paints no background under the classic theme, as matplotlib's does not. matplotlib 3.11.2:
/// <c>Axes.set_axis_off()</c> turns <c>axison</c> off, and <c>Axes.draw</c> then draws no axes patch (<c>if not (self.axison and
/// self._frameon): ... artists.remove(spine)</c> and the patch is drawn only <c>if self.axison and self._frameon</c>), and
/// <c>Axes.pie</c> calls <c>self.set_frame_on(False)</c>; the classic figure's grey (<c>figure.facecolor: 0.75</c>) therefore
/// stands behind a sparkline or a pie where its axes colour (white) would have been. Measured: the classic sparkline picture of
/// <c>ax.margins(0); ax.set_axis_off()</c> is grey to the edge of the line. The library filled the axes background behind an axis-less
/// series, a white rectangle on the grey figure. Under v2 the two grounds are both white and the library's own themes keep their panel
/// look, so nothing is drawn differently there.</summary>
public sealed class AxisLessBackgroundTests
{
    private static readonly Rect Plot = new(80, 40, 400, 300);

    private static bool PaintsTheBackground(Axes axes, Theme theme)
    {
        var ctx = axes.Drawn(Plot, theme);
        return ctx.Of<DrawRectangleCall>()
            .Any(a => a.Rect == Plot && a.Fill == theme.AxesBackground);
    }

    private static Axes Sparkline()
    {
        var axes = new Axes();
        axes.Sparkline([1.0, 3.0, 2.0, 5.0]);
        return axes;
    }

    private static Axes Pie()
    {
        var axes = new Axes();
        axes.Pie([1.0, 2.0, 3.0]);
        return axes;
    }

    private static Axes Line()
    {
        var axes = new Axes();
        axes.Plot([1.0, 2.0, 3.0], [1.0, 3.0, 2.0]);
        return axes;
    }

    [Fact]
    public void Classic_ASparklineAndAPie_PaintNoBackground_TheFiguresGroundStandsBehindThem()
    {
        Assert.False(PaintsTheBackground(Sparkline(), Theme.MatplotlibClassic));
        Assert.False(PaintsTheBackground(Pie(), Theme.MatplotlibClassic));
    }

    [Fact]
    public void Classic_AnAxesWithAxes_StillPaintsItsBackground() =>
        Assert.True(PaintsTheBackground(Line(), Theme.MatplotlibClassic));

    [Fact]
    public void Classic_AnAxesThatMixesAxisLessAndOrdinarySeries_KeepsItsAxesAndItsBackground()
    {
        var axes = Line();
        axes.Sparkline([1.0, 2.0]);

        Assert.True(PaintsTheBackground(axes, Theme.MatplotlibClassic));
    }

    [Fact]
    public void Classic_ASankeyDiagram_KeepsItsAxesBackground_AsMatplotlibsSankeyKeepsItsFrame()
    {
        var axes = new Axes();
        axes.Sankey([new SankeyNode("a"), new SankeyNode("b")], [new SankeyLink(0, 1, 1.0)]);

        Assert.True(PaintsTheBackground(axes, Theme.MatplotlibClassic));
    }

    [Fact]
    public void V2AndTheLibrarysOwnThemes_PaintTheBackgroundBehindAnAxisLessSeriesAsBefore()
    {
        Assert.True(PaintsTheBackground(Sparkline(), Theme.MatplotlibV2));
        Assert.True(PaintsTheBackground(Pie(), Theme.Default));
    }

    /// <summary>Only the classic style draws no background behind a figure that has no axes; the default style and the library's own themes do, and a clone, the
    /// data transfer object and the JSON of a figure keep what the theme said.</summary>
    [Fact]
    public void TheThemeCarriesIt_AndAClone_AndTheJson_KeepIt()
    {
        Assert.True(Theme.MatplotlibClassic.Looks.Frame.NoBackgroundWithoutAxes);
        Assert.False(Theme.MatplotlibV2.Looks.Frame.NoBackgroundWithoutAxes);
        Assert.False(Theme.Default.Looks.Frame.NoBackgroundWithoutAxes);
        Assert.True(Theme.MatplotlibClassic.Cloned().Looks.Frame.NoBackgroundWithoutAxes);
        Assert.True(Theme.MatplotlibClassic.ThroughDto().Looks.Frame.NoBackgroundWithoutAxes);
        Assert.False(Theme.MatplotlibV2.ThroughDto().Looks.Frame.NoBackgroundWithoutAxes);
    }
}
