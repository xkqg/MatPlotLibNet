// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>The pieces of code every embedded script shares, each written once and put into the scripts that need it: how a
/// script finds the chart it belongs to, how it holds a pointer for a drag, how it turns a pointer into the chart's own units and
/// how it reads a token of the interaction theme off the chart. Every script is an immediately run function of its own, so a
/// piece is part of each script that uses it; each line carries the indent of the script's body, and a piece is put on a line of
/// its own.</summary>
internal static class SvgScriptPreamble
{
    /// <summary>The start of the locator: the parent of the script that is running, which is its chart, so two charts on one page
    /// each wire their own.</summary>
    private const string Locator = "    var svg = (document.currentScript && document.currentScript.parentNode) || ";

    /// <summary>The end of the locator: a script that found no chart does nothing.</summary>
    private const string Guard = ";\n    if (!svg || svg.tagName !== 'svg') return;";

    /// <summary>The chart of the script, or, where a page runs it without a current script, the document's first chart.</summary>
    internal const string Root = Locator + "document.querySelector('svg')" + Guard;

    /// <summary>The chart of the server-interaction script, or the document's first chart with a chart id.</summary>
    internal const string ServerRoot = Locator + "document.querySelector('svg[" + SvgAttributes.ChartId + "]')" + Guard;

    /// <summary>The chart of the treemap script, or the chart of the document's first treemap tile.</summary>
    internal const string TreemapRoot = Locator + "(document.querySelector('[data-treemap-node]') || {}).ownerSVGElement" + Guard;

    /// <summary>Holds a pointer to an element for a drag: a pointer the browser does not know (a synthetic event, a mouse event
    /// without a pointer) is left alone, and a refusal never stops the drag.</summary>
    internal const string Capture = """
            function capture(el, e) { if (el.setPointerCapture && e.pointerId !== undefined) try { el.setPointerCapture(e.pointerId); } catch (_) {} }
        """;

    /// <summary>Lets a held pointer go, under the same guard as <see cref="Capture"/>; only a script that lets go itself carries it.</summary>
    internal const string Release = """
            function release(el, e) { if (el.releasePointerCapture && e.pointerId !== undefined) try { el.releasePointerCapture(e.pointerId); } catch (_) {} }
        """;

    /// <summary>A token of the interaction theme off the chart's root (<see cref="Transforms.SvgTransform.InteractionToken.Read"/>): the number the root
    /// carries, else the default the script was written with. A token of nought is a token, not a missing one.</summary>
    internal const string Token = """
            function token(name, dflt) { var v = parseFloat(svg.getAttribute(name)); return isNaN(v) ? dflt : v; }
        """;

    /// <summary>Whether the page's reader asked for reduced motion (<c>prefers-reduced-motion</c>): a script that animates then
    /// does not.</summary>
    internal const string ReducedMotion = """
            var reduceMotion = !!(window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches);
        """;

    /// <summary>What one notch of the wheel scales a range by when it zooms in (<see cref="Interaction.ZoomNotch.In"/>), as a
    /// script writes it; a notch out is its power of minus one.</summary>
    internal static readonly string ZoomNotchIn = Interaction.ZoomNotch.In.InvariantText("R");

    /// <summary>A point of the page (client pixels) in the chart's own units, through the chart's screen transform; a chart that
    /// is not laid out has none, and the point is then read against its box.</summary>
    internal const string UserPoint = """
            function userPoint(x, y) {
                var m = svg.getScreenCTM ? svg.getScreenCTM() : null;
                if (!m) { var r = svg.getBoundingClientRect(); return { x: x - r.left, y: y - r.top }; }
                var pt = svg.createSVGPoint(); pt.x = x; pt.y = y;
                return pt.matrixTransform(m.inverse());
            }
        """;
}
