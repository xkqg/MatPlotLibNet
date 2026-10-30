// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Interaction;

/// <summary>A layout whose data ranges are the ones it was made with: every member asks the layout it wraps, except the
/// ranges of the axes (<see cref="GetDataRange"/>, <see cref="GetSecondaryDataRanges"/>), which are read once when the
/// snapshot is taken. <see cref="ResetModifier"/> reads its "original" range through one, so a standalone modifier
/// keeps the range of the moment it was created.</summary>
internal sealed class SnapshotLayout : IChartLayout
{
    private readonly IChartLayout _live;
    private readonly DataRange[] _ranges;
    private readonly IReadOnlyList<SecondaryAxisRange>[] _secondaryRanges;

    /// <summary>Reads the ranges of every axes of <paramref name="live"/> now.</summary>
    /// <param name="live">The layout to wrap.</param>
    internal SnapshotLayout(IChartLayout live)
    {
        _live = live;
        _ranges = new DataRange[live.AxesCount];
        _secondaryRanges = new IReadOnlyList<SecondaryAxisRange>[live.AxesCount];
        for (int i = 0; i < _ranges.Length; i++)
        {
            _ranges[i] = live.GetDataRange(i);
            _secondaryRanges[i] = live.GetSecondaryDataRanges(i);
        }
    }

    /// <summary>The number of axes the snapshot was taken of.</summary>
    public int AxesCount => _ranges.Length;

    /// <inheritdoc />
    public Rect GetPlotArea(int axesIndex) => _live.GetPlotArea(axesIndex);

    /// <inheritdoc />
    public DataRange GetDataRange(int axesIndex) => _ranges[axesIndex];

    /// <inheritdoc />
    public IReadOnlyList<SecondaryAxisRange> GetSecondaryDataRanges(int axesIndex) => _secondaryRanges[axesIndex];

    /// <inheritdoc />
    public DataPoint? PixelToData(double pixelX, double pixelY, int axesIndex) => _live.PixelToData(pixelX, pixelY, axesIndex);

    /// <inheritdoc />
    public int? HitTestAxes(double pixelX, double pixelY) => _live.HitTestAxes(pixelX, pixelY);

    /// <inheritdoc />
    public int? HitTestLegendItem(double pixelX, double pixelY, int axesIndex) => _live.HitTestLegendItem(pixelX, pixelY, axesIndex);
}
