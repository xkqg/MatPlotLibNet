// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.CartesianParts;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.CartesianParts;

/// <summary><see cref="CartesianDirectLabelsPart"/> names a line at its end: a right-hand column of labels, each in its
/// line's colour, at the height of the line's last drawable point, pushed apart so no two overlap.</summary>
public class CartesianDirectLabelsPartUnitTests
{
    private static readonly Rect Plot = new(50, 40, 400, 300);
    private static readonly MinMaxRange AllX = new(0, 10);
    private static double PlotRight => Plot.X + Plot.Width;

    private static DataTransform Primary() => new(0, 10, 0, 10, Plot);
    private static DataTransform Secondary() => new(0, 10, 0, 100, Plot);

    /// <summary>The part for <paramref name="ax"/>: the primary placed over <paramref name="visibleX"/> with
    /// <paramref name="transform"/>, and right slot 0, when there is one, with <see cref="Secondary"/>.</summary>
    private static CartesianDirectLabelsPart Part(Axes ax, IRenderContext ctx, MinMaxRange? visibleX = null,
                                                  Theme? theme = null, DataTransform? transform = null)
    {
        var x = visibleX ?? AllX;
        var placed = new List<PlacedYSlot>
        {
            new() { Slot = ax.YAxes.Left[0], Range = new(x.Min, x.Max, 0, 10), Transform = transform ?? Primary(), Ticks = [], Source = default },
        };
        if (ax.YAxes.Right.Count > 0)
            placed.Add(new() { Slot = ax.YAxes.Right[0], Range = new(x.Min, x.Max, 0, 100), Transform = Secondary(), Ticks = [], Source = default });
        return new CartesianDirectLabelsPart(new CartesianPartContext(ax, Plot, ctx, theme ?? Theme.Default), placed, 0);
    }

    /// <summary>Draws every label the way the renderer does: each one in its series' turn, in identity order, the
    /// primary series first, then the right axis' ones.</summary>
    private static void DrawAll(CartesianDirectLabelsPart part, Axes ax)
    {
        for (int i = 0; i < ax.AllSeries.Count(); i++) part.DrawLabelOf(i);
    }

    /// <summary>A line on right slot 0, created when the axes has none.</summary>
    private static LineSeries RightLine(Axes ax, double[] x, double[] y) =>
        ax.YAxes.Right.FirstOrAdd().AddSeries(new LineSeries(x, y));

    private static LineSeries Labelled(LineSeries s, string label)
    {
        s.Label = label;
        s.LabelPlacement = SeriesLabelPlacement.LineEnd;
        return s;
    }

    private static List<dynamic> Texts(RecordingRenderContext ctx) =>
        ctx.Calls.Where(c => c.Kind is "DrawText" or "DrawRichText").Select(c => (dynamic)c.Args!).ToList();

    private static RecordingRenderContext Drawn(Axes ax, MinMaxRange? visibleX = null, Theme? theme = null)
    {
        var ctx = new RecordingRenderContext();
        DrawAll(Part(ax, ctx, visibleX, theme), ax);
        return ctx;
    }

    // ── what the label says and where it sits ──

    [Fact]
    public void TheText_IsTheSeriesLabel()
    {
        var ax = new Axes();
        Labelled(ax.Plot([0.0, 5, 10], [1.0, 2, 3]), "Revenue");

        var text = Assert.Single(Texts(Drawn(ax)));

        Assert.Equal("Revenue", (string)text.text);
        Assert.Equal(TextAlignment.Left, (TextAlignment)text.alignment);
    }

    [Fact]
    public void EveryLabel_SharesOneX_ThatLiesRightOfThePlotArea()
    {
        var ax = new Axes();
        Labelled(ax.Plot([0.0, 10], [1.0, 9]), "a");
        Labelled(ax.Plot([0.0, 6], [1.0, 5]), "bb");
        Labelled(ax.Plot([0.0, 3], [1.0, 1]), "ccc");

        var xs = Texts(Drawn(ax)).Select(t => (double)t.position.X).ToList();

        Assert.Equal(3, xs.Count);
        Assert.All(xs, x => Assert.Equal(xs[0], x, 9));
        Assert.Equal(PlotRight + DirectLabelMetrics.Gap, xs[0], 9);
    }

