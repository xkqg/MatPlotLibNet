// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Playground;

namespace MatPlotLibNet.Tests.Samples;

/// <summary>TDD verification of every Playground toggle. The Playground is the user's
/// "look and feel" entry point — every toggle MUST produce the visual change it advertises.
///
/// <para>Bugs caught by these tests (would have prevented v1.7.0 playground regressions):</para>
/// <list type="bullet">
/// <item><description>Grid toggle was inverted (checking made grid fade, unchecking left it thick)</description></item>
/// <item><description>TightLayout was applied BEFORE subplots were added, so layout calc had no data</description></item>
/// <item><description>WithBrowserInteraction didn't exist on FigureBuilder despite being documented</description></item>
/// <item><description>WithTightMargins toggle wasn't exposed in the UI</description></item>
/// </list>
/// </summary>
public class PlaygroundExampleTests
{
    private static PlaygroundOptions Defaults() => new() { Title = "Test" };

    // ── Every example must build successfully ────────────────────────────────

    [Theory]
    [MemberData(nameof(AllExampleNames))]
    public void EveryExample_BuildsWithoutThrowing(string name)
    {
        var (figure, code) = PlaygroundExamples.Build(name, Defaults());
        Assert.NotNull(figure);
        Assert.False(string.IsNullOrWhiteSpace(code), "Code snippet must not be empty");
        // Sanity: figure must produce a valid SVG
        string svg = figure.ToSvg();
        Assert.StartsWith("<svg", svg);
        Assert.Contains("</svg>", svg);
    }

    public static IEnumerable<object[]> AllExampleNames() =>
        PlaygroundExamples.Names.Select(n => new object[] { n });

    // ── Browser interaction toggle ───────────────────────────────────────────

    [Theory]
    [MemberData(nameof(AllExampleNames))]
    public void BrowserInteraction_AddsScripts_WhenEnabled(string name)
    {
        // Without the toggle: SVG should not contain pan/zoom/tooltip JS.
        var (off, _) = PlaygroundExamples.Build(name, Defaults() with { BrowserInteraction = false });
        string svgOff = off.ToSvg();

        // With the toggle: SVG must contain the embedded interaction scripts.
        var (on, _) = PlaygroundExamples.Build(name, Defaults() with { BrowserInteraction = true });
        string svgOn = on.ToSvg();

        Assert.True(svgOn.Length > svgOff.Length,
            $"[{name}] enabling BrowserInteraction should grow the SVG with embedded JS — off={svgOff.Length}, on={svgOn.Length}");
        Assert.Contains("<script", svgOn);
    }

    // ── Grid toggle (the inverted-bug regression test) ───────────────────────

    [Fact]
    public void ShowGrid_TrueKeepsThemeDefaultGrid_FalseHidesIt()
    {
        var on  = PlaygroundExamples.Build("Line Chart", Defaults() with { ShowGrid = true }).Figure;
        var off = PlaygroundExamples.Build("Line Chart", Defaults() with { ShowGrid = false }).Figure;

        // The 'on' SVG must have visible grid markers (theme default is visible);
        // 'off' must not. We use the figure model rather than text matching for robustness.
        Assert.True(on.SubPlots[0].Grid.Visible || on.Theme.DefaultGrid.Visible,
            "ShowGrid=true should leave the theme's default grid visible");
        Assert.False(off.SubPlots[0].Grid.Visible,
            "ShowGrid=false must explicitly hide the grid");
    }

    // ── TightMargins toggle ──────────────────────────────────────────────────

    [Fact]
    public void TightMargins_RemovesAxisPadding_WhenEnabled()
    {
        var tight = PlaygroundExamples.Build("Line Chart", Defaults() with { TightMargins = true }).Figure;
        // WithTightMargins() forces both axes to Margin = 0
        Assert.Equal(0, tight.SubPlots[0].XAxis.Margin);
        Assert.Equal(0, tight.SubPlots[0].YAxis.Margin);
    }

    // ── Spine toggles ────────────────────────────────────────────────────────

    [Fact]
    public void HideTopSpine_HidesTopSpine_WhenEnabled()
    {
        var fig = PlaygroundExamples.Build("Line Chart", Defaults() with { HideTopSpine = true }).Figure;
        Assert.False(fig.SubPlots[0].Spines.Top.Visible);
    }

    [Fact]
    public void HideRightSpine_HidesRightSpine_WhenEnabled()
    {
        var fig = PlaygroundExamples.Build("Line Chart", Defaults() with { HideRightSpine = true }).Figure;
        Assert.False(fig.SubPlots[0].Spines.Right.Visible);
    }

    // ── Theme + size ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Dark")]
    [InlineData("Nord")]
    [InlineData("Dracula")]
    [InlineData("Cyberpunk")]
    public void Theme_AppliesCorrectly(string themeName)
    {
        // Community themes (Nord/Dracula/Cyberpunk) are derived from a base theme so their
        // .Name may be inherited. Verify by comparing the resolved Theme INSTANCE rather
        // than its display name — different theme names must yield different Theme instances.
        var opts = Defaults() with { ThemeName = themeName };
        var defaultFig = PlaygroundExamples.Build("Line Chart", Defaults() with { ThemeName = "Default" }).Figure;
        var fig = PlaygroundExamples.Build("Line Chart", opts).Figure;

        Assert.NotSame(defaultFig.Theme, fig.Theme);
        Assert.Same(opts.Theme, fig.Theme);
    }

