// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>The three colours a theme lays everything on: the figure behind it all, the plot behind the data, and the ink of text and
/// foreground marks. A preset that recolours its base names them as one value (<see cref="ThemeBuilder.WithGround"/>).</summary>
/// <param name="Background">The colour behind the whole figure (<see cref="Theme.Background"/>).</param>
/// <param name="AxesBackground">The fill of the plot area (<see cref="Theme.AxesBackground"/>).</param>
/// <param name="ForegroundText">The colour of text and foreground marks (<see cref="Theme.ForegroundText"/>).</param>
internal readonly record struct ThemeGround(Color Background, Color AxesBackground, Color ForegroundText)
{
    /// <summary>The ground written as three hex colours, the way a palette is published.</summary>
    /// <param name="background">The figure's colour, such as <c>#2E3440</c>.</param>
    /// <param name="axesBackground">The plot's colour.</param>
    /// <param name="foregroundText">The ink.</param>
    /// <returns>The ground.</returns>
    internal static ThemeGround Hex(string background, string axesBackground, string foregroundText) =>
        new(Color.FromHex(background), Color.FromHex(axesBackground), Color.FromHex(foregroundText));
}
