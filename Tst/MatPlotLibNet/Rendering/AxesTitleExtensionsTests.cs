// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The one title rule of an axes and of its axis titles (<see cref="AxesTitleExtensions"/>): the font a title is
/// drawn in, its pad, where the axes title stands above the plot and the room the layout keeps for it, and the room the X
/// axis title takes below the bottom spine.</summary>
public class AxesTitleExtensionsTests
{
    private static readonly Theme Theme = Theme.Default;
    private static readonly Font LabelFont = new() { Size = 10 };

    private static Axes TitledAxes(TextStyle? style = null) => new() { Title = "Revenue", TitleStyle = style };

    private static Axis TitledAxis(TextStyle? style = null) => new() { Label = "Rate", LabelStyle = style };

    [Fact]
    public void TheTitleGap_IsEightPixels() => Assert.Equal(8.0, AxesTitleExtensions.TitleGap);

    /// <summary>With no style the axes title is drawn in the theme's title font, with no pad, its baseline the gap above
    /// what is drawn above the top spine.</summary>
    [Fact]
    public void AnAxesTitleWithNoStyle_IsTheThemesTitleFont_TheGapAboveTheTopBand()
    {
        var axes = TitledAxes();

        Assert.Equal(Theme.TitleFont(), axes.TitleFont(Theme));
        Assert.Equal(0, axes.TitlePad);
        Assert.Equal(100 - 8 - 12, axes.TitleBaseline(100, 12, axes.TitleLift(new SvgRenderContext(), Theme)));
    }

    /// <summary>A style changes the font and lifts the title by its pad.</summary>
    [Fact]
    public void AnAxesTitleWithAStyle_IsDrawnInItsFont_ItsPadHigher()
    {
        var axes = TitledAxes(new TextStyle { FontSize = 30, Pad = 20 });

        Assert.Equal(30, axes.TitleFont(Theme).Size);
        Assert.Equal(20, axes.TitlePad);
        Assert.Equal(100 - 8 - 20 - 12, axes.TitleBaseline(100, 12, axes.TitleLift(new SvgRenderContext(), Theme)));
    }

    /// <summary>The room the layout keeps above the top spine for an axes with no title: the top band and the pad above it that
    /// the caller asks for, nothing when nothing is drawn there.</summary>
    [Fact]
    public void TheTitleBand_WithoutATitle_IsTheTopBandAndThePad_OrNothing()
    {
        var ctx = new SvgRenderContext();

        Assert.Equal(0, new Axes().TitleBand(ctx, Theme, 0, 8));
        Assert.Equal(12 + 8, new Axes().TitleBand(ctx, Theme, 12, 8));
        Assert.Equal(12, new Axes().TitleBand(ctx, Theme, 12, 0));
    }

    /// <summary>With a title: the top band, the gap the title stands above it, the title as measured, and the pad the caller
    /// asks for above it.</summary>
    [Fact]
    public void TheTitleBand_WithATitle_HoldsTheTitleItsGapBelowAndThePadAbove()
    {
        var ctx = new SvgRenderContext();
        var axes = TitledAxes();
        double height = ctx.MeasureText("Revenue", Theme.TitleFont()).Height;

        Assert.Equal(12 + height + 8 + 8, axes.TitleBand(ctx, Theme, 12, 8));
        Assert.Equal(12 + height + 8, axes.TitleBand(ctx, Theme, 12, 0));
    }

    /// <summary>A styled title is kept room for as it is drawn: measured in its own font, and lifted by its pad.</summary>
    [Fact]
    public void TheTitleBand_OfAStyledTitle_HoldsItsFontAndItsPad()
    {
        var ctx = new SvgRenderContext();
        var axes = TitledAxes(new TextStyle { FontSize = 30, Pad = 20 });
        double height = ctx.MeasureText("Revenue", axes.TitleFont(Theme)).Height;

        Assert.Equal(12 + height + 8 + 8 + 20, axes.TitleBand(ctx, Theme, 12, 8));
    }

    /// <summary>The layout keeps a large title lifted by a pad inside the figure: its top, a font size above its baseline,
    /// does not stand above the figure's top edge.</summary>
    [Fact]
    public void ATightLayout_KeepsAStyledTitleWithAPadInsideTheFigure()
    {
        var ctx = new SvgRenderContext();
        var figure = Plt.Create().WithSize(400, 300).TightLayout()
            .AddSubPlot(1, 1, 1, ax => ax.WithTitle("Revenue", s => s with { FontSize = 40, Pad = 30 }).Plot([0.0, 1.0], [0.0, 1.0]))
            .Build();
        var axes = figure.SubPlots[0];

        var spacing = new MatPlotLibNet.Rendering.Layout.ConstrainedLayoutEngine().Compute(figure, ctx);

        double baseline = axes.TitleBaseline(spacing.MarginTop, 0, axes.TitleLift(ctx, figure.Theme));
        Assert.True(baseline - 40 >= 0, $"the title's top stands at {baseline - 40}, above the figure");
    }

    [Fact]
    public void AnAxisTitleFont_IsTheBaseFontWithTheLabelStyleApplied()
    {
        Assert.Equal(LabelFont, new Axis().TitleFont(LabelFont));
        Assert.Equal(17, TitledAxis(new TextStyle { FontSize = 17 }).TitleFont(LabelFont).Size);
    }

