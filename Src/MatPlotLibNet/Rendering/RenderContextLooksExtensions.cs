// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>Tells any surface the looks of the theme it is drawn under and the math texts the figure being drawn keeps
/// (<see cref="IThemedRenderContext"/>); a surface of your own, which holds neither, draws as a library theme does and parses a label each
/// time it reads it. The one drawing sequence asks these members at the start of each stage, never the type of the surface.</summary>
internal static class RenderContextLooksExtensions
{
    extension(IRenderContext ctx)
    {
        /// <summary>Hands the surface what a stage of a draw is drawn under: the looks of <paramref name="theme"/> and the math texts
        /// <paramref name="texts"/> its figure keeps.</summary>
        /// <param name="texts">The math texts the figure being drawn keeps (<see cref="Models.Figure.MathTexts"/>).</param>
        /// <param name="theme">The theme of the figure being drawn.</param>
        internal void UseStageOf(KeptMathTexts texts, Theme theme)
        {
            ctx.UseLooksOf(theme);
            ctx.TellMathTexts(texts);
        }

        /// <summary>Hands the surface the looks of <paramref name="theme"/>, for the marks drawn next.</summary>
        /// <param name="theme">The theme of the figure being drawn.</param>
        internal void UseLooksOf(Theme theme) => ctx.TellLooks(theme.Looks);

        /// <summary>Hands the surface <paramref name="looks"/>, for the marks drawn next; a surface that holds no looks takes none.</summary>
        /// <param name="looks">The looks of the theme of the figure being drawn.</param>
        internal void TellLooks(ThemeLooks looks)
        {
            if (ctx is IThemedRenderContext themed) themed.UseLooks(looks);
        }

        /// <summary>Hands the surface the math texts <paramref name="texts"/> the figure being drawn keeps; a surface that holds no looks
        /// takes none.</summary>
        /// <param name="texts">The math texts.</param>
        internal void TellMathTexts(KeptMathTexts texts)
        {
            if (ctx is IThemedRenderContext themed) themed.UseMathTexts(texts);
        }

        /// <summary>The looks the surface was told last; the library's own for a surface that holds none.</summary>
        internal ThemeLooks LooksTold => ctx is IThemedRenderContext themed ? themed.Looks : ThemeLooks.Library;

        /// <summary>The math texts the surface was told last (<see cref="IThemedRenderContext.MathTexts"/>); <see langword="null"/> for a
        /// surface that holds none or was told none.</summary>
        internal KeptMathTexts? MathTextsTold => ctx is IThemedRenderContext themed ? themed.MathTexts : null;

        /// <summary>The metrics the surface measures the box of a text by (<see cref="IThemedRenderContext.BoxMetrics"/>);
        /// <see langword="null"/> for a surface that names none.</summary>
        internal TextMeasurement.IFontMetrics? BoxMetricsTold => ctx is IThemedRenderContext themed ? themed.BoxMetrics : null;

        /// <summary>The faces the surface sets math in (<see cref="IThemedRenderContext.MathFonts"/>); the library's own for a surface
        /// that holds no looks.</summary>
        internal MathFontSet MathFontsTold => ctx is IThemedRenderContext themed ? themed.MathFonts : MathFontSet.Library;
    }
}
