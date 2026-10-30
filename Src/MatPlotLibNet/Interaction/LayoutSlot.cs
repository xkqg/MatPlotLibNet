// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Interaction;

/// <summary>The layout the modifiers of one controller read: every member asks <see cref="Current"/>, the layout of the
/// latest repaint. A control repaints after every change and hands over a new layout each time; the modifiers hold this
/// slot instead of a layout, so the layout can be replaced under a gesture that is still going on (a rubber band being
/// drawn, a drag) without the modifiers having to be built again and losing it.</summary>
/// <param name="current">The layout of the first paint.</param>
internal sealed class LayoutSlot(IChartLayout current) : IChartLayout
{
    /// <summary>The layout of the latest repaint.</summary>
    internal IChartLayout Current { get; set; } = current;

    /// <inheritdoc />
    public int AxesCount => Current.AxesCount;

    /// <inheritdoc />
    public Rect GetPlotArea(int axesIndex) => Current.GetPlotArea(axesIndex);

    /// <inheritdoc />
    public DataRange GetDataRange(int axesIndex) => Current.GetDataRange(axesIndex);

    /// <inheritdoc />
    public IReadOnlyList<SecondaryAxisRange> GetSecondaryDataRanges(int axesIndex) => Current.GetSecondaryDataRanges(axesIndex);

    /// <inheritdoc />
    public DataPoint? PixelToData(double pixelX, double pixelY, int axesIndex) => Current.PixelToData(pixelX, pixelY, axesIndex);

    /// <inheritdoc />
    public int? HitTestAxes(double pixelX, double pixelY) => Current.HitTestAxes(pixelX, pixelY);

    /// <inheritdoc />
    public int? HitTestLegendItem(double pixelX, double pixelY, int axesIndex) => Current.HitTestLegendItem(pixelX, pixelY, axesIndex);
}
