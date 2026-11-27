// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.CartesianParts;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.CartesianParts;

/// <summary>Direct unit tests for <see cref="CartesianAxisPart"/>, the one part that draws every axis the plot has besides the primary
/// pair: a Y axis slot on a left or a right edge, at any offset, and the top X axis. What an edge's axis honours is one rule for every
/// edge and is a theory over the edge's name: its series visible, hidden and in their own groups, its tick marks and labels by its own
/// <see cref="TickConfig"/>, its tick formatter, its title by the one band rule. What only an edge has (a slot's colour slot and offset,
/// a left slot's right-aligned labels, the top axis' band above the edge) is a fact of its own.</summary>
public class CartesianAxisPartTests
{
    private static readonly Theme DefaultTheme = Theme.Default;
    private static readonly Rect Plot = new(100, 0, 400, 300);
    private static readonly DataRange SlotRange = new(0, 10, 0, 100);
    private static readonly DataRange TopRange = new(0, 100, 0, 10);
    private static double Right => Plot.X + Plot.Width;

    // ── how a part is built on each edge ────────────────────────────────────────────────────────────────

    /// <summary>The primary as a render places it over <see cref="SlotRange"/>'s X extent.</summary>
    private static PlacedAxis PlacedPrimary(Axes axes) => new()
    {
        Axis = axes.YAxes.Left[0].Axis,
        Edge = EdgeTerms.Left,
        Series = axes.YAxes.Left[0].Series,
        Range = new DataRange(0, 10, 0, 10),
        Transform = new DataTransform(0, 10, 0, 10, Plot),
        Ticks = [],
        Source = default,
    };

    /// <summary><paramref name="slot"/> placed over <see cref="SlotRange"/>, its series numbered as the series passes number
    /// them, <paramref name="offset"/> out past the plot's edge on its side, hung from the spine on its side at the plot's edge and from
    /// its place past it, as the placement hangs a slot.</summary>
    private static PlacedAxis Placed(Axes axes, YAxisSlot slot, double offset = 0)
    {
        var seat = axes.YSlotSeats().Single(s => ReferenceEquals(s.Slot, slot));
        var edge = EdgeTerms.Of(slot.Side);
        var transform = new DataTransform(SlotRange.XMin, SlotRange.XMax, SlotRange.YMin, SlotRange.YMax, Plot);
        return new PlacedAxis
        {
            Axis = slot.Axis,
            Edge = edge,
            Series = slot.Series,
            Range = SlotRange,
            Transform = transform,
            Ticks = slot.Axis.SlotTickValues(SlotRange, AutoLocator.FullDensityLength, Theme.Default),
            Source = slot.Axis.SlotTickSource(Theme.Default),
            Pair = seat.Pair,
            FirstIndexInPair = seat.FirstIndexInPair,
            Offset = offset,
            EdgeLine = offset == 0 ? edge.SpineAt(axes.Spines, Plot, transform) : edge.EdgeAt(Plot, offset),
        };
    }

    /// <summary>The top axis of <paramref name="axes"/> placed over <see cref="TopRange"/>, as the renderer places a top axis with a
    /// range of its own: its ticks by its own rule at full density (the eight intervals of a top axis that sets none), through the
    /// linear transform of that range.</summary>
    private static PlacedAxis PlacedTop(Axes axes)
    {
        var axis = axes.SecondaryXAxis!;
        var source = axis.OwnTickSource(isX: true, Theme.Default);
        return new PlacedAxis
        {
            Axis = axis,
            Edge = EdgeTerms.Top,
            Series = axes.XSecondarySeries,
            Pair = SeriesAxisPair.SecondaryX,
            EdgeLine = Plot.Y,
            Range = TopRange,
            Transform = new DataTransform(TopRange.XMin, TopRange.XMax, TopRange.YMin, TopRange.YMax, Plot),
            Ticks = axis.TickValuesBetween(source, new Range1D(TopRange.XMin, TopRange.XMax), AutoLocator.FullDensityLength, Theme.Default, isX: true),
            Source = source,
        };
    }

    /// <summary>The part for <paramref name="slot"/>, drawing its series' end labels with the end labels of
    /// <paramref name="axes"/>.</summary>
    private static CartesianAxisPart Part(Axes axes, YAxisSlot slot, IRenderContext ctx, Theme? theme = null) =>
        BuildSlot(axes, Placed(axes, slot), ctx, theme ?? DefaultTheme);

