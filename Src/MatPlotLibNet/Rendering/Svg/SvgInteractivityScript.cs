// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>Provides the embedded JavaScript for SVG zoom and pan interactivity.</summary>
internal static class SvgInteractivityScript
{
    /// <summary>Returns a <c>&lt;script&gt;</c> block that adds zoom (mouse wheel) and pan (click-drag) behavior to the SVG.</summary>
    /// <remarks>The script manipulates the SVG <c>viewBox</c> attribute. Zoom is centered on the cursor position.
    /// Double-click resets to the original view. Wrapped in a CDATA section for XML compatibility.
    /// Only effective in browser environments; no-op in non-interactive viewers.</remarks>
    internal static string GetZoomPanScript() => """
        <script type="text/ecmascript"><![CDATA[
        (function() {
            var svg = document.querySelector('svg');
            if (!svg) return;
            var vb = svg.getAttribute('viewBox').split(' ').map(Number);
            var origVb = vb.slice();
            var isPanning = false, startX = 0, startY = 0;

            svg.addEventListener('wheel', function(e) {
                e.preventDefault();
                var scale = e.deltaY > 0 ? 1.1 : 0.9;
                var pt = svg.createSVGPoint();
                pt.x = e.clientX; pt.y = e.clientY;
                var svgPt = pt.matrixTransform(svg.getScreenCTM().inverse());
                vb[0] = svgPt.x - (svgPt.x - vb[0]) * scale;
                vb[1] = svgPt.y - (svgPt.y - vb[1]) * scale;
                vb[2] *= scale; vb[3] *= scale;
                svg.setAttribute('viewBox', vb.join(' '));
            });

            svg.addEventListener('mousedown', function(e) {
                isPanning = true; startX = e.clientX; startY = e.clientY;
                svg.style.cursor = 'grabbing';
            });

            svg.addEventListener('mousemove', function(e) {
                if (!isPanning) return;
                var dx = (e.clientX - startX) * (vb[2] / svg.clientWidth);
                var dy = (e.clientY - startY) * (vb[3] / svg.clientHeight);
                vb[0] -= dx; vb[1] -= dy;
                startX = e.clientX; startY = e.clientY;
                svg.setAttribute('viewBox', vb.join(' '));
            });

            svg.addEventListener('mouseup', function() {
                isPanning = false; svg.style.cursor = 'default';
            });

            svg.addEventListener('dblclick', function() {
                vb = origVb.slice();
                svg.setAttribute('viewBox', vb.join(' '));
            });
        })();
        ]]></script>
        """;
}
