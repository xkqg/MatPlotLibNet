// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>Provides the embedded JavaScript IIFE that forwards browser interaction events
/// (wheel, drag, Home key, legend click, and — since v1.2.2 — Shift+drag brush-select and
/// hover) to a bidirectional SignalR <c>ChartHub</c>. Emitted by
/// <see cref="Transforms.SvgTransform"/> when <c>Figure.ServerInteraction == true</c>,
/// replacing the local client-side <c>SvgInteractivityScript</c> + <c>SvgLegendToggleScript</c>.
/// The script assumes a <c>@microsoft/signalr</c> <c>HubConnection</c> has been exposed to the
/// page as <c>window.__mpl_signalr_connection</c> by whichever frontend component hosts the SVG
/// (<c>MplLiveChart</c> for Blazor; the user's own wiring for raw HTML). On discovery failure
/// the script is a graceful no-op — the SVG still renders and no JavaScript errors leak to the
/// browser console.</summary>
internal static class SvgSignalRInteractionScript
{
    /// <summary>Returns the complete <c>&lt;script&gt;</c> block. The v1.2.0 handlers
    /// (OnZoom/OnPan/OnReset/OnLegendToggle) are always included; v1.2.2 brush-select and
    /// hover branches are conditionally appended when the caller sets the respective flags
    /// via <see cref="Builders.ServerInteractionBuilder.EnableBrushSelect"/> /
    /// <see cref="Builders.ServerInteractionBuilder.EnableHover"/>.</summary>
    internal static string GetScript(bool enableBrushSelect = false, bool enableHover = false)
    {
        var sb = new StringBuilder();
        sb.Append(V120Header);

        if (enableBrushSelect)
            sb.Append(BrushSelectBranch);

        if (enableHover)
            sb.Append(HoverBranch);

        sb.Append(V120Footer);
        return sb.ToString();
    }

    // ── v1.2.0 handlers — always emitted when ServerInteraction == true ──

    private const string V120Header = """
        <script type="text/ecmascript"><![CDATA[
        (function mplSignalRInteraction() {
            var svg = document.querySelector('svg[data-chart-id]');
            if (!svg) return;
            var chartId = svg.getAttribute('data-chart-id');

            function hub() { return (typeof window !== 'undefined') ? window.__mpl_signalr_connection : null; }
            function invoke(method, payload) {
                var c = hub();
                if (c && typeof c.invoke === 'function') {
                    try { c.invoke(method, payload); } catch (e) { /* swallow */ }
                }
            }

            // current data-space limits cached on the root svg at render time
            var xMin = parseFloat(svg.getAttribute('data-xmin'));
            var xMax = parseFloat(svg.getAttribute('data-xmax'));
            var yMin = parseFloat(svg.getAttribute('data-ymin'));
            var yMax = parseFloat(svg.getAttribute('data-ymax'));
            var rxMin = parseFloat(svg.getAttribute('data-reset-xmin'));
            var rxMax = parseFloat(svg.getAttribute('data-reset-xmax'));
            var ryMin = parseFloat(svg.getAttribute('data-reset-ymin'));
            var ryMax = parseFloat(svg.getAttribute('data-reset-ymax'));

            svg.setAttribute('tabindex', '0');
            svg.setAttribute('aria-roledescription', 'interactive chart (server-authoritative)');

            svg.addEventListener('wheel', function (e) {
                if (!isFinite(xMin) || !isFinite(xMax)) return;
                e.preventDefault();
                var scale = e.deltaY > 0 ? 1.1 : 0.9;
                var cx = xMin + (xMax - xMin) / 2;
                var cy = yMin + (yMax - yMin) / 2;
                var nxMin = cx - (cx - xMin) * scale;
                var nxMax = cx + (xMax - cx) * scale;
                var nyMin = cy - (cy - yMin) * scale;
                var nyMax = cy + (yMax - cy) * scale;
                xMin = nxMin; xMax = nxMax; yMin = nyMin; yMax = nyMax;
                invoke('OnZoom', {
                    chartId: chartId, axesIndex: 0,
                    xMin: nxMin, xMax: nxMax, yMin: nyMin, yMax: nyMax
                });
            }, { passive: false });

            var dragging = false, lastX = 0, lastY = 0;
            svg.addEventListener('pointerdown', function (e) {
                if (e.button !== 0 || e.shiftKey) return;
                dragging = true; lastX = e.clientX; lastY = e.clientY;
                svg.setPointerCapture(e.pointerId);
            });
            svg.addEventListener('pointermove', function (e) {
                if (!dragging || !isFinite(xMin)) return;
                var rect = svg.getBoundingClientRect();
                var dxPx = e.clientX - lastX;
                var dyPx = e.clientY - lastY;
                lastX = e.clientX; lastY = e.clientY;
                var dxData = -(dxPx / rect.width) * (xMax - xMin);
                var dyData =  (dyPx / rect.height) * (yMax - yMin);
                xMin += dxData; xMax += dxData;
                yMin += dyData; yMax += dyData;
                invoke('OnPan', {
                    chartId: chartId, axesIndex: 0,
                    dxData: dxData, dyData: dyData
                });
            });
            svg.addEventListener('pointerup', function (e) {
                dragging = false;
                try { svg.releasePointerCapture(e.pointerId); } catch (_) {}
            });

            svg.addEventListener('keydown', function (e) {
                if (e.key === 'Home') {
                    e.preventDefault();
                    xMin = rxMin; xMax = rxMax; yMin = ryMin; yMax = ryMax;
                    invoke('OnReset', {
                        chartId: chartId, axesIndex: 0,
                        xMin: rxMin, xMax: rxMax, yMin: ryMin, yMax: ryMax
                    });
                }
            });

            // legend toggle — click on any element with data-series-index
            svg.addEventListener('click', function (e) {
                var t = e.target;
                while (t && t !== svg) {
                    if (t.getAttribute && t.getAttribute('data-series-index') !== null) {
                        var idx = parseInt(t.getAttribute('data-series-index'), 10);
                        invoke('OnLegendToggle', {
                            chartId: chartId, axesIndex: 0, seriesIndex: idx
                        });
                        return;
                    }
                    t = t.parentNode;
                }
            });

        """;

