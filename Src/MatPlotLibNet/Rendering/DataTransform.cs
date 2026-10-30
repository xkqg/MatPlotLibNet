// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Rendering;

/// <summary>Transforms data coordinates to pixel coordinates and vice versa within a plot area.
/// Optionally aware of axis breaks (data values inside break regions return NaN) and axis scales
/// (Log / SymLog map data through the scale's Forward function before pixel scaling).</summary>
public sealed class DataTransform
{
    private readonly double _xScale, _xOffset, _yScale, _yOffset;
    private readonly double _dataXMin, _dataXMax, _dataYMin, _dataYMax;
    private readonly double _fullXMin, _fullXMax, _fullYMin, _fullYMax;
    private readonly Rect _plotBounds;
    private readonly IReadOnlyList<AxisBreak>? _xBreaks;
    private readonly IReadOnlyList<AxisBreak>? _yBreaks;
    private readonly AxisScale _xAxisScale;
    private readonly AxisScale _yAxisScale;
    private readonly double _xLinThresh;
    private readonly double _yLinThresh;

    /// <summary>Gets the minimum X value in data space (compressed if breaks active).</summary>
    public double DataXMin => _dataXMin;

    /// <summary>Gets the maximum X value in data space (compressed if breaks active).</summary>
    public double DataXMax => _dataXMax;

    /// <summary>Gets the minimum Y value in data space (compressed if breaks active).</summary>
    public double DataYMin => _dataYMin;

    /// <summary>Gets the maximum Y value in data space (compressed if breaks active).</summary>
    public double DataYMax => _dataYMax;

    /// <summary>The lowest X value the axis shows, in the space the DATA lives in.</summary>
    /// <remarks>
    /// On a log or symlog axis <see cref="DataXMin"/> is the SCALED bound — 0 for data that starts at 1, because
    /// the pixel mapping works in log space. Anything that compares the axis range against raw sample values, such
    /// as viewport culling, must use this instead, or it cuts away every point above 10^DataXMax while the axis
    /// goes on drawing ticks for them. On a linear axis the two are the same number. On a reversed axis (an X limit set
    /// high to low) it is still the lowest value: the low end of the drawn range's <see cref="Range1D.Interval"/>.
    /// </remarks>
    internal double UnscaledXMin => new Range1D(_fullXMin, _fullXMax).Interval.Lo;

    /// <summary>The highest X value the axis shows, in the space the DATA lives in.</summary>
    /// <inheritdoc cref="UnscaledXMin"/>
    internal double UnscaledXMax => new Range1D(_fullXMin, _fullXMax).Interval.Hi;

    /// <summary>The pixel-space rectangle the data range is mapped onto.</summary>
    internal Rect PlotBounds => _plotBounds;

    /// <summary>The range the axes show, in data units, in drawn orientation: X from the left edge to the right edge, Y
    /// from the bottom edge to the top edge. On a log or symlog axis these are the data values at the edges, not their
    /// scaled bounds (<see cref="DataXMin"/>); a reversed limit stays reversed.</summary>
    internal DataRange DrawnRange => new(_fullXMin, _fullXMax, _fullYMin, _fullYMax);

    /// <summary>Initializes a new linear transform mapping the specified data range onto the given pixel bounds.</summary>
    /// <param name="dataXMin">Minimum X value in data space.</param>
    /// <param name="dataXMax">Maximum X value in data space.</param>
    /// <param name="dataYMin">Minimum Y value in data space.</param>
    /// <param name="dataYMax">Maximum Y value in data space.</param>
    /// <param name="plotBounds">The pixel-space rectangle to map data onto.</param>
    public DataTransform(double dataXMin, double dataXMax, double dataYMin, double dataYMax, Rect plotBounds)
        : this(dataXMin, dataXMax, dataYMin, dataYMax, plotBounds, null, null,
               dataXMin, dataXMax, dataYMin, dataYMax,
               AxisScale.Linear, AxisScale.Linear, 1.0, 1.0) { }

