// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;

namespace MatPlotLibNet.Tests.Rendering.Svg.Interaction;

/// <summary>JS-facing wrapper around an <see cref="XDocument"/>. Exposes the global
/// <c>document</c> surface that the embedded interaction scripts use:
/// <c>querySelector</c>, <c>querySelectorAll</c>, <c>createElement(NS)</c>, <c>body</c>.</summary>
public sealed class DomDocument
{
    internal readonly XDocument Xml;
    private readonly Dictionary<XElement, DomElement> _wrappers = new();
    private DomElement? _body;
    internal Jint.Engine? Engine { get; private set; }

    public DomDocument(XDocument xml) { Xml = xml; }

    /// <summary>Harness wires the Engine in after construction so DomElement[] returns
    /// from querySelectorAll get wrapped in a JsArray (which has the .forEach prototype
    /// that the embedded scripts call).</summary>
    internal void AttachEngine(Jint.Engine engine) => Engine = engine;

    /// <summary>Mirrors browser <c>document.currentScript</c> — the script element
    /// that is currently executing. The harness sets this before each
    /// <c>engine.Execute(scriptBody)</c> call so the script can self-locate its
    /// owning SVG via <c>document.currentScript.parentNode</c>. Multi-chart isolation
    /// (Phase 2) depends on this.</summary>
    public DomElement? currentScript { get; private set; }
    internal void SetCurrentScript(DomElement? el) => currentScript = el;

    /// <summary>Returns a stable wrapper per <see cref="XElement"/> so identity comparison
    /// + listener registration tracks correctly across calls.</summary>
    internal DomElement Wrap(XElement el)
    {
        if (!_wrappers.TryGetValue(el, out var w))
        {
            w = new DomElement(el, this);
            _wrappers[el] = w;
        }
        return w;
    }

    /// <summary><c>document.body</c> — synthetic for SVG-only documents. Tests that need
    /// it (e.g. tooltip injection) get a real element appended at root.</summary>
    public DomElement body
    {
        get
        {
            if (_body is null)
            {
                var bodyEl = Xml.Root!.Element("body");
                if (bodyEl is null)
                {
                    bodyEl = new XElement("body");
                    Xml.Root.Add(bodyEl);
                }
                _body = Wrap(bodyEl);
            }
            return _body;
        }
    }

    public DomElement? querySelector(string selector) =>
        DomSelector.Find(Xml.Root!, selector).FirstOrDefault() is { } e ? Wrap(e) : null;

    public object querySelectorAll(string selector) =>
        JsArrayWrap.Wrap(Engine, QuerySelectorAllRaw(selector));

    /// <summary>C#-only — returns a strongly-typed array. Use this from the test harness;
    /// the public <see cref="querySelectorAll(string)"/> returns a JsArray for JS callers.</summary>
    internal DomElement[] QuerySelectorAllRaw(string selector) =>
        DomSelector.Find(Xml.Root!, selector).Select(Wrap).ToArray();

    public DomElement createElement(string tag) =>
        Wrap(new XElement(tag));

    public DomElement createElementNS(string ns, string tag) =>
        Wrap(new XElement(XName.Get(tag, ns)));

    /// <summary>Mirrors DOM <c>document.addEventListener</c>. Some scripts attach
    /// document-level keyboard listeners (e.g. the Treemap drilldown's global
    /// Escape handler). The harness stores them and exposes
    /// <see cref="FireDocumentEvent"/> so tests can dispatch.</summary>
    private readonly Dictionary<string, List<Action<DomEvent>>> _docListeners = new();
    public void addEventListener(string type, Action<DomEvent> handler, object? _ = null)
    {
        if (!_docListeners.TryGetValue(type, out var list))
        {
            list = new List<Action<DomEvent>>();
            _docListeners[type] = list;
        }
        list.Add(handler);
    }
    public void removeEventListener(string type, Action<DomEvent> handler)
    {
        if (_docListeners.TryGetValue(type, out var list)) list.Remove(handler);
    }
    /// <summary>Test-side — fires a synthetic event on document-level listeners
    /// (e.g. the Treemap script's global <c>document.addEventListener('keydown', …)</c>).</summary>
    public void FireDocumentEvent(DomEvent ev)
    {
        if (_docListeners.TryGetValue(ev.type, out var list))
            foreach (var h in list.ToArray()) h(ev);
    }
}

/// <summary>CSS-selector → XElement matcher. Supports the subset the embedded scripts use:
/// tag-only (<c>svg</c>), attribute-presence (<c>[data-v3d]</c>), attribute-equals
/// (<c>[data-series-index="0"]</c>), descendant combinator (<c>g > polygon</c>), and union (<c>,</c>).
/// Deliberately minimal — extend ONLY when a real script needs more.</summary>
internal static class DomSelector
{
    public static IEnumerable<XElement> Find(XElement root, string selector)
    {
        // Union: split on top-level commas
        var parts = selector.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length > 1)
        {
            var seen = new HashSet<XElement>();
            foreach (var part in parts)
                foreach (var e in Find(root, part))
                    if (seen.Add(e)) yield return e;
            yield break;
        }