    // ── v1.2.2 brush-select branch — Shift+drag rubber-band ────────────────
    //
    // Marker token: mplBrushSelect (asserted by SvgSignalRInteractionScriptV122Tests).
    //
    // Reuses the existing pointerdown / pointermove / pointerup lifecycle the pan handler
    // above already installs, but only when e.shiftKey is true — so panning and brush-select
    // are gesture-disambiguated by the modifier key. Uses the SAME pixel→data conversion via
    // data-xmin/xmax/ymin/ymax on the root svg, so no new infrastructure is needed.

    private const string BrushSelectBranch = """
            // ── mplBrushSelect — Shift+drag rubber-band → OnBrushSelect ──
            var brushRect = null, brushStart = null;
            function svgPoint(evt) {
                var pt = svg.createSVGPoint();
                pt.x = evt.clientX; pt.y = evt.clientY;
                return pt.matrixTransform(svg.getScreenCTM().inverse());
            }
            function pxToDataX(pxX) {
                var rect = svg.getBoundingClientRect();
                return xMin + ((pxX - rect.left) / rect.width) * (xMax - xMin);
            }
            function pxToDataY(pxY) {
                var rect = svg.getBoundingClientRect();
                return yMax - ((pxY - rect.top) / rect.height) * (yMax - yMin);
            }
            svg.addEventListener('pointerdown', function (e) {
                if (e.button !== 0 || !e.shiftKey) return;
                e.preventDefault();
                brushStart = { clientX: e.clientX, clientY: e.clientY };
                var p = svgPoint(e);
                brushRect = document.createElementNS('http://www.w3.org/2000/svg', 'rect');
                brushRect.setAttribute('x', p.x); brushRect.setAttribute('y', p.y);
                brushRect.setAttribute('width', 0); brushRect.setAttribute('height', 0);
                brushRect.setAttribute('fill', 'rgba(100,149,237,0.25)');
                brushRect.setAttribute('stroke', '#6495ED');
                brushRect.setAttribute('stroke-width', '1');
                brushRect.setAttribute('pointer-events', 'none');
                brushRect.setAttribute('data-mpl-brush', 'true');
                svg.appendChild(brushRect);
                svg.setPointerCapture(e.pointerId);
            });
            svg.addEventListener('pointermove', function (e) {
                if (!brushRect || !brushStart) return;
                var p0 = svgPoint(brushStart), p1 = svgPoint(e);
                var x = Math.min(p0.x, p1.x), y = Math.min(p0.y, p1.y);
                var w = Math.abs(p1.x - p0.x), h = Math.abs(p1.y - p0.y);
                brushRect.setAttribute('x', x); brushRect.setAttribute('y', y);
                brushRect.setAttribute('width', w); brushRect.setAttribute('height', h);
            });
            svg.addEventListener('pointerup', function (e) {
                if (!brushRect || !brushStart) return;
                var dx1 = pxToDataX(brushStart.clientX), dy1 = pxToDataY(brushStart.clientY);
                var dx2 = pxToDataX(e.clientX),          dy2 = pxToDataY(e.clientY);
                var x1 = Math.min(dx1, dx2), y1 = Math.min(dy1, dy2);
                var x2 = Math.max(dx1, dx2), y2 = Math.max(dy1, dy2);
                invoke('OnBrushSelect', {
                    chartId: chartId, axesIndex: 0,
                    x1: x1, y1: y1, x2: x2, y2: y2
                });
                svg.removeChild(brushRect);
                brushRect = null; brushStart = null;
                try { svg.releasePointerCapture(e.pointerId); } catch (_) {}
            });
            svg.addEventListener('keydown', function (e) {
                if (e.key === 'Escape' && brushRect) {
                    svg.removeChild(brushRect);
                    brushRect = null; brushStart = null;
                }
            });

        """;

