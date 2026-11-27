// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests;

/// <summary>A layout a host other than the library's own would write: one axes, a plot area and the range it shows, and a hit test.
/// <para>It declares only the members of <see cref="IChartLayout"/> that have no default body, so the interface's own defaults (the
/// pixel read through the plot area and the range, no secondary range) still run through it. A double that observes one of those lists
/// <see cref="IChartLayout"/> again beside this base and implements the member.</para></summary>
/// <param name="plotArea">The plot area, in pixels.</param>
/// <param name="range">The range the axes shows.</param>
internal class TestChartLayout(Rect plotArea, DataRange range) : IChartLayout
{
    /// <summary>The axes a hit test answers; <see langword="null"/> for none.</summary>
    public int? AxesHit { get; init; } = 0;

    /// <summary>Whether the hit test answers <see cref="AxesHit"/> only for a pixel on the plot area, and no axes elsewhere.</summary>
    public bool HitsOnlyOnThePlot { get; init; }

    /// <summary>The legend item a legend hit test answers; <see langword="null"/> for none.</summary>
    public int? LegendItemHit { get; init; }

    /// <inheritdoc />
    public int AxesCount => 1;

    /// <inheritdoc />
    public Rect GetPlotArea(int axesIndex) => plotArea;

    /// <inheritdoc />
    public DataRange GetDataRange(int axesIndex) => range;

    /// <inheritdoc />
    public int? HitTestAxes(double pixelX, double pixelY) =>
        !HitsOnlyOnThePlot || (pixelX >= plotArea.X && pixelX <= plotArea.X + plotArea.Width
            && pixelY >= plotArea.Y && pixelY <= plotArea.Y + plotArea.Height) ? AxesHit : null;

    /// <inheritdoc />
    public int? HitTestLegendItem(double pixelX, double pixelY, int axesIndex) => LegendItemHit;
}
