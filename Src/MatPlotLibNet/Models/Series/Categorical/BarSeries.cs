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
public sealed class BarSeries : ChartSeries, ICategoryLabeled, ICategoryPlacement, IStackable, IHasColor, IHasAlpha, IHasEdgeColor, ILabelable,
    IEdgedPatch, IHatched, ISeriesKind<BarSeries>, ILabelFormatted
{
    /// <summary>The format a value label is written in when none is given: up to six significant digits, no trailing zeros, an exponent
    /// only below 1e-4 or from 1e6, its marker lower case (<c>1e+06</c>): matplotlib's <c>bar_label</c> default <c>'%g'</c>, which .NET's
    /// <c>g6</c> writes digit for digit.</summary>
    internal const string DefaultLabelFormat = "g6";

    /// <inheritdoc />
    string ILabelFormatted.DefaultLabelFormat => DefaultLabelFormat;

    /// <inheritdoc />
    /// <remarks>The bars' opacity is the one their fill is drawn at, which replaces the opacity of their colours.</remarks>
    GivenPatchEdge IEdgedPatch.GivenEdge => new(EdgeColor, GivenLineWidth, Alpha);

    /// <summary>Under the library's own themes a bar given a width is outlined in its edge colour, else in its own colour; one given an
    /// edge colour alone is outlined 1 pixel wide; one given neither has no outline.</summary>
    PatchEdge IEdgedPatch.LibraryEdge(Color? fill, Color? themeEdge) =>
        LineWidth > 0 ? new PatchEdge(EdgeColor ?? fill, LineWidth) : new PatchEdge(EdgeColor, EdgeColor.HasValue ? 1 : 0);

    /// <inheritdoc />
    /// <remarks>matplotlib's <c>bar</c> and <c>barh</c>.</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.Patch;

    /// <summary>The label of each bar, in drawing order. With numeric <see cref="XCoordinate"/> positions the labels
    /// only name the bars in tooltips.</summary>
    public string[] Categories { get; }

    /// <inheritdoc />
    string[]? ICategoryLabeled.CategoryLabels => Categories;

    /// <inheritdoc />
    bool ICategoryPlacement.CategoriesAlongY => Orientation.ValuesAlongX;

    /// <summary>The height of each bar, measured from zero or from its entry in <see cref="StackBaseline"/>; a negative
    /// value points the bar the other way and a missing one (NaN) draws no bar.</summary>
    public double[] Values { get; }

    /// <summary>Whether the bars stand up (<see cref="BarOrientation.Vertical"/>, the default) or lie down
    /// (<see cref="BarOrientation.Horizontal"/>); it decides which axis carries the categories and which the
    /// values.</summary>
    public BarOrientation Orientation { get; set => Change(ref field, value); } = BarOrientation.Vertical;

    /// <inheritdoc />
    public Color? Color { get; set => Change(ref field, value); }

    /// <inheritdoc />
    public Color? EdgeColor { get; set => Change(ref field, value); }

    /// <inheritdoc />
    /// <remarks>Edged as the bars are under the matplotlib themes: the edge colour and width given, else the theme's.</remarks>
    internal override LegendSwatch LegendSwatch => LegendSwatch.EdgedPatch;

    /// <summary>How wide each bar is, as a fraction of its category slot (1 makes neighbouring bars touch); on numeric
    /// <see cref="XCoordinate"/> positions, in data units. Default 0.8.</summary>
    public double BarWidth { get; set => Change(ref field, value); } = 0.8;

    /// <summary>Where each bar starts: bar <c>i</c> is drawn from <c>StackBaseline[i]</c> up (down, for a negative
    /// value), the way matplotlib's <c>bottom=</c> lifts a bar. <see langword="null"/> starts every bar at zero. A series with
    /// a foot of its own is a floating bar and is no layer of a stacked chart (<see cref="BarMode.Stacked"/>): the other
    /// series stack among themselves, each time the chart is drawn, and this value stays as it was set, so a Renko brick or an
    /// event on a calendar stands on its own foot under any bar mode.</summary>
    /// <exception cref="ArgumentException">The array does not hold one foot for each bar; the feet the series had are kept.</exception>
    public double[]? StackBaseline { get; set => Change(ref field, value.ThrowIfNotOnePer(Values.Length, nameof(StackBaseline))); }

    /// <inheritdoc />
    public bool ShowLabels { get; set => Change(ref field, value); }

    /// <summary>The .NET format string of each value label (<c>"F0"</c>, <c>"F2"</c>, ...). <see langword="null"/> writes
    /// a value with up to six significant digits and no trailing zeros, as matplotlib's <c>bar_label</c> does by default
    /// (<c>'%g'</c>): 12500 reads <c>12500</c>, and only a value below 1e-4 or from 1e6 takes an exponent.</summary>
    public string? LabelFormat { get; set => Change(ref field, value); }

    /// <inheritdoc />
    public double Alpha { get; set => Change(ref field, value.ThrowIfNotOpacity(nameof(Alpha))); } = RestingAlpha;

    /// <summary>Width of the outline around each bar, in pixels; reads 0 when none was given. Under the library's own themes a bar
    /// given no width draws no outline unless <see cref="EdgeColor"/> is set, which then draws a 1 pixel outline, and a positive
    /// width without an edge colour outlines the bars in the series colour. Under the matplotlib themes the outline is matplotlib's:
    /// the edge colour given, else black under the classic theme (<c>patch.force_edgecolor</c>) and none under v2, as wide as given,
    /// else 1 pt; a width of 0 draws none.</summary>
    public double LineWidth { get => GivenLineWidth ?? RestingLineWidth; set => GivenLineWidth = value; }

    /// <summary>The outline width that was given, <see langword="null"/> when none was and the theme decides.</summary>
    internal double? GivenLineWidth { get; private set => Change(ref field, value); }

    /// <summary>Whether each bar is centred in its category slot (the default, <see cref="RestingAlign"/>) or
    /// starts at the slot's left edge (<see cref="BarAlignment.Edge"/>).</summary>
    public BarAlignment Align { get; set => Change(ref field, value); } = RestingAlign;

    /// <summary>The pattern laid over the fill of each bar; <see cref="HatchPattern.None"/>, the default, leaves the
    /// bars plain.</summary>
    public HatchPattern Hatch { get; set => Change(ref field, value); } = HatchPattern.None;

    /// <summary>The colour of the hatch lines. <see langword="null"/> draws them as the theme does: in the colour of the bar's edge, black
    /// when it has none, under v2, black under classic (matplotlib's <c>hatch.color</c>), and under the library's own themes in the
    /// <see cref="EdgeColor"/> given, else in a colour that contrasts with the fill. A colour given takes the bar's opacity.</summary>
    public Color? HatchColor { get; set => Change(ref field, value); }

    /// <summary>The colour the fill blends to at the tip of each bar; <see langword="null"/> (the default) is a flat fill. The
    /// fill of every bar runs in a straight line from the series colour at the bar's foot to this colour at its end, which is
    /// the top of a bar that points up and the bottom of one that points down, whichever way the axis runs. A bar series'
    /// <see cref="Alpha"/> applies to both colours, and a hatch is laid over the gradient. Every backend (SVG, Skia, Maui) draws the
    /// gradient.</summary>
    public Color? GradientColor { get; set => Change(ref field, value); }

    private const double RestingAlpha = 1.0;
    private const double RestingLineWidth = 0.0;
    private const BarAlignment RestingAlign = BarAlignment.Center;

    /// <summary>How far, in axis units along the category axis, a bar steps aside within its slot when several bar series
    /// share the categories (render state set by <see cref="BarArrangementExtensions.ArrangeBars"/>, not by the user).</summary>
    internal double BarGroupOffset { get; set; }

    /// <summary>The bar's width while it stands in a group (render state set by
    /// <see cref="BarArrangementExtensions.ArrangeBars"/>, not by the user).</summary>
    internal double? BarGroupWidth { get; set; }

    /// <summary>Where each bar starts in a stacked render: the sum of the stackable series added before this one.
    /// Render state, worked out afresh by <see cref="BarArrangementExtensions.ArrangeBars"/> on every render of a stacked
    /// axes and cleared on every other render; never serialised and never written into <see cref="StackBaseline"/>.</summary>
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
    /// <exception cref="ArgumentException">The array does not hold one position for each bar.</exception>
    public double[]? XCoordinate { get; init => field = value.ThrowIfNotOnePer(Values.Length, nameof(XCoordinate)); }

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
        x.ThrowIfMisaligned(values);
        Categories = new string[x.Length];
        for (int i = 0; i < x.Length; i++)
            Categories[i] = x[i].InvariantText();
        Values = values;
        XCoordinate = x;
    }

    /// <inheritdoc />
    /// <remarks>Measured as upright bars measure it, the categories' extent as X and the values as Y, and put on the axes
    /// the <see cref="Orientation"/> stands the bars on: bars that lie
    /// down range their categories along Y and their values along X.</remarks>
    public override DataRangeContribution ComputeDataRange(IAxesContext context) =>
        UprightRange(context).OnAxes(Orientation);

    /// <summary>The range of the bars as if they stood upright: X along the categories, Y along the values.</summary>
    private DataRangeContribution UprightRange(IAxesContext context)
    {
        // Empty bars contribute no range. Without this guard, Values.Max()/Min() throw.
        if (Values.Length == 0) return default;
        // Report the union of actual bar EDGES, not the slot indices [0, N].
        // matplotlib contributes [first_bar_left, last_bar_right] to the data limits; so should we (no sticky edge on them).
        // This removes ~14 px of extra whitespace on each side of the bar group
        // (matches matplotlib's `axes.bar(...)` contribution to `DataLim`).
        // The edges are where the renderer draws the bars, read from the same rule (CategorySlot), so an edge-aligned bar
        // is made room for as it is drawn. Ungrouped: a group's bars stay inside the width of one bar.
        // A limit set on the axis that carries the categories (Y for bars that lie down) bounds their extent.
        bool alongY = Orientation.ValuesAlongX;
        double? limitMin = context.LimitMinAlong(isX: !alongY);
        double? limitMax = context.LimitMaxAlong(isX: !alongY);
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
        if (StackBaseline is { } bottoms)
        {
            return RangeOnBottoms(bottoms, xMin, xMax, scale);
        }

        // Every edge the bars draw: the zero they stand on and the end each reaches; stacked, the end of every layer in
        // every column of the stack the context keeps (StackableExtensions, the rule the bars stand by), so a layer that
        // falls is in range too. A missing bar (NaN) and an infinity are left out, as the renderer leaves out the bar, and so
        // is an edge the value axis cannot place: on a log axis, which has no place for zero and clips the feet to the edge
        // where it begins, the range starts at the lowest end, as matplotlib's does (the smallest positive value, its minpos).
        var reached = context.BarMode == BarMode.Stacked ? context.BarStack.Ends : [Values];
        if (reached.DrawableRange(scale) is not { } ends)
        {
            return new(xMin, xMax, null, null);
        }

        var y = ends.Including(0, scale);

        // Sticky edges: matplotlib's Axes.bar registers each bar's foot and nothing else (`r.sticky_edges.y.append(b)` for a bar that
        // rises, `.x.append(l)` for one that lies down), so the margin never pads past the zero the bars stand on, below bars that
        // rise and above bars that all fall, and pads the categories on both sides.
        return new(xMin, xMax, y.Lo, y.Hi, StickyYMin: 0, StickyYMax: 0);
    }

    /// <summary>The range of bars that start on <paramref name="bottoms"/>: from the lowest foot or end to the highest,
    /// over the ones the value axis can place. The lowest foot is the sticky floor, as zero is for bars that start at zero
    /// (matplotlib makes every bar's foot sticky, and a margin stops at the foot nearest below the data). A bar whose end
    /// the axis cannot place is not drawn, so its foot is left out too; a foot it cannot place (0 on a log axis) is drawn
    /// at the plot's edge and takes no part.</summary>
    private DataRangeContribution RangeOnBottoms(double[] bottoms, double? xMin, double? xMax, ScaleRule scale)
    {
        var feet = new double[Values.Length];
        var ends = new double[Values.Length];
        for (int i = 0; i < Values.Length; i++)
        {
            ends[i] = bottoms[i] + Values[i];
            feet[i] = ends[i].IsPlaceable(scale) ? bottoms[i] : double.NaN;
        }

        var y = new[] { feet, ends }.DrawableRange(scale);
        return new(xMin, xMax, y?.Lo, y?.Hi, StickyYMin: feet.DrawableRange(scale)?.Lo);
    }

    static string ISeriesKind<BarSeries>.Kind => "bar";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        Categories = Categories, Values = Values, Color = Color,
        Orientation = Orientation.ToWireName(),
        BarWidth = BarWidth,
        // Null when unhatched, so an unhatched bar emits no hatch bytes and its golden stays byte-identical.
        Hatching = this.Hatching,
        GradientColor = GradientColor,
        EdgeColor = EdgeColor,
        LineWidth = GivenLineWidth,
        Alpha = Alpha.OrNullAtRest(RestingAlpha),
        Align = Align.OrNullAtRest(RestingAlign),
        ShowLabels = ShowLabels.OrNullAtRest(false),
        LabelFormat = LabelFormat,
        StackBaseline = StackBaseline,
        XCoordinate = XCoordinate,
    };

    /// <summary>The reader of a <see cref="BarSeries"/> that <see cref="SeriesRegistry"/> takes; it also carries orientation.</summary>
    /// <exception cref="ArgumentException">The numeric x positions do not pair up with the values.</exception>
    internal static BarSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        string[] categories = dto.Categories ?? [];
        double[] values = dto.Values ?? [];
        var s = dto.XCoordinate is { } x ? axes.AddSeries(OnNumericX(categories, values, x)) : axes.Bar(categories, values);
        s.Color = dto.Color;
        s.EdgeColor = dto.EdgeColor;
        if (dto.LineWidth is { } lineWidth) s.LineWidth = lineWidth;
        if (dto.Alpha is { } alpha) s.Alpha = alpha;
        if (dto.Align is { } align) s.Align = align;
        if (dto.ShowLabels is { } showLabels) s.ShowLabels = showLabels;
        s.LabelFormat = dto.LabelFormat;
        s.StackBaseline = dto.StackBaseline;
        if (dto.BarWidth is { } barWidth) s.BarWidth = barWidth;

        s.LayHatching(dto.Hatching);
        s.GradientColor = dto.GradientColor;
        if (dto.Orientation.WireEnum<BarOrientation>() is { } orientation) s.Orientation = orientation;
        return s;
    }

    /// <summary>A bar series on the numeric x positions <paramref name="x"/> that keeps the categories it was written
    /// with (they label the bars' tooltips).</summary>
    /// <exception cref="ArgumentException">There is not one category, one value and one x position per bar.</exception>
    private static BarSeries OnNumericX(string[] categories, double[] values, double[] x)
    {
        categories.Length.ThrowIfMisaligned(values.Length, nameof(categories));
        // XCoordinate refuses positions that are not one per bar itself.
        return new BarSeries(categories, values) { XCoordinate = x };
    }


    /// <inheritdoc />
    /// <remarks>The categories as text, then the values under the series' own label. A bar with a foot of its own
    /// (<see cref="StackBaseline"/>) is an interval, so its table is the category and the two ends of the interval,
    /// <c>from</c> (the foot, one for each bar: the setter refuses a list of another length) and <c>to</c> (the foot plus the value).</remarks>
    public override ChartDataTable? ToDataTable() => StackBaseline is { } feet
        ? ChartDataTable.ByCategory(Categories, new NumberColumn("from", feet), new NumberColumn("to", [.. feet.Zip(Values, static (foot, value) => foot + value)]))
        : ChartDataTable.ByCategory(Categories, new NumberColumn(Label ?? "value", Values));

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
