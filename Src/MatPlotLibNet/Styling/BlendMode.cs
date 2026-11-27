// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>Compositing blend mode for alpha-layered image rendering.</summary>
/// <remarks><b>Append-only ordinal contract:</b> never reorder,
/// remove, or renumber.</remarks>
public enum BlendMode
{
    /// <summary>Standard alpha compositing: src * α + dst * (1 − α).</summary>
    Normal = 0,

    /// <summary>Multiplies src and dst per-channel: src × dst.</summary>
    Multiply = 1,

    /// <summary>Brightens by inverting multiplication: 1 − (1 − src) × (1 − dst).</summary>
    Screen = 2,

    /// <summary>Contrast-enhancing mode: Multiply for dark areas, Screen for light areas.</summary>
    Overlay = 3,
}

/// <summary>Per-pixel compositing operations for <see cref="BlendMode"/> values.</summary>
public static class CompositeOperation
{
    /// <summary>
    /// Blends a source color over a destination color using the given <paramref name="mode"/> and <paramref name="alpha"/>.
    /// All operations are performed in normalized [0, 1] space per channel.
    /// </summary>
    public static Color Blend(Color src, Color dst, BlendMode mode, double alpha)
    {
        double sr = src.R.Share, sg = src.G.Share, sb = src.B.Share;
        double dr = dst.R.Share, dg = dst.G.Share, db = dst.B.Share;

        double br, bg, bb;
        switch (mode)
        {
            case BlendMode.Multiply:
                br = sr * dr;
                bg = sg * dg;
                bb = sb * db;
                break;
            case BlendMode.Screen:
                br = 1 - (1 - sr) * (1 - dr);
                bg = 1 - (1 - sg) * (1 - dg);
                bb = 1 - (1 - sb) * (1 - db);
                break;
            case BlendMode.Overlay:
                br = OverlayChannel(sr, dr);
                bg = OverlayChannel(sg, dg);
                bb = OverlayChannel(sb, db);
                break;
            default: // Normal
                br = sr; bg = sg; bb = sb;
                break;
        }

        // Alpha composite blended result over dst
        double r = br * alpha + dr * (1 - alpha);
        double g = bg * alpha + dg * (1 - alpha);
        double b = bb * alpha + db * (1 - alpha);

        return new Color(byte.MaxValue.ScaledTruncating(r), byte.MaxValue.ScaledTruncating(g), byte.MaxValue.ScaledTruncating(b));
    }

    private static double OverlayChannel(double s, double d)
        => d < 0.5 ? 2 * s * d : 1 - 2 * (1 - s) * (1 - d);

}
