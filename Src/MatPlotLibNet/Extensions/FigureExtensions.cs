// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet;

/// <summary>Extension methods for converting a <see cref="Figure"/> to various output formats.</summary>
public static class FigureExtensions
{
    /// <summary>Binds the figure to a transform, returning a fluent <see cref="TransformResult"/> for output.</summary>
    /// <param name="figure">The figure to transform.</param>
    /// <param name="transform">The transform to apply (e.g., <c>new SvgTransform()</c>, <c>new PngTransform()</c>).</param>
    /// <returns>A <see cref="TransformResult"/> with <c>ToStream()</c>, <c>ToFile()</c>, and <c>ToBytes()</c> methods.</returns>
    public static TransformResult Transform(this Figure figure, IFigureTransform transform) =>
        new(figure, transform);

    /// <summary>Renders the figure as a standalone SVG string.</summary>
    public static string ToSvg(this Figure figure) => ChartServices.SvgRenderer.Render(figure);

    /// <summary>Serializes the figure to JSON.</summary>
    public static string ToJson(this Figure figure, bool indented = false) =>
        ChartServices.Serializer.ToJson(figure, indented);
}
