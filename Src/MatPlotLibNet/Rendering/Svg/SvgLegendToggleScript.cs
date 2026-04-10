// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>Provides the embedded JavaScript for SVG legend-toggle interactivity.</summary>
internal static class SvgLegendToggleScript
{
    /// <summary>
    /// Returns a <c>&lt;script&gt;</c> block that makes clicking a legend entry toggle
    /// the corresponding series group's visibility. Legend entries carry <c>data-legend-index</c>
    /// and series groups carry <c>data-series-index</c>.
    /// </summary>
    internal static string GetScript() => """
        <script type="text/ecmascript"><![CDATA[
        (function() {
            document.querySelectorAll('[data-legend-index]').forEach(function(entry) {
                entry.addEventListener('click', function() {
                    var idx = entry.getAttribute('data-legend-index');
                    var seriesGroup = document.querySelector('[data-series-index="' + idx + '"]');
                    if (!seriesGroup) return;
                    var hidden = seriesGroup.style.display === 'none';
                    seriesGroup.style.display = hidden ? '' : 'none';
                    entry.style.opacity = hidden ? '1' : '0.4';
                });
            });
        })();
        ]]></script>
        """;
}
