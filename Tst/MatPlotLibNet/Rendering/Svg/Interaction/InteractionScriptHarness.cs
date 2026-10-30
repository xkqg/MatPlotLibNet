// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using System.Xml.Linq;
using Jint;
using Jint.Runtime.Interop;
using MatPlotLibNet.Builders;

namespace MatPlotLibNet.Tests.Rendering.Svg.Interaction;

/// <summary>Hosts the embedded browser-interaction JS scripts inside a Jint engine, backed
/// by a real <see cref="XDocument"/>. Tests use this to simulate <c>click</c>/<c>wheel</c>/
/// <c>keydown</c>/<c>pointerdown</c> events and assert the resulting SVG mutations
/// (attribute changes, style changes, dispatched events).
///
/// <para>This is the Phase 1 foundation of the v1.7.2 interaction-hardening plan — every
/// later phase reuses this harness to verify behaviour rather than just script-emission.</para>
///
/// <para>Usage:</para>
/// <code>
/// using var h = InteractionScriptHarness.FromBuilder(b => b.WithLegendToggle()
///     .Plot([1, 2], [3, 4], s => s.Label = "A"));
/// h.Simulate("[data-legend-index='0']", "click");
/// var hidden = h.GetAttribute("[data-series-index='0']", "style");
/// Assert.Contains("display: none", hidden);
/// </code>
/// </summary>
/// <remarks>
/// <para><b>What the harness DOES simulate</b>: element-scoped event dispatch via
/// <c>DomElement.Fire</c> / <c>dispatchEvent</c>, synchronous listener invocation, attribute
/// and style mutations visible to subsequent assertions, <c>querySelector(All)</c>,
/// <c>parentNode</c> walks, <c>addEventListener</c> / <c>removeEventListener</c>,
/// <c>document.currentScript</c>, SignalR mock wiring, and per-chart SVG isolation across
/// multiple figures in one document.</para>
///
/// <para><b>What the harness DOES NOT simulate</b> (tests depending on these behaviours
/// will silently pass against real bugs — Phase R, 2026-04-19, surfaced one such gap):</para>
/// <list type="bullet">
///   <item><description><b>Event bubbling.</b> Events fire only on the literal target — there
///     is no parent-chain propagation. Tests needing delegation must fire directly at the
///     element that owns the listener. <c>stopPropagation()</c> DOES halt the in-progress
///     dispatch on the target (see capture-phase entry below) but never started bubbling
///     to ancestors anyway.</description></item>
///   <item><description><b>Capture phase.</b> Phase T (2026-04-19) — capture vs bubble is now
///     honoured. The third argument to <c>addEventListener</c> (<c>true</c> or
///     <c>{ capture: true }</c>) registers in capture phase; capture-phase listeners fire
///     FIRST in registration order, then bubble-phase listeners. <c>stopPropagation()</c>
///     in either phase halts the rest of the in-progress Fire dispatch (capture stops
///     bubble; either stops same-phase later listeners). Cross-element capture/bubble
///     traversal is still NOT faithful — Fire only walks listeners on the literal target.</description></item>
///   <item><description><b>Pointer capture target redirection.</b>
///     <see cref="DomElement.setPointerCapture"/> / <see cref="DomElement.releasePointerCapture"/>
///     are no-ops. In browsers, <c>setPointerCapture</c> causes subsequent
///     <c>pointermove</c>/<c>pointerup</c>/<c>click</c> to target the capturing element rather
///     than the element under the cursor; the harness does not model this.</description></item>
///   <item><description><b>elementFromPoint.</b> Not stubbed. Scripts that fall back to
///     <c>document.elementFromPoint(x, y)</c> to recover a target will fail/throw — use
///     alternative resolution strategies in script code (e.g., walk up from <c>e.target</c>).</description></item>
///   <item><description><b>Layout.</b> <see cref="DomElement.getBoundingClientRect"/> returns
///     synthetic fixed bounds; clientWidth/clientHeight read the SVG width/height attributes
///     verbatim with a 1-fallback to avoid NaN. Position math against real rendered geometry
///     is not available.</description></item>
///   <item><description><b>Pointer Event sequences.</b> The harness fires whatever event
///     <c>type</c> the test asks for, in isolation. It does NOT synthesize the full
///     <c>pointerdown → pointermove* → pointerup → click</c> sequence a real pointer produces.
///     Scripts that interpret such sequences must be tested explicitly — firing each event
///     in turn with realistic <c>clientX</c>/<c>clientY</c> values (see
///     <c>TreemapDrilldownTests.HoverWithoutButtonDown_DoesNotPoisonClickHandler</c> for the
///     pattern that caught the Phase P hover-poisoning bug).</description></item>
/// </list>
/// </remarks>
public sealed class InteractionScriptHarness : IDisposable
{
    private readonly Engine _engine;
    public DomDocument Document { get; }

