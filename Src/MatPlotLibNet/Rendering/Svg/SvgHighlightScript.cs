// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>Provides the embedded JavaScript for series highlight-on-hover in SVG output.</summary>
internal static class SvgHighlightScript
{
    /// <summary>
    /// Returns a <c>&lt;script&gt;</c> block that dims sibling series groups (to 30% opacity)
    /// when the cursor enters a group with a <c>data-series-index</c> attribute, restoring all on mouse-leave.
    /// </summary>
    internal static string GetScript() => """
        <script type="text/ecmascript"><![CDATA[
        (function() {
            var groups = document.querySelectorAll('[data-series-index]');
            groups.forEach(function(g) {
                g.addEventListener('mouseenter', function() {
                    groups.forEach(function(s) {
                        s.style.opacity = s === g ? '1' : '0.3';
                    });
                });
                g.addEventListener('mouseleave', function() {
                    groups.forEach(function(s) { s.style.opacity = '1'; });
                });
            });
        })();
        ]]></script>
        """;
}
