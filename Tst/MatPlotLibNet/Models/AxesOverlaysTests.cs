// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Models.Tools;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models;

/// <summary>The overlays of an axes (annotations, reference lines, trendlines, price levels, Fibonacci retracements, spans,
/// signals and patches) have one owner: it holds the eight lists, makes the lines, spans and annotations, composes a threshold
/// of them, and is the one place that knows the eight, for the axes' change mark and for the walk a kept draw compares.</summary>
public class AxesOverlaysTests
{
    [Fact]
    public void ANewOwner_HoldsNothingAndHasNoChange()
    {
        var overlays = new AxesOverlays();

        Assert.Empty(overlays.Items());
        Assert.Equal(0, overlays.LatestChange);
    }

    [Fact]
    public void AnAnnotation_IsMadeAndKept()
    {
        var overlays = new AxesOverlays();

        var annotation = overlays.Annotate("peak", 1, 2);

        Assert.Same(annotation, Assert.Single(overlays.Annotations));
        Assert.Equal(("peak", 1.0, 2.0), (annotation.Text, annotation.X, annotation.Y));
    }

    [Theory]
    [InlineData(Orientation.Horizontal)]
    [InlineData(Orientation.Vertical)]
    public void ALineAndASpan_AreMadeInTheOrientationAsked(Orientation orientation)
    {
        var overlays = new AxesOverlays();

        var line = overlays.AddLine(3, orientation);
        var span = overlays.AddSpan(1, 2, orientation);

        Assert.Same(line, Assert.Single(overlays.ReferenceLines));
        Assert.Equal((3.0, orientation), (line.Value, line.Orientation));
        Assert.Same(span, Assert.Single(overlays.Spans));
        Assert.Equal((1.0, 2.0, orientation), (span.Min, span.Max, span.Orientation));
    }

    [Fact]
    public void AHorizontalThresholdAbove_WithColourAndLabel_IsADashedLineAZoneUpAndALabel()
    {
        var overlays = new AxesOverlays();

        overlays.AddThreshold(70, Orientation.Horizontal, ThresholdBreach.Above, Colors.Red, "Alarm");

        var line = Assert.Single(overlays.ReferenceLines);
        Assert.Equal((70.0, Orientation.Horizontal, LineStyle.Dashed, (Color?)Colors.Red), (line.Value, line.Orientation, line.LineStyle, line.Color));
        var span = Assert.Single(overlays.Spans);
        Assert.Equal((70.0, double.MaxValue, Orientation.Horizontal, (Color?)Colors.Red), (span.Min, span.Max, span.Orientation, span.Color));
        var label = Assert.Single(overlays.Annotations);
        Assert.Equal(("Alarm", 0.0, 70.0), (label.Text, label.X, label.Y));
    }

    [Fact]
    public void AVerticalThresholdBelow_WithoutColourOrLabel_IsALineAndAZoneDownOnly()
    {
        var overlays = new AxesOverlays();

        overlays.AddThreshold(5, Orientation.Vertical, ThresholdBreach.Below, color: null, label: null);

        var line = Assert.Single(overlays.ReferenceLines);
        Assert.Equal((5.0, Orientation.Vertical, LineStyle.Dashed), (line.Value, line.Orientation, line.LineStyle));
        Assert.Null(line.Color);
        var span = Assert.Single(overlays.Spans);
        Assert.Equal((double.MinValue, 5.0, Orientation.Vertical), (span.Min, span.Max, span.Orientation));
        Assert.Null(span.Color);
        Assert.Empty(overlays.Annotations);
    }

    [Fact]
    public void AVerticalThresholdLabel_StandsAtTheValueOnX()
    {
        var overlays = new AxesOverlays();

        overlays.AddThreshold(5, Orientation.Vertical, ThresholdBreach.Above, color: null, label: "Limit");

        var label = Assert.Single(overlays.Annotations);
        Assert.Equal((5.0, 0.0), (label.X, label.Y));
    }

    public static TheoryData<string> Lists { get; } = new(["Annotations", "ReferenceLines", "Trendlines", "HorizontalLevels",
        "FibonacciRetracements", "Spans", "Signals", "Patches"]);

    [Theory]
    [MemberData(nameof(Lists))]
    public void AddingToAnyList_MovesTheChangeMark(string list)
    {
        var overlays = new AxesOverlays();
        long before = overlays.LatestChange;

        AddOne(overlays, list);

        Assert.True(overlays.LatestChange > before, $"Adding to {list} reported no change.");
    }

    [Fact]
    public void AChangeInsideAnItem_MovesTheChangeMark()
    {
        var overlays = new AxesOverlays();
        var annotation = overlays.Annotate("a", 1, 1);
        long before = overlays.LatestChange;

        annotation.Color = Colors.Red;

        Assert.True(overlays.LatestChange > before);
    }

    [Fact]
    public void TheItems_ComeListByListInTheOrderAKeptDrawComparesThem()
    {
        var overlays = new AxesOverlays();
        foreach (var list in Lists.Select(row => row.Data).Reverse()) AddOne(overlays, list);

        var kinds = overlays.Items().Select(item => item.GetType()).ToList();

        Assert.Equal([typeof(Annotation), typeof(ReferenceLine), typeof(Trendline), typeof(HorizontalLevel),
            typeof(FibonacciRetracement), typeof(SpanRegion), typeof(SignalMarker), typeof(Circle)], kinds);
    }

    private static void AddOne(AxesOverlays overlays, string list)
    {
        switch (list)
        {
            case "Annotations": overlays.Annotate("a", 1, 1); break;
            case "ReferenceLines": overlays.AddLine(1, Orientation.Horizontal); break;
            case "Trendlines": overlays.Trendlines.Add(new Trendline(0, 0, 1, 1)); break;
            case "HorizontalLevels": overlays.HorizontalLevels.Add(new HorizontalLevel(1)); break;
            case "FibonacciRetracements": overlays.FibonacciRetracements.Add(new FibonacciRetracement(2, 1)); break;
            case "Spans": overlays.AddSpan(1, 2, Orientation.Vertical); break;
            case "Signals": overlays.Signals.Add(new SignalMarker(1, 1, SignalDirection.Buy)); break;
            default: overlays.Patches.Add(new Circle(new(1, 1), 0.5)); break;
        }
    }
}
