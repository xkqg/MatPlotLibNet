// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Diagnostics;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A linked X axis below the plot (matplotlib 3.11.2's <c>ax.secondary_xaxis('bottom')</c> or
/// <c>ax.secondary_xaxis(-0.2)</c>, both printed as a <c>SecondaryAxis</c> by <c>python -I</c>): it shows the primary X axis' range
/// in other units, has no series of its own, and stands in the same container as the stacked Y axes, its bottom stack. Stacked, it
/// stands below everything the primary X axis draws (its tick labels and its title), each further one below the one before; given
/// a location, at that fraction of the plot's height from the bottom spine, as matplotlib's float location puts it. The layout
/// reserves what it draws, and the JSON carries it. A link given to a primary axis, which has nothing to link to, is reported
/// once a draw.</summary>
/// <remarks>Joins the <c>ChartDiagnosticsGlobalState</c> collection: one test listens to the process-wide diagnostics.</remarks>
[Collection("ChartDiagnosticsGlobalState")]
public class LinkedBottomAxisTests
{
    private static readonly Rect Plot = new(200, 50, 400, 300);

    /// <summary>A plot in degrees Celsius over 0..100, titled, with a linked axis in degrees Fahrenheit below it.</summary>
    private static Figure Celsius(Func<XAxisSlotBuilder, XAxisSlotBuilder> bottom) => Plt.Create().AddSubPlot(1, 1, 1, ax => ax
        .Plot([0.0, 50, 100], [1.0, 3, 2]).SetXLim(0, 100).SetXLabel("°C")
        .WithAddBottomXAxis(b => bottom(b.SetXLabel("°F").Link(AxisTransform.Linear(1.8, 32))))).Build();

    private static IEnumerable<DrawTextCall> Texts(RecordingRenderContext ctx) => ctx.Of<DrawTextCall>();

    private static double YOf(RecordingRenderContext ctx, string text) =>
        Texts(ctx).Where(a => a.Text == text).Select(a => (a.Position).Y).Single();

    /// <summary>The lowest label reading <paramref name="text"/>: the °F axis' one where the °C axis reads the same number.</summary>
    private static Point Lowest(RecordingRenderContext ctx, string text) =>
        Texts(ctx).Where(a => a.Text == text).Select(a => a.Position).MaxBy(p => p.Y);

    [Fact]
    public void WithAddBottomXAxis_StacksALinkedXAxis_OnTheBottomOfTheContainer()
    {
        var axes = Celsius(b => b).SubPlots[0];

        var slot = Assert.Single(axes.YAxes.Bottom);
        Assert.Equal(AxisSide.Bottom, slot.Side);
        Assert.Equal("°F", slot.Axis.Label);
        Assert.NotNull(slot.Axis.Link);
        Assert.DoesNotContain(axes.YAxes.IdentityOrder, s => s.Side == AxisSide.Bottom);
    }

    [Fact]
    public void AStackedBottomAxis_StandsBelowThePrimarysTickLabelsAndTitle()
    {
        var ctx = Celsius(b => b).SubPlots[0].Drawn(Plot);

        double primaryTitle = YOf(ctx, "°C");
        // 0 °C is 32 °F: the image of 0..100 is 32..212, laid out from 50 up; 50 °F stands where the plot has 10 °C.
        var fahrenheit = Lowest(ctx, "50");
        Assert.True(fahrenheit.Y > primaryTitle, "the °F labels stand below the °C title");
        Assert.Equal(Plot.X + (50 - 32) / 1.8 * 4, fahrenheit.X, 9);
        Assert.True(YOf(ctx, "°F") > fahrenheit.Y, "the °F title stands below its labels");
    }

    [Fact]
    public void AtLocationMinusPointTwo_TheAxisStandsAFifthOfThePlotBelowItsBottomSpine_BelowThePrimarysTicks()
    {
        var axes = Celsius(b => b.SetLocation(-0.2)).SubPlots[0];
        var ctx = axes.Drawn(Plot);

        // The spine of the °F axis: a line across the plot 60 px (0.2 of 300) below the bottom spine at 350.
        Assert.Contains(ctx.Of<DrawLineCall>(), a =>
            (a.P1).Y == 410 && (a.P2).Y == 410 && (a.P1).X == Plot.X && (a.P2).X == Plot.X + Plot.Width);
        double primaryLabel = YOf(ctx, "0");
        Assert.True(Lowest(ctx, "50").Y > 410);
        Assert.True(410 > primaryLabel, "the axis stands below the primary's tick labels");
        Assert.Equal(-0.2, axes.YAxes.Bottom[0].Location);
    }

    /// <summary>A plot in degrees Celsius whose bottom spine stands at <paramref name="fraction"/> of the axes' height (matplotlib's
    /// <c>spines['bottom'].set_position(('axes', f))</c>), with a linked axis in degrees Fahrenheit below it.</summary>
    private static Axes MovedSpine(double fraction) => Plt.Create().AddSubPlot(1, 1, 1, ax => ax
        .Plot([0.0, 50, 100], [1.0, 3, 2]).SetXLim(0, 100).SetXLabel("°C")
        .WithSpines(s => s with { Bottom = s.Bottom with { Position = SpinePosition.Axes, PositionValue = fraction } })
        .WithAddBottomXAxis(b => b.SetXLabel("°F").Link(AxisTransform.Linear(1.8, 32)))).Build().SubPlots[0];

