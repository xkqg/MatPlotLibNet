// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Diagnostics;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The top X axis linked to the primary X axis (<see cref="Axis.Link"/>, matplotlib's <c>secondary_xaxis('top')</c>): it
/// holds no series and is drawn all the same, over the primary's own range and transform, by the rule a linked Y axis is drawn by.
/// Its ticks are laid out over the image of the primary's X range in its own unit, sorted low to high, by its own locator, else by
/// the log locator when the primary is logarithmic (matplotlib's <c>functionlog</c>), else by the default placement for the plot's
/// width; each is labelled with its value and stands where the primary shows the value it maps back to. It owns the top edge, so the
/// mirrored primary labels give way to it and the axes title rises above it, and the layout reserves the band it draws. A range its
/// transform cannot place draws no ticks and says so once, never throwing. A link never changes the primary's range.</summary>
/// <remarks>Joins the <c>ChartDiagnosticsGlobalState</c> collection: one test listens to the process-wide diagnostics.</remarks>
[Collection("ChartDiagnosticsGlobalState")]
public class LinkedTopAxisTests
{
    private static readonly Rect Plot = new(200, 50, 400, 300);
    private static readonly AxisTransform Fahrenheit = AxisTransform.Linear(1.8, 32);

    /// <summary>A plot in degrees Celsius whose X axis shows 0..100 exactly: 4 px per degree, 0 at the left edge (x 200).</summary>
    private static Axes Celsius()
    {
        var axes = new Axes();
        axes.Plot([0.0, 50, 100], [1.0, 3, 2]);
        axes.XAxis.Min = 0;
        axes.XAxis.Max = 100;
        return axes;
    }

    /// <summary>Gives <paramref name="axes"/> a top axis linked through <paramref name="link"/>, with no series of its own.</summary>
    private static Axes Linked(Axes axes, AxisTransform link)
    {
        axes.TwinY();
        axes.SecondaryXAxis!.Link = link;
        return axes;
    }

    private static RecordingRenderContext Render(Axes axes, Rect? plot = null)
    {
        var ctx = new RecordingRenderContext();
        new CartesianAxesRenderer(axes, plot ?? Plot, ctx, Theme.Default).Render();
        return ctx;
    }

    /// <summary>One upright tick label: its text and the X it is centred on.</summary>
    private readonly record struct Label(string Text, double X);

    private static IEnumerable<dynamic> UprightCentred(RecordingRenderContext ctx) =>
        ctx.OfKind("DrawText").Select(c => (dynamic)c.Args!)
            .Where(a => (double)a.rotation == 0 && (TextAlignment)a.alignment == TextAlignment.Center);

    /// <summary>The labels drawn above the top spine, in the order drawn.</summary>
    private static List<Label> TopLabels(RecordingRenderContext ctx) =>
        [.. UprightCentred(ctx).Where(a => ((Point)a.position).Y < Plot.Y).Select(a => new Label((string)a.text, ((Point)a.position).X))];

    /// <summary>The primary X axis' tick labels, below the bottom spine, in the order drawn.</summary>
    private static List<Label> BottomLabels(RecordingRenderContext ctx) =>
        [.. UprightCentred(ctx).Where(a => ((Point)a.position).Y > Plot.Y + Plot.Height)
            .Select(a => new Label((string)a.text, ((Point)a.position).X))];

    private static string Text(double value) => value.ToString(CultureInfo.InvariantCulture);

    /// <summary>The image of 0..100 °C is 32..212 °F; the default placement for a 400 px plot lays it out in steps of 25.</summary>
    private static readonly double[] FahrenheitTicks = [50, 75, 100, 125, 150, 175, 200];

    /// <summary>Where the plot in degrees Celsius shows the temperature that is <paramref name="fahrenheit"/> °F.</summary>
    private static double CelsiusX(double fahrenheit) => Plot.X + (fahrenheit - 32) / 1.8 * 4;

    // ── drawn without series ────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ALinkedTopAxisWithoutSeries_IsDrawn()
    {
        Assert.NotEmpty(TopLabels(Render(Linked(Celsius(), Fahrenheit))));
    }

    [Fact]
    public void FahrenheitAboveCelsius_LabelsEachTickWithItsValue_WhereThePrimaryShowsItsInverse()
    {
        var labels = TopLabels(Render(Linked(Celsius(), Fahrenheit)));

        Assert.Equal(FahrenheitTicks.Select(Text), labels.Select(l => l.Text));
        for (int i = 0; i < FahrenheitTicks.Length; i++)
            Assert.Equal(CelsiusX(FahrenheitTicks[i]), labels[i].X, 9);
    }

