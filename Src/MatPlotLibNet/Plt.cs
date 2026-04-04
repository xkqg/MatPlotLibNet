// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet;

/// <summary>Top-level entry point for creating figures, modeled after matplotlib's pyplot interface.</summary>
public static class Plt
{
    /// <summary>Creates a new <see cref="Models.Figure"/> with the specified dimensions.</summary>
    /// <param name="width">The figure width in pixels.</param>
    /// <param name="height">The figure height in pixels.</param>
    /// <returns>A new figure instance.</returns>
    public static Figure Figure(double width = 800, double height = 600) =>
        new() { Width = width, Height = height };

    /// <summary>Creates a new <see cref="FigureBuilder"/> for fluent figure construction (e.g., <c>Plt.Create().WithTitle("My Chart").Plot(x, y).Build()</c>).</summary>
    /// <returns>A new fluent figure builder.</returns>
    public static FigureBuilder Create() => new();
}