    /// <summary>The y of the line the linked axis draws across the plot, its own edge: the lowest line across it below the plot (a
    /// spine moved out below the plot draws its own above it).</summary>
    private static double LinkedLineY(RecordingRenderContext ctx) =>
        ctx.Of<DrawLineCall>()
            .Where(a => (a.P1).X == Plot.X && (a.P2).X == Plot.X + Plot.Width && (a.P1).Y == (a.P2).Y
                && (a.P1).Y > Plot.Y + Plot.Height)
            .Select(a => (a.P1).Y).Max();

    /// <summary>A spine moved up into the plot takes its tick labels and title with it, so nothing of the primary stands below the
    /// plot's edge: the linked axis stands the container's gap below the edge, as below a plot that draws nothing there, not where
    /// the labels would be had the spine stayed (matplotlib 3.11.2 places a secondary axis from the axes' edge, in the parent's
    /// transAxes, lib/matplotlib/axes/_secondary_axes.py:118-120: its spine stays at 39.14 px whether the primary's spine is at the
    /// edge or at ('axes', 0.5), measured).</summary>
    [Fact]
    public void NextToASpineMovedIntoThePlot_TheLinkedAxisStandsTheGapBelowTheEdge()
    {
        var axes = MovedSpine(0.5);

        var ctx = axes.Drawn(Plot);

        Assert.Equal(Plot.Y + Plot.Height + axes.YAxes.TitleGapUnder(Theme.Default), LinkedLineY(ctx), 9);
    }

    /// <summary>A spine moved out below the plot hangs its tick labels and title lower: the linked axis stands its gap below them,
    /// never over them (matplotlib's secondary axis at 'bottom' stays on the edge and overlaps them, 0.94..34.28 px against the
    /// primary's -26.83..6.50 at ('outward', 20); the library stacks its linked axes below what stands above them).</summary>
    [Fact]
    public void NextToASpineMovedOutOfThePlot_TheLinkedAxisStandsBelowThePrimarysTitle()
    {
        var axes = MovedSpine(-0.1);

        var ctx = axes.Drawn(Plot);

        Assert.True(LinkedLineY(ctx) > YOf(ctx, "°C"), $"the °F axis' line ({LinkedLineY(ctx)}) stands below the °C title ({YOf(ctx, "°C")})");
    }

    /// <summary>The band the layout keeps below the plot is the stack as drawn next to the moved spine: from the plot's edge to the
    /// far side of the linked axis' title, the spine in or out.</summary>
    [Theory]
    [InlineData(0.5)]
    [InlineData(-0.1)]
    public void NextToAMovedSpine_TheLayoutReservesTheStackAsItIsDrawn(double fraction)
    {
        var axes = MovedSpine(fraction);
        var reserved = new CartesianAxesRenderer(axes, Plot, new RecordingRenderContext(), Theme.Default).MeasureEdgeBands().BottomExtent;
        var ctx = axes.Drawn(Plot);

        double title = YOf(ctx, "°F");
        Assert.True(Plot.Y + Plot.Height + reserved >= title, $"the °F title ({title}) lies inside the band reserved ({Plot.Y + Plot.Height + reserved})");
        Assert.True(Plot.Y + Plot.Height + reserved - title < 20, "and the band is no wider than the title's own depth");
    }

    [Fact]
    public void TwoStackedBottomAxes_StandOneBelowTheOther()
    {
        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot([0.0, 50, 100], [1.0, 3, 2]).SetXLim(0, 100)
            .WithAddBottomXAxis(b => b.SetXLabel("°F").Link(AxisTransform.Linear(1.8, 32)))
            .WithAddBottomXAxis(b => b.SetXLabel("K").Link(AxisTransform.Linear(1, 273.15)))).Build().SubPlots[0];

        var ctx = axes.Drawn(Plot);