    [Fact]
    public void ALinkedTopAxis_IsPlacedOnThePrimarysRangeAndTransform_ItsTicksStoodByTheLink()
    {
        var renderer = new CartesianAxesRenderer(Linked(Celsius(), Fahrenheit), Plot, new RecordingRenderContext(), Theme.Default);
        var primary = renderer.ComputeDataRanges();

        var placed = renderer.PlaceTopAxis(primary);

        Assert.Equal(primary, placed.Range);
        Assert.Equal(renderer.ComputeViewTransform().DrawnRange, placed.Transform.DrawnRange);
        Assert.IsType<LinkedTickPlacement>(placed.Placement);
        Assert.Equal(FahrenheitTicks, placed.Ticks);
        Assert.Null(placed.Diagnostic);
    }

    [Fact]
    public void ALinkedTopAxis_OwnsTheTopEdge_SoTheMirroredPrimaryLabelsGiveWay()
    {
        var axes = Linked(Celsius(), Fahrenheit);
        axes.XAxis.MajorTicks = axes.XAxis.MajorTicks with { Mirror = true };

        Assert.Equal(FahrenheitTicks.Select(Text), TopLabels(Render(axes)).Select(l => l.Text));
    }

    [Fact]
    public void ALinkedTopAxisMinorTicks_AreSplitInItsOwnUnit_EachWhereThePrimaryShowsItsInverse()
    {
        var axes = Linked(Celsius(), Fahrenheit);
        var top = axes.SecondaryXAxis!;
        top.MinorTicks = top.MinorTicks with { Visible = true };

        var ctx = Render(axes);

        var minors = ctx.OfKind("DrawLine").Select(c => (dynamic)c.Args!)
            .Where(a => Math.Abs(((Point)a.p1).Y - (Plot.Y - top.MinorTicks.Length)) < 1e-9)
            .Select(a => ((Point)a.p1).X).ToList();
        foreach (double fahrenheit in new[] { 55.0, 60, 65, 70 })
            Assert.Contains(minors, x => Math.Abs(x - CelsiusX(fahrenheit)) < 1e-9);
    }

    // ── which ticks ─────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void OnALogPrimary_TheLinkedTopAxisTakesTheLogLocatorsDecades_WrittenByTheLogFormatter()
    {
        // A period above a plot over frequency 1..1000 Hz on a log axis: matplotlib draws the linked axis on its functionlog scale,
        // so its ticks are decades of the period 0.001..1 s, each above the place where the plot shows its frequency.
        var axes = new Axes();
        axes.Plot([1.0, 10, 100, 1000], [1.0, 2, 3, 4]);
        axes.XAxis.Scale = AxisScale.Log;
        axes.XAxis.Min = 1;
        axes.XAxis.Max = 1000;
        Linked(axes, AxisTransform.Reciprocal());

        var labels = TopLabels(Render(axes));

        var decades = new LogLocator().Locate(1.0 / 1000, 1);
        Assert.True(decades.Length >= 3, $"{decades.Length} decades");
        Assert.Equal(decades.Select(new LogTickFormatter().Format), labels.Select(l => l.Text));
        for (int i = 0; i < decades.Length; i++)
            Assert.Equal(Plot.X + Math.Log10(1 / decades[i]) / 3 * Plot.Width, labels[i].X, 6);
    }

    [Fact]
    public void ALinkedTopAxisOwnLocator_Wins()
    {
        var axes = Linked(Celsius(), Fahrenheit);
        axes.SecondaryXAxis!.TickLocator = new MultipleLocator(50);

        Assert.Equal(["50", "100", "150", "200"], TopLabels(Render(axes)).Select(l => l.Text));
    }

    [Fact]
    public void ATickWhoseInverseFallsInABreakOfThePrimary_IsLeftOut_AsThePrimaryLeavesItsOwnOut()
    {
        var axes = Linked(Celsius(), AxisTransform.Linear(1));
        axes.AddXBreak(35, 65);

        var ctx = Render(axes);
        var linked = TopLabels(ctx);

        Assert.Equal(BottomLabels(ctx), linked);
        Assert.DoesNotContain("50", linked.Select(l => l.Text));
        Assert.All(ctx.OfKind("DrawLine").Select(c => (dynamic)c.Args!), a =>
            Assert.True(double.IsFinite(((Point)a.p1).X) && double.IsFinite(((Point)a.p2).X), "a mark was handed a missing value"));
    }

