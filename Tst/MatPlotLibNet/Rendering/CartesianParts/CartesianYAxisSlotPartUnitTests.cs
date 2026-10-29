// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.CartesianParts;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.CartesianParts;

/// <summary>Direct unit tests for <see cref="CartesianYAxisSlotPart"/>, the one part that draws every Y axis of the
/// container: a slot's series in their own groups and colours, its tick marks and labels by its own
/// <see cref="TickConfig"/>, its title by the one band rule, on either side and at any offset.</summary>
public class CartesianYAxisSlotPartUnitTests
{
    private static readonly Theme DefaultTheme = Theme.Default;
    private static readonly Rect Plot = new(100, 0, 400, 300);
    private static readonly DataRange Range = new(0, 10, 0, 100);
    private static double Right => Plot.X + Plot.Width;

    /// <summary>The primary as a render places it over <see cref="Range"/>'s X extent.</summary>
    private static PlacedYSlot PlacedPrimary(Axes axes) => new()
    {
        Slot = axes.YAxes.Left[0],
        Range = new DataRange(0, 10, 0, 10),
        Transform = new DataTransform(0, 10, 0, 10, Plot),
        Ticks = [],
        Source = default,
    };

    /// <summary><paramref name="slot"/> placed over <see cref="Range"/>, its series numbered as the series passes number
    /// them, <paramref name="offset"/> out past the plot's edge on its side.</summary>
    private static PlacedYSlot Placed(Axes axes, YAxisSlot slot, double offset = 0)
    {
        var seat = axes.YSlotSeats().Single(s => ReferenceEquals(s.Slot, slot));
        return new PlacedYSlot
        {
            Slot = slot,
            Range = Range,
            Transform = new DataTransform(Range.XMin, Range.XMax, Range.YMin, Range.YMax, Plot),
            Ticks = slot.Axis.SlotTickValues(Range, AxesRenderer.FullTickDensityLength),
            Source = slot.Axis.SlotTickSource(),
            Pair = seat.Pair,
            FirstIndexInPair = seat.FirstIndexInPair,
            Offset = offset,
        };
    }

    /// <summary>The part for <paramref name="slot"/>, drawing its series' end labels with the end labels of
    /// <paramref name="axes"/>.</summary>
    private static CartesianYAxisSlotPart Part(Axes axes, YAxisSlot slot, IRenderContext ctx, Theme? theme = null) =>
        Build(axes, Placed(axes, slot), ctx, theme ?? DefaultTheme);

    /// <summary>The part for <paramref name="slot"/> pushed <paramref name="offset"/> past the plot's edge on its side.</summary>
    private static CartesianYAxisSlotPart PartAt(Axes axes, YAxisSlot slot, IRenderContext ctx, double offset) =>
        Build(axes, Placed(axes, slot, offset), ctx, DefaultTheme);

    private static CartesianYAxisSlotPart Build(Axes axes, PlacedYSlot placed, IRenderContext ctx, Theme theme)
    {
        var context = new CartesianPartContext(axes, Plot, ctx, theme);
        var labels = new CartesianDirectLabelsPart(context, [PlacedPrimary(axes), placed], 0);
        return new CartesianYAxisSlotPart(context, placed, labels.DrawLabelOf);
    }

    private static YAxisSlot RightSlot(Axes axes) => axes.YAxes.Right.FirstOrAdd();

    private static LineSeries Line(YAxisSlot slot, double[] x, double[] y) => slot.AddSeries(new LineSeries(x, y));

    private static System.Xml.Linq.XElement Parsed(MatPlotLibNet.Rendering.Svg.SvgRenderContext ctx) =>
        System.Xml.Linq.XElement.Parse("<root>" + ctx.GetOutput() + "</root>");