    /// <summary>The page's <c>window</c>, which scripts listen on and dispatch to when charts talk to each other
    /// (<c>mpl:rows</c>). Replaced by the test's own object when a test wires a SignalR connection.</summary>
    public DomWindow Window { get; }
    /// <summary>The full SVG XML the figure produced. Useful for static assertions
    /// that complement behavioural ones.</summary>
    public string Svg { get; }

    private InteractionScriptHarness(string svg, DomDocument doc, Engine engine)
    {
        Svg = svg;
        Document = doc;
        _engine = engine;
        Window = new DomWindow(engine);
    }

    /// <summary>Builds the figure, extracts its embedded scripts, runs them in Jint
    /// against a stub DOM backed by the SVG XML.</summary>
    public static InteractionScriptHarness FromBuilder(Action<FigureBuilder> configure)
        => FromBuilders(configure);

    /// <summary>Multi-chart variant — wraps N figures in one synthetic document so
    /// per-chart isolation can be asserted (Phase 2). The harness then tags each SVG
    /// with <c>id="mpl-chart-0"</c>, <c>"mpl-chart-1"</c>, ... so tests can scope queries.</summary>
    public static InteractionScriptHarness FromBuilders(params Action<FigureBuilder>[] configures)
    {
        var svgs = new List<string>();
        foreach (var configure in configures)
        {
            var fb = Plt.Create();
            configure(fb);
            svgs.Add(fb.ToSvg());
        }

        return FromSvgs(svgs, reducedMotion: false);
    }

    /// <summary>One document that is not a figure's own: an SVG a transform wrote (an animation that steps through its frames), run
    /// as a page runs it. Time is the test's: an interval armed by a script fires when <see cref="Tick"/> says so.</summary>
    /// <param name="svg">The SVG document.</param>
    /// <param name="reducedMotion">Whether the page's reader asked for reduced motion (<c>prefers-reduced-motion</c>).</param>
    public static InteractionScriptHarness FromSvg(string svg, bool reducedMotion = false) => FromSvgs([svg], reducedMotion);

    private static InteractionScriptHarness FromSvgs(IReadOnlyList<string> svgs, bool reducedMotion)
    {
        var combined = string.Concat(svgs);

        // Strip XML declarations + namespace decls so XDocument can parse the multi-SVG bag
        // wrapped in a synthetic <html> root.
        var stripped = Strip(combined);

        var wrapped = $"<html>{stripped}</html>";
        var xdoc = XDocument.Parse(wrapped);

        // Tag each <svg> with a unique id AFTER parsing — doing this via text regex misfires
        // when script bodies contain literal `<svg` text in comments (the IIFE for legend
        // toggle's per-chart isolation comment, for instance).
        int idx = 0;
        foreach (var svgEl in xdoc.Descendants("svg"))
            if (svgEl.Attribute("id") is null)
                svgEl.SetAttributeValue("id", $"mpl-chart-{idx++}");

        var dom = new DomDocument(xdoc);

        var engine = new Engine(opts => opts.Strict(false));
        var harness = new InteractionScriptHarness(combined, dom, engine);
        harness.Window.PrefersReducedMotion = reducedMotion;
        harness.WireGlobals();
        harness.RunEmbeddedScripts(dom.QuerySelectorAllRaw("script"));
        return harness;
    }

    /// <summary>Strips the XML declaration and the default namespace, so a figure's SVG parses into the synthetic
    /// document the harness holds.</summary>
    private static string Strip(string svg)
    {
        var stripped = Regex.Replace(svg, @"<\?xml[^>]*\?>", "").Trim();
        return Regex.Replace(stripped, @"xmlns\s*=\s*""[^""]*""", "");
    }

