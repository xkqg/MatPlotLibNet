// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>Provides the embedded JavaScript for SVG zoom and pan interactivity.</summary>
internal static class SvgInteractivityScript
{
    /// <summary>Returns a <c>&lt;script&gt;</c> block that adds zoom (mouse wheel / keyboard) and pan (click-drag / arrow keys) behavior to the SVG.</summary>
    /// <remarks>The script manipulates the SVG <c>viewBox</c> attribute. Zoom is centered on the cursor position.
    /// Double-click or <kbd>Home</kbd> resets to the original view. Keyboard: <kbd>+</kbd>/<kbd>=</kbd> zoom in,
    /// <kbd>-</kbd> zoom out, <kbd>ArrowLeft/Right/Up/Down</kbd> pan.
    /// SVG receives <c>tabindex="0"</c> and <c>aria-roledescription="interactive chart"</c>.
    /// Wrapped in a CDATA section for XML compatibility.
    /// Only effective in browser environments; no-op in non-interactive viewers.</remarks>
    internal static string GetZoomPanScript() => """
        <script type="text/ecmascript"><![CDATA[
        (function() {
            // Per-chart isolation (Phase 2): self-locate via document.currentScript so
            // multi-chart pages don't cross-talk.
            var svg = (document.currentScript && document.currentScript.parentNode) || document.querySelector('svg');
            if (!svg || svg.tagName !== 'svg') return;
            // Phase A.2 of v1.7.2 follow-on plan — when the SVG hosts a 3D scene, the 2D
            // pan/zoom handler must NOT install. Pre-fix it stole pointer/wheel events from
            // Svg3DRotationScript (last-call-wins on setPointerCapture; bubble order on
            // wheel). matplotlib's NavigationToolbar2 disables Pan/Zoom on 3D axes for the
            // same reason — viewBox-pan-on-rotating-cube is double-translation noise.
            if (svg.querySelector('.mpl-3d-scene')) return;
            svg.setAttribute('tabindex', '0');
            svg.setAttribute('aria-roledescription', 'interactive chart');
            var vb = svg.getAttribute('viewBox').split(' ').map(Number);
            var origVb = vb.slice();
            var isPanning = false, startX = 0, startY = 0;
            var panStep = 20;

            // Phase 9 of v1.7.2 plan — URL-hash state persistence (opt-in).
            // Set data-mpl-persist="true" on the SVG to enable. Each chart's id seeds the
            // hash key (mpl-{id}=zoom:cx,cy,w,h); multi-chart pages use | separators.
            var persistKey = svg.getAttribute('id') ? 'mpl-' + svg.getAttribute('id') : null;
            var persistEnabled = persistKey && svg.getAttribute('data-mpl-persist') === 'true';
            function persistVB() {
                if (!persistEnabled || typeof window === 'undefined' || !window.location) return;
                var hashSegment = persistKey + '=zoom:' + vb.map(function(n){ return n.toFixed(2); }).join(',');
                var hash = (window.location.hash || '').replace(/^#/, '');
                var parts = hash ? hash.split('|') : [];
                var newParts = parts.filter(function(p){ return p.indexOf(persistKey + '=') !== 0; });
                newParts.push(hashSegment);
                try { window.location.hash = newParts.join('|'); } catch (_) {}
            }
            function restoreVB() {
                if (!persistEnabled || typeof window === 'undefined' || !window.location) return;
                var hash = (window.location.hash || '').replace(/^#/, '');
                if (!hash) return;
                var parts = hash.split('|');
                for (var i = 0; i < parts.length; i++) {
                    if (parts[i].indexOf(persistKey + '=zoom:') === 0) {
                        var raw = parts[i].split('zoom:')[1];
                        var nums = raw.split(',').map(Number);
                        if (nums.length === 4 && nums.every(function(n){ return !isNaN(n); })) {
                            vb[0] = nums[0]; vb[1] = nums[1]; vb[2] = nums[2]; vb[3] = nums[3];
                            svg.setAttribute('viewBox', vb.join(' '));
                            return;
                        }
                    }
                }
            }
            restoreVB();

            // Phase 5 of v1.7.2 plan — clamps prevent runaway zoom (cumulative wheel deltas
            // can otherwise reduce viewBox to near-zero, blanking the chart) and pan
            // (drag enough and chart slides off-screen with no way back). MIN_ZOOM keeps
            // the viewBox at >=10% of original (10x in); MAX_ZOOM at <=10x original (0.1x out).
            // Aspect-lock — set data-aspect-lock="true" on the <svg> for charts that must
            // not be stretched anisotropically (geographic projections, square-pixel images).
            var MIN_ZOOM = 0.1;   // viewBox can shrink to 10% of original (max zoom-in)
            var MAX_ZOOM = 10.0;  // viewBox can grow to 10x of original  (max zoom-out)
            var aspectLock = svg.getAttribute('data-aspect-lock') === 'true';
            function clampVB(target) {
                // Width clamp
                var minW = origVb[2] * MIN_ZOOM, maxW = origVb[2] * MAX_ZOOM;
                if (target[2] < minW) target[2] = minW;
                if (target[2] > maxW) target[2] = maxW;
                var minH = origVb[3] * MIN_ZOOM, maxH = origVb[3] * MAX_ZOOM;
                if (target[3] < minH) target[3] = minH;
                if (target[3] > maxH) target[3] = maxH;
                if (aspectLock) {
                    // Force isotropic: pick the larger zoom factor so neither axis stretches.
                    var fx = target[2] / origVb[2], fy = target[3] / origVb[3];
                    var f = Math.max(fx, fy);
                    target[2] = origVb[2] * f;
                    target[3] = origVb[3] * f;
                }
                // Pan-bounds: allow centre to move up to one-half-original out of view either side.
                var maxShiftX = origVb[2] * 0.5, maxShiftY = origVb[3] * 0.5;
                if (target[0] < origVb[0] - maxShiftX) target[0] = origVb[0] - maxShiftX;
                if (target[0] > origVb[0] + origVb[2] - target[2] + maxShiftX) target[0] = origVb[0] + origVb[2] - target[2] + maxShiftX;
                if (target[1] < origVb[1] - maxShiftY) target[1] = origVb[1] - maxShiftY;
                if (target[1] > origVb[1] + origVb[3] - target[3] + maxShiftY) target[1] = origVb[1] + origVb[3] - target[3] + maxShiftY;
            }

            // {passive:false} required: modern browsers default wheel listeners to passive,
            // which silently no-ops e.preventDefault() and lets the page scroll instead of zooming.
            // Phase C.1 of v1.7.2 follow-on plan — matplotlib parity:
            //   step = e.deltaY < 0 ? 1 : -1   (wheel up → +1 = zoom in)
            //   scale = 0.85 ^ step            (matplotlib NavigationToolbar2.scroll_handler L2635)
            // Pre-fix used 1.10/0.90 (~10% per notch); matplotlib's 0.85 (~15%) feels snappier
            // and matches user muscle memory across tools.
            svg.addEventListener('wheel', function(e) {
                e.preventDefault();
                var step = e.deltaY < 0 ? 1 : -1;
                var scale = Math.pow(0.85, step);
                var pt = svg.createSVGPoint();
                pt.x = e.clientX; pt.y = e.clientY;
                var svgPt = pt.matrixTransform(svg.getScreenCTM().inverse());
                vb[0] = svgPt.x - (svgPt.x - vb[0]) * scale;
                vb[1] = svgPt.y - (svgPt.y - vb[1]) * scale;
                vb[2] *= scale; vb[3] *= scale;
                clampVB(vb);
                svg.setAttribute('viewBox', vb.join(' '));
            }, { passive: false });

            // Phase C.2 of v1.7.2 follow-on — axis-lock modifier keys (mirrors matplotlib's
            // _base.py:format_deltas convention, axes3d.py L4492). Holding 'x' locks pan to
            // the X axis; 'y' locks to Y. Modifier is sticky between keydown/keyup so the
            // user can press-hold-drag (chord-style) without timing pressure.
            var lockedAxis = null;
            svg.addEventListener('keydown', function(e) {
                if (e.key === 'x' || e.key === 'y') lockedAxis = e.key;
            });
            svg.addEventListener('keyup', function(e) {
                if (e.key === lockedAxis) lockedAxis = null;
            });

            // Phase 4 of v1.7.2 plan — Pointer Events API for touch + pen + mouse parity.
            // setPointerCapture binds the pointer to the SVG so mouse-up after dragging out of
            // the chart still releases cleanly. Two simultaneous pointers → pinch zoom (the
            // ratio of inter-pointer distances drives the viewBox scale).
            var activePointers = {};
            function pointerCount() { return Object.keys(activePointers).length; }
            function pinchDistance() {
                var ids = Object.keys(activePointers);
                if (ids.length < 2) return 0;
                var a = activePointers[ids[0]], b = activePointers[ids[1]];
                var dx = a.x - b.x, dy = a.y - b.y;
                return Math.sqrt(dx*dx + dy*dy);
            }
            var lastPinchDist = 0;
            svg.addEventListener('pointerdown', function(e) {
                activePointers[e.pointerId] = { x: e.clientX, y: e.clientY };
                if (svg.setPointerCapture) try { svg.setPointerCapture(e.pointerId); } catch (_) {}
                if (pointerCount() === 1) {
                    isPanning = true; startX = e.clientX; startY = e.clientY;
                    svg.style.cursor = 'grabbing';
                } else if (pointerCount() === 2) {
                    isPanning = false;
                    lastPinchDist = pinchDistance();
                }
            });

            svg.addEventListener('pointermove', function(e) {
                if (!(e.pointerId in activePointers)) return;
                activePointers[e.pointerId] = { x: e.clientX, y: e.clientY };
                if (pointerCount() === 2) {
                    var d = pinchDistance();
                    if (lastPinchDist > 0 && d > 0) {
                        var scale = lastPinchDist / d;
                        // Pinch around centre between the two pointers.
                        var ids = Object.keys(activePointers);
                        var cx = (activePointers[ids[0]].x + activePointers[ids[1]].x) / 2;
                        var cy = (activePointers[ids[0]].y + activePointers[ids[1]].y) / 2;
                        var pt = svg.createSVGPoint();
                        pt.x = cx; pt.y = cy;
                        var svgPt = pt.matrixTransform(svg.getScreenCTM().inverse());
                        vb[0] = svgPt.x - (svgPt.x - vb[0]) * scale;
                        vb[1] = svgPt.y - (svgPt.y - vb[1]) * scale;
                        vb[2] *= scale; vb[3] *= scale;
                        svg.setAttribute('viewBox', vb.join(' '));
                    }
                    lastPinchDist = d;
                    return;
                }
                if (!isPanning) return;
                var dx = (e.clientX - startX) * (vb[2] / svg.clientWidth);
                var dy = (e.clientY - startY) * (vb[3] / svg.clientHeight);
                // Phase C.2 — axis lock (matplotlib parity). x-modifier zeroes dy; y zeroes dx.
                if (lockedAxis === 'x') dy = 0;
                else if (lockedAxis === 'y') dx = 0;
                vb[0] -= dx; vb[1] -= dy;
                startX = e.clientX; startY = e.clientY;
                svg.setAttribute('viewBox', vb.join(' '));
            });

            function endPointer(e) {
                delete activePointers[e.pointerId];
                if (pointerCount() < 2) lastPinchDist = 0;
                if (pointerCount() === 0) {
                    isPanning = false;
                    svg.style.cursor = 'default';
                }
            }
            svg.addEventListener('pointerup', endPointer);
            svg.addEventListener('pointercancel', endPointer);

            svg.addEventListener('dblclick', function() {
                vb = origVb.slice();
                svg.setAttribute('viewBox', vb.join(' '));
            });

            svg.addEventListener('keydown', function(e) {
                var step = panStep * (vb[2] / svg.clientWidth || 1);
                switch (e.key) {
                    case '+': case '=': vb[0] += vb[2]*0.05; vb[1] += vb[3]*0.05; vb[2] *= 0.9; vb[3] *= 0.9; break;
                    case '-':           vb[0] -= vb[2]*0.05; vb[1] -= vb[3]*0.05; vb[2] *= 1.1; vb[3] *= 1.1; break;
                    case 'ArrowLeft':  vb[0] -= step; break;
                    case 'ArrowRight': vb[0] += step; break;
                    case 'ArrowUp':    vb[1] -= step; break;
                    case 'ArrowDown':  vb[1] += step; break;
                    case 'Home':       vb = origVb.slice(); break;
                    default: return;
                }
                e.preventDefault();
                svg.setAttribute('viewBox', vb.join(' '));
            });
        })();
        ]]></script>
        """;
}
