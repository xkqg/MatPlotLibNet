// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Packaging.OwnedRules;

/// <summary>A theme preset is a base theme and what differs from it, said once: declared as the theme's own property, built by the
/// one fluent builder, its grounds one <c>ThemeGround</c>; a theme's settings are copied in one place and reach the JSON only
/// through each look's own keys.</summary>
internal static class ThemeRules
{
    /// <summary>The rules of the area, in the order the scan reads them.</summary>
    internal static OwnedRule[] All { get; } =
    [
        OwnedRule.Nowhere("a look's values reach the theme's record of the figure JSON only through the look's own pair of keys (IThemeKeys: WriteKeys and ReadKeys, composed by ThemeLooks): no wire form reads a member of a look or makes a look itself",
            @"\blooks?\.\w|(?<![\w.])Looks\.\w|\b\w+Look\.\w|\bnew\s+\w+Look\b",
            under: "Src/MatPlotLibNet/Serialization/") with { Finds = [
            "            EdgeColor = looks.Legend.EdgeColor.ToHex(),"] },
        new("a theme's own settings are copied in one place, Theme's copy constructor, and turned into the rc parameters a theme carries and back in one place, ThemeRcKey: no style sheet or parameter set reads a setting off a theme into a key of its own",
            @"\b(?:theme|baseTheme|other|_base|_theme)\.(?:Background|AxesBackground|ForegroundText|DefaultFont|DefaultGrid|Looks\.Lines|CycleColors|PropCycler|Pane3DColor|NeutralColor|Alarm|ViolinBodyColor|ViolinStatsColor|AxisXMargin|AxisYMargin|DefaultLegendPosition|DefaultSpacing)\b",
            "Src/MatPlotLibNet/Styling/Theme.cs", "Src/MatPlotLibNet/Styling/ThemeRcKey.cs", "Src/MatPlotLibNet/Styling/ThemeRcParts.cs") { Under = "Src/MatPlotLibNet/Styling/", Finds = [
            "        [RcParamKeys.FigureFaceColor]     = theme.Background,"] },
        OwnedRule.Nowhere("a theme builder holds the theme it is building, one Theme, and each of its verbs makes the next one from it: no field per setting of a theme beside it, copied in and copied out again",
            @"\bprivate\s+(?:readonly\s+)?(?:Color\??|Color\[\]|Font|GridStyle|PropCycler\??|AlarmPalette)\s+_\w+\s*;",
            under: "Src/MatPlotLibNet/Builders/ThemeBuilder.cs") with { Finds = [
            "    private Color _background;"] },
        OwnedRule.Nowhere("a preset's grounds (the figure, the plot and the ink) change as one ThemeGround through WithGround: no preset sets the figure's or the plot's background by itself",
            @"\.With(?:Background|AxesBackground)\(",
            under: "Src/MatPlotLibNet/") with { Finds = [
            "        .WithBackground(Color.FromHex(\"#191C1E\"))"] },
        OwnedRule.Nowhere("a preset is declared once, as Theme's own property with its value: no second list of presets forwards to it",
            @"\bCommunityThemes\b|\bThemePresets\.",
            under: "Src/") with { Finds = [
            "    public static Theme Solarize { get; } = ThemePresets.CommunityThemes.Solarize();"] },
        OwnedRule.Nowhere("a verb of the theme builder makes the next theme through Then: no verb assigns the theme and returns the builder itself",
            @"_theme\s*=\s*new\(\s*_theme\.Name",
            under: "Src/MatPlotLibNet/Builders/ThemeBuilder.cs") with { Finds = [
            "        _theme = new(_theme.Name, _theme) { Background = color };"] },
    ];
}
