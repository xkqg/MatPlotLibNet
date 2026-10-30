// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;

namespace MatPlotLibNet.Tests.Interaction;

/// <summary>The triangle of a ternary diagram is the whole of the chart: a composition outside it does not exist, so a zoom or a
/// pan has nothing to show but empty space and no way back that the data could explain. One rule, where the events of the view
/// are applied: an axes whose frame is fixed is left as it is, however the event came (a wheel, a drag, a server). The
/// modifiers do not each ask the coordinate system; they stay as they are and the rule drops what they send.</summary>
public class FixedFrameTests
{
    private static Figure TernaryFigure()
    {
        var figure = new Figure { Width = 800, Height = 600, ChartId = "t", Title = "Soil" };
        var axes = figure.AddSubPlot(1, 1, 1);
        axes.CoordinateSystem = CoordinateSystem.Ternary;
        return figure;
    }

    private static InteractionHost Attach(Figure figure)
    {
        var host = new InteractionHost();
        var layout = new ChartRenderer().ComputeLayout(figure, new SvgRenderContext());
        host.Update(figure, ChartLayout.Create(figure, layout));
        return host;
    }

    private static PointerInputArgs Left(double x, double y) => new(x, y, PointerButton.Left, ModifierKeys.None);

    [Fact]
    public void AWheelOverATernaryChart_LeavesItsJsonUnchanged()
    {
        var figure = TernaryFigure();
        var host = Attach(figure);
        var plot = new ChartRenderer().ComputeLayout(figure, new SvgRenderContext()).PlotAreas[0];
        string before = figure.ToJson();

        host.HandleScroll(new ScrollInputArgs(plot.X + plot.Width / 2, plot.Y + plot.Height / 2, 0, -1));

        Assert.Equal(before, figure.ToJson());
    }

    [Fact]
    public void ADragOverATernaryChart_LeavesItsJsonUnchanged()
    {
        var figure = TernaryFigure();
        var host = Attach(figure);
        var plot = new ChartRenderer().ComputeLayout(figure, new SvgRenderContext()).PlotAreas[0];
        string before = figure.ToJson();
        double x = plot.X + plot.Width / 2, y = plot.Y + plot.Height / 2;

        host.HandlePointerPressed(Left(x, y));
        host.HandlePointerMoved(Left(x + 30, y + 10));
        host.HandlePointerReleased(Left(x + 30, y + 10));

        Assert.Equal(before, figure.ToJson());
    }

    [Fact]
    public void EveryEventThatMovesTheView_IsDroppedOnAFixedFrame()
    {
        var figure = TernaryFigure();
        string before = figure.ToJson();

        FigureInteractionEvent[] moves =
        [
            new ZoomEvent("t", 0, 0.2, 0.4, 0.1, 0.3),
            new RectangleZoomEvent("t", 0, 0.2, 0.4, 0.1, 0.3),
            new ResetEvent("t", 0, 0, 1, 0, 1),
            new PanEvent("t", 0, 0.1, 0.1) { InView = new DataRange(0, 1, 0, 0.866) },
        ];

        foreach (var move in moves)
        {
            Assert.False(move.TryApplyTo(figure), move.GetType().Name + " was applied to a fixed frame");
        }

        Assert.Equal(before, figure.ToJson());
    }

    [Fact]
    public void AnAxesLinkedToAFixedFrame_IsMovedWithoutIt()
    {
        var figure = new Figure { Width = 800, Height = 600, ChartId = "t" };
        var cartesian = figure.AddSubPlot(1, 2, 1);
        var ternary = figure.AddSubPlot(1, 2, 2, sharex: cartesian);
        ternary.CoordinateSystem = CoordinateSystem.Ternary;

        bool applied = new ZoomEvent("t", 0, 1, 2, 3, 4).TryApplyTo(figure);

        Assert.True(applied);
        Assert.Equal(1.0, cartesian.XAxis.Min);
        Assert.Null(ternary.XAxis.Min);
        Assert.Null(ternary.XAxis.Max);
    }

    [Fact]
    public void ACartesianChart_StillZooms()
    {
        var figure = new Figure { Width = 800, Height = 600, ChartId = "t" };
        var axes = figure.AddSubPlot(1, 1, 1);

        bool applied = new ZoomEvent("t", 0, 1, 2, 3, 4).TryApplyTo(figure);

        Assert.True(applied);
        Assert.Equal(1.0, axes.XAxis.Min);
        Assert.Equal(4.0, axes.YAxis.Max);
    }

    [Fact]
    public void OnlyTheTernaryCoordinateSystem_HasAFixedFrame()
    {
        foreach (var system in Enum.GetValues<CoordinateSystem>())
        {
            Assert.Equal(system == CoordinateSystem.Ternary, new Axes { CoordinateSystem = system }.HasFixedFrame);
        }
    }

    [Fact]
    public void TheNearestMarkOfATernaryChart_IsTheCloserOne_WhicheverIsDrawnFirst()
    {
        double[] top = [0.2, 0.6];
        double[] left = [0.3, 0.2];
        double[] right = [0.5, 0.2];
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.TernaryScatter(top, left, right)).Build();
        var layout = ChartLayout.Create(figure, [new Rect(0, 0, 400, 400 * Math.Sqrt(3) / 2)]);
        var first = new TernaryPoint(0.2, 0.3, 0.5).InFrame();
        var second = new TernaryPoint(0.6, 0.2, 0.2).InFrame();

        // Wide reach, so both marks are candidates: the one drawn second is kept only when it is the closer.
        var atSecond = NearestPointFinder.Find(figure, 0, second.X, second.Y, layout, 1000);
        var atFirst = NearestPointFinder.Find(figure, 0, first.X, first.Y, layout, 1000);

        Assert.Equal(1, atSecond!.PointIndex);
        Assert.Equal(0, atFirst!.PointIndex);
    }
}
