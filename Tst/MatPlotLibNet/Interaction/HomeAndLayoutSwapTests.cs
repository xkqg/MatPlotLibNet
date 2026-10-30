// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Interaction;

/// <summary>A control repaints after every change and hands the controller the layout of that repaint
/// (<see cref="IInteractionController.UpdateLayout"/>). The controller used to build all its modifiers again for it, so
/// the reset modifier took its "original" range from the layout it was given, which after a zoom was the zoomed range:
/// Home returned to where the reader already was, and a rubber band being dragged lost its state at the next repaint.
/// The modifiers are built once and the figure remembers the view the reader started from, the declared limits, nulls
/// (autoscale) included.</summary>
public class HomeAndLayoutSwapTests
{
    private static readonly Rect Area = new(10, 10, 200, 100);

    private static (Figure Figure, InteractionController Controller) Autoscaled()
    {
        var figure = Plt.Create().Plot([1.0, 2.0, 3.0, 4.0], [10.0, 40.0, 20.0, 30.0]).Build();
        figure.ChartId = "c";
        return (figure, InteractionController.CreateLocal(figure, ChartLayout.Create(figure, [Area])));
    }

    private static void Repaint(InteractionController controller, Figure figure) =>
        controller.UpdateLayout(ChartLayout.Create(figure, [Area]));

    private static void Wheel(InteractionController controller) =>
        controller.HandleScroll(new ScrollInputArgs(110, 60, 0, -120));

    private static void Home(InteractionController controller) =>
        controller.HandleKeyDown(new KeyInputArgs("Home"));

    [Fact]
    public void AWheelZoom_ARepaint_AndHome_BringTheFigureBackToWhatItWas()
    {
        var (figure, controller) = Autoscaled();
        string first = figure.ToJson();

        Wheel(controller);
        Repaint(controller, figure);
        Assert.NotEqual(first, figure.ToJson());   // the zoom pinned the limits

        Home(controller);

        Assert.Equal(first, figure.ToJson());      // limits null again, the axes autoscale
    }

    [Fact]
    public void TwoWheelZoomsWithARepaintBetween_AreOneJourneyHome()
    {
        var (figure, controller) = Autoscaled();
        string first = figure.ToJson();

        Wheel(controller);
        Repaint(controller, figure);
        Wheel(controller);
        Repaint(controller, figure);
        Home(controller);

        Assert.Equal(first, figure.ToJson());
    }

    [Fact]
    public void HomeAfterAPan_RestoresTheStartingLimits()
    {
        var (figure, controller) = Autoscaled();
        figure.SubPlots[0].XAxis.Min = 1; figure.SubPlots[0].XAxis.Max = 4;
        figure.SubPlots[0].YAxis.Min = 0; figure.SubPlots[0].YAxis.Max = 50;
        Repaint(controller, figure);

        controller.HandlePointerPressed(new PointerInputArgs(100, 60, PointerButton.Left, ModifierKeys.None));
        controller.HandlePointerMoved(new PointerInputArgs(150, 80, PointerButton.Left, ModifierKeys.None));
        controller.HandlePointerReleased(new PointerInputArgs(150, 80, PointerButton.Left, ModifierKeys.None));
        Repaint(controller, figure);
        Assert.NotEqual(1.0, figure.SubPlots[0].XAxis.Min);

        Home(controller);

        Assert.Equal(1.0, figure.SubPlots[0].XAxis.Min);
        Assert.Equal(4.0, figure.SubPlots[0].XAxis.Max);
        Assert.Equal(0.0, figure.SubPlots[0].YAxis.Min);
        Assert.Equal(50.0, figure.SubPlots[0].YAxis.Max);
    }