    /// <summary>Mounts a redrawn chart the way a page does after an update: the <c>&lt;svg&gt;</c> carrying the same
    /// <c>data-chart-id</c> is replaced by the new figure's SVG (the rest of the document, the body with anything a script
    /// appended to it included, stays), and the new SVG's scripts run in the same engine, as a page that re-runs the
    /// scripts of the markup it mounts does. Listeners on the replaced SVG stay registered but can never fire again,
    /// as in a browser.</summary>
    /// <param name="configure">The redrawn figure; it must carry the chart id of the SVG it replaces.</param>
    public void Remount(Action<FigureBuilder> configure)
    {
        var fb = Plt.Create();
        configure(fb);
        var replacement = XElement.Parse(Strip(fb.ToSvg()));
        string? chartId = replacement.Attribute("data-chart-id")?.Value;
        var old = Document.Xml.Descendants("svg").First(e => e.Attribute("data-chart-id")?.Value == chartId);
        replacement.SetAttributeValue("id", old.Attribute("id")?.Value);
        old.ReplaceWith(replacement);
        RunEmbeddedScripts(Document.Wrap(replacement).querySelectorAllRaw("script"));
    }

    /// <summary>Installs a SignalR connection written in JavaScript at <c>window.__mpl_signalr_connection</c>, the
    /// shape a page hands the script (a <c>HubConnection</c> is a plain JS object): it records the methods invoked and
    /// the handlers registered with <c>on</c>, and lets a test deliver a server message to those handlers. Unlike
    /// <see cref="WireSignalRMock"/> the connection must be in place before the scripts that read it at start-up run,
    /// so a test wires it and then <see cref="Remount"/>s.</summary>
    public JsSignalRHub WireJsSignalRHub()
    {
        _engine.Execute("""
            window = { __mpl_signalr_connection: {
                invoked: [], payloads: [], handlers: [],
                invoke: function (method, payload) { this.invoked.push(method); this.payloads.push(JSON.stringify(payload)); },
                on: function (name, fn) { this.handlers.push({ name: name, fn: fn }); }
            } };
            """);
        return new JsSignalRHub(_engine);
    }

    private void WireGlobals()
    {
        Document.AttachEngine(_engine);
        _engine.SetValue("document", Document);
        _engine.SetValue("window", Window);
        _engine.SetValue("console", new { log = (Action<object>)(o => System.Console.WriteLine(o)) });
        _engine.SetValue("location", new { hash = "" });
        // The hover round trip arms a timer to clear its in-flight flag. The harness has no event loop, so the timer is
        // accepted and never fires: a test sees the state right after the event, the way a browser does before the timer.
        _engine.SetValue("setTimeout", new Func<Jint.Native.JsValue, Jint.Native.JsValue, int>((_, _) => 0));
        // An interval is armed and cleared for real, and fires only when the test says time has passed (Tick).
        _engine.SetValue("setInterval", new Func<Jint.Native.JsValue, Jint.Native.JsValue, int>((handler, _) =>
        {
            _intervals[++_lastIntervalId] = handler;
            return _lastIntervalId;
        }));
        _engine.SetValue("clearInterval", new Action<int>(id => _intervals.Remove(id)));
        // Phase G.5 of v1.7.2 follow-on plan — CustomEvent is needed by the selection
        // script's `svg.dispatchEvent(new CustomEvent('mpl:selection', { detail: {...} }))`.
        // Jint's `new` requires a concrete function-value, so we register a factory that
        // returns a DomEvent tagged with the detail object.
        _engine.SetValue("CustomEvent", TypeReference.CreateTypeReference<DomEvent>(_engine));
    }

    private void RunEmbeddedScripts(IEnumerable<DomElement> scriptElements)
    {
        // Run the <script> elements in document order. For EACH script, set document.currentScript to its DOM
        // wrapper, so any script that calls `document.currentScript.parentNode` gets its OWN owning <svg> — the
        // multi-chart isolation pattern this whole phase exists to support.
        foreach (var scriptEl in scriptElements)
        {
            // Extract the script body — Jint runs the JS, not the wrapper tags.
            var body = scriptEl.Xml.Value;
            // Strip CDATA wrapper if present.
            body = body.Trim();
            if (body.StartsWith("<![CDATA[")) body = body[9..];
            if (body.EndsWith("]]>")) body = body[..^3];

            // Set currentScript so the script can self-locate.
            Document.SetCurrentScript(scriptEl);
            try { _engine.Execute(body); }
            catch (Jint.Runtime.JavaScriptException ex)
            {
                throw new InvalidOperationException(
                    $"Embedded script threw: {ex.Message}\n--- script body ---\n{body}", ex);
            }
            finally
            {
                Document.SetCurrentScript(null);
            }
        }
    }