    // ──────────────────────────────────────────────────────────────────────────
    // The group a slot's series is drawn in, and its end label inside it
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void EachSeries_IsDrawnInADataGroupKeyedByItsIdentitySlot()
    {
        var ax = new Axes { EnableInteractiveAttributes = true };
        ax.Plot([0.0, 10], [0.0, 10]);
        ax.Plot([0.0, 10], [10.0, 0]);
        Line(RightSlot(ax), [0.0, 10], [10.0, 90]).Label = "S";
        var ctx = new MatPlotLibNet.Rendering.Svg.SvgRenderContext();

        Part(ax, RightSlot(ax), ctx).Render();

        var group = Assert.Single(Parsed(ctx).Elements("g"), g => (string?)g.Attribute("class") == "series");
        Assert.Equal("2", (string?)group.Attribute("data-series-index"));
        Assert.Equal("S", (string?)group.Attribute("aria-label"));
        Assert.Single(group.Descendants("polyline"));
    }

    [Fact]
    public void ASlotsEndLabel_IsDrawnInsideTheGroupOfItsSeries()
    {
        var ax = new Axes { EnableInteractiveAttributes = true };
        ax.Plot([0.0, 10], [0.0, 10]);
        var line = Line(RightSlot(ax), [0.0, 10], [10.0, 90]);
        line.Label = "named";
        line.LabelPlacement = SeriesLabelPlacement.LineEnd;
        var ctx = new MatPlotLibNet.Rendering.Svg.SvgRenderContext();

        Part(ax, RightSlot(ax), ctx).Render();

        var group = Assert.Single(Parsed(ctx).Elements("g"), g => (string?)g.Attribute("class") == "series");
        Assert.Equal("1", (string?)group.Attribute("data-series-index"));
        Assert.Single(group.Descendants("text"), t => t.Value == "named");
    }

    [Fact]
    public void WithoutInteractiveAttributes_AnUnlabelledSeries_IsDrawnWithoutAGroup()
    {
        var ax = new Axes();
        Line(RightSlot(ax), [0.0, 10], [10.0, 90]);
        var ctx = new MatPlotLibNet.Rendering.Svg.SvgRenderContext();

        Part(ax, RightSlot(ax), ctx).Render();

        Assert.DoesNotContain("class=\"series\"", ctx.GetOutput());
        Assert.Contains("<polyline", ctx.GetOutput());
    }

    // ──────────────────────────────────────────────────────────────────────────
    // The colour slot a slot's series is drawn in
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ASlotsLine_WearsTheCyclerColourOfTheSlotAfterThePrimarySeries()
    {
        var cycler = new PropCyclerBuilder().WithColors(Colors.Red, Colors.Green, Colors.Blue).Build();
        var theme = Theme.CreateFrom(Theme.Default).WithPropCycler(cycler).Build();
        var ax = new Axes();
        ax.Plot([0.0, 10], [0.0, 10]);
        Line(RightSlot(ax), [0.0, 10], [10.0, 90]);
        var ctx = new RecordingRenderContext();

        Part(ax, RightSlot(ax), ctx, theme).Render();

        var line = Assert.Single(ctx.OfKind("DrawLines"));
        Assert.Equal(Colors.Green, (Color)((dynamic)line.Args!).color);
    }

    [Fact]
    public void ASlotsSeries_ReadTheActiveTheme_NotTheDefaultOne()
    {
        // A violin body takes Theme.ViolinBodyColor when the theme sets one; a slot's series renderer used to fall back
        // to Theme.Default, which sets none.
        var body = Color.FromHex("#ABCDEF");
        var theme = new Theme("violet bodies", Theme.Default) { ViolinBodyColor = body };
        var ax = new Axes();
        RightSlot(ax).AddSeries(new ViolinSeries([[1.0, 2, 3, 4, 5]]));
        var ctx = new RecordingRenderContext();

        Part(ax, RightSlot(ax), ctx, theme).Render();

        Assert.Contains(ctx.OfKind("DrawPolygon"), c => (Color?)((dynamic)c.Args!).stroke == body);
    }