    [Fact]
    public void HomeTwice_DrawsWhatHomeOnceDrew()
    {
        // The second Home has no first view left to go back to; it reads the layout, which draws the same range.
        var (figure, controller) = Autoscaled();
        Wheel(controller);
        Repaint(controller, figure);

        Home(controller);
        Repaint(controller, figure);
        var afterOne = ChartLayout.Create(figure, [Area]).GetDataRange(0);
        Home(controller);

        Assert.Equal(afterOne, ChartLayout.Create(figure, [Area]).GetDataRange(0));
    }

    [Fact]
    public void ALimitTheAppSetsBetweenTwoRepaints_IsTheFirstViewUntilTheReaderMovesIt()
    {
        // Nothing has changed the view yet, so Home has no first view to go back to and reads the layout it is given.
        var (figure, controller) = Autoscaled();
        figure.SubPlots[0].XAxis.Min = 100;
        figure.SubPlots[0].XAxis.Max = 200;
        Repaint(controller, figure);

        Home(controller);

        Assert.Equal(100.0, figure.SubPlots[0].XAxis.Min!.Value, precision: 6);
        Assert.Equal(200.0, figure.SubPlots[0].XAxis.Max!.Value, precision: 6);
    }

    [Fact]
    public void EveryAxesOfAFigure_GoesHomeOnTheHomeKey()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 2, 1, ax => ax.Plot([1.0, 2.0, 3.0], [10.0, 40.0, 20.0]))
            .AddSubPlot(1, 2, 2, ax => ax.Plot([1.0, 2.0, 3.0], [5.0, 6.0, 7.0]))
            .Build();
        figure.ChartId = "c";
        var left = new Rect(10, 10, 100, 100);
        var right = new Rect(130, 10, 100, 100);
        var controller = InteractionController.CreateLocal(figure, ChartLayout.Create(figure, [left, right]));
        string first = figure.ToJson();

        controller.HandleScroll(new ScrollInputArgs(60, 60, 0, -120));
        controller.HandleScroll(new ScrollInputArgs(180, 60, 0, -120));
        controller.UpdateLayout(ChartLayout.Create(figure, [left, right]));
        Home(controller);

        Assert.Equal(first, figure.ToJson());
    }

    [Fact]
    public void ARubberBand_BeingDragged_SurvivesARepaint()
    {
        var (figure, controller) = Autoscaled();

        controller.HandlePointerPressed(new PointerInputArgs(50, 30, PointerButton.Left, ModifierKeys.Shift));
        controller.HandlePointerMoved(new PointerInputArgs(100, 60, PointerButton.Left, ModifierKeys.Shift));
        Assert.NotNull(controller.ActiveBrushSelect);

        Repaint(controller, figure);

        Assert.NotNull(controller.ActiveBrushSelect);   // the repaint must not drop the band the reader is drawing
    }

    [Fact]
    public void ARotation_ARepaint_AndHome_BringTheCameraBack()
    {
        var figure = Plt.Create().Surface(
            [0.0, 1.0], [0.0, 1.0], new double[,] { { 1.0, 2.0 }, { 3.0, 4.0 } }).Build();
        figure.ChartId = "c";
        double azimuth = figure.SubPlots[0].Azimuth, elevation = figure.SubPlots[0].Elevation;
        var controller = InteractionController.CreateLocal(figure, ChartLayout.Create(figure, [Area]));

        controller.HandlePointerPressed(new PointerInputArgs(100, 60, PointerButton.Right, ModifierKeys.None));
        controller.HandlePointerMoved(new PointerInputArgs(140, 70, PointerButton.Right, ModifierKeys.None));
        controller.HandlePointerReleased(new PointerInputArgs(140, 70, PointerButton.Right, ModifierKeys.None));
        Repaint(controller, figure);
        Assert.NotEqual(azimuth, figure.SubPlots[0].Azimuth);

        Home(controller);

        Assert.Equal(azimuth, figure.SubPlots[0].Azimuth, precision: 9);
        Assert.Equal(elevation, figure.SubPlots[0].Elevation, precision: 9);
    }
}
