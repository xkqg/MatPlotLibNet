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
    /// <summary>The full SVG XML the figure produced. Useful for static assertions
    /// that complement behavioural ones.</summary>
    public string Svg { get; }

    private InteractionScriptHarness(string svg, DomDocument doc, Engine engine)
    {
        Svg = svg;
        Document = doc;
        _engine = engine;
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
        var combined = string.Concat(svgs);

        // Strip XML declarations + namespace decls so XDocument can parse the multi-SVG bag
        // wrapped in a synthetic <html> root.
        var stripped = Regex.Replace(combined, @"<\?xml[^>]*\?>", "").Trim();
        stripped = Regex.Replace(stripped, @"xmlns\s*=\s*""[^""]*""", "");

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
        harness.WireGlobals();
        harness.RunEmbeddedScripts(stripped);
        return harness;
    }

    private void WireGlobals()
    {
        Document.AttachEngine(_engine);
        _engine.SetValue("document", Document);
        _engine.SetValue("window", new { });
        _engine.SetValue("console", new { log = (Action<object>)(o => System.Console.WriteLine(o)) });
        _engine.SetValue("location", new { hash = "" });
        // Phase G.5 of v1.7.2 follow-on plan — CustomEvent is needed by the selection
        // script's `svg.dispatchEvent(new CustomEvent('mpl:selection', { detail: {...} }))`.
        // Jint's `new` requires a concrete function-value, so we register a factory that
        // returns a DomEvent tagged with the detail object.
        _engine.SetValue("CustomEvent", TypeReference.CreateTypeReference<DomEvent>(_engine));
    }

    private void RunEmbeddedScripts(string svgText)
    {
        // Walk the parsed DOM looking for <script> elements in document order. For EACH script,
        // set document.currentScript to its DOM wrapper, so any script that calls
        // `document.currentScript.parentNode` gets its OWN owning <svg> — the multi-chart
        // isolation pattern this whole phase exists to support.
        var scriptElements = Document.QuerySelectorAllRaw("script");
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
