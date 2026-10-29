// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet;

/// <summary>Extension methods for converting a <see cref="Figure"/> to various output formats.</summary>
public static class FigureExtensions
{
    // The registry starts with the formats core writes itself (SVG) and a refusal for each format it knows but cannot
    // write: .png and .pdf until MatPlotLibNet.Skia registers its exporters over them, .gif for good.
    private static readonly ConcurrentDictionary<string, IFigureTransform> TransformRegistry = new()
    {
        [ExportExtension.Svg] = new SvgTransform(),
        [ExportExtension.Png] = RefusingTransform.WithoutSkia(ExportExtension.Png),
        [ExportExtension.Pdf] = RefusingTransform.WithoutSkia(ExportExtension.Pdf),
        [ExportExtension.Gif] = RefusingTransform.ForAnimation(),
    };

    /// <summary>Registers a transform globally for a file extension (e.g., ".png"). Call once at startup.</summary>
    public static void RegisterTransform(string extension, IFigureTransform transform) =>
        TransformRegistry[NormalizeExtension(extension)] = transform;

    extension(Figure figure)
    {
        /// <summary>Binds the figure to a transform, returning a fluent <see cref="TransformResult"/> for output.</summary>
        public TransformResult Transform(IFigureTransform transform) =>
            new(figure, transform);

        /// <summary>Renders the figure as a standalone SVG string.</summary>
        public string ToSvg() => SvgRendererDefault.Current.Render(figure);

        /// <summary>The figure's accessible name — what the SVG <c>&lt;title&gt;</c> says and what the data table's
        /// caption says, from one rule: the <see cref="Figure.AltText"/> an author wrote, else the
        /// <see cref="Figure.Title"/>, else, for a tile row that deliberately carries no title, the labels of its
        /// <see cref="Models.Series.StatTileSeries"/> joined by " · ". Empty when none of the three names it.</summary>
        public string AccessibleName()
        {
            if (figure.AltText is not null) return figure.AltText;
            if (figure.Title is not null) return figure.Title;
            var labels = new List<string>();
            foreach (var axes in figure.SubPlots)
                foreach (var series in axes.Series)
                    if (series is Models.Series.StatTileSeries { Label: { Length: > 0 } label })
                        labels.Add(label);
            return string.Join(" · ", labels);
        }

        /// <summary>Serializes the figure to JSON.</summary>
        public string ToJson(bool indented = false) =>
            ChartSerializerDefault.Current.ToJson(figure, indented);

        /// <summary>Saves the figure as an SVG file.</summary>
        public void SaveSvg(string path) =>
            File.WriteAllText(path, figure.ToSvg());

        /// <summary>Saves the figure to a file. The format follows the extension of <paramref name="path"/>: .json writes the
        /// figure as JSON, .svg an SVG, and with <c>MatPlotLibNet.Skia</c> loaded .png a PNG and .pdf a PDF. Without it a .png
        /// or .pdf is refused, and a .gif always is (an animation is saved from its <c>AnimationBuilder</c>), because an SVG
        /// document under one of those names would open as a broken file. A path with no extension gets ".svg" added, and
        /// one with an extension no format claims is written as an SVG.
        /// <para><paramref name="options"/> hands the format the settings it can use: the resolution sizes a PNG, the
        /// indentation and the number of decimals shape an SVG, the title and the author go into an SVG, a PNG and a PDF.
        /// Settings a format cannot use leave its file alone (see <see cref="SaveOptions"/> for each); JSON uses none of
        /// them. Without options the file is the figure's own.</para>
        /// <para>The file is written only when the export has succeeded, so a save that fails leaves the file that was
        /// there.</para></summary>
        /// <exception cref="ArgumentOutOfRangeException">A setting of <paramref name="options"/> is outside what its
        /// format accepts (for example <see cref="SaveOptions.SvgDecimalPrecision"/> outside 0 to 15), or the resolution
        /// of a PNG is not positive.</exception>
        /// <exception cref="InvalidOperationException">The extension is .png or .pdf and <c>MatPlotLibNet.Skia</c> is not
        /// loaded, or it is .gif. Nothing is written and a file that was there stays.</exception>
        public void Save(string path, SaveOptions? options = null)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();

            if (string.IsNullOrEmpty(ext))
            {
                WriteSvgFile(figure, path + ExportExtension.Svg, options);
                return;
            }

            if (ext == ExportExtension.Json)
            {
                File.WriteAllText(path, figure.ToJson());
                return;
            }

            if (TransformRegistry.TryGetValue(ext, out var transform))
            {
                using var buffer = new MemoryStream();
                if (options is null)
                    transform.Transform(figure, buffer);
                else
                    transform.Transform(figure, buffer, options);

                using var file = File.Create(path);
                buffer.WriteTo(file);
                return;
            }

            WriteSvgFile(figure, path, options);
        }
    }

    private static void WriteSvgFile(Figure figure, string path, SaveOptions? options)
    {
        string svg = figure.ToSvg();
        File.WriteAllText(path, options is null ? svg : svg.WithSaveOptions(options));
    }

    private static string NormalizeExtension(string extension) =>
        extension.StartsWith('.') ? extension.ToLowerInvariant() : $".{extension.ToLowerInvariant()}";
}
