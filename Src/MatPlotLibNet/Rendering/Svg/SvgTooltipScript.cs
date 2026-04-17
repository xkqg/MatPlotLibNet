// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>Embedded JavaScript for hover tooltips on data points in SVG output.
/// Reads <c>data-x</c> and <c>data-y</c> attributes from SVG elements and shows
/// a floating tooltip at the cursor position.</summary>
internal static class SvgTooltipScript
{
    internal static string GetScript() => """
        <script type="text/ecmascript"><![CDATA[
        (function() {
            'use strict';
            var svg = document.querySelector('svg');
            if (!svg) return;

            // Create tooltip element
            var ns = 'http://www.w3.org/2000/svg';
            var tooltip = document.createElementNS(ns, 'g');
            tooltip.setAttribute('visibility', 'hidden');
            tooltip.setAttribute('pointer-events', 'none');

            var bg = document.createElementNS(ns, 'rect');
            bg.setAttribute('fill', '#333'); bg.setAttribute('rx', '4');
            bg.setAttribute('opacity', '0.9');

            var text = document.createElementNS(ns, 'text');
            text.setAttribute('fill', '#fff'); text.setAttribute('font-size', '11');
            text.setAttribute('font-family', 'sans-serif');

            tooltip.appendChild(bg);
            tooltip.appendChild(text);
            svg.appendChild(tooltip);

            var dataPoints = svg.querySelectorAll('[data-x][data-y]');
            dataPoints.forEach(function(el) {
                el.addEventListener('mouseenter', function(e) {
                    var x = el.getAttribute('data-x');
                    var y = el.getAttribute('data-y');
                    var label = el.getAttribute('data-label') || '';
                    text.textContent = (label ? label + ': ' : '') + '(' + x + ', ' + y + ')';

                    var bbox = text.getBBox();
                    bg.setAttribute('x', bbox.x - 4);
                    bg.setAttribute('y', bbox.y - 2);
                    bg.setAttribute('width', bbox.width + 8);
                    bg.setAttribute('height', bbox.height + 4);

                    tooltip.setAttribute('visibility', 'visible');
                });

                el.addEventListener('mousemove', function(e) {
                    var pt = svg.createSVGPoint();
                    pt.x = e.clientX + 12; pt.y = e.clientY - 12;
                    var svgPt = pt.matrixTransform(svg.getScreenCTM().inverse());
                    tooltip.setAttribute('transform', 'translate(' + svgPt.x + ',' + svgPt.y + ')');
                });

                el.addEventListener('mouseleave', function() {
                    tooltip.setAttribute('visibility', 'hidden');
                });
            });
        })();
        ]]></script>
        """;
}
