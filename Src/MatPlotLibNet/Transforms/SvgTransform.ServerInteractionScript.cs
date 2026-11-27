// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Rendering;
using System.Text;

namespace MatPlotLibNet.Transforms;

public sealed partial class SvgTransform
{
    /// <summary>The server-interaction script: it forwards the browser's interaction events (wheel, drag, the Home key, a legend
    /// click and, when enabled, a Shift-drag brush select, a hover and a click on a marker) to a bidirectional SignalR <c>ChartHub</c>, in
    /// place of the local client-side scripts, when the figure's <c>ServerInteraction</c> is set. It reads a <c>@microsoft/signalr</c>
    /// <c>HubConnection</c> the page exposes as <c>window.__mpl_signalr_connection</c> (<c>MplLiveChart</c> in Blazor, the page's own
    /// wiring in plain HTML); without one it does nothing and the SVG still renders.
    /// <para>The script <paramref name="figure"/> carries: the zoom, pan, Home and legend handlers, and the branches it asks
    /// for (<see cref="ServerScriptBranches"/>): a band when it selects, a hover when it shows rich tooltips, a data cursor and the
    /// other viewers' pointers when it enables them.</para></summary>
    /// <param name="figure">A figure with server interaction.</param>
    /// <returns>The script, as markup.</returns>
    internal static string ServerInteractionScriptFor(Models.Figure figure) => ServerInteractionScript(
        (figure.EnableSelection ? ServerScriptBranches.BrushSelect : ServerScriptBranches.None)
        | (figure.EnableRichTooltips ? ServerScriptBranches.Hover : ServerScriptBranches.None)
        | (figure.EnableDataCursor ? ServerScriptBranches.DataCursor : ServerScriptBranches.None)
        | (figure.EnableViewerCursors ? ServerScriptBranches.ViewerCursors : ServerScriptBranches.None));

    /// <summary>Returns the complete <c>&lt;script&gt;</c> block with the branches of <paramref name="branches"/>, in the order
    /// <see cref="ComposeServerScript"/> writes them, between the handlers every run carries and the end. Every run also marks the chart busy while a
    /// drag or a band is in progress and idle after it (<c>data-mpl-busy</c>, <c>mpl:idle</c>), and raises <c>mpl:viewchanged</c>
    /// when another viewer moves the view. A script is put together once per combination of branches and kept, so drawing a
    /// figure again writes the string it wrote before.</summary>
    /// <param name="branches">The optional branches to include.</param>
    /// <returns>The script, as markup.</returns>
    internal static string ServerInteractionScript(ServerScriptBranches branches) =>
        ServerScripts[(int)branches] ??= ComposeServerScript(branches);

    /// <summary>The server-interaction script of every combination of branches put together so far, by the value of the combination.</summary>
    private static readonly string?[] ServerScripts = new string?[(int)ServerScriptBranches.All + 1];

    /// <summary>The server-interaction script of <paramref name="branches"/>: the handlers every run carries, then each branch
    /// asked for in the order brush select, hover, data cursor, viewer cursors, then the end.</summary>
    private static string ComposeServerScript(ServerScriptBranches branches)
    {
        var sb = new StringBuilder(V120Header);
        if ((branches & ServerScriptBranches.BrushSelect) != 0) sb.Append(BrushSelectBranch);
        if ((branches & ServerScriptBranches.Hover) != 0) sb.Append(HoverBranch);
        if ((branches & ServerScriptBranches.DataCursor) != 0) sb.Append(DataCursorBranch);
        if ((branches & ServerScriptBranches.ViewerCursors) != 0) sb.Append(ViewerCursorBranch);
        return sb.Append(V120Footer).ToString();
    }

    // ── v1.2.0 handlers — always emitted when ServerInteraction == true ──

