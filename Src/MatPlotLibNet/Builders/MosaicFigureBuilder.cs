// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Diagnostics;
using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Builders;

/// <summary>
/// Fluent builder for string-pattern subplot layouts.
/// Use <see cref="Panel"/> to configure each labeled panel, then call <see cref="Build"/> to
/// get the underlying <see cref="FigureBuilder"/>.
/// </summary>
/// <example>
/// <code>
/// Plt.Mosaic("AAB\nCCB", m =>
/// {
///     m.Panel("A", ax => ax.Plot(x, y).WithTitle("Top Left"));
///     m.Panel("B", ax => ax.Bar(labels, vals).WithTitle("Right"));
///     m.Panel("C", ax => ax.Scatter(x, y).WithTitle("Bottom Left"));
/// }).Save("mosaic.svg");
/// </code>
/// </example>
public sealed class MosaicFigureBuilder
{
    private readonly string _pattern;
    private readonly Dictionary<string, Action<AxesBuilder>> _panels = new();
    private readonly List<Action<FigureBuilder>> _figureSteps = [];

    /// <summary>Creates a new mosaic builder for the given pattern string.</summary>
    /// <param name="pattern">Mosaic string (e.g. <c>"AAB\nCCB"</c>).</param>
    public MosaicFigureBuilder(string pattern)
    {
        _pattern = pattern;
    }

    /// <summary>Configures the panel identified by <paramref name="label"/>. A second call for the same label replaces the
    /// first; a label the pattern does not hold configures nothing, and <see cref="Build"/> reports it through
    /// <see cref="ChartDiagnostics"/>.</summary>
    /// <param name="label">Single-character label string matching a character in the mosaic pattern.</param>
    /// <param name="configure">Action to configure the axes for this panel.</param>
    /// <returns>This builder for chaining.</returns>
    public MosaicFigureBuilder Panel(string label, Action<AxesBuilder> configure)
    {
        _panels[label] = configure;
        return this;
    }

    /// <summary>Configures figure-level properties (size, title, theme, etc.). The action is kept and taken on the figure
    /// builder of every <see cref="Build"/>, before the panels are added.</summary>
    public MosaicFigureBuilder Configure(Action<FigureBuilder> configure)
    {
        _figureSteps.Add(configure);
        return this;
    }

    /// <summary>Builds a new <see cref="FigureBuilder"/> with the figure-level configuration and every panel on it. Each call
    /// returns a builder of its own, so building twice gives the same panels twice over, never each panel twice, and an edit to
    /// one returned builder does not reach the next.</summary>
    /// <returns>A <see cref="FigureBuilder"/> with all subplot positions pre-configured.</returns>
    /// <remarks>A panel whose label the pattern does not hold adds nothing to the figure; each such label is reported through
    /// <see cref="ChartDiagnostics"/> (source <c>MosaicFigureBuilder</c>), the library's one channel for input it cannot use, and the
    /// figure is built without it.</remarks>
    /// <exception cref="ArgumentException">Thrown when the mosaic pattern is invalid.</exception>
    public FigureBuilder Build()
    {
        var positions = Parse(_pattern);
        var (rows, cols) = GetDimensions(_pattern);

        foreach (var label in _panels.Keys)
        {
            if (!positions.ContainsKey(label))
            {
                ChartDiagnostics.Emit(new ChartDiagnostic(
                    nameof(MosaicFigureBuilder),
                    $"Panel '{label}' is not in the mosaic pattern \"{_pattern}\" and was left out.",
                    null));
            }
        }

        var figure = new FigureBuilder();
        foreach (var step in _figureSteps) step(figure);
        figure.WithGridSpec(rows, cols);

        foreach (var (label, position) in positions)
        {
            var configure = _panels.TryGetValue(label, out var action) ? action : _ => { };
            figure.AddSubPlot(position, configure);
        }

        return figure;
    }

    /// <summary>Builds the figure and renders it to an SVG string.</summary>
    public string ToSvg() => Build().ToSvg();

    /// <summary>Builds the figure and saves it to <paramref name="path"/>.</summary>
    public void Save(string path) => Build().Save(path);

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

        // A pattern that is not all white space has a row with a character in it, so there is at least one row.
        var rows = RowsOf(pattern);

        rows.Select(row => row.Length).ThrowIfNotAllEqual(nameof(pattern));
        int cols = rows[0].Length;

        int numRows = rows.Length;

        // The smallest block of cells that holds every cell of one label, grown cell by cell (ends exclusive)
        var bounds = new Dictionary<char, GridPosition>();

        for (int r = 0; r < numRows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                char label = rows[r][c];
                bounds[label] = bounds.TryGetValue(label, out var seen)
                    ? new GridPosition(Math.Min(seen.RowStart, r), Math.Max(seen.RowEnd, r + 1), Math.Min(seen.ColStart, c), Math.Max(seen.ColEnd, c + 1))
                    : GridPosition.Single(r, c);
            }
        }

        // Validate each label fills its bounding box (no holes, no non-rectangular shapes)
        foreach (var (label, box) in bounds)
        {
            for (int r = box.RowStart; r < box.RowEnd; r++)
            for (int c = box.ColStart; c < box.ColEnd; c++)
            {
                if (rows[r][c] != label)
                    throw new ArgumentException(
                        $"Label '{label}' does not form a contiguous rectangular region. " +
                        string.Invariant($"Cell ({r},{c}) is occupied by '{rows[r][c]}'."),
                        nameof(pattern));
            }
        }

        return bounds.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value);
    }

    /// <summary>Returns the grid dimensions implied by a mosaic <paramref name="pattern"/>. Rows
    /// are separated by <c>\n</c>; whitespace-only or empty rows are dropped. Columns are taken
    /// from the first non-empty row's length after <c>TrimEnd</c>.</summary>
    /// <param name="pattern">The mosaic-string input (same shape accepted by <see cref="Builders.MosaicFigureBuilder.Parse"/>).</param>
    /// <returns>A <see cref="MatShape"/> with <c>Rows = N</c> surviving rows and <c>Cols = L</c>
    /// columns (first row length), or <c>(0, 0)</c> for an empty / whitespace-only input.</returns>
    internal static MatShape GetDimensions(string pattern)
    {
        var rows = RowsOf(pattern);
        return new(rows.Length, rows.Length > 0 ? rows[0].Length : 0);
    }

    /// <summary>The rows of a mosaic <paramref name="pattern"/>, the one rule <see cref="Builders.MosaicFigureBuilder.Parse"/> and
    /// <see cref="Builders.MosaicFigureBuilder.GetDimensions"/> both read it by: split at <c>\n</c>, each row stripped of trailing whitespace (so a
    /// <c>\r\n</c> line ending leaves no <c>\r</c>), and a row left empty dropped.</summary>
    /// <param name="pattern">The mosaic string.</param>
    /// <returns>The non-empty rows, top to bottom.</returns>
    private static string[] RowsOf(string pattern) =>
        [.. pattern.Split('\n').Select(l => l.TrimEnd()).Where(l => l.Length > 0)];
}
