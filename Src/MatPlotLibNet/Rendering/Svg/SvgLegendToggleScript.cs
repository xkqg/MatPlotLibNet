// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>Embedded JavaScript for click-to-toggle legend items in SVG output.
/// Includes ARIA attributes (role="button", aria-pressed, tabindex) for accessibility.
/// Also supports keyboard activation (Enter/Space) per WCAG 2.1.</summary>
internal static class SvgLegendToggleScript
{
    internal static string GetScript() => """
        <script type="text/ecmascript"><![CDATA[
        (function() {
            'use strict';
            // Per-chart isolation (Phase 2): find THIS script's owning <svg> via
            // document.currentScript.parentNode so multi-chart pages don't cross-talk.
            // Fallback to document.querySelector('svg') only for inline-runner environments
            // where currentScript is unavailable (e.g. some test hosts).
            var svg = (document.currentScript && document.currentScript.parentNode) || document.querySelector('svg');
            if (!svg || svg.tagName !== 'svg') return;
            var legendItems = svg.querySelectorAll('[data-legend-index]');
            legendItems.forEach(function(item) {
                item.style.cursor = 'pointer';
                item.setAttribute('tabindex', '0');
                item.setAttribute('role', 'button');
                item.setAttribute('aria-pressed', 'false');

                function toggle() {
                    var idx = item.getAttribute('data-legend-index');
                    var series = svg.querySelectorAll('[data-series-index="' + idx + '"]');
                    var hidden = false;
                    series.forEach(function(s) {
                        hidden = s.style.display === 'none';
                        s.style.display = hidden ? '' : 'none';
                    });
                    item.style.opacity = hidden ? '1' : '0.3';
                    item.setAttribute('aria-pressed', hidden ? 'false' : 'true');
                }

                // Phase S — toggle fires ONLY on a real click (full press-and-release without
                // drag). Pre-fix the script also fired toggle on pointerdown which (a) hid the
                // series the instant the user pressed (single-series charts visibly emptied —
                // user-reported "plot disappears" bug, 2026-04-19) and (b) made drag-to-reposition
                // mechanically impossible because the data vanished before drag could engage. The
                // companion SvgLegendDragScript suppresses the synthetic click that follows a
                // real drag via capture-phase stopPropagation, so toggle-on-click coexists cleanly
                // with drag.
                item.addEventListener('click', function() { toggle(); });
                item.addEventListener('keydown', function(e) {
                    if (e.key === 'Enter' || e.key === ' ') { toggle(); e.preventDefault(); }
                });
            });
        })();
        ]]></script>
        """;
}
