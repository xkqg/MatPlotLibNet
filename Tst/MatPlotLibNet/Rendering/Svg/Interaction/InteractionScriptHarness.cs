// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using System.Xml.Linq;
using Jint;
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

    public void Dispose() => _engine.Dispose();
}
