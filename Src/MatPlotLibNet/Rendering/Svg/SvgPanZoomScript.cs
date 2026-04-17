// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>Embedded JavaScript for pan (drag) and zoom (scroll wheel) in SVG output.
/// Self-contained IIFE with no external dependencies. Manipulates the SVG viewBox
/// to achieve smooth pan/zoom without re-rendering.</summary>
internal static class SvgPanZoomScript
{
    internal static string GetScript() => """
        <script type="text/ecmascript"><![CDATA[
        (function() {
            'use strict';
            var svg = document.querySelector('svg');
            if (!svg) return;
            var vb = svg.viewBox.baseVal;
            if (!vb || vb.width === 0) {
                var bbox = svg.getBBox();
                svg.setAttribute('viewBox', '0 0 ' + bbox.width + ' ' + bbox.height);
                vb = svg.viewBox.baseVal;
            }
            var dragging = false, lastX = 0, lastY = 0;

            svg.addEventListener('mousedown', function(e) {
                if (e.button !== 0 || e.shiftKey || e.ctrlKey || e.altKey) return;
                dragging = true; lastX = e.clientX; lastY = e.clientY;
                svg.style.cursor = 'grabbing'; e.preventDefault();
            });
            document.addEventListener('mousemove', function(e) {
                if (!dragging) return;
                var dx = (e.clientX - lastX) * vb.width / svg.clientWidth;
                var dy = (e.clientY - lastY) * vb.height / svg.clientHeight;
                vb.x -= dx; vb.y -= dy;
                lastX = e.clientX; lastY = e.clientY;
            });
            document.addEventListener('mouseup', function() {
                dragging = false; svg.style.cursor = 'grab';
            });

            svg.addEventListener('wheel', function(e) {
                e.preventDefault();
                var scale = e.deltaY > 0 ? 1.1 : 0.9;
                var pt = svg.createSVGPoint();
                pt.x = e.clientX; pt.y = e.clientY;
                var svgPt = pt.matrixTransform(svg.getScreenCTM().inverse());
                vb.x = svgPt.x - (svgPt.x - vb.x) * scale;
                vb.y = svgPt.y - (svgPt.y - vb.y) * scale;
                vb.width *= scale; vb.height *= scale;
            }, { passive: false });

            svg.style.cursor = 'grab';

            // Double-click to reset
            svg.addEventListener('dblclick', function() {
                var bbox = svg.getBBox();
                vb.x = 0; vb.y = 0; vb.width = bbox.width; vb.height = bbox.height;
            });
        })();
        ]]></script>
        """;
}