    /// <summary>Find every element matching <paramref name="selector"/>, dispatch a
    /// <see cref="DomEvent"/> of <paramref name="type"/> to each, and let the registered
    /// listeners mutate the DOM. Returns the count of elements the event was fired on
    /// (0 if the selector matched nothing).</summary>
    public int Simulate(string selector, string type, Action<DomEvent>? configureEvent = null)
    {
        var els = Document.QuerySelectorAllRaw(selector);
        foreach (var el in els)
        {
            var ev = new DomEvent(type) { target = el };
            configureEvent?.Invoke(ev);
            el.Fire(ev);
        }
        return els.Length;
    }

    /// <summary>Convenience: read a single element's attribute. Returns <c>null</c> if the
    /// element is missing OR the attribute is missing.</summary>
    public string? GetAttribute(string selector, string attribute) =>
        Document.querySelector(selector)?.getAttribute(attribute);

    /// <summary>Convenience: read a single element's style property (e.g. "display").</summary>
    public string? GetStyle(string selector, string property)
    {
        var el = Document.querySelector(selector);
        if (el is null) return null;
        var raw = el.getAttribute("style") ?? "";
        foreach (var pair in raw.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = pair.Split(':', 2);
            if (kv.Length == 2 && kv[0].Trim().Equals(property, StringComparison.OrdinalIgnoreCase))
                return kv[1].Trim();
        }
        return null;
    }

    /// <summary>Phase G.8 of the v1.7.2 follow-on plan — installs a mock SignalR
    /// connection at <c>window.__mpl_signalr_connection</c>. The returned
    /// <see cref="SignalRInvokeMock"/> records every
    /// <c>invoke(method, payload)</c> call the embedded scripts make, so tests
    /// can verify which hub methods got dispatched with which payload shapes.
    /// The <see cref="SvgSignalRInteractionScript"/> reads <c>window</c> lazily
    /// inside its <c>invoke()</c> helper, so wiring the mock AFTER harness
    /// construction still takes effect on every subsequent event.</summary>
    public SignalRInvokeMock WireSignalRMock()
    {
        var mock = new SignalRInvokeMock();
        _engine.SetValue("window", new SignalRWindowStub(mock));
        return mock;
    }

    private readonly Dictionary<int, Jint.Native.JsValue> _intervals = [];
    private int _lastIntervalId;

    /// <summary>How many intervals the scripts have armed and not cleared.</summary>
    public int ActiveIntervals => _intervals.Count;

    /// <summary>One interval of time passes: every interval a script has armed fires once.</summary>
    public void Tick()
    {
        foreach (var handler in _intervals.Values.ToArray())
            _engine.Invoke(handler);
    }

    public void Dispose() => _engine.Dispose();
}

/// <summary>Test-owned wrapper for <c>window</c> that holds a SignalR connection
/// mock so embedded scripts can <c>window.__mpl_signalr_connection.invoke(...)</c>.
/// Additional fields can be added as scripts evolve; this stub never throws on
/// missing members (Jint silently returns undefined).</summary>
public sealed class SignalRWindowStub
{
    public SignalRInvokeMock __mpl_signalr_connection { get; }
    public SignalRWindowStub(SignalRInvokeMock mock) { __mpl_signalr_connection = mock; }
}

/// <summary>Records every <c>invoke(method, payload)</c> call made by the
/// <see cref="MatPlotLibNet.Rendering.Svg.SvgSignalRInteractionScript"/>. Tests
/// inspect <see cref="Calls"/> to assert which hub methods the client would
/// have dispatched under a given event sequence.</summary>
public sealed class SignalRInvokeMock
{
    public List<(string Method, object? Payload)> Calls { get; } = new();
    public void invoke(string method, object? payload) => Calls.Add((method, payload));
}

/// <summary>Test-side view of the JavaScript connection <see cref="InteractionScriptHarness.WireJsSignalRHub"/> installs.</summary>
public sealed class JsSignalRHub
{
    private readonly Engine _engine;
    internal JsSignalRHub(Engine engine) => _engine = engine;

