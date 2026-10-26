// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.CartesianParts;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.CartesianParts;

/// <summary>
/// Direct unit tests for <see cref="CartesianSecondaryYAxisPart"/> (Phase B.7).
/// Covers right-edge tick marks + labels, optional secondary axis label,
/// visible/hidden series, custom tick formatter.
/// </summary>
public class CartesianSecondaryYAxisPartUnitTests
{
    private static readonly Theme DefaultTheme = Theme.Default;
    private static readonly Rect Plot = new(0, 0, 400, 300);
    private static readonly DataRange Range = new(0, 10, 0, 100);
    private static DataTransform PrimaryTransform() => new(0, 10, 0, 10, Plot);

    private static CartesianSecondaryYAxisPart Build(Axes axes, IRenderContext ctx, Theme? theme = null)
    {
        var secondary = new DataTransform(Range.XMin, Range.XMax, Range.YMin, Range.YMax, Plot);
        var labels = new CartesianDirectLabelsPart(axes, Plot, ctx, theme ?? DefaultTheme, PrimaryTransform(), [secondary],
            new MinMaxRange(Range.XMin, Range.XMax), 0);
        return new(axes, Plot, ctx, theme ?? DefaultTheme, PrimaryTransform(), axes.RightYAxes[0], 0, 0, Range, secondary, labels);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // The group a secondary series is drawn in, and its end label inside it
    // ──────────────────────────────────────────────────────────────────────────

    private static System.Xml.Linq.XElement Parsed(MatPlotLibNet.Rendering.Svg.SvgRenderContext ctx) =>
        System.Xml.Linq.XElement.Parse("<root>" + ctx.GetOutput() + "</root>");

    [Fact]
    public void Render_WithInteractiveAttributes_DrawsEachSeriesInADataGroupKeyedByItsSlot()
    {
        var ax = new Axes { EnableInteractiveAttributes = true };
        ax.Plot([0.0, 10], [0.0, 10]);
        ax.Plot([0.0, 10], [10.0, 0]);
        ax.PlotSecondary([0.0, 10], [10.0, 90]).Label = "S";
        var ctx = new MatPlotLibNet.Rendering.Svg.SvgRenderContext();

        Build(ax, ctx).Render();

        var group = Assert.Single(Parsed(ctx).Elements("g"), g => (string?)g.Attribute("class") == "series");
        Assert.Equal("2", (string?)group.Attribute("data-series-index"));
        Assert.Equal("S", (string?)group.Attribute("aria-label"));
        Assert.Single(group.Descendants("polyline"));
    }

    [Fact]
    public void Render_DrawsASecondaryEndLabel_InsideTheGroupOfItsSeries()
    {
        var ax = new Axes { EnableInteractiveAttributes = true };
        ax.Plot([0.0, 10], [0.0, 10]);
        var line = ax.PlotSecondary([0.0, 10], [10.0, 90]);
        line.Label = "named";
        line.LabelPlacement = MatPlotLibNet.Models.Series.SeriesLabelPlacement.LineEnd;
        var ctx = new MatPlotLibNet.Rendering.Svg.SvgRenderContext();

        Build(ax, ctx).Render();

        var group = Assert.Single(Parsed(ctx).Elements("g"), g => (string?)g.Attribute("class") == "series");
        Assert.Equal("1", (string?)group.Attribute("data-series-index"));
        Assert.Single(group.Descendants("text"), t => t.Value == "named");
    }

    [Fact]
    public void Render_WithoutInteractiveAttributes_AnUnlabelledSeries_IsDrawnWithoutAGroup()
    {
        var ax = new Axes();
        ax.PlotSecondary([0.0, 10], [10.0, 90]);
        var ctx = new MatPlotLibNet.Rendering.Svg.SvgRenderContext();

        Build(ax, ctx).Render();

        Assert.DoesNotContain("class=\"series\"", ctx.GetOutput());
        Assert.Contains("<polyline", ctx.GetOutput());
    }

    // ──────────────────────────────────────────────────────────────────────────
    // The slot a secondary series is drawn in
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Render_SecondaryLineWearsTheCyclerColourOfTheSlotAfterThePrimarySeries()
    {
        var cycler = new PropCyclerBuilder().WithColors(Colors.Red, Colors.Green, Colors.Blue).Build();
        var theme = Theme.CreateFrom(Theme.Default).WithPropCycler(cycler).Build();
        var ax = new Axes();
        ax.Plot([0.0, 10], [0.0, 10]);
        ax.PlotSecondary([0.0, 10], [10.0, 90]);
        var ctx = new RecordingRenderContext();

        Build(ax, ctx, theme).Render();

        var line = Assert.Single(ctx.OfKind("DrawLines"));
        Assert.Equal(Colors.Green, (Color)((dynamic)line.Args!).color);
    }

    [Fact]
    public void Render_SecondarySeriesReadTheActiveTheme_notTheDefaultOne()
    {
        // A violin body takes Theme.ViolinBodyColor when the theme sets one; the secondary renderer used to fall
        // back to Theme.Default, which sets none.
        var body = Color.FromHex("#ABCDEF");
        var theme = new Theme("violet bodies", Theme.Default) { ViolinBodyColor = body };
        var ax = new Axes();
        ax.TwinX();
        ax.AddSecondarySeries(new MatPlotLibNet.Models.Series.ViolinSeries([[1.0, 2, 3, 4, 5]]));
        var ctx = new RecordingRenderContext();

        Build(ax, ctx, theme: theme).Render();

        Assert.Contains(ctx.OfKind("DrawPolygon"), c => (Color?)((dynamic)c.Args!).stroke == body);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Ticks + labels
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Render_DefaultAxes_DrawsRightEdgeTicks()
    {
        var ax = new Axes();
        ax.TwinX();
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.True(ctx.CountOf("DrawLine") >= 1);
        Assert.True(ctx.CountOf("DrawText") >= 1);
    }

    [Fact]
    public void Render_TicksPositionedAtRightEdge()
    {
        // The secondary axis draws by its own TickConfig, so its default mark is the primary axes' default: 3.5 pt
        // (4.861 px) outward, its inner end half a spine into the plot so it meets the right spine stroke.
        var ax = new Axes();
        ax.TwinX();
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        var defaults = new TickConfig();
        double half = ax.Spines.Right.LineWidth / 2;
        var tickLines = ctx.OfKind("DrawLine");
        Assert.NotEmpty(tickLines);
        foreach (var call in tickLines)
        {
            var p1 = (Point)((dynamic)call.Args!).p1;
            var p2 = (Point)((dynamic)call.Args!).p2;
            Assert.Equal(Plot.X + Plot.Width - half, p1.X, 6);
            Assert.Equal(Plot.X + Plot.Width + defaults.Length, p2.X, 6);
        }
    }

    [Fact]
    public void Render_DefaultTicks_TakeThePrimaryAxesDefaults()
    {
        // One rule for tick geometry: mark Length, Width and colour, label at Length + Pad beyond the edge, the
        // same as the primary Y axis' mirrored side. Before the lift this part drew 5 px / 1 px / label at +8.
        var ax = new Axes();
        ax.TwinX();
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        var defaults = new TickConfig();

        var marks = ctx.OfKind("DrawLine");
        var labels = ctx.OfKind("DrawText");
        Assert.NotEmpty(marks);
        Assert.Equal(marks.Count, labels.Count);
        double right = Plot.X + Plot.Width;
        for (int i = 0; i < marks.Count; i++)
        {
            dynamic mark = marks[i].Args!;
            dynamic label = labels[i].Args!;
            var p1 = (Point)mark.p1;
            var p2 = (Point)mark.p2;
            Assert.Equal(right + defaults.Length, p2.X, 6);
            Assert.Equal(defaults.Width, (double)mark.thickness, 6);
            Assert.Equal(DefaultTheme.ForegroundText, (Color)mark.color);
            var at = (Point)label.position;
            Assert.Equal(right + defaults.Length + defaults.Pad, at.X, 6);
            Assert.Equal(p1.Y + 4, at.Y, 6);
            Assert.Equal(TextAlignment.Left, (TextAlignment)label.alignment);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // The secondary axis honours its own TickConfig and LabelStyle
    // ──────────────────────────────────────────────────────────────────────────

    private static RecordingRenderContext RenderWith(Func<TickConfig, TickConfig> ticks, Action<Axis>? axis = null)
    {
        var ax = new Axes();
        ax.TwinX();
        ax.SecondaryYAxis!.MajorTicks = ticks(ax.SecondaryYAxis.MajorTicks);
        axis?.Invoke(ax.SecondaryYAxis);
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        return ctx;
    }

    private static double Right => Plot.X + Plot.Width;

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
    public void Render_HonoursTickLabelSize()
    {
        var ctx = RenderWith(t => t with { LabelSize = 17 });

        Assert.All(ctx.OfKind("DrawText"), c => Assert.Equal(17.0, ((Font)((dynamic)c.Args!).font).Size));
    }

    [Fact]
    public void Render_HonoursTickLength()
    {
        var ctx = RenderWith(t => t with { Length = 10 });
        var pad = new TickConfig().Pad;

        Assert.All(ctx.OfKind("DrawLine"), c => Assert.Equal(Right + 10, ((Point)((dynamic)c.Args!).p2).X, 6));
        Assert.All(ctx.OfKind("DrawText"), c => Assert.Equal(Right + 10 + pad, ((Point)((dynamic)c.Args!).position).X, 6));
    }

    [Fact]
    public void Render_HonoursTickPad()
    {
        var ctx = RenderWith(t => t with { Pad = 7 });
        var length = new TickConfig().Length;

        Assert.All(ctx.OfKind("DrawText"), c => Assert.Equal(Right + length + 7, ((Point)((dynamic)c.Args!).position).X, 6));
    }

    [Fact]
    public void Render_HonoursTickWidth()
    {
        var ctx = RenderWith(t => t with { Width = 2.5 });

        Assert.All(ctx.OfKind("DrawLine"), c => Assert.Equal(2.5, (double)((dynamic)c.Args!).thickness));
    }

    [Fact]
    public void Render_HonoursTickDirectionIn_markPointsIntoThePlot()
    {
        var ax = new Axes();
        ax.TwinX();
        ax.SecondaryYAxis!.MajorTicks = ax.SecondaryYAxis.MajorTicks with { Direction = TickDirection.In };
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        var length = new TickConfig().Length;
        double half = ax.Spines.Right.LineWidth / 2;

        Assert.All(ctx.OfKind("DrawLine"), c =>
        {
            dynamic a = c.Args!;
            Assert.Equal(Right - length, ((Point)a.p1).X, 6);
            Assert.Equal(Right + half, ((Point)a.p2).X, 6);
        });
    }

    [Fact]
    public void Render_HonoursTickLabelRotation()
    {
        var ctx = RenderWith(t => t with { LabelRotation = 45 });

        Assert.All(ctx.OfKind("DrawText"), c => Assert.Equal(45.0, (double)((dynamic)c.Args!).rotation));
    }

    [Fact]
    public void Render_HonoursTicksVisibleFalse_noMarkNoLabel_andTheAxisLabelFallsBackToItsFixedOffset()
    {
        var ctx = RenderWith(t => t with { Visible = false }, axis => axis.Label = "Right axis");

        Assert.Equal(0, ctx.CountOf("DrawLine"));
        var only = Assert.Single(ctx.OfKind("DrawText"));
        dynamic a = only.Args!;
        Assert.Equal("Right axis", (string)a.text);
        Assert.Equal(Right + 45, ((Point)a.position).X, 6);
    }

    [Fact]
    public void Render_AxisLabel_ClearsTheWidestTickLabel_byTheTickGeometry()
    {
        var ctx = RenderWith(t => t, axis => axis.Label = "Right axis");
        var defaults = new TickConfig();
        var tickTexts = ctx.OfKind("DrawText").Where(c => (string)((dynamic)c.Args!).text != "Right axis").ToList();
        double widest = tickTexts.Max(c =>
        {
            dynamic a = c.Args!;
            return ctx.MeasureText((string)a.text, (Font)a.font).Width;
        });

        dynamic label = ctx.OfKind("DrawText").Single(c => (string)((dynamic)c.Args!).text == "Right axis").Args!;
        Assert.Equal(Right + defaults.Length + defaults.Pad + widest + 12, ((Point)label.position).X, 6);
        Assert.Equal(90.0, (double)label.rotation);
    }

    [Fact]
    public void Render_HonoursLabelStyle_onTheAxisTitle()
    {
        var ctx = RenderWith(t => t with { Visible = false }, axis =>
        {
            axis.Label = "Right axis";
            axis.LabelStyle = new TextStyle { Color = Colors.Green, FontSize = 19, Pad = 6 };
        });

        dynamic label = Assert.Single(ctx.OfKind("DrawText")).Args!;
        var font = (Font)label.font;
        Assert.Equal(Colors.Green, font.Color);
        Assert.Equal(19.0, font.Size);
        Assert.Equal(Right + 45 + 6, ((Point)label.position).X, 6);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Axis label
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Render_WithLabel_EmitsExtraText()
    {
        var ax = new Axes();
        ax.TwinX();
        ax.SecondaryYAxis!.Label = "Right axis";
        var ctx = new RecordingRenderContext();
        int noLabelCount = 0;
        {
            var ctxNo = new RecordingRenderContext();
            var axNo = new Axes(); axNo.TwinX();
            Build(axNo, ctxNo).Render();
            noLabelCount = ctxNo.CountOf("DrawText");
        }
        Build(ax, ctx).Render();
        Assert.Equal(noLabelCount + 1, ctx.CountOf("DrawText"));
        Assert.Contains(ctx.OfKind("DrawText"),
            c => (string)((dynamic)c.Args!).text == "Right axis");
    }

    [Fact]
    public void Render_NullLabel_NoExtraText()
    {
        var ax = new Axes();
        ax.TwinX();
        ax.SecondaryYAxis!.Label = null;
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        // Only tick labels, no axis label
        Assert.DoesNotContain(ctx.OfKind("DrawText"),
            c => (string)((dynamic)c.Args!).text == "");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Secondary series rendering
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Render_WithVisibleSecondarySeries_DrawsSeries()
    {
        var ax = new Axes();
        ax.PlotSecondary([0.0, 5, 10], [10.0, 50, 90]);
        var ctxBefore = new RecordingRenderContext();
        Build(new Axes { }.Also(a => a.TwinX()), ctxBefore).Render();
        int beforeCalls = ctxBefore.Calls.Count;

        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.True(ctx.Calls.Count > beforeCalls, "Secondary series should produce more draw calls");
    }

    [Fact]
    public void Render_HiddenSecondarySeries_SkippedSilently()
    {
        var ax = new Axes();
        var s = ax.PlotSecondary([0.0, 5, 10], [10.0, 50, 90]);
        s.Visible = false;
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        // With the series hidden, only axis ticks are drawn (no series lines).
        // Verify no DrawLines (plural) call for the hidden series.
        Assert.Equal(0, ctx.CountOf("DrawLines"));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Tick formatter
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Render_CustomTickFormatter_AppliedToLabels()
    {
        var ax = new Axes();
        ax.TwinX();
        ax.SecondaryYAxis!.TickFormatter = new PercentFormatter(100);
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        Assert.Contains(ctx.OfKind("DrawText"),
            c => ((string)((dynamic)c.Args!).text).Contains('%'));
    }

    [Fact]
    public void Render_NullTickFormatter_UsesUniformFormat()
    {
        var ax = new Axes();
        ax.TwinX();
        ax.SecondaryYAxis!.TickFormatter = null;
        var ctx = new RecordingRenderContext();
        Build(ax, ctx).Render();
        // Some numeric-looking string for each tick; just verify no % sign.
        foreach (var c in ctx.OfKind("DrawText"))
            Assert.DoesNotContain('%', (string)((dynamic)c.Args!).text);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // A second, outward-stacked right axis (ROADMAP.md row 101): its own offset, its own identity slot
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Render_WithAnOutwardOffset_DrawsItsTicksPastThePlotEdgeByThatOffset()
    {
        var ax = new Axes();
        var slot1 = ax.AddOutwardYAxis(AxisSide.Right);
        slot1.AddSeries(new MatPlotLibNet.Models.Series.LineSeries([0.0, 10], [0.0, 100]));
        var secondary = new DataTransform(Range.XMin, Range.XMax, Range.YMin, Range.YMax, Plot);
        var labels = new CartesianDirectLabelsPart(ax, Plot, new RecordingRenderContext(), DefaultTheme, PrimaryTransform(),
            [secondary], new MinMaxRange(Range.XMin, Range.XMax), 0);
        var ctx = new RecordingRenderContext();
        const double outwardOffset = 37.5;

        new CartesianSecondaryYAxisPart(ax, Plot, ctx, DefaultTheme, PrimaryTransform(), slot1, 0, outwardOffset,
            Range, secondary, labels).Render();

        // p2 is the tick's outer end: AxisEdge (right + outwardOffset) + the default tick length, unaffected by the
        // spine-half adjustment p1 carries (Render_TicksPositionedAtRightEdge covers that one, at offset 0).
        var right = Plot.X + Plot.Width;
        var length = new TickConfig().Length;
        Assert.NotEmpty(ctx.OfKind("DrawLine"));
        Assert.All(ctx.OfKind("DrawLine"), c =>
            Assert.Equal(right + outwardOffset + length, ((Point)((dynamic)c.Args!).p2).X, 6));
    }

    [Fact]
    public void Render_SecondSlot_UsesTheRunningIndexPastEverySeriesBeforeIt_ForItsColourSlot()
    {
        var cycler = new PropCyclerBuilder().WithColors(Colors.Red, Colors.Green, Colors.Blue, Colors.Amber).Build();
        var theme = Theme.CreateFrom(Theme.Default).WithPropCycler(cycler).Build();
        var ax = new Axes();
        ax.Plot([0.0, 10], [0.0, 10]); // primary: colour slot 0 (Red)
        ax.PlotSecondary([0.0, 10], [10.0, 90]); // right slot 0's only series: colour slot 1 (Green)
        var slot1 = ax.AddOutwardYAxis(AxisSide.Right);
        slot1.AddSeries(new MatPlotLibNet.Models.Series.LineSeries([0.0, 10], [0.0, 100])); // right slot 1's series: colour slot 2 (Blue)

        var secondary = new DataTransform(Range.XMin, Range.XMax, Range.YMin, Range.YMax, Plot);
        var labels = new CartesianDirectLabelsPart(ax, Plot, new RecordingRenderContext(), theme, PrimaryTransform(),
            [secondary], new MinMaxRange(Range.XMin, Range.XMax), 0);
        var ctx = new RecordingRenderContext();

        // startingIndexInPair = 1: one right-side series (slot 0's) stands before slot 1's own.
        new CartesianSecondaryYAxisPart(ax, Plot, ctx, theme, PrimaryTransform(), slot1, 1, 0, Range, secondary, labels).Render();

        var line = Assert.Single(ctx.OfKind("DrawLines"));
        Assert.Equal(Colors.Blue, (Color)((dynamic)line.Args!).color);
    }
}

internal static class AxesExtensionsForTests
{
    public static Axes Also(this Axes axes, Action<Axes> configure)
    {
        configure(axes);
        return axes;
    }
}
