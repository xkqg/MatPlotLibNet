// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.LegendRendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The colour-slot table of an axes (<see cref="SeriesSlotExtensions.ColorSlots"/>) is made once by the renderer that draws
/// the axes and handed to everything that numbers series by colour: the series passes, the part of every Y axis slot, the top X axis,
/// the end labels and the legend rows. Every build reads each series' <see cref="ChartSeries.ColorSlotSpan"/> once, so a probe series
/// that counts those reads counts the builds.</summary>
public class ColorSlotTableOnceTests
{
    private static readonly Rect Plot = new(60, 40, 300, 200);

    /// <summary>A series that draws nothing and counts how often the colour-slot table asks how many slots it holds.</summary>
    private sealed class Probe : ChartSeries
    {
        public int Builds { get; private set; }

        internal override int ColorSlotSpan { get { Builds++; return base.ColorSlotSpan; } }

        public override void Accept(ISeriesVisitor visitor, RenderArea area) { }

        public override DataRangeContribution ComputeDataRange(IAxesContext context) => default;

        public override MatPlotLibNet.Serialization.SeriesDto ToSeriesDto() => new() { Type = "probe" };
    }

    /// <summary>Axes with a series on every seat that numbers by colour (primary, a right slot, the top axis), an end label and a
    /// visible legend, so every site that once built the table has a reason to.</summary>
    private static (Axes Axes, Probe Probe) EveryConsumer()
    {
        var axes = new Axes();
        axes.AddSeries(new LineSeries([0.0, 1, 2], [1.0, 2, 3]) { Label = "ends here", LabelPlacement = SeriesLabelPlacement.LineEnd });
        var probe = axes.AddSeries(new Probe { Label = "probe" });
        axes.YAxes.Right.FirstOrAdd().AddSeries(new LineSeries([0.0, 1, 2], [10.0, 20, 30]) { Label = "right" });
        axes.AddXSecondarySeries(new LineSeries([0.0, 1, 2], [3.0, 2, 1]) { Label = "top" });
        axes.Legend = axes.Legend with { Visible = true };
        return (axes, probe);
    }

    [Theory]
    [InlineData("default")]
    [InlineData("v2")]
    public void ACartesianRender_BuildsTheTableOnce(string theme)
    {
        var (axes, probe) = EveryConsumer();

        AxesRenderer.Create(axes, Plot, new SvgRenderContext(), theme.AsTheme()).Render();

        Assert.Equal(1, probe.Builds);
    }

    [Fact]
    public void ARenderersTableIsTheOneItHandsOut()
    {
        var (axes, _) = EveryConsumer();
        var renderer = AxesRenderer.Create(axes, Plot, new SvgRenderContext(), Theme.Default);

        Assert.Same(renderer.ColorSlots, renderer.ColorSlots);
        Assert.Equal(axes.ColorSlots(Theme.Default), renderer.ColorSlots);
    }

    [Fact]
    public void TheLegendRows_AreNumberedByTheTableTheyAreHanded()
    {
        var axes = new Axes();
        axes.AddSeries(new LineSeries([0.0, 1], [1.0, 2]) { Label = "a" });
        axes.AddSeries(new LineSeries([0.0, 1], [2.0, 3]) { Label = "b" });
        int[] handed = [5, 6];

        var rows = SeriesLegendEntry.RowsOf(axes, Theme.Default, directLabelsDrawn: false, legendValues: false, handed);

        Assert.Equal([5, 6], rows.Select(row => row.ColorSlotIndex));
        Assert.Equal([0, 1], rows.Select(row => row.OriginalIndex));
    }

    [Fact]
    public void AHiddenLegend_BuildsNoRows_AndNoTable()
    {
        var (axes, probe) = EveryConsumer();
        axes.Legend = axes.Legend with { Visible = false };
        var renderer = AxesRenderer.Create(axes, Plot, new SvgRenderContext(), Theme.Default);

        Assert.Null(renderer.PlaceLegend(() => MarkObstacles.None));

        Assert.Equal(0, probe.Builds);
    }

    [Fact]
    public void ASeriesThatIsDrawnAfterTheRendererIsMade_IsNumberedByTheTableOfItsOwnRenderer()
    {
        var (axes, probe) = EveryConsumer();
        AxesRenderer.Create(axes, Plot, new SvgRenderContext(), Theme.Default).Render();
        axes.AddSeries(new LineSeries([0.0, 1], [1.0, 2]));

        AxesRenderer.Create(axes, Plot, new SvgRenderContext(), Theme.Default).Render();

        Assert.Equal(2, probe.Builds);
    }
}