    /// <summary>The part for <paramref name="slot"/> pushed <paramref name="offset"/> past the plot's edge on its side.</summary>
    private static CartesianAxisPart PartAt(Axes axes, YAxisSlot slot, IRenderContext ctx, double offset) =>
        BuildSlot(axes, Placed(axes, slot, offset), ctx, DefaultTheme);

    private static CartesianAxisPart BuildSlot(Axes axes, PlacedAxis placed, IRenderContext ctx, Theme theme)
    {
        var context = new CartesianPartContext(axes, new RenderPass(Plot, ctx, theme), axes.ColorSlots(theme), placed.Transform);
        var labels = new CartesianAxesRenderer.CartesianDirectLabelsPart(context, [PlacedPrimary(axes), placed], 0);
        return CartesianAxisPart.Of(context, placed, new TestLayerHost(context.ColorSlots, labels.DrawLabelOf));
    }

    private static CartesianAxisPart TopPart(Axes axes, IRenderContext ctx, Theme? theme = null)
    {
        var used = theme ?? DefaultTheme;
        var context = new CartesianPartContext(axes, new RenderPass(Plot, ctx, used), axes.ColorSlots(used), Plot.Linear());
        return CartesianAxisPart.Of(context, PlacedTop(axes), new TestLayerHost(context.ColorSlots));
    }

    private static YAxisSlot RightSlot(Axes axes) => axes.YAxes.Right.FirstOrAdd();

    /// <summary>The slot past the primary Y axis on the left (the primary is the first of the left stack).</summary>
    private static YAxisSlot LeftSlot(Axes axes) => axes.YAxes.Left.Count > 1 ? axes.YAxes.Left[1] : axes.YAxes.Left.Add();

    private static LineSeries Line(YAxisSlot slot, double[] x, double[] y) => slot.AddSeries(new LineSeries(x, y));

    private static System.Xml.Linq.XElement Parsed(SvgRenderContext ctx) =>
        System.Xml.Linq.XElement.Parse("<root>" + ctx.GetOutput() + "</root>");

    // ── one row per edge: where the edge stands, and how the shared rules read what the part drew ───────

    /// <summary>One edge as the shared rules read it. An axis' marks and labels reach <em>outward</em> from the edge: down the page past
    /// the top edge, rightward past the right one; <c>Outward(d)</c> is the coordinate <c>d</c> pixels out.</summary>
    private sealed record Edge(
        Func<Axes> Fresh,
        Func<Axes, Axis> AxisOf,
        Func<Axes, IRenderContext, Theme?, CartesianAxisPart> Draw,
        Func<Axes, double, LineSeries> AddLine,
        Func<double, double> Outward,
        Func<DrawLineCall, double> OuterEnd,
        Func<DrawLineCall, double> InnerEnd,
        Func<DrawLineCall, double> TickAlong,
        Func<DrawTextCall, double> LabelOut,
        Func<DrawTextCall, double> LabelAlong,
        Func<Axes, double> SpineHalf,
        double LabelRise,
        TextAlignment LabelAlignment,
        double TitleGap,
        Func<Axes, IRenderContext, double, CartesianAxisPart>? DrawAtOffset = null);

    private static double TickLabelRise => 0.35 * DefaultTheme.TickFont().Size;

