// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;

namespace MatPlotLibNet.Transforms;

/// <summary>Transforms a <see cref="Figure"/> into SVG format. Also the library's <see cref="ISvgRenderer"/>, the one writer that
/// draws a figure as a document in its parts as well, which a publisher sends only the changed parts of.</summary>
public sealed partial class SvgTransform : FigureTransform, ISvgRenderer, ISvgDocumentRenderer
{
    /// <summary>Initializes a new SVG transform with the specified chart renderer.</summary>
    public SvgTransform(IChartRenderer renderer) : base(renderer) { }

    /// <summary>Initializes a new SVG transform with the default chart renderer.</summary>
    public SvgTransform() { }

    /// <summary>Renders the figure to a complete SVG string.</summary>
    public string Render(Figure figure) => RenderDocument(figure).ToSvg();

    /// <summary>Renders the figure to an SVG document in its parts (<see cref="SvgFigureDocument"/>): written plain it is
    /// <see cref="Render"/>'s string; a publisher that sends only the parts that changed reads them one by one.</summary>
    internal SvgFigureDocument RenderDocument(Figure figure)
    {
        // The renderer the transform was made with draws onto a target that gives each subplot a context of its own and hands an
        // unchanged subplot out as it was drawn (the library's renderer draws every stage onto it in one pass; one of the caller's
        // draws onto its background). The figure keeps what was drawn of each subplot until its next draw.
        var target = new SvgFrameTarget(figure, figure.DrawnSvgAxes);
        FigureRenderer.Draw(figure, target);
        figure.DrawnSvgAxes = target.ToKeep();

        // A figure without subplots has nothing to interact with, so it carries no script.
        var scripts = figure.SubPlots.Count == 0 ? [] : ScriptsOf(figure);
        var (rootTag, headBody) = HeadOf(figure);
        return target.ToDocument(rootTag, headBody, scripts);
    }

    /// <summary>The library's writer draws every figure as a document in its parts (<see cref="RenderDocument(Figure)"/>).</summary>
    SvgFigureDocument ISvgDocumentRenderer.RenderDocument(Figure figure) => RenderDocument(figure);

    /// <summary>The scripts a figure with subplots carries, in the order they are written.</summary>
    private static List<string> ScriptsOf(Figure figure)
    {
        var scripts = new List<string>();
        // ServerInteraction routes browser events through SignalR instead of the local
        // client-side IIFEs — the SignalR dispatcher replaces all four local scripts
        // (zoom/pan, legend-toggle, selection/brush, tooltip) with one unified branch
        // that invokes ChartHub methods. Opted-in branches are included; others are
        // omitted for size. Emission order is preserved from v1.2.1 so existing static
        // figures produce byte-identical SVG output.
        if (figure.ServerInteraction)
        {
            scripts.Add(ServerInteractionScriptFor(figure));
        }
        else
        {
            if (figure.EnableZoomPan)
                scripts.Add(GetZoomPanScript());
            if (figure.EnableLegendToggle)
            {
                scripts.Add(LegendToggleScript);
                // Phase S — drag is co-emitted with toggle: they share the legend DOM
                // surface and must coordinate (drag suppresses the synthetic click that
                // would otherwise fire toggle on drop).
                scripts.Add(LegendDragScript);
            }
        }
        // v1.2.0 preserved order: RichTooltips / Highlight / Selection are always
        // emitted after the zoom/pan/legend block for static figures. v1.2.2 skips
        // RichTooltips and Selection for ServerInteraction figures (the dispatcher
        // replaces them) but keeps Highlight emission unchanged.
        if (!figure.ServerInteraction && figure.EnableRichTooltips)
            scripts.Add(CustomTooltipScript);
        if (figure.EnableHighlight)
            scripts.Add(HighlightScript);
        if (!figure.ServerInteraction && figure.EnableSelection)
            scripts.Add(SelectionScript);
        if (figure.SelectionGroup is not null)
            scripts.Add(RowSelectionScript);
        if (figure.Enable3DRotation)
            scripts.Add(GetScript());
        if (figure.EnableTreemapDrilldown)
            scripts.Add(TreemapDrilldownScript);
        if (figure.EnableSankeyHover)
            scripts.Add(SankeyHoverScript);
        return scripts;
    }
    /// <inheritdoc />
    public override void Transform(Figure figure, Stream output)
    {
        var svg = Render(figure);
        var bytes = Encoding.UTF8.GetBytes(svg);
        output.Write(bytes, 0, bytes.Length);
    }

