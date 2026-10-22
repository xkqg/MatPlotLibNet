// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Rendering.TickFormatters;

/// <summary>Maps a tick position to a category label: a tick anywhere from <c>k</c> to just below <c>k + 1</c> (such as the
/// centre of category <c>k</c>'s slot, <c>k + 0.5</c>) reads label <c>k</c>.
/// Pass <c>reversed: true</c> for an axis whose categories are drawn from the top down, such as the Y axis of a heatmap: a
/// heatmap draws its row 0 at the TOP while the Y axis counts upward, so the tick at the bottom (index 0) must read the
/// last label and the tick at the top the first.
/// <para>Made with positions (<see cref="CategoryFormatter(string[], double[])"/>), the labels stand at positions of their
/// own instead of on slots of width one: a tick at <c>positions[k]</c> reads label <c>k</c> and a tick anywhere else reads
/// nothing. That names categories whose widths differ, such as the columns of a Marimekko chart.</para></summary>
public sealed class CategoryFormatter : ITickFormatter, IJsonDescribed<TickFormatterDto>
{
    private readonly string[]  _labels;
    private readonly bool      _reversed;
    private readonly double[]? _positions;

    /// <param name="labels">Category names in display order (index 0 = first tick).</param>
    /// <param name="reversed">When <c>true</c>, index 0 maps to the last label: the categories are drawn from the top down
    /// (the rows of a heatmap).</param>
    public CategoryFormatter(string[] labels, bool reversed = false)
    {
        ArgumentNullException.ThrowIfNull(labels);
        _labels   = labels;
        _reversed = reversed;
    }

    /// <summary>Names categories at positions of their own: a tick at <c>positions[k]</c> reads <c>labels[k]</c>, and a tick
    /// at any other value reads nothing. For categories whose places along the axis are not slots of width one, such as the
    /// columns of a Marimekko chart, whose widths are shares. Pair it with a <see cref="TickLocators.FixedLocator"/> on the
    /// same positions, so a tick stands on every name.</summary>
    /// <param name="labels">The names, one for each position, in the same order.</param>
    /// <param name="positions">Where each name stands along the axis, in the axis' data units: the very values the axis'
    /// ticks are placed on.</param>
    /// <exception cref="ArgumentNullException"><paramref name="labels"/> or <paramref name="positions"/> is
    /// <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="positions"/> does not carry one position per label.</exception>
    /// <example>
    /// <code>
    /// double[] centres = [20.0, 60.0, 90.0];
    ///
    /// string svg = Plt.Create()
    ///     .AddSubPlot(1, 1, 1, ax => ax
    ///         .Plot([0.0, 100.0], [0.0, 1.0])
    ///         .SetXTickLocator(new FixedLocator(centres))
    ///         .SetXTickFormatter(new CategoryFormatter(["Retail", "Online", "Wholesale"], centres)))
    ///     .ToSvg();
    /// </code>
    /// </example>
    public CategoryFormatter(string[] labels, double[] positions)
    {
        ArgumentNullException.ThrowIfNull(labels);
        ArgumentNullException.ThrowIfNull(positions);
        if (positions.Length != labels.Length)
        {
            throw new ArgumentException(
                $"{positions.Length} positions were given for {labels.Length} labels; there must be one position per label.",
                nameof(positions));
        }

        _labels    = labels;
        _positions = positions;
    }

    /// <inheritdoc />
    TickFormatterDto IJsonDescribed<TickFormatterDto>.Describe() => new CategoryTickFormatterDto(_labels, _reversed, _positions);

    /// <inheritdoc />
    public string Format(double value)
    {
        int idx = _positions is not null ? Array.IndexOf(_positions, value)
            : _reversed ? _labels.Length - 1 - (int)value
            : (int)value;
        return idx >= 0 && idx < _labels.Length ? _labels[idx] : string.Empty;
    }
}
