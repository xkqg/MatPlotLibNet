// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>What a drawing surface keeps of what a draw tells it, for every surface alike: the looks of the theme the marks are drawn
/// under, the math texts the figure keeps from one draw to the next, and the opacity the marks are drawn at, held between 0 and 1. A
/// surface holds one as a field (<see cref="ISurfaceRenderContext"/>, which hands it out by reference) and converts what it holds to what
/// its own canvas takes; it keeps no copy of the state or of the rule that holds it. A surface that was told nothing draws as a library
/// theme does, in full opacity. A value, so a surface allocates nothing for it.</summary>
internal struct ThemedSurface
{
    private ThemeLooks? _looks;
    private double _opacity = 1.0;

    /// <summary>A surface that was told nothing: a library theme's looks, no math texts, full opacity. A constructor, not the default
    /// value, because the opacity is held as the plain double it is (an optional one would weigh eight bytes more in every context); a
    /// context holds its surface as <c>new()</c>.</summary>
    public ThemedSurface() { }

    /// <summary>The looks of the theme the marks drawn next are drawn under; a library theme's until a draw tells them.</summary>
    internal ThemeLooks Looks
    {
        readonly get => _looks ?? ThemeLooks.Library;
        set => _looks = value;
    }

    /// <summary>The math texts the figure being drawn keeps; <see langword="null"/> until a draw tells them, and a label is then parsed
    /// alone.</summary>
    internal KeptMathTexts? MathTexts { get; set; }

    /// <summary>The opacity the marks are drawn at, 0 to 1; 1 before one is held.</summary>
    internal readonly double Opacity => _opacity;

    /// <summary>The metrics a text's box is measured by: the installed metrics, for every surface of the library, so math laid out on
    /// one of them is the layout the others read.</summary>
    internal readonly IFontMetrics BoxMetrics => FontMetricsDefault.Current;

    /// <summary>Takes the opacity the marks are drawn at next, cut to 0 to 1.</summary>
    /// <param name="opacity">The opacity asked for.</param>
    /// <returns>The opacity held: <paramref name="opacity"/> at the nearest end of 0 to 1.</returns>
    internal double HoldOpacity(double opacity)
    {
        double held = opacity.Saturate();
        _opacity = held;
        return held;
    }

    /// <summary>The faces math is set in on a surface: the set of the looks it was told where the surface draws the glyphs of those faces
    /// from the package's own fonts, the library's own set (math in the label's face) where it draws in fonts it cannot choose.</summary>
    /// <param name="inPackageFonts">Whether the surface draws in the package's own fonts, which carry every face of every set.</param>
    /// <returns>The set a rich text is measured and drawn in.</returns>
    internal readonly MathFontSet MathFontsFor(bool inPackageFonts) => inPackageFonts ? Looks.Text.MathFonts : MathFontSet.Library;
}
