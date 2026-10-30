// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>A word cloud: each word set in a size that follows its weight, the heaviest at the centre, packed into the plot
/// without overlap. The size follows the square root of the weight between <see cref="Sizes"/>; the packing is
/// <c>WordCloudLayout</c>'s, which is deterministic and shrinks every size together toward the smallest when the words do not
/// fit, and announces (never silently drops) a word that still has no room.
/// <para>It fills its plot without axes around it. The table gives each word with its weight and the size the weight gives it,
/// heaviest first, the order the words are read in the SVG; there the weight is also the title of each word, and each word says
/// how wide it is to be set, so the browser's own font cannot make it run into the next.</para></summary>
public sealed class WordCloudSeries : ChartSeries
{
    private const double RestingMinSize = 12;
    private const double RestingMaxSize = 60;

    private Range1D _sizes = new(RestingMinSize, RestingMaxSize);

    /// <inheritdoc />
    internal override bool DrawsWithoutAxes => true;

    /// <summary>The words, none empty and none twice.</summary>
    public string[] Words { get; }

    /// <summary>The weight of each word, a finite number above zero; only the proportions matter.</summary>
    public double[] Weights { get; }

    /// <summary>The size of the lightest and of the heaviest word, in points; 12 and 60 unless said otherwise.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The smallest is not above zero, the largest is below the smallest, or one is not a number.</exception>
    public Range1D Sizes
    {
        get => _sizes;
        set => _sizes = value.Lo > 0 && double.IsFinite(value.Hi) && value.Hi >= value.Lo
            ? value
            : throw new ArgumentOutOfRangeException(nameof(Sizes), value, "The sizes of a word cloud are a range above zero, from the smallest to the largest.");
    }

    /// <summary>The colour of each word, by position; <see langword="null"/> (the default) names none. A word past the end of the
    /// array, or every word when it is <see langword="null"/>, wears what the figure's theme hands out at its own position. A
    /// colour that cannot be read against the background at the size of the word is moved toward the text colour until it can.</summary>
    public Color[]? Colors { get; set; }

    /// <summary>Initializes a word cloud. Built through <c>Axes.WordCloud</c>, which refuses words that cannot be laid out.</summary>
    /// <param name="words">The words.</param>
    /// <param name="weights">The weight of each word.</param>
    public WordCloudSeries(string[] words, double[] weights)
    {
        Words = words;
        Weights = weights;
    }

    /// <summary>The words by position, heaviest first and ties in ordinal order of the word: the order they are placed, drawn and
    /// tabled in.</summary>
    internal IReadOnlyList<int> ReadingOrder() =>
        [.. Enumerable.Range(0, Math.Min(Words.Length, Weights.Length)).OrderByDescending(i => Weights[i]).ThenBy(i => Words[i], StringComparer.Ordinal)];

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context) => new(null, null, null, null);

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "wordcloud",
        Categories = Words,
        Values = Weights,
        WordMinSize = Sizes.Lo.OrNullAtRest(RestingMinSize),
        WordMaxSize = Sizes.Hi.OrNullAtRest(RestingMaxSize),
        Colors = Colors?.ToList(),
    };

    /// <summary>Reconstructs a <see cref="WordCloudSeries"/> from its serialization DTO and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static WordCloudSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.WordCloud(dto.Categories ?? [], dto.Values ?? []);
        s.Sizes = new Range1D(dto.WordMinSize ?? RestingMinSize, dto.WordMaxSize ?? RestingMaxSize);
        s.Colors = dto.Colors?.ToArray();
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);

    /// <inheritdoc />
    /// <remarks>One row for each word, heaviest first: the word, its weight and the size the weight gives it over
    /// <see cref="Sizes"/>. Where the word stands and whether it found room depend on the plot and the fonts it is drawn with, so
    /// they are not in the table.</remarks>
    public override ChartDataTable? ToDataTable()
    {
        var order = ReadingOrder();
        double lightest = Weights.DefaultIfEmpty(0).Min(), heaviest = Weights.DefaultIfEmpty(0).Max();
        var rows = new IReadOnlyList<DataCell>[order.Count];
        for (int row = 0; row < order.Count; row++)
        {
            int i = order[row];
            rows[row] = [DataCell.FromText(Words[i]), DataCell.FromNumber(Weights[i]), DataCell.FromNumber(WordCloudLayout.SizeOf(Weights[i], lightest, heaviest, Sizes))];
        }

        return new ChartDataTable(null, [new("word", DataColumnKind.Text), new("weight"), new("size")], rows);
    }
}