    /// <summary>Initializes a break-aware linear transform.</summary>
    public DataTransform(
        double dataXMin, double dataXMax, double dataYMin, double dataYMax, Rect plotBounds,
        IReadOnlyList<AxisBreak>? xBreaks, IReadOnlyList<AxisBreak>? yBreaks,
        double fullXMin, double fullXMax, double fullYMin, double fullYMax)
        : this(dataXMin, dataXMax, dataYMin, dataYMax, plotBounds, xBreaks, yBreaks,
               fullXMin, fullXMax, fullYMin, fullYMax,
               AxisScale.Linear, AxisScale.Linear, 1.0, 1.0) { }

    /// <summary>Initializes a break-aware AND scale-aware transform. For Log/SymLog scales the
    /// passed <paramref name="dataYMin"/>..<paramref name="dataYMax"/> must already be in scaled
    /// space (i.e. <c>SymlogTransform.Forward(rawMin, linthresh)</c>); raw values fed to
    /// <see cref="DataToPixel"/> are mapped through the scale's Forward function before scaling.</summary>
    public DataTransform(
        double dataXMin, double dataXMax, double dataYMin, double dataYMax, Rect plotBounds,
        IReadOnlyList<AxisBreak>? xBreaks, IReadOnlyList<AxisBreak>? yBreaks,
        double fullXMin, double fullXMax, double fullYMin, double fullYMax,
        AxisScale xAxisScale, AxisScale yAxisScale, double xLinThresh, double yLinThresh)
    {
        _dataXMin = dataXMin;
        _dataXMax = dataXMax;
        _dataYMin = dataYMin;
        _dataYMax = dataYMax;
        _fullXMin = fullXMin;
        _fullXMax = fullXMax;
        _fullYMin = fullYMin;
        _fullYMax = fullYMax;
        _plotBounds = plotBounds;
        _xBreaks = xBreaks is { Count: > 0 } ? xBreaks : null;
        _yBreaks = yBreaks is { Count: > 0 } ? yBreaks : null;
        _xAxisScale = xAxisScale;
        _yAxisScale = yAxisScale;
        _xLinThresh = xLinThresh;
        _yLinThresh = yLinThresh;

        double xRange = dataXMax - dataXMin;
        double yRange = dataYMax - dataYMin;

        _xScale = xRange == 0 ? 0 : plotBounds.Width / xRange;
        _xOffset = plotBounds.X - dataXMin * _xScale;

        _yScale = yRange == 0 ? 0 : plotBounds.Height / yRange;
        _yOffset = plotBounds.Y + plotBounds.Height + dataYMin * _yScale;
    }

    /// <summary>Converts data-space coordinates to pixel-space coordinates.</summary>
    /// <param name="x">The X value in data space.</param>
    /// <param name="y">The Y value in data space.</param>
    /// <returns>The corresponding pixel position. X or Y may be NaN if the input falls inside a break region.</returns>
    public Point DataToPixel(double x, double y)
    {
        // 1. Apply axis scale forward transform (Log / SymLog)
        // 2. Remap through axis breaks if active — raw values in break regions return NaN
        double rx = PlacedX(x);
        double ry = PlacedY(y);

        double px = _xScale == 0
            ? OnFlatAxis(rx, _plotBounds.X + _plotBounds.Width / 2)
            : rx * _xScale + _xOffset;

        // Y is inverted: data-max maps to top, data-min maps to bottom
        double py = _yScale == 0
            ? OnFlatAxis(ry, _plotBounds.Y + _plotBounds.Height / 2)
            : _yOffset - ry * _yScale;

        return new Point(px, py);
    }