    [Fact]
    public void TheLabel_SitsAtTheHeightOfTheLastPoint()
    {
        var ax = new Axes();
        Labelled(ax.Plot([0.0, 4, 8], [1.0, 7, 3]), "end");
        var ctx = new RecordingRenderContext();
        var part = Part(ax, ctx);

        DrawAll(part, ax);

        var placed = Assert.Single(part.Placements);
        double endY = Primary().DataToPixel(8, 3).Y;
        Assert.Equal(endY, placed.Anchor.Y, 9);
        // The text box (one font size above the baseline) covers the end point.
        var text = Assert.Single(Texts(ctx));
        double baseline = (double)text.position.Y;
        double size = ((Font)text.font).Size;
        Assert.InRange(endY, baseline - size, baseline);
    }

    [Fact]
    public void ANaNLastValue_PlacesTheLabelAtThePreviousFinitePoint_AndNeverWritesNaN()
    {
        var ax = new Axes();
        ax.Legend = ax.Legend with { LegendValues = true };
        Labelled(ax.Plot([0.0, 4, 8], [1.0, 6.5, double.NaN]), "gap");
        var ctx = new RecordingRenderContext();
        var part = Part(ax, ctx);

        DrawAll(part, ax);

        var placed = Assert.Single(part.Placements);
        Assert.Equal(Primary().DataToPixel(4, 6.5).Y, placed.Anchor.Y, 9);
        string text = (string)Assert.Single(Texts(ctx)).text;
        Assert.Equal("gap = 6.50", text);
        Assert.DoesNotContain("NaN", text);
    }

    [Fact]
    public void XLimitsThatCutTheLine_PlaceTheLabelAtTheLastPointInsideTheVisibleRange()
    {
        var ax = new Axes();
        ax.Legend = ax.Legend with { LegendValues = true };
        Labelled(ax.Plot([0.0, 2, 4, 6, 8], [1.0, 2, 3, 4, 5]), "cut");
        var ctx = new RecordingRenderContext();
        var part = Part(ax, ctx, visibleX: new MinMaxRange(0, 5), transform: new DataTransform(0, 5, 0, 10, Plot));

        DrawAll(part, ax);

        var placed = Assert.Single(part.Placements);
        Assert.Equal(new DataTransform(0, 5, 0, 10, Plot).DataToPixel(4, 3).Y, placed.Anchor.Y, 9);
        Assert.Equal("cut = 3.00", (string)Assert.Single(Texts(ctx)).text);
    }

    [Fact]
    public void ASinglePoint_IsLabelledAtThatPoint()
    {
        var ax = new Axes();
        Labelled(ax.Plot([5.0], [2.0]), "one");
        var ctx = new RecordingRenderContext();
        var part = Part(ax, ctx);

        DrawAll(part, ax);

        Assert.Equal(Primary().DataToPixel(5, 2).Y, Assert.Single(part.Placements).Anchor.Y, 9);
        Assert.Equal("one", (string)Assert.Single(Texts(ctx)).text);
    }

    [Fact]
    public void ALineWithNoFinitePointInRange_HasNoLabel()
    {
        var ax = new Axes();
        Labelled(ax.Plot([0.0, 1], [double.NaN, double.NaN]), "nothing");
        Labelled(ax.Plot([20.0, 30], [1.0, 2]), "outside");
        Labelled(ax.Plot(Array.Empty<double>(), Array.Empty<double>()), "empty");

        Assert.Empty(Texts(Drawn(ax)));
    }

    [Fact]
    public void AnEndPointTheAxisCannotDraw_HasNoLabel()
    {
        // A log axis cannot place 0: the line has no drawable end there, so neither has the label.
        var ax = new Axes();
        Labelled(ax.Plot([1.0, 10], [1.0, 0.0]), "log");
        var logY = new DataTransform(0, 1, 0, 1, Plot, [], [], 1, 10, 1, 10, AxisScale.Linear, AxisScale.Log, 1, 1);
        var ctx = new RecordingRenderContext();

        DrawAll(Part(ax, ctx, visibleX: new MinMaxRange(1, 10), transform: logY), ax);

        Assert.Empty(Texts(ctx));
    }

    [Fact]
    public void ASecondaryLine_IsPlacedWithTheSecondaryTransform_InTheSlotAfterThePrimarySeries()
    {
        var cycler = new PropCyclerBuilder().WithColors(Colors.Red, Colors.Green, Colors.Blue).Build();
        var theme = Theme.CreateFrom(Theme.Default).WithPropCycler(cycler).Build();
        var ax = new Axes();
        ax.Plot([0.0, 10], [1.0, 2]);
        Labelled(RightLine(ax, [0.0, 10], [10.0, 80]), "margin");
        var ctx = new RecordingRenderContext();
        var part = Part(ax, ctx, theme: theme);

        DrawAll(part, ax);

        Assert.Equal(Secondary().DataToPixel(10, 80).Y, Assert.Single(part.Placements).Anchor.Y, 9);
        var text = Assert.Single(Texts(ctx));
        Assert.Equal("margin", (string)text.text);
        Assert.Equal(Colors.Green, ((Font)text.font).Color);
    }

