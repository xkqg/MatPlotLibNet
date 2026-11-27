// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Transforms;

public sealed partial class SvgTransform
{
    // What the comments of the script below said. They are not emitted: a figure that carries the script carries no
    // comment of it. Each note stands at the line it was written beside.
    // at the locator (SvgScriptPreamble.Root)
    //   Per-chart isolation: scope to THIS script's owning <svg>.
    // at `var distance  = scene.hasAttribute('data-distance') ? parseFloat(scene.getAttrib`
    //   Default to 10 (matches the server's Camera3D.RestingDistance) when the
    //   attribute is missing, so wheel zoom works on every 3D chart, not just
    //   the ones where the caller explicitly set WithCamera(..., distance: N).
    // at `var BOX_AX = 25/21, BOX_AY = 25/21, BOX_AZ = 25/28;`
    //   Full matplotlib projection port.
    //   Mirrors the server's Projection3D so first-drag reprojection
    //   matches the server-rendered initial view exactly (no visual jump).
    //
    //   Pipeline: data-v3d coords are already in Normalize() space (centered world
    //   box [-BoxAspect/2, +BoxAspect/2]). For each frame we:
    //   1. Build the camera basis (u, v, w) from elev/az.
    //   2. Project to NDC: (u·c)/(dist - w·c), (v·c)/(dist - w·c).
    //   3. Place the NDC point in the plot by matplotlib's fixed view limits (FIT below).
    //   matplotlib defaults: focal_length=1, dist=10. BoxAspect = (25/21, 25/21, 25/28).
    // at `var FIT_SIDE = Math.min(plotW, plotH);`
    //   The fit, matplotlib's rule as Projection3D follows it (axes3d.py set_top_view and apply_aspect): the
    //   plot shrunk to a square centred in it shows the projected points from -0.95/dist to 0.9/dist in both
    //   directions, whatever the corners of the box project to. The scale is the first draw's distance, so a
    //   closer camera (the wheel, + and -) draws the box larger: zoom is "I can look closer".
    // at `var SERVER_FACES = (scene.getAttribute('data-faces') || '001');`
    //   ── Camera-derived cube faces (mirror of Projection3D.SelectFaces / CubeFaceSelection).
    //   Which of each axis' two parallel faces points AWAY from the camera decides where the
    //   panes, the nine drawn cube edges, the wall grids and the tick rows live. The server
    //   stamped its own answer in data-faces; we re-run the same rule per frame and mirror the
    //   elements whose axis flipped. Without this the axis frame stays where the server left it
    //   and a drag past ±90° puts a pane on a face that is now in FRONT of the data (issue #18).
    // at `var a1 = (axis + 1) % 3, a2 = (axis + 2) % 3;`
    //   Mean post-divide projected z over the four corners of one axis-aligned face.
    // at `sum += -b.d / (b.d - viewZ(c[0], c[1], c[2], b));`
    //   Post-divide projected z, exactly as Projection3D.ProjectedDepth:
    //   mz = -dist (the persp matrix' c term), mw = dist - viewZ. Larger = FARTHER.
    // at `function faceMirror(b) {`
    //   +1 per axis when this frame agrees with the server, -1 when the face flipped. The
    //   emitted coordinates are centred, so the opposite plane is the exact negation.
    // at `function pinnedMirror(el, mirror) {`
    //   Applies the mirror to the components an element declared as pinned to a selected face.
    // at `function negateToken(text) {`
    //   Negates a coordinate by flipping the SIGN CHARACTER, never by re-formatting: the
    //   attribute keeps the server's exact G6 text, so data-v3d stays comparable with a
    //   server render at the same angle.
    // at `function resortDepth(b) {`
    //   Depth-resort is scoped to the data tier only. Axis infrastructure (panes,
    //   edges, grid, labels, ticks) lives in separate mpl-3d-back / mpl-3d-front
    //   subgroups and stays in fixed DOM order — matches matplotlib's draw hierarchy
    //   (axes3d.py:458-470). Sorting the panes together with the series would paint
    //   back-corner surface quads before the panes and let the panes cover them on
    //   rotation.
    // at `if (!dataGroup) return;`
    //   Backward-compat: figures without subgroups skip resort.
    // at `var centroid = project(0, 0, 0, b, fit);`
    //   The cube centroid is the origin of the centred coordinates the vertices carry;
    //   labels are pushed away from THIS point, matching the server's
    //   CubeAxisRow.OutwardPerp. (Flipping against the plot-rect centre instead moved
    //   labels to the wrong side of their axis on the first drag frame.)
    // at `if (m[0] < 0 || m[1] < 0 || m[2] < 0) {`
    //   Write the mirrored geometry back so data-v3d always describes where the
    //   element IS. Without this the attribute would keep naming the face the
    //   server picked, and the next frame would mirror an already-mirrored vertex.
    // at `var labelX = points[0][0], labelY = points[0][1];`
    //   Preserve the perpendicular pad that pushes tick-labels +
    //   axis-titles outside the cube.
    //   Mirrors the server's CubeAxisRow.OutwardPerp: build 2D perpendicular
    //   from the projected axis edge (data-v3d-edge), flip it away
    //   from the plot centre, apply pad magnitude.
    // at `if (perpX * (centroid[0] - labelX) + perpY * (centroid[1] - labelY) > 0) {`
    //   Flip perp so it points AWAY from the projected cube centroid.
    // at `var now = selectFaces(b);`
    //   The DOM now holds the geometry for THIS camera, so record the selection it
    //   was mirrored to; the next frame measures its flips against this one.
    // at `function startDrag(e) {`
    //   Pointer Events. setPointerCapture binds the pointer to the
    //   scene so dragging out of the scene still releases cleanly. Mouse fallback
    //   kept for pre-pointer-events runtimes.
    //   stopPropagation prevents the event bubbling to the SVG root and
    //   triggering the 2D pan handler (defence in depth — the 2D handler also
    //   bails at init for 3D charts).
    // at `function moveDrag(e) {`
    //   matplotlib parity
    //   (mpl_toolkits/mplot3d/axes3d.py:_on_move, roll = 0):
    //   dazim = -(dx / w) * 180   ;   delev = -(dy / h) * 180
    //   i.e. a full-axes drag = 180° rotation.
    // at `scene.addEventListener('wheel', function(e) {`
    //   Scroll-wheel zoom: tracks data-distance directly.
    //   {passive:false} required so preventDefault overrides browser/iframe scroll.
    // at `e.preventDefault(); e.stopPropagation();`
    //   The init above defaults the distance to 10, so this handler always
    //   does work.
    //   Matplotlib's axes3d._scroll_zoom uses a MULTIPLICATIVE 1.1× per step;
    //   matching that makes zoom feel responsive and scales correctly at
    //   any distance (an additive step is only responsive near the MIN clamp).
    // at `persistAngles();`
    //   No elevation clamp — matches matplotlib (the V-vector flip in
    //   project() handles |elev|>90° by inverting the up-direction).
    /// <summary>The script that turns a 3-D axes by dragging it, as markup to put in the SVG.</summary>
    /// <returns>The script, as markup.</returns>
    internal static string GetScript() => $$"""
        <script type="text/ecmascript"><![CDATA[
        (function() {
            'use strict';
        {{SvgScriptPreamble.Root}}
        {{SvgScriptPreamble.Capture}}
            var scenes = svg.querySelectorAll('.mpl-3d-scene');
            scenes.forEach(function(scene) {
                var elevation = parseFloat(scene.getAttribute('data-elevation'));
                var azimuth   = parseFloat(scene.getAttribute('data-azimuth'));
                var distance  = parseFloat(scene.getAttribute('data-distance'));
                var plotX     = parseFloat(scene.getAttribute('{{SvgAttributes.PlotX}}'));
                var plotY     = parseFloat(scene.getAttribute('{{SvgAttributes.PlotY}}'));
                var plotW     = parseFloat(scene.getAttribute('{{SvgAttributes.PlotW}}'));
                var plotH     = parseFloat(scene.getAttribute('{{SvgAttributes.PlotH}}'));

                var initEl = elevation, initAz = azimuth, initDistance = distance;
                var dragging = false, lastX = 0, lastY = 0;

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
                        d: distance
                    };
                }

                function projectNdc(b, cx, cy, cz) {
                    var vu = b.u[0]*cx + b.u[1]*cy + b.u[2]*cz;
                    var vv = b.v[0]*cx + b.v[1]*cy + b.v[2]*cz;
                    var vw = b.w[0]*cx + b.w[1]*cy + b.w[2]*cz;
                    var denom = b.d - vw;
                    return [vu / denom, vv / denom];
                }

                var FIT_SIDE = Math.min(plotW, plotH);
                var FIT = {
                    originX: plotX + (plotW - FIT_SIDE) / 2 + FIT_SIDE * 0.95 / 1.85,
                    originY: plotY + (plotH - FIT_SIDE) / 2 + FIT_SIDE * 0.9 / 1.85,
                    scale: FIT_SIDE * initDistance / 1.85
                };

                function project(cx, cy, cz, b, fit) {
                    var n = projectNdc(b, cx, cy, cz);
                    return [fit.originX + n[0] * fit.scale, fit.originY - n[1] * fit.scale];
                }

                function viewZ(cx, cy, cz, b) {
                    return b.w[0]*cx + b.w[1]*cy + b.w[2]*cz;
                }

                var SERVER_FACES = (scene.getAttribute('data-faces') || '001');
                var HALF = [BOX_AX/2, BOX_AY/2, BOX_AZ/2];
                var MACHINE_EPS = 2.220446049250313e-16;

                function planeMeanZ(b, axis, side) {
                    var a1 = (axis + 1) % 3, a2 = (axis + 2) % 3;
                    var c = [0, 0, 0];
                    c[axis] = side * HALF[axis];
                    var sum = 0;
                    for (var i = -1; i <= 1; i += 2) {
                        c[a1] = i * HALF[a1];
                        for (var j = -1; j <= 1; j += 2) {
                            c[a2] = j * HALF[a2];
                            sum += -b.d / (b.d - viewZ(c[0], c[1], c[2], b));
                        }
                    }
                    return sum / 4;
                }

                function selectFaces(b) {
                    var backAtMax = [false, false, false], edgeOn = [false, false, false], edgeOnCount = 0;
                    for (var axis = 0; axis < 3; axis++) {
                        var meanLo = planeMeanZ(b, axis, -1), meanHi = planeMeanZ(b, axis, +1);
                        backAtMax[axis] = meanLo < meanHi;
                        edgeOn[axis] = Math.abs(meanLo - meanHi) <= MACHINE_EPS;
                        if (edgeOn[axis]) edgeOnCount++;
                    }
                    if (edgeOnCount === 2) {
                        var vertical = !edgeOn[0] ? 0 : (!edgeOn[1] ? 1 : 2);
                        if (vertical === 2) { backAtMax[0] = true; backAtMax[1] = true; }
                        else if (vertical === 1) { backAtMax[0] = true; backAtMax[2] = false; }
                        else { backAtMax[1] = false; backAtMax[2] = false; }
                    }
                    return backAtMax;
                }

                function faceMirror(b) {
                    var now = selectFaces(b);
                    return [
                        (now[0] === (SERVER_FACES[0] === '1')) ? 1 : -1,
                        (now[1] === (SERVER_FACES[1] === '1')) ? 1 : -1,
                        (now[2] === (SERVER_FACES[2] === '1')) ? 1 : -1
                    ];
                }

                function pinnedMirror(el, mirror) {
                    var pinned = el.getAttribute('data-v3d-pinned');
                    if (!pinned) return [1, 1, 1];
                    return [
                        pinned.indexOf('x') >= 0 ? mirror[0] : 1,
                        pinned.indexOf('y') >= 0 ? mirror[1] : 1,
                        pinned.indexOf('z') >= 0 ? mirror[2] : 1
                    ];
                }

                function negateToken(text) {
                    return text.charAt(0) === '-' ? text.substring(1) : '-' + text;
                }

                function mirrorV3dText(raw, m) {
                    return raw.trim().split(' ').map(function(p) {
                        var c = p.split(',');
                        return (m[0] < 0 ? negateToken(c[0]) : c[0]) + ',' +
                               (m[1] < 0 ? negateToken(c[1]) : c[1]) + ',' +
                               (m[2] < 0 ? negateToken(c[2]) : c[2]);
                    }).join(' ');
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

                function resortDepth(b) {
                    var dataGroup = scene.querySelector('.mpl-3d-data');
                    if (!dataGroup) return;
                    resortChildren(dataGroup, b);
                    Array.from(dataGroup.querySelectorAll('g')).forEach(function(g) { resortChildren(g, b); });
                }

                function resortChildren(parent, b) {
                    var children = Array.from(parent.children).filter(function(c) { return c.hasAttribute('data-v3d'); });
                    children.sort(function(a, b2) { return avgViewZ(a, b) - avgViewZ(b2, b); });
                    children.forEach(function(c) { parent.appendChild(c); });
                }

                function reprojectAll() {
                    var b = buildBasis();
                    var fit = FIT;
                    var mirror = faceMirror(b);
                    var centroid = project(0, 0, 0, b, fit);
                    var polys = scene.querySelectorAll('[data-v3d]');
                    polys.forEach(function(el) {
                        var m = pinnedMirror(el, mirror);
                        if (m[0] < 0 || m[1] < 0 || m[2] < 0) {
                            el.setAttribute('data-v3d', mirrorV3dText(el.getAttribute('data-v3d'), m));
                            var edgeAttr = el.getAttribute('data-v3d-edge');
                            if (edgeAttr) el.setAttribute('data-v3d-edge', mirrorV3dText(edgeAttr, m));
                        }
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
                        } else if (el.tagName === 'g' && points.length > 0) {
                            var c0 = el.getAttribute('data-c').split(',');
                            el.setAttribute('transform', 'translate(' + (points[0][0] - parseFloat(c0[0])).toFixed(2) + ',' + (points[0][1] - parseFloat(c0[1])).toFixed(2) + ')');
                        } else if (el.tagName === 'line' && points.length >= 2) {
                            el.setAttribute('x1', points[0][0].toFixed(2));
                            el.setAttribute('y1', points[0][1].toFixed(2));
                            el.setAttribute('x2', points[1][0].toFixed(2));
                            el.setAttribute('y2', points[1][1].toFixed(2));
                        } else if (el.tagName === 'text' && points.length > 0) {
                            var labelX = points[0][0], labelY = points[0][1];
                            var edgeRaw = el.getAttribute('data-v3d-edge');
                            var padRaw = el.getAttribute('data-pad');
                            if (edgeRaw && padRaw) {
                                var pad = parseFloat(padRaw);
                                var epts = edgeRaw.trim().split(' ');
                                var eA = epts[0].split(','), eB = epts[1].split(',');
                                var pA = project(parseFloat(eA[0]), parseFloat(eA[1]), parseFloat(eA[2]), b, fit);
                                var pB = project(parseFloat(eB[0]), parseFloat(eB[1]), parseFloat(eB[2]), b, fit);
                                var ex = pB[0] - pA[0], ey = pB[1] - pA[1];
                                var elen = Math.sqrt(ex*ex + ey*ey) || 1;
                                var perpX = -ey / elen, perpY = ex / elen;
                                if (perpX * (centroid[0] - labelX) + perpY * (centroid[1] - labelY) > 0) {
                                    perpX = -perpX; perpY = -perpY;
                                }
                                labelX += perpX * pad;
                                labelY += perpY * pad;
                            }
                            el.setAttribute('x', labelX.toFixed(2));
                            el.setAttribute('y', labelY.toFixed(2));
                        }
                    });
                    resortDepth(b);
                    var now = selectFaces(b);
                    SERVER_FACES = (now[0] ? '1' : '0') + (now[1] ? '1' : '0') + (now[2] ? '1' : '0');
                    scene.setAttribute('data-faces', SERVER_FACES);
                }

                function startDrag(e) {
                    dragging = true; lastX = e.clientX; lastY = e.clientY;
                    e.preventDefault(); e.stopPropagation();
                    capture(scene, e);
                }
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
                    scene.setAttribute('data-distance', distance.toFixed(4));
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

                scene.addEventListener('wheel', function(e) {
                    e.preventDefault(); e.stopPropagation();
                    var scale = e.deltaY > 0 ? 1.1 : 1 / 1.1;
                    distance = Math.max(2, Math.min(100, distance * scale));
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
                        case '+': distance = Math.max(2, distance - 0.5); break;
                        case '-': distance = distance + 0.5; break;
                        case 'Home': elevation = initEl; azimuth = initAz; distance = initDistance; break;
                        default: return;
                    }
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