    /// <summary>Converts a point whose Y is the BASE a mark stands on — a bar's foot, the lower edge of an area or of a
    /// stacked layer, the total a waterfall step starts from, the lower end of a box or a whisker — to pixels: as
    /// <see cref="DataToPixel"/>, except that a finite Y the Y axis' scale cannot place (0 or a negative value on a log
    /// axis, <see cref="AxisScaleExtensions.HasPlaceFor"/>) is clipped to the edge where the Y axis begins: the plot's
    /// lower edge, its upper edge on a reversed axis. matplotlib's log scale clips the same way
    /// (<c>nonpositive='clip'</c>), so a bar on a log axis runs to the bottom of the plot instead of vanishing.</summary>
    /// <remarks>Only the base is clipped. The value a mark reaches is placed by <see cref="DataToPixel"/>, so a mark whose
    /// value the axis cannot place is still skipped, as a line or a marker there is masked. A missing value, an infinity
    /// and a value inside an axis break are not clipped either: they stay NaN.</remarks>
    /// <param name="x">The X value in data space.</param>
    /// <param name="y">The base, in data space.</param>
    /// <returns>The pixel position, with the base clipped.</returns>
    internal Point DataToPixelClippedY(double x, double y)
    {
        var pixel = DataToPixel(x, y);
        return ClipsBase(y, _yAxisScale) ? pixel with { Y = YAxisStart } : pixel;
    }

    /// <summary>Converts a point whose X is the BASE a mark stands on (a horizontal bar's foot, the left end of a
    /// horizontal box) to pixels: a finite X the X axis' scale cannot place is clipped to the edge where the X axis begins,
    /// the plot's left edge, its right edge on a reversed axis. The rule of <see cref="DataToPixelClippedY"/>.</summary>
    /// <param name="x">The base, in data space.</param>
    /// <param name="y">The Y value in data space.</param>
    /// <returns>The pixel position, with the base clipped.</returns>
    internal Point DataToPixelClippedX(double x, double y)
    {
        var pixel = DataToPixel(x, y);
        return ClipsBase(x, _xAxisScale) ? pixel with { X = XAxisStart } : pixel;
    }

    /// <summary>Batch-transforms the bases of a mark (an area's lower edge, a stacked layer's floor) to pixel Y values: as
    /// <see cref="TransformY"/>, with every base the Y axis' scale cannot place clipped to the edge where the axis begins,
    /// by the rule of <see cref="DataToPixelClippedY"/>. On an axis that places every finite value the result is
    /// <see cref="TransformY"/>'s, bit for bit.</summary>
    /// <param name="yData">The bases, in data space.</param>
    /// <returns>Pixel-space Y values, one per input element.</returns>
    internal double[] TransformYClipped(ReadOnlySpan<double> yData)
    {
        var dst = TransformY(yData);
        for (int i = 0; i < dst.Length; i++)
        {
            if (ClipsBase(yData[i], _yAxisScale)) dst[i] = YAxisStart;
        }

        return dst;
    }

    /// <summary>The X counterpart of <see cref="TransformYClipped"/>: the bases of a mark that lies down (the left edge of a fill
    /// between curves of X) to pixel X values, a base the X axis' scale cannot place clipped to the edge where the X axis
    /// begins.</summary>
    /// <param name="xData">The bases, in data space.</param>
    /// <returns>Pixel-space X values, one per input element.</returns>
    internal double[] TransformXClipped(ReadOnlySpan<double> xData)
    {
        var dst = TransformX(xData);
        for (int i = 0; i < dst.Length; i++)
        {
            if (ClipsBase(xData[i], _xAxisScale)) dst[i] = XAxisStart;
        }

        return dst;
    }

    /// <summary>Whether a base is clipped: a finite value that <paramref name="scale"/> has no place for. A missing value
    /// and an infinity are not: no axis places them.</summary>
    private static bool ClipsBase(double value, AxisScale scale) => value.IsDrawable() && !scale.HasPlaceFor(value);

