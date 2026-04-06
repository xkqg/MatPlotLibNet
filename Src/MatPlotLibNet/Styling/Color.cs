// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>
/// Represents an RGBA color value.
/// </summary>
public readonly record struct Color(byte R, byte G, byte B, byte A = 255)
{
    // Named colors

    /// <summary>Gets the color red (255, 0, 0).</summary>
    public static Color Red => new(255, 0, 0);

    /// <summary>Gets the color green (0, 128, 0).</summary>
    public static Color Green => new(0, 128, 0);

    /// <summary>Gets the color blue (0, 0, 255).</summary>
    public static Color Blue => new(0, 0, 255);

    /// <summary>Gets the color white (255, 255, 255).</summary>
    public static Color White => new(255, 255, 255);

    /// <summary>Gets the color black (0, 0, 0).</summary>
    public static Color Black => new(0, 0, 0);

    /// <summary>Gets the color yellow (255, 255, 0).</summary>
    public static Color Yellow => new(255, 255, 0);

    /// <summary>Gets the color cyan (0, 255, 255).</summary>
    public static Color Cyan => new(0, 255, 255);

    /// <summary>Gets the color magenta (255, 0, 255).</summary>
    public static Color Magenta => new(255, 0, 255);

    /// <summary>Gets the color orange (255, 165, 0).</summary>
    public static Color Orange => new(255, 165, 0);

    /// <summary>Gets the color gray (128, 128, 128).</summary>
    public static Color Gray => new(128, 128, 128);

    /// <summary>Gets the color light gray (211, 211, 211).</summary>
    public static Color LightGray => new(211, 211, 211);

    /// <summary>Gets the color dark gray (64, 64, 64).</summary>
    public static Color DarkGray => new(64, 64, 64);

    /// <summary>Gets a fully transparent color (0, 0, 0, 0).</summary>
    public static Color Transparent => new(0, 0, 0, 0);

    // Matplotlib Tab10 named colors (frequently used as defaults)

    /// <summary>Gets the matplotlib default blue (#1f77b4).</summary>
    public static Color Tab10Blue => new(0x1F, 0x77, 0xB4);

    /// <summary>Gets the matplotlib default orange (#ff7f0e).</summary>
    public static Color Tab10Orange => new(0xFF, 0x7F, 0x0E);

    /// <summary>Gets the matplotlib default green (#2ca02c).</summary>
    public static Color Tab10Green => new(0x2C, 0xA0, 0x2C);

    // Common rendering colors

    /// <summary>Gets the default grid color (#CCCCCC).</summary>
    public static Color GridGray => new(0xCC, 0xCC, 0xCC);

    /// <summary>Gets the default 3D edge color (#666666).</summary>
    public static Color EdgeGray => new(0x66, 0x66, 0x66);

    /// <summary>Gets the amber/warning color (#FFC107).</summary>
    public static Color Amber => new(0xFF, 0xC1, 0x07);

    /// <summary>Gets the Fibonacci orange (#FF9800).</summary>
    public static Color FibonacciOrange => new(0xFF, 0x98, 0x00);

    /// <summary>
    /// Creates a <see cref="Color"/> from a hexadecimal string.
    /// </summary>
    /// <param name="hex">A 6- or 8-digit hex string, optionally prefixed with '#'.</param>
    /// <returns>The parsed color.</returns>
    /// <exception cref="FormatException">Thrown when the hex string is not a valid format.</exception>
    public static Color FromHex(string hex)
    {
        var span = hex.AsSpan();
        if (span.Length > 0 && span[0] == '#')
            span = span[1..];

        if (span.Length == 6)
        {
            return new Color(
                ParseHexByte(span[0..2]),
                ParseHexByte(span[2..4]),
                ParseHexByte(span[4..6]));
        }

        if (span.Length == 8)
        {
            return new Color(
                ParseHexByte(span[0..2]),
                ParseHexByte(span[2..4]),
                ParseHexByte(span[4..6]),
                ParseHexByte(span[6..8]));
        }

        throw new FormatException($"Invalid hex color format: '{hex}'. Expected 6 or 8 hex digits, optionally prefixed with '#'.");
    }

    /// <summary>
    /// Creates a <see cref="Color"/> from normalized RGBA values in the range [0, 1].
    /// </summary>
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

    /// <summary>
    /// Returns a new color with the same RGB values and the specified alpha.
    /// </summary>
    /// <param name="alpha">The alpha byte value.</param>
    /// <returns>A new color with the given alpha.</returns>
    public Color WithAlpha(byte alpha) => new(R, G, B, alpha);

    /// <summary>
    /// Converts the color to a hexadecimal string in the format #RRGGBB.
    /// </summary>
    /// <returns>The hex representation of the color.</returns>
    public string ToHex() => $"#{R:X2}{G:X2}{B:X2}";

    /// <summary>
    /// Converts the color to an RGBA string in the format rgba(R,G,B,A).
    /// </summary>
    /// <returns>The RGBA string representation.</returns>
    public string ToRgbaString() => $"rgba({R},{G},{B},{A / 255.0:F2})";

    private static byte ParseHexByte(ReadOnlySpan<char> hex) =>
        byte.Parse(hex, System.Globalization.NumberStyles.HexNumber);
}
