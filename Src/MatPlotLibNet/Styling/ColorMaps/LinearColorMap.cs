// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Styling.ColorMaps;

/// <summary>Colormap that linearly interpolates between evenly-spaced (or explicitly positioned) color stops.</summary>
/// <remarks>A map built in code travels through the figure JSON as what it is made of (its colours, their positions and
/// its under, over and bad colours), so it comes back from <c>FromJson</c>, the MCP server and the transports as itself;
/// a map the library ships travels by its name.</remarks>
public sealed class LinearColorMap : IColorMap, IJsonDescribed<ColorMapDto>
{
    /// <inheritdoc />
    ColorMapDto IJsonDescribed<ColorMapDto>.Describe() => new LinearColorMapDto(Name, _stops, _positions, UnderColor, OverColor, BadColor);

    public string Name { get; }

    private readonly Color[] _stops;
    private readonly double[]? _positions; // null → evenly spaced

    public Color? UnderColor { get; init; }

    public Color? OverColor { get; init; }

    public Color? BadColor { get; init; }

    /// <summary>Creates a colormap that interpolates between evenly-spaced color stops.</summary>
    public LinearColorMap(string name, Color[] stops)
    {
        Name = name;
        _stops = stops;
    }

    /// <summary>Creates a colormap from its stops, at <paramref name="positions"/> or evenly spaced when that is
    /// <see langword="null"/>. The callers have checked the stops (<see cref="ThrowIfNotStops"/>).</summary>
    internal LinearColorMap(string name, Color[] stops, double[]? positions)
    {
        Name = name;
        _stops = stops;
        _positions = positions;
    }

    /// <summary>The rule positioned stops keep: at least two, strictly increasing.</summary>
    /// <param name="positions">The stops' positions.</param>
    /// <param name="paramName">The parameter the exception names.</param>
    /// <exception cref="ArgumentException">Fewer than 2 positions, or positions that do not strictly increase.</exception>
    internal static void ThrowIfNotStops(IReadOnlyList<double> positions, string paramName)
    {
        if (positions.Count < 2)
            throw new ArgumentException("At least 2 stops required.", paramName);
        for (int i = 1; i < positions.Count; i++)
            if (positions[i] <= positions[i - 1])
                throw new ArgumentException("Positions must be strictly increasing.", paramName);
    }

    /// <summary>Creates a colormap from a list of <see cref="ColorStop"/> stops and auto-registers it
    /// in <see cref="ColorMapRegistry"/> under <paramref name="name"/> and its reversed variant
    /// (<c>{name}_r</c>). Positions are auto-normalised to <c>[0, 1]</c> if the range is not already
    /// <c>[0, 1]</c>.</summary>
    /// <param name="name">Registry name. Must be non-empty; existing registrations with the same name are overwritten.</param>
    /// <param name="colors">At least 2 <see cref="ColorStop"/> entries with strictly increasing positions.
    /// Positions need not start at 0 or end at 1 — they are normalised automatically.</param>
    /// <returns>The newly created <see cref="LinearColorMap"/>, already registered under both
    /// <paramref name="name"/> and <c>{name}_r</c>.</returns>
    /// <exception cref="ArgumentException">Fewer than 2 stops, non-increasing positions, or <paramref name="name"/> is null/whitespace.</exception>
    public static LinearColorMap FromList(string name, IReadOnlyList<ColorStop> colors)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name must not be empty.", nameof(name));
        if (colors.Count < 2)
            throw new ArgumentException("At least 2 stops required.", nameof(colors));

        double minPos = colors[0].Position;
        double maxPos = colors[colors.Count - 1].Position;

        for (int i = 1; i < colors.Count; i++)
            if (colors[i].Position <= colors[i - 1].Position)
                throw new ArgumentException("Positions must be strictly increasing.", nameof(colors));

        double range = maxPos - minPos;

        double[] positions = range == 0
            ? colors.Select((_, i) => (double)i / (colors.Count - 1)).ToArray()
            : colors.Select(c => (c.Position - minPos) / range).ToArray();

        Color[] stops = colors.Select(c => c.Color).ToArray();
        var map = new LinearColorMap(name, stops, positions);
        ColorMapRegistry.RegisterWithItsReverse(map);
        return map;
    }

    /// <summary>Creates a colormap with explicitly positioned <see cref="ColorStop"/> stops. Unlike
    /// <see cref="FromList"/> this overload does <b>not</b> normalise — positions must already be
    /// strictly increasing within <c>[0, 1]</c>.</summary>
    /// <param name="name">Colormap name (not registered with <see cref="ColorMapRegistry"/>).</param>
    /// <param name="stops">At least 2 stops with strictly increasing positions in <c>[0, 1]</c>.</param>
    /// <returns>The newly created colormap.</returns>
    /// <exception cref="ArgumentException">Fewer than 2 stops or non-increasing positions.</exception>
    public static LinearColorMap FromPositions(string name, ColorStop[] stops)
    {
        ThrowIfNotStops([.. stops.Select(s => s.Position)], nameof(stops));
        return new(name,
            stops.Select(s => s.Color).ToArray(),
            stops.Select(s => s.Position).ToArray());
    }

    /// <inheritdoc />
    public Color GetColor(double value)
    {
        // A stop index is never computed from NaN: on net8.0 the cast gives int.MinValue and the lookup threw.
        if (value.IsMissing()) return this.MissingValueColor();
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