    [Fact]
    public void EveryLabel_IsDrawnInItsOwnSeriesTurn_OnEitherAxis()
    {
        var ax = new Axes();
        Labelled(ax.Plot([0.0, 10], [1.0, 2]), "primary");
        Labelled(RightLine(ax, [0.0, 10], [10.0, 80]), "secondary");
        var ctx = new RecordingRenderContext();
        var part = Part(ax, ctx);

        part.DrawLabelOf(1);
        Assert.Equal(["secondary"], Texts(ctx).Select(t => (string)t.text));

        part.DrawLabelOf(0);
        Assert.Equal(["secondary", "primary"], Texts(ctx).Select(t => (string)t.text));
    }

    [Fact]
    public void DrawLabelOf_ThePrimarySeriesIdentitySlot_DrawsOnlyThePrimaryLabel()
    {
        // The key is the series' identity slot: 0 names the primary series, 1 the right axis' first series.
        var ax = new Axes();
        Labelled(ax.Plot([0.0, 10], [1.0, 2]), "primary");
        Labelled(RightLine(ax, [0.0, 10], [10.0, 80]), "secondary");
        var ctx = new RecordingRenderContext();

        Part(ax, ctx).DrawLabelOf(0);

        Assert.Equal(["primary"], Texts(ctx).Select(t => (string)t.text));
    }

    [Fact]
    public void DrawLabelOf_ASeriesWithoutAnEndLabel_DrawsNothing()
    {
        var ax = new Axes();
        ax.Plot([0.0, 10], [1.0, 2]).Label = "legend";
        Labelled(ax.Plot([0.0, 10], [1.0, 2]), "end");
        var ctx = new RecordingRenderContext();

        Part(ax, ctx).DrawLabelOf(0);

        Assert.Empty(Texts(ctx));
    }

    [Fact]
    public void TheLabel_WearsTheColourOfItsLine()
    {
        var ax = new Axes();
        var own = Labelled(ax.Plot([0.0, 10], [1.0, 2]), "own");
        own.Color = Colors.Magenta;
        Labelled(ax.Plot([0.0, 10], [5.0, 6]), "cycled");

        var colours = Texts(Drawn(ax)).ToDictionary(t => (string)t.text, t => ((Font)t.font).Color);

        Assert.Equal(Colors.Magenta, colours["own"]);
        Assert.Equal(Theme.Default.CycleColors[1], colours["cycled"]);
    }

    [Fact]
    public void TheFont_IsTheEndLabelFont()
    {
        var ax = new Axes();
        ax.Legend = ax.Legend with { FontSize = 17 };
        Labelled(ax.Plot([0.0, 10], [1.0, 2]), "sized");

        var font = (Font)Assert.Single(Texts(Drawn(ax))).font;

        Assert.Equal(17, font.Size);
    }

    [Fact]
    public void TheLegendValuesSuffix_IsWrittenInTheInvariantCulture()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("nl-NL");
            var ax = new Axes();
            ax.Legend = ax.Legend with { LegendValues = true };
            Labelled(ax.Plot([0.0, 10], [1.0, 1234.5]), "Revenue");

            Assert.Equal("Revenue = 1234.50", (string)Assert.Single(Texts(Drawn(ax))).text);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void AMathLabel_IsDrawnAsRichText()
    {
        var ax = new Axes();
        Labelled(ax.Plot([0.0, 10], [1.0, 2]), @"$\alpha_{max}$");
        var ctx = new RecordingRenderContext();

        DrawAll(Part(ax, ctx), ax);

        Assert.Equal(0, ctx.CountOf("DrawText"));
        var rich = Assert.Single(ctx.OfKind("DrawRichText"));
        Assert.Equal(MathTextParser.Parse(@"$\alpha_{max}$").Spans.Count, ((RichText)((dynamic)rich.Args!).richText).Spans.Count);
    }

