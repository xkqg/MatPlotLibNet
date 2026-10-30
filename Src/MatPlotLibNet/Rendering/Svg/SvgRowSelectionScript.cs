// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>Provides the embedded JavaScript that links the selection of the charts of one group on a page.</summary>
internal static class SvgRowSelectionScript
{
    /// <summary>
    /// Returns a <c>&lt;script&gt;</c> block that links the chart to the others of its selection group
    /// (<c>data-selection-group</c> on the root). When the chart's brush ends (<c>mpl:selection</c> on the SVG) it works out which
    /// marks the rectangle covers, those that carry <c>data-row</c> and whose middle lies inside it, and says so to the window as
    /// <c>CustomEvent('mpl:rows', { detail: { group, rows } })</c>; every chart of that group, this one included, lights those
    /// rows (<c>aria-selected="true"</c>, the rest <c>"false"</c> and dimmed) and a chart of another group hears nothing.
    /// <kbd>Escape</kbd> on the chart clears the selection of the whole group. The rows in <c>data-selected-rows</c> are lit when
    /// the script starts. A chart that is mounted again leaves no second listener: the listener of a chart that is no longer in
    /// the document removes itself the first time the window speaks. A window event, not a channel between tabs: the link is the
    /// page's, as the chart is.
    /// </summary>
    internal static string GetScript() => """
        <script type="text/ecmascript"><![CDATA[
        (function() {
            // Per-chart isolation: self-locate via document.currentScript.
            var svg = (document.currentScript && document.currentScript.parentNode) || document.querySelector('svg');
            if (!svg || svg.tagName !== 'svg') return;
            var group = svg.getAttribute('data-selection-group');
            if (!group || typeof window === 'undefined' || !window.addEventListener) return;

            function marks() { return svg.querySelectorAll('[data-row]'); }

            // Lights the rows and dims the rest; no rows lights nothing and dims nothing.
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

            // The rows whose marks have their middle inside the rectangle (in the SVG's own units).
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
                // A chart that has been replaced is no longer in the document: its listener goes, and the new chart has its own.
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
