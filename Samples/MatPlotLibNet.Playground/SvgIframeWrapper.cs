// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Playground;

/// <summary>
/// Wraps an SVG payload in a minimal HTML document so it can be handed to an
/// <c>&lt;iframe srcdoc&gt;</c> and have its embedded browser-interaction
/// scripts execute reliably. Passing bare SVG to <c>srcdoc</c> lands it in an
/// SVG-in-HTML parsing context where inline <c>&lt;script&gt;</c> elements
/// don't run in every browser — the HTML-document wrap gives the parser a
/// proper top-level HTML context where script execution is well-defined.
/// </summary>
public static class SvgIframeWrapper
{
    private const string Prelude =
        "<!DOCTYPE html><html><head><meta charset=\"utf-8\">" +
        "<style>html,body{margin:0;padding:0;overflow:hidden;" +
        "width:100%;height:100%;}svg{width:100%;height:100%;display:block;}" +
        "</style></head><body>";

    private const string Coda = "</body></html>";

    /// <summary>Wraps the supplied SVG markup in a self-contained HTML document.</summary>
    public static string WrapForIframe(string svg) => Prelude + svg + Coda;
}