    // ── a range the link cannot place ───────────────────────────────────────────────────────────────────

    [Fact]
    public void AReciprocalAcrossZero_DrawsNoTicks_AndSaysWhyOnce_WithoutThrowing()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot([-1.0, 0, 1], [1.0, 2, 3]).SetXLim(-1, 1)
            .WithSecondaryXAxis(period => period.Link(AxisTransform.Reciprocal()))).Build();
        var diagnostics = new List<ChartDiagnostic>();
        void Collect(ChartDiagnostic diagnostic) => diagnostics.Add(diagnostic);

        ChartDiagnostics.Emitted += Collect;
        string svg;
        try
        {
            svg = figure.ToSvg();
        }
        finally
        {
            ChartDiagnostics.Emitted -= Collect;
        }

        var said = Assert.Single(diagnostics);
        Assert.Equal(nameof(CartesianAxesRenderer), said.Source);
        Assert.Null(said.Exception);
        Assert.Contains("top X axis", said.Message, StringComparison.Ordinal);
        Assert.Contains("-1..1", said.Message, StringComparison.Ordinal);
        Assert.Contains("<svg", svg, StringComparison.Ordinal);
        Assert.Empty(TopLabels(Render(figure.SubPlots[0])));
    }

    // ── it owns the top edge: the layout reserves what is drawn, the title rises above it ─────────────────

    [Theory]
    [InlineData(400.0)]
    [InlineData(180.0)]
    public void TheBandTheLayoutReservesForALinkedTopAxis_IsTheBandItIsDrawnIn(double width)
    {
        // Turned labels reach as far as their text is long, and the default placement depends on the plot's width: the layout must
        // place the linked axis at the width it is drawn at. The band reaches the top of the axis' title.
        var plot = new Rect(Plot.X, Plot.Y, width, Plot.Height);
        var axes = Linked(Celsius(), Fahrenheit);
        var top = axes.SecondaryXAxis!;
        top.Label = "°F";
        top.LabelStyle = new TextStyle { Pad = 3, FontSize = 20 };
        top.LabelRotation = 30;

        double measured = AxesRenderer.Create(axes, plot, new RecordingRenderContext(), Theme.Default).MeasureEdgeBands().Top;
        var ctx = Render(axes, plot);

        var title = ctx.OfKind("DrawText").Select(c => (dynamic)c.Args!).Single(a => (string)a.text == "°F");
        double titleTop = ((Point)title.position).Y - ctx.MeasureText("°F", (Font)title.font).Height;
        Assert.Contains(ctx.OfKind("DrawText").Select(c => (dynamic)c.Args!), a => (double)a.rotation == 30);
        Assert.Equal(plot.Y - titleTop, measured, 9);
    }

    [Fact]
    public void TheAxesTitle_RisesByTheBandOfTheLinkedTopAxis()
    {
        static double TitleY(RecordingRenderContext ctx) =>
            ((Point)ctx.OfKind("DrawText").Select(c => (dynamic)c.Args!).Single(a => (string)a.text == "A Title").position).Y;
        var plain = Celsius();
        plain.Title = "A Title";
        var axes = Linked(Celsius(), Fahrenheit);
        axes.Title = "A Title";

        double band = AxesRenderer.Create(axes, Plot, new RecordingRenderContext(), Theme.Default).MeasureEdgeBands().Top;

        Assert.True(band > 0);
        Assert.Equal(TitleY(Render(plain)) - band, TitleY(Render(axes)), 9);
    }

    // ── no range of its own ─────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ALink_NeverChangesThePrimarysRange_NorItsTicks_WhateverLimitTheLinkedAxisCarries()
    {
        static Axes Data()
        {
            var axes = new Axes();
            axes.Plot([3.0, 40, 77], [1.0, 3, 2]);
            return axes;
        }
        var linked = Linked(Data(), Fahrenheit);
        linked.SecondaryXAxis!.Min = 500;
        linked.SecondaryXAxis.Max = 600;
        static DataRange RangeOf(Axes axes) =>
            new CartesianAxesRenderer(axes, Plot, new RecordingRenderContext(), Theme.Default).ComputeDataRanges();

        Assert.Equal(RangeOf(Data()), RangeOf(linked));
        Assert.Equal(BottomLabels(Render(Data())), BottomLabels(Render(linked)));
        Assert.NotEmpty(TopLabels(Render(linked)));
    }
}
