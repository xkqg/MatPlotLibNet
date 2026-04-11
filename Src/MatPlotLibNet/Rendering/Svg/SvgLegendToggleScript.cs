// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>Provides the embedded JavaScript for SVG legend-toggle interactivity.</summary>
internal static class SvgLegendToggleScript
{
    /// <summary>
    /// Returns a <c>&lt;script&gt;</c> block that makes clicking (or keyboard-activating) a legend entry toggle
    /// the corresponding series group's visibility. Legend entries carry <c>data-legend-index</c>
    /// and series groups carry <c>data-series-index</c>.
    /// Keyboard: <kbd>Enter</kbd> or <kbd>Space</kbd> on a focused legend entry triggers toggle.
    /// ARIA: each entry receives <c>role="button"</c>, <c>tabindex="0"</c>, and <c>aria-pressed</c>.
    /// </summary>
    internal static string GetScript() => """
        <script type="text/ecmascript"><![CDATA[
        (function() {
            document.querySelectorAll('[data-legend-index]').forEach(function(entry) {
                entry.setAttribute('tabindex', '0');
                entry.setAttribute('role', 'button');
                entry.setAttribute('aria-pressed', 'false');

                function toggle() {
                    var idx = entry.getAttribute('data-legend-index');
                    var seriesGroup = document.querySelector('[data-series-index="' + idx + '"]');
                    if (!seriesGroup) return;
                    var hidden = seriesGroup.style.display === 'none';
                    seriesGroup.style.display = hidden ? '' : 'none';
                    entry.style.opacity = hidden ? '1' : '0.4';
                    entry.setAttribute('aria-pressed', hidden ? 'false' : 'true');
                }

                entry.addEventListener('click', toggle);
                entry.addEventListener('keydown', function(e) {
                    if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); toggle(); }
                });
            });
        })();
        ]]></script>
        """;
}
