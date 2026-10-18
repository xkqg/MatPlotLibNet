// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Interaction;

/// <summary>
/// A replay shows one timeline per category, stacked, and they only make sense on the same stretch of time.
/// <c>ShareX</c> already said so for the automatic scale; pan and zoom ignored it, so zooming one lane left the
/// others where they were. A shared X axis now moves together, whichever lane the reader's pointer is over.
/// Raised in discussion #22.
/// </summary>
public class SharedTimeWindowTests
{
    private static readonly StateSegment[] Lane = [new(0, 50, "Running", Colors.Blue)];

    private static Figure Stacked(bool shared = true)
    {
        var figure = Plt.Create()
            .AddSubPlot(3, 1, 1, ax => ax.StateTimeline(Lane).SetXLim(0, 100), key: "time")
            .AddSubPlot(3, 1, 2, ax => { ax.StateTimeline(Lane).SetXLim(0, 100); if (shared) ax.ShareX("time"); })
            .AddSubPlot(3, 1, 3, ax => { ax.StateTimeline(Lane).SetXLim(0, 100); if (shared) ax.ShareX("time"); })
            .Build();
        return figure;
    }

    private static InteractionController Control(Figure figure) =>
        InteractionController.CreateLocal(figure, ChartLayout.Create(figure,
            [new Rect(0, 0, 100, 20), new Rect(0, 25, 100, 20), new Rect(0, 50, 100, 20)]));

    [Fact]
    public void ZoomingOneLane_ZoomsEveryLaneThatSharesItsTime()
    {
        var figure = Stacked();

        Control(figure).HandleScroll(new ScrollInputArgs(50, 10, 0, -3.0, ModifierKeys.None));

        var first = figure.SubPlots[0].XAxis;
        Assert.True(first.Max!.Value - first.Min!.Value < 100.0);
        foreach (var lane in figure.SubPlots)
        {
            Assert.Equal(first.Min, lane.XAxis.Min);
            Assert.Equal(first.Max, lane.XAxis.Max);
        }
    }

    [Fact]
    public void PanningTheLastLane_MovesTheFirstOneToo()
    {
        // The link runs both ways: the lane that carries the key and the lanes that point at it are one group.
        var figure = Stacked();
        var controller = Control(figure);

        controller.HandlePointerPressed(new PointerInputArgs(40, 60, PointerButton.Left, ModifierKeys.None));
        controller.HandlePointerMoved(new PointerInputArgs(30, 60, PointerButton.Left, ModifierKeys.None));
        controller.HandlePointerReleased(new PointerInputArgs(30, 60, PointerButton.Left, ModifierKeys.None));

        Assert.Equal(10.0, figure.SubPlots[0].XAxis.Min!.Value, precision: 6);
        Assert.Equal(10.0, figure.SubPlots[1].XAxis.Min!.Value, precision: 6);
        Assert.Equal(10.0, figure.SubPlots[2].XAxis.Min!.Value, precision: 6);
    }

    [Fact]
    public void LanesThatDoNotShareTheirTime_AreLeftAlone()
    {
        var figure = Stacked(shared: false);

        Control(figure).HandleScroll(new ScrollInputArgs(50, 10, 0, -3.0, ModifierKeys.None));

        Assert.NotEqual(0.0, figure.SubPlots[0].XAxis.Min);
        Assert.Equal(0.0, figure.SubPlots[1].XAxis.Min);
        Assert.Equal(0.0, figure.SubPlots[2].XAxis.Min);
    }

    [Fact]
    public void AnAxesSharedWithAnotherAfterwards_FormsOneGroupWithItsTarget()
    {
        // Only the second lane points at the first; the third is on its own. Zooming the third leaves them be.
        var figure = Plt.Create()
            .AddSubPlot(3, 1, 1, ax => ax.StateTimeline(Lane).SetXLim(0, 100), key: "time")
            .AddSubPlot(3, 1, 2, ax => ax.StateTimeline(Lane).SetXLim(0, 100).ShareX("time"))
            .AddSubPlot(3, 1, 3, ax => ax.StateTimeline(Lane).SetXLim(0, 100))
            .Build();

        Control(figure).HandleScroll(new ScrollInputArgs(50, 60, 0, -3.0, ModifierKeys.None));

        Assert.Equal(0.0, figure.SubPlots[0].XAxis.Min);
        Assert.Equal(0.0, figure.SubPlots[1].XAxis.Min);
        Assert.NotEqual(0.0, figure.SubPlots[2].XAxis.Min);
    }

    [Fact]
    public void OnADateAxis_TheTooltipNamesTheEventAndItsTimes()
    {
        var start = new DateTime(2026, 6, 1, 0, 0, 0);
        StateSegment[] shift = [new(start.ToOADate(), start.AddHours(6).ToOADate(), "Running", Colors.Blue)];
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.StateTimeline(shift).SetXDateAxis()).Build();
        var layout = ChartLayout.Create(figure, [new Rect(0, 0, 100, 50)]);

        var result = NearestPointFinder.Find(figure, 0, start.AddHours(1).ToOADate(), 0.5, layout);

        Assert.Equal("Running (2026-06-01 00:00–2026-06-01 06:00)", result!.Detail);
    }

    [Theory]
    [InlineData(-1.0e7)]
    [InlineData(5.0e6)]
    public void OnADateAxis_AValueNoDateCanHold_IsShownAsANumberInsteadOfFailingTheHover(double outOfRange)
    {
        StateSegment[] odd = [new(outOfRange, outOfRange + 10, "Odd", Colors.Blue)];
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.StateTimeline(odd).SetXDateAxis().SetXLim(outOfRange - 5, outOfRange + 20)).Build();
        var layout = ChartLayout.Create(figure, [new Rect(0, 0, 100, 50)]);

        var result = NearestPointFinder.Find(figure, 0, outOfRange + 5, 0.5, layout);

        Assert.Matches(@"^Odd \(\S+E\+0\d–\S+E\+0\d\)$", result!.Detail);
    }
}
