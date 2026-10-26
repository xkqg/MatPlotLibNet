// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>Shared predicates over a drawn series list — read by <see cref="Axes.HasFixedYRange"/> (the primary
/// list) and by a secondary axis slot's own series list, so the two never diverge on the same question
/// (CLAUDE.md §5: two layers deciding the same thing must use the same rule).</summary>
internal static class SeriesListExtensions
{
    extension(IReadOnlyList<ISeries> series)
    {
        /// <summary>True when this list is non-empty and every series in it fixes its own Y range (a state
        /// timeline), so pan and zoom have nothing to move vertically.</summary>
        internal bool HasFixedYRange() => series.Count > 0 && series.All(s => s is ChartSeries { FixesYRange: true });
    }
}
