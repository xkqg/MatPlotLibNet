// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling.ColorMaps;

/// <summary>
/// Provides cyclic color maps where the start and end colors match,
/// suitable for periodic data such as phase angles or time of day.
/// </summary>
public static class CyclicColorMaps
{
    /// <summary>Pale lavender through blue-purple to a near-black purple, then brown and tan back to the pale start colour (after matplotlib's <c>twilight</c>).</summary>
    public static IColorMap Twilight { get; } = new LinearColorMap("twilight",
    [
        Color.FromHex("#E2D9E2"),
        Color.FromHex("#9E9CC2"),
        Color.FromHex("#5C5698"),
        Color.FromHex("#2D1E3E"),
        Color.FromHex("#6C3624"),
        Color.FromHex("#BA7B54"),
        Color.FromHex("#E2C8A4"),
        Color.FromHex("#E2D9E2"),
    ]);

    /// <summary>The <see cref="Twilight"/> cycle rotated by half a turn: it starts and ends at the dark purple and is palest in the middle (after matplotlib's <c>twilight_shifted</c>).</summary>
    public static IColorMap TwilightShifted { get; } = new LinearColorMap("twilight_shifted",
    [
        Color.FromHex("#2D1E3E"),
        Color.FromHex("#6C3624"),
        Color.FromHex("#BA7B54"),
        Color.FromHex("#E2C8A4"),
        Color.FromHex("#E2D9E2"),
        Color.FromHex("#9E9CC2"),
        Color.FromHex("#5C5698"),
        Color.FromHex("#2D1E3E"),
    ]);

    /// <summary>The hue wheel at full saturation: red, yellow, green, cyan, blue and magenta, then back to red (after matplotlib's <c>hsv</c>).</summary>
    public static IColorMap Hsv { get; } = new LinearColorMap("hsv",
    [
        Color.FromHex("#FF0000"),
        Color.FromHex("#FFFF00"),
        Color.FromHex("#00FF00"),
        Color.FromHex("#00FFFF"),
        Color.FromHex("#0000FF"),
        Color.FromHex("#FF00FF"),
        Color.FromHex("#FF0000"),
    ]);
}
