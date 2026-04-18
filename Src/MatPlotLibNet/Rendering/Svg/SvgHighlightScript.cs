// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>Provides the embedded JavaScript for series highlight-on-hover in SVG output.</summary>
internal static class SvgHighlightScript
{
    /// <summary>
    /// Returns a <c>&lt;script&gt;</c> block that dims sibling series groups (to 30% opacity)
    /// when the cursor enters (or keyboard focus reaches) a group with a <c>data-series-index</c> attribute,
    /// restoring all on mouse-leave or blur.
    /// Each series group receives <c>tabindex="0"</c> for keyboard reachability.
    /// </summary>
    internal static string GetScript() => """
        <script type="text/ecmascript"><![CDATA[
        (function() {
            // Per-chart isolation (Phase 2): scope query to THIS script's owning <svg>.
            var svg = (document.currentScript && document.currentScript.parentNode) || document;
            // Phase 7 of v1.7.2 plan — read themable opacity from data-mpl-highlight-opacity
            // (set by FigureBuilder.WithInteractionTheme). Default 0.3 preserves v1.7.1 behaviour.
            var dimOpacity = parseFloat(svg.getAttribute && svg.getAttribute('data-mpl-highlight-opacity')) || 0.3;
            var groups = svg.querySelectorAll('[data-series-index]');
            groups.forEach(function(g) {
                g.setAttribute('tabindex', '0');

                function highlight() {
                    groups.forEach(function(s) {
                        s.style.opacity = s === g ? '1' : String(dimOpacity);
                    });
                }
                function restore() {
                    // Phase 8 of v1.7.2 plan — restore each element to its data-mpl-opacity-base
                    // (captured below on first dim) instead of forcing 1.0. Preserves explicit
                    // opacity that callers set via series.Alpha or CSS overrides.
                    groups.forEach(function(s) {
                        var base = s.getAttribute('data-mpl-opacity-base');
                        s.style.opacity = base !== null ? base : '1';
                    });
                }
                // Capture original opacity once (lazy on first highlight) so restore is correct
                // even when a series has explicit opacity="0.5" before any hover happens.
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
}