    private static readonly Dictionary<string, Edge> Edges = new()
    {
        ["Right"] = new(
            () => new Axes(),
            axes => RightSlot(axes).Axis,
            (axes, ctx, theme) => Part(axes, RightSlot(axes), ctx, theme),
            (axes, y) => Line(RightSlot(axes), [0.0, 5, 10], [y, 50, 90]),
            d => Right + d,
            c => c.P2.X,
            c => c.P1.X,
            c => c.P1.Y,
            c => c.Position.X,
            c => c.Position.Y,
            axes => axes.Spines.Right.LineWidth / 2,
            TickLabelRise,
            TextAlignment.Left,
            45,
            (axes, ctx, offset) => PartAt(axes, RightSlot(axes), ctx, offset)),
        ["Left"] = new(
            () => new Axes(),
            axes => LeftSlot(axes).Axis,
            (axes, ctx, theme) => Part(axes, LeftSlot(axes), ctx, theme),
            (axes, y) => Line(LeftSlot(axes), [0.0, 5, 10], [y, 50, 90]),
            d => Plot.X - d,
            c => c.P1.X,
            c => c.P2.X,
            c => c.P1.Y,
            c => c.Position.X,
            c => c.Position.Y,
            axes => axes.Spines.Left.LineWidth / 2,
            TickLabelRise,
            TextAlignment.Right,
            45,
            (axes, ctx, offset) => PartAt(axes, LeftSlot(axes), ctx, offset)),
        ["Top"] = new(
            () =>
            {
                var axes = new Axes();
                axes.TwinY();
                return axes;
            },
            axes => axes.SecondaryXAxis!,
            (axes, ctx, theme) => TopPart(axes, ctx, theme),
            (axes, y) => axes.PlotXSecondary([0.0, 50, 100], [y, 5, 9]),
            d => Plot.Y - d,
            c => c.P1.Y,
            c => c.P2.Y,
            c => c.P1.X,
            c => c.Position.Y,
            c => c.Position.X,
            axes => axes.Spines.Top.LineWidth / 2,
            0,
            TextAlignment.Center,
            6),
    };

    /// <summary>The edges whose axis the rules below hold: a right slot, a left slot past the primary, and the top axis.</summary>
    public static TheoryData<string> EveryEdge { get; } = ["Right", "Left", "Top"];

    private static RecordingRenderContext RenderWith(Edge edge, Func<TickConfig, TickConfig> ticks, Action<Axis>? axis = null)
    {
        var ax = edge.Fresh();
        var drawn = edge.AxisOf(ax);
        drawn.MajorTicks = ticks(drawn.MajorTicks);
        axis?.Invoke(drawn);
        var ctx = new RecordingRenderContext();
        edge.Draw(ax, ctx, null).Render();
        return ctx;
    }

    private static RecordingRenderContext Drawn(Edge edge, Action<Axes, Axis>? arrange = null)
    {
        var ax = edge.Fresh();
        arrange?.Invoke(ax, edge.AxisOf(ax));
        var ctx = new RecordingRenderContext();
        edge.Draw(ax, ctx, null).Render();
        return ctx;
    }

    // ── ticks and labels ────────────────────────────────────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(EveryEdge))]
    public void AnAxis_DrawsTickMarksAndLabels(string name)
    {
        var ctx = Drawn(Edges[name]);

        Assert.True(ctx.Count<DrawLineCall>() >= 1);
        Assert.True(ctx.Count<DrawTextCall>() >= 1);
    }

    [Theory]
    [MemberData(nameof(EveryEdge))]
    public void AnAxisTicks_StandOnItsEdge_MeetingTheSpine(string name)
    {
        // An axis draws by its own TickConfig, so its default mark is the primary axes' default: 3.5 pt (4.861 px) outward, its inner
        // end half a spine into the plot so it meets the spine stroke.
        var edge = Edges[name];
        var ax = edge.Fresh();
        var ctx = new RecordingRenderContext();
        edge.Draw(ax, ctx, null).Render();

        var tickLines = ctx.Of<DrawLineCall>();
        Assert.NotEmpty(tickLines);
        foreach (var call in tickLines)
        {
            Assert.Equal(edge.Outward(-edge.SpineHalf(ax)), edge.InnerEnd(call), 6);
            Assert.Equal(edge.Outward(new TickConfig().Length), edge.OuterEnd(call), 6);
        }
    }

    [Theory]
    [MemberData(nameof(EveryEdge))]
    public void AnAxisDefaultTicks_TakeThePrimaryAxesDefaults(string name)
    {
        // One rule for tick geometry: mark Length, Width and colour, label at Length + Pad beyond the edge, centred on its tick.
        var edge = Edges[name];
        var ctx = Drawn(edge);
        var defaults = new TickConfig();

        var marks = ctx.Of<DrawLineCall>();
        var labels = ctx.Of<DrawTextCall>();
        Assert.NotEmpty(marks);
        Assert.Equal(marks.Count, labels.Count);
        for (int i = 0; i < marks.Count; i++)
        {
            Assert.Equal(edge.Outward(defaults.Length), edge.OuterEnd(marks[i]), 6);
            Assert.Equal(defaults.Width, marks[i].Thickness, 6);
            Assert.Equal(DefaultTheme.ForegroundText, marks[i].Color);
            Assert.Equal(edge.Outward(defaults.Length + defaults.Pad), edge.LabelOut(labels[i]), 6);
            Assert.Equal(edge.TickAlong(marks[i]) + edge.LabelRise, edge.LabelAlong(labels[i]), 6);
            Assert.Equal(edge.LabelAlignment, labels[i].Alignment);
        }
    }

