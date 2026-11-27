// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>What <see cref="MatPlotLibNet.Rendering.CartesianAxesRenderer"/> draws of a 2-D axes through the whole pipeline: the
/// category labels of a bar chart, spans, reference lines, annotations and signals with their labels, stacked bars, tick labels and
/// minor ticks, the Y axes stacked on either side, and an X axis whose major ticks are hidden.</summary>
public class CartesianAxesRendererTests
{
    private static string RenderFigure(Action<AxesBuilder> configure, Action<global::MatPlotLibNet.Models.Figure>? postBuild = null)
    {
        var fig = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, configure)
            .Build();
        postBuild?.Invoke(fig);
        return fig.ToSvg();
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("RenderCategoryLabels_BarChart_DrawsCategoryNames", () => RenderFigure(ax => ax.Bar(["Cat A", "Cat B", "Cat C"], [1.0, 2.0, 3.0])), ">Cat A<", ">Cat B<", ">Cat C<"),
        new DrawnToken("Render_WithVerticalSpanLabelledAndStyled_DrawsBorderAndLabel", () => RenderFigure(ax => ax
            .Plot([1.0, 5], [1.0, 5])
            .AxVSpan(2, 3, sp => { sp.Alpha = 0.4; sp.LineStyle = LineStyle.Dashed; sp.LineWidth = 2; sp.Label = "v"; })), ">v<"),
        new DrawnToken("Render_WithHorizontalSpanLabelled_DrawsLabel", () => RenderFigure(ax => ax
            .Plot([1.0, 5], [1.0, 5])
            .AxHSpan(2, 3, sp => { sp.Alpha = 0.3; sp.LineStyle = LineStyle.Solid; sp.Label = "h"; })), ">h<"),
        new DrawnToken("Render_WithAnnotationBoxStyle_RendersCalloutBox", () => RenderFigure(ax => ax
            .Plot([1.0, 5], [1.0, 5])
            .Annotate("box", 2, 3, a => { a.BoxStyle = BoxStyle.Round; a.BoxFaceColor = Colors.Yellow; a.BoxEdgeColor = Colors.Black; })), ">box<"),
        new DrawnToken("Render_WithAnnotationBackgroundColor_RendersBackgroundRect", () => RenderFigure(ax => ax
            .Plot([1.0, 5], [1.0, 5])
            .Annotate("bg", 2, 3, a => a.BackgroundColor = Colors.Yellow)), ">bg<"),
        new DrawnToken("Render_WithAnnotationArrow_RendersArrowAndConnection", () => RenderFigure(ax => ax
            .Plot([1.0, 5], [1.0, 5])
            .Annotate("ann", 2, 3, 4, 4)), ">ann<"),
        new DrawnToken("Render_WithAnnotationArrowStyleNone_SkipsArrow", () => RenderFigure(ax => ax
            .Plot([1.0, 5], [1.0, 5])
            .Annotate("noarrow", 2, 3, 4, 4, a => a.ArrowStyle = ArrowStyle.None)), ">noarrow<"),
        new DrawnToken("Render_WithAnnotationCustomArrowColor_UsesOverride", () => RenderFigure(ax => ax
            .Plot([1.0, 5], [1.0, 5])
            .Annotate("c", 2, 3, 4, 4, a => a.ArrowColor = Colors.Magenta)), ">c<"),
        new DrawnToken("Render_WithAnnotationCustomFont_UsesFont", () => RenderFigure(ax => ax
            .Plot([1.0, 5], [1.0, 5])
            .Annotate("f", 2, 3, a => a.Font = new Font { Family = "monospace", Size = 14 })), ">f<"),
        new DrawnToken("Render_WithBuySignal_DrawsGreenTriangleUp", () => RenderFigure(ax => ax
            .Plot([1.0, 5], [1.0, 5])
            .AddSignal(2, 3, SignalDirection.Buy)), "<polygon"),
        new DrawnToken("Render_WithSellSignal_DrawsRedTriangleDown", () => RenderFigure(ax => ax
            .Plot([1.0, 5], [1.0, 5])
            .AddSignal(2, 3, SignalDirection.Sell)), "<polygon"),
        new DrawnToken("Render_WithSignalCustomColor_UsesOverride", () => RenderFigure(ax => ax
            .Plot([1.0, 5], [1.0, 5])
            .AddSignal(2, 3, SignalDirection.Buy, m => m.Color = Colors.Magenta)), "<polygon"),
        new DrawnToken("Render_StackedBarMode_AssignsBaselineToEachSeries", () => RenderFigure(ax => ax
            .Bar(["A", "B"], [1.0, 2.0], s => s.Color = Colors.Blue)
            .Bar(["A", "B"], [3.0, 4.0], s => s.Color = Colors.Red)
            .SetBarMode(BarMode.Stacked)), "<rect"),
        new DrawnToken("Render_WithVerticalReferenceLineLabelled_DrawsLabel", () => RenderFigure(ax => ax
            .Plot([1.0, 5], [1.0, 5])
            .AxVLine(2.5, r => { r.Label = "v-ref"; r.LineStyle = LineStyle.Dotted; })), ">v-ref<"),
        new DrawnToken("Render_WithHorizontalReferenceLineLabelled_DrawsLabel", () => RenderFigure(ax => ax
            .Plot([1.0, 5], [1.0, 5])
            .AxHLine(3.0, r => { r.Label = "h-ref"; r.LineStyle = LineStyle.Dotted; })), ">h-ref<"),
        new DrawnToken("Render_TickLabelSizeOverride_AppliesCustomSize", () =>
        {
            var fig = Plt.Create()
                .WithSize(500, 400)
                .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2, 3], [1.0, 2, 3]))
                .Build();
            fig.SubPlots[0].XAxis.MajorTicks = fig.SubPlots[0].XAxis.MajorTicks with { LabelSize = 14 };
            fig.SubPlots[0].YAxis.MajorTicks = fig.SubPlots[0].YAxis.MajorTicks with { LabelSize = 12 };
            return fig.ToSvg();
        }, "<text"),
        new DrawnToken("Render_MinorYTicksEnabled_DrawsMinorTicks", () =>
        {
            var fig = Plt.Create()
                .WithSize(500, 400)
                .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2, 3, 4, 5], [10.0, 20, 30, 40, 50]))
                .Build();
            fig.SubPlots[0].YAxis.MinorTicks = fig.SubPlots[0].YAxis.MinorTicks with { Visible = true };
            return fig.ToSvg();
        }, "<line"),
        new DrawnToken("Render_MinorXTicksEnabled_DrawsMinorXTicks", () =>
        {
            var fig = Plt.Create()
                .WithSize(500, 400)
                .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2, 3, 4, 5], [1.0, 4, 9, 16, 25]))
                .Build();
            fig.SubPlots[0].XAxis.MinorTicks = fig.SubPlots[0].XAxis.MinorTicks with { Visible = true, Color = Colors.Gray };
            return fig.ToSvg();
        }, "<line"),
        new DrawnToken("Render_YTickMarkColorOverride_UsesCustomColor", () =>
        {
            var fig = Plt.Create()
                .WithSize(500, 400)
                .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2, 3], [1.0, 2, 3]))
                .Build();
            fig.SubPlots[0].YAxis.MajorTicks = fig.SubPlots[0].YAxis.MajorTicks with { Color = Colors.Blue };
            return fig.ToSvg();
        }, "<line"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    /// <summary>The full render pipeline (<c>Figure.ToSvg()</c>, not an isolated Part) with two stacked
    /// right axes, each with its own series: both lines reach the SVG, at two different Y scales, with no exception
    /// from the orchestration this change touched (CartesianAxesRenderer.Render, the layout engine, the direct-label
    /// column).</summary>
    [Fact]
    public void Render_TwoStackedRightAxes_BothLinesReachTheSvg_AtTheirOwnScale()
    {
        var fig = Plt.Create().WithSize(500, 400).AddSubPlot(1, 1, 1, ax => ax
            .Plot([0.0, 10], [0.0, 10])
            .WithAddRightYAxis(y2 => y2.Plot([0.0, 10], [1_000.0, 2_000.0]))).Build();
        fig.SubPlots[0].YAxes.Right.Add()
            .AddSeries(new MatPlotLibNet.Models.Series.LineSeries([0.0, 10], [-50.0, -10.0]));

        var svg = fig.ToSvg();

        // Three series, three polylines: the primary, slot 0's and slot 1's — none dropped, none merged.
        Assert.Equal(3, svg.Elements("polyline").Count);
    }

    /// <summary>The owner's exact spec (the roadmap, 2026-10-25): "4 axes, 2 left and 2 right" — the primary Y
    /// plus one left-stacked axis, the first right axis plus one right-stacked axis, all four with
    /// their own series, end to end through the real render pipeline.</summary>
    [Fact]
    public void Render_FourYAxesTwoLeftTwoRight_AllFourSeriesReachTheSvg()
    {
        var fig = Plt.Create().WithSize(600, 400).AddSubPlot(1, 1, 1, ax => ax
            .Plot([0.0, 10], [0.0, 10])                                   // primary (left slot 0)
            .WithAddRightYAxis(y2 => y2.Plot([0.0, 10], [1_000.0, 2_000.0]))).Build(); // right slot 0
        var axes = fig.SubPlots[0];
        axes.YAxes.Left.Add().AddSeries(new MatPlotLibNet.Models.Series.LineSeries([0.0, 10], [-50.0, -10.0])); // left slot 1
        axes.YAxes.Right.Add().AddSeries(new MatPlotLibNet.Models.Series.LineSeries([0.0, 10], [500_000.0, 900_000.0])); // right slot 1

        var svg = fig.ToSvg();

        Assert.Equal(4, svg.Elements("polyline").Count);
        // The primary Y is the left stack's own slot 0, so "2 left" is TWO slots on the left stack and "2 right" is
        // TWO slots on the right stack.
        Assert.Equal(2, axes.YAxes.Left.Count);
        Assert.Equal(2, axes.YAxes.Right.Count);
    }

    /// <summary>Reproduces the user-reported defect (2026-10-26, a rendered four-axis chart): the primary Y axis
    /// ALWAYS draws its own ticks + title past the plot's left edge (<see cref="AxesRenderer.RenderAxisLabels"/>),
    /// but the first left-outward-stacked slot's offset came only from <c>LeftAxisBands(...).StackedOffsets()</c> —
    /// the outward left slots summed among THEMSELVES, with no base term for the primary axis's own band — so slot 0
    /// drew its own ticks/title at the EXACT SAME pixel column the primary axis already occupied: both titles'
    /// rotated-text anchors landed on close to the same X, reading as interleaved digits/letters. Unlike the right
    /// side (nothing stands before its own slot 0), the left side's slot 0 always has the primary's band before it.</summary>
    [Fact]
    public void Render_LeftOutwardAxis_TitleStandsPastThePrimaryAxisOwnBand_NotOnTopOfIt()
    {
        var fig = Plt.Create().WithSize(600, 400).AddSubPlot(1, 1, 1, ax => ax
            .Plot([0.0, 10], [0.0, 10])
            .SetYLabel("Primary"))
            .Build();
        var leftSlot = fig.SubPlots[0].YAxes.Left.Add();
        leftSlot.AddSeries(new MatPlotLibNet.Models.Series.LineSeries([0.0, 10], [-50.0, -10.0]));
        leftSlot.Axis.Label = "LeftOuter";

        var svg = fig.ToSvg();

        double XOf(string label)
        {
            var title = svg.Elements("text").FirstOrDefault(text => text.Text == label && Math.Abs(text.Rotation() ?? 0) == 90 && text.RotationCentre() is not null);
            Assert.True(title.Attributes is not null, $"no rotated title found for '{label}' in:\n{svg}");
            return title.RotationCentre()!.Value.X;
        }

        double primaryX = XOf("Primary");
        double leftOuterX = XOf("LeftOuter");

        // Both titles are centred, rotated 90 degrees, growing further LEFT (smaller X) past the plot's edge — the
        // outward slot's title must stand clearly past (smaller X than) the primary's own, never at/near the same
        // column. The pre-fix code put them within ~1px of each other (the SAME column); require a real gap.
        Assert.True(primaryX - leftOuterX > 15.0,
            $"expected LeftOuter's title to stand well past Primary's (gap > 15px), got primary={primaryX}, leftOuter={leftOuterX}, gap={primaryX - leftOuterX}");
    }

    /// <summary>X major ticks hidden: no tick marks or labels are drawn for the X axis.
    /// Assert: hiding X ticks produces fewer &lt;text&gt; elements than the visible baseline.</summary>
    [Fact]
    public void Render_XMajorTicksHidden_SkipsXTickDraw()
    {
        var fig = Plt.Create()
            .WithSize(500, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2, 3], [100.0, 200, 300]))
            .Build();

        var svgVisible = fig.ToSvg();
        int textVisible = svgVisible.Split("<text").Length - 1;

        fig.SubPlots[0].XAxis.MajorTicks = fig.SubPlots[0].XAxis.MajorTicks with { Visible = false };
        var svgHidden = fig.ToSvg();
        int textHidden = svgHidden.Split("<text").Length - 1;

        // Hiding X ticks removes all X axis tick labels → fewer <text> elements
        Assert.True(textHidden < textVisible, $"Expected fewer <text> elements when X ticks hidden ({textHidden} < {textVisible})");
    }
}
