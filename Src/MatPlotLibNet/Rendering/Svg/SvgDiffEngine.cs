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

    /// <summary>The content of every series group by its index: what stands between the end of the group's opening tag and the
    /// <c>&lt;/g&gt;</c> that closes it, found by counting the groups opened inside it, so a group of its own (an alpha group, a
    /// 3-D run) is part of the content rather than its end. A group written self-closed has no content; one never closed runs
    /// to the end of the document.</summary>
    private static Dictionary<int, string> ExtractSeriesGroups(string svg)
    {
        var groups = new Dictionary<int, string>();
        int from = 0;
        while (SeriesGroupRegex().Match(svg, from) is { Success: true } open)
        {
            int start = open.Index + open.Length;
            bool selfClosed = svg[start - 2] == '/';
            int end = selfClosed ? start : ClosingTagOf(svg, start);
            if (int.TryParse(open.Groups[1].Value, out int index))
                groups[index] = svg[start..end];
            from = selfClosed ? start : Math.Min(svg.Length, end + GroupClose.Length);
        }
        return groups;
    }

    private const string GroupClose = "</g>";

    /// <summary>Where the <c>&lt;/g&gt;</c> closing a group whose content starts at <paramref name="start"/> stands: each group
    /// opened inside it (not written self-closed) must close first. The document's length when it never closes.</summary>
    private static int ClosingTagOf(string svg, int start)
    {
        int depth = 0;
        for (int at = svg.IndexOf('<', start); at >= 0; at = svg.IndexOf('<', at + 1))
        {
            if (string.CompareOrdinal(svg, at, GroupClose, 0, GroupClose.Length) == 0)
            {
                if (depth == 0) return at;
                depth--;
            }
            else if (OpensGroup(svg, at))
            {
                int tagEnd = svg.IndexOf('>', at);
                if (tagEnd < 0) break;
                if (svg[tagEnd - 1] != '/') depth++;
                at = tagEnd;
            }
        }
        return svg.Length;
    }

    /// <summary>Whether the tag at <paramref name="at"/> opens a group: <c>&lt;g</c> followed by the end of the tag or a space,
    /// never another element whose name starts with g.</summary>
    private static bool OpensGroup(string svg, int at) =>
        at + 2 < svg.Length && svg[at + 1] == 'g' && (svg[at + 2] is '>' or '/' || char.IsWhiteSpace(svg[at + 2]));

    [GeneratedRegex(SvgAttributes.SeriesIndex + @"=""(\d+)""[^>]*>", RegexOptions.Singleline)]
    private static partial Regex SeriesGroupRegex();
}