    /// <summary>How many handlers the scripts registered for the server message <paramref name="name"/>.</summary>
    public int HandlerCount(string name) => (int)_engine.Evaluate(
        $"window.__mpl_signalr_connection.handlers.filter(function (h) {{ return h.name === '{name}'; }}).length").AsNumber();

    /// <summary>Delivers a server message to every handler registered for it, as the connection does.</summary>
    public void Deliver(string name, string chartId, string html) => _engine.Invoke(_engine.Evaluate("""
        (function (name, id, html) {
            var hs = window.__mpl_signalr_connection.handlers;
            for (var i = 0; i < hs.length; i++) if (hs[i].name === name) hs[i].fn(id, html);
        })
        """), name, chartId, html);

    /// <summary>Delivers a server message with any arguments to every handler registered for it, as the connection does.</summary>
    /// <param name="name">The client method the server called.</param>
    /// <param name="args">Its arguments, in order.</param>
    public void DeliverMessage(string name, params object[] args)
    {
        var deliver = _engine.Evaluate("""
            (function (name, args) {
                var hs = window.__mpl_signalr_connection.handlers;
                for (var i = 0; i < hs.length; i++) if (hs[i].name === name) hs[i].fn.apply(null, args);
            })
            """);
        var list = (Jint.Native.JsArray)_engine.Evaluate("[]");
        foreach (var arg in args)
        {
            list.Push(Jint.Native.JsValue.FromObject(_engine, arg));
        }

        _engine.Invoke(deliver, name, list);
    }

    /// <summary>The JSON payload of the last time the scripts invoked <paramref name="method"/>, or <see langword="null"/>.</summary>
    public string? LastPayload(string method) => _engine.Evaluate($$"""
        (function () {
            var c = window.__mpl_signalr_connection;
            for (var i = c.invoked.length - 1; i >= 0; i--) if (c.invoked[i] === '{{method}}') return c.payloads[i];
            return null;
        })()
        """) is var v && v.IsString() ? v.AsString() : null;

    /// <summary>The hub methods the scripts invoked, in order.</summary>
    public string[] Invoked => _engine.Evaluate("window.__mpl_signalr_connection.invoked.join(',')").AsString()
        .Split(',', StringSplitOptions.RemoveEmptyEntries);
}

/// <summary>The page's <c>window</c> as a script sees it: an event target the charts of a page use to talk to each other. A
/// listener is kept by the function the script passed, so <c>removeEventListener</c> removes the one that was added, and the
/// number left can be asserted.</summary>
public sealed class DomWindow
{
    private readonly Engine _engine;
    private readonly List<Listener> _listeners = [];

    private readonly record struct Listener(string Type, Jint.Native.JsValue Handler);

    internal DomWindow(Engine engine) => _engine = engine;

    /// <summary>Whether the page's reader asked for reduced motion; what <c>matchMedia('(prefers-reduced-motion: reduce)')</c> answers.</summary>
    public bool PrefersReducedMotion { get; set; }

    /// <summary>Mirrors <c>window.matchMedia</c> for the one query the scripts ask.</summary>
    public DomMediaQuery matchMedia(string query) =>
        new() { matches = query.Contains("prefers-reduced-motion", StringComparison.Ordinal) && PrefersReducedMotion };

    public void addEventListener(string type, Jint.Native.JsValue handler, object? options = null) =>
        _listeners.Add(new Listener(type, handler));

    public void removeEventListener(string type, Jint.Native.JsValue handler, object? options = null) =>
        _listeners.RemoveAll(listener => listener.Type == type && ReferenceEquals(listener.Handler, handler));

    /// <summary>Delivers the event to every listener of its type, in the order they were added; a listener added or removed while
    /// it is delivered does not change who hears this one.</summary>
    public void dispatchEvent(DomEvent ev)
    {
        foreach (var listener in _listeners.Where(l => l.Type == ev.type).ToArray())
            _engine.Invoke(listener.Handler, ev);
    }

    /// <summary>How many listeners of this type are registered now.</summary>
    public int ListenerCount(string type) => _listeners.Count(listener => listener.Type == type);
}

/// <summary>The answer <c>matchMedia</c> gives.</summary>
public sealed class DomMediaQuery
{
    public bool matches { get; set; }
}