    /// <summary>Writes the figure as an SVG document, applying the settings of <paramref name="options"/> an SVG can
    /// use: <see cref="SaveOptions.PrettifySvg"/>, <see cref="SaveOptions.SvgDecimalPrecision"/>,
    /// <see cref="SaveOptions.Title"/> and <see cref="SaveOptions.Author"/>. An SVG has no pixels, so
    /// <see cref="SaveOptions.Dpi"/> changes nothing. Options that ask for none of the four write the same document
    /// as <see cref="Transform(Figure, Stream)"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><see cref="SaveOptions.SvgDecimalPrecision"/> is outside 0 to 15.</exception>
    public override void Transform(Figure figure, Stream output, SaveOptions options)
    {
        var svg = Render(figure).WithSaveOptions(options);
        var bytes = Encoding.UTF8.GetBytes(svg);
        output.Write(bytes, 0, bytes.Length);
    }

    private static bool HasHyperlink(Figure figure)
    {
        foreach (var axes in figure.SubPlots)
            foreach (var series in axes.Series)
                if (series is Models.Series.StatTileSeries { Url: { Length: > 0 } })
                    return true;
        return false;
    }

    /// <summary>The opening tag of the root element and, after it, the title and the description.</summary>
    private static SvgHead HeadOf(Figure figure)
    {
        var sb = new StringBuilder(512);

        // The accessible name — alt text, else the title, else a tile row's labels — is ONE rule the figure owns
        // (FigureExtensions.AccessibleName); the data table's caption reads the same rule, so the two can never
        // name the same picture differently. An empty name still writes an empty <title>.
        string altText = figure.AccessibleName();
        bool hasDescription = figure.Description is not null;
        // A figure that contains a hyperlink is a GROUP, not an image: role="img" makes every descendant
        // presentational, so the link's aria-label and aria-expanded would never be announced (WAI-ARIA
        // children-presentational rule).
        string role = HasHyperlink(figure) ? "group" : "img";

        // The size is written in three places and formatted once.
        Span<char> width = stackalloc char[32], height = stackalloc char[32];
        width = width[..figure.Width.FormatExactSvgNumber(width)];
        height = height[..figure.Height.FormatExactSvgNumber(height)];
        sb.Append("<svg xmlns=\"").Append(SvgAttributes.Namespace).Append("\" viewBox=\"0 0 ")
          .Append(width).Append(' ').Append(height)
          .Append("\" width=\"").Append(width)
          .Append("\" height=\"").Append(height);
        if (figure.ResponsiveSvg)
            // Phase P fix (v1.7.2 follow-on, 2026-04-18) — was max-width:100% which only
            // scaled DOWN. User wanted the chart to EXTEND to fill wider viewports too,
            // not just shrink when narrow. width:100% + height:auto scales both
            // directions while viewBox preserves aspect ratio. CSS width:100% does NOT
            // affect naturalWidth (read from the width="..." HTML attribute), so PNG
            // export via <img> still sees the intrinsic pixel size.
            sb.Append("\" style=\"width:100%;height:auto");
        sb.Append("\" role=\"").Append(role).Append('"')
          .Append(" aria-labelledby=\"chart-title\"");
        if (hasDescription)
            sb.Append(" aria-describedby=\"chart-desc\"");
        // A plot with an aspect must not be stretched by zooming the picture: the zoom script reads this to keep the view isotropic.
        if (figure.SubPlots.Any(axes => axes.AspectUnder(figure.Theme) is { } aspect && aspect.IsPositiveFinite()))
            sb.Append(" data-aspect-lock=\"true\"");
        // The link to the other charts of the same data: the group's name, and the rows the chart starts with selected.
        if (figure.SelectionGroup is { } selectionGroup)
        {
            sb.Append(" data-selection-group=\"").Append(selectionGroup.EscapeForXml()).Append('"');
            if (figure.SelectedRows is { Count: > 0 } selectedRows)
                sb.Append(" data-selected-rows=\"").Append(System.Text.Json.JsonSerializer.Serialize(selectedRows).EscapeForXml()).Append('"');
        }
        if (figure.ServerInteraction && figure.ChartId is { } chartId)
        {
            sb.Append(' ').Append(SvgAttributes.ChartId).Append("=\"").Append(chartId.EscapeForXml()).Append('"');
            // The view of the first axes and the range Home returns to, on the root: what the server script falls back on for
            // an axes drawn without a Cartesian range (polar, 3-D). Written with every digit, as every axes' own view is.
            if (figure.SubPlots.Count > 0)
            {
                var ax0 = figure.SubPlots[0];
                double xmin = ax0.XAxis.Min ?? 0, xmax = ax0.XAxis.Max ?? 1;
                double ymin = ax0.YAxis.Min ?? 0, ymax = ax0.YAxis.Max ?? 1;
                sb.AppendExactAttribute(SvgAttributes.XMin, xmin).AppendExactAttribute(SvgAttributes.XMax, xmax)
                  .AppendExactAttribute(SvgAttributes.YMin, ymin).AppendExactAttribute(SvgAttributes.YMax, ymax)
                  .AppendExactAttribute(SvgAttributes.ResetXMin, xmin).AppendExactAttribute(SvgAttributes.ResetXMax, xmax)
                  .AppendExactAttribute(SvgAttributes.ResetYMin, ymin).AppendExactAttribute(SvgAttributes.ResetYMax, ymax);
            }
        }
        // The tokens of the interaction theme the figure changed, which its scripts read back over the default's.
        InteractionToken.AppendDifferences(sb, figure.InteractionTheme);
        sb.AppendLineLf(">");
        string rootTag = sb.ToString();

        // SVG title (always emitted for role="img" accessibility)
        sb.Clear();
        sb.Append("<title id=\"chart-title\">")
          .Append(altText.EscapeForXml())
          .AppendLineLf("</title>");

        if (hasDescription)
        {
            sb.Append("<desc id=\"chart-desc\">")
              .Append(figure.Description!.EscapeForXml())
              .AppendLineLf("</desc>");
        }

        return new SvgHead(rootTag, sb.ToString());
    }

