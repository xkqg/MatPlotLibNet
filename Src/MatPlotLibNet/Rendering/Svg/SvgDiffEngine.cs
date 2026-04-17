// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>Computes minimal diffs between two SVG strings by comparing series group content.
/// Series groups are identified by <c>data-series-index</c> attributes emitted by the renderer.
/// The diff output contains only the groups whose content changed — typically 10x smaller than
/// the full SVG for streaming scenarios where axes/ticks/labels stay constant.</summary>
public static partial class SvgDiffEngine
{
    /// <summary>A single element replacement in the SVG diff.</summary>
    /// <param name="SeriesIndex">The data-series-index of the changed group.</param>
    /// <param name="NewContent">The full new SVG content for this series group.</param>
    public readonly record struct SvgPatchEntry(int SeriesIndex, string NewContent);

    /// <summary>Result of computing an SVG diff.</summary>
    /// <param name="Patches">Changed series groups. Empty if SVGs are identical.</param>
    /// <param name="IsFullReplace">True if the diff couldn't be computed (structural change) and
    /// the full SVG should be sent instead.</param>
    public readonly record struct SvgPatch(IReadOnlyList<SvgPatchEntry> Patches, bool IsFullReplace);

    /// <summary>Computes the diff between <paramref name="previous"/> and <paramref name="current"/> SVG strings.</summary>
    /// <param name="previous">The previously rendered SVG.</param>
    /// <param name="current">The newly rendered SVG.</param>
    /// <returns>A patch containing only the changed series groups, or a full-replace flag if
    /// the structure changed (axes added/removed, series count changed).</returns>
    public static SvgPatch Compute(string previous, string current)
    {
        if (previous == current)
            return new SvgPatch([], false);

        var prevGroups = ExtractSeriesGroups(previous);
        var currGroups = ExtractSeriesGroups(current);

        // If series count changed, structural change — full replace
        if (prevGroups.Count != currGroups.Count)
            return new SvgPatch([], true);

        var patches = new List<SvgPatchEntry>();
        foreach (var (index, content) in currGroups)
        {
            if (!prevGroups.TryGetValue(index, out var prevContent) || prevContent != content)
                patches.Add(new SvgPatchEntry(index, content));
        }

        return new SvgPatch(patches, false);
    }

    /// <summary>Estimates the bandwidth savings of using the patch vs full SVG.</summary>
    /// <param name="patch">The computed patch.</param>
    /// <param name="fullSvgLength">Length of the full SVG string.</param>
    /// <returns>Compression ratio (0.0 = no savings, 1.0 = empty patch).</returns>
    public static double CompressionRatio(SvgPatch patch, int fullSvgLength)
    {
        if (patch.IsFullReplace || fullSvgLength == 0) return 0.0;
        int patchSize = 0;
        foreach (var p in patch.Patches) patchSize += p.NewContent.Length;
        return 1.0 - (double)patchSize / fullSvgLength;
    }

    private static Dictionary<int, string> ExtractSeriesGroups(string svg)
    {
        var groups = new Dictionary<int, string>();
        var matches = SeriesGroupRegex().Matches(svg);
        foreach (Match match in matches)
        {
            if (int.TryParse(match.Groups[1].Value, out int index))
                groups[index] = match.Groups[2].Value;
        }
        return groups;
    }

    [GeneratedRegex(@"data-series-index=""(\d+)""[^>]*>(.*?)</g>", RegexOptions.Singleline)]
    private static partial Regex SeriesGroupRegex();
}
