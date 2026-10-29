// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Where the title of every Y axis of the container stands, read off the SVG the way a reader sees it: the gap
/// between a title's ink and the ink of the widest tick label beside it is the same on the left and on the right, for
/// every slot including the primary, 4 px unless the figure asks for another; the layout reserves exactly what the slots
/// draw; and the gap between two stacked slots is the same on both sides.</summary>
/// <remarks>The ink of a title is read by the convention the library places it by (<see cref="FacingInk"/>): a Y title is
/// drawn <c>rotate(-90)</c> about its anchor x with no <c>dy</c>, so its ink spans [x - 0.8 em, x + 0.2 em]; the ink it is
/// read to face its tick labels with is 0.6 em above the anchor on the right and the anchor itself on the left. The ink of a
/// tick label is its anchor and the library's width estimate (<see cref="FontMetricsDefault"/>): a left label ends at its
/// anchor (<c>text-anchor="end"</c>), a right one starts there. A browser draws digits narrower than the estimate, so the
/// gap a browser shows is a few pixels wider; it is the same on both sides, which the browser check of the step measures.
/// The SVG writes coordinates to two decimals, hence the tolerance.</remarks>
public class YAxisTitleGapTests
{
    private const double Tolerance = 0.02;
    private const double Width = 900, Height = 400;
    private static readonly double[] X = [0.0, 1.0, 2.0];

    // ── the SVG, read ────────────────────────────────────────────────────────

    private readonly record struct Drawn(double X, double FontSize, string Anchor, bool Turned, string Text)
    {
        /// <summary>The library's width estimate of the text, the width it was laid out by.</summary>
        public double TextWidth => FontMetricsDefault.Current.Measure(Text, new Font { Size = FontSize }).Width;
    }

    private static List<Drawn> Texts(string svg) =>
        [.. Regex.Matches(svg, @"<text x=""(?<x>-?[0-9.]+)"" y=""-?[0-9.]+"" font-family=""[^""]*"" font-size=""(?<fs>[0-9.]+)"" text-anchor=""(?<a>[a-z]+)""(?<rest>[^>]*)>(?<t>[^<]*)</text>")
            .Select(m => new Drawn(
                double.Parse(m.Groups["x"].Value, CultureInfo.InvariantCulture),
                double.Parse(m.Groups["fs"].Value, CultureInfo.InvariantCulture),
                m.Groups["a"].Value,
                m.Groups["rest"].Value.Contains("rotate(-90", StringComparison.Ordinal),
                m.Groups["t"].Value))];

    /// <summary>One Y axis slot as drawn: its edge (where its tick marks hang from), the far side of its widest tick
    /// label's ink, its title (when it has one) and which side it stands on.</summary>
    private readonly record struct DrawnSides(List<DrawnSlot> Left, List<DrawnSlot> Right);

    private readonly record struct DrawnSlot(AxisSide Side, double Edge, double TickLabelsFar, Drawn? Title, string[] Labels)
    {
        private double Out => Side == AxisSide.Left ? -1 : 1;

        /// <summary>The blank gap between the widest tick label's ink and the title's ink that faces it.</summary>
        public double TitleGap => Side == AxisSide.Left
            ? TickLabelsFar - (Title!.Value.X + FacingInk.TurnedSink * Title.Value.FontSize)
            : Title!.Value.X - FacingInk.TurnedRise * Title.Value.FontSize - TickLabelsFar;

        /// <summary>The far side of everything the slot draws: its title's outward ink, or its tick labels without one.</summary>
        public double OuterInk => Title is { } t
            ? (Side == AxisSide.Left ? t.X - FacingInk.Ascent * t.FontSize : t.X + FacingInk.Descent * t.FontSize)
            : TickLabelsFar;

        /// <summary>From the slot's edge to the far side of its ink: the band it draws.</summary>
        public double Band => Out * (OuterInk - Edge);
    }

