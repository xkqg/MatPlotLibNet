// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Packaging.OwnedRules;

/// <summary>The rules of a colour: its bytes and its opacity, the colours the library names, the palettes and the cycle, a colour
/// map, a normaliser and a colour bar.</summary>
internal static class ColourRules
{
    /// <summary>The rules of the area, in the order the scan reads them.</summary>
    internal static OwnedRule[] All { get; } =
    [
        new("a colour channel or an opacity becomes a byte by the byte block of ColorExtensions: Scaled and ScaledTruncating (a share of it) and Toward (the step a share of the way to another), which WithOpacity, Color.Lerp and CompositedOver read: the cast, its rounding and its hold are written once",
            @"\(byte\)\s*(?:Math\.(?:Round|Clamp)\()?[^;]*?(?:\bchannel\s*\*|\.(?:R|G|B|A|Alpha)\s*[*+\-]|\*\s*255\b|\b255\s*\*)|\(byte\)\s*Math\.Round\(",
            "Src/MatPlotLibNet/Styling/ColorExtensions.cs") { Finds = [
            "        byte a = (byte)Math.Round(c.A * _opacity);"] },
        new("a colour takes an opacity through WithOpacityGiven, which keeps a colour of none none as matplotlib's to_rgba('none', alpha) does: no site lays an opacity over a colour with WithOpacity, which would fill a series of none in black (a surface's opacity scope, ctx.WithOpacity, is another rule)",
            @"(?<!\b(?:[cC]tx|context))\.WithOpacity\(",
            "Src/MatPlotLibNet/Styling/ColorExtensions.cs") { Under = "Src/", Finds = [
            "            alpha is { } a && !color.IsNone ? color.WithOpacity(a) : color;"] },
        new("how opaque a series is drawn under a theme is series.OpacityUnder(theme): the number a theme names for a kind is read where the kind says it (ThemeAlphaOf), by nobody else",
            @"\b[lL]ooks\.(?:Patch|Dots)\.Alpha\b",
            "Src/MatPlotLibNet/Styling/IGivenAlpha.cs", "Src/MatPlotLibNet/Styling/IDotSeries.cs", "Src/MatPlotLibNet/Styling/ISurfaceAlpha.cs") { Finds = [
            "    double? ThemeAlphaOf(ThemeLooks looks) => looks.Patch.LikeCollections ? looks.Patch.Alpha ?? PatchLook.RestingAlpha : null;"] },
        new("an opacity applied only below full is WithOpacityBelowFull, of a colour (the resting 1 keeps the colour's own alpha) or of a surface (the resting 1 tells it nothing): no site types the guard",
            @"\b\w*(?:[aA]lpha|[oO]pacity)\s*(?:<|>=)\s*1(?:\.0)?\s*\?",
            "Src/MatPlotLibNet/Styling/ColorExtensions.cs", "Src/MatPlotLibNet/Rendering/RenderContextScopeExtensions.cs") { Under = "Src/", Finds = [
            "        alpha >= 1.0 ? color : color.WithAlpha((byte)Math.Round(color.A * Math.Clamp(alpha, 0.0, 1.0)));"] },
        OwnedRule.Nowhere("the 148 CSS4 names are written once, as the properties of Css4Colors: each registers itself under its own name, so the table Color.FromName reads is not a second list, and a name CSS4 spells two ways reads its twin",
            @"^\s*\[""[a-z]+""\]\s*=|=>\s*new\(\s*0x[0-9A-Fa-f]{2}\s*,\s*0x[0-9A-Fa-f]{2}\s*,\s*0x[0-9A-Fa-f]{2}\s*\)\s*;", under: "Src/MatPlotLibNet/Styling/Css4Colors.cs") with { Finds = [
            "    public static Color Pink             => new(0xFF, 0xC0, 0xCB);"] },
        new("a colour the library names (black, white, none, a CSS4 name) is read by its name, and the black at 80 of a drop shadow or of a 3-D face's edge by its owner (Colors.DropShadow, PatchLook.RestingSurfaceEdgeColor): no site types its bytes",
            @"\bnew Color\((?:\s*(?:0|255)\s*,){2}\s*(?:0|255)\s*(?:,\s*\d+\s*)?\)|\bnew\(\s*30\s*,\s*144\s*,\s*255\b|\bnew\(\s*0\s*,\s*0\s*,\s*0\s*,\s*80\s*\)|\bWithAlpha\(\s*80\s*\)",
            "Src/MatPlotLibNet/Styling/Colors.cs", "Src/MatPlotLibNet/Styling/Looks/PatchLook.cs") { Under = "Src/", Finds = [
            "        var color = shape.Fill ?? shape.Stroke ?? new Color(0, 0, 0);"] },
        OwnedRule.Nowhere("matplotlib's legend edge '0.8' is LegendLook.RestingEdgeColor: the look's default and a theme that draws it pass the name, never the grey again",
            @"\bedgeColor:\s*Color\.FromHex\(|\bEdgeColor\s*\{\s*get;\s*init;\s*\}\s*=\s*Color\.FromHex\(", under: "Src/MatPlotLibNet/Styling/") with { Finds = [
            "    public Color EdgeColor { get; init; } = Color.FromHex(\"#cccccc\");"] },
        OwnedRule.Nowhere("the colours a table draws where none was given are the series' own (DrawnHeaderColor, DrawnCellColor, DrawnBorderColor), beside the settings: the renderer reads them and supplies no colour of its own",
            @"\.(?:HeaderColor|CellColor|BorderColor)\s*\?\?", under: "Src/MatPlotLibNet/Rendering/") with { Finds = [
            "        var headerColor = series.HeaderColor ?? new Color(200, 200, 200);"] },
        new("a share becomes a byte by cutting its fraction off (ScaledTruncating) only where matplotlib writes a colour map's or an image's bytes ((x * 255).astype(uint8)): an image sample, a blend of an image; an opacity, a fade and a shade take the nearest step",
            @"\bScaledTruncating\(",
            "Src/MatPlotLibNet/Styling/ColorExtensions.cs", "Src/MatPlotLibNet/Rendering/Interpolation/ColourSample.cs", "Src/MatPlotLibNet/Styling/BlendMode.cs") { Under = "Src/", Finds = [
            "        internal byte ScaledTruncating(double share) => (byte)(channel * share.Saturate());"] },
        new("a colour's three channels are scaled together by Modulate, the one shading of a colour that every light reads (LightSourceExtensions): no site scales R, G and B itself",
            @"\.R\.Scaled(?:Truncating)?\(|\.Modulate\(\s*(?:\w+\.)?ComputeIntensity\(",
            "Src/MatPlotLibNet/Styling/ColorExtensions.cs", "Src/MatPlotLibNet/Rendering/Lighting/LightSourceExtensions.cs") { Under = "Src/", Finds = [
            "    Color Shade(Color color, double nx, double ny, double nz) => color.Modulate(ComputeIntensity(nx, ny, nz));"] },
        new("a byte is read as a share of the full channel by byte.Share, the other way of Scaled: no site divides a channel by 255 (Luminance keeps the published Rec. 709 sum over the encoded channels, in the owner's file)",
            @"/\s*255(?:\.0|d)\b",
            "Src/MatPlotLibNet/Styling/ColorExtensions.cs") { Under = "Src/", Finds = [
            "        double sr = src.R / 255.0, sg = src.G / 255.0, sb = src.B / 255.0;"] },
        new("a colour of none (matplotlib's 'none', an opacity of 0) is Color.IsNone: no site tests the alpha byte against 0",
            @"\.A\s*(?:==|!=|>)\s*0\b|\{\s*A:\s*>\s*0\s*\}",
            "Src/MatPlotLibNet/Styling/ColorExtensions.cs") { Under = "Src/", Finds = [
            "            if (skipTransparentCells && color.A == 0) continue;"] },
        new("where a value stands in a scale, from 0 to 1, is LinearNormalizer.Normalize: (value - min) / range held to the unit interval is not typed again",
            @"\(\(?[\w.]+\s*-\s*[\w.]+\)\s*/\s*[\w.]+\)\.Saturate\(\)",
            "Src/MatPlotLibNet/Styling/ColorMaps/INormalizer.cs") { Finds = [
            "        return ((value - min) / range).Saturate();"] },
        new("the middle of a band between two levels where a normaliser names no other is INormalizerRule.LinearMiddleOf: the arithmetic mean is typed once, the default of every normaliser and the fallback of the log one",
            @"\(\s*high\s*-\s*low\s*\)\s*\*\s*0\.5|\(\s*low\s*\+\s*high\s*\)\s*(?:/\s*2|\*\s*0\.5)",
            "Src/MatPlotLibNet/Styling/ColorMaps/INormalizer.cs") { Under = "Src/MatPlotLibNet/Styling/ColorMaps/", Finds = [
            "    static double LinearMiddleOf(double low, double high) => low + (high - low) * 0.5;"] },
        new("the body of a colour bar (its strip, the ticks along it and the marks on them) is drawn by ColorBarRenderer.DrawBody for both orientations: an orientation places only its labels",
            @"\bColorBarStrip\.Of\(|\bScaleAlong\(\s*strip\.AxisLength",
            "Src/MatPlotLibNet/Rendering/ColorBarRendering/ColorBarRenderer.cs") { Under = "Src/MatPlotLibNet/Rendering/", Finds = [
            "        var strip = ColorBarStrip.Of(Cb, body, vertical, Steps, Theme, Lines);"] },
        new("the colour a series is drawn in is asked of the slot in two places only, the renderer base (ColorOf) and a legend row (SeriesLegendEntry: SlotUnder reads its colour slot, HandleColorIn its part's colour else the slot's): no series renderer and no legend drawer asks the slot itself",
            @"\b[sS]lot\.ColorOf\(|\.SeriesSlot\(\s*(?:\w+\.)?ColorSlotIndex\s*\)",
            "Src/MatPlotLibNet/Rendering/SeriesRenderers/SeriesRenderer.cs", "Src/MatPlotLibNet/Rendering/LegendRendering/SeriesLegendEntry.cs") { Finds = [
            "        DrawSwatch(entry.Series, entry.Swatch, swatch.X, swatch.Y, swatch.Width, swatch.Height, theme.SeriesSlot(entry.ColorSlotIndex));"] },
        new("the colours of a rise and a fall (green, red) are typed once, as UpDownColors.Standard: a model default, a verb, a renderer fallback and an indicator read it; the other uses of the two names are looks of their own (a box's median and mean, a violin's mean, a gauge's bands, a reference line)",
            @"\bColors\.(?:Green|Red)\b",
            "Src/MatPlotLibNet/Styling/ValueObjects/UpDownColors.cs", "Src/MatPlotLibNet/Styling/BoxLook.cs", "Src/MatPlotLibNet/Styling/Looks/ViolinLook.cs",
            "Src/MatPlotLibNet/Styling/Themes/MatplotlibThemeFactory.cs", "Src/MatPlotLibNet/Rendering/SeriesRenderers/Circular/GaugeSeriesRenderer.cs",
            "Src/MatPlotLibNet/Builders/DistributionAxesExtensions.cs") { Finds = [
            "            var up = BricksOf(bricks, rising: true, Colors.Green, \"up\");"] },
        new("the first two colours of the standard cycle (Tab10 blue and orange) are named once, as CyclePair.Standard: a part whose colour is a choice made before any theme is known (the value a progress bar's fill, a waterfall's total and a patch read at rest, the pair of a Pareto chart, a cartogram's blue, the indicators' look, a cycler that names no colour) reads it, and every other part given no colour wears its theme's cycle (Theme.FirstCycleColor)",
            @"\bColors\.Tab10(?:Blue|Orange)\b",
            "Src/MatPlotLibNet/Styling/ValueObjects/CyclePair.cs") { Finds = [
            "                Color = Colors.Tab10Blue,"] },
        new("matplotlib's default colour cycle and the Okabe-Ito palette are written as hex literals in QualitativeColorMaps and nowhere else: a theme's cycle and the alarm palette's amber and vermillion read them (Tab10Cycle, Tab10At, OkabeItoCycle, OkabeItoAt); the Okabe-Ito orange is the hue no other palette shares, the hues bmh's cycle also carries are matplotlib's own table for it",
            @"(?i)#?\b(?:1f77b4|ff7f0e|2ca02c|d62728|9467bd|8c564b|e377c2|7f7f7f|bcbd22|17becf|e69f00)\b",
            "Src/MatPlotLibNet/Styling/ColorMaps/QualitativeColorMaps.cs") { Finds = [
            "        Color.FromHex(\"#1f77b4\"), // blue"] },
        OwnedRule.Nowhere("what a normaliser means beyond its Normalize (which normaliser a theme reads it as, what it masks, the middle of a band, the scale its range and its levels are taken over, the normaliser a colour bar draws, whether a bar names decades) is asked of its INormalizerRule: no site tests which type a normaliser is",
            @"\b(?:is|as)\s+(?:not\s+)?(?:[\w.]+\.)?(?:LogNorm|LogNormalizer|LinearNormalizer|TwoSlopeNormalizer|BoundaryNormalizer|SymLogNormalizer|PowerNormNormalizer|CenteredNormNormalizer|NoNormNormalizer)\b",
            under: "Src/") with { Finds = [
            "        if (series.Normalizer is LogNormalizer) return LogScale;"] },
        new("the colour map a series, a figure image or a part that names one draws with is read off the theme's colour-map look by the colour-scale owners (ColorScaledExtensions for a series or an image, ColorMapping for a part, the colour bar's own over its provider's): no renderer or model reads the look for a map itself",
            @"\bLooks\.ColorMap\.Of\(",
            "Src/MatPlotLibNet/Models/Series/IColorScaled.cs", "Src/MatPlotLibNet/Styling/ColorMaps/ColorMapping.cs",
            "Src/MatPlotLibNet/Rendering/ColorBarRendering/ResolvedColorBar.cs") { Under = "Src/", Finds = [
            "var cmap = Context.Theme.Looks.ColorMap.Of(series.ColorMap);",
            "return new ColormapContext(theme.Looks.ColorMap.Of(ColorMap), n, min, max);"] },
        new("a colour bar is resolved once over the series it reads (ResolvedColorBar: the series that backs it, its map, range, normaliser, steps and lines): no renderer or layout picks the series or fetches a part of the bar itself",
            @"OfType<IColorBarDataProvider>\(\)|(?<!IThemedColorBar)\.ColorBar(?:Normalizer|Lines)Under\(|ColorBarStrip\.StepsFor\(|\.RangeOf\(\s*(?:provider|dataProvider)\b",
            "Src/MatPlotLibNet/Rendering/ColorBarRendering/ResolvedColorBar.cs") { Under = "Src/", Finds = [
            "var dataProvider = Axes.Series.OfType<IColorBarDataProvider>().FirstOrDefault();",
            "Create(cb, map, min, max, dataProvider.ColorBarNormalizerUnder(Theme), PlotArea, Ctx, Theme,",
            "ColorBarStrip.StepsFor(colorMap, theme), clearance, provider.ColorBarLinesUnder(theme), room).Render();",
            "var (min, max) = cb.RangeOf(provider, theme);"] },
        new("the normaliser a theme reads a series' own as is asked by its colour mapping (ColorMappingUnder), and for a bar by the bar's normaliser rule: no renderer or model puts a map, a normaliser and a range together itself",
            @"\bNormalizerUnder\(",
            "Src/MatPlotLibNet/Styling/ColorMaps/LogNorm.cs", "Src/MatPlotLibNet/Models/Series/IColorScaled.cs",
            "Src/MatPlotLibNet/Rendering/ColorBarRendering/ResolvedColorBar.cs") { Under = "Src/", Finds = [
            "var norm = series.NormalizerUnder(Context.Theme);",
            "double lowest = this.NormalizerUnder(theme).RangeScale.LowestPlaced;"] },
        OwnedRule.Nowhere("a colour range of no width is opened one wide by ColorMapping.Opened (through ColorMapping.RangeOf): no kind opens one, falls back to 0 to 1 or reads the public grid scan itself",
            @"\bif\s*\(\s*(?<a>\w+)\s*==\s*(?<b>\w+)\s*\)\s*\k<b>\s*=\s*\k<a>\s*\+\s*1\b|\?\s*new\([^;]*\)\s*:\s*new\(\s*0\s*,\s*1\s*\)|\bScanColorBarRange\(|==\s*\w+\.Hi\s*\?\s*\w+\.Lo\s*\+\s*1\b",
            under: "Src/MatPlotLibNet/Models/") with { Finds = [
            "        if (min == max) max = min + 1;",
            "        return min < max ? new(min, max) : new(0, 1);",
            "Z.Data.DrawableRange(scale) is { } z ? new(z.Lo, z.Hi) : new(0, 1);",
            "? new(range.Lo, range.Lo == range.Hi ? range.Lo + 1 : range.Hi)",
            "var range = Data.ScanColorBarRange(scale);"] },
    ];
}
