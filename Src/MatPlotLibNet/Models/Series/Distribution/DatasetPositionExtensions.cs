// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Models.Series;

/// <summary>Where the datasets of a box or a violin series stand along the axis that carries their positions, and the
/// range that follows from it. The renderer places a dataset by <see cref="PositionOf"/> and the series ranges over the same
/// places by <see cref="CoverAlongPositions"/>, so the picture and the axes cannot disagree about where a dataset is.</summary>
internal static class DatasetPositionExtensions
{
    /// <summary>The position of the dataset at <paramref name="index"/>: the one <paramref name="positions"/> names for it,
    /// otherwise its index (a dataset the array does not reach, or no array at all, stands at 0, 1, 2 and so on).</summary>
    /// <param name="positions">The series' positions; <see langword="null"/> when it sets none.</param>
    /// <param name="index">The index of the dataset.</param>
    /// <returns>Where the dataset is drawn.</returns>
    internal static double PositionOf(this double[]? positions, int index) =>
        positions is not null && index < positions.Length ? positions[index] : index;

    /// <summary>The range a series that draws one thing per dataset covers along its positions. Placed by
    /// <paramref name="positions"/>, only the places its datasets are drawn at count, and a series that draws nothing covers
    /// nothing. Not placed (<see langword="null"/> or empty), the datasets stand at 0, 1, 2 and so on and the series keeps
    /// the room <paramref name="frame"/> leaves around them, widened for a dataset drawn wider than that.</summary>
    /// <param name="drawn">What each dataset that can be drawn covers around its position, in data units.</param>
    /// <param name="positions">The series' positions; <see langword="null"/> when it sets none.</param>
    /// <param name="frame">The room around the default positions: the range a series without positions has always covered.</param>
    /// <returns>The range, or <see langword="null"/> when positions are set and nothing can be drawn.</returns>
    internal static Range1D? CoverAlongPositions(this IEnumerable<Range1D> drawn, double[]? positions, Range1D frame)
    {
        var covered = drawn.DrawableRange();
        if (positions is { Length: > 0 })
        {
            return covered;
        }

        return covered is { } range ? frame.Merge(range.Lo, range.Hi) : frame;
    }
}