    /// <summary>The opening tag of the root element, and the title and the description that follow it.</summary>
    private readonly record struct SvgHead(string RootTag, string Body);

    // What the comments of the script below said. They are not emitted: a figure that carries the script carries no
    // comment of it. Each note stands at the line it was written beside.
    // at the locator (SvgScriptPreamble.Root)
    //   Per-chart isolation: only attach to titles inside THIS script's owning <svg>.
    // at `parent.addEventListener('focus', function(e) {`
    //   The focus tooltip uses the focused element's bounds, not (0, 0): a tooltip at
    //   (0, 0) would jump to the top-left corner of the viewport on keyboard focus,
    //   often off-screen and inaccessible.
    /// <summary>Provides the embedded JavaScript for styled HTML tooltip overlays in SVG output.</summary>
    /// <remarks>
    /// The <c>&lt;script&gt;</c> block that intercepts elements containing a <c>&lt;title&gt;</c>
    /// child and displays a styled floating tooltip div instead of the native browser tooltip.
    /// The tooltip div carries <c>role="tooltip"</c> and <c>aria-live="polite"</c>.
    /// Focus/blur on the parent element mirrors mouseover/mouseout for keyboard accessibility.
    /// </remarks>
    internal static string CustomTooltipScript => field ??= $$"""
        <script type="text/ecmascript"><![CDATA[
        (function() {
        {{SvgScriptPreamble.Root}}
        {{SvgScriptPreamble.Token}}
            var offsetX = {{InteractionToken.TooltipOffsetX.Read}}, offsetY = {{InteractionToken.TooltipOffsetY.Read}};
            var tip = document.createElement('div');
            tip.setAttribute('role', 'tooltip');
            tip.setAttribute('aria-live', 'polite');
            tip.style.cssText = 'position:fixed;background:#333;color:#fff;padding:4px 8px;border-radius:4px;font-size:12px;pointer-events:none;display:none;z-index:9999;white-space:pre';
            document.body.appendChild(tip);
            svg.querySelectorAll('g > title').forEach(function(title) {
                var parent = title.parentNode;
                function showTip(x, y) {
                    tip.textContent = title.textContent;
                    tip.style.display = 'block';
                    tip.style.left = (x + offsetX) + 'px';
                    tip.style.top  = (y + offsetY) + 'px';
                }
                function hideTip() { tip.style.display = 'none'; }

                parent.addEventListener('mouseover', function(e) { showTip(e.clientX, e.clientY); });
                parent.addEventListener('mousemove', function(e) {
                    tip.style.left = (e.clientX + offsetX) + 'px';
                    tip.style.top  = (e.clientY + offsetY) + 'px';
                });
                parent.addEventListener('mouseout', hideTip);
                parent.addEventListener('focus', function(e) {
                    var rect = (e.target.getBoundingClientRect && e.target.getBoundingClientRect()) || {left: 0, top: 0, width: 0, height: 0};
                    showTip(rect.left + rect.width / 2, rect.top);
                });
                parent.addEventListener('blur', hideTip);
            });
        })();
        ]]></script>
        """;