    // What the comments of the script below said. They are not emitted: a figure that carries the script carries no
    // comment of it. Each note stands at the line it was written beside.
    // at the locator (SvgScriptPreamble.ServerRoot)
    //   Self-locate through document.currentScript, like every other chart script, so two charts on one page
    //   each wire their own svg; the document's first chart is only the fallback where currentScript is absent.
    // at `function hub() {`
    //   The connection to send through: the one the chart's host element carries (a live chart component gives its
    //   chart a proxy to the connection its client holds, one per chart, so two live charts on a page cannot reach
    //   for the same global), else the page's own window.__mpl_signalr_connection.
    // at `if (sent && typeof sent.catch === 'function') sent.catch(function () { });`
    //   A connection that is down rejects the send; that must not surface as an unhandled rejection.
    //   swallow
    // at `} catch (e) { }`
    //   swallow
    // at `function num(el, name) { return parseFloat(el.getAttribute(name)); }`
    //   -- the view: what the pointer is converted with --
    //   Every axes writes its view (data-axes-index, the plot area data-plot-x/y/w/h in SVG user units and
    //   the range it actually drew, data-xmin/xmax/ymin/ymax -- after autoscale, not the axis' explicit
    //   limits). The pointer, in user units, is converted by DataTransform.PixelToData's rule with the view of
    //   the axes under it: the one whose plot area is nearest (0 inside it). An axes drawn without a Cartesian
    //   range (polar, 3-D), or a figure without axes, falls back to the limits cached on the root over the
    //   whole picture. Wheel, pan and Home move a view; it starts at what was drawn, and Home returns to that.
    //   A log, logit or symlog axis draws in a scaled space: its view range is in that space and the view names the
    //   scale (data-xscale / data-yscale, data-xlinthresh / data-ylinthresh). The pointer is interpolated in the
    //   drawn space and the scale inverted (unscale: ScaleRule.Inverse's rule), so every value sent to
    //   the server is in data units; a pan sends its delta in the drawn space, where PanEvent applies it.
    //   An axis with breaks has them cut out of its drawn space; the view lists each break as it is cut
    //   (data-xbreaks / data-ybreaks: from, to, the place it is cut, its gap), and the pointer is moved up by the gap
    //   of every break cut below it before the scale is inverted (unbreak: AxisBreakMap.Inverse's rule).
    // at `function limits(view, r) {`
    //   limits of a view in data units: what a reset applies to the axis
    // at `function markRooms(cuts, room, a, b) {`
    //   An axis drawn in parts writes the room each break between two parts leaves (data-xroom / data-yroom: AxisBreakMap.Room); a break
    //   is such a one when it is cut strictly inside the view's drawn range, the cuts at its ends being past an end of the range.
    // at `function withoutRooms(v, cuts, room) {`
    //   a place of the drawn space in the space of the parts alone, every room taken out (AxisBreakMap.WithoutRooms' rule)
    // at `function unbreakWithoutRooms(c, cuts, room) {`
    //   a place in the space of the parts read back past every break (AxisBreakMap.InverseWithoutRooms' rule)
    // at `function zoomedAlong(cuts, room, lo, hi, at, scale, s, t) {`
    //   the range a wheel zoom shows along one axis, taken in the space of the parts (DataTransform.ZoomedAbout's rule): a room is a
    //   share of the parts and grows and shrinks with them, so the value under the pointer stays under it; with no room, the drawn space.
    // at `function readSecondarySlot(el, side, sideOrdinal, slot) {`
    //   One outward-stacked secondary Y axis, read from the data-sec-<side>-<slot>-*
    //   attributes SvgWriterExtensions.AppendSecondaryViews writes. side is sent to the server as AxisSide's
    //   ORDINAL (Right = 1, Left = 0), not its name: the default JSON hub protocol carries no
    //   JsonStringEnumConverter, so a name would not bind.
    // at `function secondaryRangesPayload(view) {`
    //   Every secondary slot's new range, each unscaled in ITS OWN units — read, not re-derived, by the
    //   caller: zoomSecondary/panSecondary/resetSecondary each compute the drawn-space value their own way,
    //   this only wraps the result in the shape OnZoom/OnPan/OnReset send.
    // at `function zoomSecondary(view, f, scale) {`
    //   Zooms every secondary slot about the SAME fraction-of-plot point the primary zoomed about, so a log
    //   or symlog secondary axis narrows correctly too (the drawn-space point under the pointer, read
    //   through that slot's own range, mirrors drawnAt's rule for the primary).
    // at `function panSecondary(view, f1, f0) {`
    //   Shifts every secondary slot by the SAME fraction-of-plot delta the primary panned by, scaled to
    //   THAT slot's own span -- the same pixel move is a different data-space delta on an axis with a
    //   different range (PanModifier's rule: the plot's edges moved by the pointer, read in that slot's drawn space).
    // at `function resetSecondary(view) {`
    //   Restores every secondary slot to the range it had when this view was first read.
    // at `SvgScriptPreamble.UserPoint`
    //   client pixel -> SVG user units
    // at `function viewAt(evt) {`
    //   the axes under the pointer: the nearest plot area, the first one that contains the point
    // at `function fractionAt(view, evt) {`
    //   where across the plot the pointer is: 0..1 from the left, 0..1 from the top
    // at `function drawnAt(view, evt) {`
    //   the pointer in the drawn space of a view (the scaled space on a log, logit or symlog axis)
    // at `function dataAt(view, evt) {`
    //   the ONE conversion: pointer -> data, with a view (DataTransform.PixelToData's rule)
    // at `var dragging = false, dragView = null, dragLast = null;`
    //   shared gesture state: the pan drag here, the Shift-drag brush when that branch is emitted
    // at `function busy(on) {`
    //   A page that mounts a redrawn chart replaces this svg, and with it the gesture in progress on it. So the
    //   chart says when it is busy (a drag, a band): the code that mounts waits for data-mpl-busy to go and for
    //   the mpl:idle event, and mounts the latest chart then.
    // at `var peerHub = hub();`
    //   What the connection says about the other viewers arrives on the page's connection: ONE handler per message
    //   on it, routing by chart id to the latest run of that chart (a page runs this script again for every
    //   redrawn chart, and what a run leaves on the connection outlives the svg it was wired to).
    // at `svg.addEventListener('wheel', function (e) {`
    //   wheel: zoom the axes under the pointer about the data point under the pointer
    // at `svg.addEventListener('pointerdown', function (e) {`
    //   drag: the data follows the pointer. The event carries the view it moved (inView), so an axes that
    //   scales itself -- no limits on the server yet -- pans from what is on screen.
    // at `svg.addEventListener('keydown', function (e) {`
    //   Home: every axes back to the view it was first drawn with
    // at `var LEGEND_ATTR = 'data-leg' + 'end-index';`
    //   Legend-toggle dispatcher. A toggle comes ONLY from a legend entry: walk up from the click target
    //   to the element carrying the legend attribute (the series' slot, the value the server toggles). A
    //   plotted series group carries the series index too, but a click there is a click on the data --
    //   the data cursor's -- and must never hide the line that was clicked.
    private static readonly string V120Header = $$"""
        <script type="text/ecmascript"><![CDATA[
        (function mplSignalRInteraction() {
        {{SvgScriptPreamble.ServerRoot}}
        {{SvgScriptPreamble.Capture}}
        {{SvgScriptPreamble.Release}}
        {{SvgScriptPreamble.UserPoint}}
            var chartId = svg.getAttribute('{{SvgAttributes.ChartId}}');

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
                        if (sent && typeof sent.catch === 'function') sent.catch(function () { });
                    } catch (e) { }
                }
            }

            function num(el, name) { return parseFloat(el.getAttribute(name)); }
            function readBreaks(el, name) {
                var text = el.getAttribute(name);
                if (!text) return null;
                var n = text.split(' '), cuts = [];
                for (var i = 0; i + 3 < n.length; i += 4)
                    cuts.push({ from: parseFloat(n[i]), to: parseFloat(n[i + 1]), edge: parseFloat(n[i + 2]), gap: parseFloat(n[i + 3]) });
                return cuts;
            }
            function unbreak(v, cuts) {
                if (!cuts) return v;
                var shift = 0;
                for (var i = 0; i < cuts.length; i++) if (cuts[i].edge < v) shift += cuts[i].gap;
                return v + shift;
            }
            function markRooms(cuts, room, a, b) {
                if (!cuts) return;
                var lo = Math.min(a, b), hi = Math.max(a, b);
                for (var i = 0; i < cuts.length; i++) cuts[i].room = room > 0 && cuts[i].edge > lo && cuts[i].edge < hi;
            }
            function withoutRooms(v, cuts, room) {
                if (!cuts || !(room > 0)) return v;
                var rooms = 0;
                for (var i = 0; i < cuts.length; i++) {
                    if (!cuts[i].room) continue;
                    if (v >= cuts[i].edge + room) rooms += room;
                    else if (v > cuts[i].edge) return cuts[i].edge - rooms;
                }
                return v - rooms;
            }
            function unbreakWithoutRooms(c, cuts, room) {
                if (!cuts) return c;
                var shift = 0, rooms = 0;
                for (var i = 0; i < cuts.length; i++) {
                    var isRoom = room > 0 && cuts[i].room;
                    if (cuts[i].edge - rooms < c) shift += cuts[i].gap + (isRoom ? room : 0);
                    if (isRoom) rooms += room;
                }
                return c + shift;
            }
            function zoomedAlong(cuts, room, lo, hi, at, scale, s, t) {
                var c = withoutRooms(at, cuts, room), c0 = withoutRooms(lo, cuts, room), c1 = withoutRooms(hi, cuts, room);
                return {
                    min: unscale(unbreakWithoutRooms(c - (c - c0) * scale, cuts, room), s, t),
                    max: unscale(unbreakWithoutRooms(c + (c1 - c) * scale, cuts, room), s, t)
                };
            }
            function unscale(v, scale, t) {
                if (scale === 'log') return Math.pow(10, v);
                if (scale === 'logit') return 1 / (1 + Math.pow(10, -v));
                if (scale === 'symlog') {
                    if (!(t > 0)) t = 1;
                    var a = Math.abs(v), stretch = 1 / (1 - 1 / 10);
                    return a <= t * stretch ? v / stretch : (v < 0 ? -1 : 1) * t * Math.pow(10, a / t - stretch);
                }
                return v;
            }
            function dataX(view, v) { return unscale(unbreak(v, view.xBreaks), view.xScale, view.xLinThresh); }
            function dataY(view, v) { return unscale(unbreak(v, view.yBreaks), view.yScale, view.yLinThresh); }
            function limits(view, r) {
                return { xMin: dataX(view, r.xMin), xMax: dataX(view, r.xMax), yMin: dataY(view, r.yMin), yMax: dataY(view, r.yMax) };
            }
            var root = {
                axesIndex: 0, mapped: false,
                xMin: num(svg, '{{SvgAttributes.XMin}}'), xMax: num(svg, '{{SvgAttributes.XMax}}'),
                yMin: num(svg, '{{SvgAttributes.YMin}}'), yMax: num(svg, '{{SvgAttributes.YMax}}'),
                reset: {
                    xMin: num(svg, '{{SvgAttributes.ResetXMin}}'), xMax: num(svg, '{{SvgAttributes.ResetXMax}}'),
                    yMin: num(svg, '{{SvgAttributes.ResetYMin}}'), yMax: num(svg, '{{SvgAttributes.ResetYMax}}')
                }
            };
            function readSecondarySlot(el, side, sideOrdinal, slot) {
                var prefix = '{{SvgAttributes.SecondaryView}}' + side + '-' + slot + '-';
                var s = {
                    sideOrdinal: sideOrdinal, slot: slot,
                    yMin: num(el, prefix + '{{SvgAttributes.YMinStem}}'), yMax: num(el, prefix + '{{SvgAttributes.YMaxStem}}'),
                    yScale: el.getAttribute(prefix + 'y{{SvgAttributes.ScaleStem}}'), yLinThresh: num(el, prefix + 'y{{SvgAttributes.LinThreshStem}}')
                };
                s.resetYMin = s.yMin; s.resetYMax = s.yMax;
                return s;
            }
            function readSecondarySlots(el) {
                var out = [];
                var rightCount = parseInt(el.getAttribute('{{SvgAttributes.SecondaryRightCount}}'), 10) || 0;
                for (var i = 0; i < rightCount; i++) out.push(readSecondarySlot(el, '{{SvgAttributes.RightSide}}', 1, i));
                var leftCount = parseInt(el.getAttribute('{{SvgAttributes.SecondaryLeftCount}}'), 10) || 0;
                for (var j = 0; j < leftCount; j++) out.push(readSecondarySlot(el, '{{SvgAttributes.LeftSide}}', 0, j));
                return out;
            }
            function readView(el) {
                var v = {
                    axesIndex: parseInt(el.getAttribute('{{SvgAttributes.AxesIndex}}'), 10) || 0,
                    x: num(el, '{{SvgAttributes.PlotX}}'), y: num(el, '{{SvgAttributes.PlotY}}'),
                    w: num(el, '{{SvgAttributes.PlotW}}'), h: num(el, '{{SvgAttributes.PlotH}}'),
                    xMin: num(el, '{{SvgAttributes.XMin}}'), xMax: num(el, '{{SvgAttributes.XMax}}'),
                    yMin: num(el, '{{SvgAttributes.YMin}}'), yMax: num(el, '{{SvgAttributes.YMax}}'),
                    xScale: el.getAttribute('{{SvgAttributes.XScale}}'), xLinThresh: num(el, '{{SvgAttributes.XLinThresh}}'),
                    yScale: el.getAttribute('{{SvgAttributes.YScale}}'), yLinThresh: num(el, '{{SvgAttributes.YLinThresh}}'),
                    xBreaks: readBreaks(el, '{{SvgAttributes.XBreaks}}'), yBreaks: readBreaks(el, '{{SvgAttributes.YBreaks}}'),
                    xRoom: num(el, '{{SvgAttributes.XRoom}}') || 0, yRoom: num(el, '{{SvgAttributes.YRoom}}') || 0
                };
                markRooms(v.xBreaks, v.xRoom, v.xMin, v.xMax);
                markRooms(v.yBreaks, v.yRoom, v.yMin, v.yMax);
                v.mapped = v.w > 0 && v.h > 0 && isFinite(v.xMin) && isFinite(v.xMax) && isFinite(v.yMin) && isFinite(v.yMax);
                var start = v.mapped ? v : root;
                v.xMin = start.xMin; v.xMax = start.xMax; v.yMin = start.yMin; v.yMax = start.yMax;
                v.reset = v.mapped ? { xMin: v.xMin, xMax: v.xMax, yMin: v.yMin, yMax: v.yMax } : root.reset;
                v.secondary = v.mapped ? readSecondarySlots(el) : [];
                return v;
            }
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
            function zoomSecondary(view, f, scale) {
                for (var i = 0; i < view.secondary.length; i++) {
                    var s = view.secondary[i];
                    var sy = s.yMin + (1 - f.fy) * (s.yMax - s.yMin);
                    s.yMin = sy - (sy - s.yMin) * scale;
                    s.yMax = sy + (s.yMax - sy) * scale;
                }
                return secondaryRangesPayload(view);
            }
            function panSecondary(view, f1, f0) {
                for (var i = 0; i < view.secondary.length; i++) {
                    var s = view.secondary[i];
                    var dy = (f1.fy - f0.fy) * (s.yMax - s.yMin);
                    s.yMin += dy; s.yMax += dy;
                }
                return secondaryRangesPayload(view);
            }
            function resetSecondary(view) {
                for (var i = 0; i < view.secondary.length; i++) {
                    var s = view.secondary[i];
                    s.yMin = s.resetYMin; s.yMax = s.resetYMax;
                }
                return secondaryRangesPayload(view);
            }
            var views = [];
            var axesEls = svg.querySelectorAll('[{{SvgAttributes.AxesIndex}}]');
            for (var ai = 0; ai < axesEls.length; ai++) views.push(readView(axesEls[ai]));
            if (views.length === 0) views.push(root);

            function viewAt(evt) {
                var p = userPoint(evt.clientX, evt.clientY), best = views[0], bestD = Infinity;
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
            function fractionAt(view, evt) {
                if (view.mapped) {
                    var p = userPoint(evt.clientX, evt.clientY);
                    return { fx: (p.x - view.x) / view.w, fy: (p.y - view.y) / view.h };
                }
                var r = svg.getBoundingClientRect();
                return { fx: (evt.clientX - r.left) / r.width, fy: (evt.clientY - r.top) / r.height };
            }
            function drawnAt(view, evt) {
                var f = fractionAt(view, evt);
                return {
                    x: view.xMin + f.fx * (view.xMax - view.xMin),
                    y: view.yMin + (1 - f.fy) * (view.yMax - view.yMin)
                };
            }
            function dataAt(view, evt) {
                var s = drawnAt(view, evt);
                return { x: dataX(view, s.x), y: dataY(view, s.y) };
            }

            svg.setAttribute('tabindex', '0');
            svg.setAttribute('aria-roledescription', 'interactive chart (server-authoritative)');

            var dragging = false, dragView = null, dragLast = null;
            var brushRect = null, brushStart = null, brushView = null;

            function busy(on) {
                if (on) { svg.setAttribute('{{SvgAttributes.Busy}}', 'true'); return; }
                if (svg.getAttribute('{{SvgAttributes.Busy}}') === null) return;
                svg.removeAttribute('{{SvgAttributes.Busy}}');
                svg.dispatchEvent(new CustomEvent('mpl:idle', { bubbles: true }));
            }

            var peerHub = hub();
            var peerRoutes = null;
            if (peerHub && typeof peerHub.on === 'function') {
                peerRoutes = peerHub.__mplPeerRoutes;
                if (!peerRoutes) {
                    peerRoutes = {};
                    peerHub.__mplPeerRoutes = peerRoutes;
                    peerHub.on('{{HubMethodNames.ViewChanged}}', function (id, viewerId, name) {
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

            svg.addEventListener('wheel', function (e) {
                var v = viewAt(e);
                if (!isFinite(v.xMin) || !isFinite(v.xMax)) return;
                e.preventDefault();
                var scale = Math.pow({{SvgScriptPreamble.ZoomNotchIn}}, e.deltaY < 0 ? 1 : -1);
                var f = fractionAt(v, e);
                var c = drawnAt(v, e);
                var zx = zoomedAlong(v.xBreaks, v.xRoom, v.xMin, v.xMax, c.x, scale, v.xScale, v.xLinThresh);
                var zy = zoomedAlong(v.yBreaks, v.yRoom, v.yMin, v.yMax, c.y, scale, v.yScale, v.yLinThresh);
                v.xMin = c.x - (c.x - v.xMin) * scale; v.xMax = c.x + (v.xMax - c.x) * scale;
                v.yMin = c.y - (c.y - v.yMin) * scale; v.yMax = c.y + (v.yMax - c.y) * scale;
                invoke('{{HubMethodNames.Zoom}}', {
                    chartId: chartId, axesIndex: v.axesIndex,
                    xMin: zx.min, xMax: zx.max, yMin: zy.min, yMax: zy.max,
                    secondaryRanges: zoomSecondary(v, f, scale)
                });
            }, { passive: false });

            svg.addEventListener('pointerdown', function (e) {
                if (e.button !== 0 || e.shiftKey) return;
                dragging = true; dragView = viewAt(e);
                dragLast = { clientX: e.clientX, clientY: e.clientY };
                busy(true);
                capture(svg, e);
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
                invoke('{{HubMethodNames.Pan}}', {
                    chartId: chartId, axesIndex: v.axesIndex,
                    dxData: dxData, dyData: dyData, inView: inView,
                    secondaryRanges: panSecondary(v, f1, f0)
                });
            });
            function endDrag(e) {
                if (!dragging) return;
                dragging = false; dragView = null;
                busy(false);
                release(svg, e);
            }
            svg.addEventListener('pointerup', endDrag);
            svg.addEventListener('pointercancel', endDrag);

            svg.addEventListener('keydown', function (e) {
                if (e.key === 'Home') {
                    e.preventDefault();
                    for (var i = 0; i < views.length; i++) {
                        var v = views[i], r = v.reset, d = limits(v, r);
                        v.xMin = r.xMin; v.xMax = r.xMax; v.yMin = r.yMin; v.yMax = r.yMax;
                        invoke('{{HubMethodNames.Reset}}', {
                            chartId: chartId, axesIndex: v.axesIndex,
                            xMin: d.xMin, xMax: d.xMax, yMin: d.yMin, yMax: d.yMax,
                            secondaryRanges: resetSecondary(v)
                        });
                    }
                }
            });

            var LEGEND_ATTR = '{{SvgAttributes.LegendIndex}}';
            svg.addEventListener('click', function (e) {
                var t = e.target;
                while (t && t !== svg) {
                    var legend = t.getAttribute ? t.getAttribute(LEGEND_ATTR) : null;
                    if (legend !== null) {
                        invoke('{{HubMethodNames.LegendToggle}}', {
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
    // The branch is told by the call it makes to the hub, invoke('OnBrushSelect' (read by SvgTransformTests).
    //
    // Reuses the existing pointerdown / pointermove / pointerup lifecycle the pan handler
    // above already installs, but only when e.shiftKey is true — so panning and brush-select
    // are gesture-disambiguated by the modifier key. Uses the SAME pixel→data conversion (dataAt
    // with the view of the axes under the pointer) as hover, zoom and pan.

    // What the comments of the script below said. They are not emitted: a figure that carries the script carries no
    // comment of it. Each note stands at the line it was written beside.
    // at `svg.addEventListener('pointerdown', function (e) {`
    //   ── mplBrushSelect — Shift+drag rubber-band → OnBrushSelect ──
    //   Both corners convert with the view of the axes the drag started in (dataAt, the one conversion).
    private const string BrushSelectBranch = $$"""
            svg.addEventListener('pointerdown', function (e) {
                if (e.button !== 0 || !e.shiftKey) return;
                e.preventDefault();
                brushStart = { clientX: e.clientX, clientY: e.clientY };
                brushView = viewAt(e);
                var p = userPoint(e.clientX, e.clientY);
                brushRect = document.createElementNS('{{SvgAttributes.Namespace}}', 'rect');
                brushRect.setAttribute('x', p.x); brushRect.setAttribute('y', p.y);
                brushRect.setAttribute('width', 0); brushRect.setAttribute('height', 0);
                brushRect.setAttribute('fill', 'rgba(100,149,237,0.25)');
                brushRect.setAttribute('stroke', '#6495ED');
                brushRect.setAttribute('stroke-width', '1');
                brushRect.setAttribute('pointer-events', 'none');
                brushRect.setAttribute('data-mpl-brush', 'true');
                svg.appendChild(brushRect);
                busy(true);
                capture(svg, e);
            });
            svg.addEventListener('pointermove', function (e) {
                if (!brushRect || !brushStart) return;
                var p0 = userPoint(brushStart.clientX, brushStart.clientY), p1 = userPoint(e.clientX, e.clientY);
                var x = Math.min(p0.x, p1.x), y = Math.min(p0.y, p1.y);
                var w = Math.abs(p1.x - p0.x), h = Math.abs(p1.y - p0.y);
                brushRect.setAttribute('x', x); brushRect.setAttribute('y', y);
                brushRect.setAttribute('width', w); brushRect.setAttribute('height', h);
            });
            svg.addEventListener('pointerup', function (e) {
                if (!brushRect || !brushStart) return;
                var d1 = dataAt(brushView, brushStart), d2 = dataAt(brushView, e);
                invoke('{{HubMethodNames.BrushSelect}}', {
                    chartId: chartId, axesIndex: brushView.axesIndex,
                    x1: Math.min(d1.x, d2.x), y1: Math.min(d1.y, d2.y),
                    x2: Math.max(d1.x, d2.x), y2: Math.max(d1.y, d2.y)
                });
                svg.removeChild(brushRect);
                brushRect = null; brushStart = null; brushView = null;
                busy(false);
                release(svg, e);
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
    // The branch is told by the call it makes to the hub, invoke('OnHover' (read by SvgTransformTests).
    // Hover events fire every mousemove which can saturate
    // the hub; the dispatcher coalesces to at most one in-flight request via a `pending`
    // flag, with a queued point that issues when the response arrives. The tooltip overlay
    // reuses the same role="tooltip" / aria-live="polite" semantics CustomTooltipScript
    // uses for static tooltips.

    // What the comments of the script below said. They are not emitted: a figure that carries the script carries no
    // comment of it. Each note stands at the line it was written beside.
    // at `var hoverPending = false, hoverQueued = null;`
    //   ── mplHoverRoundtrip — cursor → OnHover → server tooltip ──
    //   A page runs this script again each time it mounts a redrawn chart, and what a run leaves behind outlives
    //   the SVG it was wired to. So the branch keeps ONE tooltip element per chart (marked with the chart id and
    //   reused by every run) and ONE ReceiveTooltipContent handler per connection, which hands a tooltip to the
    //   latest run of the chart it names. A run first hides the tooltip of the chart it replaces: the
    //   pointerleave that would have hidden it sat on the removed SVG.
    // at `setTimeout(function () { hoverPending = false; }, 500);`
    //   invoke is fire-and-forget; clear pending when server responds (below)
    //   or after a short timeout to avoid deadlock on dropped packets.
    // at `var _hubForHover = hub();`
    //   The tooltip responses arrive on the page's hub connection: one handler on it, routing by chart id.
    private static readonly string HoverBranch = $$"""
        {{SvgScriptPreamble.Token}}
            var tipOffsetX = {{InteractionToken.TooltipOffsetX.Read}}, tipOffsetY = {{InteractionToken.TooltipOffsetY.Read}};
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
                invoke('{{HubMethodNames.Hover}}', { chartId: chartId, axesIndex: v.axesIndex, x: d.x, y: d.y });
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
                    tt.style.left = (lastHoverClient.clientX + tipOffsetX) + 'px';
                    tt.style.top  = (lastHoverClient.clientY + tipOffsetY) + 'px';
                }
                tt.style.display = 'block';
                if (hoverQueued) {
                    var q = hoverQueued; hoverQueued = null;
                    hoverPending = true;
                    invoke('{{HubMethodNames.Hover}}', { chartId: chartId, axesIndex: q.axesIndex, x: q.x, y: q.y });
                    setTimeout(function () { hoverPending = false; }, 500);
                }
            }
            var _hubForHover = hub();
            if (_hubForHover && typeof _hubForHover.on === 'function') {
                var tooltipRoutes = _hubForHover.__mplTooltipRoutes;
                if (!tooltipRoutes) {
                    tooltipRoutes = {};
                    _hubForHover.__mplTooltipRoutes = tooltipRoutes;
                    _hubForHover.on('{{HubMethodNames.ReceiveTooltipContent}}', function (id, html) {
                        var show = tooltipRoutes[id];
                        if (show) show(html);
                    });
                }
                tooltipRoutes[chartId] = showTooltip;
            }

        """;

    // What the comments of the script below said. They are not emitted: a figure that carries the script carries no
    // comment of it. Each note stands at the line it was written beside.
    // at `svg.addEventListener('click', function (e) {`
    //   -- mplDataCursor -- click a marker -> OnDataCursor --
    //   Every series is drawn inside a group carrying data-series-index and the series' own aria-label,
    //   so the click knows WHICH series it landed on without the page holding any of the data. The group
    //   also carries the mapping its series was drawn with -- data-plot-x/y/w/h (the plot area) and
    //   data-xmin/xmax/ymin/ymax (the range of the series' OWN axes: primary, secondary Y or secondary X) --
    //   and the pointer is converted with it (readView + dataAt: the one conversion the header defines). A
    //   group drawn without a Cartesian range (polar, 3-D) carries none; then the view of the axes under the
    //   pointer is used, which for such axes is the limits cached on the root.
    private const string DataCursorBranch = $$"""
            svg.addEventListener('click', function (e) {
                var target = e.target;
                if (!target || typeof target.closest !== 'function') return;
                var group = target.closest('[{{SvgAttributes.SeriesIndex}}]');
                if (!group) return;
                var axesView = viewAt(e), groupView = readView(group);
                var d = dataAt(groupView.mapped ? groupView : axesView, e);
                var rect = svg.getBoundingClientRect();
                invoke('{{HubMethodNames.DataCursor}}', {
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

    // What the comments of the script below said. They are not emitted: a figure that carries the script carries no
    // comment of it. Each note stands at the line it was written beside.
    // at `var CURSOR_INTERVAL = 50, lastCursorSent = 0, cursorTimer = null, cursorPending `
    //   ── mplViewerCursors — my pointer out, theirs in ──
    // at `function scaleFwd(value, scale, t) {`
    //   the forward of unscale: data -> the scaled space of an axis (log, logit, symlog or linear)
    // at `function placed(value, scale, t, cuts) {`
    //   data -> the drawn space of an axis: through its scale, then down by the gap of every break wholly below the
    //   value; a value inside a break has no place (AxisBreakMap.Remap's rule)
    // at `var px = (placed(x, v.xScale, v.xLinThresh, v.xBreaks) - v.xMin) * (v.w / (v.xMa`
    //   the rule the chart places a point by (DataTransform.DataToPixel's): the value less where the range starts,
    //   times the pixels per unit, plus where the plot starts, so the marker lands on the chart's own pixel
    private const string ViewerCursorBranch = $$"""
            var CURSOR_INTERVAL = 50, lastCursorSent = 0, cursorTimer = null, cursorPending = null;
            function sendCursor(p) {
                lastCursorSent = Date.now(); cursorPending = null;
                invoke('{{HubMethodNames.Cursor}}', { chartId: chartId, axesIndex: p.axesIndex, x: p.x, y: p.y });
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

            function scaleFwd(value, scale, t) {
                if (scale === 'log') return Math.log(value) / Math.LN10;
                if (scale === 'logit') return value > 0 && value < 1 ? Math.log(value / (1 - value)) / Math.LN10 : NaN;
                if (scale === 'symlog') {
                    if (!(t > 0)) t = 1;
                    var a = Math.abs(value), stretch = 1 / (1 - 1 / 10);
                    return a <= t ? value * stretch : (value < 0 ? -1 : 1) * t * (stretch + Math.log(a / t) / Math.LN10);
                }
                return value;
            }
            function placed(value, scale, t, cuts) {
                var shift = 0;
                if (cuts) for (var i = 0; i < cuts.length; i++) {
                    if (value > cuts[i].from && value < cuts[i].to) return NaN;
                    if (cuts[i].to <= value) shift += cuts[i].gap;
                }
                return scaleFwd(value, scale, t) - shift;
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
                layer = document.createElementNS('{{SvgAttributes.Namespace}}', 'g');
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
                var px = (placed(x, v.xScale, v.xLinThresh, v.xBreaks) - v.xMin) * (v.w / (v.xMax - v.xMin)) + v.x;
                var py = (placed(y, v.yScale, v.yLinThresh, v.yBreaks) - v.yMin) * -(v.h / (v.yMax - v.yMin)) + (v.y + v.h);
                if (!isFinite(px) || !isFinite(py)) return;
                var layer = peerLayer(), marker = peerMarker(layer, viewerId);
                if (!marker) {
                    marker = document.createElementNS('{{SvgAttributes.Namespace}}', 'g');
                    marker.setAttribute('data-mpl-peer', viewerId);
                    var dot = document.createElementNS('{{SvgAttributes.Namespace}}', 'circle');
                    dot.setAttribute('r', 4); dot.setAttribute('fill', '#E4572E'); dot.setAttribute('stroke', '#ffffff');
                    marker.appendChild(dot);
                    var label = document.createElementNS('{{SvgAttributes.Namespace}}', 'text');
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
                    peerHub.on('{{HubMethodNames.ReceiveCursor}}', function (id, viewerId, name, axesIndex, x, y) {
                        var r = peerRoutes[id];
                        if (r && r.cursor) r.cursor(viewerId, name, axesIndex, x, y);
                    });
                    peerHub.on('{{HubMethodNames.ViewerLeft}}', function (id, viewerId) {
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

    /// <summary>The optional parts of the server-interaction script a figure carries, as one value: the zoom, pan, Home and legend
    /// handlers are always there; each of these is added when the figure asks for it.</summary>
    [Flags]
    internal enum ServerScriptBranches
    {
        /// <summary>The handlers every server-interactive figure carries, and nothing more.</summary>
        None = 0,

        /// <summary>A Shift-drag band sent as a brush selection.</summary>
        BrushSelect = 1,

        /// <summary>The pointer sent as a hover, and the tooltip the server works out shown.</summary>
        Hover = 2,

        /// <summary>A click on a series sent as a data cursor.</summary>
        DataCursor = 4,

        /// <summary>This viewer's pointer sent to the others, and theirs drawn here.</summary>
        ViewerCursors = 8,

        /// <summary>Every branch.</summary>
        All = BrushSelect | Hover | DataCursor | ViewerCursors,
    }
}
