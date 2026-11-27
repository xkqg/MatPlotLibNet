// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Builders;

namespace MatPlotLibNet;

/// <summary>What a viewer of the figure can do with it in a browser: zoom and pan, tooltips, highlight, selection, the scripts of
/// the 3-D, treemap and Sankey charts, and the interaction a server answers.</summary>
public static class FigureBuilderInteractionVerbs
{
    extension(FigureBuilder figure)
    {
        /// <summary>Enables interactive zoom and pan via JavaScript in SVG output.</summary>
        public FigureBuilder WithZoomPan(bool enabled = true) => figure.OnFigure(f => f.EnableZoomPan = enabled);

        /// <summary>Enables click-to-toggle legend entries in SVG output.</summary>
        public FigureBuilder WithLegendToggle(bool enabled = true) => figure.OnFigure(f => f.EnableLegendToggle = enabled);

        /// <summary>Enables styled HTML tooltip overlays (replacing native browser tooltips) in SVG output.</summary>
        public FigureBuilder WithRichTooltips(bool enabled = true) => figure.OnFigure(f => f.EnableRichTooltips = enabled);

        /// <summary>Enables series highlight-on-hover (dims sibling series) in SVG output.</summary>
        public FigureBuilder WithHighlight(bool enabled = true) => figure.OnFigure(f => f.EnableHighlight = enabled);

        /// <summary>Enables Shift+drag rectangular data selection in SVG output.</summary>
        public FigureBuilder WithSelection(bool enabled = true) => figure.OnFigure(f => f.EnableSelection = enabled);

        /// <summary>Links the chart to the others of the same data on a page: a brush on it lights the same rows (<c>RowKeys</c>) on every
        /// chart that shares <paramref name="group"/>, and theirs light the rows on it. Switches the brush
        /// (<see cref="WithSelection"/>) on, because the brush is how rows are selected.</summary>
        /// <param name="group">The name the charts share.</param>
        /// <param name="selectedRows">The rows the chart starts with selected; none by default.</param>
        /// <exception cref="ArgumentException">The group is blank. A blank row key is refused by the figure, so
        /// <see cref="FigureBuilder.Build"/> throws it.</exception>
        public FigureBuilder LinkSelection(string group, IReadOnlyList<string>? selectedRows = null)
        {
            if (string.IsNullOrWhiteSpace(group))
                throw new ArgumentException("A selection group needs a name.", nameof(group));
            return figure.OnFigure(f =>
            {
                f.EnableSelection = true;
                f.SelectionGroup = group;
                f.SelectedRows = selectedRows;
            });
        }

        /// <summary>Convenience that enables ALL browser-side interactions in one call:
        /// <see cref="WithZoomPan"/>, <see cref="WithRichTooltips"/>, <see cref="WithLegendToggle"/>,
        /// <see cref="WithHighlight"/>, <see cref="WithSelection"/>, plus the chart-type-specific
        /// <see cref="With3DRotation"/> (drag to rotate 3D scenes), <see cref="WithTreemapDrilldown"/>
        /// (click a parent tile to expand/collapse its children), and
        /// <see cref="WithSankeyHover"/> (highlight upstream/downstream flows). Each chart-type-specific
        /// script is inert when its element is absent, so a 2D line chart with this convenience pays
        /// no runtime cost in the browser for the 3D / treemap / sankey scripts. The resulting SVG works standalone
        /// in any browser — no .NET runtime needed on the client. Every document carries the scripts: on a two-line
        /// chart they come to nine scripts and 56,101 characters, the 3D, treemap and sankey ones included, so a chart that is
        /// drawn again many times a second sends them every time — switch on only the interactions it uses there.</summary>
        public FigureBuilder WithBrowserInteraction(bool enabled = true) => figure.OnFigure(f =>
        {
            f.EnableZoomPan = enabled;
            f.EnableRichTooltips = enabled;
            f.EnableLegendToggle = enabled;
            f.EnableHighlight = enabled;
            f.EnableSelection = enabled;
            f.Enable3DRotation = enabled;
            f.EnableTreemapDrilldown = enabled;
            f.EnableSankeyHover = enabled;
        });

        /// <summary>Opts the figure into bidirectional SignalR interaction under the supplied
        /// <paramref name="chartId"/>. Interaction events (zoom, pan, reset, legend-toggle) selected
        /// in <paramref name="configure"/> are routed through the ChartHub to the server's
        /// <c>FigureRegistry</c>, which mutates the registered figure and re-publishes the updated
        /// SVG. When this method is called, the SVG output embeds the SignalR dispatcher script
        /// instead of the local client-side IIFE scripts.</summary>
        public FigureBuilder WithServerInteraction(string chartId, Action<ServerInteractionBuilder> configure)
        {
            var builder = new ServerInteractionBuilder();
            configure(builder);
            // What the callback asked for is read here, once: a builder the callback kept and changed later changes nothing.
            bool zoomPan = builder.Zoom || builder.Pan || builder.Reset, legendToggle = builder.LegendToggle, selection = builder.BrushSelect,
                tooltips = builder.Hover, dataCursor = builder.DataCursor, viewerCursors = builder.ViewerCursors;
            return figure.OnFigure(f =>
            {
                f.ChartId = chartId;
                f.ServerInteraction = true;
                if (zoomPan) f.EnableZoomPan = true;
                if (legendToggle) f.EnableLegendToggle = true;
                if (selection) f.EnableSelection = true;
                if (tooltips) f.EnableRichTooltips = true;
                if (dataCursor) f.EnableDataCursor = true;
                if (viewerCursors) f.EnableViewerCursors = true;
            });
        }

        /// <summary>Enables interactive 3D rotation via mouse drag and keyboard arrow keys in the SVG output.</summary>
        public FigureBuilder With3DRotation(bool enabled = true) => figure.OnFigure(f => f.Enable3DRotation = enabled);

        /// <summary>Enables expand/collapse interaction on treemap rectangles in the SVG output.
        /// <para>The initial interactive view is pixel-identical to the static SVG — every node at
        /// every depth is visible on first paint (z-order paints children over parents, so the
        /// deepest visible label wins in any overlapping region). Clicking a parent rect
        /// <em>collapses</em> its entire subtree (transitively — descendants' own expansion state is
        /// preserved); clicking again restores. Multiple subtrees can be collapsed independently.
        /// Leaves are not toggleable. Requires a <c>TreemapSeries</c> on the figure.</para>
        /// <para>The method is named <c>WithTreemapDrilldown</c> from the time a click zoomed into a
        /// node instead of collapsing it; the name was kept so existing callers still compile.</para></summary>
        public FigureBuilder WithTreemapDrilldown(bool enabled = true) => figure.OnFigure(f => f.EnableTreemapDrilldown = enabled);

        /// <summary>Enables Sankey hover emphasis in the SVG output: hovering a node dims every
        /// link not reachable upstream or downstream from it (ECharts <c>focus: adjacency</c>).
        /// Requires a <c>SankeySeries</c> on the figure.</summary>
        public FigureBuilder WithSankeyHover(bool enabled = true) => figure.OnFigure(f => f.EnableSankeyHover = enabled);

        /// <summary>Configures opacity / transition tokens for the embedded browser-side
        /// interaction scripts. Defaults match the values the scripts hard-coded before they were
        /// configurable, so calling this is only necessary if you want different fade opacities,
        /// transition durations, or tooltip offsets.</summary>
        public FigureBuilder WithInteractionTheme(Models.InteractionTheme theme) => figure.OnFigure(f => f.InteractionTheme = theme);
    }
}
