// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.CartesianParts;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.CartesianParts;

/// <summary>
/// Direct unit tests for <see cref="CartesianSecondaryXAxisPart"/> (Phase B.8).
/// Covers top-edge tick marks + labels, optional secondary X axis label,
/// visible/hidden series, custom tick formatter.
/// </summary>
public class CartesianSecondaryXAxisPartUnitTests
{
    private static readonly Theme DefaultTheme = Theme.Default;
    private static readonly Rect Plot = new(0, 0, 400, 300);
    private static readonly DataRange Range = new(0, 100, 0, 10);
    private static DataTransform PrimaryTransform() => new(0, 10, 0, 10, Plot);

    private static CartesianSecondaryXAxisPart Build(Axes axes, IRenderContext ctx, Theme? theme = null) =>
        new(axes, Plot, ctx, theme ?? DefaultTheme, PrimaryTransform(), Range);

    [Fact]
    public void Render_WithInteractiveAttributes_DrawsEachSeriesInADataGroupKeyedByItsSlot()
    {
        // Slots run primary, then secondary Y, then secondary X: one of each puts the top-axis line in slot 2.
        var ax = new Axes { EnableInteractiveAttributes = true };
        ax.Plot([0.0, 10], [0.0, 10]);
        ax.YAxes.Right.FirstOrAdd().AddSeries(new MatPlotLibNet.Models.Series.LineSeries([0.0, 10], [10.0, 90]));
        ax.PlotXSecondary([0.0, 100], [0.0, 10]).Label = "top";
        var ctx = new MatPlotLibNet.Rendering.Svg.SvgRenderContext();

        Build(ax, ctx).Render();

        var svg = System.Xml.Linq.XElement.Parse("<root>" + ctx.GetOutput() + "</root>");
        var group = Assert.Single(svg.Elements("g"), g => (string?)g.Attribute("class") == "series");
        Assert.Equal("2", (string?)group.Attribute("data-series-index"));
        Assert.Equal("top", (string?)group.Attribute("aria-label"));
        Assert.Single(group.Descendants("polyline"));
    }

    [Fact]
    public void Render_WithoutInteractiveAttributes_AnUnlabelledSeries_IsDrawnWithoutAGroup()
    {
        var ax = new Axes();
        ax.PlotXSecondary([0.0, 100], [0.0, 10]);
        var ctx = new MatPlotLibNet.Rendering.Svg.SvgRenderContext();

        Build(ax, ctx).Render();

        Assert.DoesNotContain("class=\"series\"", ctx.GetOutput());
        Assert.Contains("<polyline", ctx.GetOutput());
    }

    [Fact]
    public void Render_TopAxisLineWearsTheCyclerColourOfTheSlotAfterBothOtherLists()
    {
        // Slots run primary, then secondary Y, then secondary X: one of each puts the top-axis line in slot 2.
        var cycler = new PropCyclerBuilder().WithColors(Colors.Red, Colors.Green, Colors.Blue).Build();
        var theme = Theme.CreateFrom(Theme.Default).WithPropCycler(cycler).Build();
        var ax = new Axes();
        ax.Plot([0.0, 10], [0.0, 10]);
        ax.YAxes.Right.FirstOrAdd().AddSeries(new MatPlotLibNet.Models.Series.LineSeries([0.0, 10], [10.0, 90]));
        ax.PlotXSecondary([0.0, 100], [0.0, 10]);
        var ctx = new RecordingRenderContext();

        Build(ax, ctx, theme).Render();

        var line = Assert.Single(ctx.OfKind("DrawLines"));
        Assert.Equal(Colors.Blue, (Color)((dynamic)line.Args!).color);
    }

    [Fact]
    public void Render_DefaultAxes_DrawsTopEdgeTicks()
    {
        var ax = new Axes();
        ax.TwinY();
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.True(ctx.CountOf("DrawLine") >= 1);
        Assert.True(ctx.CountOf("DrawText") >= 1);
    }

    [Fact]
    public void Render_TicksPositionedAtTopEdge()
    {
        var ax = new Axes();
        ax.TwinY();
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        // The top axis draws by its own TickConfig: the default mark runs 3.5 pt (4.861 px) up from the edge, its
        // inner end half a spine into the plot so it meets the top spine stroke.
        var defaults = new TickConfig();
        double half = ax.Spines.Top.LineWidth / 2;
        Assert.NotEmpty(ctx.OfKind("DrawLine"));
        foreach (var call in ctx.OfKind("DrawLine"))
        {
            var p1 = (Point)((dynamic)call.Args!).p1;
            var p2 = (Point)((dynamic)call.Args!).p2;
            Assert.Equal(Plot.Y - defaults.Length, p1.Y, 6);
            Assert.Equal(Plot.Y + half, p2.Y, 6);
        }
    }