        var s = selector.Trim();
        // Descendant combinator " > " — narrow children of left side
        var gtIdx = s.IndexOf(" > ", StringComparison.Ordinal);
        if (gtIdx > 0)
        {
            var left = s[..gtIdx].Trim();
            var right = s[(gtIdx + 3)..].Trim();
            foreach (var l in Find(root, left))
                foreach (var r in l.Elements().Where(e => MatchesSimple(e, right)))
                    yield return r;
            yield break;
        }
        // Plain descendant (space) — split on the FIRST top-level space, ignoring spaces
        // that fall inside `[...]` attribute brackets (e.g. `[data-x="a b"]`).
        int splitIdx = -1, depth = 0;
        for (int i = 0; i < s.Length; i++)
        {
            if (s[i] == '[') depth++;
            else if (s[i] == ']') depth--;
            else if (s[i] == ' ' && depth == 0) { splitIdx = i; break; }
        }
        if (splitIdx > 0)
        {
            var left = s[..splitIdx].Trim();
            var right = s[(splitIdx + 1)..].Trim();
            foreach (var l in Find(root, left))
                foreach (var r in l.Descendants().Where(e => MatchesSimple(e, right)))
                    yield return r;
            yield break;
        }

        // Simple selector — match on root + every descendant
        if (MatchesSimple(root, s)) yield return root;
        foreach (var e in root.Descendants().Where(e => MatchesSimple(e, s)))
            yield return e;
    }

    /// <summary>True when <paramref name="el"/> matches <paramref name="simple"/>.
    /// Forms supported: <c>tag</c>, <c>#id</c>, <c>[attr]</c>, <c>[attr="value"]</c>,
    /// <c>tag[attr=...]</c>, <c>.classname</c>, <c>tag.classname</c>.</summary>
    private static bool MatchesSimple(XElement el, string simple)
    {
        if (string.IsNullOrEmpty(simple) || simple == "*") return true;

        // Strip id selector (#foo) from the start if present
        string tagPart = simple;
        string? idNeeded = null;
        if (tagPart.StartsWith('#'))
        {
            idNeeded = tagPart[1..];
            tagPart = "*";
        }

        // Strip class selector (.foo) from the end if present.
        // A dot is ONLY a class separator when it appears at the TOP level (not inside
        // `[...]` brackets — e.g. `rect[data-treemap-node='0.0']` must NOT split at the
        // inner dot). Walk right-to-left tracking bracket depth; first dot at depth 0
        // is the class separator. If no top-level dot exists, there's no class selector.
        string? classNeeded = null;
        int dotIdx = -1, depth2 = 0;
        for (int i = tagPart.Length - 1; i >= 0; i--)
        {
            if (tagPart[i] == ']') depth2++;
            else if (tagPart[i] == '[') depth2--;
            else if (tagPart[i] == '.' && depth2 == 0) { dotIdx = i; break; }
        }
        if (dotIdx >= 0)
        {
            classNeeded = tagPart[(dotIdx + 1)..];
            tagPart = tagPart[..dotIdx];
            if (tagPart.Length == 0) tagPart = "*";
        }

        // Strip attribute selector ([...]) if present
        string? attrPart = null;
        var bracketIdx = tagPart.IndexOf('[');
        if (bracketIdx >= 0)
        {
            attrPart = tagPart[bracketIdx..];
            tagPart = tagPart[..bracketIdx];
            if (tagPart.Length == 0) tagPart = "*";
        }

        if (tagPart != "*" && !el.Name.LocalName.Equals(tagPart, StringComparison.OrdinalIgnoreCase))
            return false;

        if (idNeeded is not null && el.Attribute("id")?.Value != idNeeded)
            return false;

        if (classNeeded is not null)
        {
            var cls = el.Attribute("class")?.Value ?? "";
            if (!cls.Split(' ').Any(c => c.Equals(classNeeded, StringComparison.Ordinal))) return false;
        }

        if (attrPart is not null)
        {
            // [attr] or [attr="value"]
            var inner = attrPart.Trim('[', ']');
            var eqIdx = inner.IndexOf('=');
            if (eqIdx < 0)
            {
                if (el.Attribute(inner) is null) return false;
            }
            else
            {
                var aname = inner[..eqIdx];
                var avalue = inner[(eqIdx + 1)..].Trim('"', '\'');
                if (el.Attribute(aname)?.Value != avalue) return false;
            }
        }
        return true;
    }
}