    // ── an axis honours its own TickConfig and LabelStyle ───────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(EveryEdge))]
    public void AnAxis_HonoursItsTickColor(string name)
    {
        var ctx = RenderWith(Edges[name], t => t with { Color = Colors.Red });

        Assert.NotEmpty(ctx.Of<DrawLineCall>());
        Assert.All(ctx.Of<DrawLineCall>(), c => Assert.Equal(Colors.Red, c.Color));
    }

    [Theory]
    [MemberData(nameof(EveryEdge))]
    public void AnAxis_HonoursItsTickLabelColor(string name)
    {
        var ctx = RenderWith(Edges[name], t => t with { LabelColor = Colors.Blue });

        Assert.NotEmpty(ctx.Of<DrawTextCall>());
        Assert.All(ctx.Of<DrawTextCall>(), c => Assert.Equal(Colors.Blue, c.Font.Color));
    }

    [Theory]
    [MemberData(nameof(EveryEdge))]
    public void AnAxis_HonoursItsTickLength(string name)
    {
        var edge = Edges[name];
        var ctx = RenderWith(edge, t => t with { Length = 10 });
        var pad = new TickConfig().Pad;

        Assert.All(ctx.Of<DrawLineCall>(), c => Assert.Equal(edge.Outward(10), edge.OuterEnd(c), 6));
        Assert.All(ctx.Of<DrawTextCall>(), c => Assert.Equal(edge.Outward(10 + pad), edge.LabelOut(c), 6));
    }

    [Theory]
    [MemberData(nameof(EveryEdge))]
    public void AnAxis_HonoursItsTickPad(string name)
    {
        var edge = Edges[name];
        var ctx = RenderWith(edge, t => t with { Pad = 7 });
        var length = new TickConfig().Length;

        Assert.All(ctx.Of<DrawTextCall>(), c => Assert.Equal(edge.Outward(length + 7), edge.LabelOut(c), 6));
    }

    [Theory]
    [MemberData(nameof(EveryEdge))]
    public void AnAxis_HonoursItsTickWidth(string name)
    {
        var ctx = RenderWith(Edges[name], t => t with { Width = 2.5 });

        Assert.All(ctx.Of<DrawLineCall>(), c => Assert.Equal(2.5, c.Thickness));
    }

    [Theory]
    [MemberData(nameof(EveryEdge))]
    public void AnAxis_HonoursItsTickLabelRotation(string name)
    {
        var ctx = RenderWith(Edges[name], t => t with { LabelRotation = 45 });

        Assert.All(ctx.Of<DrawTextCall>(), c => Assert.Equal(45.0, c.Rotation));
    }

    [Theory]
    [MemberData(nameof(EveryEdge))]
    public void AnAxisWithHiddenTicks_DrawsNoMarkNoLabel_AndItsTitleStandsAtTheFixedOffset(string name)
    {
        var edge = Edges[name];
        var ctx = RenderWith(edge, t => t with { Visible = false }, axis => axis.Label = "The axis");

        Assert.Equal(0, ctx.Count<DrawLineCall>());
        var only = Assert.Single(ctx.Of<DrawTextCall>());
        Assert.Equal("The axis", only.Text);
        Assert.Equal(edge.Outward(edge.TitleGap), edge.LabelOut(only), 6);
    }

