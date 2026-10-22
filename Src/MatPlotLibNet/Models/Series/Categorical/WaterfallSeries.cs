// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a waterfall chart showing how an initial value is affected by sequential positive and negative changes.
/// Its steps are categories: the X axis names each step under its bar, on the centre of the step's slot
/// (<see cref="CategorySlot"/>), as it names the categories of a bar chart.</summary>
public sealed class WaterfallSeries : ChartSeries, ICategoryLabeled
{
    private int[]? _totalIndices;

    /// <summary>The name of each step, in order. The X axis writes them under the bars.</summary>
    public string[] Categories { get; }

    string[]? ICategoryLabeled.CategoryLabels => Categories;

    /// <summary>The signed change of each step, in order. A step named in <see cref="TotalIndices"/> holds a level instead:
    /// the total it reaches.</summary>
    public double[] Values { get; }

    /// <summary>The colour of a step that raises the running total.</summary>
    public Color IncreaseColor { get; set; } = Colors.Green;

    /// <summary>The colour of a step that lowers the running total.</summary>
    public Color DecreaseColor { get; set; } = Colors.Red;

    /// <summary>The colour of a total bar, a step named in <see cref="TotalIndices"/>. A series that names none never uses it.</summary>
    public Color TotalColor { get; set; } = Colors.Tab10Blue;

    /// <summary>The width of a bar, as a share of the space for one step.</summary>
    public double BarWidth { get; set; } = 0.6;

    /// <summary>The steps that are totals, by their position in <see cref="Values"/>; <see langword="null"/> when none is.
    /// A total is a level, not a change: its bar stands on zero, reaches the value it holds and is drawn in
    /// <see cref="TotalColor"/>, and the steps after it start from that level, whatever the steps before it added up to
    /// (a total is stated, never summed). A total whose value is missing (NaN) is not drawn and leaves the running total
    /// unknown until the next total.</summary>
    /// <exception cref="ArgumentOutOfRangeException">An index is negative or past the last step. The list is not kept.</exception>
    /// <example>
    /// <code>
    /// ax.Waterfall(["Q1", "Q2", "Q3", "Q4", "Year"], [120, -45, 80, 30, 185], s => s.TotalIndices = [4]);
    /// </code>
    /// </example>
    public int[]? TotalIndices
    {
        get => _totalIndices;
        set
        {
            if (value is not null)
            {
                foreach (int index in value)
                {
                    if ((uint)index >= (uint)Values.Length)
                    {
                        throw new ArgumentOutOfRangeException(nameof(value), index,
                            $"TotalIndices (\"totalIndices\" in JSON) holds {index}, which names no step. Steps are numbered from 0 and the series has {Values.Length}.");
                    }
                }
            }

            _totalIndices = value;
        }
    }

    /// <summary>Initializes a new <see cref="WaterfallSeries"/> with category labels and step values.</summary>
    /// <param name="categories">Label for each step.</param>
    /// <param name="values">Signed delta for each step.</param>
    public WaterfallSeries(string[] categories, double[] values)
    {
        Categories = categories; Values = values;
    }

    /// <summary>The bars as they stand now: for each step the level it stands on and the level it reaches. A step stands on
    /// the running total before it and reaches that total plus its value; a total stands on zero and reaches the level it
    /// holds, and the running total becomes that level. The one place that says what a bar is; the data range and the
    /// renderer both read it.</summary>
    /// <returns>One bar per value, in order.</returns>
    internal WaterfallBar[] Bars()
    {
        var bars = new WaterfallBar[Values.Length];
        var totals = _totalIndices is { Length: > 0 } marked ? new HashSet<int>(marked) : null;
        double running = 0;
        for (int i = 0; i < bars.Length; i++)
        {
            double value = Values[i];
            bool isTotal = totals?.Contains(i) == true;
            double foot = isTotal ? 0 : running;
            double top = isTotal ? value : running + value;
            bars[i] = new WaterfallBar(foot, top, isTotal, value >= 0);
            running = top;
        }

        return bars;
    }

    /// <summary>Where the bar of step <paramref name="index"/> stands: centred in the step's slot
    /// (<see cref="CategorySlot"/>), <see cref="BarWidth"/> of the slot wide. The renderer draws the bar and its connectors
    /// here, where the axis writes the step's name.</summary>
    /// <param name="index">The step's index, from 0.</param>
    /// <returns>The stretch the bar covers along X.</returns>
    internal CategorySlot SlotOf(int index) => CategorySlot.Of(index).Bar(BarWidth, 0, BarAlignment.Center);

    /// <inheritdoc />
    /// <remarks>Along X the slots of the steps, sticky at both ends, unless the axes carry limits, which win. Along Y the
    /// zero the first step and the totals stand on and every level a step reaches (each step's foot is the level before
    /// it), over what the Y axis can place: on a log axis, which has no place for zero, from the lowest level.</remarks>
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        double xMin = context.XAxisMin ?? CategorySlot.Of(0).Left;
        double xMax = context.XAxisMax ?? CategorySlot.Of(Categories.Length - 1).Right;
        var scale = context.YScale;
        // A level that cannot be placed (from a missing or infinite step on, until a total states one) is not drawn, so
        // it is left out here too.
        var y = Bars().Aggregate(Range1D.Empty.Including(0, scale), (range, bar) => range.Including(bar.Top, scale)).OrNone();
        return new(xMin, xMax, y?.Lo, y?.Hi,
            StickyXMin: xMin, StickyXMax: xMax, StickyYMin: 0);
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "waterfall",
        Categories = Categories, Values = Values,
        IncreaseColor = IncreaseColor, DecreaseColor = DecreaseColor,
        TotalColor = TotalColor, BarWidth = BarWidth,
        // No total marked, or an empty list, writes nothing: a series at rest writes the bytes it always wrote.
        TotalIndices = _totalIndices is { Length: > 0 } ? _totalIndices : null,
    };

    /// <summary>Reconstructs a <see cref="WaterfallSeries"/> from its serialization DTO, including increase/decrease/total colours, and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static WaterfallSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Waterfall(dto.Categories ?? [], dto.Values ?? []);
        if (dto.IncreaseColor.HasValue)
        {
            s.IncreaseColor = dto.IncreaseColor.Value;
        }
        if (dto.DecreaseColor.HasValue)
        {
            s.DecreaseColor = dto.DecreaseColor.Value;
        }
        if (dto.TotalColor.HasValue)
        {
            s.TotalColor = dto.TotalColor.Value;
        }
        if (dto.BarWidth.HasValue)
        {
            s.BarWidth = dto.BarWidth.Value;
        }

        s.TotalIndices = dto.TotalIndices;
        return s;
    }


    /// <inheritdoc />
    /// <remarks>The categories as text, then the values under the series' own label.</remarks>
    public override ChartDataTable? ToDataTable()
    {
        int count = Math.Min(Categories.Length, Values.Length);
        var rows = new IReadOnlyList<DataCell>[count];
        for (int i = 0; i < count; i++)
        {
            rows[i] = [DataCell.FromText(Categories[i]), DataCell.FromNumber(Values[i])];
        }

        return new ChartDataTable(null, [new("category", DataColumnKind.Text), new(Label ?? "value")], rows);
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
