// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;

namespace MatPlotLibNet.Maui.Tests;

/// <summary>The MAUI view could not send its gestures to a server (the feature stopped at the WPF, Avalonia and Uno chart controls). It
/// is wired the way every control is, by the library's one route, for the chart view and for the streaming view.</summary>
public class ServerInteractionTests
{
    [Fact]
    public void WithServerInteraction_MakesEitherViewInteractive_SendsItsGesturesThroughTheDelegate_AndHandsItBack()
    {
        var probe = new ServerInteractionProbe();
        var chart = new MplChartView();
        var streaming = new MplStreamingChartView();
        Assert.Same(chart, chart.WithServerInteraction(probe.Send));
        Assert.Same(streaming, streaming.WithServerInteraction(probe.Send));
        probe.AssertEveryGestureWasSent(new(chart.IsInteractive, chart.Host), new(streaming.IsInteractive, streaming.Host));
    }

    [Fact]
    public void AViewThatIsNotInteractive_IsNotListening_EvenWithAPaintBehindIt()
    {
        var view = new MplChartView();
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();
        view.Host.Update(figure, ChartLayout.Create(figure, [new MatPlotLibNet.Rendering.Rect(0, 0, 100, 100)]));

        Assert.False(view.Host.Listening);
        view.IsInteractive = true;
        Assert.True(view.Host.Listening);
    }
}