    [Theory]
    [MemberData(nameof(EveryEdge))]
    public void AnAxisTitle_HonoursItsLabelStyle(string name)
    {
        var edge = Edges[name];
        var ctx = RenderWith(edge, t => t with { Visible = false }, axis =>
        {
            axis.Label = "The axis";
            axis.LabelStyle = new TextStyle { Color = Colors.Green, FontSize = 19, Pad = 6 };
        });

        var label = Assert.Single(ctx.Of<DrawTextCall>());
        var font = label.Font;
        Assert.Equal(Colors.Green, font.Color);
        Assert.Equal(19.0, font.Size);
        Assert.Equal(edge.Outward(edge.TitleGap + 6), edge.LabelOut(label), 6);
    }

    // ── an axis' title ──────────────────────────────────────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(EveryEdge))]
    public void AnAxisWithATitle_DrawsOneTextMore(string name)
    {
        var edge = Edges[name];
        var without = Drawn(edge);

        var ctx = Drawn(edge, (_, axis) => axis.Label = "The axis");

        Assert.Equal(without.Count<DrawTextCall>() + 1, ctx.Count<DrawTextCall>());
        Assert.Contains(ctx.Of<DrawTextCall>(), c => c.Text == "The axis");
    }

    [Theory]
    [MemberData(nameof(EveryEdge))]
    public void AnAxisWithoutATitle_DrawsNoEmptyTitle(string name)
    {
        var ctx = Drawn(Edges[name], (_, axis) => axis.Label = null);

        Assert.DoesNotContain(ctx.Of<DrawTextCall>(), c => c.Text == "");
    }

    // ── an axis' series ─────────────────────────────────────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(EveryEdge))]
    public void AnAxisVisibleSeries_AreDrawn(string name)
    {
        var edge = Edges[name];
        var before = Drawn(edge);

        var ctx = Drawn(edge, (ax, _) => edge.AddLine(ax, 10));

        Assert.True(ctx.Calls.Count > before.Calls.Count, "an axis' series should produce more draw calls");
    }

    [Theory]
    [MemberData(nameof(EveryEdge))]
    public void AnAxisHiddenSeries_IsSkipped(string name)
    {
        var edge = Edges[name];

        var ctx = Drawn(edge, (ax, _) => edge.AddLine(ax, 10).Visible = false);

        Assert.Equal(0, ctx.Count<DrawLinesCall>());
    }

    [Theory]
    [MemberData(nameof(EveryEdge))]
    public void WithoutInteractiveAttributes_AnUnlabelledSeries_IsDrawnWithoutAGroup(string name)
    {
        var edge = Edges[name];
        var ax = edge.Fresh();
        edge.AddLine(ax, 10);
        var ctx = new SvgRenderContext();

        edge.Draw(ax, ctx, null).Render();

        Assert.DoesNotContain("class=\"series\"", ctx.GetOutput());
        Assert.Contains("<polyline", ctx.GetOutput());
    }

    // ── the tick formatter ──────────────────────────────────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(EveryEdge))]
    public void AnAxisTickFormatter_WritesItsLabels(string name)
    {
        var ctx = Drawn(Edges[name], (_, axis) => axis.TickFormatter = new PercentFormatter(100));

        Assert.Contains(ctx.Of<DrawTextCall>(), c => c.Text.Contains('%'));
    }

    [Theory]
    [MemberData(nameof(EveryEdge))]
    public void AnAxisWithoutAFormatter_WritesTheUniformFormat(string name)
    {
        var ctx = Drawn(Edges[name], (_, axis) => axis.TickFormatter = null);

        foreach (var c in ctx.Of<DrawTextCall>())
            Assert.DoesNotContain('%', c.Text);
    }

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
        var ctx = new SvgRenderContext();

        Part(ax, RightSlot(ax), ctx).Render();

