// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Builders;

/// <summary>
/// Parses a subplot mosaic string into labeled <see cref="GridPosition"/> values.
/// </summary>
/// <remarks>
/// Each character in the mosaic string maps to one subplot label. Rows are separated by
/// <c>\n</c>. Each label must form a contiguous rectangular region; non-rectangular or
/// discontiguous spans throw <see cref="ArgumentException"/>.
/// </remarks>
/// <example>
/// <code>
/// // "AAB\nCCB" → A:(0,1,0,2), B:(0,2,2,3), C:(1,2,0,2)
/// var panels = SubplotMosaicParser.Parse("AAB\nCCB");
/// </code>
/// </example>
internal static class SubplotMosaicParser
{
    /// <summary>
    /// Parses the mosaic <paramref name="pattern"/> into a dictionary of label → <see cref="GridPosition"/>.
    /// </summary>
    /// <param name="pattern">Mosaic string with rows separated by <c>\n</c>.
    /// Each character is a subplot label; use the same character in multiple cells to span.</param>
    /// <returns>Dictionary mapping each label (single character as a string) to its grid position (0-based, exclusive end).</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the pattern is empty, rows have different lengths, or a label's cells do not
    /// form a contiguous rectangular region.
    /// </exception>
    internal static IReadOnlyDictionary<string, GridPosition> Parse(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            throw new ArgumentException("Mosaic pattern must not be empty.", nameof(pattern));

        var lines = pattern.Split('\n', StringSplitOptions.None);
        // Strip trailing whitespace but keep empty lines for now so we can give a better error
        var rows = lines
            .Select(l => l.TrimEnd())
            .Where(l => l.Length > 0)
            .ToArray();

        if (rows.Length == 0)
            throw new ArgumentException("Mosaic pattern contains no non-empty rows.", nameof(pattern));

        int cols = rows[0].Length;
        for (int r = 1; r < rows.Length; r++)
        {
            if (rows[r].Length != cols)
                throw new ArgumentException(
                    $"All rows must have the same length. Row 0 has {cols} cells; row {r} has {rows[r].Length}.",
                    nameof(pattern));
        }

        int numRows = rows.Length;

        // Collect bounding box per label
        var minRow = new Dictionary<char, int>();
        var maxRow = new Dictionary<char, int>();
        var minCol = new Dictionary<char, int>();
        var maxCol = new Dictionary<char, int>();

        for (int r = 0; r < numRows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                char label = rows[r][c];
                if (!minRow.ContainsKey(label))
                {
                    minRow[label] = r;
                    maxRow[label] = r;
                    minCol[label] = c;
                    maxCol[label] = c;
                }
                else
                {
                    if (r < minRow[label]) minRow[label] = r;
                    if (r > maxRow[label]) maxRow[label] = r;
                    if (c < minCol[label]) minCol[label] = c;
                    if (c > maxCol[label]) maxCol[label] = c;
                }
            }
        }

        // Validate each label fills its bounding box (no holes, no non-rectangular shapes)
        foreach (var label in minRow.Keys)
        {
            int rMin = minRow[label], rMax = maxRow[label];
            int cMin = minCol[label], cMax = maxCol[label];
            for (int r = rMin; r <= rMax; r++)
            for (int c = cMin; c <= cMax; c++)
            {
                if (rows[r][c] != label)
                    throw new ArgumentException(
                        $"Label '{label}' does not form a contiguous rectangular region. " +
                        $"Cell ({r},{c}) is occupied by '{rows[r][c]}'.",
                        nameof(pattern));
            }
        }

        // Build result dictionary (rowEnd and colEnd are exclusive)
        return minRow.ToDictionary(
            kv => kv.Key.ToString(),
            kv => new GridPosition(
                kv.Value,
                maxRow[kv.Key] + 1,
                minCol[kv.Key],
                maxCol[kv.Key] + 1));
    }

    /// <summary>Returns the grid dimensions implied by a mosaic <paramref name="pattern"/>. Rows
    /// are separated by <c>\n</c>; whitespace-only or empty rows are dropped. Columns are taken
    /// from the first non-empty row's length after <c>TrimEnd</c>.</summary>
    /// <param name="pattern">The mosaic-string input (same shape accepted by <see cref="Parse"/>).</param>
    /// <returns>A <see cref="MatShape"/> with <c>Rows = N</c> surviving rows and <c>Cols = L</c>
    /// columns (first row length), or <c>(0, 0)</c> for an empty / whitespace-only input.</returns>
    internal static MatShape GetDimensions(string pattern)
    {
        var rows = pattern.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.TrimEnd()).Where(l => l.Length > 0).ToArray();
        return new(rows.Length, rows.Length > 0 ? rows[0].Length : 0);
    }
}
