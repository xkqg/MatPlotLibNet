// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>Provides the embedded JavaScript for styled HTML tooltip overlays in SVG output.</summary>
internal static class SvgCustomTooltipScript
{
    /// <summary>
    /// Returns a <c>&lt;script&gt;</c> block that intercepts elements containing a <c>&lt;title&gt;</c>
    /// child and displays a styled floating tooltip div instead of the native browser tooltip.
    /// </summary>
    internal static string GetScript() => """
        <script type="text/ecmascript"><![CDATA[
        (function() {
            var tip = document.createElement('div');
            tip.style.cssText = 'position:fixed;background:#333;color:#fff;padding:4px 8px;border-radius:4px;font-size:12px;pointer-events:none;display:none;z-index:9999;white-space:pre';
            document.body.appendChild(tip);
            document.querySelectorAll('g > title').forEach(function(title) {
                var parent = title.parentNode;
                parent.addEventListener('mouseover', function(e) {
                    tip.textContent = title.textContent;
                    tip.style.display = 'block';
                    tip.style.left = (e.clientX + 12) + 'px';
                    tip.style.top  = (e.clientY - 4)  + 'px';
                });
                parent.addEventListener('mousemove', function(e) {
                    tip.style.left = (e.clientX + 12) + 'px';
                    tip.style.top  = (e.clientY - 4)  + 'px';
                });
                parent.addEventListener('mouseout', function() {
                    tip.style.display = 'none';
                });
            });
        })();
        ]]></script>
        """;
}
