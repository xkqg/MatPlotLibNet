// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Packaging.OwnedRules;

/// <summary>The vocabulary the SVG writer, its scripts, the page and the clients share (a hub method, an attribute, a part marker,
/// a token, a script's preamble, the namespace, a notch, a camera, a number, the loopback address), and the elements one owner of
/// the SVG surface writes.</summary>
internal static class ScriptAndSvgRules
{
/// <summary>The name of a hub method of the chart hub travels as a string: the page's script, the Interactive page and the Blazor
    /// client each spelled it, so a rename on the server reached none of them. HubMethodNames writes each once.</summary>
    private static readonly OwnedRule HubMethodName = new(
        "the name of a hub method of the chart hub (an event a page sends, a message the server sends, a subscription) is written once, in HubMethodNames: no script, page or client spells one",
        @"[""'](?:On(?:Zoom|Pan|Reset|LegendToggle|BrushSelect|Hover|DataCursor|RectangleZoom|SpanSelect|Rotate3D|AddTrendline|AddHorizontalLevel|AddFibonacciRetracement|Cursor)|UpdateChart(?:Svg|Parts)?|ReceiveTooltipContent|ReceiveCursor|ViewChanged|ViewerLeft|Subscribe(?:ToParts)?|Unsubscribe)[""']",
        "Src/MatPlotLibNet/Interaction/HubMethodNames.cs") { Under = "Src/", Finds = [
        "_svgUpdatedToken = _connector.Listen(_hub, \"UpdateChartSvg\", HandleSvgUpdatedAsync);",
        "                invoke('OnZoom', {",
        "            await connection.invoke(\"SubscribeToParts\", chartId);"] };

/// <summary>The attributes the SVG writer puts on a chart for its interaction scripts (the chart id, an axes' view, a series, a
    /// legend entry) are its protocol with them: SvgAttributes names each once, the writers append it and the scripts read it.</summary>
    private static readonly OwnedRule ScriptAttributeName = new(
        "the name of an attribute the SVG writer puts on a chart for its scripts (the chart id, an axes' view, a series, a legend entry) is written once, in SvgAttributes: no writer, script or reader spells one",
        @"data-(?:chart-id|axes-index|series-index|legend-index|plot-[xywh]|(?:reset-)?[xy](?:min|max)|[xy](?:scale|linthresh|breaks)|sec-|mpl-busy)",
        "Src/MatPlotLibNet/Rendering/Svg/SvgAttributes.cs") { Under = "Src/", Finds = [
        "            var groups = svg.querySelectorAll('[data-series-index]');",
        "                    x: num(el, 'data-plot-x'), y: num(el, 'data-plot-y'),",
        "                sb.Append(\" data-reset-xmin=\\\"\").Append(xmin.ToString(\"G6\", invA))"] };

/// <summary>The marker of a part of a document sent in parts is written by the core, re-read as text by Blazor and as markup by
    /// the Interactive page.</summary>
    private static readonly OwnedRule PartMarker = new(
        "the marker of a part of a document sent in parts (its group, its attribute, its end) is written once, in SvgPartMarker: no writer, client or page spells it",
        @"data-mpl-part|<g data-mpl",
        "Src/MatPlotLibNet/Rendering/Svg/SvgPartMarker.cs") { Under = "Src/", Finds = [
        "    private const string PartStart = \"<g data-mpl-part=\\\"\";"] };

/// <summary>A token of the interaction theme is written on the root under its attribute and read by a script.</summary>
    private static readonly OwnedRule InteractionTokenName = new(
        "a token of the interaction theme is written on the root under its attribute and read back by a script through InteractionToken: no writer or script spells a token's attribute",
        @"data-mpl-(?:highlight-opacity|sankey-(?:link|node)-opacity|treemap-transition-ms|tooltip-offset-[xy])",
        "Src/MatPlotLibNet/Transforms/SvgTransform.cs") { Under = "Src/", Finds = [
        "var dim = parseFloat(svg.getAttribute('data-mpl-highlight-opacity')) || 0.3;"] };

/// <summary>How a script finds its chart, holds a pointer and turns a pointer into the chart's own units is one fragment each.</summary>
    private static readonly OwnedRule ScriptPreambleRule = new(
        "a script finds the chart it belongs to, holds a pointer and turns a pointer into the chart's user units through SvgScriptPreamble's fragments: no script spells the current-script locator, a pointer capture or a screen-to-user transform itself",
        @"document\.currentScript|\.setPointerCapture\(|\.releasePointerCapture\(|createSVGPoint\(|getScreenCTM\(",
        "Src/MatPlotLibNet/Rendering/Svg/SvgScriptPreamble.cs") { Under = "Src/", Finds = [
        "            var svg = (document.currentScript && document.currentScript.parentNode) || document;",
        "                svg.setPointerCapture(e.pointerId);",
        "                var pt = svg.createSVGPoint();"] };

/// <summary>The SVG namespace.</summary>
    private static readonly OwnedRule SvgNamespace = new(
        "the SVG namespace is written once, in SvgAttributes.Namespace: the writers, the scripts and the page read it",
        @"www\.w3\.org/2000/svg",
        "Src/MatPlotLibNet/Rendering/Svg/SvgAttributes.cs") { Under = "Src/", Finds = [
        "                    const svgNs = \"http://www.w3.org/2000/svg\";"] };

/// <summary>The scripts zoom by the notch they are given, never by a factor of their own.</summary>
    private static readonly OwnedRule ZoomNotchOfAScript = OwnedRule.Nowhere(
        "a script zooms one notch of the wheel by ZoomNotch's factor, which it is given: no script types a notch",
        @"Math\.pow\(\s*0\.85|\?\s*1\.1\s*:\s*0\.9\b|\b0\.85\b",
        under: "Src/MatPlotLibNet/Rendering/Svg/") with { Finds = [
        "                var scale = e.deltaY > 0 ? 1.1 : 0.9;",
        "                var scale = Math.pow(0.85, step);"] };

/// <summary>The camera of a 3-D scene is always written on the scene, so its script reads it and types none of its own.</summary>
    private static readonly OwnedRule CameraOfAScript = OwnedRule.Nowhere(
        "a 3-D scene's script reads the camera the writer always puts on the scene: it types no resting elevation, azimuth or distance",
        @"(?:\|\||[?:])\s*'?-?(?:30|60|10)'?\s*(?:[;)]|$)",
        under: "Src/MatPlotLibNet/Transforms/SvgTransform.Svg3DRotationScript.cs") with { Finds = [
        "                var elevation = parseFloat(scene.getAttribute('data-elevation') || '30');",
        "                        d: distance !== null ? distance : 10"] };

/// <summary>A number in an attribute the SVG transforms write is the SVG writer's own number text.</summary>
    private static readonly OwnedRule NumberOfAnAttribute = OwnedRule.Nowhere(
        "a number in an attribute an SVG transform writes is the SVG writer's own number text (AppendSvgNumber for the drawing, AppendExactSvgNumber for what a script reads back): no transform chooses an invariant format at the call site",
        @"ToString\(\s*""(?:G\d*|0\.#+|F\d*|R)""",
        under: "Src/MatPlotLibNet/Transforms/") with { Finds = [
        "        string size(double v) => v.ToString(\"0.##\", CultureInfo.InvariantCulture);"] };

/// <summary>The local chart server is listened on and reached at one address.</summary>
    private static readonly OwnedRule LoopbackAddress = new(
        "the address the local chart server listens on and is reached at is ChartServer's loopback, written once; its page reaches the hub on the origin it was served from",
        @"127\.0\.0\.1|localhost",
        "Src/MatPlotLibNet.Interactive/ChartServer.cs") { Under = "Src/", Finds = [
        "                        .withUrl(\"http://localhost:{{port}}/charts-hub\")"] };

/// <summary>The data attributes of a treemap's tile and text are written by <c>TagTreemapNode</c> alone.</summary>
    private static readonly OwnedRule TreemapNodeTag = new(
        "the data attributes a treemap tile and its text carry (treemap-node, -depth, -parent, -label) are written by ctx.TagTreemapNode: no renderer sets them one by one",
        @"SetNextElementData\(\s*""treemap-",
        "Src/MatPlotLibNet/Rendering/SeriesRenderers/Hierarchical/TreemapSeriesRenderer.cs") { Under = "Src/", Finds = [
        "            Ctx.SetNextElementData(\"treemap-node\", nodeId);",
        "        Ctx.SetNextElementData(\"treemap-label\", node.Label ?? string.Empty);"] };

/// <summary>An SVG element meets the clips in the one envelope of the element buffer: a mark applies the clips a text lifted, a text
    /// and a group of the browser lift them (<c>MarkClips</c>). No writer asks the clips itself.</summary>
    private static readonly OwnedRule ClipsOfAnSvgElement = new(
        "an SVG element meets the clips in SvgElementBuffer's one envelope (a mark applies the clips a text lifted, a text lifts them unless the texts are cut, a group lifts them): no SVG writer asks the clips itself",
        @"_clips\.(?:Mark|Lift|Text)\(|\.(?:Mark|Lift|Text)\(this\)",
        "Src/MatPlotLibNet/Rendering/Svg/SvgElementBuffer.cs") { Under = "Src/MatPlotLibNet/Rendering/Svg/", Finds = [
        "        _clips.Mark(this);",
        "        _clips.Lift(this);"] };

/// <summary>What a draw asks of the next SVG element (its depth in a tree, a class, data for a script, the width a text is set to)
    /// is kept by the element buffer and written by its one envelope.</summary>
    private static readonly OwnedRule NextElementAttributes = new(
        "what is asked of the next SVG element (its aria-level, class, data-* and text length) is kept and written by SvgElementBuffer's one envelope: no writer keeps or writes it itself",
        @"""\s+aria-level=|""\s+data-""|\b_pending\w+\b",
        "Src/MatPlotLibNet/Rendering/Svg/SvgElementBuffer.cs") { Under = "Src/MatPlotLibNet/Rendering/Svg/", Finds = [
        "            _sb.Append(\" aria-level=\\\"\").Append(level.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append('\"');",
        "            _sb.Append(\" data-\").Append(attr.Key).Append(\"=\\\"\").Append(attr.Value.EscapeForXml()).Append('\"');",
        "    private string? _pendingClass;"] };

/// <summary>The SVG series visitor names the renderer of a kind once, in the Visit line of that kind, and makes it there: a visitor
    /// is made for one series (<c>SeriesPass</c>), so a lazy field per kind kept nothing between two series and named every renderer
    /// twice (the field's type and the Visit that read it).</summary>
    private static readonly OwnedRule RendererOfAKindNamedOnce = OwnedRule.Nowhere(
        "the SVG series visitor makes the renderer of a kind in the Visit line of that kind: no field per kind, no lazy ??= new(_context)",
        @"\?\?=\s*new\(_context\)|private\s+(?:\w+SeriesRenderer|MonotonicLineRenderer<\w+>)\??\s+_\w+\s*;",
        under: "Src/MatPlotLibNet/Rendering/") with { Finds = [
        "public void Visit(LineSeries s, RenderArea a) => Dispatch(_line ??= new(_context), s);",
        "private LineSeriesRenderer? _line;",
        "private MonotonicLineRenderer<SignalSeries>? _signal;"], Ignores = [
        "public void Visit(LineSeries s, RenderArea a) => Dispatch(new LineSeriesRenderer(_context), s);"] };

    /// <summary>The rules of the area, in the order the scan reads them.</summary>
    internal static OwnedRule[] All { get; } =
    [
        HubMethodName,
        ScriptAttributeName,
        PartMarker,
        InteractionTokenName,
        ScriptPreambleRule,
        SvgNamespace,
        ZoomNotchOfAScript,
        CameraOfAScript,
        NumberOfAnAttribute,
        LoopbackAddress,
        TreemapNodeTag,
        new("the SVG clip path element is written by the element buffer that owns the clip stack",
            "<clipPath",
            "Src/MatPlotLibNet/Rendering/Svg/SvgElementBuffer.cs") { Finds = [
            "        _sb.Append(\"<defs><clipPath id=\\\"\").Append(id)"] },
        ClipsOfAnSvgElement,
        NextElementAttributes,
        new("the SVG gradient paint server is written by the gradient registry",
            "<linearGradient",
            "Src/MatPlotLibNet/Rendering/Svg/SvgElementBuffer.cs") { Finds = [
            "        _sb.Append(\"<defs><linearGradient id=\\\"\").Append(refId)"] },
        new("the SVG hatch paint server is written by the hatch registry",
            "<pattern ",
            "Src/MatPlotLibNet/Rendering/Svg/SvgElementBuffer.cs") { Finds = [
            "        _sb.Append(\"<defs><pattern id=\\\"\").Append(refId)"] },
        RendererOfAKindNamedOnce,
    ];
}
