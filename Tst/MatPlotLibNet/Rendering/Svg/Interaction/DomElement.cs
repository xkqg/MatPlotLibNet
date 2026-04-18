// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;

namespace MatPlotLibNet.Tests.Rendering.Svg.Interaction;

/// <summary>JS-facing wrapper around an <see cref="XElement"/>. Exposes the DOM Element
/// surface that the embedded interaction scripts use: <c>getAttribute</c>, <c>setAttribute</c>,
/// <c>tagName</c>, <c>style</c>, <c>addEventListener</c>, <c>querySelector(All)</c>, etc.
/// State changes (attribute writes, style writes) mutate the underlying XElement so the
/// test can assert against the actual XML afterwards.</summary>
public sealed class DomElement
{
    internal readonly XElement Xml;
    private readonly DomDocument _doc;
    private readonly Dictionary<string, List<Action<DomEvent>>> _listeners = new();
    private readonly DomStyle _style;

    internal DomElement(XElement xml, DomDocument doc)
    {
        Xml = xml;
        _doc = doc;
        _style = new DomStyle(xml);
    }

    /// <summary>Lower-case tag name, mirroring HTML/SVG convention (`polygon`, `text`, `g`).</summary>
    public string tagName => Xml.Name.LocalName;

    public DomStyle style => _style;

    public DomElement? parentNode => Xml.Parent is { } p ? _doc.Wrap(p) : null;

    public DomElement[] children => Xml.Elements().Select(_doc.Wrap).ToArray();

    public string? getAttribute(string name) => Xml.Attribute(name)?.Value;

    public bool hasAttribute(string name) => Xml.Attribute(name) is not null;

    public void setAttribute(string name, object value) => Xml.SetAttributeValue(name, value?.ToString() ?? "");

    public void removeAttribute(string name) => Xml.Attribute(name)?.Remove();

    public DomElement? querySelector(string selector) =>
        DomSelector.Find(Xml, selector).FirstOrDefault() is { } e ? _doc.Wrap(e) : null;

    public object querySelectorAll(string selector)
    {
        var arr = DomSelector.Find(Xml, selector).Select(_doc.Wrap).ToArray();
        return JsArrayWrap.Wrap(_doc.Engine, arr);
    }

    /// <summary>Registers an event listener. The optional third argument (typically
    /// `{ passive: false }` or `true` for capture) is accepted but ignored — the harness
    /// fires every registered listener regardless of capture/passive flags. Tests assert
    /// the option's PRESENCE in the script source separately.</summary>
    public void addEventListener(string type, Action<DomEvent> handler, object? options = null)
    {
        if (!_listeners.TryGetValue(type, out var list))
        {
            list = new List<Action<DomEvent>>();
            _listeners[type] = list;
        }
        list.Add(handler);
    }

    public void removeEventListener(string type, Action<DomEvent> handler)
    {
        if (_listeners.TryGetValue(type, out var list)) list.Remove(handler);
    }

    /// <summary>Used by the test harness (NOT by the script under test) to fire events.</summary>
    internal void Fire(DomEvent ev)
    {
        if (_listeners.TryGetValue(ev.type, out var list))
            foreach (var h in list.ToArray()) h(ev);
    }

    public void dispatchEvent(DomEvent ev) => Fire(ev);

    public void appendChild(DomElement child)
    {
        Xml.Add(child.Xml);
    }

    public void removeChild(DomElement child) => child.Xml.Remove();

    /// <summary>Stub for SVG's <c>createSVGPoint().matrixTransform(...)</c> chain used by
    /// the zoom script. Returns a tiny object that supports the few props the script uses.</summary>
    public DomSvgPoint createSVGPoint() => new();

    /// <summary>Stub returning identity matrix — coordinate transformation in tests is
    /// asserted by the test directly, not by chasing SVG transform math.</summary>
    public DomSvgMatrix? getScreenCTM() => new();
}

/// <summary>Wrapper for `element.style` — supports indexer-style writes the scripts use:
/// <c>el.style.cursor = 'grab'</c> sets <c>style="cursor: grab"</c> on the XElement.</summary>
public sealed class DomStyle
{
    private readonly XElement _xml;
    private readonly Dictionary<string, string> _props = new();
    public DomStyle(XElement xml)
    {
        _xml = xml;
        // Parse pre-existing style attribute so reads/writes are consistent
        var existing = xml.Attribute("style")?.Value;
        if (!string.IsNullOrWhiteSpace(existing))
            foreach (var pair in existing.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var kv = pair.Split(':', 2);
                if (kv.Length == 2) _props[kv[0].Trim()] = kv[1].Trim();
            }
    }

    public string cursor      { get => _props.GetValueOrDefault("cursor", ""); set => Set("cursor", value); }
    public string display     { get => _props.GetValueOrDefault("display", ""); set => Set("display", value); }
    public string opacity     { get => _props.GetValueOrDefault("opacity", ""); set => Set("opacity", value); }
    public string visibility  { get => _props.GetValueOrDefault("visibility", ""); set => Set("visibility", value); }

    private void Set(string key, string value)
    {
        _props[key] = value;
        _xml.SetAttributeValue("style", string.Join("; ", _props.Select(kv => $"{kv.Key}: {kv.Value}")));
    }
}

/// <summary>Bag with the few <see cref="MouseEvent"/>-like properties scripts read from
/// dispatched events. Tests construct one and pass to <see cref="DomElement.dispatchEvent"/>.</summary>
public sealed class DomEvent
{
    public string type { get; }
    public double clientX { get; set; }
    public double clientY { get; set; }
    public double deltaY { get; set; }
    public string key { get; set; } = "";
    public bool shiftKey { get; set; }
    public int button { get; set; }
    public int clickCount { get; set; }
    public DomElement? target { get; set; }
    public bool DefaultPrevented { get; private set; }

    public DomEvent(string type) { this.type = type; }

    public void preventDefault() => DefaultPrevented = true;
    public void stopPropagation() { /* no-op for now — harness doesn't bubble */ }
}

public sealed class DomSvgPoint
{
    public double x { get; set; }
    public double y { get; set; }
    public DomSvgPoint matrixTransform(DomSvgMatrix? _) => this;
}

public sealed class DomSvgMatrix
{
    public DomSvgMatrix? inverse() => this;
}
