// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Numerics;
using System.Runtime.Intrinsics;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.Interpolation;

/// <summary>One cell of an image's colours as matplotlib's <c>'rgba'</c> interpolation stage resamples them: red, green and blue, each
/// multiplied by the opacity, and the opacity, 0 to 1, in the four lanes of one vector, so an engine resamples the four channels in one pass
/// with the arithmetic it resamples a value with (each lane the same double operations as a channel resampled on its own).</summary>
/// <param name="channels">The premultiplied red, green and blue and the opacity, in that order.</param>
internal readonly struct ColourSample(Vector256<double> channels)
    : IAdditionOperators<ColourSample, ColourSample, ColourSample>, IMultiplyOperators<ColourSample, double, ColourSample>,
      IAdditiveIdentity<ColourSample, ColourSample>
{
    /// <summary>The premultiplied red, green and blue and the opacity.</summary>
    internal Vector256<double> Channels { get; } = channels;

    /// <summary>A colour premultiplied by its opacity (<c>A = A * alpha_in[..., None]</c>).</summary>
    /// <param name="colour">The colour.</param>
    /// <returns>The premultiplied sample.</returns>
    internal static ColourSample Premultiplied(Color colour)
    {
        double opacity = colour.A.Share;
        return new(Vector256.Create(colour.R.Share * opacity, colour.G.Share * opacity, colour.B.Share * opacity, opacity));
    }

    /// <summary>The colour again, as matplotlib takes it back (3.11.2): the colour channels divided by the opacity they came out with, left
    /// as they are where it is 0, and each channel made a byte by cutting off its fraction (<c>to_rgba(res, bytes=True)</c>:
    /// <c>(x * 255).astype(uint8)</c>), a channel outside 0 to 1 held at its end.</summary>
    /// <returns>The colour as a byte per channel.</returns>
    internal Color Unpremultiplied()
    {
        double opacity = Channels[3], divisor = opacity != 0 ? opacity : 1;
        return new Color(Byte(Channels[0] / divisor), Byte(Channels[1] / divisor), Byte(Channels[2] / divisor), Byte(opacity));

        static byte Byte(double channel) => byte.MaxValue.ScaledTruncating(channel);
    }

    /// <inheritdoc />
    public static ColourSample AdditiveIdentity => default;

    /// <inheritdoc />
    public static ColourSample operator +(ColourSample left, ColourSample right) => new(left.Channels + right.Channels);

    /// <inheritdoc />
    public static ColourSample operator *(ColourSample left, double right) => new(left.Channels * right);
}