    [Fact]
    public void Size_PropagatesToFigure()
    {
        // Phase L.5 dropped the Width/Height sliders from the playground — the SVG
        // is responsive and fills its container. The PlaygroundOptions record still
        // carries Width/Height for the code-snippet + intrinsic viewBox aspect;
        // verify the property still flows through end-to-end for programmatic callers.
        var fig = PlaygroundExamples.Build("Line Chart", Defaults() with { Width = 1100, Height = 700 }).Figure;
        Assert.Equal(1100, fig.Width);
        Assert.Equal(700, fig.Height);
    }

    [Fact]
    public void DefaultSize_Is_16By9_800x450()
    {
        // Phase L.5 — playground's default intrinsic size changed from 800×500 (8:5)
        // to 800×450 (16:9 widescreen) for a more modern default aspect. This is the
        // viewBox aspect the responsive SVG preserves as the container resizes.
        var fig = PlaygroundExamples.Build("Line Chart", Defaults()).Figure;
        Assert.Equal(800, fig.Width);
        Assert.Equal(450, fig.Height);
    }

    // ── Code snippet reflects toggles (so users can copy a working repro) ────

    [Fact]
    public void CodeSnippet_IncludesBrowserInteraction_WhenEnabled()
    {
        var (_, code) = PlaygroundExamples.Build("Line Chart", Defaults() with { BrowserInteraction = true });
        Assert.Contains("WithBrowserInteraction", code);
    }

    [Fact]
    public void CodeSnippet_IncludesTightLayout_WhenEnabled()
    {
        var (_, code) = PlaygroundExamples.Build("Line Chart", Defaults() with { TightLayout = true });
        Assert.Contains("TightLayout", code);
    }

    [Fact]
    public void CodeSnippet_IncludesShowGridFalse_WhenDisabled()
    {
        var (_, code) = PlaygroundExamples.Build("Line Chart", Defaults() with { ShowGrid = false });
        Assert.Contains("ShowGrid(false)", code);
    }

    // ── Registry sanity ──────────────────────────────────────────────────────

    [Fact]
    public void Names_Contains_SixteenExamples()
    {
        // Phase G.7 of v1.7.2 follow-on plan — "Sankey Flow" added (16th example)
        // to exercise the browser-side Sankey hover script end-to-end.
        Assert.Equal(16, PlaygroundExamples.Names.Count);
    }

    [Fact]
    public void SupportsLineControls_IsTrueOnlyForExamplesThatDrawALine()
    {
        // L.6 of Phase L — scatter plots have NO line, so line-style / line-width
        // controls make no visual difference on them. Exclude Scatter.
        Assert.True(PlaygroundExamples.SupportsLineControls("Line Chart"));
        Assert.True(PlaygroundExamples.SupportsLineControls("Multi-Series"));
        Assert.False(PlaygroundExamples.SupportsLineControls("Scatter Plot"));
        Assert.False(PlaygroundExamples.SupportsLineControls("Heatmap"));
        Assert.False(PlaygroundExamples.SupportsLineControls("Pie Chart"));
    }

    [Fact]
    public void SupportsMarkerControls_IsTrueForLineScatterAndMulti()
    {
        // L.6 of Phase L — the playground's Marker / Marker-size controls apply to
        // any example whose primary series exposes a MarkerStyle: Scatter (always-on)
        // and the line families (optional per-point markers).
        Assert.True(PlaygroundExamples.SupportsMarkerControls("Line Chart"));
        Assert.True(PlaygroundExamples.SupportsMarkerControls("Scatter Plot"));
        Assert.True(PlaygroundExamples.SupportsMarkerControls("Multi-Series"));
        Assert.False(PlaygroundExamples.SupportsMarkerControls("Heatmap"));
        Assert.False(PlaygroundExamples.SupportsMarkerControls("Pie Chart"));
    }

    [Fact]
    public void Scatter_AppliesMarkerStyleFromOptions()
    {
        var opts = Defaults() with { MarkerStyle = "Square" };
        var figure = PlaygroundExamples.Build("Scatter Plot", opts).Figure;
        var series = (MatPlotLibNet.Models.Series.ScatterSeries)figure.SubPlots[0].Series[0];
        Assert.Equal(MatPlotLibNet.Styling.MarkerStyle.Square, series.Marker);
    }

    [Fact]
    public void Scatter_AppliesMarkerSizeFromOptions()
    {
        var opts = Defaults() with { MarkerStyle = "Circle", MarkerSize = 14 };
        var figure = PlaygroundExamples.Build("Scatter Plot", opts).Figure;
        var series = (MatPlotLibNet.Models.Series.ScatterSeries)figure.SubPlots[0].Series[0];
        Assert.Equal(14, series.MarkerSize);
    }

    [Fact]
    public void SupportsColormap_IsTrueOnlyForGridFamilies()
    {
        Assert.True(PlaygroundExamples.SupportsColormap("Heatmap"));
        Assert.True(PlaygroundExamples.SupportsColormap("Contour Plot"));
        Assert.False(PlaygroundExamples.SupportsColormap("Line Chart"));
    }

    [Fact]
    public void Build_UnknownExample_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            PlaygroundExamples.Build("Nonexistent", Defaults()));
    }
}
