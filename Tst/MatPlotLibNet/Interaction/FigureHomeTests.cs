// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Interaction;

/// <summary>What the figure remembers of the view the reader started from, and what each event does with it, applied
/// straight to a figure the way a server applies the events it receives.</summary>
public class FigureHomeTests
{
    private static Figure TwoRightAxes()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0, 3.0], [10.0, 20.0, 30.0])
                .WithAddRightYAxis(y => y.SetYLim(0, 500).Plot([1.0, 2.0, 3.0], [100.0, 200.0, 300.0]))
                .WithAddLeftYAxis(y => y.Plot([1.0, 2.0, 3.0], [1.0, 2.0, 3.0])))
            .Build();
        figure.ChartId = "c";
        return figure;
    }

    private static ZoomEvent ZoomEverything() => new("c", 0, 1.5, 2.5, 15, 25)
    {
        SecondaryRanges =
        [
            new(AxisSide.Right, 0, new DataRange(1.5, 2.5, 100, 200)),
            new(AxisSide.Left, 0, new DataRange(1.5, 2.5, 1.5, 2.5)),
        ],
    };

    [Fact]
    public void Home_PutsBackEveryYAxisOfEitherSide_NotOnlyThePrimary()
    {
        var figure = TwoRightAxes();
        var axes = figure.SubPlots[0];
        var (rightMin, rightMax) = (axes.YAxes.Right[0].Axis.Min, axes.YAxes.Right[0].Axis.Max);
        var (leftMin, leftMax) = (axes.YAxes.Left[1].Axis.Min, axes.YAxes.Left[1].Axis.Max);

        ZoomEverything().ApplyTo(figure);
        Assert.Equal(100.0, axes.YAxes.Right[0].Axis.Min);
        new ResetEvent("c", 0, 0, 0, 0, 0).ApplyTo(figure);

        Assert.Null(axes.XAxis.Min);
        Assert.Null(axes.YAxis.Min);
        Assert.Equal(rightMin, axes.YAxes.Right[0].Axis.Min);
        Assert.Equal(rightMax, axes.YAxes.Right[0].Axis.Max);
        Assert.Equal(leftMin, axes.YAxes.Left[1].Axis.Min);
        Assert.Equal(leftMax, axes.YAxes.Left[1].Axis.Max);
    }

    [Fact]
    public void TheSecondMove_DoesNotBecomeTheStart()
    {
        var figure = TwoRightAxes();

        new ZoomEvent("c", 0, 1.5, 2.5, 15, 25).ApplyTo(figure);
        new ZoomEvent("c", 0, 1.8, 2.2, 18, 22).ApplyTo(figure);
        new PanEvent("c", 0, 0.1, 1).ApplyTo(figure);
        new ResetEvent("c", 0, 1.8, 2.2, 18, 22).ApplyTo(figure);

        Assert.Null(figure.SubPlots[0].XAxis.Min);
        Assert.Null(figure.SubPlots[0].YAxis.Max);
    }

    [Fact]
    public void AnAxesNothingMoved_TakesTheRangeTheResetCarries()
    {
        var figure = TwoRightAxes();

        new ResetEvent("c", 0, 0, 5, 0, 50).ApplyTo(figure);

        Assert.Equal(0.0, figure.SubPlots[0].XAxis.Min);
        Assert.Equal(50.0, figure.SubPlots[0].YAxis.Max);
    }

    [Fact]
    public void AxesLinkedOnX_GoHomeTogether_EachToItsOwnStart()
    {
        var figure = Plt.Create()
            .AddSubPlot(2, 1, 1, ax => ax.Plot([1.0, 2.0, 3.0], [10.0, 20.0, 30.0]), key: "top")
            .AddSubPlot(2, 1, 2, ax => ax.Plot([1.0, 2.0, 3.0], [1.0, 2.0, 3.0]).ShareX("top"))
            .Build();
        figure.ChartId = "c";
        figure.SubPlots[1].XAxis.Min = 0.5;   // the lower one started with a limit of its own

        new ZoomEvent("c", 0, 1.5, 2.5, 15, 25).ApplyTo(figure);
        Assert.Equal(1.5, figure.SubPlots[1].XAxis.Min);
        new ResetEvent("c", 0, 0, 0, 0, 0).ApplyTo(figure);

        Assert.Null(figure.SubPlots[0].XAxis.Min);
        Assert.Equal(0.5, figure.SubPlots[1].XAxis.Min);
    }

    [Fact]
    public void AnEventForAnAxesTheFigureLacks_RemembersNothingAndChangesNothing()
    {
        var figure = TwoRightAxes();

        Assert.False(new ZoomEvent("c", 7, 1, 2, 3, 4).TryApplyTo(figure));
        Assert.False(new ResetEvent("c", 7, 1, 2, 3, 4).TryApplyTo(figure));
        Assert.False(figure.Home.Remembers(figure.SubPlots[0]));
    }

    [Fact]
    public void AFigureReadBackFromJson_RemembersNothingYet()
    {
        var figure = TwoRightAxes();
        new ZoomEvent("c", 0, 1.5, 2.5, 15, 25).ApplyTo(figure);

        var readBack = ChartSerializerDefault.Current.FromJson(figure.ToJson());

        Assert.False(readBack.Home.Remembers(readBack.SubPlots[0]));
        Assert.True(figure.Home.Remembers(figure.SubPlots[0]));
    }
}
