// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;

namespace MatPlotLibNet.Tests.Interaction;

/// <summary>The WPF, Avalonia, Uno and MAUI controls each asked the same question before every gesture (has a paint given the host a
/// figure, and is the control interactive) and each handed the layout of a paint to the host with the same three lines. The host now
/// answers the one and takes the other: a control gives it the facts that are the control's (whether it is interactive, how it is
/// painted again and how to reach its thread) when it is made.</summary>
public class InteractionHostListeningTests
{
    private static Figure NewFigure()
    {
        var figure = Plt.Create().Plot([1.0, 2.0, 3.0, 4.0], [10.0, 40.0, 20.0, 30.0]).Build();
        figure.ChartId = "c";
        return figure;
    }

    private static LayoutResult PaintedLayout(Figure figure) => new ChartRenderer().ComputeLayout(figure, new SvgRenderContext());

    /// <summary>The repaint of a control these tests do not paint.</summary>
    private static void Nothing() { }

    [Fact]
    public void BeforeAPaint_AnInteractiveControl_IsNotListening()
    {
        var host = new InteractionHost(() => true, Nothing);

        Assert.False(host.Listening);
    }

    [Fact]
    public void AfterAPaintOfAnInteractiveControl_TheHostIsListening()
    {
        var host = new InteractionHost(() => true, Nothing);
        var figure = NewFigure();

        host.Painted(figure, PaintedLayout(figure));

        Assert.True(host.IsAttached);
        Assert.True(host.Listening);
    }

    [Fact]
    public void AControlThatStopsBeingInteractive_StopsListening_WhateverTheHostHolds()
    {
        bool interactive = true;
        var host = new InteractionHost(() => interactive, Nothing);
        var figure = NewFigure();
        host.Painted(figure, PaintedLayout(figure));

        interactive = false;

        Assert.True(host.IsAttached);
        Assert.False(host.Listening);
    }

    [Fact]
    public void APaintOfAControlThatIsNotInteractive_GivesTheHostNothing()
    {
        var host = new InteractionHost(() => false, Nothing);
        var figure = NewFigure();

        host.Painted(figure, PaintedLayout(figure));

        Assert.False(host.IsAttached);
        Assert.False(host.Listening);
    }

    [Fact]
    public void APaint_AimsTheHostAtTheFigurePainted_SoTheNextGestureActsOnIt()
    {
        var host = new InteractionHost(() => true, Nothing);
        var figure = NewFigure();
        var layout = PaintedLayout(figure);
        host.Painted(figure, layout);
        var plot = layout.PlotAreas[0];

        host.HandleScroll(new ScrollInputArgs(plot.X + plot.Width / 2, plot.Y + plot.Height / 2, 0, -120));

        Assert.NotNull(figure.SubPlots[0].XAxis.Min);
    }

    [Fact]
    public void APaintOnAnotherThread_WaitsForTheControlsThread_BeforeTheHostSeesTheFigure()
    {
        var thread = new PostedWork();
        var host = new InteractionHost(() => true, Nothing, thread.Post);
        var figure = NewFigure();

        host.Painted(figure, PaintedLayout(figure));
        Assert.False(host.IsAttached);   // posted, not yet run: a gesture on the UI thread sees no half-handed layout
        thread.RunAll();

        Assert.True(host.Listening);
    }

    [Fact]
    public void APaintOfAControlThatIsNotInteractive_PostsNothing()
    {
        var thread = new PostedWork();
        var host = new InteractionHost(() => false, Nothing, thread.Post);
        var figure = NewFigure();

        host.Painted(figure, PaintedLayout(figure));

        Assert.Empty(thread.Waiting);
    }

    /// <summary>A control hands the host its repaint when it makes it, as it hands it its interactivity: a gesture that changed the
    /// figure asks for it, and the control subscribes to nothing.</summary>
    [Fact]
    public void AGestureThatChangesTheFigure_AsksForTheRepaintTheControlGaveTheHost()
    {
        int repaints = 0;
        var host = new InteractionHost(() => true, () => repaints++);
        var figure = NewFigure();
        var layout = PaintedLayout(figure);
        host.Painted(figure, layout);
        var plot = layout.PlotAreas[0];

        host.HandleScroll(new ScrollInputArgs(plot.X + plot.Width / 2, plot.Y + plot.Height / 2, 0, -120));

        Assert.Equal(1, repaints);
    }

    [Fact]
    public void AHostMadeWithNoControl_ListensAsSoonAsItHasAFigure()
    {
        var host = new InteractionHost();
        var figure = NewFigure();

        Assert.False(host.Listening);
        host.Update(figure, ChartLayout.Create(figure, PaintedLayout(figure)));

        Assert.True(host.Listening);
    }
}