    /// <summary>The pixel Y of the edge where the Y axis begins, at its lowest value: the bottom of the plot, the top on a
    /// reversed axis.</summary>
    private double YAxisStart => _yScale < 0 ? _plotBounds.Y : _plotBounds.Y + _plotBounds.Height;

    /// <summary>The pixel X of the edge where the X axis begins, at its lowest value: the left of the plot, the right on a
    /// reversed axis.</summary>
    private double XAxisStart => _xScale < 0 ? _plotBounds.X + _plotBounds.Width : _plotBounds.X;

    private static double ApplyScale(double v, AxisScale scale, double linthresh) => scale.Forward(v, linthresh);

    /// <summary>An X value in the space the axis is drawn in: through its scale, then past its breaks (NaN inside one).</summary>
    private double PlacedX(double x)
    {
        double sx = ApplyScale(x, _xAxisScale, _xLinThresh);
        return _xBreaks is null ? sx : AxisBreakMapper.Remap(_xBreaks, sx, _fullXMin, _fullXMax);
    }

    /// <summary>A Y value in the space the axis is drawn in: through its scale, then past its breaks (NaN inside one).</summary>
    private double PlacedY(double y)
    {
        double sy = ApplyScale(y, _yAxisScale, _yLinThresh);
        return _yBreaks is null ? sy : AxisBreakMapper.Remap(_yBreaks, sy, _fullYMin, _fullYMax);
    }

    /// <summary>Where a value lands on an axis whose range has no width: the axis' centre, when the axis can place it at
    /// all. A value it cannot place (NaN, an infinity, a non-positive value on a log axis, a value inside a break) stays
    /// NaN, as on every other axis, so a renderer asking the pixel whether it can be drawn hears no
    /// (<see cref="MissingValueExtensions"/>).</summary>
    private static double OnFlatAxis(double placed, double centre) => placed.IsDrawable() ? centre : double.NaN;

    /// <summary>The scale of the X axis this transform maps through; <see cref="DataXMin"/>/<see cref="DataXMax"/> are
    /// in its drawn space.</summary>
    internal AxisScale XScale => _xAxisScale;

    /// <summary>The scale of the Y axis this transform maps through; <see cref="DataYMin"/>/<see cref="DataYMax"/> are
    /// in its drawn space.</summary>
    internal AxisScale YScale => _yAxisScale;

    /// <summary>The symlog linear threshold of the X axis (meaningful when <see cref="XScale"/> is symlog).</summary>
    internal double XLinThresh => _xLinThresh;

    /// <summary>The symlog linear threshold of the Y axis (meaningful when <see cref="YScale"/> is symlog).</summary>
    internal double YLinThresh => _yLinThresh;

    // -------------------------------------------------------------------------
    // Batch / vectorized transforms (SIMD via VectorMath)
    // -------------------------------------------------------------------------

    /// <summary>Batch-transforms X data coordinates to pixel X values using SIMD MultiplyAdd.</summary>
    /// <param name="xData">Data-space X values.</param>
    /// <returns>Pixel-space X values, one per input element.</returns>
    public double[] TransformX(ReadOnlySpan<double> xData)
    {
        var dst = new double[xData.Length];
        if (_xScale == 0)
            for (int i = 0; i < xData.Length; i++)
                dst[i] = OnFlatAxis(PlacedX(xData[i]), _plotBounds.X + _plotBounds.Width / 2);
        else if (_xBreaks is null && _xAxisScale == AxisScale.Linear)
            Numerics.VectorMath.MultiplyAdd(xData, _xScale, _xOffset, dst);
        else
            for (int i = 0; i < xData.Length; i++)
                dst[i] = PlacedX(xData[i]) * _xScale + _xOffset;
        return dst;
    }