    // What the comments of the script below said. They are not emitted: a figure that carries the script carries no
    // comment of it. Each note stands at the line it was written beside.
    // at the locator (SvgScriptPreamble.Root)
    //   Per-chart isolation: find THIS script's owning <svg> via
    //   document.currentScript.parentNode so multi-chart pages don't cross-talk.
    //   Fallback to document.querySelector('svg') only for inline-runner environments
    //   where currentScript is unavailable (e.g. some test hosts).
    // at `legendItems.forEach(function(entry) {`
    //   Every entry of this series shows the one state: a series with named parts (the layers of a stacked
    //   area) has an entry for each, all carrying its index.
    // at `item.addEventListener('click', function() { toggle(); });`
    //   Toggle fires ONLY on a real click (full press-and-release without drag). Firing
    //   on pointerdown would (a) hide the series the instant the user pressed (a
    //   single-series chart would visibly empty) and (b) make drag-to-reposition
    //   mechanically impossible because the data would vanish before the drag could
    //   engage. The companion legend-drag script suppresses the synthetic click that
    //   follows a real drag via capture-phase stopPropagation, so toggle-on-click
    //   coexists cleanly with drag.
    /// <summary>Embedded JavaScript for click-to-toggle legend items in SVG output.
    /// Includes ARIA attributes (role="button", aria-pressed, tabindex) for accessibility.
    /// Also supports keyboard activation (Enter/Space) per WCAG 2.1.</summary>
    internal static string LegendToggleScript => $$"""
        <script type="text/ecmascript"><![CDATA[
        (function() {
            'use strict';
        {{SvgScriptPreamble.Root}}
            var legendItems = svg.querySelectorAll('[{{SvgAttributes.LegendIndex}}]');
            legendItems.forEach(function(item) {
                item.style.cursor = 'pointer';
                item.setAttribute('tabindex', '0');
                item.setAttribute('role', 'button');
                item.setAttribute('aria-pressed', 'false');

                function toggle() {
                    var idx = item.getAttribute('{{SvgAttributes.LegendIndex}}');
                    var series = svg.querySelectorAll('[{{SvgAttributes.SeriesIndex}}="' + idx + '"]');
                    var hidden = false;
                    series.forEach(function(s) {
                        hidden = s.style.display === 'none';
                        s.style.display = hidden ? '' : 'none';
                    });
                    legendItems.forEach(function(entry) {
                        if (entry.getAttribute('{{SvgAttributes.LegendIndex}}') !== idx) return;
                        entry.style.opacity = hidden ? '1' : '0.3';
                        entry.setAttribute('aria-pressed', hidden ? 'false' : 'true');
                    });
                }

                item.addEventListener('click', function() { toggle(); });
                item.addEventListener('keydown', function(e) {
                    if (e.key === 'Enter' || e.key === ' ') { toggle(); e.preventDefault(); }
                });
            });
        })();
        ]]></script>
        """;

