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

                // Phase 4 — pointerdown handles touch + pen + mouse uniformly. Click stays
                // wired for legacy environments and as the test-fire path, but is suppressed
                // when it directly follows a pointerdown (which synthesizes click as a sequel).
                var pointerActivated = false;
                item.addEventListener('pointerdown', function(e) {
                    toggle();
                    pointerActivated = true;
                    e.preventDefault();
                });
                item.addEventListener('click', function() {
                    if (pointerActivated) { pointerActivated = false; return; }
                    toggle();
                });
                item.addEventListener('keydown', function(e) {
                    if (e.key === 'Enter' || e.key === ' ') { toggle(); e.preventDefault(); }
                });
            });
        })();
        ]]></script>
        """;
}