    /// <summary>The slots of one side, nearest the plot first. Left tick labels are end-anchored, right ones
    /// start-anchored; each slot's labels share one anchor, which stands <c>Length + Pad</c> past its edge.</summary>
    private static List<DrawnSlot> Slots(string svg, Axes axes, AxisSide side)
    {
        var texts = Texts(svg);
        bool left = side == AxisSide.Left;
        var stack = left ? axes.YAxes.Left : axes.YAxes.Right;
        var groups = texts.Where(t => !t.Turned && t.Anchor == (left ? "end" : "start"))
            .GroupBy(t => t.X).OrderBy(g => left ? -g.Key : g.Key).ToList();
        var titles = texts.Where(t => t.Turned && (left ? t.X < Width / 2 : t.X > Width / 2))
            .OrderBy(t => left ? -t.X : t.X).ToList();
        Assert.Equal(stack.Count, groups.Count);
        var slots = new List<DrawnSlot>();
        int title = 0;
        for (int k = 0; k < groups.Count; k++)
        {
            var ticks = stack[k].Axis.MajorTicks;
            double reach = ticks.Length + ticks.Pad;
            double edge = left ? groups[k].Key + reach : groups[k].Key - reach;
            double far = left ? groups[k].Min(t => t.X - t.TextWidth) : groups[k].Max(t => t.X + t.TextWidth);
            Drawn? drawnTitle = stack[k].Axis.Label is null ? null : titles[title++];
            slots.Add(new DrawnSlot(side, edge, far, drawnTitle, [.. groups[k].Select(t => t.Text)]));
        }
        return slots;
    }

    // ── the figures ──────────────────────────────────────────────────────────

    /// <summary>Four Y axes, two to a side, each titled and each with tick labels of its own width.</summary>
    private static Figure FourAxes(double? titleSize = null, bool tight = false, Action<AxesBuilder>? more = null)
    {
        var builder = Plt.Create().WithSize(Width, Height);
        if (tight) builder = builder.TightLayout();
        var figure = builder.AddSubPlot(1, 1, 1, ax =>
        {
            ax.Plot(X, [0.0, 600, 1325]).SetYLabel("Delivered (msg/sec)")
                .WithAddLeftYAxis(l => l.Plot(X, [0.0, 40, 90]).SetYLabel("Queue (gp)"))
                .WithAddRightYAxis(r => r.Plot(X, [0.0, 400, 760]).SetYLabel("Traffic (KiB/sec)"))
                .WithAddRightYAxis(r => r.Plot(X, [0.0, 0.2, 0.5]).SetYLabel("Ratio (gp)"));
            more?.Invoke(ax);
        }).Build();
        if (titleSize is { } size)
        {
            foreach (var slot in figure.SubPlots[0].YAxes.IdentityOrder)
                slot.Axis.LabelStyle = (slot.Axis.LabelStyle ?? new TextStyle()) with { FontSize = size };
        }
        return figure;
    }

    private static DrawnSides Drawn4(Figure figure)
    {
        string svg = figure.ToSvg();
        var axes = figure.SubPlots[0];
        return new DrawnSides(Slots(svg, axes, AxisSide.Left), Slots(svg, axes, AxisSide.Right));
    }

    /// <summary>The fixture names its two sides (a record struct, not a tuple), so a failure reads as Left or Right.</summary>
    [Fact]
    public void TheFourAxesFixture_NamesItsLeftAndRightSides()
    {
        DrawnSides sides = Drawn4(FourAxes());

        Assert.NotEmpty(sides.Left);
        Assert.NotEmpty(sides.Right);
    }

    // ── A5b: the same gap on both sides, for every slot ─────────────────────

    /// <summary>The mirror pin (A5b): the gap between a Y title's ink and the ink of the widest tick label beside it is the
    /// same left and right, for the primary against right slot 0 and for the outward slots against each other, at the
    /// theme's title size and at two set ones.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData(13.0)]
    [InlineData(18.0)]
    public void TheYTitleInkGap_IsEqualLeftAndRight_ForEverySlotInclThePrimary(double? titleSize)
    {
        var (left, right) = Drawn4(FourAxes(titleSize));

        for (int k = 0; k < 2; k++)
        {
            Assert.True(Math.Abs(left[k].TitleGap - right[k].TitleGap) <= 0.5,
                $"slot {k}: the left title stands {left[k].TitleGap:F2} px from its tick labels, the right one {right[k].TitleGap:F2} px (title size {titleSize?.ToString(CultureInfo.InvariantCulture) ?? "theme"})");
        }
    }

