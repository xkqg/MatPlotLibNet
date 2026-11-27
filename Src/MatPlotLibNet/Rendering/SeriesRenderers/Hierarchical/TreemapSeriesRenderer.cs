// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;
using System.Globalization;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="TreemapSeries"/>: each node as a rectangle, its children laid out inside it by
/// <see cref="SquarifiedLayout"/>. Leaves whose nodes have no explicit colour are coloured by their index through the series
/// colormap (default viridis), so flat trees don't render as a single-colour block.</summary>
internal sealed class TreemapSeriesRenderer : SeriesRenderer<TreemapSeries>
{
    /// <inheritdoc />
    public TreemapSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(TreemapSeries series)
    {
        var bounds = Context.Area.PlotBounds;
        _scale = series.ColorScaleUnder(Context.Theme);
        _edges = Context.Theme.PatchEdgesOf(series);
        // An EMPTY tree draws nothing. A childless root used to render as one full-area leaf — "one healthy
        // thing fills the fleet" on a wall that in fact knows nothing (measured 2026-08-30).
        if (series.Root.Children.Count == 0)
        {
            return;
        }
        // squarify's tiles are bars, which matplotlib clips to the axes (the half of an edge beyond the frame is cut off): the series pass
        // cuts them off there, as every artist an axes adds, and their labels stand whole.
        RenderNode(series.Root, bounds, series,
            depth: 0, indexInParent: 0, siblingCount: 1, nodeId: "0", parentId: "");
    }

    /// <summary>The colour scale of this render (the series' <c>ColorScaleUnder</c>), resolved once in <see cref="Render"/>.</summary>
    private ColormapContext _scale;

    /// <summary>What the theme decides about the edge of a tile, resolved once in <see cref="Render"/>: a tile asks it, not the theme.</summary>
    private ResolvedPatchEdge _edges;

    /// <summary>
    /// Renders one node and recurses into its children. <paramref name="nodeId"/> is a
    /// deterministic path-based identifier (root = "0", first child of root = "0.0", etc.)
    /// used by <c>SvgTransform.TreemapDrilldownScript</c> to navigate the hierarchy on click. Path IDs
    /// are emitted as <c>data-treemap-*</c> attributes on every rect regardless of whether
    /// interactivity is enabled — the cost is 3 attributes per rect (~60 bytes each) and the
    /// drilldown JS is only loaded when <see cref="Models.Figure.EnableTreemapDrilldown"/>
    /// is set, so non-interactive renders carry harmless unused attributes.
    /// </summary>
    private void RenderNode(TreeNode node, Rect bounds, TreemapSeries series,
        int depth, int indexInParent, int siblingCount,
        string nodeId, string parentId)
    {
        // Phase W (v1.7.2, 2026-04-19): constant readable font at every depth.
        // Children paint OVER parents (RenderNode recurses LAST per-parent, lines 124-129
        // — Shneiderman z-order), so the visible label in any region is the deepest
        // visible one. No depth-shrink, no rect-fit hide gate (overflow is fine). Users
        // navigate overflowing labels via WithBrowserInteraction's pan/zoom, or call
        // FigureBuilder.WithAutoSize(root) for a canvas big enough to avoid overflow.
        // "When no browserInteraction he sees all" — every label emits unconditionally
        // so the static SVG carries the full hierarchy in the DOM (selectable, screen-
        // reader accessible, zoomable via OS controls).
        double fontSize = series.TextSizeUnder(Context.Theme);

        // A node that LEADS somewhere is one anchor around everything it draws — its rect, its text and, for an
        // interior node, the whole subtree nested inside it.
        bool linked = !string.IsNullOrEmpty(node.Url);
        if (linked)
        {
            Ctx.BeginHyperlink(node.Url!, node.Label, node.Expanded);
        }
        try
        {
            RenderNodeCore(node, bounds, series, depth, indexInParent, siblingCount, nodeId, parentId, fontSize);
        }
        finally
        {
            if (linked)
            {
                Ctx.EndHyperlink();
            }
        }
    }

