// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Interaction;

/// <summary>
/// An axes that scales itself has no limits, and a pan has nothing to shift until the reader has zoomed once.
/// The pointer's side knows the range that is on screen, so the event carries it and the pan pins and moves it.
/// A limit the axes already has still wins: the reader may have set it on purpose.
/// </summary>
public class PanEventInViewTests
{
    private static Figure Line() => Plt.Create().Plot([0.0, 1.0], [0.0, 1.0]).Build();

    [Fact]
    public void AnAutoScaledAxes_IsPinnedAtTheRangeInViewAndMoved()
    {
        var figure = Line();

        new PanEvent("c", 0, DxData: 5, DyData: 2) { InView = new DataRange(10, 20, 100, 200) }.ApplyTo(figure);

        var axes = figure.SubPlots[0];
        Assert.Equal(15.0, axes.XAxis.Min);
        Assert.Equal(25.0, axes.XAxis.Max);
        Assert.Equal(102.0, axes.YAxis.Min);
        Assert.Equal(202.0, axes.YAxis.Max);
    }

    [Fact]
    public void ALimitTheAxesAlreadyHas_WinsOverTheRangeInView()
    {
        var figure = Line();
        figure.SubPlots[0].XAxis.Min = 0;
        figure.SubPlots[0].XAxis.Max = 1;

        new PanEvent("c", 0, DxData: 5, DyData: 0) { InView = new DataRange(10, 20, 100, 200) }.ApplyTo(figure);

        Assert.Equal(5.0, figure.SubPlots[0].XAxis.Min);
        Assert.Equal(6.0, figure.SubPlots[0].XAxis.Max);
    }

    [Fact]
    public void WithoutARangeInView_AnAutoScaledAxesStaysAsItWas()
    {
        var figure = Line();

        new PanEvent("c", 0, DxData: 5, DyData: 5).ApplyTo(figure);

        Assert.Null(figure.SubPlots[0].XAxis.Min);
        Assert.Null(figure.SubPlots[0].YAxis.Min);
    }

    [Fact]
    public void ATimelineAxes_IsPinnedAlongXAndLeftAloneAlongY()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.StateTimeline([new StateSegment(0, 10, "A", Colors.Blue)]))
            .Build();

        new PanEvent("c", 0, DxData: 5, DyData: 5) { InView = new DataRange(0, 10, 0, 1) }.ApplyTo(figure);

        Assert.Equal(5.0, figure.SubPlots[0].XAxis.Min);
        Assert.Null(figure.SubPlots[0].YAxis.Min);
    }

    [Fact]
    public void TheModifier_SendsTheRangeItWasLookingAt()
    {
        var figure = Line();
        var seen = new List<FigureInteractionEvent>();
        var layout = ChartLayout.Create(figure, [new Rect(0, 0, 100, 50)]);
        var pan = new PanModifier("c", layout, seen.Add);

        pan.OnPointerPressed(new PointerInputArgs(40, 25, PointerButton.Left, ModifierKeys.None));
        pan.OnPointerMoved(new PointerInputArgs(30, 25, PointerButton.Left, ModifierKeys.None));

        var sent = Assert.IsType<PanEvent>(Assert.Single(seen));
        Assert.Equal(layout.GetDataRange(0), sent.InView);
    }

    [Fact]
    public void AVerticalLimitWithOnlyOneEndSet_AndNoRangeInView_StaysAsItWas()
    {
        // Half a pair of limits is not a range to move: shifting only one end would stretch the axes.
        var figure = Line();
        figure.SubPlots[0].YAxis.Min = 3;

        new PanEvent("c", 0, DxData: 0, DyData: 5).ApplyTo(figure);

        Assert.Equal(3.0, figure.SubPlots[0].YAxis.Min);
        Assert.Null(figure.SubPlots[0].YAxis.Max);
    }
}
