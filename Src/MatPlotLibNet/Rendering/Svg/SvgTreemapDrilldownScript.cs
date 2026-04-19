// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>Provides the embedded JavaScript for interactive treemap expand/collapse.</summary>
internal static class SvgTreemapDrilldownScript
{
    /// <summary>
    /// Returns a <c>&lt;script&gt;</c> block that makes every NON-LEAF treemap node
    /// clickable to toggle visibility of its direct children. Multiple parents can be
    /// expanded at once — the interaction model is expand/collapse (file-tree style),
    /// not single-path drill-zoom.
    /// </summary>
    /// <remarks>
    /// <para>The renderer emits all nodes at all depths in the static SVG, tagged with
    /// <c>data-treemap-node</c> (path id like "0.1") and <c>data-treemap-parent</c>
    /// (parent's id). On script init, every rect + text whose parent is not root "0"
    /// is hidden via CSS. Clicking a parent's rect shows all nodes whose
    /// <c>data-treemap-parent</c> equals that parent's id — i.e., the parent's direct
    /// children. Clicking again hides them.</para>
    ///
    /// <para>Click dispatch: the zoom/pan script's <c>setPointerCapture</c> redirects
    /// the synthetic <c>click</c> to the SVG root, so this script uses event delegation
    /// at the root plus <c>elementFromPoint</c> as a fallback for the captured case.
    /// Drag-suppression (pointermove threshold &gt; 5px) avoids toggling when the user
    /// was panning.</para>
    /// </remarks>
    internal static string GetScript() => """
        <script type="text/ecmascript"><![CDATA[
        (function() {
            var svg = (document.currentScript && document.currentScript.parentNode) || null;
            if (!svg || svg.tagName !== 'svg') {
                var probe = document.querySelectorAll('[data-treemap-node]');
                if (probe.length === 0) return;
                svg = probe[0].ownerSVGElement;
                if (!svg) return;
            }
            var all = Array.prototype.slice.call(svg.querySelectorAll('[data-treemap-node]'));
            if (all.length === 0) return;

            // Classify: which nodes are rects (clickable parents potentially) vs text labels,
            // and which have children (non-leaves — clicking these toggles their children).
            var hasChildren = {};
            all.forEach(function(el) {
                var p = el.getAttribute('data-treemap-parent');
                if (p) hasChildren[p] = true;
            });

            // Expansion state keyed by node id. Initially: only direct children of the
            // root ("0") are visible. Their children (depth ≥ 2) are hidden until the
            // user expands their parent by clicking.
            var expanded = { '0': true };
            function applyVisibility() {
                all.forEach(function(el) {
                    var p = el.getAttribute('data-treemap-parent');
                    // Element is visible iff its parent's expanded flag is true.
                    el.style.display = expanded[p] ? '' : 'none';
                });
            }
            applyVisibility();

            // Style: parents (nodes that have children) get a pointer cursor on BOTH the
            // rect AND the label text — pre-fix only the rect did, so the cursor reverted
            // to default the moment the user moved over the parent's label, making the
            // tile look unclickable mid-hover. Leaves get the default cursor either way.
            all.forEach(function(el) {
                var id = el.getAttribute('data-treemap-node');
                if (hasChildren[id]) el.style.cursor = 'pointer';
            });

            function toggle(id) {
                if (!hasChildren[id]) return;            // leaves aren't toggleable
                expanded[id] = !expanded[id];
                applyVisibility();
            }

            // Drag-vs-click suppression — if the pointer moved more than 5px, treat as pan.
            var pointerDownX = 0, pointerDownY = 0, pointerMoved = false;
            svg.addEventListener('pointerdown', function(e) {
                pointerDownX = e.clientX; pointerDownY = e.clientY; pointerMoved = false;
            }, true);
            svg.addEventListener('pointermove', function(e) {
                var dx = e.clientX - pointerDownX, dy = e.clientY - pointerDownY;
                if (dx*dx + dy*dy > 25) pointerMoved = true;
            }, true);

            // Walk-up-from-target — avoids depending on Element.closest() or
            // document.elementFromPoint(), which the Jint test harness doesn't stub.
            // Works identically in the browser (standard parentNode traversal).
            function findTreemapNode(el) {
                while (el) {
                    if (el.getAttribute && el.getAttribute('data-treemap-node')) return el;
                    el = el.parentNode;
                }
                return null;
            }

            // Event delegation at the SVG root — the pan/zoom script's setPointerCapture
            // redirects the click to the SVG, so per-rect click listeners wouldn't fire.
            svg.addEventListener('click', function(e) {
                if (pointerMoved) return;
                var target = findTreemapNode(e.target);
                if (!target) return;
                var id = target.getAttribute('data-treemap-node');
                if (hasChildren[id]) {
                    toggle(id);
                    e.stopPropagation();
                }
            });
        })();
        ]]></script>
        """;
}
