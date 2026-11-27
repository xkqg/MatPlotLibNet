// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>How a <see cref="PairGridSeries"/> reads its <see cref="PairGridSeries.HueGroups"/>: whether they can colour its
/// samples, and the colour each group takes. Shared by every painter that respects them.</summary>
internal static class PairGridHue
{
    extension(PairGridSeries series)
    {
        /// <summary>Whether <c>HueGroups</c> is set with a length matching the sample axis. Mismatched lengths fall back to
        /// single-colour rendering — the same defensive pattern as <c>ClustermapSeries.ResolveLeafOrder</c>. A grid always
        /// holds at least one variable, so the first one's length is the sample axis.</summary>
        internal bool HasValidHue =>
            series.HueGroups is { Length: > 0 } hue
            && hue.Length == series.Variables[0].Length;

        /// <summary>Builds the per-render hue→colour lookup table, keyed by group ID. The caller has already verified
        /// <c>HasValidHue</c>, so every group ID present in <c>HueGroups</c> is in the table.</summary>
        /// <param name="theme">The theme the grid is drawn under, whose cycle colours the groups when the series names no palette.</param>
        internal Dictionary<int, Color> HueColors(Theme theme)
        {
            var colors = new Dictionary<int, Color>();
            foreach (int g in series.HueGroups!)
                if (!colors.ContainsKey(g)) colors[g] = series.HueColorOf(g, theme);
            return colors;
        }

        /// <summary>Resolves the colour for a given hue <paramref name="group"/>. When the series has a
        /// <see cref="PairGridSeries.HuePalette"/>, indexes into it modulo length; otherwise indexes the theme's cycle
        /// (<c>Theme.CycleMap</c>) modulo its length.</summary>
        /// <param name="group">The hue group.</param>
        /// <param name="theme">The theme the grid is drawn under.</param>
        internal Color HueColorOf(int group, Theme theme)
        {
            if (series.HuePalette is { Length: > 0 } palette)
                return palette[((group % palette.Length) + palette.Length) % palette.Length];

            var cycle = theme.CycleMap;
            int colours = cycle.ColorCount;
            int bucket = ((group % colours) + colours) % colours;
            return cycle.GetColor((bucket + 0.5) / colours);
        }

        /// <summary>The samples of one variable split by their hue group: one slice per distinct group, smallest group first,
        /// each holding the group's samples in their order. The caller has already verified <c>HasValidHue</c>, so the groups
        /// are as long as the variable.</summary>
        /// <param name="variable">The index of the variable in <see cref="PairGridSeries.Variables"/>.</param>
        /// <returns>One slice per group.</returns>
        internal PairGridHueSlice[] SamplesByHue(int variable)
        {
            double[] data = series.Variables[variable];
            int[] hue = series.HueGroups!;
            var byGroup = new Dictionary<int, List<double>>();
            for (int k = 0; k < data.Length; k++)
            {
                if (!byGroup.TryGetValue(hue[k], out var list))
                    byGroup[hue[k]] = list = [];
                list.Add(data[k]);
            }

            var slices = new PairGridHueSlice[byGroup.Count];
            int s = 0;
            foreach (var (group, list) in byGroup) slices[s++] = new PairGridHueSlice(group, [.. list]);
            Array.Sort(slices, static (a, b) => a.Group.CompareTo(b.Group));
            return slices;
        }
    }
}

/// <summary>One hue group's samples of a pair grid's variable.</summary>
/// <param name="Group">The hue group, which picks the colour.</param>
/// <param name="Data">The group's samples, in their order in the variable.</param>
internal readonly record struct PairGridHueSlice(int Group, double[] Data);