    // ── v1.2.2 hover branch — cursor → server-rendered tooltip ─────────────
    //
    // Marker token: mplHoverRoundtrip. Hover events fire every mousemove which can saturate
    // the hub; the dispatcher coalesces to at most one in-flight request via a `pending`
    // flag, with a queued point that issues when the response arrives. The tooltip overlay
    // reuses the same role="tooltip" / aria-live="polite" semantics SvgCustomTooltipScript
    // uses for static tooltips.

    private const string HoverBranch = """
            // ── mplHoverRoundtrip — cursor → OnHover → server tooltip ──
            var hoverTooltip = null, hoverPending = false, hoverQueued = null;
            function ensureTooltip() {
                if (hoverTooltip) return hoverTooltip;
                hoverTooltip = document.createElement('div');
                hoverTooltip.setAttribute('role', 'tooltip');
                hoverTooltip.setAttribute('aria-live', 'polite');
                hoverTooltip.style.position = 'fixed';
                hoverTooltip.style.pointerEvents = 'none';
                hoverTooltip.style.background = 'rgba(33,33,33,0.92)';
                hoverTooltip.style.color = '#fff';
                hoverTooltip.style.padding = '4px 8px';
                hoverTooltip.style.borderRadius = '4px';
                hoverTooltip.style.font = '12px sans-serif';
                hoverTooltip.style.zIndex = '99999';
                hoverTooltip.style.display = 'none';
                document.body.appendChild(hoverTooltip);
                return hoverTooltip;
            }
            var lastHoverClient = null;
            svg.addEventListener('pointermove', function (e) {
                if (brushRect || dragging) return;
                var rect = svg.getBoundingClientRect();
                var x = xMin + ((e.clientX - rect.left) / rect.width) * (xMax - xMin);
                var y = yMax - ((e.clientY - rect.top) / rect.height) * (yMax - yMin);
                lastHoverClient = { clientX: e.clientX, clientY: e.clientY };
                if (hoverPending) {
                    hoverQueued = { x: x, y: y };
                    return;
                }
                hoverPending = true;
                invoke('OnHover', { chartId: chartId, axesIndex: 0, x: x, y: y });
                // invoke is fire-and-forget; clear pending when server responds (below)
                // or after a short timeout to avoid deadlock on dropped packets.
                setTimeout(function () { hoverPending = false; }, 500);
            });
            svg.addEventListener('pointerleave', function () {
                if (hoverTooltip) hoverTooltip.style.display = 'none';
                hoverQueued = null;
            });
            // Hook onto the existing hub connection to receive tooltip responses.
            var _hubForHover = hub();
            if (_hubForHover && typeof _hubForHover.on === 'function') {
                _hubForHover.on('ReceiveTooltipContent', function (id, html) {
                    if (id !== chartId) return;
                    hoverPending = false;
                    var tt = ensureTooltip();
                    tt.innerHTML = html;
                    if (lastHoverClient) {
                        tt.style.left = (lastHoverClient.clientX + 12) + 'px';
                        tt.style.top  = (lastHoverClient.clientY + 12) + 'px';
                    }
                    tt.style.display = 'block';
                    if (hoverQueued) {
                        var q = hoverQueued; hoverQueued = null;
                        hoverPending = true;
                        invoke('OnHover', { chartId: chartId, axesIndex: 0, x: q.x, y: q.y });
                        setTimeout(function () { hoverPending = false; }, 500);
                    }
                });
            }

        """;

    private const string V120Footer = """
        })();
        ]]></script>
        """;
}
