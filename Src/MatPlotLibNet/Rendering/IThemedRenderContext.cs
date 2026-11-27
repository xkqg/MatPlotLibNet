// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>A surface that is told the looks of the theme it draws under, once per stage of a draw, and holds what it does with a stroke
/// by them: whether a raster export puts straight marks on its pixel grid and covers its lines as Agg does, the lengths a dash is drawn
/// at, the join of a stroke. A renderer never reads them off the surface: the one drawing sequence (<see cref="ChartRenderer"/>) tells
/// the surface through <see cref="RenderContextLooksExtensions"/>, and a surface that has not been told draws as a library theme does.
/// The one reader of <see cref="Looks"/> is the seam a text is measured by (<see cref="TextMeasurement.TextExtentExtensions"/>), which
/// measures a text as the told looks say. It lives here and not on <see cref="IRenderContext"/> so that an application can still
/// implement, proxy or mock the public interface.</summary>
internal interface IThemedRenderContext : IRenderContext
{
    /// <summary>Takes the looks the marks drawn next are drawn under.</summary>
    /// <param name="looks">The looks of the theme of the figure being drawn.</param>
    void UseLooks(ThemeLooks looks);

    /// <summary>The looks the surface was told last; the library's own until it is told.</summary>
    ThemeLooks Looks { get; }

    /// <summary>Takes the math texts the figure being drawn keeps from one draw to the next (<see cref="MathText.KeptMathTexts"/>), told
    /// with the looks at the start of each stage, which a label drawn or measured on this surface is read from.</summary>
    /// <param name="texts">The math texts of the figure being drawn.</param>
    void UseMathTexts(MathText.KeptMathTexts texts);

    /// <summary>The math texts the surface was told last; <see langword="null"/> until it is told, and a label is then parsed alone.</summary>
    MathText.KeptMathTexts? MathTexts { get; }

    /// <summary>The metrics the surface measures the box of a text by (its <see cref="IRenderContext.MeasureText"/>): the installed
    /// metrics for every surface of the library, so math laid out on one of them is the layout the others read, from one draw to the next
    /// (<see cref="MathText.MathMeasure"/>); <see langword="null"/> where the surface measures by something else.</summary>
    TextMeasurement.IFontMetrics? BoxMetrics { get; }

    /// <summary>The faces this surface sets math in (<see cref="MathText.MathFontSet"/>): the set of the looks it was told where it draws
    /// the glyphs of those faces, the library's own (the label's face) where it draws in fonts it cannot choose. Read by the surface's
    /// rich draw and by the seam a rich text is measured by, so a text is measured in the faces it is drawn in.</summary>
    MathText.MathFontSet MathFonts { get; }
}