    /// <summary>A5c: without a setting, that gap is 4 px on every slot of both sides (matplotlib's <c>axes.labelpad</c>).</summary>
    [Fact]
    public void TheDefaultYTitleInkGap_IsFourPixels()
    {
        var (left, right) = Drawn4(FourAxes());

        Assert.All(left.Concat(right), slot => Assert.True(Math.Abs(slot.TitleGap - 4) <= Tolerance,
            $"{slot.Side} slot at edge {slot.Edge:F2}: the title stands {slot.TitleGap:F2} px from its tick labels, not 4"));
    }

    /// <summary>The agreement with the owner: 1.23.0 stood a Y title's anchor 12 px past the far side of its tick labels, on
    /// both sides. The right title was where the eye wanted it (4 px of blank), so it stays where 1.23.0 had it; the left
    /// title stood 8 px too far and comes to the same 4 px, which is 8 px closer. Held at the theme's title size and at 13.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData(13.0)]
    public void TheRightTitle_StaysWhereVersion123PutIt_AndTheLeftOneComesEightPixelsCloser(double? titleSize)
    {
        var (left, right) = Drawn4(FourAxes(titleSize));

        double right123 = right[0].Title!.Value.X - right[0].TickLabelsFar;
        double left123 = left[0].TickLabelsFar - left[0].Title!.Value.X;

        Assert.True(Math.Abs(right123 - 12) <= 0.5,
            $"the right title's anchor stands {right123:F2} px past its tick labels; 1.23.0 stood it 12 px past them");
        Assert.True(Math.Abs(left123 - 4) <= 0.5,
            $"the left title's anchor stands {left123:F2} px past its tick labels; it comes 8 px closer than the 12 px of 1.23.0");
    }

    /// <summary>What the gap is made of does not depend on the font: the right title's anchor stands the gap plus 0.6 em past
    /// its tick labels, the left one's the gap alone, so a large title keeps its 4 px instead of touching its labels.</summary>
    [Theory]
    [InlineData(13.0)]
    [InlineData(18.0)]
    [InlineData(24.0)]
    public void TheTitleAnchorDistance_FollowsTheFontOnTheRightAndNotOnTheLeft(double titleSize)
    {
        var (left, right) = Drawn4(FourAxes(titleSize));

        Assert.Equal(4 + 0.6 * titleSize, right[0].Title!.Value.X - right[0].TickLabelsFar, 1);
        Assert.Equal(4, left[0].TickLabelsFar - left[0].Title!.Value.X, 1);
    }

    /// <summary>The container's gap (<see cref="AxesBuilder.WithYAxisTitleGap"/>) replaces the 4 px on every slot of both
    /// sides, the primary included, and between stacked slots.</summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(10.0)]
    public void AContainerTitleGap_OverridesTheDefaultOnBothSides(double gap)
    {
        var (left, right) = Drawn4(FourAxes(more: ax => ax.WithYAxisTitleGap(gap)));

        Assert.All(left.Concat(right), slot => Assert.True(Math.Abs(slot.TitleGap - gap) <= Tolerance,
            $"{slot.Side} slot at edge {slot.Edge:F2}: the title stands {slot.TitleGap:F2} px from its tick labels, not {gap}"));
        Assert.Equal(gap, left[0].OuterInk - left[1].Edge, 1);
        Assert.Equal(gap, right[1].Edge - right[0].OuterInk, 1);
    }

    /// <summary>A title's own pad (<see cref="TextStyle.Pad"/>) keeps its meaning: it moves that one title further out, on
    /// top of the container's gap, on either side.</summary>
    [Fact]
    public void LabelStylePad_AddsOnTopOfTheContainerGap()
    {
        var figure = FourAxes();
        var axes = figure.SubPlots[0];
        axes.YAxis.LabelStyle = new TextStyle { Pad = 3 };
        axes.YAxes.Right[0].Axis.LabelStyle = new TextStyle { Pad = 3 };

        var (left, right) = Drawn4(figure);

        Assert.Equal(4 + 3, left[0].TitleGap, 1);
        Assert.Equal(4 + 3, right[0].TitleGap, 1);
        Assert.Equal(4, left[1].TitleGap, 1);
        Assert.Equal(4, right[1].TitleGap, 1);
    }

    // ── D1: the left title inside the figure ────────────────────────────────

