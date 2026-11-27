// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.Lighting;

namespace MatPlotLibNet.Styling;

/// <summary>Extension methods for color lighting operations on <see cref="Color"/>.</summary>
public static class ColorExtensions
{
    extension(Color color)
    {
        /// <summary>Shades a color by a directional light at the resting ambient (<see cref="DirectionalLight.RestingAmbient"/>) and
        /// diffuse (<see cref="DirectionalLight.RestingDiffuse"/>): the shading of <see cref="DirectionalLight"/>, <c>k = ambient + diffuse
        /// · (dot + 1) / 2</c>, which at rest is matplotlib's <c>0.65 + 0.35 · dot</c> in <c>_shade_colors</c>. Returns
        /// <paramref name="color"/> unchanged when either normal or light vector has near-zero length. A light with other strengths shades
        /// through its own <see cref="ILightSource.Shade"/>.</summary>
        public Color Shade(
            double nx, double ny, double nz,
            double lx, double ly, double lz) =>
            new DirectionalLight(lx, ly, lz).Shade(color, nx, ny, nz);

        /// <summary>Scales a color's RGB channels by <paramref name="intensity"/> (clamped to [0,1]), each to the nearest step as matplotlib's
        /// <c>_shade_colors</c> and its writers round them: the one shading of a colour, a light's included,
        /// preserving alpha.</summary>
        public Color Modulate(double intensity) =>
            new(color.R.Scaled(intensity), color.G.Scaled(intensity), color.B.Scaled(intensity), color.A);

        /// <summary>This colour with its saturation scaled by <paramref name="share"/> (held between 0 and 1), as seaborn's
        /// <c>utils.desaturate(color, prop)</c> makes it: to hue, lightness and saturation (python's <c>colorsys.rgb_to_hls</c>), the
        /// saturation times the share, and back (<c>hls_to_rgb</c>), each channel to the nearest step as Agg takes a float colour; the
        /// opacity kept. A share of 1 is the colour itself, and a grey stays grey.</summary>
        /// <param name="share">How much of the saturation is kept, from 0 (a grey of the same lightness) to 1 (the colour as it is).</param>
        /// <returns>The desaturated colour.</returns>
        internal Color Desaturated(double share)
        {
            double r = color.R.Share, g = color.G.Share, b = color.B.Share;
            double max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b));
            if (share >= 1 || max == min) return color;

            double range = max - min, lightness = (max + min) / 2;
            double saturation = (lightness <= 0.5 ? range / (max + min) : range / (2 - max - min)) * share.Saturate();
            double sector = r == max ? (max - b - (max - g)) / range
                : g == max ? 2 + ((max - r) - (max - b)) / range
                : 4 + ((max - g) - (max - r)) / range;
            double hue = sector / 6 - Math.Floor(sector / 6);