        var group = Assert.Single(Parsed(ctx).Elements("g"), g => (string?)g.Attribute("class") == "series");
        Assert.Equal("2", (string?)group.Attribute("data-series-index"));
        Assert.Equal("S", (string?)group.Attribute("aria-label"));
        Assert.Single(group.Descendants("polyline"));
    }

    [Fact]
    public void ATopAxisSeries_IsDrawnInADataGroupKeyedByItsSlot()
    {
        // Slots run primary, then secondary Y, then secondary X: one of each puts the top-axis line in slot 2.
        var ax = new Axes { EnableInteractiveAttributes = true };
        ax.Plot([0.0, 10], [0.0, 10]);
        ax.YAxes.Right.FirstOrAdd().AddSeries(new LineSeries([0.0, 10], [10.0, 90]));
        ax.PlotXSecondary([0.0, 100], [0.0, 10]).Label = "top";
        var ctx = new SvgRenderContext();

        TopPart(ax, ctx).Render();

        var group = Assert.Single(Parsed(ctx).Elements("g"), g => (string?)g.Attribute("class") == "series");
        Assert.Equal("2", (string?)group.Attribute("data-series-index"));
        Assert.Equal("top", (string?)group.Attribute("aria-label"));
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
        var ctx = new SvgRenderContext();

        Part(ax, RightSlot(ax), ctx).Render();

        var group = Assert.Single(Parsed(ctx).Elements("g"), g => (string?)g.Attribute("class") == "series");
        Assert.Equal("1", (string?)group.Attribute("data-series-index"));
        Assert.Single(group.Descendants("text"), t => t.Value == "named");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // The colour slot an axis' series is drawn in
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

        var line = Assert.Single(ctx.Of<DrawLinesCall>());
        Assert.Equal(Colors.Green, line.Color);
    }

    [Fact]
    public void ATopAxisLine_WearsTheCyclerColourOfTheSlotAfterBothOtherLists()
    {
        // Slots run primary, then secondary Y, then secondary X: one of each puts the top-axis line in slot 2.
        var cycler = new PropCyclerBuilder().WithColors(Colors.Red, Colors.Green, Colors.Blue).Build();
        var theme = Theme.CreateFrom(Theme.Default).WithPropCycler(cycler).Build();
        var ax = new Axes();
        ax.Plot([0.0, 10], [0.0, 10]);
        ax.YAxes.Right.FirstOrAdd().AddSeries(new LineSeries([0.0, 10], [10.0, 90]));
        ax.PlotXSecondary([0.0, 100], [0.0, 10]);
        var ctx = new RecordingRenderContext();

        TopPart(ax, ctx, theme).Render();

        var line = Assert.Single(ctx.Of<DrawLinesCall>());
        Assert.Equal(Colors.Blue, line.Color);
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

        Assert.Contains(ctx.Of<DrawPolygonCall>(), c => c.Stroke == body);
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

        var line = Assert.Single(ctx.Of<DrawLinesCall>());
        Assert.Equal(Colors.Blue, line.Color);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // What only a slot's edge has
    // ──────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Right", 37.5)]
    [InlineData("Left", 28.0)]
    public void ASlotWithAnOffset_DrawsItsTicksThatFarPastThePlotEdge(string name, double outwardOffset)
    {
        var edge = Edges[name];
        var ax = edge.Fresh();
        edge.AddLine(ax, 0);
        var ctx = new RecordingRenderContext();

        edge.DrawAtOffset!(ax, ctx, outwardOffset).Render();

        // The tick's outer end: the slot's edge pushed out by the offset, plus the default tick length.
        Assert.NotEmpty(ctx.Of<DrawLineCall>());
        Assert.All(ctx.Of<DrawLineCall>(), c => Assert.Equal(edge.Outward(outwardOffset + new TickConfig().Length), edge.OuterEnd(c), 6));
    }

    [Fact]
    public void ASlotsInwardTicks_PointIntoThePlot()
    {
        var ctx = RenderWith(Edges["Right"], t => t with { Direction = TickDirection.In });
        var length = new TickConfig().Length;
        double half = new Axes().Spines.Right.LineWidth / 2;

        Assert.All(ctx.Of<DrawLineCall>(), c =>
        {
            Assert.Equal(Right - length, c.P1.X, 6);
            Assert.Equal(Right + half, c.P2.X, 6);
        });
    }

    [Fact]
    public void ASlotsTitle_ClearsTheWidestTickLabel_ByTheGapAndItsFacingInk()
    {
        var ctx = RenderWith(Edges["Right"], t => t, axis => axis.Label = "Right axis");
        var defaults = new TickConfig();
        var tickTexts = ctx.Of<DrawTextCall>().Where(c => c.Text != "Right axis").ToList();
        double widest = tickTexts.Max(c => ctx.MeasureText(c.Text, c.Font).Width);

        var label = ctx.Of<DrawTextCall>().Single(c => c.Text == "Right axis");
        // The body of the title's text faces the tick labels on the right: it stands the container's gap past their band,
        // so the anchor stands that gap and the rise of the body (0.6 of the title's size) out.
        double rise = FacingInk.TurnedRise * label.Font.Size;
        Assert.Equal(Right + defaults.Length + defaults.Pad + widest + 4 + rise, label.Position.X, 6);
        Assert.Equal(90.0, label.Rotation);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // What only the top edge has
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ATopAxisTickLabelRotation_AnchorsTheLabelAtItsTick_SoItRisesAwayFromTheAxis()
    {
        var ctx = RenderWith(Edges["Top"], t => t with { LabelRotation = 30 });

        Assert.All(ctx.Of<DrawTextCall>(), c => Assert.Equal(TextAlignment.Left, c.Alignment));
    }

    /// <summary>The band above the top edge the part drew (<see cref="AxesTitleExtensions"/>' <c>TopBand</c> of what it drew).</summary>
    private static double Band(Axes axes, IRenderContext ctx, double reach) =>
        axes.SecondaryXAxis!.TopBand(axes.SecondaryXAxis!.Label, ctx, DefaultTheme, reach);

    /// <summary>The band above the top edge measured before anything is drawn, as the layout measures it.</summary>
    private static double MeasureBand(Axes axes, IRenderContext ctx) =>
        axes.SecondaryXAxis!.TopBand(axes.SecondaryXAxis!.Label, ctx, DefaultTheme, PlacedTop(axes).OwnSideLabelReach(ctx, DefaultTheme, axes.Spines));

    /// <summary>A top axis title that holds math is drawn as the runs of its layout, as every other axis title is, and the band keeps the
    /// room that layout takes.</summary>
    [Fact]
    public void ATopAxisMathTitle_IsDrawnAsTheRunsOfItsLayout_InTheRoomItTakes()
    {
        var ax = new Axes();
        ax.PlotXSecondary([0.0, 50, 100], [1.0, 5, 9]);
        ax.SecondaryXAxis!.Label = @"$\sum x$";
        ax.SecondaryXAxis.MajorTicks = ax.SecondaryXAxis.MajorTicks with { Visible = false };
        var ctx = new RecordingRenderContext();

        double reach = TopPart(ax, ctx).Render();

        Assert.Empty(ctx.Of<DrawRichTextCall>());
        var runs = ctx.Of<DrawTextCall>();
        Assert.Equal(["∑", "x"], runs.Select(c => c.Text));
        var title = runs[1];
        var font = title.Font;
        double drawn = ((IRenderContext)ctx).MeasureRichText(MathTextParser.Parse(ax.SecondaryXAxis.Label), font).Height;
        Assert.Equal(Plot.Y - (title.Position.Y - (drawn - FacingInk.Descent * font.Size)), Band(ax, ctx, reach), 6);
        Assert.Equal(Band(ax, ctx, reach), MeasureBand(ax, ctx), 9);
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

        double measured = MeasureBand(ax, ctx);
        double reach = TopPart(ax, ctx).Render();

        // From the edge to the top of what was drawn: the title's box when there is one (its part above the baseline, the descent
        // facing the tick labels below it), the tallest tick label otherwise.
        var tops = ctx.Of<DrawTextCall>().Select(c =>
        {
            var font = c.Font;
            double height = ctx.MeasureText(c.Text, font).Height;
            return c.Position.Y - (c.Text == label ? height - FacingInk.Descent * font.Size : height);
        }).ToList();
        Assert.NotEmpty(tops);
        Assert.Equal(Plot.Y - tops.Min(), Band(ax, ctx, reach), 6);
        Assert.Equal(Band(ax, ctx, reach), measured, 9);
    }

    [Fact]
    public void Band_IsZero_WhenNothingIsDrawnAboveTheEdge()
    {
        var ax = new Axes();
        ax.PlotXSecondary([0.0, 50, 100], [1.0, 5, 9]);
        ax.SecondaryXAxis!.MajorTicks = ax.SecondaryXAxis.MajorTicks with { Visible = false };
        var ctx = new RecordingRenderContext();

        double reach = TopPart(ax, ctx).Render();

        Assert.Equal(0, Band(ax, ctx, reach));
        Assert.Equal(0, MeasureBand(ax, ctx));
    }
}