    /// <summary>Under a theme, an axis title is drawn in the theme's axis label font with the axis' label style applied: the one
    /// font every axis title (the X, the top, a linked bottom, every Y slot, a ternary corner) is drawn and measured in.</summary>
    [Fact]
    public void AnAxisTitleFontUnderATheme_IsTheThemesLabelFontWithTheLabelStyleApplied()
    {
        var styled = TitledAxis(new TextStyle { FontSize = 17, Color = Colors.Red });

        Assert.Equal(Theme.MatplotlibV2.LabelFont(), new Axis().TitleFontUnder(Theme.MatplotlibV2));
        Assert.Equal(new TextStyle { FontSize = 17, Color = Colors.Red }.ApplyTo(Theme.Dark.LabelFont()), styled.TitleFontUnder(Theme.Dark));
    }

    [Fact]
    public void AnAxisTitlePad_IsItsStylesPad_OrNone()
    {
        Assert.Equal(0, TitledAxis().TitlePad);
        Assert.Equal(6, TitledAxis(new TextStyle { Pad = 6 }).TitlePad);
    }

    /// <summary>The room the X axis title takes below the bottom spine: none without a title; otherwise the one placement
    /// rule (<see cref="TitleClearance.TitleExtent"/> beside the bottom edge) in the title's own font, past its pad.</summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(14.0)]
    public void TheBottomTitleExtent_IsThePlacementRuleInTheTitlesFontPastItsPad(double tallestTickLabel)
    {
        var ctx = new SvgRenderContext();
        var axis = TitledAxis(new TextStyle { FontSize = 17, Pad = 6 });
        var font = axis.TitleFont(Theme.Default.LabelFont());
        double expected = TitleClearance.Beside(EdgeTerms.Bottom, axis.MajorTicks.TickLabelBand(tallestTickLabel), TitleClearance.XLabelGap, 17)
            .TitleExtent(ctx.MeasureText("Rate", font).Height, 6);

        Assert.Equal(expected, axis.BottomTitleExtent(axis.Label, ctx, Theme.Default, tallestTickLabel));
        Assert.Equal(0, new Axis().BottomTitleExtent(null, ctx, Theme.Default, tallestTickLabel));
    }

    /// <summary>The band above the top spine of a top X axis with a title ends where the title's box ends, by the rule the room below
    /// the bottom spine is kept by (<see cref="TitleClearance.TitleExtent"/> beside the top edge), in the title's own font past its pad:
    /// matplotlib stands the axes title above <c>xaxis.get_tightbbox().ymax</c>, the top of the axis title's text box
    /// (<c>Axes._update_title_position</c>), so the box's part below the baseline, which faces the tick labels, is counted once. Without
    /// a title the band is the tick label band.</summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(14.0)]
    public void TheTopBand_IsThePlacementRuleInTheTitlesFontPastItsPad(double tallestTickLabel)
    {
        var ctx = new SvgRenderContext();
        var axis = TitledAxis(new TextStyle { FontSize = 17, Pad = 6 });
        var font = axis.TitleFont(Theme.Default.LabelFont());
        double expected = TitleClearance.Beside(EdgeTerms.Top, axis.MajorTicks.TickLabelBand(tallestTickLabel), TitleClearance.XLabelGap, 17)
            .TitleExtent(ctx.MeasureText("Rate", font).Height, 6);

        Assert.Equal(expected, axis.TopBand(axis.Label, ctx, Theme.Default, tallestTickLabel));
        Assert.Equal(new Axis().MajorTicks.TickLabelBand(tallestTickLabel), new Axis().TopBand(null, ctx, Theme.Default, tallestTickLabel));
    }

    /// <summary>A title that holds math is drawn as the rich text it parses to, so the layout reserves the height of that rich
    /// text: a large operator is set larger than a line of the title's font.</summary>
    [Fact]
    public void TheTitleBand_OfAMathTitle_IsTheHeightItIsDrawnAt()
    {
        IRenderContext ctx = new SvgRenderContext();
        var axes = new Axes { Title = @"$\sum x$" };
        var font = axes.TitleFont(Theme);
        double drawn = ctx.MeasureRichText(MathTextParser.Parse(axes.Title), font).Height;

        Assert.NotEqual(ctx.MeasureText(axes.Title, font).Height, drawn);
        Assert.Equal(12 + drawn + Theme.TitleGapUnder + 8, axes.TitleBand(ctx, Theme, 12, 8));
    }

    /// <summary>An X axis title that holds math takes the room of the rich text it is drawn as.</summary>
    [Fact]
    public void TheBottomTitleExtent_OfAMathTitle_IsTheHeightItIsDrawnAt()
    {
        IRenderContext ctx = new SvgRenderContext();
        var axis = new Axis { Label = @"$\sum x$" };
        var font = axis.TitleFont(Theme.Default.LabelFont());
        double drawn = ctx.MeasureRichText(MathTextParser.Parse(axis.Label), font).Height;
        double expected = TitleClearance.Beside(EdgeTerms.Bottom, axis.MajorTicks.TickLabelBand(14), Theme.Default.XLabelGapUnder, font.Size)
            .TitleExtent(drawn, 0);

        Assert.Equal(expected, axis.BottomTitleExtent(axis.Label, ctx, Theme.Default, 14));
    }
}
