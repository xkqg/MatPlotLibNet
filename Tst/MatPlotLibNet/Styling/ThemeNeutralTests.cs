// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>The grey that reference graphics wear over the plot (reference lines, trendlines, levels, Fibonacci retracements, a
/// waterfall's connectors, a stem's baseline, the lines of a contour without a colour map, a dumbbell's connector, a map's
/// outlines) belongs to the theme, which knows its plot: it stands at the 3 : 1 contrast WCAG asks of a graphic against it.
/// Before, every one of them wore #808080, which reaches 3 : 1 on 25 of the 30 shipped themes and stands at 2.32 to 2.94 on
/// Nord, Dracula, Monokai, Gruvbox and OpsPanel.</summary>
public class ThemeNeutralTests
{
    public static TheoryData<string> BuiltInThemes { get; } =
        new(typeof(Theme).GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(p => p.PropertyType == typeof(Theme))
            .Select(p => p.Name));

    private static Theme BuiltIn(string name) =>
        (Theme)typeof(Theme).GetProperty(name, BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;

    /// <summary>The five themes whose plot cannot carry #808080 at 3 : 1, with the grey the theme derives instead: the nearest
    /// grey to #808080 that reaches it. The other 25 keep #808080.</summary>
    private static readonly Dictionary<string, string> Derived = new()
    {
        ["Nord"] = "#8D8D8D", ["Dracula"] = "#949494", ["Monokai"] = "#868686", ["Gruvbox"] = "#828282", ["OpsPanel"] = "#787878",
    };

    /// <summary>What the viewer sees behind a graphic: the axes background, laid over the figure's when it is translucent.</summary>
    private static Color PlotOf(Theme theme) => theme.AxesBackground.CompositedOver(theme.Background);

    // ── the neutral of every shipped theme ───────────────────────────────────────────────────────────

    /// <summary>Measured before the change: #808080 stood at 2.55 (Nord), 2.32 (Dracula), 2.77 (Monokai), 2.94 (Gruvbox) and
    /// 2.70 (OpsPanel) : 1 against the plot.</summary>
    [Theory]
    [MemberData(nameof(BuiltInThemes))]
    public void TheNeutral_OfEveryShippedTheme_StandsAtThreeToOneAgainstItsPlot(string name)
    {
        var theme = BuiltIn(name);

        Assert.True(theme.Neutral.ContrastRatioWith(PlotOf(theme)) >= 3.0,
            $"{name}: the neutral {theme.Neutral.ToHex()} stands at {theme.Neutral.ContrastRatioWith(PlotOf(theme)):F2} : 1 against {PlotOf(theme).ToHex()}");
    }

    [Theory]
    [MemberData(nameof(BuiltInThemes))]
    public void TheNeutral_OfEveryShippedTheme_IsThePinnedGrey(string name) =>
        Assert.Equal(Derived.GetValueOrDefault(name, "#808080"), BuiltIn(name).Neutral.ToHex());

    [Fact]
    public void TheNeutral_OfAThemeThatNamesOne_IsThatColour_WhateverItsPlotIs()
    {
        var theme = Theme.CreateFrom(Theme.Nord).WithNeutralColor(Color.FromHex("#112233")).Build();

        Assert.Equal(Color.FromHex("#112233"), theme.NeutralColor);
        Assert.Equal(Color.FromHex("#112233"), theme.Neutral);
    }

    // ── derived from the final plot ──────────────────────────────────────────────────────────────────

    /// <summary>The derivation reads the theme's colours when it is asked, not when the theme was derived from another: a
    /// dark theme given a white plot is a light theme, and its neutral is the one a light plot takes.</summary>
    [Fact]
    public void TheNeutral_OfADerivedTheme_FollowsThePlotItEndsWith()
    {
        var onDark = Theme.CreateFrom(Theme.Nord).Build();
        var onWhite = Theme.CreateFrom(Theme.Nord).WithAxesBackground(Colors.White).Build();

        Assert.Equal(Color.FromHex("#8D8D8D"), onDark.Neutral);
        Assert.Equal(Color.FromHex("#808080"), onWhite.Neutral);
        Assert.Null(onWhite.NeutralColor);
    }

    [Fact]
    public void ANamedNeutral_DoesNotDependOnTheOrderTheBuilderIsTold()
    {
        var named = Color.FromHex("#334455");

        var before = Theme.CreateFrom(Theme.Default).WithNeutralColor(named).WithAxesBackground(Colors.Black).Build();
        var after = Theme.CreateFrom(Theme.Default).WithAxesBackground(Colors.Black).WithNeutralColor(named).Build();

        Assert.Equal(named, before.Neutral);
        Assert.Equal(before.Neutral, after.Neutral);
    }

    /// <summary>A translucent plot is seen over the figure's background, so the grey is chosen against what is seen: a clear
    /// plot over Nord's dark plot colour gets Nord's grey, over white the library's.</summary>
    [Fact]
    public void ATranslucentPlot_IsReadOverTheFiguresBackground()
    {
        var overNord = Theme.CreateFrom(Theme.Default).WithBackground(Theme.Nord.AxesBackground).WithAxesBackground(Colors.White.WithAlpha(0)).Build();
        var overWhite = Theme.CreateFrom(Theme.Default).WithBackground(Colors.White).WithAxesBackground(Colors.White.WithAlpha(0)).Build();

        Assert.Equal(Theme.Nord.Neutral, overNord.Neutral);
        Assert.Equal(Color.FromHex("#808080"), overWhite.Neutral);
    }

    [Fact]
    public void ACopyOfATheme_KeepsTheNeutralItNames()
    {
        var named = Theme.CreateFrom(Theme.Dark).WithNeutralColor(Color.FromHex("#445566")).Build();

        var copy = Theme.CreateFrom(named).WithName("copy").Build();

        Assert.Equal(Color.FromHex("#445566"), copy.NeutralColor);
    }

    // ── on the wire ──────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ATheme_ThatNamesNoNeutral_WritesNone()
    {
        string json = new ChartSerializer().ToJson(Plt.Create().WithTheme(Theme.Nord).AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2], [1.0, 2])).Build());

        Assert.DoesNotContain("neutralColor", json);
    }

    [Fact]
    public void ANamedNeutral_IsWrittenAndReadBack()
    {
        var theme = Theme.CreateFrom(Theme.Nord).WithNeutralColor(Color.FromHex("#112233")).Build();
        var serializer = new ChartSerializer();
        var figure = Plt.Create().WithTheme(theme).AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2], [1.0, 2])).Build();

        string json = serializer.ToJson(figure);
        var read = serializer.FromJson(json);

        using var document = JsonDocument.Parse(json);
        Assert.Equal("#112233", document.RootElement.GetProperty("theme").GetProperty("neutralColor").GetString()!.ToUpperInvariant());
        Assert.Equal(Color.FromHex("#112233"), read.Theme.NeutralColor);
        Assert.Equal(figure.ToSvg(), read.ToSvg());
    }

    /// <summary>A figure written before the theme named a neutral reads back on the theme that derives it, so the grey it
    /// draws is the one the theme gives its plot.</summary>
    [Fact]
    public void AJsonWrittenBeforeTheNeutral_ReadsBackOnAThemeThatDerivesIt()
    {
        const string older = """{"width":400,"height":300,"theme":{"name":"nord","axesBackground":"#2E3440","background":"#2E3440"},"subPlots":[]}""";

        var theme = new ChartSerializer().FromJson(older).Theme;

        Assert.Null(theme.NeutralColor);
        Assert.True(theme.Neutral.ContrastRatioWith(PlotOf(theme)) >= 3.0);
    }
}