    private void RenderNodeCore(TreeNode node, Rect bounds, TreemapSeries series,
        int depth, int indexInParent, int siblingCount,
        string nodeId, string parentId, double fontSize)
    {
        if (node.Children.Count == 0)
        {
            // Leaf: its own colour, else its value through the scale, else the map at its place among its siblings.
            var color = HierarchicalSeries.ColourOf(node, _scale, indexInParent, siblingCount);
            TagTreemapNode(Ctx, nodeId, depth, parentId, node.Label ?? string.Empty);
            Ctx.DrawRectangleOnPixelGrid(bounds, TileStyle(color, node.Hatching));
            var cellLabel = CellLabel.Standard;
            if (series.ShowLabels && !string.IsNullOrEmpty(node.Label))
            {
                var font = Context.Theme.DataTextFontOn(color, fontSize, GroundInk.Readable);
                var label = FitLabel(node.Label, font, cellLabel.RoomIn(bounds), series.LabelFit);
                var name = cellLabel.Name(bounds);
                var nameExtent = label is null ? default : Ctx.ExtentOf(TextLabel.Of(label), font);
                if (label is not null)
                {
                    TagTreemapNode(Ctx, nodeId, depth, parentId);
                    Ctx.Draw(TextLabel.Of(label), font, name, nameExtent);
                }
                // The MEASURE, under the name and larger — the stat tile's anatomy inside the cell. It is the
                // FIRST thing dropped when the cell cannot hold both: a number without its subject says
                // nothing, so a cell whose name did not fit never shows a bare number either.
                if (label is not null && !string.IsNullOrEmpty(node.Headline))
                {
                    var headlineFont = Context.Theme.DataTextFontOn(color, series.HeadlineFontSize, GroundInk.Readable, FontWeight.Bold);
                    var measure = cellLabel.MeasureUnder(bounds, new TextBox(name, nameExtent).Baseline.Y);
                    var measureExtent = Ctx.ExtentOf(TextLabel.Of(node.Headline), headlineFont);
                    if (cellLabel.MeasureFits(bounds, new TextBox(measure, measureExtent).Baseline.Y) && measureExtent.Width <= cellLabel.RoomIn(bounds))
                    {
                        Ctx.Draw(TextLabel.Of(node.Headline), headlineFont, measure, measureExtent);
                    }
                }
            }
            return;
        }

        // Interior node: draw the parent's colour as a BORDER/frame around its children
        // (Shneiderman-style nested treemap, same layout as the d3 `flare.json` demo).
        // Reserve a header strip at the top for the parent label and a thin padding on
        // the remaining sides — children are squarified into the REDUCED bounds so the
        // parent colour is visible as a frame. Pre-fix the parent rect was overlaid by
        // children edge-to-edge, so the parent label collided with the first child's
        // label and the hierarchy was invisible.
        var interiorColor = HierarchicalSeries.ColourOf(node, _scale, indexInParent, siblingCount);
        TagTreemapNode(Ctx, nodeId, depth, parentId, node.Label ?? string.Empty);
        Ctx.DrawRectangleOnPixelGrid(bounds, TileStyle(interiorColor));

        // Header strip: constant height (paired with the constant 12 pt font, Phase W).
        // Pre-W the header shrank with the depth-driven font; post-W it's fixed at 18 px
        // so nested parents share a single visual rhythm. The interior label emits
        // unconditionally — overflow is fine; children render after this header
        // (Shneiderman z-order) so the deepest visible label wins in any overlapping
        // region. Only suppress the header when the parent rect itself can't even hold
        // a header strip + a tiny child area beneath it (sub-pixel-noise gate).
        const double headerH = HeaderHeightPx;
        const double sidePad = SidePaddingPx;
        Rect childBounds = bounds;
        bool headerDrawn = false;
        if (series.ShowLabels && !string.IsNullOrEmpty(node.Label)
            && bounds.Height > headerH + 20 && bounds.Width > 40)
        {
            var font = Context.Theme.DataTextFontOn(interiorColor, fontSize, GroundInk.Readable);
            TagTreemapNode(Ctx, nodeId, depth, parentId);
            Ctx.Draw(TextLabel.Of(node.Label), font, new TextPlacement(CellLabel.Standard.HeaderName(bounds, headerH), TextAlignment.Left));
            // The parent's own measure, at the far end of its header strip — the total beside the name.
            if (!string.IsNullOrEmpty(node.Headline)
                && CellLabel.Standard.HeaderPairFits(Ctx.ExtentOf(TextLabel.Of(node.Headline), font).Width, Ctx.ExtentOf(TextLabel.Of(node.Label), font).Width, bounds))
            {
                Ctx.Draw(TextLabel.Of(node.Headline), font, new TextPlacement(CellLabel.Standard.HeaderMeasure(bounds, headerH), TextAlignment.Right));
            }
            headerDrawn = true;
        }
        if (headerDrawn)
            childBounds = new Rect(
                bounds.X + sidePad,
                bounds.Y + headerH,
                bounds.Width - sidePad * 2,
                bounds.Height - headerH - sidePad);

        // Lay the children out in the REDUCED bounds so the parent frame stays visible.
        var cells = SquarifiedLayout.Of(node, childBounds, series.Padding, Context.Theme.Looks.Treemap.LikeSquarify);
        for (int i = 0; i < cells.Length; i++)
        {
            string childId = nodeId + "." + i.InvariantText();
            RenderNode(cells[i].Node, cells[i].Bounds, series,
                depth + 1, i, cells.Length, childId, nodeId);
        }
    }

