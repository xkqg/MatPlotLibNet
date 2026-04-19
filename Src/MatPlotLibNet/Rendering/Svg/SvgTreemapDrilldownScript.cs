// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>Provides the embedded JavaScript for interactive treemap expand/collapse.</summary>
internal static class SvgTreemapDrilldownScript
{
    /// <summary>
    /// Returns a <c>&lt;script&gt;</c> block that makes every NON-LEAF treemap node
    /// clickable to collapse / restore its entire subtree. Default state on first paint
    /// is everything-expanded (interactive view = static SVG, "steady pictures" — no
    /// visual jump entering interactive mode). Multiple subtrees can be collapsed
    /// independently.
    /// </summary>
    /// <remarks>
    /// <para>The renderer emits all nodes at all depths in the static SVG, tagged with
    /// <c>data-treemap-node</c> (path id like "0.1") and <c>data-treemap-parent</c>
    /// (parent's id). On script init, every parent's <c>expanded</c> flag is set to
    /// true so every rect renders. A click on a parent flips its flag to false; visibility
    /// is then recomputed by an <em>ancestry walk</em> — an element is visible iff every
    /// ancestor up to root is expanded — so collapsing a top-level rect transitively hides
    /// everything beneath it in one click.</para>
    ///
    /// <para>Click dispatch: the zoom/pan script's <c>setPointerCapture</c> redirects
    /// the synthetic <c>click</c> to the SVG root, so this script uses event delegation
    /// at the root plus <c>elementFromPoint</c> as a fallback for the captured case.
    /// Drag-suppression (pointermove threshold &gt; 5px while button down) avoids
    /// toggling when the user was panning.</para>
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
            // Also build parentOf[id] so applyVisibility can walk ancestors transitively —
            // collapsing an interior node must hide its ENTIRE subtree, not just its direct
            // children (Phase W follow-up, 2026-04-19 — exposed by the Playwright T4 repro:
            // clicking root collapsed depth-1 but depth-2+ stayed visible because their own
            // parent's expanded flag was still true).
            var hasChildren = {};
            var parentOf = {};
            all.forEach(function(el) {
                var id = el.getAttribute('data-treemap-node');
                var p = el.getAttribute('data-treemap-parent');
                if (p) hasChildren[p] = true;
                // First occurrence wins — an id may appear on both rect and text; both share
                // the same parent so either is authoritative.
                if (parentOf[id] === undefined) parentOf[id] = p;
            });

            // Expansion state keyed by node id. Phase W follow-up (2026-04-19, "steady
            // pictures"): start with EVERY parent expanded so the initial interactive
            // render is pixel-identical to the static SVG ("user sees all" in both
            // modes — z-order means deeper labels paint over shallower ones, so the
            // visible label in any region is always the deepest one). Click then
            // becomes opt-in collapse: the user hides a subtree to focus, rather than
            // having to expand to discover content. No visual jump on first paint;
            // movement only when the user explicitly clicks. Regressions:
            //   TreemapDrilldownTests.RootRect_IsVisible_OnInitialState
            //   TreemapDrilldownTests.InitialState_AllParentsExpanded
            var expanded = { '': true };
            all.forEach(function(el) {
                var id = el.getAttribute('data-treemap-node');
                if (hasChildren[id]) expanded[id] = true;
            });

            // Element visible iff EVERY ancestor (its parent, grandparent, ... up to root)
            // is in expanded[]. Walking ancestors makes hide transitive: collapsing root
            // hides depth-1, depth-2 and depth-3 in one shot. Cost: O(depth) per element
            // per applyVisibility(); trivial for any real treemap (depth typically < 10).
            // Pre-fix the script only checked the immediate parent, leaving depth-2+
            // visible after collapsing a depth-1 ancestor (Playwright T4 regression).
            function isAncestryOpen(id) {
                var p = parentOf[id];
                while (p !== undefined && p !== '') {
                    if (!expanded[p]) return false;
                    p = parentOf[p];
                }
                return expanded[''] !== false;
            }
            function applyVisibility() {
                all.forEach(function(el) {
                    var id = el.getAttribute('data-treemap-node');
                    el.style.display = isAncestryOpen(id) ? '' : 'none';
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

            // Drag-vs-click suppression — if the pointer moved more than 5px BETWEEN
            // pointerdown and pointerup, treat as pan and skip the click toggle.
            // The isPointerDown gate is essential: without it, plain hover (pointermove
            // with no button held) would latch pointerMoved=true on the very first
            // mouse motion across the chart, then the click handler's
            // `if (pointerMoved) return;` would suppress every subsequent click.
            // Regression test: TreemapDrilldownTests.HoverWithoutButtonDown_DoesNotPoisonClickHandler
            // (bug shipped in v1.7.2 / Phase P, surfaced 2026-04-19).
            var pointerDownX = 0, pointerDownY = 0, pointerMoved = false, isPointerDown = false;
            svg.addEventListener('pointerdown', function(e) {
                pointerDownX = e.clientX; pointerDownY = e.clientY;
                pointerMoved = false; isPointerDown = true;
            }, true);
            svg.addEventListener('pointermove', function(e) {
                if (!isPointerDown) return;             // hover does NOT poison the flag
                var dx = e.clientX - pointerDownX, dy = e.clientY - pointerDownY;
                if (dx*dx + dy*dy > 25) pointerMoved = true;
            }, true);
            svg.addEventListener('pointerup', function(e) { isPointerDown = false; }, true);
            svg.addEventListener('pointercancel', function(e) { isPointerDown = false; }, true);

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
            // redirects pointerup AND the synthetic click to the SVG root rather than the
            // rect under the cursor. Two-stage target resolution:
            //   1. Walk up from e.target — works for unredirected clicks (most browsers
            //      when no setPointerCapture is in flight, or when capture is on a child).
            //   2. Fallback: hit-test (clientX, clientY) via document.elementFromPoint —
            //      recovers the real rect when capture redirected the target to <svg>.
            // Regression tests: TreemapDrilldownTests.HoverWithoutButtonDown_…
            //                  TreemapDrilldownTests.Click_RedirectedToSvgRoot_FallsBackTo_…
            svg.addEventListener('click', function(e) {
                if (pointerMoved) return;
                var target = findTreemapNode(e.target);
                if (!target && document.elementFromPoint) {
                    var hit = document.elementFromPoint(e.clientX, e.clientY);
                    target = findTreemapNode(hit);
                }
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
