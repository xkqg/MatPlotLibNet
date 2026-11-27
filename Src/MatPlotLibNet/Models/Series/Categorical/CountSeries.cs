// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a count plot series that draws a bar chart from the frequency of raw categorical values.</summary>
public sealed class CountSeries : ChartSeries, ICategoryLabeled, ICategoryPlacement, IHasColor, IEdgedPatch, INamesValueAxis, ISaturatedFill,
    ISeriesKind<CountSeries>
{
    /// <inheritdoc />
    /// <remarks>seaborn's <c>countplot(saturation=.75)</c> under the matplotlib themes (<see cref="SeabornVerbLook.CountSaturation"/>).</remarks>
    double ISaturatedFill.SaturationUnder(Theme theme) => theme.Looks.Verbs.CountSaturation;

    /// <inheritdoc />
    /// <remarks>seaborn's <c>countplot</c> names its value axis 'count'.</remarks>
    string INamesValueAxis.ValueAxisName => "count";

    /// <inheritdoc />
    bool INamesValueAxis.ValuesAlongX => Orientation.ValuesAlongX;

    /// <inheritdoc />
    /// <remarks>seaborn's <c>countplot</c> asks the axes once, through a scout <c>bar</c>.</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.Patch;

    /// <inheritdoc />
    internal override LegendSwatch LegendSwatch => LegendSwatch.EdgedPatch;

    /// <summary>The raw categorical observations: each distinct value becomes one bar, as high as the number of times
    /// it occurs.</summary>
    public string[] Values { get; }

    /// <inheritdoc />
    string[]? ICategoryLabeled.CategoryLabels => Values.Distinct().ToArray();

    /// <inheritdoc />
    bool ICategoryPlacement.CategoriesAlongY => Orientation.ValuesAlongX;

    /// <inheritdoc />
    public Color? Color { get; set => Change(ref field, value); }

    /// <summary>Whether the bars stand up (<see cref="BarOrientation.Vertical"/>, the default) or lie down
    /// (<see cref="BarOrientation.Horizontal"/>); it decides which axis carries the categories and which the
    /// counts.</summary>
    public BarOrientation Orientation { get; set => Change(ref field, value); } = BarOrientation.Vertical;

    /// <summary>How wide each bar is, as a fraction of its category slot (1 makes neighbouring bars touch). Default
    /// 0.8.</summary>
    public double BarWidth { get; set => Change(ref field, value); } = 0.8;

    /// <summary>Initializes a new instance of <see cref="CountSeries"/> with the specified raw values.</summary>
    /// <param name="values">The raw categorical values to count.</param>
    public CountSeries(string[] values)
    {
        Values = values;
    }

    /// <summary>Computes the frequency counts grouped by distinct category.</summary>
    public Dictionary<string, int> ComputeCounts()
        => Values.GroupBy(v => v).ToDictionary(g => g.Key, g => g.Count());

    /// <inheritdoc />
    internal override bool HoldsNoData => Values.Length == 0;

    /// <inheritdoc />
    /// <remarks>seaborn's <c>countplot</c> returns before it touches the axes when it is handed no data: they keep (0, 1).</remarks>
    internal override bool AsksWhenEmpty => false;

    /// <inheritdoc />
    /// <remarks>Measured as upright bars measure it and put on the axes the <see cref="Orientation"/> stands the bars on:
    /// a count plot that lies down ranges its categories along Y and its
    /// counts along X.</remarks>
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        if (HoldsNoData) return NoDataUnderTheLibrary;
        double[] counts = [.. Values.GroupBy(v => v).Select(g => (double)g.Count())];
        // The counts the bars reach and the zero they stand on, over what the value axis can place: on a log axis, which
        // has no place for zero, from the lowest count. Every category holds at least one value, so the counts have a range.
        var scale = Orientation.ValueScale(context);
        var y = counts.DrawableRange(scale)!.Value.Including(0, scale);
        // The slots of the categories, sticky at both ends: the bars stand centred in them (CategorySlot). The zero the bars stand on is
        // sticky too, as every bar's foot is (Axes.bar: r.sticky_edges.y.append(0), BarSeries), so no margin opens below them.
        double first = CategorySlot.Of(0).Left, last = CategorySlot.Of(counts.Length - 1).Right;
        return new DataRangeContribution(first, last, y.Lo, y.Hi, StickyXMin: first, StickyXMax: last, StickyYMin: 0).OnAxes(Orientation);
    }

    static string ISeriesKind<CountSeries>.Kind => "count";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        Categories = Values,
        Color = Color,
        Orientation = Orientation.ToWireName(),
        BarWidth = BarWidth
    };

    /// <summary>The reader of a <see cref="CountSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static CountSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Countplot(dto.Categories ?? []);
        if (dto.Color is { } color) s.Color = color;
        if (dto.BarWidth is { } barWidth) s.BarWidth = barWidth;
        if (dto.Orientation.WireEnum<BarOrientation>() is { } orientation) s.Orientation = orientation;
        return s;
    }


    /// <inheritdoc />
    /// <remarks>What the chart draws is the COUNT per distinct value, so that is what the table says - the same
    /// reduction the renderer makes, not the raw sample list.</remarks>
    public override ChartDataTable? ToDataTable()
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var value in Values)
        {
            counts.TryGetValue(value, out int seen);
            counts[value] = seen + 1;
        }

        var rows = new List<IReadOnlyList<DataCell>>(counts.Count);
        foreach (var pair in counts)
        {
            rows.Add([DataCell.FromText(pair.Key), DataCell.FromNumber(pair.Value)]);
        }

        return new ChartDataTable(null, [new("category", DataColumnKind.Text), new(Label ?? "count")], rows);
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
