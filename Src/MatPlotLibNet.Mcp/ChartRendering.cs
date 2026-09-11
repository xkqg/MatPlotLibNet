// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Skia;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet.Mcp;

/// <summary>The output formats the server can produce. Public because these three names ARE the vocabulary the
/// tool schema publishes to the model — the SDK turns the enum into the `enum` of the `format` argument.</summary>
public enum ChartFormat
{
    /// <summary>A raster image the model can look at.</summary>
    Png,

    /// <summary>Vector text, written to a file — inline it would cost a model more tokens than the picture is worth.</summary>
    Svg,

    /// <summary>A print-ready document, written to a file.</summary>
    Pdf,
}

/// <summary>A rendered chart, in memory.</summary>
internal readonly record struct ChartRenderResult(byte[] Bytes, ChartFormat Format, string MediaType, FigureSummary Summary);

/// <summary>A rendered chart, on disk. <see cref="Path"/> is the file that now exists — not the path that was
/// asked for, which the library's own writer would silently turn into something else.</summary>
internal readonly record struct ChartSaveResult(string Path, ChartFormat FormatWritten, long ByteCount, FigureSummary Summary);

/// <summary>Spec in, bytes out — one owner, two sinks. The format is resolved once, here, and the transform picked
/// from it explicitly. The library's <c>figure.Save(path)</c> is deliberately not used: it decides the format from
/// the file extension and writes SVG for any extension it does not recognise, so a PNG asked for under the name
/// <c>chart.jpg</c> becomes an SVG document reported as a success.</summary>
internal sealed class ChartRendering
{
    /// <summary>The only media type that travels inline: the other two formats go to a file.</summary>
    private const string PngMediaType = "image/png";

    private readonly ChartSpecReader _reader;
    private readonly ChartSummarizer _summarizer;

    /// <summary>Creates the renderer over the reader that validates a spec and the summarizer that describes it.</summary>
    public ChartRendering(ChartSpecReader reader, ChartSummarizer summarizer)
    {
        _reader = reader;
        _summarizer = summarizer;

        // The Skia assembly installs the glyph-path provider and the PNG/PDF transforms from a module initializer,
        // which the runtime runs the first time a method NAMING a Skia type is prepared. Forcing it here means the
        // very first chart this process renders is drawn the same way as the thousandth — without it, an SVG asked
        // for before the first PNG would carry <text> where every later one carries glyph outlines.
        System.Runtime.CompilerServices.RuntimeHelpers.RunModuleConstructor(
            typeof(FigureSkiaExtensions).Module.ModuleHandle);
    }

    /// <summary>Renders the spec to bytes the tool result carries inline. Only PNG travels inline.</summary>
    public ChartRenderResult Render(ChartSpec spec, ChartFormat format)
    {
        if (format is not ChartFormat.Png)
        {
            throw new ToolRefusalException(
                $"{format.ToString().ToUpperInvariant()} is too large to return inline; save_chart writes it to a file and returns the path.");
        }

        Figure figure = _reader.Read(spec);
        return new ChartRenderResult(Bytes(figure, format), format, PngMediaType, _summarizer.Describe(figure));
    }

    /// <summary>Renders the spec and writes it to <paramref name="path"/> inside the resolver's root.</summary>
    public ChartSaveResult Save(ChartSpec spec, ChartFormat format, string path, bool overwrite, OutputPathResolver resolver)
    {
        RequireMatchingExtension(format, path);
        string full = resolver.Resolve(path);
        if (!overwrite && File.Exists(full))
        {
            throw new ToolRefusalException($"{full} already exists. Pass overwrite: true to replace it, or choose another name.");
        }

        Figure figure = _reader.Read(spec);
        byte[] bytes = Bytes(figure, format);
        File.WriteAllBytes(full, bytes);
        return new ChartSaveResult(full, format, bytes.LongLength, _summarizer.Describe(figure));
    }

    /// <summary>The format a file extension asks for, for a message that names both.</summary>
    public static ChartFormat? FormatOfExtension(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".png" => ChartFormat.Png,
        ".svg" => ChartFormat.Svg,
        ".pdf" => ChartFormat.Pdf,
        _ => null,
    };

    private static void RequireMatchingExtension(ChartFormat format, string path)
    {
        var extension = Path.GetExtension(path);
        if (FormatOfExtension(path) != format)
        {
            throw new ToolRefusalException(
                $"The format is {format.ToString().ToLowerInvariant()} but the path ends in '{extension}'. "
                + "Name the file with the extension of the format you asked for (.png, .svg or .pdf).");
        }
    }

    private static byte[] Bytes(Figure figure, ChartFormat format)
    {
        IFigureTransform transform = format switch
        {
            ChartFormat.Png => new PngTransform(),
            ChartFormat.Svg => new SvgTransform(),
            _ => new PdfTransform(),
        };

        try
        {
            return figure.Transform(transform).ToBytes();
        }
        catch (Exception ex) when (ex is not ToolRefusalException)
        {
            throw new ToolRefusalException($"The chart could not be rendered: {ex.Message}");
        }
    }
}
