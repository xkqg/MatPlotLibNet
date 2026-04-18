// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Playground;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

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
///
/// <para>Phase N.1 of v1.7.2 — every example identifier is now the typed
/// <see cref="PlaygroundExample"/> enum, not a magic string.</para>
/// </summary>
public class PlaygroundExampleTests
{
    private static PlaygroundOptions Defaults() => new() { Title = "Test" };

    // ── Every example must build successfully ────────────────────────────────

    [Theory]
    [MemberData(nameof(AllExamples))]
    public void EveryExample_BuildsWithoutThrowing(PlaygroundExample example)
    {
        var (figure, code) = PlaygroundExamples.Build(example, Defaults());
        Assert.NotNull(figure);
        Assert.False(string.IsNullOrWhiteSpace(code), "Code snippet must not be empty");
        // Sanity: figure must produce a valid SVG
        string svg = figure.ToSvg();
        Assert.StartsWith("<svg", svg);
        Assert.Contains("</svg>", svg);
    }

    public static IEnumerable<object[]> AllExamples() =>
        Enum.GetValues<PlaygroundExample>().Select(e => new object[] { e });

    // ── Browser interaction toggle ───────────────────────────────────────────

    [Theory]
    [MemberData(nameof(AllExamples))]
    public void BrowserInteraction_AddsScripts_WhenEnabled(PlaygroundExample example)
    {
        // Without the toggle: SVG should not contain pan/zoom/tooltip JS.
        var (off, _) = PlaygroundExamples.Build(example, Defaults() with { BrowserInteraction = false });
        string svgOff = off.ToSvg();

        // With the toggle: SVG must contain the embedded interaction scripts.
        var (on, _) = PlaygroundExamples.Build(example, Defaults() with { BrowserInteraction = true });
        string svgOn = on.ToSvg();

        Assert.True(svgOn.Length > svgOff.Length,
            $"[{example}] enabling BrowserInteraction should grow the SVG with embedded JS — off={svgOff.Length}, on={svgOn.Length}");
        Assert.Contains("<script", svgOn);
    }

    // ── Grid toggle (the inverted-bug regression test) ───────────────────────

    [Fact]
    public void ShowGrid_TrueKeepsThemeDefaultGrid_FalseHidesIt()
    {
        var on  = PlaygroundExamples.Build(PlaygroundExample.LineChart, Defaults() with { ShowGrid = true }).Figure;
        var off = PlaygroundExamples.Build(PlaygroundExample.LineChart, Defaults() with { ShowGrid = false }).Figure;

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
        var tight = PlaygroundExamples.Build(PlaygroundExample.LineChart, Defaults() with { TightMargins = true }).Figure;
        // WithTightMargins() forces both axes to Margin = 0
        Assert.Equal(0, tight.SubPlots[0].XAxis.Margin);
        Assert.Equal(0, tight.SubPlots[0].YAxis.Margin);
    }

    // ── Spine toggles ────────────────────────────────────────────────────────

    [Fact]
    public void HideTopSpine_HidesTopSpine_WhenEnabled()
    {
        var fig = PlaygroundExamples.Build(PlaygroundExample.LineChart, Defaults() with { HideTopSpine = true }).Figure;
        Assert.False(fig.SubPlots[0].Spines.Top.Visible);
    }

    [Fact]
    public void HideRightSpine_HidesRightSpine_WhenEnabled()
    {
        var fig = PlaygroundExamples.Build(PlaygroundExample.LineChart, Defaults() with { HideRightSpine = true }).Figure;
        Assert.False(fig.SubPlots[0].Spines.Right.Visible);
    }

    // ── Theme + size ─────────────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(DistinctThemes))]
    public void Theme_AppliesCorrectly(Theme theme)
    {
        // Community themes (Nord/Dracula/Cyberpunk) are derived from a base theme so their
        // .Name may be inherited. Verify by comparing the resolved Theme INSTANCE rather
        // than its display name — different theme instances yield different figures.
        var opts = Defaults() with { Theme = theme };
        var defaultFig = PlaygroundExamples.Build(PlaygroundExample.LineChart, Defaults() with { Theme = Theme.Default }).Figure;
        var fig = PlaygroundExamples.Build(PlaygroundExample.LineChart, opts).Figure;

        Assert.NotSame(defaultFig.Theme, fig.Theme);
        Assert.Same(opts.Theme, fig.Theme);
    }

    public static IEnumerable<object[]> DistinctThemes() => new[]
    {
        new object[] { Theme.Dark },
        new object[] { Theme.Nord },
        new object[] { Theme.Dracula },
        new object[] { Theme.Cyberpunk },
    };

    [Fact]
    public void Size_PropagatesToFigure()
    {
        // Phase L.5 dropped the Width/Height sliders from the playground — the SVG
        // is responsive and fills its container. The PlaygroundOptions record still
        // carries Width/Height for the code-snippet + intrinsic viewBox aspect;
        // verify the property still flows through end-to-end for programmatic callers.
        var fig = PlaygroundExamples.Build(PlaygroundExample.LineChart, Defaults() with { Width = 1100, Height = 700 }).Figure;
        Assert.Equal(1100, fig.Width);
        Assert.Equal(700, fig.Height);
    }