    /// <summary>The style of a tile of <paramref name="fill"/>: edged as a bar of matplotlib is under the matplotlib themes (squarify draws
    /// its tiles as bars: black at 1 pt under classic, none under the default style; a tile's own opacity is its colour's, which its edge
    /// does not take, as matplotlib's <c>bar</c> given no <c>alpha</c> keeps the edge's), in a white pixel under every other theme
    /// (<see cref="Theme.PatchEdgeOf"/>).</summary>
    /// <param name="fill">The tile's colour.</param>
    /// <param name="hatch">The pattern its node lays over it and the colour the node names for it: the colour of a bar's hatch where it
    /// names none, the theme's rule over the tile's edge.</param>
    private ShapeStyle TileStyle(Color fill, HatchFill hatch = default) => _edges.StyleOf(fill, fill, hatch);

    /// <summary>The label as it will be drawn under <paramref name="fit"/>: the full text, a shortened one with
    /// an ellipsis, or null for "not drawn" — the rect carries the full text in <c>data-treemap-label</c> either way.</summary>
    private string? FitLabel(string label, Font font, double width, TreemapLabelFit fit)
    {
        if (fit == TreemapLabelFit.Always || Ctx.ExtentOf(TextLabel.Of(label), font).Width <= width)
        {
            return label;
        }
        if (fit == TreemapLabelFit.Fit)
        {
            return null;
        }
        for (int keep = label.Length - 1; keep > 0; keep--)
        {
            var shortened = label[..keep].TrimEnd() + "…";
            if (Ctx.ExtentOf(TextLabel.Of(shortened), font).Width <= width)
            {
                return shortened;
            }
        }
        return "…";
    }

    /// <summary>Pixel height reserved at the top of an interior rectangle for its label.</summary>
    private const double HeaderHeightPx = 18.0;

    /// <summary>Pixel padding between sibling rectangles inside an interior frame.</summary>
    private const double SidePaddingPx = 2.0;

    /// <summary>Tags the next element drawn with the node it stands for (<c>data-treemap-node</c>, <c>-depth</c>, <c>-parent</c>)
    /// and, when <paramref name="label"/> is given, the node's label (<c>-label</c>): the rect carries the full text whatever
    /// is drawn of it.</summary>
    /// <param name="nodeId">The node's path, the root "0", its first child "0.0".</param>
    /// <param name="depth">How many levels the node stands below the root.</param>
    /// <param name="parentId">The path of the node's parent; empty for the root.</param>
    /// <param name="label">The node's full label for a tile, or <see langword="null"/> for the text drawn on it.</param>
    /// <param name="ctx">The <see cref="IRenderContext"/> it works on.</param>
    internal static void TagTreemapNode(IRenderContext ctx, string nodeId, int depth, string parentId, string? label = null)
    {
        ctx.SetNextElementData("treemap-node", nodeId);
        ctx.SetNextElementData("treemap-depth", depth.InvariantText());
        ctx.SetNextElementData("treemap-parent", parentId);
        if (label is not null) ctx.SetNextElementData("treemap-label", label);
    }
}
