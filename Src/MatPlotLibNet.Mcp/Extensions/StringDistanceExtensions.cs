// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Mcp;

/// <summary>Edit distance between two words, used to suggest the chart type a misspelling most likely meant.</summary>
internal static class StringDistanceExtensions
{
    /// <summary>The Levenshtein distance between <paramref name="word"/> and <paramref name="other"/>, compared
    /// without regard to case: the number of single-character insertions, deletions and substitutions between them.</summary>
    internal static int EditDistanceTo(this string word, string other)
    {
        string a = word.ToLowerInvariant();
        string b = other.ToLowerInvariant();
        var previous = new int[b.Length + 1];
        var current = new int[b.Length + 1];
        for (int j = 0; j <= b.Length; j++)
        {
            previous[j] = j;
        }

        for (int i = 1; i <= a.Length; i++)
        {
            current[0] = i;
            for (int j = 1; j <= b.Length; j++)
            {
                int substitution = previous[j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1);
                current[j] = Math.Min(Math.Min(previous[j] + 1, current[j - 1] + 1), substitution);
            }
            (previous, current) = (current, previous);
        }

        return previous[b.Length];
    }
}