    /// <summary>A token of the <see cref="InteractionTheme"/> as a chart carries it for its scripts: the attribute of the root it is
    /// written as, and how it is read off a theme. The root writes a token only where the figure's theme differs from
    /// <see cref="InteractionTheme.Default"/> (<see cref="Transforms.SvgTransform.InteractionToken.AppendDifferences"/>), and a script reads it back with the default's value as
    /// its fallback (<see cref="Transforms.SvgTransform.InteractionToken.Read"/>), so a figure at rest carries no token and its scripts do what the default says.</summary>
    /// <param name="Attribute">The attribute of the root the token is written as.</param>
    /// <param name="Of">How the token is read off a theme.</param>
    internal readonly record struct InteractionToken(string Attribute, Func<InteractionTheme, double> Of)
    {
        /// <summary>The opacity a highlight dims the other series to.</summary>
        internal static readonly InteractionToken HighlightOpacity = new("data-mpl-highlight-opacity", t => t.HighlightOpacity);

        /// <summary>The opacity a Sankey hover dims the unreachable links to.</summary>
        internal static readonly InteractionToken SankeyLinkOpacity = new("data-mpl-sankey-link-opacity", t => t.SankeyDimLinkOpacity);

        /// <summary>The opacity a Sankey hover dims the unreachable nodes to.</summary>
        internal static readonly InteractionToken SankeyNodeOpacity = new("data-mpl-sankey-node-opacity", t => t.SankeyDimNodeOpacity);

        /// <summary>How long a treemap's collapse and expand take, in milliseconds.</summary>
        internal static readonly InteractionToken TreemapTransitionMs = new("data-mpl-treemap-transition-ms", t => t.TreemapTransitionMs);

        /// <summary>How far right of the pointer a tooltip stands, in pixels.</summary>
        internal static readonly InteractionToken TooltipOffsetX = new("data-mpl-tooltip-offset-x", t => t.TooltipOffsetX);

        /// <summary>How far below the pointer a tooltip stands, in pixels.</summary>
        internal static readonly InteractionToken TooltipOffsetY = new("data-mpl-tooltip-offset-y", t => t.TooltipOffsetY);

        /// <summary>Every token, in the order the root writes them.</summary>
        internal static readonly InteractionToken[] All =
            [HighlightOpacity, SankeyLinkOpacity, SankeyNodeOpacity, TreemapTransitionMs, TooltipOffsetX, TooltipOffsetY];

        /// <summary>The expression a script reads the token with: the root's attribute, else the default theme's value. It calls the
        /// <c>token</c> function of <see cref="SvgScriptPreamble.Token"/>, which the script carries.</summary>
        internal string Read => string.Invariant($"token('{Attribute}', {Of(InteractionTheme.Default)})");

        /// <summary>Appends, as attributes of the root, every token on which <paramref name="theme"/> differs from the default.</summary>
        /// <param name="root">The root's opening tag, being written.</param>
        /// <param name="theme">The figure's interaction theme.</param>
        internal static void AppendDifferences(StringBuilder root, InteractionTheme theme)
        {
            // A figure at the default theme, the usual one, carries no token.
            if (theme == InteractionTheme.Default) return;
            foreach (var token in All)
            {
                double value = token.Of(theme);
                if (value != token.Of(InteractionTheme.Default)) root.AppendExactAttribute(token.Attribute, value);
            }
        }
    }

    // What the comments of the script below said. They are not emitted: a figure that carries the script carries no
    // comment of it. Each note stands at the line it was written beside.
    // at the locator (SvgScriptPreamble.Root)
    //   Per-chart isolation: scope query to THIS script's owning <svg>.
    // at `var dimOpacity = parseFloat(svg.getAttribute && svg.getAttribute('data-mpl-highl`
    //   Read the themable opacity from data-mpl-highlight-opacity
    //   (set by FigureBuilder.WithInteractionTheme). The default is 0.3.
    // at `groups.forEach(function(s) {`
    //   Restore each element to its data-mpl-opacity-base (captured below on
    //   first dim) instead of forcing 1.0. Preserves explicit opacity that
    //   callers set via series.Alpha or CSS overrides.
    // at `var captured = false;`
    //   Capture original opacity once (lazy on first highlight) so restore is correct
    //   even when a series has explicit opacity="0.5" before any hover happens.
    /// <summary>Provides the embedded JavaScript for series highlight-on-hover in SVG output.</summary>
    /// <remarks>
    /// The <c>&lt;script&gt;</c> block that dims sibling series groups (to 30% opacity)
    /// when the cursor enters (or keyboard focus reaches) a group with a <c>data-series-index</c> attribute,
    /// restoring all on mouse-leave or blur.
    /// Each series group receives <c>tabindex="0"</c> for keyboard reachability.
    /// </remarks>
    internal static string HighlightScript => field ??= $$"""
        <script type="text/ecmascript"><![CDATA[
        (function() {
        {{SvgScriptPreamble.Root}}
        {{SvgScriptPreamble.Token}}
            var dimOpacity = {{InteractionToken.HighlightOpacity.Read}};
            var groups = svg.querySelectorAll('[{{SvgAttributes.SeriesIndex}}]');
            groups.forEach(function(g) {
                g.setAttribute('tabindex', '0');

                function highlight() {
                    groups.forEach(function(s) {
                        s.style.opacity = s === g ? '1' : String(dimOpacity);
                    });
                }
                function restore() {
                    groups.forEach(function(s) {
                        var base = s.getAttribute('data-mpl-opacity-base');
                        s.style.opacity = base !== null ? base : '1';
                    });
                }
                var captured = false;
                function captureOnce() {
                    if (captured) return;
                    captured = true;
                    groups.forEach(function(s) {
                        if (s.getAttribute('data-mpl-opacity-base') === null) {
                            var explicit = s.getAttribute('opacity');
                            s.setAttribute('data-mpl-opacity-base', explicit !== null ? explicit : '1');
                        }
                    });
                }
                var origHighlight = highlight;
                highlight = function() { captureOnce(); origHighlight(); };

                g.addEventListener('mouseenter', highlight);
                g.addEventListener('mouseleave', restore);
                g.addEventListener('focus', highlight);
                g.addEventListener('blur', restore);
            });
        })();
        ]]></script>
        """;