    [Fact]
    public void DefaultSize_Is_16By9_800x450()
    {
        // Phase L.5 — playground's default intrinsic size changed from 800×500 (8:5)
        // to 800×450 (16:9 widescreen) for a more modern default aspect. This is the
        // viewBox aspect the responsive SVG preserves as the container resizes.
        var fig = PlaygroundExamples.Build(PlaygroundExample.LineChart, Defaults()).Figure;
        Assert.Equal(800, fig.Width);
        Assert.Equal(450, fig.Height);
    }

    // ── Code snippet reflects toggles (so users can copy a working repro) ────

    [Fact]
    public void CodeSnippet_IncludesBrowserInteraction_WhenEnabled()
    {
        var (_, code) = PlaygroundExamples.Build(PlaygroundExample.LineChart, Defaults() with { BrowserInteraction = true });
        Assert.Contains("WithBrowserInteraction", code);
    }

    [Fact]
    public void CodeSnippet_IncludesTightLayout_WhenEnabled()
    {
        var (_, code) = PlaygroundExamples.Build(PlaygroundExample.LineChart, Defaults() with { TightLayout = true });
        Assert.Contains("TightLayout", code);
    }

    [Fact]
    public void CodeSnippet_IncludesShowGridFalse_WhenDisabled()
    {
        var (_, code) = PlaygroundExamples.Build(PlaygroundExample.LineChart, Defaults() with { ShowGrid = false });
        Assert.Contains("ShowGrid(false)", code);
    }

    // ── Registry sanity ──────────────────────────────────────────────────────

    [Fact]
    public void Examples_Contains_SixteenMembers()
    {
        // Phase G.7 of v1.7.2 follow-on plan — "Sankey Flow" added (16th example)
        // to exercise the browser-side Sankey hover script end-to-end.
        // Phase N.1 — Examples is now an IReadOnlyList<PlaygroundExample> backed
        // by Enum.GetValues<PlaygroundExample>().
        Assert.Equal(16, PlaygroundExamples.Examples.Count);
    }

    [Fact]
    public void SupportsLineControls_IsTrueOnlyForExamplesThatDrawALine()
    {
        // L.6 of Phase L — scatter plots have NO line, so line-style / line-width
        // controls make no visual difference on them. Exclude Scatter.
        Assert.True(PlaygroundExamples.SupportsLineControls(PlaygroundExample.LineChart));
        Assert.True(PlaygroundExamples.SupportsLineControls(PlaygroundExample.MultiSeries));
        Assert.False(PlaygroundExamples.SupportsLineControls(PlaygroundExample.ScatterPlot));
        Assert.False(PlaygroundExamples.SupportsLineControls(PlaygroundExample.Heatmap));
        Assert.False(PlaygroundExamples.SupportsLineControls(PlaygroundExample.PieChart));
    }

    [Fact]
    public void SupportsMarkerControls_IsTrueForLineScatterAndMulti()
    {
        // L.6 of Phase L — the playground's Marker / Marker-size controls apply to
        // any example whose primary series exposes a MarkerStyle: Scatter (always-on)
        // and the line families (optional per-point markers).
        Assert.True(PlaygroundExamples.SupportsMarkerControls(PlaygroundExample.LineChart));
        Assert.True(PlaygroundExamples.SupportsMarkerControls(PlaygroundExample.ScatterPlot));
        Assert.True(PlaygroundExamples.SupportsMarkerControls(PlaygroundExample.MultiSeries));
        Assert.False(PlaygroundExamples.SupportsMarkerControls(PlaygroundExample.Heatmap));
        Assert.False(PlaygroundExamples.SupportsMarkerControls(PlaygroundExample.PieChart));
    }

    [Fact]
    public void Scatter_AppliesMarkerStyleFromOptions()
    {
        var opts = Defaults() with { Marker = MarkerStyle.Square };
        var figure = PlaygroundExamples.Build(PlaygroundExample.ScatterPlot, opts).Figure;
        var series = (MatPlotLibNet.Models.Series.ScatterSeries)figure.SubPlots[0].Series[0];
        Assert.Equal(MarkerStyle.Square, series.Marker);
    }

    [Fact]
    public void Scatter_AppliesMarkerSizeFromOptions()
    {
        var opts = Defaults() with { Marker = MarkerStyle.Circle, MarkerSize = 14 };
        var figure = PlaygroundExamples.Build(PlaygroundExample.ScatterPlot, opts).Figure;
        var series = (MatPlotLibNet.Models.Series.ScatterSeries)figure.SubPlots[0].Series[0];
        Assert.Equal(14, series.MarkerSize);
    }

    [Fact]
    public void SupportsColormap_IsTrueOnlyForGridFamilies()
    {
        Assert.True(PlaygroundExamples.SupportsColormap(PlaygroundExample.Heatmap));
        Assert.True(PlaygroundExamples.SupportsColormap(PlaygroundExample.ContourPlot));
        Assert.False(PlaygroundExamples.SupportsColormap(PlaygroundExample.LineChart));
    }

    [Fact]
    public void Build_UnknownEnumValue_Throws()
    {
        // An invalid cast forces an enum value outside the defined range — must
        // fail at the dispatcher rather than silently returning a default figure.
        Assert.Throws<ArgumentException>(() =>
            PlaygroundExamples.Build((PlaygroundExample)999, Defaults()));
    }
}
