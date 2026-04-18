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
        double sx = ApplyScale(x, _xAxisScale, _xLinThresh);
        double sy = ApplyScale(y, _yAxisScale, _yLinThresh);

        // 2. Remap through axis breaks if active — raw values in break regions return NaN
        double rx = _xBreaks is null ? sx : AxisBreakMapper.Remap(_xBreaks, sx, _fullXMin, _fullXMax);
        double ry = _yBreaks is null ? sy : AxisBreakMapper.Remap(_yBreaks, sy, _fullYMin, _fullYMax);

        double px = _xScale == 0
            ? _plotBounds.X + _plotBounds.Width / 2
            : rx * _xScale + _xOffset;

        // Y is inverted: data-max maps to top, data-min maps to bottom
        double py = _yScale == 0
            ? _plotBounds.Y + _plotBounds.Height / 2
            : _yOffset - ry * _yScale;

        return new Point(px, py);
    }

    private static double ApplyScale(double v, AxisScale scale, double linthresh) => scale switch
    {
        AxisScale.SymLog => SymlogTransform.Forward(v, linthresh),
        AxisScale.Log    => v > 0 ? Math.Log10(v) : double.NaN,
        _                => v,
    };

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
            Array.Fill(dst, _plotBounds.X + _plotBounds.Width / 2);
        else if (_xBreaks is null && _xAxisScale == AxisScale.Linear)
            Numerics.VectorMath.MultiplyAdd(xData, _xScale, _xOffset, dst);
        else
            for (int i = 0; i < xData.Length; i++)
            {
                double sx = ApplyScale(xData[i], _xAxisScale, _xLinThresh);
                double rx = _xBreaks is null ? sx : AxisBreakMapper.Remap(_xBreaks, sx, _fullXMin, _fullXMax);
                dst[i] = rx * _xScale + _xOffset;
            }
        return dst;
    }

    /// <summary>Batch-transforms Y data coordinates to pixel Y values using SIMD MultiplyAdd.
    /// Note: Y axis is inverted — larger data values map to smaller pixel Y.</summary>
    /// <remarks>When <c>axis.Inverted == true</c> the scale is negated here; downstream renderers
    /// receive already-inverted pixel coordinates and must not apply a second inversion.</remarks>
    /// <param name="yData">Data-space Y values.</param>
    /// <returns>Pixel-space Y values, one per input element.</returns>
    public double[] TransformY(ReadOnlySpan<double> yData)
    {
        var dst = new double[yData.Length];
        if (_yScale == 0)
            Array.Fill(dst, _plotBounds.Y + _plotBounds.Height / 2);
        else if (_yBreaks is null && _yAxisScale == AxisScale.Linear)
            Numerics.VectorMath.MultiplyAdd(yData, -_yScale, _yOffset, dst);
        else
            for (int i = 0; i < yData.Length; i++)
            {
                double sy = ApplyScale(yData[i], _yAxisScale, _yLinThresh);
                double ry = _yBreaks is null ? sy : AxisBreakMapper.Remap(_yBreaks, sy, _fullYMin, _fullYMax);
                dst[i] = _yOffset - ry * _yScale;
            }
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

        Span<double> raw = MemoryMarshal.Cast<Point, double>(result);
        Numerics.VectorMath.TransformInterleave(xData, yData, xS, xO, yS, yO, raw);

        return result;
    }

    /// <summary>Converts pixel-space coordinates back to data-space coordinates.</summary>
    /// <param name="pixel">The pixel-space point to convert.</param>
    /// <returns>The corresponding data-space point.</returns>
    public Point PixelToData(Point pixel)
    {
        double xRange = _dataXMax - _dataXMin;
        double yRange = _dataYMax - _dataYMin;

        double x = _dataXMin + (pixel.X - _plotBounds.X) / _plotBounds.Width * xRange;
        double y = _dataYMin + (1 - (pixel.Y - _plotBounds.Y) / _plotBounds.Height) * yRange;

        return new Point(x, y);
    }
}
