// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>Provides the embedded JavaScript IIFE that forwards browser interaction events
/// (wheel, drag, Home key, legend click and, when enabled, Shift+drag brush-select, hover and
/// a click on a marker) to a bidirectional SignalR <c>ChartHub</c>. Emitted by
/// <see cref="Transforms.SvgTransform"/> when <c>Figure.ServerInteraction == true</c>,
/// replacing the local client-side <c>SvgInteractivityScript</c> + <c>SvgLegendToggleScript</c>.
/// The script assumes a <c>@microsoft/signalr</c> <c>HubConnection</c> has been exposed to the
/// page as <c>window.__mpl_signalr_connection</c> by whichever frontend component hosts the SVG
/// (<c>MplLiveChart</c> for Blazor; the user's own wiring for raw HTML). On discovery failure
/// the script is a graceful no-op — the SVG still renders and no JavaScript errors leak to the
/// browser console.</summary>
internal static class SvgSignalRInteractionScript
{
    /// <summary>Returns the complete <c>&lt;script&gt;</c> block. The zoom, pan, reset and legend-toggle
    /// handlers (OnZoom/OnPan/OnReset/OnLegendToggle) are always included; the brush-select, hover and
    /// data-cursor branches are appended when the caller sets the respective flags
    /// via <see cref="Builders.ServerInteractionBuilder.EnableBrushSelect"/> /
    /// <see cref="Builders.ServerInteractionBuilder.EnableHover"/> /
    /// <see cref="Builders.ServerInteractionBuilder.EnableDataCursor"/>; the viewer-cursor branch (this viewer's
    /// pointer sent to the others, the others' drawn here) when <paramref name="enableViewerCursors"/> is set.
    /// Every run also marks the chart busy while a drag or a band is in progress and idle after it
    /// (<c>data-mpl-busy</c>, <c>mpl:idle</c>), and raises <c>mpl:viewchanged</c> when another viewer moves the view.</summary>
    internal static string GetScript(bool enableBrushSelect = false, bool enableHover = false,
        bool enableDataCursor = false, bool enableViewerCursors = false)
    {
        var sb = new StringBuilder();
        sb.Append(V120Header);

        if (enableBrushSelect)
            sb.Append(BrushSelectBranch);

        if (enableHover)
            sb.Append(HoverBranch);

        if (enableDataCursor)
            sb.Append(DataCursorBranch);

        if (enableViewerCursors)
            sb.Append(ViewerCursorBranch);

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

            // The connection to send through: the one the chart's host element carries (a live chart component gives its
            // chart a proxy to the connection its client holds, one per chart, so two live charts on a page cannot reach
            // for the same global), else the page's own window.__mpl_signalr_connection.
            function hub() {
                var host = svg.closest ? svg.closest('[data-mpl-live]') : null;
                if (host && host.__mplHub) return host.__mplHub;
                return (typeof window !== 'undefined') ? window.__mpl_signalr_connection : null;
            }
            function invoke(method, payload) {
                var c = hub();
                if (c && typeof c.invoke === 'function') {
                    try {
                        var sent = c.invoke(method, payload);
                        // A connection that is down rejects the send; that must not surface as an unhandled rejection.
                        if (sent && typeof sent.catch === 'function') sent.catch(function () { /* swallow */ });
                    } catch (e) { /* swallow */ }
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
            // One outward-stacked secondary Y axis (ROADMAP.md row 101), read from the data-sec-<side>-<slot>-*
            // attributes SvgRenderContext.AppendSecondaryViews writes. side is sent to the server as AxisSide's
            // ORDINAL (Right = 1, Left = 0), not its name: the default JSON hub protocol carries no
            // JsonStringEnumConverter (pinned by SignalRInteractionTestsV122.OnZoom_WithSecondaryRanges_BindsAxisSideFromItsOrdinal_NotItsName).
            function readSecondarySlot(el, side, sideOrdinal, slot) {
                var prefix = 'data-sec-' + side + '-' + slot + '-';
                var s = {
                    sideOrdinal: sideOrdinal, slot: slot,
                    yMin: num(el, prefix + 'ymin'), yMax: num(el, prefix + 'ymax'),
                    yScale: el.getAttribute(prefix + 'yscale'), yLinThresh: num(el, prefix + 'ylinthresh')
                };
                s.resetYMin = s.yMin; s.resetYMax = s.yMax;
                return s;
            }
            function readSecondarySlots(el) {
                var out = [];
                var rightCount = parseInt(el.getAttribute('data-sec-right-count'), 10) || 0;
                for (var i = 0; i < rightCount; i++) out.push(readSecondarySlot(el, 'right', 1, i));
                var leftCount = parseInt(el.getAttribute('data-sec-left-count'), 10) || 0;
                for (var j = 0; j < leftCount; j++) out.push(readSecondarySlot(el, 'left', 0, j));
                return out;
            }
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
                v.secondary = v.mapped ? readSecondarySlots(el) : [];
                return v;
            }
            // Every secondary slot's new range, each unscaled in ITS OWN units — read, not re-derived, by the
            // caller: zoomSecondary/panSecondary/resetSecondary each compute the drawn-space value their own way
            // (ROADMAP.md row 100), this only wraps the result in the shape OnZoom/OnPan/OnReset send.
            function secondaryRangesPayload(view) {
                var out = [];
                for (var i = 0; i < view.secondary.length; i++) {
                    var s = view.secondary[i];
                    out.push({
                        side: s.sideOrdinal, slotIndex: s.slot,
                        range: { xMin: 0, xMax: 0, yMin: dataY(s, s.yMin), yMax: dataY(s, s.yMax) }
                    });
                }
                return out;
            }
            // Zooms every secondary slot about the SAME fraction-of-plot point the primary zoomed about, so a log
            // or symlog secondary axis narrows correctly too (the drawn-space point under the pointer, read
            // through that slot's own range, mirrors drawnAt's rule for the primary).
            function zoomSecondary(view, f, scale) {
                for (var i = 0; i < view.secondary.length; i++) {
                    var s = view.secondary[i];
                    var sy = s.yMin + (1 - f.fy) * (s.yMax - s.yMin);
                    s.yMin = sy - (sy - s.yMin) * scale;
                    s.yMax = sy + (s.yMax - sy) * scale;
                }
                return secondaryRangesPayload(view);
            }
            // Shifts every secondary slot by the SAME fraction-of-plot delta the primary panned by, scaled to
            // THAT slot's own span -- the same pixel move is a different data-space delta on an axis with a
            // different range (mirrors PanEvent.Shifted's forward/inverse rule, in the drawn space already).
            function panSecondary(view, f1, f0) {
                for (var i = 0; i < view.secondary.length; i++) {
                    var s = view.secondary[i];
                    var dy = (f1.fy - f0.fy) * (s.yMax - s.yMin);
                    s.yMin += dy; s.yMax += dy;
                }
                return secondaryRangesPayload(view);
            }
            // Restores every secondary slot to the range it had when this view was first read.
            function resetSecondary(view) {
                for (var i = 0; i < view.secondary.length; i++) {
                    var s = view.secondary[i];
                    s.yMin = s.resetYMin; s.yMax = s.resetYMax;
                }
                return secondaryRangesPayload(view);
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

            // A page that mounts a redrawn chart replaces this svg, and with it the gesture in progress on it. So the
            // chart says when it is busy (a drag, a band): the code that mounts waits for data-mpl-busy to go and for
            // the mpl:idle event, and mounts the latest chart then.
            function busy(on) {
                if (on) { svg.setAttribute('data-mpl-busy', 'true'); return; }
                if (svg.getAttribute('data-mpl-busy') === null) return;
                svg.removeAttribute('data-mpl-busy');
                svg.dispatchEvent(new CustomEvent('mpl:idle', { bubbles: true }));
            }

            // What the connection says about the other viewers arrives on the page's connection: ONE handler per message
            // on it, routing by chart id to the latest run of that chart (a page runs this script again for every
            // redrawn chart, and what a run leaves on the connection outlives the svg it was wired to).
            var peerHub = hub();
            var peerRoutes = null;
            if (peerHub && typeof peerHub.on === 'function') {
                peerRoutes = peerHub.__mplPeerRoutes;
                if (!peerRoutes) {
                    peerRoutes = {};
                    peerHub.__mplPeerRoutes = peerRoutes;
                    peerHub.on('ViewChanged', function (id, viewerId, name) {
                        var r = peerRoutes[id];
                        if (r && r.changed) r.changed(viewerId, name);
                    });
                }
                peerRoutes[chartId] = {
                    changed: function (viewerId, name) {
                        svg.dispatchEvent(new CustomEvent('mpl:viewchanged', { detail: { viewerId: viewerId, viewerName: name }, bubbles: true }));
                    }
                };
            }

            // wheel: zoom the axes under the pointer about the data point under the pointer
            svg.addEventListener('wheel', function (e) {
                var v = viewAt(e);
                if (!isFinite(v.xMin) || !isFinite(v.xMax)) return;
                e.preventDefault();
                var scale = e.deltaY > 0 ? 1.1 : 0.9;
                var f = fractionAt(v, e);
                var c = drawnAt(v, e);
                v.xMin = c.x - (c.x - v.xMin) * scale; v.xMax = c.x + (v.xMax - c.x) * scale;
                v.yMin = c.y - (c.y - v.yMin) * scale; v.yMax = c.y + (v.yMax - c.y) * scale;
                var z = limits(v, v);
                invoke('OnZoom', {
                    chartId: chartId, axesIndex: v.axesIndex,
                    xMin: z.xMin, xMax: z.xMax, yMin: z.yMin, yMax: z.yMax,
                    secondaryRanges: zoomSecondary(v, f, scale)
                });
            }, { passive: false });

            // drag: the data follows the pointer. The event carries the view it moved (inView), so an axes that
            // scales itself -- no limits on the server yet -- pans from what is on screen.
            svg.addEventListener('pointerdown', function (e) {
                if (e.button !== 0 || e.shiftKey) return;
                dragging = true; dragView = viewAt(e);
                dragLast = { clientX: e.clientX, clientY: e.clientY };
                busy(true);
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
                    dxData: dxData, dyData: dyData, inView: inView,
                    secondaryRanges: panSecondary(v, f1, f0)
                });
            });
            function endDrag(e) {
                if (!dragging) return;
                dragging = false; dragView = null;
                busy(false);
                try { svg.releasePointerCapture(e.pointerId); } catch (_) {}
            }
            svg.addEventListener('pointerup', endDrag);
            svg.addEventListener('pointercancel', endDrag);

            // Home: every axes back to the view it was first drawn with
            svg.addEventListener('keydown', function (e) {
                if (e.key === 'Home') {
                    e.preventDefault();
                    for (var i = 0; i < views.length; i++) {
                        var v = views[i], r = v.reset, d = limits(v, r);
                        v.xMin = r.xMin; v.xMax = r.xMax; v.yMin = r.yMin; v.yMax = r.yMax;
                        invoke('OnReset', {
                            chartId: chartId, axesIndex: v.axesIndex,
                            xMin: d.xMin, xMax: d.xMax, yMin: d.yMin, yMax: d.yMax,
                            secondaryRanges: resetSecondary(v)
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
                busy(true);
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
                busy(false);
                try { svg.releasePointerCapture(e.pointerId); } catch (_) {}
            });
            svg.addEventListener('pointercancel', function () {
                if (!brushRect) return;
                svg.removeChild(brushRect);
                brushRect = null; brushStart = null; brushView = null;
                busy(false);
            });
            svg.addEventListener('keydown', function (e) {
                if (e.key === 'Escape' && brushRect) {
                    svg.removeChild(brushRect);
                    brushRect = null; brushStart = null; brushView = null;
                    busy(false);
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

    // ── viewer cursors — this viewer's pointer to the others, theirs drawn here ──
    //
    // Opt-in (Figure.EnableViewerCursors). The pointer is sent at most once in CURSOR_INTERVAL, the last position of a
    // burst by a trailing timer, so a still pointer is where the others see it. The others' pointers arrive as
    // ReceiveCursor (never echoed to the sender) and ViewerLeft; each viewer has ONE marker in a layer that does not take
    // the pointer, found by viewer id and moved, not added again.

    private const string ViewerCursorBranch = """
            // ── mplViewerCursors — my pointer out, theirs in ──
            var CURSOR_INTERVAL = 50, lastCursorSent = 0, cursorTimer = null, cursorPending = null;
            function sendCursor(p) {
                lastCursorSent = Date.now(); cursorPending = null;
                invoke('OnCursor', { chartId: chartId, axesIndex: p.axesIndex, x: p.x, y: p.y });
            }
            svg.addEventListener('pointermove', function (e) {
                var v = viewAt(e);
                if (!v || !v.mapped) return;
                var d = dataAt(v, e), p = { axesIndex: v.axesIndex, x: d.x, y: d.y };
                var wait = CURSOR_INTERVAL - (Date.now() - lastCursorSent);
                if (wait <= 0) { sendCursor(p); return; }
                cursorPending = p;
                if (!cursorTimer) cursorTimer = setTimeout(function () {
                    cursorTimer = null;
                    if (cursorPending) sendCursor(cursorPending);
                }, wait);
            });

            // the forward of unscale: data -> the drawn space of an axis (log, symlog or linear)
            function scaleFwd(value, scale, t) {
                if (scale === 'log') return Math.log(value) / Math.LN10;
                if (scale === 'symlog') {
                    if (!(t > 0)) t = 1;
                    var a = Math.abs(value);
                    return a <= t ? value : (value < 0 ? -1 : 1) * t * (Math.log(a / t) / Math.LN10 + 1);
                }
                return value;
            }
            function escapeText(s) {
                return String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
            }
            function viewByIndex(index) {
                for (var i = 0; i < views.length; i++) if (views[i].axesIndex === index) return views[i];
                return null;
            }
            function peerLayer() {
                var layer = svg.querySelector('[data-mpl-peers]');
                if (layer) return layer;
                layer = document.createElementNS('http://www.w3.org/2000/svg', 'g');
                layer.setAttribute('data-mpl-peers', 'true');
                layer.setAttribute('pointer-events', 'none');
                layer.setAttribute('aria-hidden', 'true');
                svg.appendChild(layer);
                return layer;
            }
            function peerMarker(layer, viewerId) {
                var all = layer.querySelectorAll('[data-mpl-peer]');
                for (var i = 0; i < all.length; i++) if (all[i].getAttribute('data-mpl-peer') === viewerId) return all[i];
                return null;
            }
            function showPeer(viewerId, name, axesIndex, x, y) {
                var v = viewByIndex(axesIndex);
                if (!v || !v.mapped) return;
                var fx = (scaleFwd(x, v.xScale, v.xLinThresh) - v.xMin) / (v.xMax - v.xMin);
                var fy = (scaleFwd(y, v.yScale, v.yLinThresh) - v.yMin) / (v.yMax - v.yMin);
                if (!isFinite(fx) || !isFinite(fy)) return;
                var px = v.x + fx * v.w, py = v.y + (1 - fy) * v.h;
                var layer = peerLayer(), marker = peerMarker(layer, viewerId);
                if (!marker) {
                    marker = document.createElementNS('http://www.w3.org/2000/svg', 'g');
                    marker.setAttribute('data-mpl-peer', viewerId);
                    var dot = document.createElementNS('http://www.w3.org/2000/svg', 'circle');
                    dot.setAttribute('r', 4); dot.setAttribute('fill', '#E4572E'); dot.setAttribute('stroke', '#ffffff');
                    marker.appendChild(dot);
                    var label = document.createElementNS('http://www.w3.org/2000/svg', 'text');
                    label.setAttribute('x', 7); label.setAttribute('y', -7);
                    label.setAttribute('font-size', 11); label.setAttribute('fill', '#333333');
                    label.innerHTML = escapeText(name);
                    marker.appendChild(label);
                    layer.appendChild(marker);
                }
                marker.setAttribute('transform', 'translate(' + px + ',' + py + ')');
            }
            function hidePeer(viewerId) {
                var layer = svg.querySelector('[data-mpl-peers]');
                if (!layer) return;
                var marker = peerMarker(layer, viewerId);
                if (marker) layer.removeChild(marker);
            }
            if (peerRoutes) {
                if (!peerHub.__mplCursorWired) {
                    peerHub.__mplCursorWired = true;
                    peerHub.on('ReceiveCursor', function (id, viewerId, name, axesIndex, x, y) {
                        var r = peerRoutes[id];
                        if (r && r.cursor) r.cursor(viewerId, name, axesIndex, x, y);
                    });
                    peerHub.on('ViewerLeft', function (id, viewerId) {
                        var r = peerRoutes[id];
                        if (r && r.left) r.left(viewerId);
                    });
                }
                peerRoutes[chartId].cursor = showPeer;
                peerRoutes[chartId].left = hidePeer;
            }

        """;

    private const string V120Footer = """
        })();
        ]]></script>
        """;
}
