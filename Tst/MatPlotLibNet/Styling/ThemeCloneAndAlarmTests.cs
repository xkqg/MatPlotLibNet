// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>Pins two things a control-room wall depends on: a derived theme keeps everything it did not
/// change, and every theme agrees on what an alarm colour means.</summary>
public class ThemeCloneAndAlarmTests
{
    /// <summary>Deriving a theme preserves every property the caller did not touch.
    /// <para>This is the regression guard for a real defect: <c>Build()</c> constructed a fresh theme from
    /// eight of fifteen properties, so <c>Theme.CreateFrom(Theme.Dark).WithBackground(x).Build()</c> came back having
    /// silently discarded the dark theme's spacing, patch-edge colour, violin colours and axis margins. A
    /// hand-maintained argument list drifts the moment someone adds a property; a clone cannot.</para></summary>
    [Fact]
    public void DerivingATheme_PreservesEveryUntouchedProperty()
    {
        var baseTheme = Theme.Dark;

        var derived = Theme.CreateFrom(baseTheme)
            .WithBackground(Colors.Black)
            .Build();

        Assert.Equal(Colors.Black, derived.Background);            // the one thing that was changed
        Assert.Equivalent(new
        {
            baseTheme.DefaultSpacing, baseTheme.PatchEdgeColor, baseTheme.ViolinBodyColor, baseTheme.ViolinStatsColor,
            baseTheme.AxisXMargin, baseTheme.AxisYMargin, baseTheme.Pane3DColor, baseTheme.Alarm,
        }, derived);
    }

    public static TheoryData<Func<Theme>, Func<Theme, object?>, object> WhatADerivedThemeHolds => new()
    {
        { () => Theme.CreateFrom(Theme.Dark).WithName("ops-night").Build(), theme => theme.Name, "ops-night" },
        { () => Theme.CreateFrom(Theme.MatplotlibClassic).WithBackground(Colors.Black).Build(), theme => theme.DefaultLegendPosition, LegendPosition.UpperRight },
    };

    [Theory]
    [MemberData(nameof(WhatADerivedThemeHolds))]
    public void ADerivedTheme_CanBeNamed_AndKeepsTheLegendPositionOfItsBase(Func<Theme> derive, Func<Theme, object?> read, object expected) =>
        Assert.Equal(expected, read(derive()));

    /// <summary>Every theme the library hands out by a property has a name of its own: the sixteen community presets were built by
    /// <c>CreateFrom</c> and answered to "custom-dark" or "custom-default", so a style sheet or a figure's JSON could not tell
    /// Nord from Dracula.</summary>
    [Fact]
    public void EveryThemeTheLibraryHandsOut_HasANameOfItsOwn()
    {
        var themes = typeof(Theme).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(property => property.PropertyType == typeof(Theme))
            .Select(property => (Property: property.Name, Name: ((Theme)property.GetValue(null)!).Name))
            .ToArray();

        Assert.True(themes.Length >= 30, $"only {themes.Length} themes found");
        Assert.Empty(themes.Where(theme => theme.Name.StartsWith("custom-", StringComparison.Ordinal)).Select(theme => $"{theme.Property} = {theme.Name}"));
        Assert.Empty(themes.GroupBy(theme => theme.Name).Where(group => group.Count() > 1).Select(group => $"{group.Key}: {string.Join(", ", group.Select(theme => theme.Property))}"));
    }

    /// <summary>Every theme carries an alarm palette, and no theme paints the resting state in an alarm colour.
    /// This is the invariant the whole wall rests on: colour is scarce, so the normal state spends none of it.</summary>
    [Theory]
    [MemberData(nameof(OpsThemes))]
    public void NoTheme_PaintsTheRestingStateInAnAlarmColour(string name, Theme theme)
    {
        Assert.NotEqual(theme.Alarm.Warning, theme.Alarm.Resting);
        Assert.NotEqual(theme.Alarm.Critical, theme.Alarm.Resting);
        Assert.Equal(name, theme.Name);
    }

    /// <summary>The ground is the operator's to choose; the alarm hues are not. Across all four operator
    /// backgrounds the warning and critical colours keep the SAME hue — only their luminance shifts, so they
    /// stay equally loud on a dark wall and on a bright panel. A palette whose meaning drifts per theme is
    /// worse than no palette at all.</summary>
    [Theory]
    [MemberData(nameof(OpsThemes))]
    public void AcrossEveryOperatorBackground_TheAlarmHuesKeepTheirMeaning(string name, Theme theme)
    {
        Assert.True(IsAmber(theme.Alarm.Warning), $"{name}: warning is not an amber");
        Assert.True(IsVermillion(theme.Alarm.Critical), $"{name}: critical is not a vermillion");
    }

    public static TheoryData<string, Theme> OpsThemes => new()
    {
        { "ops-night", Theme.OpsNight },
        { "ops-panel", Theme.OpsPanel },
        { "ops-warm", Theme.OpsWarm },
        { "ops-contrast", Theme.OpsContrast }
    };

    // Hue, not exact value: an amber is red-dominant with substantial green and little blue; a vermillion is
    // red-dominant with distinctly less green. Both stay separable under red-green colour deficiency, which is
    // why the Okabe-Ito pair was chosen in the first place.
    private static bool IsAmber(Color c) => c.R > c.G && c.G > c.B && c.G >= c.R * 0.55;
    private static bool IsVermillion(Color c) => c.R > c.G && c.G > c.B && c.G < c.R * 0.55;
}
