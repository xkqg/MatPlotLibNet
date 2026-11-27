// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Packaging.OwnedRules;

/// <summary>The rules of a drawing surface: what it was told by a draw and keeps, a rectangle on its canvas, a font's weight and
/// slant, the turn of a text, a join, the layers of a shape, a clip and an opacity scope.</summary>
internal static class SurfaceRules
{
/// <summary>What a drawing surface was told by a draw and keeps (the looks of the theme, the math texts of the figure, the opacity of
    /// the marks), is held in the <c>ThemedSurface</c> every surface composes; no surface declares a field of its own for any of them.</summary>
    private static readonly OwnedRule SurfaceStateHeld = new(
        "what a drawing surface was told (its theme's looks, the figure's math texts, the opacity of its marks) is kept in the ThemedSurface it composes: no surface declares a field for it",
        @"\bThemeLooks\??\s+_looks\b|\bKeptMathTexts\??\s+_mathTexts\b|\b(?:double|float)\??\s+_opacity\b",
        "Src/MatPlotLibNet/Rendering/ThemedSurface.cs") { Under = "Src/", Finds = [
        "    private ThemeLooks _looks = ThemeLooks.Library;",
        "    private KeptMathTexts? _mathTexts;",
        "    private double _opacity = 1.0;",
        "    private float _opacity = 1f;",
        "    private ThemeLooks? _looks;",
        "    private double? _opacity;",
        "    private float? _opacity = 1f;"] };

/// <summary>A rectangle of the library becomes the rectangle of a canvas by one conversion per package (<c>ToSkRect</c>,
    /// <c>ToRectF</c>). Two corners that are not a rectangle of the library (the band a line covers) are another thing.</summary>
    private static readonly OwnedRule RectToTheCanvas = new(
        "a rectangle becomes the rectangle of a canvas by Rect.ToSkRect (Skia) or Rect.ToRectF (MAUI): no surface spells its x, y, width and height as the canvas' constructor takes them; the band AggPixelGrid covers between two corners is no Rect of the library",
        @"\bnew (?:SKRect|RectF)\(\s*\(float\)(?<r>\w+)\.X\s*,\s*\(float\)\k<r>\.Y\b|\(float\)(?<q>\w+)\.X\s*,\s*\(float\)\k<q>\.Y\s*,\s*\(float\)\k<q>\.Width\b",
        "Src/MatPlotLibNet.Skia/SkiaRenderContext.AggPixelGrid.cs", "Src/MatPlotLibNet.Skia/SkiaRenderContext.cs", "Src/MatPlotLibNet.Maui/MauiGraphicsRenderContext.cs") { Under = "Src/", Finds = [
        "        var skRect = new SKRect((float)rect.X, (float)rect.Y,",
        "            var bounds = new RectF((float)rect.X, (float)rect.Y, (float)rect.Width, (float)rect.Height);",
        "        _canvas.DrawRectangle((float)rect.X, (float)rect.Y, (float)rect.Width, (float)rect.Height);",
        "        _canvas.DrawEllipse((float)bounds.X, (float)bounds.Y, (float)bounds.Width, (float)bounds.Height);",
        "        _canvas.ClipRectangle((float)clipRect.X, (float)clipRect.Y, (float)clipRect.Width, (float)clipRect.Height);"] };

/// <summary>Whether a font is bold or italic is asked of the font; a surface converts the answer to what its canvas calls it. The
    /// way back, a face the canvas loaded read as the library's weight and slant, is the Skia package's <c>ToFontWeight</c> and
    /// <c>ToFontSlant</c>; what a face is keyed under is asked of those, never of the canvas' enum.</summary>
    private static readonly OwnedRule FontBoldOrItalic = new(
        "whether a font is bold or italic is Font.IsBold and Font.IsItalic, whether a slant leans FontSlant.IsSlanted (asked by the surfaces and by math alike), and a canvas face's weight and slant read back are SkiaFonts.ToFontWeight and ToFontSlant: no surface or layout compares a weight or slant to the library's or the canvas' enum itself",
        @"\.(?:Weight|Slant)\s*(?:==|!=|<=|>=|<|>)\s*(?:\(int\)\s*)?(?:Font|SKFontStyle)(?:Weight|Slant)\.|\bis\s+(?:not\s+)?FontSlant\.",
        "Src/MatPlotLibNet/Styling/FontStyleExtensions.cs") { Under = "Src/", Finds = [
        "        if (font.Weight == FontWeight.Bold) _sb.Append(\" font-weight=\\\"bold\\\"\");",
        "        font.Slant == FontSlant.Italic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright);",
        "        bool italic = style.Slant != SKFontStyleSlant.Upright;",
        "        bool bold = style.Weight >= (int)SKFontStyleWeight.SemiBold;",
        "    internal bool Slanted => Leans || (Own?.Slant ?? Slant) is FontSlant.Italic or FontSlant.Oblique;"] };

/// <summary>The library turns a text counter-clockwise, every canvas clockwise: the sign is flipped by <c>ToClockwise</c> alone.</summary>
    private static readonly OwnedRule TurnOfAText = OwnedRule.Nowhere(
        "a text turned counter-clockwise by the library is turned clockwise by every canvas through rotation.ToClockwise(): no surface negates the angle itself",
        @"[(,]\s*-\s*(?:\(float\)\s*)?rotation\b",
        under: "Src/") with { Finds = [
        "            _canvas.RotateDegrees(-(float)rotation, (float)position.X, (float)position.Y);",
        "            _canvas.Rotate(-(float)rotation, (float)position.X, (float)position.Y);",
        "            _sb.Append(\" rotate(\").AppendSvgNumber(-rotation).Append(')');"] };