    /// <summary>The figure the gap was first measured on: an operations chart 1215 x 330 with a coloured primary and a
    /// coloured first right axis, plus a left axis past the primary.</summary>
    private static Figure Ops(Theme theme, bool tight, double? titleSize)
    {
        var x = Enumerable.Range(0, 120).Select(i => (double)i).ToArray();
        var builder = Plt.Create().WithSize(1215, 330).WithTheme(theme);
        if (tight) builder = builder.TightLayout();
        var figure = builder.AddSubPlot(1, 1, 1, ax => ax
                .Plot(x, [.. x.Select(v => v * 11.0)]).SetYLabel("Delivered (msg/sec)").SetYAxisColor(Colors.Blue)
                .WithAddRightYAxis(r => r.Plot(x, [.. x.Select(v => v * 6.3)]).SetYLabel("Traffic (KiB/sec)").SetYAxisColor(Colors.Orange)))
            .Build();
        if (titleSize is { } size)
        {
            foreach (var slot in figure.SubPlots[0].YAxes.IdentityOrder)
                slot.Axis.LabelStyle = (slot.Axis.LabelStyle ?? new TextStyle()) with { FontSize = size };
        }
        return figure;
    }

    /// <summary>D1: the ink of every left title stays inside the figure and the ink of every right title too, in both
    /// layout paths, at the theme's title size and a large one, and with the theme's whole font enlarged.</summary>
    [Theory]
    [InlineData(false, false, null)]
    [InlineData(false, false, 18.0)]
    [InlineData(true, false, null)]
    [InlineData(true, false, 18.0)]
    [InlineData(false, true, null)]
    [InlineData(true, true, null)]
    public void TheLeftTitleInk_NeverCrossesTheFigureEdge(bool tight, bool themeFont18, double? titleSize)
    {
        var theme = themeFont18 ? Theme.CreateFrom(Theme.OpsNight).WithFont(f => f with { Size = 18 }).Build() : Theme.OpsNight;
        var figure = Ops(theme, tight, titleSize);
        string svg = figure.ToSvg();
        var titles = Texts(svg).Where(t => t.Turned).ToList();
        string context = $"tight={tight}, themeFont18={themeFont18}, titleSize={titleSize?.ToString(CultureInfo.InvariantCulture) ?? "theme"}";

        var leftTitle = Assert.Single(titles, t => t.X < 1215 / 2.0);
        var rightTitle = Assert.Single(titles, t => t.X > 1215 / 2.0);
        Assert.True(leftTitle.X - FacingInk.Ascent * leftTitle.FontSize >= 0,
            $"the left title's ink starts at {leftTitle.X - FacingInk.Ascent * leftTitle.FontSize:F2}, left of the figure ({context})");
        Assert.True(rightTitle.X + FacingInk.Descent * rightTitle.FontSize <= 1215,
            $"the right title's ink ends at {rightTitle.X + FacingInk.Descent * rightTitle.FontSize:F2}, right of the figure ({context})");
    }

    // ── the reserve is the drawn band ───────────────────────────────────────

    /// <summary>The layout reserves for each slot exactly the band the slot draws, and the tight layout's margins end
    /// exactly the layout's pad past the outermost ink on each side: nothing clipped, nothing wasted.</summary>
    [Fact]
    public void TheLayoutReserve_EqualsTheDrawnBand_ForEverySlot()
    {
        var figure = FourAxes(tight: true);
        var axes = figure.SubPlots[0];
        var (left, right) = Drawn4(figure);
        var ctx = new SvgRenderContext();

        var rightReserve = ConstrainedLayoutEngine.SecondaryAxisBands(axes, ctx, figure.Theme);
        var leftReserve = ConstrainedLayoutEngine.LeftAxisBands(axes, ctx, figure.Theme);

        Assert.Equal(right.Count, rightReserve.Length);
        for (int k = 0; k < right.Count; k++)
            Assert.True(Math.Abs(rightReserve[k] - right[k].Band) <= Tolerance,
                $"right slot {k}: the layout reserves {rightReserve[k]:F2}, the slot draws {right[k].Band:F2}");
        Assert.Equal(left.Count - 1, leftReserve.Length);
        for (int k = 1; k < left.Count; k++)
            Assert.True(Math.Abs(leftReserve[k - 1] - left[k].Band) <= Tolerance,
                $"left slot {k}: the layout reserves {leftReserve[k - 1]:F2}, the slot draws {left[k].Band:F2}");
        // The primary is reserved inside the left margin: the outermost left ink stands the layout's pad from the figure's
        // edge, the outermost right ink the layout's right pad.
        Assert.True(Math.Abs(left[^1].OuterInk - ConstrainedLayoutEngine.PadLeft) <= Tolerance,
            $"the outermost left ink is at {left[^1].OuterInk:F2}, the layout reserved for {ConstrainedLayoutEngine.PadLeft}");
        Assert.True(Math.Abs(Width - right[^1].OuterInk - ConstrainedLayoutEngine.PadRight) <= Tolerance,
            $"the outermost right ink is at {right[^1].OuterInk:F2}, the layout reserved for {Width - ConstrainedLayoutEngine.PadRight}");
    }

