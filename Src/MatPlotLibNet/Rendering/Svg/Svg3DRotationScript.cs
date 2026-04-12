// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>Provides the embedded JavaScript for interactive 3D rotation in SVG output.</summary>
internal static class Svg3DRotationScript
{
    internal static string GetScript() => """
        <script type="text/ecmascript"><![CDATA[
        (function() {
            'use strict';
            var scenes = document.querySelectorAll('.mpl-3d-scene');
            scenes.forEach(function(scene) {
                var elevation = parseFloat(scene.getAttribute('data-elevation') || '30');
                var azimuth   = parseFloat(scene.getAttribute('data-azimuth')   || '-60');
                var distance  = scene.hasAttribute('data-distance') ? parseFloat(scene.getAttribute('data-distance')) : null;
                var plotX     = parseFloat(scene.getAttribute('data-plot-x') || '0');
                var plotY     = parseFloat(scene.getAttribute('data-plot-y') || '0');
                var plotW     = parseFloat(scene.getAttribute('data-plot-w') || '400');
                var plotH     = parseFloat(scene.getAttribute('data-plot-h') || '300');

                var initEl = elevation, initAz = azimuth;
                var dragging = false, lastX = 0, lastY = 0;

                function degToRad(d) { return d * Math.PI / 180; }

                function project(nx, ny, nz) {
                    var azRad = degToRad(azimuth), elRad = degToRad(elevation);
                    var cosAz = Math.cos(azRad), sinAz = Math.sin(azRad);
                    var cosEl = Math.cos(elRad), sinEl = Math.sin(elRad);
                    var rx = nx * cosAz - ny * sinAz;
                    var ry = nx * sinAz + ny * cosAz;
                    var rz = nz;
                    var pz = ry * sinEl + rz * cosEl;
                    if (distance !== null) {
                        var viewDepth = ry * cosEl - rz * sinEl;
                        var scale = distance / (distance - viewDepth);
                        rx *= scale; pz *= scale;
                    }
                    var px = plotX + plotW * (rx + 1) / 2;
                    var py = plotY + plotH * (1 - (pz + 1) / 2);
                    return [px, py];
                }

                function reprojectAll() {
                    var polys = scene.querySelectorAll('[data-v3d]');
                    polys.forEach(function(el) {
                        var raw = el.getAttribute('data-v3d');
                        var pts = raw.trim().split(' ');
                        var points = pts.map(function(p) {
                            var c = p.split(',');
                            return project(parseFloat(c[0]), parseFloat(c[1]), parseFloat(c[2]));
                        });
                        if (el.tagName === 'polygon' || el.tagName === 'polyline') {
                            el.setAttribute('points', points.map(function(p) { return p[0].toFixed(2) + ',' + p[1].toFixed(2); }).join(' '));
                        } else if (el.tagName === 'circle' && points.length > 0) {
                            el.setAttribute('cx', points[0][0].toFixed(2));
                            el.setAttribute('cy', points[0][1].toFixed(2));
                        }
                    });
                }

                scene.addEventListener('mousedown', function(e) { dragging = true; lastX = e.clientX; lastY = e.clientY; e.preventDefault(); });
                document.addEventListener('mousemove', function(e) {
                    if (!dragging) return;
                    azimuth   += (e.clientX - lastX) * 0.5;
                    elevation -= (e.clientY - lastY) * 0.5;
                    elevation = Math.max(-90, Math.min(90, elevation));
                    lastX = e.clientX; lastY = e.clientY;
                    reprojectAll();
                });
                document.addEventListener('mouseup', function() { dragging = false; });

                scene.setAttribute('style', 'cursor:grab');
                scene.addEventListener('keydown', function(e) {
                    switch(e.key) {
                        case 'ArrowLeft':  azimuth   -= 5; break;
                        case 'ArrowRight': azimuth   += 5; break;
                        case 'ArrowUp':    elevation += 5; break;
                        case 'ArrowDown':  elevation -= 5; break;
                        case '+': if (distance !== null) distance = Math.max(2, distance - 0.5); break;
                        case '-': if (distance !== null) distance = distance + 0.5; break;
                        case 'Home': elevation = initEl; azimuth = initAz; break;
                        default: return;
                    }
                    elevation = Math.max(-90, Math.min(90, elevation));
                    reprojectAll();
                    e.preventDefault();
                });
                scene.setAttribute('tabindex', '0');
            });
        })();
        ]]></script>
        """;
}