    // What the comments of the script below said. They are not emitted: a figure that carries the script carries no
    // comment of it. Each note stands at the line it was written beside.
    // at the locator (SvgScriptPreamble.Root)
    //   Per-chart isolation: self-locate via document.currentScript.
    // at `function startSelection(e) {`
    //   Pointer Events for touch + pen + mouse. setPointerCapture binds
    //   the pointer to the SVG so dragging out of the chart still releases cleanly.
    // at `svg.addEventListener('mousedown', startSelection);`
    //   Mouse fallback for pre-pointer-events runtimes.
    /// <summary>Provides the embedded JavaScript for rectangular data selection in SVG output.</summary>
    /// <remarks>
    /// The <c>&lt;script&gt;</c> block that activates a selection rectangle on Shift+mousedown.
    /// On mouseup, a <c>CustomEvent('mpl:selection', { detail: { x1, y1, x2, y2 } })</c> is dispatched
    /// on the SVG element, allowing host applications to react to user-defined data regions.
    /// <kbd>Escape</kbd> cancels an active selection without dispatching the event.
    /// The selection rect carries <c>aria-label="Data selection area"</c>.
    /// </remarks>
    internal static string SelectionScript => $$"""
        <script type="text/ecmascript"><![CDATA[
        (function() {
        {{SvgScriptPreamble.Root}}
        {{SvgScriptPreamble.Capture}}
        {{SvgScriptPreamble.UserPoint}}
            var rect = null, startPt = null;
            function startSelection(e) {
                if (!e.shiftKey) return;
                e.preventDefault();
                capture(svg, e);
                startPt = userPoint(e.clientX, e.clientY);
                rect = document.createElementNS('{{SvgAttributes.Namespace}}', 'rect');
                rect.setAttribute('fill', 'rgba(100,149,237,0.25)');
                rect.setAttribute('stroke', '#6495ED');
                rect.setAttribute('stroke-width', '1');
                rect.setAttribute('aria-label', 'Data selection area');
                rect.setAttribute('x', startPt.x);
                rect.setAttribute('y', startPt.y);
                rect.setAttribute('width', '0');
                rect.setAttribute('height', '0');
                svg.appendChild(rect);
            }
            function updateSelection(e) {
                if (!rect || !startPt) return;
                var cur = userPoint(e.clientX, e.clientY);
                var x = Math.min(startPt.x, cur.x), y = Math.min(startPt.y, cur.y);
                var w = Math.abs(cur.x - startPt.x), h = Math.abs(cur.y - startPt.y);
                rect.setAttribute('x', x); rect.setAttribute('y', y);
                rect.setAttribute('width', w); rect.setAttribute('height', h);
            }
            function endSelection(e) {
                if (!rect || !startPt) return;
                var endPt = userPoint(e.clientX, e.clientY);
                svg.dispatchEvent(new CustomEvent('mpl:selection', { detail: {
                    x1: Math.min(startPt.x, endPt.x), y1: Math.min(startPt.y, endPt.y),
                    x2: Math.max(startPt.x, endPt.x), y2: Math.max(startPt.y, endPt.y)
                }, bubbles: true }));
                svg.removeChild(rect);
                rect = null; startPt = null;
            }
            svg.addEventListener('pointerdown', startSelection);
            svg.addEventListener('pointermove', updateSelection);
            svg.addEventListener('pointerup',   endSelection);
            svg.addEventListener('mousedown', startSelection);
            svg.addEventListener('mousemove', updateSelection);
            svg.addEventListener('mouseup',   endSelection);
            svg.addEventListener('keydown', function(e) {
                if (e.key === 'Escape' && rect) {
                    svg.removeChild(rect);
                    rect = null; startPt = null;
                }
            });
        })();
        ]]></script>
        """;

