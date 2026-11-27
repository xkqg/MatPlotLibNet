// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Builders;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Rendering.Svg.Interaction;

/// <summary>The gestures the interaction tests make on a page and the payloads they read back, each written once. The harness' screen
/// transform is the identity, so a point read from the drawing is also the client position a gesture is aimed at.</summary>
public static class InteractionGestures
{
    extension(InteractionScriptHarness h)
    {
        /// <summary>The one chart of the page: its <c>svg</c> element, where the script's listeners sit.</summary>
        public DomElement Chart() => h.Document.QuerySelectorAllRaw("svg").Single();

        /// <summary>Moves the pointer onto <paramref name="at"/> and returns what the page sends for the hover.</summary>
        /// <param name="at">Where the pointer goes, in client pixels.</param>
        public IDictionary<string, object?> HoverAt(Point at)
        {
            var mock = h.WireSignalRMock();
            var chart = h.Chart();
            chart.Fire(new DomEvent("pointermove") { target = chart, clientX = at.X, clientY = at.Y });
            return mock.Payload("OnHover");
        }

        /// <summary>Turns the wheel one notch in over <paramref name="at"/>.</summary>
        /// <param name="at">Where the pointer stands, in client pixels.</param>
        /// <param name="deltaY">The wheel's delta; negative zooms in.</param>
        public void Wheel(Point at, double deltaY = -100)
        {
            var chart = h.Chart();
            chart.Fire(new DomEvent("wheel") { target = chart, clientX = at.X, clientY = at.Y, deltaY = deltaY });
        }

        /// <summary>Presses the pointer down on <paramref name="from"/>, drags it to <paramref name="to"/> and lets go.</summary>
        /// <param name="from">Where the pointer goes down.</param>
        /// <param name="to">Where it is dragged to and released.</param>
        public void Drag(Point from, Point to)
        {
            var chart = h.Chart();
            chart.Fire(new DomEvent("pointerdown") { target = chart, clientX = from.X, clientY = from.Y });
            chart.Fire(new DomEvent("pointermove") { target = chart, clientX = to.X, clientY = to.Y });
            chart.Fire(new DomEvent("pointerup") { target = chart, clientX = to.X, clientY = to.Y });
        }

        /// <summary>Draws a brush with the shift key down from <paramref name="from"/> to <paramref name="to"/>.</summary>
        /// <param name="from">Where the brush starts.</param>
        /// <param name="to">Where it ends.</param>
        public void BrushBetween(Point from, Point to)
        {
            var chart = h.Chart();
            chart.Fire(new DomEvent("pointerdown") { target = chart, clientX = from.X, clientY = from.Y, shiftKey = true });
            chart.Fire(new DomEvent("pointerup") { target = chart, clientX = to.X, clientY = to.Y, shiftKey = true });
        }

        /// <summary>Presses the Home key on the chart.</summary>
        public void PressHome()
        {
            var chart = h.Chart();
            chart.Fire(new DomEvent("keydown") { target = chart, key = "Home" });
        }

        /// <summary>Clicks the point <paramref name="pointIndex"/> of the series labelled <paramref name="label"/> and returns the
        /// annotation the data cursor sends. The click is fired at the chart, where the listener sits, with the line as its target, as a
        /// browser's bubbling delivers it.</summary>
        /// <param name="label">The label of the series.</param>
        /// <param name="pointIndex">The point of the series, counted along its line.</param>
        public IDictionary<string, object?> ClickDataCursor(string label, int pointIndex)
        {
            var mock = h.WireSignalRMock();
            var group = h.Document.QuerySelectorAllRaw("[data-series-index]")
                .Single(g => g.getAttribute("aria-label") == label && !g.hasAttribute("data-legend-index"));
            var line = h.Document.Wrap(group.Xml.Each("polyline").First());
            var at = h.Svg.PointsOf(label)[pointIndex];
            h.Chart().Fire(new DomEvent("click") { target = line, clientX = at.X, clientY = at.Y });
            return mock.Payload("OnDataCursor").Nested("annotation");
        }
    }

    extension(SignalRInvokeMock mock)
    {
        /// <summary>The payload of the one call to <paramref name="method"/> the page made.</summary>
        /// <param name="method">The hub method.</param>
        public IDictionary<string, object?> Payload(string method) =>
            Assert.IsAssignableFrom<IDictionary<string, object?>>(Assert.Single(mock.Calls, c => c.Method == method).Payload);
    }

    extension(IDictionary<string, object?> payload)
    {
        /// <summary>The number a payload field holds.</summary>
        /// <param name="key">The field.</param>
        public double Num(string key) => Convert.ToDouble(payload[key], CultureInfo.InvariantCulture);

        /// <summary>The payload nested in a field.</summary>
        /// <param name="key">The field.</param>
        public IDictionary<string, object?> Nested(string key) => Assert.IsAssignableFrom<IDictionary<string, object?>>(payload[key]);

        /// <summary>The zoom event a server applies for the payload of an <c>OnZoom</c>.</summary>
        public ZoomEvent ToZoomEvent() => new("c", payload.AxesIndex(), payload.Num("xMin"), payload.Num("xMax"), payload.Num("yMin"), payload.Num("yMax"));

        /// <summary>The reset event a server applies for the payload of an <c>OnReset</c>.</summary>
        public ResetEvent ToResetEvent() => new("c", payload.AxesIndex(), payload.Num("xMin"), payload.Num("xMax"), payload.Num("yMin"), payload.Num("yMax"));

        /// <summary>The pan event a server applies for the payload of an <c>OnPan</c>, with the range that was on screen.</summary>
        public PanEvent ToPanEvent()
        {
            var inView = payload.Nested("inView");
            return new PanEvent("c", payload.AxesIndex(), payload.Num("dxData"), payload.Num("dyData"))
            {
                InView = new DataRange(inView.Num("xMin"), inView.Num("xMax"), inView.Num("yMin"), inView.Num("yMax")),
            };
        }

        private int AxesIndex() => payload.TryGetValue("axesIndex", out var index) ? Convert.ToInt32(index, CultureInfo.InvariantCulture) : 0;
    }

    extension(Action<FigureBuilder> configure)
    {
        /// <summary>The figure the builder callback draws.</summary>
        public Figure Build()
        {
            var builder = Plt.Create();
            configure(builder);
            return builder.Build();
        }
    }
}
