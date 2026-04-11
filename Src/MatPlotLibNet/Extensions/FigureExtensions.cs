// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using MatPlotLibNet.Models;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet;

/// <summary>Extension methods for converting a <see cref="Figure"/> to various output formats.</summary>
public static class FigureExtensions
{
    private const string ExtSvg = ".svg";
    private const string ExtJson = ".json";

    private static readonly ConcurrentDictionary<string, IFigureTransform> TransformRegistry = new()
    {
        [ExtSvg] = new SvgTransform()
    };

    /// <summary>Registers a transform globally for a file extension (e.g., ".png"). Call once at startup.</summary>
    public static void RegisterTransform(string extension, IFigureTransform transform) =>
        TransformRegistry[NormalizeExtension(extension)] = transform;

    /// <summary>Binds the figure to a transform, returning a fluent <see cref="TransformResult"/> for output.</summary>
    public static TransformResult Transform(this Figure figure, IFigureTransform transform) =>
        new(figure, transform);

    /// <summary>Renders the figure as a standalone SVG string.</summary>
    public static string ToSvg(this Figure figure) => ChartServices.SvgRenderer.Render(figure);

    /// <summary>Serializes the figure to JSON.</summary>
    public static string ToJson(this Figure figure, bool indented = false) =>
        ChartServices.Serializer.ToJson(figure, indented);

    /// <summary>Saves the figure as an SVG file.</summary>
    public static void SaveSvg(this Figure figure, string path) =>
        File.WriteAllText(path, figure.ToSvg());

    /// <summary>Saves the figure to a file. Format is auto-detected from the file extension. No extension defaults to SVG.</summary>
    public static void Save(this Figure figure, string path, SaveOptions? options = null)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();

        if (string.IsNullOrEmpty(ext))
        {
            figure.SaveSvg(path + ExtSvg);
            return;
        }

        if (ext == ExtJson)
        {
            File.WriteAllText(path, figure.ToJson());
            return;
        }

        if (TransformRegistry.TryGetValue(ext, out var transform))
        {
            figure.Transform(transform).ToFile(path);
            return;
        }

        figure.SaveSvg(path);
    }

    private static string NormalizeExtension(string extension) =>
        extension.StartsWith('.') ? extension.ToLowerInvariant() : $".{extension.ToLowerInvariant()}";
}
