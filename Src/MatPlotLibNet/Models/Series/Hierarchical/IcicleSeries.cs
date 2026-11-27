// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>An icicle chart: a hierarchy drawn as rows of cells, one row for each level, the cells of a node laid side by side
/// under it, each as wide as its share of the node's width (the children of a node divide the node's width in proportion to
/// their total values, in the order they are stored, by the rule the sunburst divides an angle by). The root is not drawn, as
/// the sunburst's is not: the first row is the root's children. A node without children is one row tall wherever it stands,
/// so a shallow branch ends where it ends and leaves the rows below it empty.</summary>
/// <remarks>The cells are coloured as a treemap's are: a node's own <see cref="TreeNode.Color"/>, otherwise its
/// <see cref="TreeNode.ColorValue"/> through the series' colour map, otherwise the colour map at the node's place among its
/// siblings. The series fills its plot area and has no axes of its own.</remarks>
public sealed class IcicleSeries : HierarchicalSeries, ISeriesKind<IcicleSeries>, IGivenTextSize
{
    /// <summary>Initializes an icicle chart of the hierarchy under <paramref name="root"/>.</summary>
    /// <param name="root">The root of the hierarchy; it is not drawn, its children are the first row.</param>
    public IcicleSeries(TreeNode root) : base(root) { }

    static string ISeriesKind<IcicleSeries>.Kind => "icicle";

    /// <summary>The size of the name in each cell, in pixels (the unit of <see cref="Font.Size"/>). Default <see cref="RestingFontSize"/>. Given no size, the name in each cell
    /// is the size of the theme's text under the matplotlib themes, as matplotlib draws the text it writes beside its data
    /// (<c>font.size</c>), and <see cref="RestingFontSize"/> under every other theme; a size that is given, the default included, is drawn as given.</summary>
    public double FontSize { get => GivenFontSize ?? RestingFontSize; set => GivenFontSize = value; }

    /// <summary>The size a cell's name takes under a theme that names none.</summary>
    private const double RestingFontSize = 12;

    /// <summary>The size that was given, <see langword="null"/> when none was.</summary>
    internal double? GivenFontSize { get; private set => Change(ref field, value); }

    double? IGivenTextSize.GivenTextSize => GivenFontSize;

    double IGivenTextSize.RestingTextSize => RestingFontSize;

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => this.DescribeSharedSettings(new() { Type = this.WireKind(), FontSize = GivenFontSize });

    /// <summary>The reader of a <see cref="IcicleSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static IcicleSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var root = dto.TreeRoot.TreeOrBareRoot();
        var s = axes.Icicle(root);
        s.RestoreSharedSettings(dto);
        if (dto.FontSize is { } fontSize) s.FontSize = fontSize;
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