    [Fact]
    public void Render_DefaultTicks_TakeThePrimaryAxesDefaults()
    {
        // One rule for tick geometry: before the lift this part drew 5 px / 1 px marks and labels at -8.
        var ax = new Axes();
        ax.TwinY();
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        var defaults = new TickConfig();

        var marks = ctx.OfKind("DrawLine");
        var labels = ctx.OfKind("DrawText");
        Assert.NotEmpty(marks);
        Assert.Equal(marks.Count, labels.Count);
        double top = Plot.Y;
        for (int i = 0; i < marks.Count; i++)
        {
            dynamic mark = marks[i].Args!;
            dynamic label = labels[i].Args!;
            var p1 = (Point)mark.p1;
            Assert.Equal(top - defaults.Length, p1.Y, 6);
            Assert.Equal(defaults.Width, (double)mark.thickness, 6);
            Assert.Equal(DefaultTheme.ForegroundText, (Color)mark.color);
            var at = (Point)label.position;
            Assert.Equal(p1.X, at.X, 6);
            Assert.Equal(top - defaults.Length - defaults.Pad, at.Y, 6);
            Assert.Equal(TextAlignment.Center, (TextAlignment)label.alignment);
        }
    }

    // ── The top axis honours its own TickConfig and LabelStyle ──────────────────

    private static RecordingRenderContext RenderWith(Func<TickConfig, TickConfig> ticks, Action<Axis>? axis = null)
    {
        var ax = new Axes();
        ax.TwinY();
        ax.SecondaryXAxis!.MajorTicks = ticks(ax.SecondaryXAxis.MajorTicks);
        axis?.Invoke(ax.SecondaryXAxis);
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        return ctx;
    }

    [Fact]
    public void Render_HonoursTickColor()
    {
        var ctx = RenderWith(t => t with { Color = Colors.Red });

        Assert.NotEmpty(ctx.OfKind("DrawLine"));
        Assert.All(ctx.OfKind("DrawLine"), c => Assert.Equal(Colors.Red, (Color)((dynamic)c.Args!).color));
    }

    [Fact]
    public void Render_HonoursTickLabelColor()
    {
        var ctx = RenderWith(t => t with { LabelColor = Colors.Blue });

        Assert.NotEmpty(ctx.OfKind("DrawText"));
        Assert.All(ctx.OfKind("DrawText"), c => Assert.Equal(Colors.Blue, ((Font)((dynamic)c.Args!).font).Color));
    }

    [Fact]
    public void Render_HonoursTickLength()
    {
        var ctx = RenderWith(t => t with { Length = 10 });
        var pad = new TickConfig().Pad;

        Assert.All(ctx.OfKind("DrawLine"), c => Assert.Equal(Plot.Y - 10, ((Point)((dynamic)c.Args!).p1).Y, 6));
        Assert.All(ctx.OfKind("DrawText"), c => Assert.Equal(Plot.Y - 10 - pad, ((Point)((dynamic)c.Args!).position).Y, 6));
    }

    [Fact]
    public void Render_HonoursTickPad()
    {
        var ctx = RenderWith(t => t with { Pad = 7 });
        var length = new TickConfig().Length;

        Assert.All(ctx.OfKind("DrawText"), c => Assert.Equal(Plot.Y - length - 7, ((Point)((dynamic)c.Args!).position).Y, 6));
    }

    [Fact]
    public void Render_HonoursTickWidth()
    {
        var ctx = RenderWith(t => t with { Width = 2.5 });

        Assert.All(ctx.OfKind("DrawLine"), c => Assert.Equal(2.5, (double)((dynamic)c.Args!).thickness));
    }

    [Fact]
    public void Render_HonoursTickLabelRotation_anchoringTheLabelAtItsTickSoItRisesAwayFromTheAxis()
    {
        var ctx = RenderWith(t => t with { LabelRotation = 30 });

        Assert.All(ctx.OfKind("DrawText"), c =>
        {
            dynamic a = c.Args!;
            Assert.Equal(30.0, (double)a.rotation);
            Assert.Equal(TextAlignment.Left, (TextAlignment)a.alignment);
        });
    }

    [Fact]
    public void Render_HonoursTicksVisibleFalse_noMarkNoLabel()
    {
        var ctx = RenderWith(t => t with { Visible = false }, axis => axis.Label = "Top axis");

        Assert.Equal(0, ctx.CountOf("DrawLine"));
        dynamic a = Assert.Single(ctx.OfKind("DrawText")).Args!;
        Assert.Equal("Top axis", (string)a.text);
        // No tick label is drawn, so the title stands the 6 px gap and its descent (a fifth of its size) above the edge.
        Assert.Equal(Plot.Y - (6 + 0.2 * ((Font)a.font).Size), ((Point)a.position).Y, 6);
    }

    [Fact]
    public void Render_HonoursLabelStyle_onTheAxisTitle()
    {
        var ctx = RenderWith(t => t with { Visible = false }, axis =>
        {
            axis.Label = "Top axis";
            axis.LabelStyle = new TextStyle { Color = Colors.Green, FontSize = 19, Pad = 6 };
        });

        dynamic label = Assert.Single(ctx.OfKind("DrawText")).Args!;
        var font = (Font)label.font;
        Assert.Equal(Colors.Green, font.Color);
        Assert.Equal(19.0, font.Size);
        Assert.Equal(Plot.Y - (6 + 0.2 * 19) - 6, ((Point)label.position).Y, 6);
    }

