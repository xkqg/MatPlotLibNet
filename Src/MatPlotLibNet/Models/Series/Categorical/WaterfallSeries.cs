// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a waterfall chart showing how an initial value is affected by sequential positive and negative changes.
/// Its steps are categories: the X axis names each step under its bar, on the centre of the step's slot,
/// as it names the categories of a bar chart.</summary>
public sealed class WaterfallSeries : ChartSeries, ICategoryLabeled, IEdgedPatch, ISeriesKind<WaterfallSeries>
{
    private int[]? _totalIndices;
    private double[]? _targets;

    /// <inheritdoc />
    internal override LegendSwatch LegendSwatch => LegendSwatch.EdgedPatch;

    /// <summary>The name of each step, in order. The X axis writes them under the bars.</summary>
    public string[] Categories { get; }

    string[]? ICategoryLabeled.CategoryLabels => Categories;

    /// <summary>The signed change of each step, in order. A step named in <see cref="TotalIndices"/> holds a level instead:
    /// the total it reaches.</summary>
    public double[] Values { get; }

    /// <summary>The colour of a step that raises the running total.</summary>
    public Color IncreaseColor { get; set => Change(ref field, value); } = UpDownColors.Standard.Up;

    /// <summary>The colour of a step that lowers the running total.</summary>
    public Color DecreaseColor { get; set => Change(ref field, value); } = UpDownColors.Standard.Down;

    /// <summary>The colour of a total bar, a step named in <see cref="TotalIndices"/>. A series that names none never uses it.</summary>
    public Color TotalColor { get; set => Change(ref field, value); } = CyclePair.Standard.First;

    /// <summary>The width of a bar, as a share of the space for one step.</summary>
    public double BarWidth { get; set => Change(ref field, value); } = 0.6;

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
                    index.ThrowIfNotIndexOf(Values.Length, nameof(value), "A step named by TotalIndices (\"totalIndices\" in JSON)");
                }
            }

            Change(ref _totalIndices, value);
        }
    }

    /// <summary>The level each step is meant to reach, one for each step, as absolute levels on the value axis (the same scale the
    /// bars are drawn on, not a change): a short mark across the bar at that level, drawn over the bar, so the distance between the
    /// bar's end and the mark is how far the step fell short or went past. A missing level (NaN) is a step with no mark;
    /// <see langword="null"/> (the default) is a chart with no targets. The mark has no legend entry. The value axis makes room
    /// for the targets, and the data table gains a <c>target</c> column only when there are any.</summary>
    /// <exception cref="ArgumentException">The array does not hold one level for each step. The list is not kept.</exception>
    /// <example>
    /// <code>
    /// ax.Waterfall(["Q1", "Q2", "Q3"], [100, 30, -20], s => s.Targets = [100, 125, 110]);
    /// </code>
    /// </example>
    public double[]? Targets
    {
        get => _targets;
        set => Change(ref _targets, value.ThrowIfNotOnePer(Values.Length, nameof(Targets)));
    }

    /// <summary>The colour of the target marks; <see langword="null"/> (the default) is the theme's text colour.</summary>
    public Color? TargetColor { get; set => Change(ref field, value); }

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
        var scale = context.YScale.Rule;
        // A level that cannot be placed (from a missing or infinite step on, until a total states one) is not drawn, so
        // it is left out here too.
        var bars = Bars().Aggregate(Range1D.Empty.Including(0, scale), (range, bar) => range.Including(bar.Top, scale));
        // A target is a level on the same axis, so the axis makes room for it; a missing or unplaceable one is left out.
        var y = (_targets ?? []).Aggregate(bars, (range, level) => range.Including(level, scale)).OrNone();
        return new(xMin, xMax, y?.Lo, y?.Hi,
            StickyXMin: xMin, StickyXMax: xMax, StickyYMin: 0);
    }

    static string ISeriesKind<WaterfallSeries>.Kind => "waterfall";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        Categories = Categories, Values = Values,
        IncreaseColor = IncreaseColor, DecreaseColor = DecreaseColor,
        TotalColor = TotalColor, BarWidth = BarWidth,
        // No total marked, or an empty list, writes nothing: a series at rest writes the bytes it always wrote.
        TotalIndices = _totalIndices is { Length: > 0 } ? _totalIndices : null,
        Targets = _targets,
        TargetColor = TargetColor,
    };

    /// <summary>The reader of a <see cref="WaterfallSeries"/> that <see cref="SeriesRegistry"/> takes; it also carries increase/decrease/total colours.</summary>
    internal static WaterfallSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Waterfall(dto.Categories ?? [], dto.Values ?? []);
        if (dto.IncreaseColor is { } increaseColor) s.IncreaseColor = increaseColor;
        if (dto.DecreaseColor is { } decreaseColor) s.DecreaseColor = decreaseColor;
        if (dto.TotalColor is { } totalColor) s.TotalColor = totalColor;
        if (dto.BarWidth is { } barWidth) s.BarWidth = barWidth;

        s.TotalIndices = dto.TotalIndices;
        s.Targets = dto.Targets;
        s.TargetColor = dto.TargetColor;
        return s;
    }


    /// <inheritdoc />
    /// <remarks>The categories as text, then the values under the series' own label, then the level each step was meant to reach
    /// as a <c>target</c> column when the series has targets, and only then.</remarks>
    public override ChartDataTable? ToDataTable() => _targets is { } levels
        ? ChartDataTable.ByCategory(Categories, new NumberColumn(Label ?? "value", Values), new NumberColumn("target", levels))
        : ChartDataTable.ByCategory(Categories, new NumberColumn(Label ?? "value", Values));

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
