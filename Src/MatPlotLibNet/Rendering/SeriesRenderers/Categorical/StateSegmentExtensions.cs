// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>The one fill a state segment is drawn with, read by the timeline that draws the segment and by the legend swatch
/// that shows its state, so the two cannot disagree.</summary>
internal static class StateSegmentExtensions
{
    extension(StateSegment segment)
    {
        /// <summary>The fill of the segment: its colour, and a pattern for it. A hatched segment reads as "no information" rather
        /// than as a state (see <see cref="StateSegment.Hatch"/>). When the caller stated a condition instead of a pattern, the
        /// condition supplies one against the theme's alarm palette; an explicit hatch still wins.</summary>
        /// <param name="theme">The theme whose alarm palette resolves a stated condition.</param>
        internal ShapeStyle Fill(Theme theme) =>
            new(segment.Color, null, 0)
            {
                Hatch = segment.Hatch != HatchPattern.None ? segment.Hatch : segment.Condition.Resolve(theme.Alarm).Hatch,
            };
    }

    extension(IEnumerable<StateSegment> segments)
    {
        /// <summary>The distinct fills of the segments that can be drawn, in the order each first appears: a segment with an end
        /// that is missing or infinite is not drawn, so its state shows nowhere.</summary>
        /// <param name="theme">The theme whose alarm palette resolves a stated condition.</param>
        internal IReadOnlyList<ShapeStyle> DistinctFills(Theme theme) =>
            [.. segments.Where(s => double.IsFinite(s.Start) && double.IsFinite(s.End)).Select(s => s.Fill(theme)).Distinct()];
    }
}
