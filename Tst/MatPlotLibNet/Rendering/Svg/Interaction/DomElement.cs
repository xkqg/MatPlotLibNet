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

    /// <summary>Stubs for DOM Pointer Events capture API. The harness does not emulate
    /// pointer ID routing, but scripts call these to clean up drag state; returning
    /// silently keeps them happy.</summary>
    public void setPointerCapture(int _) { }
    public void releasePointerCapture(int _) { }

    /// <summary>Mirrors DOM <c>element.getBoundingClientRect()</c>. Tests don't run a
    /// real layout engine, but Phase-12 focus tooltip positioning (G.4) depends on
    /// this returning non-(0,0,0,0) bounds so <c>left + width/2</c> is meaningful.
    /// Returns synthetic positive bounds derived from the element's identity.</summary>
    public DomBoundingRect getBoundingClientRect() => new()
    {
        left = 50,
        top = 30,
        width = 20,
        height = 10,
        right = 70,
        bottom = 40,
    };

    /// <summary>Stub <c>clientWidth</c> / <c>clientHeight</c> — for SVG elements, reads the
    /// rendered <c>width</c>/<c>height</c> attributes the figure pipeline always emits.
    /// Falls back to 1 (not 0) so divisions don't NaN. Phase C of v1.7.2 follow-on plan
    /// added matplotlib-style pan math that divides by clientWidth.</summary>
    public double clientWidth => double.TryParse(Xml.Attribute("width")?.Value,
        System.Globalization.CultureInfo.InvariantCulture, out var w) ? w : 1;
    public double clientHeight => double.TryParse(Xml.Attribute("height")?.Value,
        System.Globalization.CultureInfo.InvariantCulture, out var h) ? h : 1;
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
    public string left        { get => _props.GetValueOrDefault("left", ""); set => Set("left", value); }
    public string top         { get => _props.GetValueOrDefault("top", ""); set => Set("top", value); }
    public string transition  { get => _props.GetValueOrDefault("transition", ""); set => Set("transition", value); }

    /// <summary>Mirrors DOM <c>element.style.cssText</c> — assigning a full CSS declaration
    /// block parses it in one shot, replacing any previously-set properties on this element.
    /// Used by <see cref="MatPlotLibNet.Rendering.Svg.SvgCustomTooltipScript"/> to initialize
    /// the floating tooltip <c>div</c>'s visual styles.</summary>
    public string cssText
    {
        get => string.Join("; ", _props.Select(kv => $"{kv.Key}: {kv.Value}"));
        set
        {
            _props.Clear();
            if (!string.IsNullOrWhiteSpace(value))
            {
                foreach (var pair in value.Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    var kv = pair.Split(':', 2);
                    if (kv.Length == 2) _props[kv[0].Trim()] = kv[1].Trim();
                }
            }
            _xml.SetAttributeValue("style", string.Join("; ", _props.Select(kv => $"{kv.Key}: {kv.Value}")));
        }
    }

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
    public int pointerId { get; set; }
    public DomElement? target { get; set; }
    public bool DefaultPrevented { get; private set; }
    /// <summary>Payload attached via <c>new CustomEvent(type, { detail: … })</c>.</summary>
    public object? detail { get; set; }

    public DomEvent(string type) { this.type = type; }

    /// <summary>Mirrors JS <c>new CustomEvent(type, { detail, bubbles })</c>. Jint
    /// interop calls this ctor when scripts dispatch custom events (e.g. the
    /// selection script's <c>mpl:selection</c>).</summary>
    public DomEvent(string type, Jint.Native.Object.ObjectInstance? options) : this(type)
    {
        if (options is null) return;
        var d = options.Get("detail");
        var obj = d?.ToObject();
        if (obj is not null) detail = obj;
    }

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

/// <summary>Stub for DOM <c>Element.getBoundingClientRect()</c> return value.
/// Synthetic positive bounds used by <see cref="MatPlotLibNet.Rendering.Svg.SvgCustomTooltipScript"/>
/// focus-positioning tests (Phase 12 of v1.7.2 plan + Phase G.4 behavioural pin).</summary>
public sealed class DomBoundingRect
{
    public double left { get; set; }
    public double top { get; set; }
    public double width { get; set; }
    public double height { get; set; }
    public double right { get; set; }
    public double bottom { get; set; }
}