    // What the comments of the script below said. They are not emitted: a figure that carries the script carries no
    // comment of it. Each note stands at the line it was written beside.
    // at the locator (SvgScriptPreamble.Root)
    //   Per-chart isolation: self-locate via document.currentScript.
    // at `function apply(rows) {`
    //   Lights the rows and dims the rest; no rows lights nothing and dims nothing.
    // at `function rowsIn(r) {`
    //   The rows whose marks have their middle inside the rectangle (in the SVG's own units).
    // at `if (!svg.isConnected) { window.removeEventListener('mpl:rows', onRows); return; `
    //   A chart that has been replaced is no longer in the document: its listener goes, and the new chart has its own.
    /// <summary>Provides the embedded JavaScript that links the selection of the charts of one group on a page.</summary>
    /// <remarks>
    /// The <c>&lt;script&gt;</c> block that links the chart to the others of its selection group
    /// (<c>data-selection-group</c> on the root). When the chart's brush ends (<c>mpl:selection</c> on the SVG) it works out which
    /// marks the rectangle covers, those that carry <c>data-row</c> and whose middle lies inside it, and says so to the window as
    /// <c>CustomEvent('mpl:rows', { detail: { group, rows } })</c>; every chart of that group, this one included, lights those
    /// rows (<c>aria-selected="true"</c>, the rest <c>"false"</c> and dimmed) and a chart of another group hears nothing.
    /// <kbd>Escape</kbd> on the chart clears the selection of the whole group. The rows in <c>data-selected-rows</c> are lit when
    /// the script starts. A chart that is mounted again leaves no second listener: the listener of a chart that is no longer in
    /// the document removes itself the first time the window speaks. A window event, not a channel between tabs: the link is the
    /// page's, as the chart is.
    /// </remarks>
    internal static string RowSelectionScript => $$"""
        <script type="text/ecmascript"><![CDATA[
        (function() {
        {{SvgScriptPreamble.Root}}
            var group = svg.getAttribute('data-selection-group');
            if (!group || typeof window === 'undefined' || !window.addEventListener) return;

            function marks() { return svg.querySelectorAll('[data-row]'); }

            function apply(rows) {
                var wanted = {};
                for (var i = 0; i < rows.length; i++) wanted[rows[i]] = true;
                var any = rows.length > 0;
                var list = marks();
                for (var j = 0; j < list.length; j++) {
                    var mark = list[j];
                    if (!any) {
                        mark.removeAttribute('aria-selected');
                        mark.style.opacity = '';
                        continue;
                    }
                    var on = wanted[mark.getAttribute('data-row')] === true;
                    mark.setAttribute('aria-selected', on ? 'true' : 'false');
                    mark.style.opacity = on ? '' : '0.25';
                }
            }

            function rowsIn(r) {
                var out = [];
                var list = marks();
                for (var i = 0; i < list.length; i++) {
                    var b = list[i].getBBox();
                    var cx = b.x + b.width / 2, cy = b.y + b.height / 2;
                    if (cx >= r.x1 && cx <= r.x2 && cy >= r.y1 && cy <= r.y2) out.push(list[i].getAttribute('data-row'));
                }
                return out;
            }

            function publish(rows) {
                window.dispatchEvent(new CustomEvent('mpl:rows', { detail: { group: group, rows: rows } }));
            }

            function onRows(e) {
                if (!svg.isConnected) { window.removeEventListener('mpl:rows', onRows); return; }
                var d = e && e.detail;
                if (!d || d.group !== group || !d.rows) return;
                apply(d.rows);
            }

            window.addEventListener('mpl:rows', onRows);
            svg.addEventListener('mpl:selection', function(e) { if (e.detail) publish(rowsIn(e.detail)); });
            svg.addEventListener('keydown', function(e) { if (e.key === 'Escape') publish([]); });

            var initial = svg.getAttribute('data-selected-rows');
            if (initial) { try { apply(JSON.parse(initial)); } catch (_) {} }
        })();
        ]]></script>
        """;
}