            double high = lightness <= 0.5 ? lightness * (1 + saturation) : lightness + saturation - lightness * saturation;
            double low = 2 * lightness - high;
            return new Color(HlsChannel(low, high, hue + 1.0 / 3), HlsChannel(low, high, hue), HlsChannel(low, high, hue - 1.0 / 3), color.A);
        }

        /// <summary>This colour with its own opacity scaled by <paramref name="alpha"/> (held between 0 and 1): a colour that is
        /// already translucent becomes that much more translucent, and a colour with no opacity stays without. The rule a series'
        /// alpha is applied by.</summary>
        /// <param name="alpha">The factor, from 0 (clear) to 1 (the colour as it is).</param>
        internal Color ScaleAlpha(double alpha) => color.WithAlpha(color.A.Scaled(alpha));

        /// <summary>This colour at the opacity <paramref name="opacity"/> in place of its own (held between 0 and 1), rounded to the
        /// nearest step: the byte of an opacity, as <see cref="Color.FromRgba"/> makes its alpha. Where a colour's own opacity is
        /// kept and scaled, <see cref="ScaleAlpha"/> is the rule.</summary>
        /// <param name="opacity">The opacity, from 0 (clear) to 1 (opaque).</param>
        internal Color WithOpacity(double opacity) => color.WithAlpha(byte.MaxValue.Scaled(opacity));

        /// <summary>This colour drawn at the opacity <paramref name="alpha"/> gives in place of its own (held between 0 and 1), as
        /// matplotlib's <c>to_rgba(color, alpha)</c> replaces it; the colour as it is when no opacity is given, and a colour of none
        /// (<see cref="Colors.Transparent"/>, matplotlib's <c>'none'</c>) stays none. The rule an opacity given to a patch reaches
        /// each of its colours by.</summary>
        /// <param name="alpha">The opacity given, from 0 (clear) to 1 (opaque); <see langword="null"/> when none was.</param>
        internal Color WithOpacityGiven(double? alpha) =>
            alpha is { } a && !color.IsNone ? color.WithOpacity(a) : color;

        /// <summary>This colour at the opacity <paramref name="alpha"/> gives (<see cref="WithOpacityGiven"/>) when it is below full, and
        /// the colour exactly as it is at full opacity: the resting opacity 1 of a series lets a colour with an alpha of its own keep it.</summary>
        /// <param name="alpha">The opacity of the series, from 0 (clear) to 1 (opaque).</param>
        internal Color WithOpacityBelowFull(double alpha) => alpha < 1.0 ? color.WithOpacityGiven(alpha) : color;

        /// <summary>Whether this is a colour of none (<see cref="Colors.Transparent"/>, matplotlib's <c>'none'</c>): an opacity of 0, so a
        /// fill, an edge or a cell in it draws nothing, and an opacity given to it leaves it none.</summary>
        internal bool IsNone => color.A == 0;

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
            double alpha = color.A.Share;
            return new Color(backdrop.R.Toward(color.R, alpha), backdrop.G.Toward(color.G, alpha), backdrop.B.Toward(color.B, alpha));
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

    extension(byte channel)
    {
        /// <summary>This channel (or opacity) scaled by <paramref name="share"/>, held between 0 and 1, and rounded to the nearest
        /// step (halves to the even one, <see cref="Math.Round(double)"/>): the rule an opacity or a channel is scaled by where the
        /// step nearest the exact value is the one drawn. <c>byte.MaxValue.Scaled(opacity)</c> is the byte of an opacity.</summary>
        /// <param name="share">The share kept, from 0 to 1.</param>
        internal byte Scaled(double share) => (byte)Math.Round(channel * share.Saturate());

        /// <summary>This channel (or opacity) scaled by <paramref name="share"/>, held between 0 and 1, with the fraction cut off:
        /// matplotlib's <c>(x * 255).astype(uint8)</c>, the rule a colour map's output and the blends that follow it are written by.
        /// The step is one below <see cref="Scaled"/>'s wherever the exact value has a fraction of a half or more.</summary>
        /// <param name="share">The share kept, from 0 to 1.</param>
        internal byte ScaledTruncating(double share) => (byte)(channel * share.Saturate());

        /// <summary>This channel (or opacity) <paramref name="share"/> of the way to <paramref name="to"/>, rounded to the nearest step
        /// (halves to the even one): the one blend of two bytes, which a blend of two colours (<see cref="Color.Lerp"/>) and a colour laid
        /// over its backdrop (<see cref="CompositedOver"/>, where the share is the colour's opacity) read.</summary>
        /// <param name="to">The channel at a share of 1.</param>
        /// <param name="share">How far toward <paramref name="to"/>, from 0 (this channel) to 1.</param>
        internal byte Toward(byte to, double share) => (byte)Math.Round(channel + (to - channel) * share);

        /// <summary>This channel (or opacity) as a share of the full channel, from 0 to 1: the other way of <see cref="Scaled"/> on
        /// <see cref="byte.MaxValue"/>, and the one place a byte is read as a number between 0 and 1.</summary>
        internal double Share => channel / 255.0;
    }

    extension(Color fill)
    {
        /// <summary>Returns <see cref="Colors.Black"/> when <paramref name="fill"/> has luminance ≥ 0.5,
        /// otherwise <see cref="Colors.White"/> — the higher-contrast text colour against the fill.</summary>
        public Color ContrastingTextColor() =>
            fill.Luminance() >= 0.5 ? Colors.Black : Colors.White;
    }

    /// <summary>One channel of a colour made from hue, lightness and saturation: python's <c>colorsys._v(m1, m2, hue)</c> over the low and
    /// the high end of the lightness band, to the nearest step.</summary>
    private static byte HlsChannel(double low, double high, double hue)
    {
        hue -= Math.Floor(hue);
        double share = hue < 1.0 / 6 ? low + (high - low) * hue * 6
            : hue < 0.5 ? high
            : hue < 2.0 / 3 ? low + (high - low) * (2.0 / 3 - hue) * 6
            : low;
        return byte.MaxValue.Scaled(share);
    }

    private static double Linear(byte channel)
    {
        double s = channel.Share;
        return s <= 0.04045 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
    }
}
