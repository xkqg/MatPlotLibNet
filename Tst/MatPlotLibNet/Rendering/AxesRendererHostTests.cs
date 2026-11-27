// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A renderer of your own (<see cref="AxesRenderer"/>) is the host of its series passes (<see cref="ILayerHost"/>): its protected
/// <c>RenderSeries()</c> draws the series over the unit square by their own z-order, and no patch, since such a renderer says it draws none;
/// a step that is not a series, which only the library's own renderers list, is passed over by the base.</summary>
public class AxesRendererHostTests
{
    private static readonly Rect Plot = new(0, 0, 200, 100);

    private sealed class OwnRenderer(Axes axes, IRenderContext ctx) : AxesRenderer(axes, Plot, ctx, Theme.MatplotlibV2)
    {
        public override void Render() => RenderSeries();
    }

    [Fact]
    public void ARendererOfYourOwn_DrawsItsSeriesByTheirOwnZOrder_AndNoPatch()
    {
        var axes = new Axes();
        axes.AddSeries(new LineSeries([0.0, 1.0], [0.0, 1.0]) { ZOrder = 3, Label = "front" });
        axes.AddSeries(new LineSeries([0.0, 1.0], [1.0, 0.0]) { ZOrder = 1, Label = "back" });
        axes.AddPatch(new Rectangle(new(0, 0), 1, 1));
        var drawn = new RecordingRenderContext();

        new OwnRenderer(axes, drawn).Render();

        var lines = drawn.Of<DrawLinesCall>();
        Assert.Equal(2, lines.Count);
        Assert.Equal(Theme.MatplotlibV2.SeriesSlot(1).CycleColor, lines[0].Color);
        Assert.Empty(drawn.Of<DrawPathCall>());
    }

    [Fact]
    public void AStepThatIsNotASeries_IsPassedOverByTheBase()
    {
        ILayerHost host = new OwnRenderer(new Axes(), new RecordingRenderContext());
        var pass = SeriesPass.OnUnitSquare(new Axes(), new RenderPass(Plot, new RecordingRenderContext(), Theme.Default), null);
        LayerStep[] steps = [new(0, 0, DataLayer.Grid, 0), new(0, 1, DataLayer.Patch, 0)];

        Assert.Equal(1, host.DrawLayerAt(pass, steps, 1));
        Assert.Empty(host.ColorSlots);
    }
}