    /// <summary>Batch-transforms Y data coordinates to pixel Y values using SIMD MultiplyAdd.
    /// Note: Y axis is inverted — larger data values map to smaller pixel Y.</summary>
    /// <remarks>The orientation is the range's: a Y range handed over high to low (a limit set high to low, or an axis
    /// drawn <see cref="Models.Axis.Inverted"/>, which the renderer turns into such a range before it builds this transform)
    /// maps through a scale of the other sign, so larger values land lower. Downstream renderers receive pixel coordinates
    /// in the drawn orientation and must not apply a second inversion.</remarks>
    /// <param name="yData">Data-space Y values.</param>
    /// <returns>Pixel-space Y values, one per input element.</returns>
    public double[] TransformY(ReadOnlySpan<double> yData)
    {
        var dst = new double[yData.Length];
        if (_yScale == 0)
            for (int i = 0; i < yData.Length; i++)
                dst[i] = OnFlatAxis(PlacedY(yData[i]), _plotBounds.Y + _plotBounds.Height / 2);
        else if (_yBreaks is null && _yAxisScale == AxisScale.Linear)
            Numerics.VectorMath.MultiplyAdd(yData, -_yScale, _yOffset, dst);
        else
            for (int i = 0; i < yData.Length; i++)
                dst[i] = _yOffset - PlacedY(yData[i]) * _yScale;
        return dst;
    }

    /// <summary>Batch-transforms paired X/Y data coordinates to pixel-space <see cref="Point"/> values.</summary>
    /// <param name="xData">Data-space X values.</param>
    /// <param name="yData">Data-space Y values; must be the same length as <paramref name="xData"/>.</param>
    /// <returns>Array of pixel-space points.</returns>
    /// <remarks>Single-pass, zero intermediate allocations. Uses AVX SIMD interleave on x86-64
    /// (FMA multiply-add → UnpackLow/High → Permute2x128 → direct store into <c>Point[]</c> memory).
    /// Falls back to a branchless scalar loop on other architectures.</remarks>
    public Point[] TransformBatch(ReadOnlySpan<double> xData, ReadOnlySpan<double> yData)
    {
        int n = xData.Length;
        var result = new Point[n];

        // Fast SIMD path only when no breaks and linear scales — otherwise fall back to scalar.
        bool needScalar = _xBreaks is not null || _yBreaks is not null
            || _xAxisScale != AxisScale.Linear || _yAxisScale != AxisScale.Linear;

        if (needScalar)
        {
            for (int i = 0; i < n; i++)
                result[i] = DataToPixel(xData[i], yData[i]);
            return result;
        }

        double xS = _xScale, xO = _xOffset;
        double yS = -_yScale, yO = _yOffset;
        if (_xScale == 0) { xS = 0; xO = _plotBounds.X + _plotBounds.Width / 2; }
        if (_yScale == 0) { yS = 0; yO = _plotBounds.Y + _plotBounds.Height / 2; }

        Span<double> raw = MemoryMarshal.Cast<Point, double>((Span<Point>)result);
        Numerics.VectorMath.TransformInterleave(xData, yData, xS, xO, yS, yO, raw);

        return result;
    }

    /// <summary>Converts pixel-space coordinates back to data-space coordinates.</summary>
    /// <remarks>The pixel is interpolated across the drawn range (in the axis' scaled space on a log or symlog axis),
    /// then the axis scale is inverted (<see cref="AxisScaleExtensions.Inverse"/>), so the result is in data units on
    /// every scale. The server-interaction script converts the pointer by the same rule.</remarks>
    /// <param name="pixel">The pixel-space point to convert.</param>
    /// <returns>The corresponding data-space point.</returns>
    public Point PixelToData(Point pixel)
    {
        double xRange = _dataXMax - _dataXMin;
        double yRange = _dataYMax - _dataYMin;

        double x = _dataXMin + (pixel.X - _plotBounds.X) / _plotBounds.Width * xRange;
        double y = _dataYMin + (1 - (pixel.Y - _plotBounds.Y) / _plotBounds.Height) * yRange;

        return new Point(_xAxisScale.Inverse(x, _xLinThresh), _yAxisScale.Inverse(y, _yLinThresh));
    }
}
