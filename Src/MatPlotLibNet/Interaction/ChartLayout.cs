// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Linq;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;

namespace MatPlotLibNet.Interaction;

/// <summary>A rendered figure's layout, used by interaction modifiers to convert pixel-space pointer positions to
/// data-space coordinates. It holds the plot areas and legend entries of the render; the data range and scale of each
/// axes are the renderer's own (<see cref="PixelToData"/>, <see cref="GetDataRange"/>).
/// Construct after each render by calling <see cref="Create(Figure, LayoutResult)"/>.</summary>
public sealed class ChartLayout : IChartLayout
{
    private readonly Figure _figure;
    private readonly IReadOnlyList<Rect> _plotAreas;
    private readonly IReadOnlyList<IReadOnlyList<LegendItemBounds>> _legendItems;

    /// <summary>The surface the axes renderer is built on to compute a view (<see cref="View"/>). Computing a view draws
    /// and measures nothing, so nothing is ever written to it.</summary>
    private readonly SvgRenderContext _viewSurface = new();

    /// <summary>The last view computed per axes (<see cref="View"/>), with the inputs it was computed for.</summary>
    private readonly CachedView?[] _views;

    private ChartLayout(Figure figure, IReadOnlyList<Rect> plotAreas,
        IReadOnlyList<IReadOnlyList<LegendItemBounds>> legendItems)
    {
        _figure      = figure;
        _plotAreas   = plotAreas;
        _legendItems = legendItems;
        _views       = new CachedView?[plotAreas.Count];
    }

    /// <summary>Creates a <see cref="ChartLayout"/> from a <see cref="LayoutResult"/>
    /// returned by <c>ChartRenderer.ComputeLayout</c>.</summary>
    public static ChartLayout Create(Figure figure, LayoutResult layoutResult)
    {
        if (figure is null) throw new ArgumentNullException(nameof(figure));
        if (layoutResult is null) throw new ArgumentNullException(nameof(layoutResult));
        return new ChartLayout(figure, layoutResult.PlotAreas, layoutResult.LegendItems);
    }

    /// <summary>Creates a <see cref="ChartLayout"/> from the given figure and the plot-area rects
    /// returned by <c>ChartRenderer.ComputeSubPlotLayout</c>. Legend hit-testing will be
    /// unavailable (returns <c>null</c>).</summary>
    public static ChartLayout Create(Figure figure, IReadOnlyList<Rect> plotAreas)
    {
        if (figure is null) throw new ArgumentNullException(nameof(figure));
        if (plotAreas is null) throw new ArgumentNullException(nameof(plotAreas));
        return new ChartLayout(figure, plotAreas, Array.Empty<IReadOnlyList<LegendItemBounds>>());
    }

    /// <inheritdoc />
    public int AxesCount => _plotAreas.Count;

    /// <inheritdoc />
    public Rect GetPlotArea(int axesIndex) => _plotAreas[axesIndex];

    /// <inheritdoc />
    /// <remarks>The range the renderer draws: after autoscale, margins and limits, in data units on every scale, in drawn
    /// orientation (a reversed limit stays reversed). See <see cref="View"/>.</remarks>
    public DataRange GetDataRange(int axesIndex) => View(axesIndex).DrawnRange;

    /// <inheritdoc />
    /// <remarks>Not cached like <see cref="View"/> — a figure with secondary axes being actively dragged recomputes
    /// every move. Scoped deliberately: the primary cache exists because a line of a million points is 8ms to
    /// autoscale and a drag converts several times a frame; a secondary axis pays that cost again per move until
    /// profiling shows it matters in practice.</remarks>
    public IReadOnlyList<SecondaryAxisRange> GetSecondaryDataRanges(int axesIndex) =>
        SecondaryViews(axesIndex).Select(v => new SecondaryAxisRange(v.Side, v.SlotIndex, v.Transform.DrawnRange)).ToList();

    /// <summary>The transform the renderer draws every secondary Y axis with, for the figure as it is now — the
    /// secondary counterpart of <see cref="View"/>, read by an interaction modifier through
    /// <see cref="ChartLayoutViewExtensions.SecondaryViewsOf"/> instead of <see cref="GetSecondaryDataRanges"/> when it
    /// needs to convert a pixel position through a SPECIFIC secondary axis' own scale.</summary>
    internal IReadOnlyList<SecondaryAxisTransform> SecondaryViews(int axesIndex)
    {
        var axes = _figure.SubPlots[axesIndex];
        var area = _plotAreas[axesIndex];
        return AxesRenderer.Create(axes, area, _viewSurface, _figure.Theme).ComputeSecondaryViewTransforms();
    }

