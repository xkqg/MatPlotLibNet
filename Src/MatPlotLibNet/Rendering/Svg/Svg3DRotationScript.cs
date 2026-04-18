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
            // Per-chart isolation (Phase 2): scope to THIS script's owning <svg>.
            var svg = (document.currentScript && document.currentScript.parentNode) || document;
            var scenes = svg.querySelectorAll('.mpl-3d-scene');
            scenes.forEach(function(scene) {
                var elevation = parseFloat(scene.getAttribute('data-elevation') || '30');
                var azimuth   = parseFloat(scene.getAttribute('data-azimuth')   || '-60');
                var distance  = scene.hasAttribute('data-distance') ? parseFloat(scene.getAttribute('data-distance')) : null;
                var plotX     = parseFloat(scene.getAttribute('data-plot-x') || '0');
                var plotY     = parseFloat(scene.getAttribute('data-plot-y') || '0');
                var plotW     = parseFloat(scene.getAttribute('data-plot-w') || '400');
                var plotH     = parseFloat(scene.getAttribute('data-plot-h') || '300');

                var initEl = elevation, initAz = azimuth, initDistance = distance;
                var dragging = false, lastX = 0, lastY = 0;

                // Phase B.4 of v1.7.2 follow-on plan — full matplotlib projection port.
                // Mirrors Src/MatPlotLibNet/Rendering/Projection3D.cs so first-drag reprojection
                // matches the server-rendered initial view exactly (no visual jump).
                //
                // Pipeline: data-v3d coords are already in Normalize() space (centered world
                // box [-BoxAspect/2, +BoxAspect/2]). For each frame we:
                //   1. Build the camera basis (u, v, w) from elev/az.
                //   2. Project to NDC: (u·c)/(dist - w·c), (v·c)/(dist - w·c).
                //   3. Walk the 8 cube corners to find the NDC bbox at this angle.
                //   4. Fit-to-plot uniformly (matches Projection3D.Project lines 244-267).
                // matplotlib defaults: focal_length=1, dist=10. BoxAspect = (25/21, 25/21, 25/28).
                var BOX_AX = 25/21, BOX_AY = 25/21, BOX_AZ = 25/28;
                function degToRad(d) { return d * Math.PI / 180; }

                function buildBasis() {
                    var elRad = degToRad(elevation), azRad = degToRad(azimuth);
                    var cosEl = Math.cos(elRad), sinEl = Math.sin(elRad);
                    var cosAz = Math.cos(azRad), sinAz = Math.sin(azRad);
                    return {
                        u: [-sinAz,           cosAz,           0     ],
                        v: [-sinEl * cosAz,  -sinEl * sinAz,   cosEl ],
                        w: [ cosEl * cosAz,   cosEl * sinAz,   sinEl ],
                        d: distance !== null ? distance : 10
                    };
                }

                function projectNdc(b, cx, cy, cz) {
                    var vu = b.u[0]*cx + b.u[1]*cy + b.u[2]*cz;
                    var vv = b.v[0]*cx + b.v[1]*cy + b.v[2]*cz;
                    var vw = b.w[0]*cx + b.w[1]*cy + b.w[2]*cz;
                    var denom = b.d - vw;
                    return [vu / denom, vv / denom];
                }

                function computeFit(b) {
                    var minX = Infinity, maxX = -Infinity, minY = Infinity, maxY = -Infinity;
                    for (var i = 0; i < 8; i++) {
                        var cx = (i & 1) ? BOX_AX/2 : -BOX_AX/2;
                        var cy = (i & 2) ? BOX_AY/2 : -BOX_AY/2;
                        var cz = (i & 4) ? BOX_AZ/2 : -BOX_AZ/2;
                        var n = projectNdc(b, cx, cy, cz);
                        if (n[0] < minX) minX = n[0];
                        if (n[0] > maxX) maxX = n[0];
                        if (n[1] < minY) minY = n[1];
                        if (n[1] > maxY) maxY = n[1];
                    }
                    var rangeX = maxX - minX, rangeY = maxY - minY;
                    return {
                        cxFit: (minX + maxX) / 2,
                        cyFit: (minY + maxY) / 2,
                        scale: Math.min(plotW / rangeX, plotH / rangeY)
                    };
                }

                function project(cx, cy, cz, b, fit) {
                    var n = projectNdc(b, cx, cy, cz);
                    var px = plotX + plotW/2 + (n[0] - fit.cxFit) * fit.scale;
                    var py = plotY + plotH/2 - (n[1] - fit.cyFit) * fit.scale;
                    return [px, py];
                }

                function viewZ(cx, cy, cz, b) {
                    return b.w[0]*cx + b.w[1]*cy + b.w[2]*cz;
                }

                function avgViewZ(el, b) {
                    var raw = el.getAttribute('data-v3d');
                    if (!raw) return 0;
                    var pts = raw.trim().split(' ');
                    var sum = 0;
                    for (var i = 0; i < pts.length; i++) {
                        var c = pts[i].split(',');
                        sum += viewZ(parseFloat(c[0]), parseFloat(c[1]), parseFloat(c[2]), b);
                    }
                    return sum / pts.length;
                }

                // Phase F of v1.7.2 follow-on — depth-resort is scoped to the data tier
                // only. Axis infrastructure (panes, edges, grid, labels, ticks) lives in
                // separate mpl-3d-back / mpl-3d-front subgroups and stays in fixed DOM
                // order — matches matplotlib's draw hierarchy (axes3d.py:458-470). Pre-F
                // the resort mixed panes with series, and back-corner surface quads would
                // get painted BEFORE the panes then covered by them on rotation.
                function resortDepth(b) {
                    var dataGroup = scene.querySelector('.mpl-3d-data');
                    if (!dataGroup) return;  // Backward-compat: figures without subgroups skip resort.
                    var children = Array.from(dataGroup.children).filter(function(c) { return c.hasAttribute('data-v3d'); });
                    children.sort(function(a, b2) { return avgViewZ(a, b) - avgViewZ(b2, b); });
                    children.forEach(function(c) { dataGroup.appendChild(c); });
                }

                function reprojectAll() {
                    var b = buildBasis();
                    var fit = computeFit(b);
                    var polys = scene.querySelectorAll('[data-v3d]');
                    polys.forEach(function(el) {
                        var raw = el.getAttribute('data-v3d');
                        var pts = raw.trim().split(' ');
                        var points = pts.map(function(p) {
                            var c = p.split(',');
                            return project(parseFloat(c[0]), parseFloat(c[1]), parseFloat(c[2]), b, fit);
                        });
                        if (el.tagName === 'polygon' || el.tagName === 'polyline') {
                            el.setAttribute('points', points.map(function(p) { return p[0].toFixed(2) + ',' + p[1].toFixed(2); }).join(' '));
                        } else if (el.tagName === 'circle' && points.length > 0) {
                            el.setAttribute('cx', points[0][0].toFixed(2));
                            el.setAttribute('cy', points[0][1].toFixed(2));
                        } else if (el.tagName === 'line' && points.length >= 2) {
                            el.setAttribute('x1', points[0][0].toFixed(2));
                            el.setAttribute('y1', points[0][1].toFixed(2));
                            el.setAttribute('x2', points[1][0].toFixed(2));
                            el.setAttribute('y2', points[1][1].toFixed(2));
                        } else if (el.tagName === 'text' && points.length > 0) {
                            el.setAttribute('x', points[0][0].toFixed(2));
                            el.setAttribute('y', points[0][1].toFixed(2));
                        }
                    });
                    resortDepth(b);
                }

                // Phase 4 — Pointer Events. setPointerCapture binds the pointer to the
                // scene so dragging out of the scene still releases cleanly. Mouse fallback
                // kept for pre-pointer-events runtimes.
                // Phase A.1 of v1.7.2 follow-on plan — stopPropagation prevents the event
                // bubbling to the SVG root and triggering the 2D pan handler (defence in
                // depth — Phase A.2 also bails the 2D handler at init for 3D charts).
                function startDrag(e) {
                    dragging = true; lastX = e.clientX; lastY = e.clientY;
                    e.preventDefault(); e.stopPropagation();
                    if (scene.setPointerCapture && e.pointerId !== undefined)
                        try { scene.setPointerCapture(e.pointerId); } catch (_) {}
                }
                // Phase B.2 of v1.7.2 follow-on plan — matplotlib parity
                // (mpl_toolkits/mplot3d/axes3d.py:_on_move, roll = 0):
                //   dazim = -(dx / w) * 180   ;   delev = -(dy / h) * 180
                // i.e. a full-axes drag = 180° rotation. Pre-fix used a fixed
                // 0.5°/pixel + inverted azimuth sign + clamped elev to ±90°.
                function moveDrag(e) {
                    if (!dragging) return;
                    var dx = e.clientX - lastX, dy = e.clientY - lastY;
                    azimuth   -= (dx / plotW) * 180;
                    elevation -= (dy / plotH) * 180;
                    persistAngles();
                    lastX = e.clientX; lastY = e.clientY;
                    reprojectAll();
                }
                function persistAngles() {
                    scene.setAttribute('data-azimuth',   azimuth.toFixed(4));
                    scene.setAttribute('data-elevation', elevation.toFixed(4));
                    if (distance !== null) scene.setAttribute('data-distance', distance.toFixed(4));
                }
                function endDrag() { dragging = false; }
                scene.addEventListener('pointerdown', startDrag);
                scene.addEventListener('pointermove', moveDrag);
                scene.addEventListener('pointerup',   endDrag);
                scene.addEventListener('pointercancel', endDrag);
                scene.addEventListener('mousedown', startDrag);
                scene.addEventListener('mousemove', moveDrag);
                scene.addEventListener('mouseup',   endDrag);
                scene.addEventListener('mouseleave', endDrag);

                // Scroll-wheel zoom: tracks data-distance directly (no-op when distance is null,
                // i.e. orthographic projection — wheel doesn't promote to perspective silently).
                // {passive:false} required so preventDefault overrides browser/iframe scroll.
                scene.addEventListener('wheel', function(e) {
                    if (distance === null) return;
                    e.preventDefault(); e.stopPropagation();
                    distance = Math.max(2, Math.min(100, distance + (e.deltaY > 0 ? 0.5 : -0.5)));
                    persistAngles();
                    reprojectAll();
                }, { passive: false });

                scene.setAttribute('style', 'cursor:grab');
                scene.addEventListener('keydown', function(e) {
                    switch(e.key) {
                        case 'ArrowLeft':  azimuth   -= 5; break;
                        case 'ArrowRight': azimuth   += 5; break;
                        case 'ArrowUp':    elevation += 5; break;
                        case 'ArrowDown':  elevation -= 5; break;
                        case '+': if (distance !== null) distance = Math.max(2, distance - 0.5); break;
                        case '-': if (distance !== null) distance = distance + 0.5; break;
                        case 'Home': elevation = initEl; azimuth = initAz; distance = initDistance; break;
                        default: return;
                    }
                    // No elevation clamp — matches matplotlib (the V-vector flip in
                    // project() handles |elev|>90° by inverting the up-direction).
                    persistAngles();
                    reprojectAll();
                    e.preventDefault();
                });
                scene.setAttribute('tabindex', '0');
            });
        })();
        ]]></script>
        """;
}
