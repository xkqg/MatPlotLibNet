// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.Lighting;

namespace MatPlotLibNet.Styling;

/// <summary>Extension methods for color lighting operations on <see cref="Color"/>.</summary>
public static class ColorExtensions
{
    extension(Color color)
    {
        /// <summary>Shades a color by a directional light at the default ambient (0.3) and diffuse (0.7): the shading of
        /// <see cref="DirectionalLight"/>, <c>k = 0.3 + 0.7 · (dot + 1) / 2</c>, which is matplotlib's
        /// <c>0.65 + 0.35 · dot</c> in <c>_shade_colors</c>. Returns <paramref name="color"/> unchanged when either normal or
        /// light vector has near-zero length. A light with other strengths shades through its own
        /// <see cref="ILightSource.Shade"/>.</summary>
        public Color Shade(
            double nx, double ny, double nz,
            double lx, double ly, double lz) =>
            new DirectionalLight(lx, ly, lz).Shade(color, nx, ny, nz);

        /// <summary>Scales a color's RGB channels by <paramref name="intensity"/> (clamped to [0,1]),
        /// preserving alpha.</summary>
        public Color Modulate(double intensity)
        {
            intensity = Math.Clamp(intensity, 0, 1);
            return new Color(
                (byte)(color.R * intensity),
                (byte)(color.G * intensity),
                (byte)(color.B * intensity),
                color.A);
        }

        /// <summary>Relative luminance per Rec. 709: <c>L = 0.2126·R + 0.7152·G + 0.0722·B</c>
        /// on channels normalised to [0,1]. Returned value is in [0,1]; alpha is ignored.
        /// Used by renderers (e.g. heatmap cell annotations) to pick black or white text
        /// against an arbitrary fill colour for maximum contrast.</summary>
        public double Luminance() =>
            (0.2126 * color.R + 0.7152 * color.G + 0.0722 * color.B) / 255.0;

        /// <summary>The WCAG 2 relative luminance of this colour, from 0 (black) to 1 (white): the Rec. 709 weights on the
        /// linearised sRGB channels. It is not <see cref="Luminance"/>, which weighs the encoded channels the way a cell
        /// label's black-or-white choice needs; the contrast ratio is defined over this one. Alpha is ignored.</summary>
        internal double RelativeLuminance() =>
            0.2126 * Linear(color.R) + 0.7152 * Linear(color.G) + 0.0722 * Linear(color.B);

        /// <summary>The WCAG 2 contrast ratio of this colour and <paramref name="other"/>, from 1 (the same colour) to 21
        /// (black on white); the order does not matter. WCAG asks a graphic that is needed to understand the content for 3 : 1.</summary>
        /// <param name="other">The colour it is read against.</param>
        internal double ContrastRatioWith(Color other)
        {
            double a = color.RelativeLuminance(), b = other.RelativeLuminance();
            return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
        }

        /// <summary>This colour laid over <paramref name="backdrop"/> by its own alpha: the opaque colour a viewer sees. An
        /// opaque colour is itself; the backdrop is taken as opaque.</summary>
        /// <param name="backdrop">The colour behind it.</param>
        internal Color CompositedOver(Color backdrop)
        {
            double alpha = color.A / 255.0;
            byte Mix(byte over, byte under) => (byte)Math.Round(over * alpha + under * (1 - alpha));
            return new Color(Mix(color.R, backdrop.R), Mix(color.G, backdrop.G), Mix(color.B, backdrop.B));
        }

        /// <summary>The grey that a graphic drawn over this plot colour wears to be seen: #808080 when it reaches the 3 : 1
        /// contrast WCAG asks of a graphic against it, and otherwise the grey nearest to #808080 that does (the lighter one
        /// when two are equally near). Black or white always reaches 3 : 1 against any colour, since the better of the two
        /// stands at 4.58 : 1 or more, so the search always ends.</summary>
        internal Color NeutralGrey()
        {
            for (int distance = 0; ; distance++)
            {
                foreach (int level in new[] { 128 + distance, 128 - distance })
                {
                    var grey = new Color((byte)level, (byte)level, (byte)level);
                    if (grey.ContrastRatioWith(color) >= 3.0) return grey;
                }
            }
        }
    }

    extension(Color fill)
    {
        /// <summary>Returns <see cref="Colors.Black"/> when <paramref name="fill"/> has luminance ≥ 0.5,
        /// otherwise <see cref="Colors.White"/> — the higher-contrast text colour against the fill.</summary>
        public Color ContrastingTextColor() =>
            fill.Luminance() >= 0.5 ? Colors.Black : Colors.White;
    }

    private static double Linear(byte channel)
    {
        double s = channel / 255.0;
        return s <= 0.04045 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
    }
}
