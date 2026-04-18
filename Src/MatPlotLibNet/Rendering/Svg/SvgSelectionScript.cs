// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>Provides the embedded JavaScript for rectangular data selection in SVG output.</summary>
internal static class SvgSelectionScript
{
    /// <summary>
    /// Returns a <c>&lt;script&gt;</c> block that activates a selection rectangle on Shift+mousedown.
    /// On mouseup, a <c>CustomEvent('mpl:selection', { detail: { x1, y1, x2, y2 } })</c> is dispatched
    /// on the SVG element, allowing host applications to react to user-defined data regions.
    /// <kbd>Escape</kbd> cancels an active selection without dispatching the event.
    /// The selection rect carries <c>aria-label="Data selection area"</c>.
    /// </summary>
    internal static string GetScript() => """
        <script type="text/ecmascript"><![CDATA[
        (function() {
            // Per-chart isolation (Phase 2): self-locate via document.currentScript.
            var svg = (document.currentScript && document.currentScript.parentNode) || document.querySelector('svg');
            if (!svg || svg.tagName !== 'svg') return;
            var rect = null, startPt = null;
            // Phase 4 — Pointer Events for touch + pen + mouse. setPointerCapture binds
            // the pointer to the SVG so dragging out of the chart still releases cleanly.
            function startSelection(e) {
                if (!e.shiftKey) return;
                e.preventDefault();
                if (svg.setPointerCapture && e.pointerId !== undefined)
                    try { svg.setPointerCapture(e.pointerId); } catch (_) {}
                var pt = svg.createSVGPoint();
                pt.x = e.clientX; pt.y = e.clientY;
                startPt = pt.matrixTransform(svg.getScreenCTM().inverse());
                rect = document.createElementNS('http://www.w3.org/2000/svg', 'rect');
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
                var pt = svg.createSVGPoint();
                pt.x = e.clientX; pt.y = e.clientY;
                var cur = pt.matrixTransform(svg.getScreenCTM().inverse());
                var x = Math.min(startPt.x, cur.x), y = Math.min(startPt.y, cur.y);
                var w = Math.abs(cur.x - startPt.x), h = Math.abs(cur.y - startPt.y);
                rect.setAttribute('x', x); rect.setAttribute('y', y);
                rect.setAttribute('width', w); rect.setAttribute('height', h);
            }
            function endSelection(e) {
                if (!rect || !startPt) return;
                var pt = svg.createSVGPoint();
                pt.x = e.clientX; pt.y = e.clientY;
                var endPt = pt.matrixTransform(svg.getScreenCTM().inverse());
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
            // Mouse fallback for pre-pointer-events runtimes.
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
}
