// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Samples;

/// <summary>
/// Phase M.1 — "Open in new tab" must hand the browser a full HTML document
/// so embedded pan/zoom/tooltip scripts run AND the chart fills the viewport.
/// Pre-fix: bare SVG blob (<c>image/svg+xml</c>) landed in a standalone-SVG
/// context where neither worked.
/// <para>The wrap helper is <c>SvgIframeWrapper.WrapForIframe</c> (already
/// covered by <c>SvgIframeWrapperTests</c>). This file pins two integration
/// contracts that are easy to break accidentally during future refactors:
/// (a) the Razor page's <c>OpenInNewTab</c> method still references the
/// wrapper; (b) the wrapper output fits the <c>&lt;iframe srcdoc&gt;</c>
/// contract shared with the new-tab blob path.</para>
/// </summary>
public class PlaygroundNewTabTests
{
    // Path to the Razor source, relative to the repo root. Running tests via
    // `dotnet run` from the Tst project means cwd is that project; walk up.
    private static string PlaygroundRazorPath()
    {
        // Walk up from AppContext.BaseDirectory to the repo root, identified by
        // the presence of a .sln or CHANGELOG.md file at the top level.
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null
               && !File.Exists(Path.Combine(dir.FullName, "CHANGELOG.md"))
               && !File.Exists(Path.Combine(dir.FullName, "MatPlotLibNet.sln")))
            dir = dir.Parent;
        Assert.NotNull(dir);
        var razor = Path.Combine(dir!.FullName,
            "Samples", "MatPlotLibNet.Playground", "Pages", "Playground.razor");
        Assert.True(File.Exists(razor),
            $"Playground.razor not found at expected path: {razor} (repo root detected as {dir.FullName})");
        return razor;
    }

    [Fact]
    public void OpenInNewTab_RazorMethod_DelegatesToSvgIframeWrapper()
    {
        // Regression guard: if someone reverts the M.1 change, the Razor source
        // will go back to `JS.InvokeVoidAsync("openInNewTab", _svg)` — bare SVG.
        // This test fails the build the moment that happens.
        string razor = File.ReadAllText(PlaygroundRazorPath());
        Assert.Contains("openInNewTab", razor);
        Assert.Contains("SvgIframeWrapper.WrapForIframe(_svg)", razor);
    }

    [Fact]
    public void WrappedHtml_ContainsScriptTag_InsideBody()
    {
        // The new-tab path shares the wrapper with the iframe path. If a future
        // refactor splits them, this test forces a conscious decision.
        string sample = "<svg><script>window.sigil='NEW_TAB_M1'</script></svg>";
        string wrapped = MatPlotLibNet.Playground.SvgIframeWrapper.WrapForIframe(sample);

        Assert.StartsWith("<!DOCTYPE html>", wrapped);
        int body   = wrapped.IndexOf("<body", StringComparison.Ordinal);
        int script = wrapped.IndexOf("NEW_TAB_M1", StringComparison.Ordinal);
        int bodyEnd = wrapped.IndexOf("</body>", StringComparison.Ordinal);
        Assert.True(body >= 0 && bodyEnd > body && script > body && script < bodyEnd,
            "wrapped payload must place script inside <body> so the browser executes it on parse");
    }
}
