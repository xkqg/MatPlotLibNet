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
    internal static string GetScript(bool enableBrushSelect = false, bool enableHover = false,
        bool enableDataCursor = false)
    {
        var sb = new StringBuilder();
        sb.Append(V120Header);

        if (enableBrushSelect)
            sb.Append(BrushSelectBranch);

        if (enableHover)
            sb.Append(HoverBranch);

        if (enableDataCursor)
            sb.Append(DataCursorBranch);

        sb.Append(V120Footer);
        return sb.ToString();
    }

    // ── v1.2.0 handlers — always emitted when ServerInteraction == true ──

    private const string V120Header = """
        <script type="text/ecmascript"><![CDATA[
        (function mplSignalRInteraction() {
            // Self-locate through document.currentScript, like every other chart script, so two charts on one page
            // each wire their own svg; the document's first chart is only the fallback where currentScript is absent.
            var svg = (document.currentScript && document.currentScript.parentNode) || document.querySelector('svg[data-chart-id]');
            if (!svg || !svg.getAttribute) return;
            var chartId = svg.getAttribute('data-chart-id');

            function hub() { return (typeof window !== 'undefined') ? window.__mpl_signalr_connection : null; }
            function invoke(method, payload) {
                var c = hub();
                if (c && typeof c.invoke === 'function') {
                    try { c.invoke(method, payload); } catch (e) { /* swallow */ }
                }
            }

            // -- the view: what the pointer is converted with --
            // Every axes writes its view (data-axes-index, the plot area data-plot-x/y/w/h in SVG user units and
            // the range it actually drew, data-xmin/xmax/ymin/ymax -- after autoscale, not the axis' explicit
            // limits). The pointer, in user units, is converted by DataTransform.PixelToData's rule with the view of
            // the axes under it: the one whose plot area is nearest (0 inside it). An axes drawn without a Cartesian
            // range (polar, 3-D), or a figure without axes, falls back to the limits cached on the root over the
            // whole picture. Wheel, pan and Home move a view; it starts at what was drawn, and Home returns to that.
            // A log or symlog axis draws in a scaled space: its view range is in that space and the view names the
            // scale (data-xscale / data-yscale, data-xlinthresh / data-ylinthresh). The pointer is interpolated in the
            // drawn space and the scale inverted (unscale: AxisScaleExtensions.Inverse's rule), so every value sent to
            // the server is in data units; a pan sends its delta in the drawn space, where PanEvent applies it.
            function num(el, name) { return parseFloat(el.getAttribute(name)); }
            function unscale(v, scale, t) {
                if (scale === 'log') return Math.pow(10, v);
                if (scale === 'symlog') {
                    if (!(t > 0)) t = 1;
                    var a = Math.abs(v);
                    return a <= t ? v : (v < 0 ? -1 : 1) * t * Math.pow(10, a / t - 1);
                }
                return v;
            }
            function dataX(view, v) { return unscale(v, view.xScale, view.xLinThresh); }
            function dataY(view, v) { return unscale(v, view.yScale, view.yLinThresh); }
            // limits of a view in data units: what a zoom or a reset applies to the axis
            function limits(view, r) {
                return { xMin: dataX(view, r.xMin), xMax: dataX(view, r.xMax), yMin: dataY(view, r.yMin), yMax: dataY(view, r.yMax) };
            }
            var root = {
                axesIndex: 0, mapped: false,
                xMin: num(svg, 'data-xmin'), xMax: num(svg, 'data-xmax'),
                yMin: num(svg, 'data-ymin'), yMax: num(svg, 'data-ymax'),
                reset: {
                    xMin: num(svg, 'data-reset-xmin'), xMax: num(svg, 'data-reset-xmax'),
                    yMin: num(svg, 'data-reset-ymin'), yMax: num(svg, 'data-reset-ymax')
                }
            };
            function readView(el) {
                var v = {
                    axesIndex: parseInt(el.getAttribute('data-axes-index'), 10) || 0,
                    x: num(el, 'data-plot-x'), y: num(el, 'data-plot-y'),
                    w: num(el, 'data-plot-w'), h: num(el, 'data-plot-h'),
                    xMin: num(el, 'data-xmin'), xMax: num(el, 'data-xmax'),
                    yMin: num(el, 'data-ymin'), yMax: num(el, 'data-ymax'),
                    xScale: el.getAttribute('data-xscale'), xLinThresh: num(el, 'data-xlinthresh'),
                    yScale: el.getAttribute('data-yscale'), yLinThresh: num(el, 'data-ylinthresh')
                };
                v.mapped = v.w > 0 && v.h > 0 && isFinite(v.xMin) && isFinite(v.xMax) && isFinite(v.yMin) && isFinite(v.yMax);
                var start = v.mapped ? v : root;
                v.xMin = start.xMin; v.xMax = start.xMax; v.yMin = start.yMin; v.yMax = start.yMax;
                v.reset = v.mapped ? { xMin: v.xMin, xMax: v.xMax, yMin: v.yMin, yMax: v.yMax } : root.reset;
                return v;
            }
            var views = [];
            var axesEls = svg.querySelectorAll('[data-axes-index]');
            for (var ai = 0; ai < axesEls.length; ai++) views.push(readView(axesEls[ai]));
            if (views.length === 0) views.push(root);

            // client pixel -> SVG user units
            function userPoint(evt) {
                var m = svg.getScreenCTM ? svg.getScreenCTM() : null;
                if (!m) {
                    var r = svg.getBoundingClientRect();
                    return { x: evt.clientX - r.left, y: evt.clientY - r.top };
                }
                var pt = svg.createSVGPoint();
                pt.x = evt.clientX; pt.y = evt.clientY;
                return pt.matrixTransform(m.inverse());
            }
            // the axes under the pointer: the nearest plot area, the first one that contains the point
            function viewAt(evt) {
                var p = userPoint(evt), best = views[0], bestD = Infinity;
                for (var i = 0; i < views.length; i++) {
                    var v = views[i];
                    if (!(v.w > 0 && v.h > 0)) continue;
                    var dx = Math.max(v.x - p.x, 0, p.x - (v.x + v.w));
                    var dy = Math.max(v.y - p.y, 0, p.y - (v.y + v.h));
                    var d = dx * dx + dy * dy;
                    if (d < bestD) { bestD = d; best = v; }
                }
                return best;
            }
            // where across the plot the pointer is: 0..1 from the left, 0..1 from the top
            function fractionAt(view, evt) {
                if (view.mapped) {
                    var p = userPoint(evt);
                    return { fx: (p.x - view.x) / view.w, fy: (p.y - view.y) / view.h };
                }
                var r = svg.getBoundingClientRect();
                return { fx: (evt.clientX - r.left) / r.width, fy: (evt.clientY - r.top) / r.height };
            }
            // the pointer in the drawn space of a view (the scaled space on a log or symlog axis)
            function drawnAt(view, evt) {
                var f = fractionAt(view, evt);
                return {
                    x: view.xMin + f.fx * (view.xMax - view.xMin),
                    y: view.yMin + (1 - f.fy) * (view.yMax - view.yMin)
                };
            }
            // the ONE conversion: pointer -> data, with a view (DataTransform.PixelToData's rule)
            function dataAt(view, evt) {
                var s = drawnAt(view, evt);
                return { x: dataX(view, s.x), y: dataY(view, s.y) };
            }

            svg.setAttribute('tabindex', '0');
            svg.setAttribute('aria-roledescription', 'interactive chart (server-authoritative)');

            // shared gesture state: the pan drag here, the Shift-drag brush when that branch is emitted
            var dragging = false, dragView = null, dragLast = null;
            var brushRect = null, brushStart = null, brushView = null;

            // wheel: zoom the axes under the pointer about the data point under the pointer
            svg.addEventListener('wheel', function (e) {
                var v = viewAt(e);
                if (!isFinite(v.xMin) || !isFinite(v.xMax)) return;
                e.preventDefault();
                var scale = e.deltaY > 0 ? 1.1 : 0.9;
                var c = drawnAt(v, e);
                v.xMin = c.x - (c.x - v.xMin) * scale; v.xMax = c.x + (v.xMax - c.x) * scale;
                v.yMin = c.y - (c.y - v.yMin) * scale; v.yMax = c.y + (v.yMax - c.y) * scale;
                var z = limits(v, v);
                invoke('OnZoom', {
                    chartId: chartId, axesIndex: v.axesIndex,
                    xMin: z.xMin, xMax: z.xMax, yMin: z.yMin, yMax: z.yMax
                });
            }, { passive: false });

            // drag: the data follows the pointer. The event carries the view it moved (inView), so an axes that
            // scales itself -- no limits on the server yet -- pans from what is on screen.
            svg.addEventListener('pointerdown', function (e) {
                if (e.button !== 0 || e.shiftKey) return;
                dragging = true; dragView = viewAt(e);
                dragLast = { clientX: e.clientX, clientY: e.clientY };
                svg.setPointerCapture(e.pointerId);
            });
            svg.addEventListener('pointermove', function (e) {
                if (!dragging || !dragView || !isFinite(dragView.xMin)) return;
                var v = dragView;
                var f0 = fractionAt(v, dragLast), f1 = fractionAt(v, e);
                dragLast = { clientX: e.clientX, clientY: e.clientY };
                var inView = limits(v, v);
                var dxData = -(f1.fx - f0.fx) * (v.xMax - v.xMin);
                var dyData =  (f1.fy - f0.fy) * (v.yMax - v.yMin);
                v.xMin += dxData; v.xMax += dxData;
                v.yMin += dyData; v.yMax += dyData;
                invoke('OnPan', {
                    chartId: chartId, axesIndex: v.axesIndex,
                    dxData: dxData, dyData: dyData, inView: inView
                });
            });
            svg.addEventListener('pointerup', function (e) {
                dragging = false; dragView = null;
                try { svg.releasePointerCapture(e.pointerId); } catch (_) {}
            });

            // Home: every axes back to the view it was first drawn with
            svg.addEventListener('keydown', function (e) {
                if (e.key === 'Home') {
                    e.preventDefault();
                    for (var i = 0; i < views.length; i++) {
                        var v = views[i], r = v.reset, d = limits(v, r);
                        v.xMin = r.xMin; v.xMax = r.xMax; v.yMin = r.yMin; v.yMax = r.yMax;
                        invoke('OnReset', {
                            chartId: chartId, axesIndex: v.axesIndex,
                            xMin: d.xMin, xMax: d.xMax, yMin: d.yMin, yMax: d.yMax
                        });
                    }
                }
            });

            // Legend-toggle dispatcher. A toggle comes ONLY from a legend entry: walk up from the click target
            // to the element carrying the legend attribute (the series' slot, the value the server toggles). A
            // plotted series group carries the series index too, but a click there is a click on the data --
            // the data cursor's -- and must never hide the line that was clicked.
            var LEGEND_ATTR = 'data-leg' + 'end-index';
            svg.addEventListener('click', function (e) {
                var t = e.target;
                while (t && t !== svg) {
                    var legend = t.getAttribute ? t.getAttribute(LEGEND_ATTR) : null;
                    if (legend !== null) {
                        invoke('OnLegendToggle', {
                            chartId: chartId, axesIndex: viewAt(e).axesIndex, seriesIndex: parseInt(legend, 10)
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
    // are gesture-disambiguated by the modifier key. Uses the SAME pixel→data conversion (dataAt
    // with the view of the axes under the pointer) as hover, zoom and pan.

    private const string BrushSelectBranch = """
            // ── mplBrushSelect — Shift+drag rubber-band → OnBrushSelect ──
            // Both corners convert with the view of the axes the drag started in (dataAt, the one conversion).
            svg.addEventListener('pointerdown', function (e) {
                if (e.button !== 0 || !e.shiftKey) return;
                e.preventDefault();
                brushStart = { clientX: e.clientX, clientY: e.clientY };
                brushView = viewAt(e);
                var p = userPoint(e);
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
                var p0 = userPoint(brushStart), p1 = userPoint(e);
                var x = Math.min(p0.x, p1.x), y = Math.min(p0.y, p1.y);
                var w = Math.abs(p1.x - p0.x), h = Math.abs(p1.y - p0.y);
                brushRect.setAttribute('x', x); brushRect.setAttribute('y', y);
                brushRect.setAttribute('width', w); brushRect.setAttribute('height', h);
            });
            svg.addEventListener('pointerup', function (e) {
                if (!brushRect || !brushStart) return;
                var d1 = dataAt(brushView, brushStart), d2 = dataAt(brushView, e);
                invoke('OnBrushSelect', {
                    chartId: chartId, axesIndex: brushView.axesIndex,
                    x1: Math.min(d1.x, d2.x), y1: Math.min(d1.y, d2.y),
                    x2: Math.max(d1.x, d2.x), y2: Math.max(d1.y, d2.y)
                });
                svg.removeChild(brushRect);
                brushRect = null; brushStart = null; brushView = null;
                try { svg.releasePointerCapture(e.pointerId); } catch (_) {}
            });
            svg.addEventListener('keydown', function (e) {
                if (e.key === 'Escape' && brushRect) {
                    svg.removeChild(brushRect);
                    brushRect = null; brushStart = null; brushView = null;
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
            // A page runs this script again each time it mounts a redrawn chart, and what a run leaves behind outlives
            // the SVG it was wired to. So the branch keeps ONE tooltip element per chart (marked with the chart id and
            // reused by every run) and ONE ReceiveTooltipContent handler per connection, which hands a tooltip to the
            // latest run of the chart it names. A run first hides the tooltip of the chart it replaces: the
            // pointerleave that would have hidden it sat on the removed SVG.
            var hoverPending = false, hoverQueued = null;
            function findTooltip() {
                var all = document.querySelectorAll('[data-mpl-hover-tooltip]');
                for (var i = 0; i < all.length; i++)
                    if (all[i].getAttribute('data-mpl-hover-tooltip') === chartId) return all[i];
                return null;
            }
            var staleTooltip = findTooltip();
            if (staleTooltip) staleTooltip.style.display = 'none';
            function ensureTooltip() {
                var tt = findTooltip();
                if (tt) return tt;
                tt = document.createElement('div');
                tt.setAttribute('data-mpl-hover-tooltip', chartId);
                tt.setAttribute('role', 'tooltip');
                tt.setAttribute('aria-live', 'polite');
                tt.style.position = 'fixed';
                tt.style.pointerEvents = 'none';
                tt.style.background = 'rgba(33,33,33,0.92)';
                tt.style.color = '#fff';
                tt.style.padding = '4px 8px';
                tt.style.borderRadius = '4px';
                tt.style.font = '12px sans-serif';
                tt.style.zIndex = '99999';
                tt.style.display = 'none';
                document.body.appendChild(tt);
                return tt;
            }
            var lastHoverClient = null;
            svg.addEventListener('pointermove', function (e) {
                if (brushRect || dragging) return;
                var v = viewAt(e), d = dataAt(v, e);
                lastHoverClient = { clientX: e.clientX, clientY: e.clientY };
                if (hoverPending) {
                    hoverQueued = { axesIndex: v.axesIndex, x: d.x, y: d.y };
                    return;
                }
                hoverPending = true;
                invoke('OnHover', { chartId: chartId, axesIndex: v.axesIndex, x: d.x, y: d.y });
                // invoke is fire-and-forget; clear pending when server responds (below)
                // or after a short timeout to avoid deadlock on dropped packets.
                setTimeout(function () { hoverPending = false; }, 500);
            });
            svg.addEventListener('pointerleave', function () {
                var tt = findTooltip();
                if (tt) tt.style.display = 'none';
                hoverQueued = null;
            });
            function showTooltip(html) {
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
                    invoke('OnHover', { chartId: chartId, axesIndex: q.axesIndex, x: q.x, y: q.y });
                    setTimeout(function () { hoverPending = false; }, 500);
                }
            }
            // The tooltip responses arrive on the page's hub connection: one handler on it, routing by chart id.
            var _hubForHover = hub();
            if (_hubForHover && typeof _hubForHover.on === 'function') {
                var tooltipRoutes = _hubForHover.__mplTooltipRoutes;
                if (!tooltipRoutes) {
                    tooltipRoutes = {};
                    _hubForHover.__mplTooltipRoutes = tooltipRoutes;
                    _hubForHover.on('ReceiveTooltipContent', function (id, html) {
                        var show = tooltipRoutes[id];
                        if (show) show(html);
                    });
                }
                tooltipRoutes[chartId] = showTooltip;
            }

        """;

    private const string DataCursorBranch = """
            // -- mplDataCursor -- click a marker -> OnDataCursor --
            // Every series is drawn inside a group carrying data-series-index and the series' own aria-label,
            // so the click knows WHICH series it landed on without the page holding any of the data. The group
            // also carries the mapping its series was drawn with -- data-plot-x/y/w/h (the plot area) and
            // data-xmin/xmax/ymin/ymax (the range of the series' OWN axes: primary, secondary Y or secondary X) --
            // and the pointer is converted with it (readView + dataAt: the one conversion the header defines). A
            // group drawn without a Cartesian range (polar, 3-D) carries none; then the view of the axes under the
            // pointer is used, which for such axes is the limits cached on the root.
            svg.addEventListener('click', function (e) {
                var target = e.target;
                if (!target || typeof target.closest !== 'function') return;
                var group = target.closest('[data-series-index]');
                if (!group) return;
                var axesView = viewAt(e), groupView = readView(group);
                var d = dataAt(groupView.mapped ? groupView : axesView, e);
                var rect = svg.getBoundingClientRect();
                invoke('OnDataCursor', {
                    chartId: chartId,
                    axesIndex: axesView.axesIndex,
                    annotation: {
                        seriesLabel: group.getAttribute('aria-label'),
                        dataX: d.x,
                        dataY: d.y,
                        pixelX: e.clientX - rect.left,
                        pixelY: e.clientY - rect.top,
                        axesIndex: axesView.axesIndex
                    }
                });
            });

        """;

    private const string V120Footer = """
        })();
        ]]></script>
        """;
}
