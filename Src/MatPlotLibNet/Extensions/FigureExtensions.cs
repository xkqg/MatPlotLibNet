// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet;

/// <summary>Extension methods for converting a <see cref="Figure"/> to various output formats.</summary>
public static class FigureExtensions
{
    /// <summary>Renders the figure as a standalone SVG string (e.g., <c>figure.ToSvg()</c>).</summary>
    /// <returns>A complete SVG document as a string.</returns>
    public static string ToSvg(this Figure figure) => ChartServices.SvgRenderer.Render(figure);

    /// <summary>Serializes the figure to JSON (e.g., <c>figure.ToJson(indented: true)</c>).</summary>
    /// <param name="figure">The figure to serialize.</param>
    /// <param name="indented">Whether to produce indented JSON output.</param>
    /// <returns>A JSON string representing the figure.</returns>
    public static string ToJson(this Figure figure, bool indented = false) =>
        ChartServices.Serializer.ToJson(figure, indented);
}