    // ── The band the part draws is the band it measures ─────────────────────────

    [Theory]
    [InlineData(null, true)]
    [InlineData("Top axis", true)]
    [InlineData("Top axis", false)]
    public void Band_IsWhatRenderDrew_AndWhatMeasureBandMeasuresBeforehand(string? label, bool ticksVisible)
    {
        var ax = new Axes();
        ax.PlotXSecondary([0.0, 50, 100], [1.0, 5, 9]);
        ax.SecondaryXAxis!.Label = label;
        ax.SecondaryXAxis.LabelStyle = new TextStyle { Pad = 2, FontSize = 15 };
        ax.SecondaryXAxis.MajorTicks = ax.SecondaryXAxis.MajorTicks with { Visible = ticksVisible, LabelSize = 16 };
        var ctx = new RecordingRenderContext();

        double measured = CartesianSecondaryXAxisPart.MeasureBand(ax, ctx, DefaultTheme, Range);
        var part = Build(ax, ctx);
        part.Render();

        // From the edge to the top of what was drawn: the title when there is one, the tallest tick label otherwise.
        var tops = ctx.OfKind("DrawText").Select(c =>
        {
            dynamic a = c.Args!;
            return ((Point)a.position).Y - ctx.MeasureText((string)a.text, (Font)a.font).Height;
        }).ToList();
        Assert.NotEmpty(tops);
        Assert.Equal(Plot.Y - tops.Min(), part.Band, 6);
        Assert.Equal(part.Band, measured, 9);
    }

    [Fact]
    public void Band_IsZero_WhenNothingIsDrawnAboveTheEdge()
    {
        var ax = new Axes();
        ax.PlotXSecondary([0.0, 50, 100], [1.0, 5, 9]);
        ax.SecondaryXAxis!.MajorTicks = ax.SecondaryXAxis.MajorTicks with { Visible = false };
        var ctx = new RecordingRenderContext();

        var part = Build(ax, ctx);
        part.Render();

        Assert.Equal(0, part.Band);
        Assert.Equal(0, CartesianSecondaryXAxisPart.MeasureBand(ax, ctx, DefaultTheme, Range));
    }

    [Fact]
    public void Render_WithLabel_EmitsExtraText()
    {
        var ax = new Axes();
        ax.TwinY();
        ax.SecondaryXAxis!.Label = "Top axis";
        var ctx = new RecordingRenderContext();

        var axNo = new Axes(); axNo.TwinY();
        var ctxNo = new RecordingRenderContext();
        Build(axNo, ctxNo).Render();

        Build(ax, ctx).Render();
        Assert.Equal(ctxNo.CountOf("DrawText") + 1, ctx.CountOf("DrawText"));
        Assert.Contains(ctx.OfKind("DrawText"),
            c => (string)((dynamic)c.Args!).text == "Top axis");
    }

    [Fact]
    public void Render_NullLabel_NoExtraText()
    {
        var ax = new Axes();
        ax.TwinY();
        ax.SecondaryXAxis!.Label = null;
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.DoesNotContain(ctx.OfKind("DrawText"),
            c => (string)((dynamic)c.Args!).text == "");
    }

    [Fact]
    public void Render_WithVisibleSecondarySeries_DrawsSeries()
    {
        var ax = new Axes();
        ax.PlotXSecondary([0.0, 50, 100], [1.0, 5, 9]);

        var axNo = new Axes(); axNo.TwinY();
        var ctxNo = new RecordingRenderContext();
        Build(axNo, ctxNo).Render();

        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.True(ctx.Calls.Count > ctxNo.Calls.Count);
    }

    [Fact]
    public void Render_HiddenSecondarySeries_SkippedSilently()
    {
        var ax = new Axes();
        var s = ax.PlotXSecondary([0.0, 50, 100], [1.0, 5, 9]);
        s.Visible = false;
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Equal(0, ctx.CountOf("DrawLines"));
    }

    [Fact]
    public void Render_CustomTickFormatter_AppliedToLabels()
    {
        var ax = new Axes();
        ax.TwinY();
        ax.SecondaryXAxis!.TickFormatter = new PercentFormatter(100);
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Contains(ctx.OfKind("DrawText"),
            c => ((string)((dynamic)c.Args!).text).Contains('%'));
    }

    [Fact]
    public void Render_NullTickFormatter_UsesUniformFormat()
    {
        var ax = new Axes();
        ax.TwinY();
        ax.SecondaryXAxis!.TickFormatter = null;
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        foreach (var c in ctx.OfKind("DrawText"))
            Assert.DoesNotContain('%', (string)((dynamic)c.Args!).text);
    }
}
