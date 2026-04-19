// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>Embedded JavaScript that lets the user drag the legend group to any position on the
/// chart. Press-and-hold any legend item, drag the cursor, release to drop. The translation is
/// applied client-side as a <c>transform="translate(dx,dy)"</c> attribute on the parent
/// <c>&lt;g class="legend"&gt;</c>; it is NOT persisted across server-side re-renders.</summary>
/// <remarks>
/// <para>Phase S (2026-04-19). Coexists with <see cref="SvgLegendToggleScript"/> via DOM-level
/// coordination, not shared state:</para>
/// <list type="bullet">
///   <item><description><b>Toggle vs drag</b>: pointerdown does NOT toggle (Phase S fix in toggle
///     script). Drag tracks pointer movement; once it crosses the 5-px² threshold, the synthetic
///     click that follows pointerup gets suppressed via a one-shot capture-phase
///     <c>stopPropagation</c> listener. Click without drag falls through to toggle.</description></item>
///   <item><description><b>Pan/zoom vs drag</b>: drag listeners run in capture phase on the legend
///     items themselves; once a drag is engaged we <c>stopPropagation</c> on each pointermove so
///     the SVG-root pan handler cannot also pan the chart while the user is repositioning the
///     legend.</description></item>
///   <item><description><b>Hover vs drag</b> (Phase R lesson): the pointermove handler bails when
///     <c>isDown</c> is false, so hovering a legend item does not latch any state.</description></item>
///   <item><description><b>Pointer capture</b>: <c>setPointerCapture</c> is wrapped in try/catch
///     and called on the item that received pointerdown — keeps subsequent pointermove/pointerup
///     events targeted at that item even if the cursor leaves the legend's bbox during the
///     drag.</description></item>
/// </list>
/// <para><b>Harness gap</b>: the test harness (<see cref="MatPlotLibNet.Tests.Rendering.Svg.Interaction.InteractionScriptHarness"/>)
/// does not honour capture-phase ordering, so the drag-suppresses-click behaviour cannot be
/// pinned in xUnit; that contract is verified end-to-end by the Phase S Playwright harness
/// (<c>c:/tmp/legend_repro.py</c>). The xUnit-pinnable behaviours — translate-on-drag, threshold,
/// hover-no-poison, accumulate-across-drops — are covered by
/// <see cref="MatPlotLibNet.Tests.Rendering.Svg.Interaction.LegendDragTests"/>.</para>
/// </remarks>
internal static class SvgLegendDragScript
{
    internal static string GetScript() => """
        <script type="text/ecmascript"><![CDATA[
        (function() {
            'use strict';
            var svg = (document.currentScript && document.currentScript.parentNode) || document.querySelector('svg');
            if (!svg || svg.tagName !== 'svg') return;
            var legend = svg.querySelector('g.legend');
            if (!legend) return;

            // Shared drag state across all legend-item listeners — only one finger drags at a time.
            var dx = 0, dy = 0;                        // current accumulated translate
            var startX = 0, startY = 0;                // pointer at mousedown
            var startDx = 0, startDy = 0;              // translate at mousedown
            var isDown = false, dragged = false;
            var THRESH2 = 25;                          // 5-px² (Phase R parity)

            function setTransform() {
                legend.setAttribute('transform', 'translate(' + dx + ',' + dy + ')');
            }

            // Per-item closures (Jint test harness doesn't bind `this` to the element on
            // listener invocation, so we capture `item` from the forEach closure instead).
            var items = svg.querySelectorAll('[data-legend-index]');
            items.forEach(function(item) {
                item.style.cursor = 'grab';
                // Suppress native text-selection during drag — without this the browser
                // interprets the drag as a select-text gesture and the legend label gets
                // visually highlighted while the user is moving the legend.
                item.style.userSelect = 'none';
                item.style.webkitUserSelect = 'none';
                item.addEventListener('pointerdown', function(e) {
                    isDown = true; dragged = false;
                    startX = e.clientX; startY = e.clientY;
                    startDx = dx; startDy = dy;
                    if (item.setPointerCapture) try { item.setPointerCapture(e.pointerId); } catch (_) {}
                    legend.style.cursor = 'grabbing';
                    e.preventDefault();   // belt-and-braces: also blocks text-selection start
                    // Prevent the pan/zoom script's bubble-phase pointerdown listener from
                    // also calling svg.setPointerCapture(e.pointerId) — last-call-wins on
                    // pointer capture would otherwise redirect every subsequent pointermove
                    // and the synthetic click to the SVG root, never reaching the legend
                    // item and breaking both drag tracking and the click-to-toggle handoff.
                    e.stopPropagation();
                }, true);
                item.addEventListener('pointermove', function(e) {
                    if (!isDown) return;               // hover does NOT poison
                    var ddx = e.clientX - startX, ddy = e.clientY - startY;
                    if (!dragged && ddx*ddx + ddy*ddy > THRESH2) dragged = true;
                    if (dragged) {
                        dx = startDx + ddx; dy = startDy + ddy;
                        setTransform();
                        e.stopPropagation();           // suppress pan/zoom while dragging legend
                    }
                }, true);
                item.addEventListener('pointerup', function(e) {
                    if (!isDown) return;
                    isDown = false;
                    legend.style.cursor = 'grab';
                    if (dragged) {
                        // Suppress the synthetic click that the browser fires after pointerup —
                        // otherwise the toggle handler would hide the series at drag-drop.
                        var swallow = function(ev) {
                            ev.stopPropagation();
                            item.removeEventListener('click', swallow, true);
                        };
                        item.addEventListener('click', swallow, true);
                    }
                }, true);
                item.addEventListener('pointercancel', function() {
                    if (isDown) { isDown = false; legend.style.cursor = 'grab'; }
                }, true);
            });
        })();
        ]]></script>
        """;
}
