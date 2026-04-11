// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling.ColorMaps;

/// <summary>Colormap that linearly interpolates between evenly-spaced (or explicitly positioned) color stops.</summary>
public sealed class LinearColorMap : IColorMap
{
    /// <inheritdoc />
    public string Name { get; }

    private readonly Color[] _stops;
    private readonly double[]? _positions; // null → evenly spaced

    /// <summary>Gets or sets the color for values below the minimum. <c>null</c> clamps to the first stop.</summary>
    public Color? UnderColor { get; init; }

    /// <summary>Gets or sets the color for values above the maximum. <c>null</c> clamps to the last stop.</summary>
    public Color? OverColor { get; init; }

    /// <summary>Gets or sets the color for masked/invalid (NaN) values.</summary>
    public Color? BadColor { get; init; }

    /// <summary>Creates a colormap that interpolates between evenly-spaced color stops.</summary>
    public LinearColorMap(string name, Color[] stops)
    {
        Name = name;
        _stops = stops;
    }

    private LinearColorMap(string name, Color[] stops, double[] positions)
    {
        Name = name;
        _stops = stops;
        _positions = positions;
    }

    /// <summary>Creates a colormap with explicitly positioned stops. Positions must be strictly increasing in [0, 1].</summary>
    public static LinearColorMap FromPositions(string name, (double Position, Color Color)[] stops)
    {
        if (stops.Length < 2)
            throw new ArgumentException("At least 2 stops required.", nameof(stops));
        for (int i = 1; i < stops.Length; i++)
            if (stops[i].Position <= stops[i - 1].Position)
                throw new ArgumentException("Positions must be strictly increasing.", nameof(stops));
        return new(name,
            stops.Select(s => s.Color).ToArray(),
            stops.Select(s => s.Position).ToArray());
    }

    /// <inheritdoc />
    public Color GetColor(double value)
    {
        value = Math.Clamp(value, 0, 1);

        if (_positions is null)
        {
            double scaled = value * (_stops.Length - 1);
            int lower = (int)Math.Floor(scaled);
            int upper = Math.Min(lower + 1, _stops.Length - 1);
            return Lerp(_stops[lower], _stops[upper], scaled - lower);
        }

        // Binary search for bracketing stops
        int lo = 0, hi = _positions.Length - 2;
        while (lo < hi)
        {
            int mid = (lo + hi + 1) / 2;
            if (_positions[mid] <= value) lo = mid; else hi = mid - 1;
        }
        double span = _positions[lo + 1] - _positions[lo];
        double t = span == 0 ? 0 : (value - _positions[lo]) / span;
        return Lerp(_stops[lo], _stops[lo + 1], t);
    }

    /// <inheritdoc />
    Color? IColorMap.GetUnderColor() => UnderColor;

    /// <inheritdoc />
    Color? IColorMap.GetOverColor() => OverColor;

    /// <inheritdoc />
    Color? IColorMap.GetBadColor() => BadColor;

    private static Color Lerp(Color a, Color b, double t) => new(
        (byte)Math.Round(a.R + (b.R - a.R) * t),
        (byte)Math.Round(a.G + (b.G - a.G) * t),
        (byte)Math.Round(a.B + (b.B - a.B) * t),
        (byte)Math.Round(a.A + (b.A - a.A) * t));
}
