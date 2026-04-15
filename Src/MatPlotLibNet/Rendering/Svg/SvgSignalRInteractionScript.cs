// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>Provides the embedded JavaScript IIFE that forwards browser interaction events
/// (wheel, drag, Home key, legend click) to a bidirectional SignalR <c>ChartHub</c>. Emitted
/// by <see cref="Transforms.SvgTransform"/> when <c>Figure.ServerInteraction == true</c>,
/// replacing the local client-side <c>SvgInteractivityScript</c> + <c>SvgLegendToggleScript</c>.
/// The script assumes a <c>@microsoft/signalr</c> <c>HubConnection</c> has been exposed to the
/// page as <c>window.__mpl_signalr_connection</c> by whichever frontend component hosts the SVG
/// (<c>MplLiveChart</c> for Blazor; the user's own wiring for raw HTML). On discovery failure
/// the script is a graceful no-op — the SVG still renders and no JavaScript errors leak to the
/// browser console.</summary>
internal static class SvgSignalRInteractionScript
{
    /// <summary>Returns the complete <c>&lt;script&gt;</c> block. The IIFE's internal marker
    /// token <c>mplSignalRInteraction</c> is used by tests to assert emission.</summary>
    internal static string GetScript() => """
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
                if (e.button !== 0) return;
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
        })();
        ]]></script>
        """;
}
