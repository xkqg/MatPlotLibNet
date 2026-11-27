// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a treemap series that renders hierarchical data as nested rectangles.</summary>
public sealed class TreemapSeries : HierarchicalSeries, IEdgedPatch, ISeriesKind<TreemapSeries>, IGivenTextSize
{
    /// <summary>Under the library's own themes a tile is outlined by one white pixel.</summary>
    PatchEdge IEdgedPatch.LibraryEdge(Color? fill, Color? themeEdge) => PatchEdge.WhitePixel;

    private const double RestingPadding = 2.0;
    /// <summary>The gap, in pixels, left between neighbouring rectangles. Default <see cref="RestingPadding"/>; 0 makes them touch.</summary>
    public double Padding { get; set => Change(ref field, value); } = RestingPadding;

    private const TreemapLabelFit RestingLabelFit = TreemapLabelFit.Always;
    /// <summary>What to do with a label wider than its rect — see <see cref="TreemapLabelFit"/>.</summary>
    public TreemapLabelFit LabelFit { get; set => Change(ref field, value); } = RestingLabelFit;

    private const double RestingHeadlineFontSize = 20;
    /// <summary>The size, in pixels (the unit of <see cref="Styling.Font.Size"/>), of a leaf's <see cref="TreeNode.Headline"/> — the cell's big number. An interior
    /// node's headline rides its header strip at the label's own size; a strip has no room to shout.</summary>
    public double HeadlineFontSize { get; set => Change(ref field, value); } = RestingHeadlineFontSize;

    /// <summary>Initializes a new instance of <see cref="TreemapSeries"/> with the specified root node.</summary>
    public TreemapSeries(TreeNode root) : base(root) { }

    static string ISeriesKind<TreemapSeries>.Kind => "treemap";

    /// <summary>The size of the name in each tile and in the header of each interior tile, in pixels (the unit of <see cref="Font.Size"/>). Default <see cref="RestingFontSize"/>. Given no size, the name in each tile and in the header of each interior tile
    /// is the size of the theme's text under the matplotlib themes, as matplotlib draws the text it writes beside its data
    /// (<c>font.size</c>), and <see cref="RestingFontSize"/> under every other theme; a size that is given, the default included, is drawn as given.</summary>
    public double FontSize { get => GivenFontSize ?? RestingFontSize; set => GivenFontSize = value; }

    /// <summary>The size a tile's name takes under a theme that names none.</summary>
    private const double RestingFontSize = 12;

    /// <summary>The size that was given, <see langword="null"/> when none was.</summary>
    internal double? GivenFontSize { get; private set => Change(ref field, value); }

    double? IGivenTextSize.GivenTextSize => GivenFontSize;

    double IGivenTextSize.RestingTextSize => RestingFontSize;

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => this.DescribeSharedSettings(new()
    {
        Type = this.WireKind(),
        FontSize = GivenFontSize,
        Padding = Padding.OrNullAtRest(RestingPadding),
        LabelFit = LabelFit.OrNullAtRest(RestingLabelFit),
        HeadlineFontSize = HeadlineFontSize.OrNullAtRest(RestingHeadlineFontSize),
    });

    /// <summary>The reader of a <see cref="TreemapSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static TreemapSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var root = dto.TreeRoot.TreeOrBareRoot();
        var s = axes.Treemap(root);
        if (dto.Padding is { } padding) s.Padding = padding;
        if (dto.LabelFit is { } labelFit) s.LabelFit = labelFit;
        if (dto.HeadlineFontSize is { } headlineFontSize) s.HeadlineFontSize = headlineFontSize;
        s.RestoreSharedSettings(dto);
        if (dto.FontSize is { } fontSize) s.FontSize = fontSize;
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