    /// <summary>A join of a stroke becomes what a canvas calls it by one conversion per surface (<c>ToSvgName</c>, <c>ToSkia</c>,
    /// <c>ToMaui</c>), beside its other conversions.</summary>
    private static readonly OwnedRule JoinToTheCanvas = new(
        "a stroke's join becomes its canvas' by JoinStyle.ToSvgName, ToSkia or ToMaui: no surface switches on or compares a JoinStyle itself",
        @"\bJoinStyle\.(?:Round|Bevel)\s*=>|\bis\s+JoinStyle\.(?:Round|Bevel)|==\s*JoinStyle\.Round\b",
        "Src/MatPlotLibNet/Rendering/Svg/SvgWriterExtensions.cs", "Src/MatPlotLibNet.Skia/SkiaRenderContext.cs",
        "Src/MatPlotLibNet.Maui/MauiGraphicsRenderContext.cs") { Under = "Src/", Finds = [
        "            StrokeJoin = join switch { JoinStyle.Round => SKStrokeJoin.Round, JoinStyle.Bevel => SKStrokeJoin.Bevel, _ => SKStrokeJoin.Miter },",
        "            if (stroke.Join is JoinStyle.Round or JoinStyle.Bevel)",
        "                sb.Append(stroke.Join == JoinStyle.Round ? \"round\" : \"bevel\");"] };

/// <summary>A shape on a canvas (the Skia and MAUI surfaces) is painted in one sequence: the fill, the hatch over it, then the outline,
    /// each when it shows (<c>CanvasShapeExtensions.PaintIn</c>); a surface paints the layers it is asked for. The SVG surface writes one
    /// element with its paint attributes, another mechanism, whose own question of a fill and a hatch stays in its writers.</summary>
    private static readonly OwnedRule ShapeOnACanvas = new(
        "the fill, the hatch and the outline of a shape on a canvas are painted in that order, each when it shows, by CanvasShapeExtensions.PaintIn: no canvas surface asks whether a fill or a hatch shows itself",
        @"\.HasVisible(?:Fill|Hatch)\b",
        "Src/MatPlotLibNet/Rendering/CanvasShapeExtensions.cs", "Src/MatPlotLibNet/Rendering/Svg/SvgElementBuffer.cs",
        "Src/MatPlotLibNet/Rendering/Svg/SvgRenderContext.cs", "Src/MatPlotLibNet/Rendering/Svg/SvgWriterExtensions.cs") { Under = "Src/", Finds = [
        "        if (shape.HasVisibleFill)",
        "            if (shape.HasVisibleHatch)"] };

    /// <summary>The rules of the area, in the order the scan reads them.</summary>
    internal static OwnedRule[] All { get; } =
    [
        new("an opacity set for a group of marks is ctx.WithOpacity(a), which puts it back: the pair of calls is not typed at the site; the stages of a figure start at full opacity by ctx.AtFullOpacity() and the depth queue sets the opacity of each face it draws",
            @"\.SetOpacity\(",
            "Src/MatPlotLibNet/Rendering/RenderContextScopeExtensions.cs", "Src/MatPlotLibNet/Rendering/DepthQueue3D.cs") { Finds = [
            "            ctx.SetOpacity(Alpha);"] },
        new("a clip around a group of marks is ctx.ClippedTo(region), which takes it off: the push and the pop are not typed at the site",
            @"^(?!.*\bvoid\b).*\.(?:PushClip|PopClip)\(",
            "Src/MatPlotLibNet/Rendering/RenderContextScopeExtensions.cs") { Finds = [
            "            if (clipped) Ctx.PopClip();"] },
        new("the looks a surface was told, the library's own when it holds none, and the math texts and box metrics it reads by are told and asked through RenderContextLooksExtensions (UseStageOf, UseLooksOf, TellLooks, TellMathTexts, LooksTold, MathTextsTold, BoxMetricsTold, MathFontsTold): no other site asks a surface whether it holds looks",
            @"\bis IThemedRenderContext\b",
            "Src/MatPlotLibNet/Rendering/RenderContextLooksExtensions.cs") { Under = "Src/MatPlotLibNet/", Finds = [
            "            if (ctx is IThemedRenderContext themed) themed.UseLooks(looks);"] },
        SurfaceStateHeld,
        RectToTheCanvas,
        FontBoldOrItalic,
        TurnOfAText,
        JoinToTheCanvas,
        ShapeOnACanvas,
    ];
}
