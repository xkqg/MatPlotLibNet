// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Specifies the orientation of a bar chart.</summary>
/// <remarks><b>Append-only ordinal contract:</b> never reorder,
/// remove, or renumber.</remarks>
public enum BarOrientation
{
    /// <summary>Bars are drawn vertically.</summary>
    Vertical = 0,

    /// <summary>Bars are drawn horizontally.</summary>
    Horizontal = 1,
}

/// <summary>Represents a bar chart series displaying categorical data as rectangular bars.</summary>
public sealed class BarSeries : ChartSeries, ICategoryLabeled, IStackable, IHasColor, IHasAlpha, IHasEdgeColor, ILabelable,
    IHasBarOrientation
{
    public string[] Categories { get; }

    /// <inheritdoc />
    string[]? ICategoryLabeled.CategoryLabels => Categories;

    public double[] Values { get; }

    public BarOrientation Orientation { get; set; } = BarOrientation.Vertical;

    public Color? Color { get; set; }

    public Color? EdgeColor { get; set; }

    public double BarWidth { get; set; } = 0.8;

    /// <summary>Where each bar starts: bar <c>i</c> is drawn from <c>StackBaseline[i]</c> up (down, for a negative
    /// value), the way matplotlib's <c>bottom=</c> lifts a bar. <see langword="null"/> starts every bar at zero. A
    /// stacked chart (<see cref="BarMode.Stacked"/>) works out its own stacking each time it is drawn and leaves this
    /// value as it was set.</summary>
    public double[]? StackBaseline { get; set; }

    public bool ShowLabels { get; set; }

    /// <summary>The .NET format string of each value label (<c>"F0"</c>, <c>"F2"</c>, ...). <see langword="null"/> writes
    /// a value with up to six significant digits and no trailing zeros, as matplotlib's <c>bar_label</c> does by default
    /// (<c>'%g'</c>): 12500 reads <c>12500</c>, and only a value below 1e-4 or from 1e6 takes an exponent.</summary>
    public string? LabelFormat { get; set; }

    public double Alpha { get; set; } = RestingAlpha;

    public double LineWidth { get; set; } = RestingLineWidth;

    public BarAlignment Align { get; set; } = RestingAlign;

    public HatchPattern Hatch { get; set; } = HatchPattern.None;

    public Color? HatchColor { get; set; }

    private const double RestingAlpha = 1.0;
    private const double RestingLineWidth = 0.0;
    private const BarAlignment RestingAlign = BarAlignment.Center;

    /// <summary>How far, in axis units along the category axis, a bar steps aside within its slot when several bar series
    /// share the categories (set by the renderer, not the user).</summary>
    internal double BarGroupOffset { get; set; }

    /// <summary>Override for bar width when rendering as part of a group (set by renderer, not user).</summary>
    internal double? BarGroupWidth { get; set; }

    /// <summary>Where each bar starts in a stacked render: the sum of the stackable series added before this one.
    /// Render state, worked out afresh on every render of a stacked axes and cleared on every other render; never
    /// serialised and never written into <see cref="StackBaseline"/>.</summary>
    internal double[]? StackedBottoms { get; set; }

    /// <summary>Where each bar starts in the current render: the stack below it in a stacked render, otherwise the
    /// <see cref="StackBaseline"/> the user set, otherwise <see langword="null"/> (every bar at zero). The renderer
    /// draws from it and the side-by-side grouping leaves out every bar that has one.</summary>
    internal double[]? Bottoms => StackedBottoms ?? StackBaseline;

    /// <summary>Where bar <paramref name="index"/> stands along the category axis in the current render: around its x when
    /// the series has numeric <see cref="XCoordinate"/> positions, otherwise in its category's slot, aligned as
    /// <see cref="Align"/> says; as wide, and moved as far, as its group makes it (<see cref="BarGroupWidth"/>,
    /// <see cref="BarGroupOffset"/>). The renderer draws the bar here; the rule itself is <see cref="CategorySlot"/>'s.</summary>
    /// <param name="index">The bar's index in <see cref="Values"/>.</param>
    /// <returns>The stretch of the axis the bar covers, and its centre.</returns>
    internal CategorySlot SlotOf(int index)
    {
        double width = BarGroupWidth ?? BarWidth;
        return XCoordinate is { Length: > 0 } x
            ? CategorySlot.Around(x[index] + BarGroupOffset, width)
            : CategorySlot.Of(index).Bar(width, BarGroupOffset, Align);
    }

    /// <summary>
    /// Optional numeric positions for bars along the axis that carries the categories (X for upright bars, Y for bars
    /// that lie down), enabling bars on a continuous axis instead of categorical slot indices. Matches matplotlib's
    /// <c>ax.bar(x, heights)</c> where <c>x</c> is a numeric array. When set, <see cref="Categories"/> is used only for
    /// tooltip labels and <c>BarSeriesRenderer</c> centres each bar on its position, <see cref="BarWidth"/> wide. Used
    /// e.g. by the MACD histogram so it shares a numeric X axis with the MACD and signal line series drawn in the same
    /// panel.
    /// </summary>
    public double[]? XCoordinate { get; init; }

    /// <summary>Initializes a new instance of <see cref="BarSeries"/> with the specified categories and values.</summary>
    /// <param name="categories">The category labels for each bar.</param>
    /// <param name="values">The numeric values for each bar.</param>
    public BarSeries(string[] categories, double[] values)
    {
        Categories = categories;
        Values = values;
    }

    /// <summary>Initializes a bar series with numeric X positions — bars render on a continuous
    /// X axis instead of categorical slot indices. Categories are auto-generated as numeric
    /// strings so <see cref="ICategoryLabeled"/> still works for mixed usage.</summary>
    public BarSeries(double[] x, double[] values)
    {
        if (x.Length != values.Length)
            throw new ArgumentException($"x length ({x.Length}) must match values length ({values.Length}).");
        Categories = new string[x.Length];
        for (int i = 0; i < x.Length; i++)
            Categories[i] = x[i].ToString(System.Globalization.CultureInfo.InvariantCulture);
        Values = values;
        XCoordinate = x;
    }

    /// <inheritdoc />
    /// <remarks>Measured as upright bars measure it, the categories' extent as X and the values as Y, and put on the axes
    /// the <see cref="Orientation"/> stands the bars on (<see cref="BarOrientationExtensions.OnAxes"/>): bars that lie
    /// down range their categories along Y and their values along X.</remarks>
    public override DataRangeContribution ComputeDataRange(IAxesContext context) =>
        UprightRange(context).OnAxes(Orientation);

    /// <summary>The range of the bars as if they stood upright: X along the categories, Y along the values.</summary>
    private DataRangeContribution UprightRange(IAxesContext context)
    {
        // Empty bars contribute no range. Without this guard, Values.Max()/Min() throw.
        if (Values.Length == 0) return new DataRangeContribution(null, null, null, null);
        // Report the union of actual bar EDGES, not the slot indices [0, N].
        // matplotlib contributes [first_bar_left, last_bar_right]; so should we.
        // This removes ~14 px of extra whitespace on each side of the bar group
        // (matches matplotlib's `axes.bar(...)` contribution to `DataLim`).
        // The edges are where the renderer draws the bars, read from the same rule (CategorySlot), so an edge-aligned bar
        // is made room for as it is drawn. Ungrouped: a group's bars stay inside the width of one bar.
        // A limit set on the axis that carries the categories (Y for bars that lie down) bounds their extent.
        bool alongY = Orientation.CategoriesAlongY();
        double? limitMin = alongY ? context.YAxisMin : context.XAxisMin;
        double? limitMax = alongY ? context.YAxisMax : context.XAxisMax;
        double? xMin, xMax;
        if (XCoordinate is { Length: > 0 } xc)
        {
            // A bar at an x that cannot be placed is not drawn, so it is left out of the extent too.
            var x = xc.DrawableRange();
            xMin = limitMin ?? (x is { } first ? CategorySlot.Around(first.Lo, BarWidth).Left : (double?)null);
            xMax = limitMax ?? (x is { } last ? CategorySlot.Around(last.Hi, BarWidth).Right : (double?)null);
        }
        else
        {
            xMin = limitMin ?? CategorySlot.Of(0).Bar(BarWidth, 0, Align).Left;
            xMax = limitMax ?? CategorySlot.Of(Categories.Length - 1).Bar(BarWidth, 0, Align).Right;
        }

        var scale = Orientation.ValueScale(context);
        if (context.BarMode != BarMode.Stacked && StackBaseline is { } bottoms)
        {
            return RangeOnBottoms(bottoms, xMin, xMax, scale);
        }

        // Every edge the bars draw: the zero they stand on and the end each reaches; stacked, the end of every layer in
        // every column (StackableExtensions, the rule the renderer stacks by), so a layer that falls is in range too. A
        // missing bar (NaN) and an infinity are left out, as the renderer leaves out the bar, and so is an edge the value
        // axis cannot place: on a log axis, which has no place for zero and clips the feet to the edge where it begins,
        // the range starts at the lowest end, as matplotlib's does (the smallest positive value, its minpos).
        var reached = context.BarMode == BarMode.Stacked ? context.AllSeries.OfType<IStackable>().ToList().StackedEnds() : [Values];
        if (reached.DrawableRange(scale) is not { } ends)
        {
            return new(xMin, xMax, null, null, StickyXMin: xMin, StickyXMax: xMax);
        }

        var y = ends.Including(0, scale);

        // Sticky edges: matplotlib's BarContainer registers each bar's foot, `sticky_edges.y = [0]`, so the margin never
        // pads past the zero the bars stand on: below bars that rise, above bars that all fall.
        return new(xMin, xMax, y.Lo, y.Hi, StickyXMin: xMin, StickyXMax: xMax, StickyYMin: 0, StickyYMax: 0);
    }

    /// <summary>The range of bars that start on <paramref name="bottoms"/>: from the lowest foot or end to the highest,
    /// over the ones the value axis can place. The lowest foot is the sticky floor, as zero is for bars that start at zero
    /// (matplotlib makes every bar's foot sticky, and a margin stops at the foot nearest below the data). A bar whose end
    /// the axis cannot place is not drawn, so its foot is left out too; a foot it cannot place (0 on a log axis) is drawn
    /// at the plot's edge and takes no part.</summary>
    private DataRangeContribution RangeOnBottoms(double[] bottoms, double? xMin, double? xMax, AxisScale scale)
    {
        var feet = new double[Values.Length];
        var ends = new double[Values.Length];
        for (int i = 0; i < Values.Length; i++)
        {
            ends[i] = bottoms[i] + Values[i];
            feet[i] = ends[i].IsPlaceable(scale) ? bottoms[i] : double.NaN;
        }

        var y = new[] { feet, ends }.DrawableRange(scale);
        return new(xMin, xMax, y?.Lo, y?.Hi, StickyXMin: xMin, StickyXMax: xMax, StickyYMin: feet.DrawableRange(scale)?.Lo);
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "bar",
        Categories = Categories, Values = Values, Color = Color,
        Orientation = Orientation.ToString().ToLowerInvariant(),
        BarWidth = BarWidth,
        // Null when unhatched, so an unhatched bar emits no hatch bytes and its golden stays byte-identical.
        Hatch = Hatch != HatchPattern.None ? Hatch : null,
        HatchColor = HatchColor,
        EdgeColor = EdgeColor,
        LineWidth = LineWidth.OrNullAtRest(RestingLineWidth),
        Alpha = Alpha.OrNullAtRest(RestingAlpha),
        Align = Align.OrNullAtRest(RestingAlign),
        ShowLabels = ShowLabels.OrNullAtRest(false),
        LabelFormat = LabelFormat,
        StackBaseline = StackBaseline,
        XCoordinate = XCoordinate,
    };

    /// <summary>Reconstructs a <see cref="BarSeries"/> from its serialization DTO, including orientation, and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    /// <exception cref="ArgumentException">The numeric x positions do not pair up with the values.</exception>
    internal static BarSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        string[] categories = dto.Categories ?? [];
        double[] values = dto.Values ?? [];
        var s = dto.XCoordinate is { } x ? axes.AddSeries(OnNumericX(categories, values, x)) : axes.Bar(categories, values);
        s.Color = dto.Color;
        s.EdgeColor = dto.EdgeColor;
        s.LineWidth = dto.LineWidth ?? RestingLineWidth;
        s.Alpha = dto.Alpha ?? RestingAlpha;
        s.Align = dto.Align ?? RestingAlign;
        s.ShowLabels = dto.ShowLabels == true;
        s.LabelFormat = dto.LabelFormat;
        s.StackBaseline = dto.StackBaseline;
        if (dto.BarWidth.HasValue)
        {
            s.BarWidth = dto.BarWidth.Value;
        }

        if (dto.Hatch.HasValue)
        {
            s.Hatch = dto.Hatch.Value;
        }

        s.HatchColor = dto.HatchColor;
        dto.Orientation.ApplyEnum<BarOrientation>(v => s.Orientation = v);
        return s;
    }

    /// <summary>A bar series on the numeric x positions <paramref name="x"/> that keeps the categories it was written
    /// with (they label the bars' tooltips).</summary>
    /// <exception cref="ArgumentException">There is not one category, one value and one x position per bar.</exception>
    private static BarSeries OnNumericX(string[] categories, double[] values, double[] x) =>
        categories.Length == values.Length && x.Length == values.Length
            ? new BarSeries(categories, values) { XCoordinate = x }
            : throw new ArgumentException(
                $"A bar series on numeric x positions needs one category, one value and one xCoordinate per bar; got {categories.Length}, {values.Length} and {x.Length}.");


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