    // ── stacking ─────────────────────────────────────────────────────────────

    /// <summary>Between the outermost ink of a slot and the tick marks of the slot stacked past it stands the same gap on
    /// the left and on the right: the container's title gap.</summary>
    [Fact]
    public void TheGapBetweenStackedSlots_IsEqualLeftAndRight()
    {
        var (left, right) = Drawn4(FourAxes());

        double leftGap = left[0].OuterInk - left[1].Edge;
        double rightGap = right[1].Edge - right[0].OuterInk;

        Assert.True(Math.Abs(leftGap - rightGap) <= 0.5, $"stacked slots stand {leftGap:F2} px apart on the left, {rightGap:F2} px on the right");
        Assert.True(Math.Abs(leftGap - 4) <= Tolerance, $"stacked slots stand {leftGap:F2} px apart on the left, not 4");
    }

    // ── W8: a slot is charged for the ticks it draws ─────────────────────────

    /// <summary>A slot given its own tick locator draws the ticks the locator places, and the layout reserves the band of
    /// those ticks, not of the ticks the slot would draw without it.</summary>
    [Fact]
    public void ASlotWithALocator_IsChargedForTheTicksItDraws()
    {
        var figure = FourAxes(tight: true);
        var axes = figure.SubPlots[0];
        axes.YAxes.Right[1].Axis.TickLocator = new MultipleLocator(0.125);
        axes.YAxes.Left[1].Axis.TickLocator = new FixedLocator([10.0, 45.0, 80.0]);

        var (left, right) = Drawn4(figure);
        var ctx = new SvgRenderContext();

        Assert.Contains("0.125", right[1].Labels);
        Assert.Contains("0.375", right[1].Labels);
        Assert.Equal(["10", "45", "80"], left[1].Labels.Order());
        double rightReserve = ConstrainedLayoutEngine.SecondaryAxisBands(axes, ctx, figure.Theme)[1];
        double leftReserve = ConstrainedLayoutEngine.LeftAxisBands(axes, ctx, figure.Theme)[0];
        Assert.True(Math.Abs(rightReserve - right[1].Band) <= Tolerance, $"right slot 1: reserved {rightReserve:F2}, drawn {right[1].Band:F2}");
        Assert.True(Math.Abs(leftReserve - left[1].Band) <= Tolerance, $"left slot 1: reserved {leftReserve:F2}, drawn {left[1].Band:F2}");
    }

    /// <summary>A slot on a log or a symlog scale draws the ticks its scale's locator places, and the layout reserves the
    /// band of those ticks: one tick source for the drawing and the reserve.</summary>
    [Theory]
    [InlineData(AxisScale.Log)]
    [InlineData(AxisScale.SymLog)]
    public void ASlotOnAScale_IsChargedForTheTicksItDraws(AxisScale scale)
    {
        var figure = FourAxes(tight: true);
        var axes = figure.SubPlots[0];
        axes.YAxes.Right[1].Axis.Scale = scale;
        axes.YAxes.Left[1].Axis.Scale = scale;

        var (left, right) = Drawn4(figure);
        var ctx = new SvgRenderContext();

        double rightReserve = ConstrainedLayoutEngine.SecondaryAxisBands(axes, ctx, figure.Theme)[1];
        double leftReserve = ConstrainedLayoutEngine.LeftAxisBands(axes, ctx, figure.Theme)[0];
        Assert.True(Math.Abs(rightReserve - right[1].Band) <= Tolerance, $"right slot 1: reserved {rightReserve:F2}, drawn {right[1].Band:F2}");
        Assert.True(Math.Abs(leftReserve - left[1].Band) <= Tolerance, $"left slot 1: reserved {leftReserve:F2}, drawn {left[1].Band:F2}");
    }
}
