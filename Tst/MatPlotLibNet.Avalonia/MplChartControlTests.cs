// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Avalonia;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Avalonia.Tests;

public class MplChartControlTests
{
    [Fact]
    public void FigureProperty_DefaultsToNull()
    {
        var ctrl = new MplChartControl();
        Assert.Null(ctrl.Figure);
    }

    [Fact]
    public void FigureProperty_CanBeSet()
    {
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();
        var ctrl = new MplChartControl { Figure = figure };
        Assert.Same(figure, ctrl.Figure);
    }

    [Fact]
    public void FigureProperty_Exists()
    {
        Assert.NotNull(MplChartControl.FigureProperty);
    }

    [Fact]
    public void IsInteractiveProperty_DefaultsFalse()
    {
        var ctrl = new MplChartControl();
        Assert.False(ctrl.IsInteractive);
    }

    [Fact]
    public void IsInteractiveProperty_CanBeSetTrue()
    {
        var ctrl = new MplChartControl { IsInteractive = true };
        Assert.True(ctrl.IsInteractive);
    }

    [Fact]
    public void IsInteractiveProperty_Exists()
    {
        Assert.NotNull(MplChartControl.IsInteractiveProperty);
    }

    // ── the clicked data point ─────────────────────────────────────────────────
    //
    // The controller has always known which point was clicked and had nowhere to send it. The control now passes
    // it on. Attach is the seam these tests drive: the real code path builds the controller inside a dispatcher
    // callback, which needs a running Avalonia application, while the wiring it does is exactly what is worth
    // testing.

    private static (Figure Figure, ChartLayout Layout) Pinnable()
    {
        var figure = Plt.Create().Plot([0.0, 1.0, 2.0], [0.0, 5.0, 10.0], s => s.Label = "load").Build();
        figure.ChartId = "chart-1";
        figure.SubPlots[0].XAxis.Min = 0; figure.SubPlots[0].XAxis.Max = 2;
        figure.SubPlots[0].YAxis.Min = 0; figure.SubPlots[0].YAxis.Max = 10;
        return (figure, ChartLayout.Create(figure, [new MatPlotLibNet.Rendering.Rect(0, 0, 200, 100)]));
    }

    [Fact]
    public void AClickOnADataPoint_ReachesTheApplication()
    {
        var (figure, layout) = Pinnable();
        var ctrl = new MplChartControl { Figure = figure, IsInteractive = true };
        var pinned = new List<PinnedAnnotation>();
        ctrl.DataPointClicked += pinned.Add;

        var controller = ctrl.Attach(figure, layout);
        controller.HandlePointerPressed(new PointerInputArgs(100, 50, PointerButton.Left, ModifierKeys.None));

        var point = Assert.Single(pinned);
        Assert.Equal("load", point.SeriesLabel);
        Assert.Equal(1.0, point.DataX);
        Assert.Equal(5.0, point.DataY);
    }

    [Fact]
    public void AClickOnADataPoint_WithNobodyListening_IsHarmless()
    {
        var (figure, layout) = Pinnable();
        var ctrl = new MplChartControl { Figure = figure, IsInteractive = true };

        var controller = ctrl.Attach(figure, layout);
        controller.HandlePointerPressed(new PointerInputArgs(100, 50, PointerButton.Left, ModifierKeys.None));

        Assert.Null(controller.ActiveTooltip);
    }
}
