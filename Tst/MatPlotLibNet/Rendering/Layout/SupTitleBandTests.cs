// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Builders;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.Layout;

/// <summary>The band the figure's title takes at the top: a pad above it, the title as measured, a pad below it. The drawing
/// (where the first row of subplots starts, where the title's baseline stands) and the layout engine (how much top margin to
/// reserve) read it from one owner, so they cannot reserve one band and draw another.</summary>
public sealed class SupTitleBandTests
{
    [Fact]
    public void ThePads_AreEightAbove_AndTwelveBelow()
    {
        Assert.Equal(8.0, SupTitleBand.TopPad);
        Assert.Equal(12.0, SupTitleBand.BottomPad);
    }

    [Theory]
    [InlineData(0.0, 20.0)]
    [InlineData(18.5, 38.5)]
    [InlineData(40.0, 60.0)]
    public void TheBandNeeded_IsThePadsAroundTheTitleAsMeasured(double titleHeight, double expected)
    {
        Assert.Equal(expected, SupTitleBand.Needed(titleHeight));
    }

    /// <summary>The engine reserves the band the frame starts the first row below: a figure with a title and nothing above
    /// its plot reserves at least <see cref="SupTitleBand.Needed"/> of the title it measured, and the frame starts its first
    /// row no higher than that.</summary>
    [Fact]
    public void TheEngineReservesWhatTheFrameDrawsBelow()
    {
        var ctx = new SvgRenderContext();
        var figure = new FigureBuilder().WithSize(800, 600).WithTitle("A title of the figure")
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2.0], [1.0, 2.0])).Build();
        double titleHeight = ctx.MeasureText(figure.Title!, figure.Theme.SupTitleFont()).Height;

        var spacing = new ConstrainedLayoutEngine().Compute(figure, ctx);
        var frame = new ChartRenderer().FrameOf(figure, ctx);

        Assert.Equal(titleHeight, frame.TitleHeight);
        Assert.True(spacing.MarginTop >= SupTitleBand.Needed(titleHeight));
        Assert.True(frame.PlotAreaTop >= SupTitleBand.Needed(titleHeight));
    }

    /// <summary>A figure title written as math is measured as it is drawn, by the layout as by the frame: a sum with its limits
    /// stands taller than its source text, and the tight layout keeps the band the frame draws it in.</summary>
    [Fact]
    public void TheTightLayout_ReservesAMathTitle_AsItIsDrawn()
    {
        var ctx = new SvgRenderContext();
        var figure = new FigureBuilder().WithSize(800, 600).WithTitle(@"$\sum_{i=0}^{n} x_i$ of the whole").TightLayout()
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2.0], [1.0, 2.0])).Build();

        var frame = new ChartRenderer().FrameOf(figure, ctx);

        Assert.True(frame.TitleHeight > ctx.MeasureText(figure.Title!, figure.Theme.SupTitleFont()).Height);
        Assert.True(frame.Spacing.MarginTop >= SupTitleBand.Needed(frame.TitleHeight),
            $"top margin {frame.Spacing.MarginTop}, band of the title as drawn {SupTitleBand.Needed(frame.TitleHeight)}");
    }

    /// <summary>The font size and weight a figure's title is given are the ones it is measured, reserved and drawn in.
    /// matplotlib 3.11.2, 640 by 480 under the default style: <c>fig.suptitle(..., fontsize=14, fontweight='bold')</c> and
    /// <c>tight_layout()</c> move the top of the axes from 0.925 to 0.853 of the height, the band of the title in its own size.</summary>
    [Fact]
    public void AStyledTitle_IsMeasuredReservedAndDrawnInItsStyle()
    {
        var ctx = new SvgRenderContext();
        const double FourteenPoints = 14 * 100.0 / 72;
        var styled = new FigureBuilder().WithSize(640, 480).TightLayout()
            .WithTitle("A title of the figure", style => style with { FontSize = FourteenPoints, FontWeight = FontWeight.Bold })
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2.0], [1.0, 2.0])).Build();
        var plain = new FigureBuilder().WithSize(640, 480).TightLayout().WithTitle("A title of the figure")
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2.0], [1.0, 2.0])).Build();
        var font = styled.Theme.SupTitleFont() with { Size = FourteenPoints, Weight = FontWeight.Bold };

        var frame = new ChartRenderer().FrameOf(styled, ctx);
        var drawn = new RecordingRenderContext();
        new ChartRenderer().Render(styled, drawn);

        Assert.Equal(ctx.MeasureText(styled.Title!, font).Height, frame.TitleHeight);
        Assert.True(frame.TitleHeight > new ChartRenderer().FrameOf(plain, ctx).TitleHeight);
        Assert.True(frame.Spacing.MarginTop >= SupTitleBand.Needed(frame.TitleHeight));
        var title = drawn.Of<DrawTextCall>().Single(c => c.Text == styled.Title);
        Assert.Equal(font, title.Font);
    }

    /// <summary>The pad of a title's style moves the subplots further from the title, as it moves an axes title further from
    /// its plot: the band the title takes grows by it, under every layout.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ThePadOfTheTitlesStyle_WidensTheBandBelowIt(bool tight)
    {
        var ctx = new SvgRenderContext();
        FigureBuilder Titled(double? pad)
        {
            var builder = new FigureBuilder().WithSize(640, 480).WithSubPlotSpacing(s => s with { MarginTop = 0 })
                .WithTitle("A title of the figure", style => style with { Pad = pad })
                .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2.0], [1.0, 2.0]));
            return tight ? builder.TightLayout() : builder;
        }

        var without = new ChartRenderer().FrameOf(Titled(null).Build(), ctx);
        var with = new ChartRenderer().FrameOf(Titled(10).Build(), ctx);

        Assert.Equal(10, with.PlotAreas[0].Y - without.PlotAreas[0].Y, 9);
    }

    /// <summary>A title given no style, or a style edit that is <see langword="null"/>, is the title alone.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ATitleWithoutAStyle_KeepsTheThemesFont(bool withNoEdit)
    {
        var builder = new FigureBuilder();
        var figure = (withNoEdit ? builder.WithTitle("plain", configure: null) : builder.WithTitle("plain")).Build();
        var drawn = new RecordingRenderContext();

        new ChartRenderer().Render(figure, drawn);

        Assert.Null(figure.TitleStyle);
        var title = drawn.Of<DrawTextCall>().Single(c => c.Text == "plain");
        Assert.Equal(figure.Theme.SupTitleFont(), title.Font);
    }

    [Fact]
    public void AFigureWithoutATitle_HasATitleOfNoHeight()
    {
        Assert.Equal(0, new MatPlotLibNet.Models.Figure().TitleHeight(new SvgRenderContext()));
    }
}
