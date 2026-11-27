// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using NSubstitute;
using MauiRect = Microsoft.Maui.Graphics.Rect;

namespace MatPlotLibNet.Maui.Tests;

/// <summary>One <see cref="IChartEventHandler"/> reacts to a chart wherever it is shown: a server session takes it, and so
/// must the view a MAUI application puts on its page. The interaction host has had the handler since it was written; these
/// tests hold that a reader of the view can reach it, and the failures it reports, from the view they hold, by driving the
/// view with a finger the way the platform does.</summary>
public class ChartEventHandlerTests
{
    private const int GestureId = 1;

    private static Figure NewFigure() => Plt.Create()
        .AddSubPlot(1, 1, 1, ax => ax.Plot([40.0, 60.0], [40.0, 60.0]).SetXLim(0, 100).SetYLim(0, 100))
        .Build();

    private static MplChartView Show(Figure figure, IChartEventHandler handler)
    {
        var view = new MplChartView { Figure = figure, IsInteractive = true, ChartEventHandler = handler };
        ((IView)view).Arrange(new MauiRect(0, 0, figure.Width, figure.Height));
        view.Drawable!.Draw(Substitute.For<ICanvas>(), new RectF(0, 0, 800, 600));
        return view;
    }

    /// <summary>A finger goes down inside the plot and drags: the view pans.</summary>
    private static void Drag(MplChartView view, Figure figure)
    {
        var layout = ChartRendererDefault.Current.ComputeLayout(figure, MauiRig.Fresh().Context);
        var area = ChartLayout.Create(figure, layout).GetPlotArea(0);
        ((IGraphicsView)view).StartInteraction([new PointF((float)(area.X + area.Width / 2), (float)(area.Y + area.Height / 2))]);

        var pan = (IPanGestureController)view.GestureRecognizers.OfType<PanGestureRecognizer>().Single();
        pan.SendPanStarted(view, GestureId);
        pan.SendPan(view, 30, 0, GestureId);
        pan.SendPan(view, 60, 0, GestureId);
        pan.SendPanCompleted(view, GestureId);
    }

    [Fact]
    public void TheView_TellsItsHandlerOfAGesture_AndReportsItsFailures()
    {
        var figure = NewFigure();
        var failures = new List<Exception>();
        var handler = new RecordingChartEventHandler { Throw = new InvalidOperationException("boom") };
        var view = Show(figure, handler);
        view.ChartEventHandlerFaulted += failures.Add;

        var thrown = Record.Exception(() => Drag(view, figure));

        Assert.Null(thrown);
        Assert.Contains("PanEvent", handler.Names);
        Assert.Same(handler, view.ChartEventHandler);
        Assert.NotEmpty(failures);
        Assert.All(failures, failure => Assert.IsType<InvalidOperationException>(failure));
    }
}
