// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

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
            document.querySelectorAll('g > title').forEach(function(title) {
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
                parent.addEventListener('focus', function() { showTip(0, 0); });
                parent.addEventListener('blur', hideTip);
            });
        })();
        ]]></script>
        """;
}