        Assert.True(YOf(ctx, "K") > YOf(ctx, "°F"));
    }

    [Fact]
    public void TheLayout_ReservesTheBandOfTheBottomAxes()
    {
        var with = new CartesianAxesRenderer(Celsius(b => b).SubPlots[0], Plot, new RecordingRenderContext(), Theme.Default);
        var without = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 50, 100], [1.0, 3, 2]).SetXLim(0, 100).SetXLabel("°C")).Build();
        var plain = new CartesianAxesRenderer(without.SubPlots[0], Plot, new RecordingRenderContext(), Theme.Default);

        double reserved = with.MeasureEdgeBands().BottomExtent;
        var ctx = Celsius(b => b).SubPlots[0].Drawn(Plot);

        Assert.True(reserved > plain.MeasureEdgeBands().BottomExtent);
        Assert.True(Plot.Y + Plot.Height + reserved >= YOf(ctx, "°F"), "the °F title lies inside the band reserved");
    }

    [Fact]
    public void TheJson_CarriesTheBottomAxes_TheirLinkAndLocation()
    {
        var serializer = new ChartSerializer();
        var figure = Celsius(b => b.SetLocation(-0.25));

        var read = figure.RoundTripped().SubPlots[0];

        var slot = Assert.Single(read.YAxes.Bottom);
        Assert.Equal("°F", slot.Axis.Label);
        Assert.Equal(-0.25, slot.Location);
        Assert.Equal(212, slot.Axis.Link!.Forward(100), 9);
        Assert.Equal(serializer.ToJson(figure), serializer.ToJson(figure.RoundTripped()));
        Assert.DoesNotContain("bottomXAxes", serializer.ToJson(Plt.Create().Plot([1.0, 2], [1.0, 2]).Build()), StringComparison.Ordinal);
    }

    [Fact]
    public void ABottomAxis_HoldsNoSeriesOfItsOwn()
    {
        var slot = new Axes().YAxes.Bottom.Add();

        Assert.Throws<InvalidOperationException>(() => slot.AddSeries(new LineSeries([1.0], [1.0])));
    }

    [Fact]
    public void ABottomAxisWithoutALink_DrawsItsLineAndTitleOnly()
    {
        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot([0.0, 50, 100], [1.0, 3, 2]).SetXLim(0, 100)
            .WithAddBottomXAxis(b => b.SetXLabel("nothing"))
            .WithAddBottomXAxis(b => b.Link(AxisTransform.Linear(1.8, 32)))).Build().SubPlots[0];

        var ctx = axes.Drawn(Plot);

        Assert.Single(Texts(ctx), a => a.Text == "nothing");
        Assert.Contains(Texts(ctx), a => a.Text == "212" || a.Text == "200");
    }

    /// <summary>A turned label of a linked axis below the plot hangs as a turned label of the primary X axis does, from its end: one
    /// rule for a label below the plot, whichever axis draws it. matplotlib 3.11.2 (<c>python -I</c>, <c>ax.tick_params(axis='x',
    /// labelrotation=30)</c> and the same on <c>ax.secondary_xaxis(-0.2)</c>) aligns the label of the secondary axis below as it aligns the
    /// primary's: <c>label1.get_ha()</c> is <c>'center'</c> on both, <c>get_rotation_mode()</c> <c>'default'</c> on both. The library's
    /// themes hang a turned label below the plot from its end (the primary's rule since the first release), so the linked axis does
    /// too; it hung from its start, the rule of a label above the plot.</summary>
    [Fact]
    public void ATurnedLabelOfALinkedBottomAxis_HangsFromItsEnd_AsATurnedLabelOfThePrimaryBottomAxisDoes()
    {
        var axes = Celsius(b => b).SubPlots[0];
        axes.XAxis.MajorTicks = axes.XAxis.MajorTicks with { LabelRotation = 45 };
        axes.YAxes.Bottom[0].Axis.MajorTicks = axes.YAxes.Bottom[0].Axis.MajorTicks with { LabelRotation = 45 };

        var ctx = axes.Drawn(Plot);

        var turned = Texts(ctx).Where(a => a.Rotation == 45).ToList();
        double primaryTitle = YOf(ctx, "°C");
        Assert.Contains(turned, a => (a.Position).Y < primaryTitle);
        Assert.Contains(turned, a => (a.Position).Y > primaryTitle);
        Assert.All(turned, a => Assert.Equal(TextAlignment.Right, a.Alignment));
    }

    [Fact]
    public void AColouredBottomAxis_DrawsItsLineInItsColour_AndALocationThatIsNotFiniteIsRefused()
    {
        var red = Color.FromHex("#D62728");
        var axes = Celsius(b => b.SetXAxisColor(red).SetLocation(-0.2)).SubPlots[0];

        var ctx = axes.Drawn(Plot);

        Assert.Contains(ctx.Of<DrawLineCall>(), a => (a.P1).Y == 410 && a.Color == red);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2], [1.0, 2]).WithAddBottomXAxis(b => b.SetLocation(double.NaN))).Build());
        Assert.Throws<ArgumentNullException>(() => Plt.Create().AddSubPlot(1, 1, 1, ax => ax.WithAddBottomXAxis(null!)).Build());
    }

    [Fact]
    public void ABottomLinkThatCannotPlaceTheRange_DrawsNoTicks_AndSaysWhyOnce()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot([-1.0, 0, 1], [1.0, 2, 3]).SetXLim(-1, 1)
            .WithAddBottomXAxis(period => period.Link(AxisTransform.Reciprocal()))).Build();
        var diagnostics = DiagnosticsCapture.During(() => figure.ToSvg());

        Assert.Contains("bottom X axis", Assert.Single(diagnostics).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ALinkOnAPrimaryAxis_IsReportedOnceADraw()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2, 3], [1.0, 2, 3])).Build();
        figure.SubPlots[0].XAxis.Link = AxisTransform.Linear(2, 0);
        var diagnostics = DiagnosticsCapture.During(() => figure.ToSvg());

        var said = Assert.Single(diagnostics);
        Assert.Equal(nameof(CartesianAxesRenderer), said.Source);
        Assert.Contains("primary X axis", said.Message, StringComparison.Ordinal);
    }
}
