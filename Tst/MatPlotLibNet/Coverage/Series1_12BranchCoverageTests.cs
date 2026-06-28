// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Coverage;

/// <summary>Branch coverage for the v1.12 series-integration arms not hit by the model/feature tests — closing the
/// baseline regressions the 1.12.0 series introduced in <see cref="Axes"/> + <see cref="ChartSerializer"/>:
/// <list type="bullet">
/// <item>the ChartSerializer <c>Create*</c> null/default arms (a DTO with absent value/colour/segment arrays);</item>
/// <item><see cref="Axes.AddThreshold"/>'s <b>Vertical</b> line/span/annotation arms — the existing
///   <c>ThresholdLineTests</c> are all <c>Orientation.Horizontal</c>, so the vertical path was uncovered;</item>
/// <item>the <c>AxesBuilder.Threshold</c> fluent wrapper (the existing tests call <c>Axes.AddThreshold</c> directly).</item>
/// </list></summary>
public class Series1_12BranchCoverageTests
{
    // ── ChartSerializer.CreateStatTile — GaugeValue == null -> 0 ; Color.HasValue == false -> no accent ──
    [Fact]
    public void CreateStatTile_FromDtoWithoutValueOrColour_DefaultsToZero_NoAccent()
    {
        var s = (StatTileSeries)SeriesRegistry.Create("stattile", new Axes(), new SeriesDto { Type = "stattile" })!;
        Assert.Equal(0.0, s.Value);
        Assert.Null(s.AccentColor);
    }

    // ── ChartSerializer.CreateStateTimeline — all segment arrays null -> [] defaults ; count 0 -> loop not entered ──
    [Fact]
    public void CreateStateTimeline_FromDtoWithoutSegments_YieldsEmptyTimeline()
    {
        var s = SeriesRegistry.Create("statetimeline", new Axes(), new SeriesDto { Type = "statetimeline" });
        Assert.IsType<StateTimelineSeries>(s);
    }

    // ── Axes.AddThreshold — VERTICAL line + span + (labelled) annotation arms (the Horizontal arms are covered elsewhere) ──
    [Fact]
    public void AddThreshold_Vertical_WithColourAndLabel_AddsVerticalLineSpanAnnotation()
    {
        var ax = new Axes();
        ax.AddThreshold(50.0, Orientation.Vertical, ThresholdBreach.Above, Colors.Red, "Limit");
        Assert.Single(ax.ReferenceLines);   // AxVLine path
        Assert.Single(ax.Spans);            // AxVSpan path
        Assert.Single(ax.Annotations);      // label != null -> vertical annotation (annotX=value, annotY=0)
        Assert.Equal("Limit", ax.Annotations[0].Text);
    }

    // ── Axes.AddThreshold — VERTICAL + Below, no colour, no label (the skip arms on the vertical path) ──
    [Fact]
    public void AddThreshold_Vertical_Below_NoColourNoLabel_AddsLineSpan_NoAnnotation()
    {
        var ax = new Axes();
        ax.AddThreshold(50.0, Orientation.Vertical, ThresholdBreach.Below);
        Assert.Single(ax.ReferenceLines);
        Assert.Single(ax.Spans);
        Assert.Empty(ax.Annotations);       // label null -> annotation skipped
    }

    // ── AxesBuilder.Threshold — the fluent wrapper over Axes.AddThreshold ──
    [Fact]
    public void AxesBuilder_Threshold_FluentWrapper_AddsThresholdToTheSubplot()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Threshold(50.0, Orientation.Horizontal, ThresholdBreach.Above, Colors.Red, "L"))
            .Build();
        Assert.Single(fig.SubPlots[0].ReferenceLines);
        Assert.Single(fig.SubPlots[0].Annotations);
    }

    // ── AxesBuilder.StatTile / StateTimeline fluent wrappers (ax.-level; the existing tests go via FigureBuilder) ──
    [Fact]
    public void AxesBuilder_StatTile_And_StateTimeline_FluentWrappers_AddBothSeries()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .StatTile(5.0, t => t.Label = "kpi")
                .StateTimeline([new StateSegment(0.0, 1.0, "Active", Colors.Blue)]))
            .Build();
        Assert.Equal(2, fig.SubPlots[0].Series.Count);   // StatTileSeries + StateTimelineSeries
    }
}
