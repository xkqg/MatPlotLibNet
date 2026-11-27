// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Interaction;

/// <summary>Wiring a native control to a server was written three times (WPF, Avalonia, Uno) for the chart control alone, and the
/// streaming controls and the MAUI view could not do it at all. A control that is <c>IServerInteractive</c> (it says whether it is
/// interactive and holds its host) is wired by the one route.</summary>
public class ServerRouteExtensionsTests
{
    private sealed class StandInControl : IServerInteractive
    {
        public bool IsInteractive { get; set; }

        public InteractionHost Host { get; } = new();
    }

    [Fact]
    public void ARoutedControl_IsInteractive_AndSendsItsGesturesThroughTheDelegate()
    {
        var control = new StandInControl();
        var sent = new List<string>();

        var same = control.RouteToServer((method, _) =>
        {
            sent.Add(method);
            return Task.CompletedTask;
        });

        Assert.Same(control, same);
        Assert.True(control.IsInteractive);
        Assert.NotNull(control.Host.ServerEventSink);
        control.Host.ServerEventSink!(new ZoomEvent("c", 0, 0, 1, 0, 1));
        Assert.Equal(["OnZoom"], sent);
    }

    [Fact]
    public void ARoutedControl_SendsTheGestureOfItsFigure_AndLeavesTheFigureForTheServer()
    {
        var control = new StandInControl();
        var sent = new List<string>();
        control.RouteToServer((method, _) =>
        {
            sent.Add(method);
            return Task.CompletedTask;
        });
        var figure = Plt.Create().Plot([1.0, 2.0, 3.0, 4.0], [10.0, 40.0, 20.0, 30.0]).Build();
        figure.ChartId = "c";
        var plot = new MatPlotLibNet.Rendering.Rect(10, 10, 200, 100);
        control.Host.Update(figure, ChartLayout.Create(figure, [plot]));

        control.Host.HandleScroll(new ScrollInputArgs(plot.X + plot.Width / 2, plot.Y + plot.Height / 2, 0, -120));

        Assert.Equal(["OnZoom"], sent);
        Assert.Null(figure.SubPlots[0].XAxis.Min);
    }
}