    /// <summary>The transform the renderer draws axes <paramref name="axesIndex"/> with, for the figure as it is now
    /// (<see cref="AxesRenderer.ComputeViewTransform"/>, the call <c>Render</c> makes): the plot area, the range after
    /// autoscale, margins and limits, and the axes' scales. After a paint that is the picture on screen; after a gesture
    /// applied to the figure in place and before the repaint, it is the picture the repaint draws, so a second wheel
    /// notch zooms on from the first. Axes that draw no Cartesian range (polar, 3-D) map their explicit limits, or 0..1,
    /// linearly over the plot area.</summary>
    /// <remarks>Computing a view folds every series' data range (8 ms for a line of a million points), and a pointer move
    /// converts several times, so each axes keeps its view until what a gesture changes — the limits and the scales —
    /// has changed. Data that changes without a gesture is followed by a repaint, which builds a new layout.</remarks>
    internal DataTransform View(int axesIndex)
    {
        var axes = _figure.SubPlots[axesIndex];
        var area = _plotAreas[axesIndex];
        var inputs = ViewInputs.Of(axes);
        if (_views[axesIndex] is { } cached && cached.Inputs == inputs) return cached.Transform;

        var transform = AxesRenderer.Create(axes, area, _viewSurface, _figure.Theme).ComputeViewTransform()
            ?? new DataTransform(axes.XAxis.Min ?? 0.0, axes.XAxis.Max ?? 1.0, axes.YAxis.Min ?? 0.0, axes.YAxis.Max ?? 1.0, area);
        _views[axesIndex] = new CachedView(inputs, transform);
        return transform;
    }

    /// <summary>What a gesture can change about the range an axes draws: its limits and its scales.</summary>
    private readonly record struct ViewInputs(
        double? XMin, double? XMax, double? YMin, double? YMax,
        AxisScale XScale, AxisScale YScale, double XLinThresh, double YLinThresh)
    {
        public static ViewInputs Of(Axes axes) => new(
            axes.XAxis.Min, axes.XAxis.Max, axes.YAxis.Min, axes.YAxis.Max,
            axes.XAxis.Scale, axes.YAxis.Scale, axes.XAxis.SymLogLinThresh, axes.YAxis.SymLogLinThresh);
    }

    /// <summary>A view and the inputs it was computed for.</summary>
    private sealed record CachedView(ViewInputs Inputs, DataTransform Transform);

    /// <inheritdoc />
    public int? HitTestAxes(double pixelX, double pixelY)
    {
        for (int i = 0; i < _plotAreas.Count; i++)
        {
            var r = _plotAreas[i];
            if (pixelX >= r.X && pixelX <= r.X + r.Width &&
                pixelY >= r.Y && pixelY <= r.Y + r.Height)
                return i;
        }
        return null;
    }

    /// <inheritdoc />
    public int? HitTestLegendItem(double pixelX, double pixelY, int axesIndex)
    {
        if (axesIndex < 0 || axesIndex >= _legendItems.Count) return null;
        var items = _legendItems[axesIndex];
        for (int i = 0; i < items.Count; i++)
        {
            var b = items[i].Bounds;
            if (pixelX >= b.X && pixelX <= b.X + b.Width &&
                pixelY >= b.Y && pixelY <= b.Y + b.Height)
                return items[i].SeriesIndex;
        }
        return null;
    }

    /// <summary>Converts a pixel position to data-space coordinates for the given axes, with the range and the scale the
    /// renderer draws (<see cref="DataTransform.PixelToData"/>): the value under the pointer on an autoscaled, limited,
    /// log or symlog axis. Returns <c>null</c> if the pixel is outside the axes plot area.</summary>
    public DataPoint? PixelToData(double pixelX, double pixelY, int axesIndex)
    {
        var area = _plotAreas[axesIndex];
        if (pixelX < area.X || pixelX > area.X + area.Width ||
            pixelY < area.Y || pixelY > area.Y + area.Height)
            return null;

        var data = View(axesIndex).PixelToData(new Point(pixelX, pixelY));
        return new DataPoint(data.X, data.Y);
    }
}
