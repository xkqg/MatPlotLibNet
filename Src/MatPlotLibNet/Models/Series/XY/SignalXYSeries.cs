// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>
/// A line series optimised for large datasets with monotonically ascending (but non-uniform) X values.
/// Uses two <c>Array.BinarySearch</c> calls to find the visible index range in O(log n),
/// avoiding the O(n) scan of <see cref="T:MatPlotLibNet.Rendering.Downsampling.ViewportCuller"/>.
/// </summary>
public sealed class SignalXYSeries : XYSeries, XY.IMonotonicXY, IHasColor
{
    public Color? Color { get; set; }

    public LineStyle LineStyle { get; set; } = LineStyle.Solid;

    public double LineWidth { get; set; } = 1.5;

    /// <summary>Initializes a new instance with ascending X values and corresponding Y values.</summary>
    /// <param name="xData">Monotonically ascending X values. Must match the length of <paramref name="yData"/>.</param>
    /// <param name="yData">Y values parallel to <paramref name="xData"/>.</param>
    public SignalXYSeries(double[] xData, double[] yData) : base(xData, yData) { }

    // ── IMonotonicXY ─────────────────────────────────────────────────────────

    /// <inheritdoc />
    public int Length => XData.Length;

    /// <inheritdoc />
    public double XAt(int i) => XData[i];

    /// <inheritdoc />
    public double YAt(int i) => YData[i];

    /// <inheritdoc />
    /// <remarks>Uses two <see cref="Array.BinarySearch(Array,object)"/> calls — O(log n).</remarks>
    public XY.IndexRange IndexRangeFor(double xMin, double xMax)
    {
        int n = XData.Length;
        if (n == 0) return new(0, 0);

        // lo = first index where XData[lo] >= xMin
        int lo = Array.BinarySearch(XData, xMin);
        if (lo < 0) lo = ~lo;   // ~lo = insertion point = first index > xMin

        // hi = last index where XData[hi] <= xMax
        int hi = Array.BinarySearch(XData, xMax);
        if (hi < 0) hi = ~hi - 1;  // ~hi - 1 = last index < xMax

        // Check for no overlap before adding guard points
        if (lo > n - 1 || hi < 0 || lo > hi) return new(0, 0);

        // Extend one guard point each side for rendering continuity at edges
        lo = Math.Max(0, lo - 1);
        hi = Math.Min(n - 1, hi + 1);

        return new(lo, hi + 1);
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "signal-xy",
        XData = XData, YData = YData,
        Label = Label,
        LineStyle = LineStyle.ToString().ToLowerInvariant(),
        LineWidth = LineWidth,
        Color = Color
    };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
