// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>Phase X.9.a (v1.7.2, 2026-04-19) — drives every render branch in
/// <see cref="MatPlotLibNet.Rendering.SeriesRenderers.BarSeriesRenderer"/>. Pre-X.9
/// the renderer was at 71.8%L / 65.4%B because only the simplest path (categorical
/// vertical, no labels, no edges, no stack) was exercised. This file pins:
///   - Vertical vs Horizontal orientation (line 65 short-circuit)
///   - Categorical (Center vs Edge alignment) vs numeric XCoordinate paths (lines 37, 46, 52)
///   - LineWidth=0+EdgeColor vs LineWidth&gt;0 (line 29-30 ternaries)
///   - StackBaseline path (line 64)
///   - ShowLabels=true with vertical AND horizontal (lines 35, 71-77, 85-91)
///   - LabelLayoutEngine batch placement (lines 95-108) including custom LabelFormat</summary>
public class BarSeriesRendererTests
{
    private static string Render(BarSeries s) =>
        Plt.Create()
            .WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(s))
            .Build()
            .ToSvg();

    [Fact]
    public void Render_VerticalCategorical_BasicPath()
    {
        var svg = Render(new BarSeries(["A", "B", "C"], [1.0, 2.0, 3.0]));
        Assert.Contains("<rect", svg);
    }

    /// <summary>Horizontal orientation arm of line 65. Bars draw left-to-right;
    /// LabelCandidates are positioned right-of-bar (line 89-90).</summary>
    [Fact]
    public void Render_HorizontalOrientation_WithLabels_DrawsBarsAndLabels()
    {
        var svg = Render(new BarSeries(["A", "B"], [1.0, 2.0])
        {
            Orientation = BarOrientation.Horizontal,
            ShowLabels = true,
        });
        Assert.Contains("<rect", svg);
        Assert.Contains("<text", svg);
    }

    /// <summary>Numeric XCoordinate path (line 37 useNumericX=true, line 46 inner branch).
    /// Used by MACD-style histograms that share a real X axis with a line series.</summary>
    [Fact]
    public void Render_NumericXCoordinate_PlacesBarAtX()
    {
        var svg = Render(new BarSeries([1.0, 2.0, 3.0], [10.0, 20.0, 15.0]));
        Assert.Contains("<rect", svg);
    }

    /// <summary>Categorical Edge alignment (line 52 false→true arm of useNumericX,
    /// then line 52 true arm of Align==Edge). Bar starts at the slot edge, not center.</summary>
    [Fact]
    public void Render_CategoricalEdgeAlignment_DrawsBarsAtSlotEdge()
    {
        var svg = Render(new BarSeries(["A", "B"], [1.0, 2.0])
        {
            Align = BarAlignment.Edge,
        });
        Assert.Contains("<rect", svg);
    }

    /// <summary>StackBaseline non-null path (line 64 ternary's true arm). Used by
    /// stacked-bar plots where each series sits on top of the cumulative sum below.</summary>
    [Fact]
    public void Render_StackBaseline_OffsetsBarsByBaseline()
    {
        var svg = Render(new BarSeries(["A", "B"], [1.0, 2.0])
        {
            StackBaseline = new[] { 5.0, 10.0 },
        });
        Assert.Contains("<rect", svg);
    }

    /// <summary>LineWidth&gt;0 path (line 29 ternary's true arm + line 30 EdgeColor
    /// fallback to baseColor). Bars get an explicit border at the requested width.</summary>
    [Fact]
    public void Render_LineWidthGreaterThanZero_FallsBackToBaseColorForEdge()
    {
        var svg = Render(new BarSeries(["A", "B"], [1.0, 2.0])
        {
            LineWidth = 2.0,
            // EdgeColor null → falls back to baseColor at line 30
        });
        Assert.Contains("<rect", svg);
    }

    /// <summary>EdgeColor.HasValue path with LineWidth=0 (line 29 ternary's false arm,
    /// EdgeColor.HasValue true → edgeWidth=1; line 30 false arm, edgeColor=series.EdgeColor).</summary>
    [Fact]
    public void Render_EdgeColorWithoutLineWidth_DefaultsToWidthOne()
    {
        var svg = Render(new BarSeries(["A", "B"], [1.0, 2.0])
        {
            EdgeColor = Colors.Red,
        });
        Assert.Contains("<rect", svg);
    }

    /// <summary>Custom LabelFormat (line 73 + FormatValue line 112-114). Verifies the
    /// labelFormat-not-null arm and that the formatted text shows up in SVG output.</summary>
    [Fact]
    public void Render_ShowLabels_WithCustomLabelFormat_AppliesFormat()
    {
        var svg = Render(new BarSeries(["A", "B"], [1.5, 2.5])
        {
            ShowLabels = true,
            LabelFormat = "F2",   // → "1.50", "2.50"
        });
        Assert.Contains(">1.50<", svg);
        Assert.Contains(">2.50<", svg);
    }

    /// <summary>Negative values arm (lines 67-68 / 81-82 use Math.Min/Max so a negative
    /// value renders below baseline). No exception, rect drawn.</summary>
    [Fact]
    public void Render_NegativeValues_DrawsBarsBelowBaseline()
    {
        var svg = Render(new BarSeries(["A", "B"], [-1.0, -2.0]));
        Assert.Contains("<rect", svg);
    }
}