    [Fact]
    public void ASecondRightSlot_TakesTheColourSlotPastEverySeriesBeforeIt()
    {
        var cycler = new PropCyclerBuilder().WithColors(Colors.Red, Colors.Green, Colors.Blue, Colors.Amber).Build();
        var theme = Theme.CreateFrom(Theme.Default).WithPropCycler(cycler).Build();
        var ax = new Axes();
        ax.Plot([0.0, 10], [0.0, 10]);                       // primary: colour slot 0 (Red)
        Line(RightSlot(ax), [0.0, 10], [10.0, 90]);           // right slot 0's only series: colour slot 1 (Green)
        var slot1 = ax.YAxes.Right.Add();
        Line(slot1, [0.0, 10], [0.0, 100]);                   // right slot 1's series: colour slot 2 (Blue)
        var ctx = new RecordingRenderContext();

        Part(ax, slot1, ctx, theme).Render();

        var line = Assert.Single(ctx.OfKind("DrawLines"));
        Assert.Equal(Colors.Blue, (Color)((dynamic)line.Args!).color);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Ticks + labels
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ARightSlot_DrawsTickMarksAndLabels()
    {
        var ax = new Axes();
        var ctx = new RecordingRenderContext();

        Part(ax, RightSlot(ax), ctx).Render();

        Assert.True(ctx.CountOf("DrawLine") >= 1);
        Assert.True(ctx.CountOf("DrawText") >= 1);
    }

    [Fact]
    public void ARightSlotsTicks_StandOnTheRightEdge_MeetingTheSpine()
    {
        // A slot draws by its own TickConfig, so its default mark is the primary axes' default: 3.5 pt (4.861 px)
        // outward, its inner end half a spine into the plot so it meets the right spine stroke.
        var ax = new Axes();
        var ctx = new RecordingRenderContext();

        Part(ax, RightSlot(ax), ctx).Render();

        var defaults = new TickConfig();
        double half = ax.Spines.Right.LineWidth / 2;
        var tickLines = ctx.OfKind("DrawLine");
        Assert.NotEmpty(tickLines);
        foreach (var call in tickLines)
        {
            var p1 = (Point)((dynamic)call.Args!).p1;
            var p2 = (Point)((dynamic)call.Args!).p2;
            Assert.Equal(Right - half, p1.X, 6);
            Assert.Equal(Right + defaults.Length, p2.X, 6);
        }
    }

    [Fact]
    public void ASlotsDefaultTicks_TakeThePrimaryAxesDefaults()
    {
        // One rule for tick geometry: mark Length, Width and colour, label at Length + Pad beyond the edge, the same as
        // the primary Y axis' mirrored side.
        var ax = new Axes();
        var ctx = new RecordingRenderContext();

        Part(ax, RightSlot(ax), ctx).Render();

        var defaults = new TickConfig();
        var marks = ctx.OfKind("DrawLine");
        var labels = ctx.OfKind("DrawText");
        Assert.NotEmpty(marks);
        Assert.Equal(marks.Count, labels.Count);
        for (int i = 0; i < marks.Count; i++)
        {
            dynamic mark = marks[i].Args!;
            dynamic label = labels[i].Args!;
            var p1 = (Point)mark.p1;
            var p2 = (Point)mark.p2;
            Assert.Equal(Right + defaults.Length, p2.X, 6);
            Assert.Equal(defaults.Width, (double)mark.thickness, 6);
            Assert.Equal(DefaultTheme.ForegroundText, (Color)mark.color);
            var at = (Point)label.position;
            Assert.Equal(Right + defaults.Length + defaults.Pad, at.X, 6);
            Assert.Equal(p1.Y + 4, at.Y, 6);
            Assert.Equal(TextAlignment.Left, (TextAlignment)label.alignment);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // A slot honours its own TickConfig and LabelStyle
    // ──────────────────────────────────────────────────────────────────────────

    private static RecordingRenderContext RenderWith(Func<TickConfig, TickConfig> ticks, Action<Axis>? axis = null)
    {
        var ax = new Axes();
        var slot = RightSlot(ax);
        slot.Axis.MajorTicks = ticks(slot.Axis.MajorTicks);
        axis?.Invoke(slot.Axis);
        var ctx = new RecordingRenderContext();
        Part(ax, slot, ctx).Render();
        return ctx;
    }

    [Fact]
    public void ASlot_HonoursItsTickColor()
    {
        var ctx = RenderWith(t => t with { Color = Colors.Red });

        Assert.NotEmpty(ctx.OfKind("DrawLine"));
        Assert.All(ctx.OfKind("DrawLine"), c => Assert.Equal(Colors.Red, (Color)((dynamic)c.Args!).color));
    }

    [Fact]
    public void ASlot_HonoursItsTickLabelColor()
    {
        var ctx = RenderWith(t => t with { LabelColor = Colors.Blue });

        Assert.NotEmpty(ctx.OfKind("DrawText"));
        Assert.All(ctx.OfKind("DrawText"), c => Assert.Equal(Colors.Blue, ((Font)((dynamic)c.Args!).font).Color));
    }

    [Fact]
    public void ASlot_HonoursItsTickLabelSize()
    {
        var ctx = RenderWith(t => t with { LabelSize = 17 });

        Assert.All(ctx.OfKind("DrawText"), c => Assert.Equal(17.0, ((Font)((dynamic)c.Args!).font).Size));
    }

    [Fact]
    public void ASlot_HonoursItsTickLength()
    {
        var ctx = RenderWith(t => t with { Length = 10 });
        var pad = new TickConfig().Pad;

        Assert.All(ctx.OfKind("DrawLine"), c => Assert.Equal(Right + 10, ((Point)((dynamic)c.Args!).p2).X, 6));
        Assert.All(ctx.OfKind("DrawText"), c => Assert.Equal(Right + 10 + pad, ((Point)((dynamic)c.Args!).position).X, 6));
    }

    [Fact]
    public void ASlot_HonoursItsTickPad()
    {
        var ctx = RenderWith(t => t with { Pad = 7 });
        var length = new TickConfig().Length;

        Assert.All(ctx.OfKind("DrawText"), c => Assert.Equal(Right + length + 7, ((Point)((dynamic)c.Args!).position).X, 6));
    }

    [Fact]
    public void ASlot_HonoursItsTickWidth()
    {
        var ctx = RenderWith(t => t with { Width = 2.5 });

        Assert.All(ctx.OfKind("DrawLine"), c => Assert.Equal(2.5, (double)((dynamic)c.Args!).thickness));
    }

    [Fact]
    public void ASlotsInwardTicks_PointIntoThePlot()
    {
        var ctx = RenderWith(t => t with { Direction = TickDirection.In });
        var length = new TickConfig().Length;
        double half = new Axes().Spines.Right.LineWidth / 2;

        Assert.All(ctx.OfKind("DrawLine"), c =>
        {
            dynamic a = c.Args!;
            Assert.Equal(Right - length, ((Point)a.p1).X, 6);
            Assert.Equal(Right + half, ((Point)a.p2).X, 6);
        });
    }

    [Fact]
    public void ASlot_HonoursItsTickLabelRotation()
    {
        var ctx = RenderWith(t => t with { LabelRotation = 45 });

        Assert.All(ctx.OfKind("DrawText"), c => Assert.Equal(45.0, (double)((dynamic)c.Args!).rotation));
    }

    [Fact]
    public void ASlotWithHiddenTicks_DrawsNoMarkNoLabel_AndItsTitleStandsAtTheFixedOffset()
    {
        var ctx = RenderWith(t => t with { Visible = false }, axis => axis.Label = "Right axis");

        Assert.Equal(0, ctx.CountOf("DrawLine"));
        var only = Assert.Single(ctx.OfKind("DrawText"));
        dynamic a = only.Args!;
        Assert.Equal("Right axis", (string)a.text);
        Assert.Equal(Right + 45, ((Point)a.position).X, 6);
    }

    [Fact]
    public void ASlotsTitle_ClearsTheWidestTickLabel_ByTheGapAndItsFacingInk()
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
        // The body of the title's text faces the tick labels on the right: it stands the container's gap past their band,
        // so the anchor stands that gap and the rise of the body (0.6 of the title's size) out.
        double rise = FacingInk.TurnedRise * ((Font)label.font).Size;
        Assert.Equal(Right + defaults.Length + defaults.Pad + widest + 4 + rise, ((Point)label.position).X, 6);
        Assert.Equal(90.0, (double)label.rotation);
    }

    [Fact]
    public void ASlotsTitle_HonoursItsLabelStyle()
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
    // A slot's title
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ASlotWithATitle_DrawsOneTextMore()
    {
        var without = new RecordingRenderContext();
        var axNo = new Axes();
        Part(axNo, RightSlot(axNo), without).Render();
        var ax = new Axes();
        RightSlot(ax).Axis.Label = "Right axis";
        var ctx = new RecordingRenderContext();

        Part(ax, RightSlot(ax), ctx).Render();

        Assert.Equal(without.CountOf("DrawText") + 1, ctx.CountOf("DrawText"));
        Assert.Contains(ctx.OfKind("DrawText"), c => (string)((dynamic)c.Args!).text == "Right axis");
    }

    [Fact]
    public void ASlotWithoutATitle_DrawsNoEmptyTitle()
    {
        var ax = new Axes();
        RightSlot(ax).Axis.Label = null;
        var ctx = new RecordingRenderContext();

        Part(ax, RightSlot(ax), ctx).Render();

        Assert.DoesNotContain(ctx.OfKind("DrawText"), c => (string)((dynamic)c.Args!).text == "");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // A slot's series
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ASlotsVisibleSeries_AreDrawn()
    {
        var empty = new Axes();
        var before = new RecordingRenderContext();
        Part(empty, RightSlot(empty), before).Render();
        var ax = new Axes();
        Line(RightSlot(ax), [0.0, 5, 10], [10.0, 50, 90]);
        var ctx = new RecordingRenderContext();

        Part(ax, RightSlot(ax), ctx).Render();

        Assert.True(ctx.Calls.Count > before.Calls.Count, "a slot's series should produce more draw calls");
    }

    [Fact]
    public void ASlotsHiddenSeries_IsSkipped()
    {
        var ax = new Axes();
        Line(RightSlot(ax), [0.0, 5, 10], [10.0, 50, 90]).Visible = false;
        var ctx = new RecordingRenderContext();

        Part(ax, RightSlot(ax), ctx).Render();

        Assert.Equal(0, ctx.CountOf("DrawLines"));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Tick formatter
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ASlotsTickFormatter_WritesItsLabels()
    {
        var ax = new Axes();
        RightSlot(ax).Axis.TickFormatter = new PercentFormatter(100);
        var ctx = new RecordingRenderContext();

        Part(ax, RightSlot(ax), ctx).Render();

        Assert.Contains(ctx.OfKind("DrawText"), c => ((string)((dynamic)c.Args!).text).Contains('%'));
    }

    [Fact]
    public void ASlotWithoutAFormatter_WritesTheUniformFormat()
    {
        var ax = new Axes();
        RightSlot(ax).Axis.TickFormatter = null;
        var ctx = new RecordingRenderContext();

        Part(ax, RightSlot(ax), ctx).Render();

        foreach (var c in ctx.OfKind("DrawText"))
            Assert.DoesNotContain('%', (string)((dynamic)c.Args!).text);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // A slot stacked out past the plot's edge
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ARightSlotWithAnOffset_DrawsItsTicksThatFarPastThePlotEdge()
    {
        var ax = new Axes();
        var slot1 = ax.YAxes.Right.Add();
        Line(slot1, [0.0, 10], [0.0, 100]);
        var ctx = new RecordingRenderContext();
        const double outwardOffset = 37.5;

        PartAt(ax, slot1, ctx, outwardOffset).Render();

        // p2 is the tick's outer end: the slot's edge (right + offset) + the default tick length.
        var length = new TickConfig().Length;
        Assert.NotEmpty(ctx.OfKind("DrawLine"));
        Assert.All(ctx.OfKind("DrawLine"), c =>
            Assert.Equal(Right + outwardOffset + length, ((Point)((dynamic)c.Args!).p2).X, 6));
    }

    [Fact]
    public void ALeftSlot_DrawsItsTicksAtThePlotsLeftEdge()
    {
        var ax = new Axes();
        var slot = ax.YAxes.Left.Add();
        Line(slot, [0.0, 10], [0.0, 100]);
        var ctx = new RecordingRenderContext();

        Part(ax, slot, ctx).Render();

        // p1 is the tick's outer (farther) end: the edge less the length, mirroring the right side's p2.
        var length = new TickConfig().Length;
        Assert.NotEmpty(ctx.OfKind("DrawLine"));
        Assert.All(ctx.OfKind("DrawLine"), c => Assert.Equal(Plot.X - length, ((Point)((dynamic)c.Args!).p1).X, 6));
    }

    [Fact]
    public void ALeftSlotWithAnOffset_DrawsItsTicksThatFarPastThePlotsLeftEdge()
    {
        var ax = new Axes();
        var slot = ax.YAxes.Left.Add();
        Line(slot, [0.0, 10], [0.0, 100]);
        var ctx = new RecordingRenderContext();
        const double outwardOffset = 28.0;

        PartAt(ax, slot, ctx, outwardOffset).Render();

        var length = new TickConfig().Length;
        Assert.NotEmpty(ctx.OfKind("DrawLine"));
        Assert.All(ctx.OfKind("DrawLine"), c =>
            Assert.Equal(Plot.X - outwardOffset - length, ((Point)((dynamic)c.Args!).p1).X, 6));
    }

    [Fact]
    public void ALeftSlotsTickLabels_AreRightAligned_SoTheyGrowFurtherLeft()
    {
        var ax = new Axes();
        var slot = ax.YAxes.Left.Add();
        Line(slot, [0.0, 10], [0.0, 100]);
        var ctx = new RecordingRenderContext();

        Part(ax, slot, ctx).Render();

        Assert.NotEmpty(ctx.OfKind("DrawText"));
        Assert.All(ctx.OfKind("DrawText"), c => Assert.Equal(TextAlignment.Right, (TextAlignment)((dynamic)c.Args!).alignment));
    }

    [Fact]
    public void ALeftSlotWithATitle_DrawsIt()
    {
        var ax = new Axes();
        var slot = ax.YAxes.Left.Add();
        Line(slot, [0.0, 10], [0.0, 100]);
        slot.Axis.Label = "Left axis";
        var ctx = new RecordingRenderContext();

        Part(ax, slot, ctx).Render();

        Assert.Contains(ctx.OfKind("DrawText"), c => (string)((dynamic)c.Args!).text == "Left axis");
    }

    /// <summary>The title's own LabelStyle (colour, size, pad) is read on the left too, not only the theme's font.</summary>
    [Fact]
    public void ALeftSlotsTitle_HonoursItsLabelStyle()
    {
        var ax = new Axes();
        var slot = ax.YAxes.Left.Add();
        Line(slot, [0.0, 10], [0.0, 100]);
        slot.Axis.Label = "Left axis";
        slot.Axis.LabelStyle = new TextStyle { Color = Colors.Green, FontSize = 19, Pad = 6 };
        var ctx = new RecordingRenderContext();

        Part(ax, slot, ctx).Render();

        dynamic label = Assert.Single(ctx.OfKind("DrawText"), c => (string)((dynamic)c.Args!).text == "Left axis").Args!;
        var font = (Font)label.font;
        Assert.Equal(Colors.Green, font.Color);
        Assert.Equal(19.0, font.Size);
        // Further left than the plot's own left edge: the style's pad is added to the offset, not ignored.
        Assert.True(((Point)label.position).X < Plot.X);
    }

    [Fact]
    public void ALeftSlotsHiddenSeries_IsSkipped()
    {
        var ax = new Axes();
        var slot = ax.YAxes.Left.Add();
        Line(slot, [0.0, 10], [0.0, 100]).Visible = false;
        var ctx = new RecordingRenderContext();

        Part(ax, slot, ctx).Render();

        Assert.Equal(0, ctx.CountOf("DrawLines"));
    }
}
