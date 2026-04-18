// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Playground;

namespace MatPlotLibNet.Tests.Samples;

/// <summary>
/// L.7 regression fix: the playground iframe preview must wrap SVG in a proper
/// HTML document so embedded browser-interaction scripts actually execute.
/// Passing bare SVG to <c>&lt;iframe srcdoc&gt;</c> lands it in an
/// SVG-in-HTML parsing context where inline scripts do not run reliably.
/// </summary>
public class SvgIframeWrapperTests
{
    private const string SampleSvg =
        "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 100 50\">" +
        "<rect width=\"100\" height=\"50\" fill=\"red\"/>" +
        "<script>(function(){/*pan-zoom*/})();</script>" +
        "</svg>";

    [Fact]
    public void WrapForIframe_StartsWithHtmlDoctype()
    {
        string wrapped = SvgIframeWrapper.WrapForIframe(SampleSvg);
        Assert.StartsWith("<!DOCTYPE html>", wrapped);
    }

    [Fact]
    public void WrapForIframe_PreservesOriginalSvg()
    {
        string wrapped = SvgIframeWrapper.WrapForIframe(SampleSvg);
        Assert.Contains(SampleSvg, wrapped);
    }

    [Fact]
    public void WrapForIframe_ContainsScriptInsideBody()
    {
        string wrapped = SvgIframeWrapper.WrapForIframe(SampleSvg);
        int bodyStart  = wrapped.IndexOf("<body", StringComparison.Ordinal);
        int scriptIdx  = wrapped.IndexOf("<script", StringComparison.Ordinal);
        int bodyEnd    = wrapped.IndexOf("</body>", StringComparison.Ordinal);
        Assert.True(bodyStart >= 0, "wrapped document must contain <body>");
        Assert.True(bodyEnd   > bodyStart, "wrapped document must contain </body>");
        Assert.True(scriptIdx > bodyStart && scriptIdx < bodyEnd,
            "<script> must live inside <body> so the HTML parser schedules it");
    }

    [Fact]
    public void WrapForIframe_EmitsFillStyleSoSvgFillsIframe()
    {
        // Without explicit CSS, bare pre-Phase-L SVGs with pixel width/height would
        // not resize with the iframe — the L.7 regression fix emits its own fill-CSS
        // so it works standalone from the L.1–L.2 responsive-SVG default.
        string wrapped = SvgIframeWrapper.WrapForIframe(SampleSvg);
        Assert.Contains("width:100%", wrapped);
        Assert.Contains("height:100%", wrapped);
    }

    [Fact]
    public void WrapForIframe_IsIdempotentOnEmptyInput()
    {
        string wrapped = SvgIframeWrapper.WrapForIframe("");
        Assert.StartsWith("<!DOCTYPE html>", wrapped);
        Assert.Contains("<body>", wrapped);
        Assert.Contains("</body>", wrapped);
    }
}
