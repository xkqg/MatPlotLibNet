// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.Layout;

/// <summary>The room a layout keeps above the top band and the title of the top row. matplotlib 3.11.2 keeps none in a fixed layout:
/// <c>Axes.title</c> is a text at (0.5, 1.0) in axes coordinates lifted by <c>axes.titlepad</c> (6 points,
/// <c>_set_title_offset_trans</c>), <c>_update_title_position</c> only moves it up past the top tick labels, and the figure's
/// <c>suptitle</c> stands at 0.98 of the height, <c>va='top'</c>, asking the axes for nothing; the subplot parameters are what
/// they are (<c>PolarAxes.clear</c> sets <c>self.title.set_y(1.05)</c>, a title that rises above its square). Only
/// <c>tight_layout</c> keeps a pad: <c>_auto_adjust_subplotpars</c> adds <c>pad_inch</c> above the first row's content and
/// <c>margin_top += rel_height + pad_inch</c> for the suptitle (<c>pad=1.08</c> times the font size, at the figure's edge and
/// between the suptitle and the axes). The library's own themes keep the pad they have always kept, the layout's own buffer
/// (<c>ConstrainedLayoutEngine.PadTop</c>).</summary>
public sealed class TitlePadAboveTests
{
    private static Figure Polar(Theme theme, bool tight = false, string? suptitle = null)
    {
        var builder = Plt.Create().WithSize(800, 600).WithTheme(theme);
        if (tight) builder.TightLayout();
        if (suptitle is not null) builder.WithTitle(suptitle);
        return builder.AddSubPlot(1, 1, 1, ax => ax.WithTitle("Rose").PolarPlot([1.0, 2.0, 3.0], [0.0, 1.0, 2.0])).Build();
    }

    /// <summary>The classic subplot parameters leave a cell of 620 x 480 (left 0.125, right 0.9, bottom 0.1, top 0.9) and a polar axes
    /// is the centred square of its cell: 480 across. The rise of the title, the title and the gap below it come to 55 px, inside
    /// the 60 px margin, so nothing widens the margin and the square keeps its 480.</summary>
    [Theory]
    [InlineData("classic", 170, 60, 480)]
    [InlineData("v2", 179, 72, 462)]
    public void APolarTitle_LeavesTheFixedTopMarginAndTheSquareAlone(string theme, double x, double y, double side)
    {
        var plot = Polar(theme.AsTheme()).FirstPlotArea(new SvgRenderContext());

        Assert.Equal(new Rect(x, y, side, side), plot);
    }

    /// <summary>Under the library's own themes the pad above the title stays: the margin holds the rise, the title and a gap on both
    /// sides of it.</summary>
    [Fact]
    public void TheLibrarysTheme_KeepsThePadAboveTheTitle()
    {
        var ctx = new SvgRenderContext();
        var figure = Polar(Theme.Default);

        var plot = figure.FirstPlotArea(ctx);

        double title = ctx.MeasureText("Rose", figure.SubPlots[0].TitleFont(figure.Theme)).Height;
        Assert.True(plot.Y >= 0.05 * plot.Height + title + 2 * AxesTitleExtensions.TitleGap - 1e-9);
    }

    /// <summary>A tight layout keeps the pad under every theme, as <c>tight_layout</c> does: <c>_auto_adjust_subplotpars</c> keeps
    /// <c>pad_inch</c> between the figure's edge and the top of the axes' tight box, which holds the title wherever the theme's polar
    /// look raises it (over the spoke labels under the default style, at the top of the box under the classic one). The check reads
    /// the title where it is drawn, so it holds whatever rule places it.</summary>
    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    public void ATightLayout_KeepsThePadAboveTheTitle_UnderTheMatplotlibThemes(string themeId)
    {
        var figure = Polar(themeId.AsTheme(), tight: true);

        var recording = new RecordingRenderContext();
        new ChartRenderer().Render(figure, recording);
        var baseline = recording.Of<DrawTextCall>()
            .Where(args => args.Text == "Rose").Select(args => (args.Position).Y).Single();

        double titleTop = baseline - recording.ExtentOf(TextLabel.Plain("Rose"), figure.SubPlots[0].TitleFont(figure.Theme)).Ascent;
        Assert.True(titleTop >= ConstrainedLayoutEngine.PadTop - 1e-9, $"the title's top stands at {titleTop}, the pad is {ConstrainedLayoutEngine.PadTop}");
    }

    /// <summary>A top title stands clear of the figure's title: the suptitle's band, then the axes title's band, in a tight layout
    /// as <c>margin_top += rel_height + pad_inch</c> stacks them.</summary>
    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    public void ATopTitle_ClearsTheFiguresTitle(string themeId)
    {
        var ctx = new SvgRenderContext();
        var theme = themeId.AsTheme();
        var figure = Plt.Create().WithSize(800, 600).WithTheme(theme).TightLayout().WithTitle("Super")
            .AddSubPlot(1, 1, 1, ax => ax.WithTitle("Axes").Plot([1.0, 2.0, 3.0], [1.0, 3.0, 2.0])).Build();

        var plot = figure.FirstPlotArea(ctx);

        double suptitle = ctx.MeasureText("Super", theme.SupTitleFont()).Height;
        double title = ctx.MeasureText("Axes", figure.SubPlots[0].TitleFont(theme)).Height;
        double expected = SupTitleBand.Needed(suptitle) + title + theme.TitleGapUnder + AxesTitleExtensions.TitleGap;
        Assert.True(plot.Y >= expected - 1e-9, $"the plot starts at {plot.Y}, the two titles need {expected}");
    }

    /// <summary>The title of a lower row has the upper row's tick labels above it, and keeps the pad that holds them apart.</summary>
    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    public void ALowerRowsTitle_KeepsThePadAboveIt(string themeId)
    {
        var ctx = new SvgRenderContext();
        var theme = themeId.AsTheme();
        var figure = Plt.Create().WithSize(800, 600).WithTheme(theme)
            .AddSubPlot(2, 1, 1, ax => ax.WithTitle("Upper").Plot([1.0, 2.0, 3.0], [1.0, 3.0, 2.0]))
            .AddSubPlot(2, 1, 2, ax => ax.WithTitle("Lower").Plot([1.0, 2.0, 3.0], [1.0, 3.0, 2.0])).Build();

        var plots = new ChartRenderer().ComputeLayout(figure, ctx).PlotAreas;

        double title = ctx.MeasureText("Lower", figure.SubPlots[1].TitleFont(theme)).Height;
        double gap = plots[1].Y - plots[0].Bottom;
        double needed = title + theme.TitleGapUnder + AxesTitleExtensions.TitleGap;
        Assert.True(gap >= needed, $"the rows are {gap} apart, the lower title needs {needed}");
    }

    [Fact]
    public void TheThemeCarriesTheRule_AndTheJsonDoesToo()
    {
        Assert.True(Theme.MatplotlibV2.Looks.Figure.NoPadAboveTitle);
        Assert.True(Theme.MatplotlibClassic.Looks.Figure.NoPadAboveTitle);
        Assert.False(Theme.Default.Looks.Figure.NoPadAboveTitle);
        Assert.True(Theme.MatplotlibClassic.Cloned().Looks.Figure.NoPadAboveTitle);

        Assert.True(Polar(Theme.MatplotlibV2).RoundTripped().Theme.Looks.Figure.NoPadAboveTitle);
        Assert.False(Polar(Theme.Default).RoundTripped().Theme.Looks.Figure.NoPadAboveTitle);
    }
}