    [Fact]
    public void AStreamingLine_IsLabelledAtItsNewestPoint()
    {
        var ax = new Axes();
        var live = ax.AddSeries(new StreamingLineSeries(capacity: 8) { Label = "live", LabelPlacement = SeriesLabelPlacement.LineEnd });
        live.AppendPoint(1, 2);
        live.AppendPoint(9, 4);
        var ctx = new RecordingRenderContext();
        var part = Part(ax, ctx);

        DrawAll(part, ax);

        Assert.Equal(Primary().DataToPixel(9, 4).Y, Assert.Single(part.Placements).Anchor.Y, 9);
    }

    // ── what draws nothing ──

    [Fact]
    public void AHiddenSeries_DrawsNoLabel()
    {
        var ax = new Axes();
        Labelled(ax.Plot([0.0, 10], [1.0, 2]), "hidden").Visible = false;

        Assert.Empty(Texts(Drawn(ax)));
    }

    [Fact]
    public void AnEmptyLabel_DrawsNothing()
    {
        var ax = new Axes();
        Labelled(ax.Plot([0.0, 10], [1.0, 2]), "");
        ax.Plot([0.0, 10], [1.0, 2]).LabelPlacement = SeriesLabelPlacement.LineEnd;

        Assert.Empty(Texts(Drawn(ax)));
    }

    [Fact]
    public void ALegendPlacement_DrawsNothing()
    {
        var ax = new Axes();
        ax.Plot([0.0, 10], [1.0, 2]).Label = "in the legend";

        Assert.Empty(Texts(Drawn(ax)));
    }

    // ── de-collision ──

    public static TheoryData<int> ClusterSizes => [2, 4, 8, 12];

    /// <summary>The vertical distance between neighbouring labels, in the order the anchors run down the plot.</summary>
    private static void AssertSeparated(CartesianDirectLabelsPart part, RecordingRenderContext ctx, int count)
    {
        var texts = Texts(ctx);
        Assert.Equal(count, texts.Count);
        double height = texts.Max(t => ctx.MeasureText((string)t.text, (Font)t.font).Height);
        var byAnchor = part.Placements.OrderBy(p => p.Anchor.Y).ThenBy(p => p.Label.IndexInPair).ToList();
        for (int k = 1; k < byAnchor.Count; k++)
        {
            double gap = byAnchor[k].Position.Y - byAnchor[k - 1].Position.Y;
            Assert.True(gap >= height - 1e-6, $"labels {k - 1} and {k} are {gap:F4} px apart, a label is {height:F4} px high");
        }
        Assert.All(part.Placements, p => Assert.Equal(part.Placements[0].Position.X, p.Position.X, 9));
    }

    [Theory]
    [MemberData(nameof(ClusterSizes))]
    public void LabelsAnchoredOnePixelApart_AreAtLeastOneLabelHeightApart(int count)
    {
        var ax = new Axes();
        // y values 1 px apart on a 300 px tall plot over 0..10.
        for (int i = 0; i < count; i++)
            Labelled(ax.Plot([0.0, 10], [5.0, 5.0 + i * 10.0 / 300]), $"L{i}");
        var ctx = new RecordingRenderContext();
        var part = Part(ax, ctx);

        DrawAll(part, ax);

        AssertSeparated(part, ctx, count);
    }

    [Theory]
    [MemberData(nameof(ClusterSizes))]
    public void AClusterAtTheBottomEdge_IsPushedUp_AndStaysSeparated(int count)
    {
        var ax = new Axes();
        for (int i = 0; i < count; i++)
            Labelled(ax.Plot([0.0, 10], [5.0, i * 10.0 / 300]), $"bottom {i}");
        var ctx = new RecordingRenderContext();
        var part = Part(ax, ctx);

        DrawAll(part, ax);

        AssertSeparated(part, ctx, count);
        Assert.All(part.Placements, p => Assert.True(p.Position.Y <= Plot.Y + Plot.Height + ((Font)p.Font).Size,
            $"a label at {p.Position.Y:F2} hangs below the plot"));
    }

    [Fact]
    public void Spreading_KeepsTheOrderOfTheEnds()
    {
        var ax = new Axes();
        Labelled(ax.Plot([0.0, 10], [1.0, 5.02]), "lower");
        Labelled(ax.Plot([0.0, 10], [1.0, 5.0]), "middle");
        Labelled(ax.Plot([0.0, 10], [1.0, 5.04]), "upper");
        var ctx = new RecordingRenderContext();
        var part = Part(ax, ctx);

        DrawAll(part, ax);

        // Higher value = smaller pixel y = drawn above.
        Assert.Equal(["upper", "lower", "middle"],
            part.Placements.OrderBy(p => p.Position.Y).Select(p => p.Label.Series.Label));
    }
}
