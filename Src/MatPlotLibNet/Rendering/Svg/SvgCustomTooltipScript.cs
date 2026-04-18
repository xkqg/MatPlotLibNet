// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>Provides the embedded JavaScript for styled HTML tooltip overlays in SVG output.</summary>
internal static class SvgCustomTooltipScript
{
    /// <summary>
    /// Returns a <c>&lt;script&gt;</c> block that intercepts elements containing a <c>&lt;title&gt;</c>
    /// child and displays a styled floating tooltip div instead of the native browser tooltip.
    /// The tooltip div carries <c>role="tooltip"</c> and <c>aria-live="polite"</c>.
    /// Focus/blur on the parent element mirrors mouseover/mouseout for keyboard accessibility.
    /// </summary>
    internal static string GetScript() => """
        <script type="text/ecmascript"><![CDATA[
        (function() {
            var tip = document.createElement('div');
            tip.setAttribute('role', 'tooltip');
            tip.setAttribute('aria-live', 'polite');
            tip.style.cssText = 'position:fixed;background:#333;color:#fff;padding:4px 8px;border-radius:4px;font-size:12px;pointer-events:none;display:none;z-index:9999;white-space:pre';
            document.body.appendChild(tip);
            // Per-chart isolation (Phase 2): only attach to titles inside THIS script's owning <svg>.
            var svg = (document.currentScript && document.currentScript.parentNode) || document;
            svg.querySelectorAll('g > title').forEach(function(title) {
                var parent = title.parentNode;
                function showTip(x, y) {
                    tip.textContent = title.textContent;
                    tip.style.display = 'block';
                    tip.style.left = (x + 12) + 'px';
                    tip.style.top  = (y - 4)  + 'px';
                }
                function hideTip() { tip.style.display = 'none'; }

                parent.addEventListener('mouseover', function(e) { showTip(e.clientX, e.clientY); });
                parent.addEventListener('mousemove', function(e) {
                    tip.style.left = (e.clientX + 12) + 'px';
                    tip.style.top  = (e.clientY - 4)  + 'px';
                });
                parent.addEventListener('mouseout', hideTip);
                // Phase 12 of v1.7.2 plan — focus tooltip uses the focused element's bounds,
                // not (0, 0). Pre-Phase-12 the tooltip jumped to the top-left corner of the
                // viewport on keyboard focus, often off-screen and inaccessible.
                parent.addEventListener('focus', function(e) {
                    var rect = (e.target.getBoundingClientRect && e.target.getBoundingClientRect()) || {left: 0, top: 0, width: 0, height: 0};
                    showTip(rect.left + rect.width / 2, rect.top - 4);
                });
                parent.addEventListener('blur', hideTip);
            });
        })();
        ]]></script>
        """;
}
