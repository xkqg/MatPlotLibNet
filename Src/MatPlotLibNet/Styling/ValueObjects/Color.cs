// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>Represents an RGBA color value.</summary>
/// <param name="R">Red component (0–255).</param>
/// <param name="G">Green component (0–255).</param>
/// <param name="B">Blue component (0–255).</param>
/// <param name="A">Alpha component (0–255); defaults to 255 (fully opaque).</param>
/// <remarks>Named color constants are available on <see cref="Colors"/>.</remarks>
public readonly record struct Color(byte R, byte G, byte B, byte A = 255)
{
    /// <summary>Creates a <see cref="Color"/> from a hexadecimal string.</summary>
    /// <param name="hex">A 6- or 8-digit hex string, optionally prefixed with '#'.</param>
    /// <returns>The parsed color.</returns>
    /// <exception cref="FormatException">Thrown when the hex string is not a valid format.</exception>
    public static Color FromHex(string hex)
    {
        var span = hex.AsSpan();
        if (span.Length > 0 && span[0] == '#')
            span = span[1..];

        if (span.Length == 6)
            return new Color(ParseHexByte(span[0..2]), ParseHexByte(span[2..4]), ParseHexByte(span[4..6]));

        if (span.Length == 8)
            return new Color(ParseHexByte(span[0..2]), ParseHexByte(span[2..4]), ParseHexByte(span[4..6]), ParseHexByte(span[6..8]));

        throw new FormatException($"Invalid hex color format: '{hex}'. Expected 6 or 8 hex digits, optionally prefixed with '#'.");
    }

    /// <summary>Creates a <see cref="Color"/> from normalized RGBA values in the range [0, 1].</summary>
    /// <param name="r">Red component (0.0 to 1.0).</param>
    /// <param name="g">Green component (0.0 to 1.0).</param>
    /// <param name="b">Blue component (0.0 to 1.0).</param>
    /// <param name="a">Alpha component (0.0 to 1.0); defaults to 1.0 (fully opaque).</param>
    /// <returns>The constructed color.</returns>
    public static Color FromRgba(double r, double g, double b, double a = 1.0) =>
        new(
            (byte)Math.Round(Math.Clamp(r, 0, 1) * 255),
            (byte)Math.Round(Math.Clamp(g, 0, 1) * 255),
            (byte)Math.Round(Math.Clamp(b, 0, 1) * 255),
            (byte)Math.Round(Math.Clamp(a, 0, 1) * 255));

    /// <summary>Returns a new color with the same RGB values and the specified alpha.</summary>
    /// <param name="alpha">The alpha byte value.</param>
    /// <returns>A new color with the given alpha.</returns>
    public Color WithAlpha(byte alpha) => new(R, G, B, alpha);

    /// <summary>Converts the color to a hexadecimal string in the format #RRGGBB.</summary>
    /// <returns>The hex representation of the color.</returns>
    public string ToHex() => $"#{R:X2}{G:X2}{B:X2}";

    /// <summary>Converts the color to an RGBA CSS string in the format rgba(R,G,B,A).</summary>
    /// <returns>The RGBA string representation.</returns>
    public string ToRgbaString() => $"rgba({R},{G},{B},{A / 255.0:F2})";

    private static byte ParseHexByte(ReadOnlySpan<char> hex) =>
        byte.Parse(hex, System.Globalization.NumberStyles.HexNumber);
}
