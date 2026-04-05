// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling.ColorMaps;

internal sealed class LerpColorMap : IColorMap
{
    /// <inheritdoc />
    public string Name { get; }
    private readonly Color[] _stops;

    internal LerpColorMap(string name, Color[] stops)
    {
        Name = name;
        _stops = stops;
    }

    /// <inheritdoc />
    public Color GetColor(double value)
    {
        value = Math.Clamp(value, 0, 1);

        if (_stops.Length == 1)
            return _stops[0];

        double scaled = value * (_stops.Length - 1);
        int lower = (int)Math.Floor(scaled);
        int upper = Math.Min(lower + 1, _stops.Length - 1);
        double t = scaled - lower;

        var a = _stops[lower];
        var b = _stops[upper];

        return new Color(
            Lerp(a.R, b.R, t),
            Lerp(a.G, b.G, t),
            Lerp(a.B, b.B, t),
            Lerp(a.A, b.A, t));
    }

    private static byte Lerp(byte a, byte b, double t) =>
        (byte)Math.Round(a + (b - a) * t);
}
