// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling.ColorMaps;

/// <summary>
/// Provides cyclic color maps where the start and end colors match,
/// suitable for periodic data such as phase angles or time of day.
/// </summary>
public static class CyclicColorMaps
{
    /// <summary>Gets the Twilight color map (symmetric purple-orange-purple).</summary>
    public static IColorMap Twilight { get; } = new LerpColorMap("twilight",
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

    /// <summary>Gets the Twilight Shifted color map (shifted version starting from dark).</summary>
    public static IColorMap TwilightShifted { get; } = new LerpColorMap("twilight_shifted",
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

    /// <summary>Gets the HSV color map (full hue cycle: red → yellow → green → cyan → blue → magenta → red).</summary>
    public static IColorMap Hsv { get; } = new LerpColorMap("hsv",
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
