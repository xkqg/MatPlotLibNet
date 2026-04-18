// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>Provides the embedded JavaScript for interactive treemap drilldown navigation.</summary>
internal static class SvgTreemapDrilldownScript
{
    /// <summary>
    /// Returns a <c>&lt;script&gt;</c> block that makes treemap rectangles clickable for zoom-in
    /// navigation. Clicking a rect animates the SVG root <c>viewBox</c> to the bounds of the
    /// clicked node, progressively revealing its children at full size. The drill path is kept
    /// in a JS stack so <kbd>Escape</kbd> (or clicking the breadcrumb) pops back one level.
    /// </summary>
    /// <remarks>
    /// <para>Rectangles are identified by the <c>data-treemap-node</c> attribute emitted by
    /// <see cref="SeriesRenderers.TreemapSeriesRenderer"/> when the axes has
    /// <see cref="Models.Axes.EnableInteractiveAttributes"/> set. Each rect carries
    /// <c>data-treemap-depth</c> and <c>data-treemap-parent</c> for navigation.</para>
    ///
    /// <para>The viewBox-based zoom keeps all SVG elements at their original coordinates — no
    /// per-element transform math, no child reselection. The browser handles the smooth
    /// transition via CSS.</para>
    ///
    /// <para>Keyboard: <kbd>Enter</kbd>/<kbd>Space</kbd> on a focused rect drills in;
    /// <kbd>Escape</kbd> zooms out one level. ARIA: each rect receives <c>role="button"</c>,
    /// <c>tabindex="0"</c>, and <c>aria-label</c> built from the node's label + depth.</para>
    /// </remarks>
    internal static string GetScript() => """
        <script type="text/ecmascript"><![CDATA[
        (function() {
            // Per-chart isolation (Phase 2): self-locate via document.currentScript.
            var svg = (document.currentScript && document.currentScript.parentNode) || null;
            if (!svg || svg.tagName !== 'svg') {
                // Fallback for inline-runner environments without currentScript: walk from first
                // tagged rect via ownerSVGElement (the v1.7.1 behaviour).
                var probe = document.querySelectorAll('[data-treemap-node]');
                if (probe.length === 0) return;
                svg = probe[0].ownerSVGElement;
                if (!svg) return;
            }
            var rects = svg.querySelectorAll('[data-treemap-node]');
            if (rects.length === 0) return;
            var initialViewBox = svg.getAttribute('viewBox');
            svg.style.transition = 'all 0.35s ease-out';

            // Drill stack: each entry is the viewBox string we were at BEFORE drilling.
            var stack = [];

            // Phase 12 of v1.7.2 plan — UX hint when the user has drilled at least once.
            // The hint is a small <text> element appended to the SVG; toggleHint() shows/hides
            // it based on whether the drill stack is non-empty. Without this, users have no
            // visual cue that Escape exits the drill — many never discover it.
            var hint = null;
            function ensureHint() {
                if (hint) return;
                hint = document.createElementNS('http://www.w3.org/2000/svg', 'text');
                hint.setAttribute('x', '8');
                hint.setAttribute('y', '14');
                hint.setAttribute('font-family', 'sans-serif');
                hint.setAttribute('font-size', '11');
                hint.setAttribute('fill', '#555');
                hint.setAttribute('aria-live', 'polite');
                hint.setAttribute('data-mpl-treemap-hint', 'true');
                hint.textContent = 'Press Esc to zoom out';
                svg.appendChild(hint);
            }
            function toggleHint() {
                if (stack.length === 0 && hint) { hint.style.display = 'none'; return; }
                if (stack.length > 0) { ensureHint(); hint.style.display = 'block'; }
            }

            function drill(rect) {
                var x = parseFloat(rect.getAttribute('x'));
                var y = parseFloat(rect.getAttribute('y'));
                var w = parseFloat(rect.getAttribute('width'));
                var h = parseFloat(rect.getAttribute('height'));
                if (!(w > 0) || !(h > 0)) return;
                stack.push(svg.getAttribute('viewBox'));
                svg.setAttribute('viewBox', x + ' ' + y + ' ' + w + ' ' + h);
                toggleHint();
            }

            function zoomOut() {
                var prev = stack.pop();
                if (prev === undefined || prev === null) {
                    if (initialViewBox) svg.setAttribute('viewBox', initialViewBox);
                    toggleHint();
                    return;
                }
                svg.setAttribute('viewBox', prev);
                toggleHint();
            }

            rects.forEach(function(rect) {
                rect.style.cursor = 'pointer';
                rect.setAttribute('tabindex', '0');
                rect.setAttribute('role', 'button');
                var label = rect.getAttribute('data-treemap-node') || '';
                var depth = rect.getAttribute('data-treemap-depth') || '';
                rect.setAttribute('aria-label', 'Drill into ' + label + ' (depth ' + depth + ')');

                rect.addEventListener('click', function(e) {
                    drill(rect);
                    e.stopPropagation();
                });
                rect.addEventListener('keydown', function(e) {
                    if (e.key === 'Enter' || e.key === ' ') {
                        e.preventDefault();
                        drill(rect);
                    } else if (e.key === 'Escape') {
                        e.preventDefault();
                        zoomOut();
                    }
                });
            });

            // Global Escape anywhere in the document pops the drill stack.
            document.addEventListener('keydown', function(e) {
                if (e.key === 'Escape') zoomOut();
            });
        })();
        ]]></script>
        """;
}
