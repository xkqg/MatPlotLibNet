// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>The WCAG 2 contrast ratio of two colours, for the tests that ask whether a part of a chart can be seen against the
/// plot behind it. WCAG asks a graphic that is needed to understand the content for a ratio of 3 : 1.
/// <para>This is not <see cref="ColorExtensions.Luminance"/>, which weighs the encoded channels the way a cell label's
/// black-or-white choice needs; the ratio is defined over the linearised sRGB channels.</para></summary>
public static class WcagContrastExtensions
{
    /// <summary>The contrast ratio of <paramref name="first"/> and <paramref name="second"/>, from 1 (the same colour) to 21
    /// (black on white). The order does not matter.</summary>
    /// <param name="first">One colour.</param>
    /// <param name="second">The other colour.</param>
    public static double ContrastRatioWith(this Color first, Color second)
    {
        double a = first.RelativeLuminance(), b = second.RelativeLuminance();
        return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
    }

    private static double RelativeLuminance(this Color color) =>
        0.2126 * Linear(color.R) + 0.7152 * Linear(color.G) + 0.0722 * Linear(color.B);

    private static double Linear(byte channel)
    {
        double s = channel / 255.0;
        return s <= 0.04045 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
    }
}
