// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Interaction;

/// <summary>Snapshot of a rendered figure's layout used by interaction modifiers to convert
/// pixel-space pointer positions to data-space coordinates.
/// Construct after each render by calling <see cref="Create(Figure, LayoutResult)"/>.</summary>
public sealed class ChartLayout : IChartLayout
{
    private readonly Figure _figure;
    private readonly IReadOnlyList<Rect> _plotAreas;
    private readonly IReadOnlyList<IReadOnlyList<LegendItemBounds>> _legendItems;

    private ChartLayout(Figure figure, IReadOnlyList<Rect> plotAreas,
        IReadOnlyList<IReadOnlyList<LegendItemBounds>> legendItems)
    {
        _figure      = figure;
        _plotAreas   = plotAreas;
        _legendItems = legendItems;
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
    public (double XMin, double XMax, double YMin, double YMax) GetDataRange(int axesIndex)
    {
        var axes = _figure.SubPlots[axesIndex];
        // Fall back to 0-1 when axis limits are not yet materialised (auto-range).
        double xMin = axes.XAxis.Min ?? 0.0;
        double xMax = axes.XAxis.Max ?? 1.0;
        double yMin = axes.YAxis.Min ?? 0.0;
        double yMax = axes.YAxis.Max ?? 1.0;
        return (xMin, xMax, yMin, yMax);
    }

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

    /// <summary>Converts a pixel position to data-space coordinates for the given axes.
    /// Returns <c>null</c> if the pixel is outside the axes plot area.</summary>
    public (double DataX, double DataY)? PixelToData(double pixelX, double pixelY, int axesIndex)
    {
        var area = _plotAreas[axesIndex];
        if (pixelX < area.X || pixelX > area.X + area.Width ||
            pixelY < area.Y || pixelY > area.Y + area.Height)
            return null;

        var (xMin, xMax, yMin, yMax) = GetDataRange(axesIndex);

        double dataX = xMin + (pixelX - area.X) / area.Width  * (xMax - xMin);
        // Y is inverted: top of plot = yMax, bottom = yMin (screen-Y increases downward).
        double dataY = yMax - (pixelY - area.Y) / area.Height * (yMax - yMin);

        return (dataX, dataY);
    }
}
