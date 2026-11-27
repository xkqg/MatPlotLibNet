// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>What a Skia transform does with the save options it is handed, held for the PDF and the PNG transform alike: a figure
/// made with no options and with default ones is the same file, and the settings that only an SVG can use leave the file as it was.
/// A transform's own tests derive from this and add what is its own (the title of a PDF, the resolution of a PNG).</summary>
/// <typeparam name="TTransform">The transform under test.</typeparam>
public abstract class TransformOptionsTests<TTransform> where TTransform : FigureTransform, new()
{
    /// <summary>The width of the figure <see cref="Chart"/> makes, in units.</summary>
    protected abstract int Width { get; }

    /// <summary>The height of the figure <see cref="Chart"/> makes, in units.</summary>
    protected abstract int Height { get; }

    /// <summary>The extension a file of this transform is saved under, with its dot.</summary>
    protected abstract string Extension { get; }

    /// <summary>A figure of the size the transform's tests work with, drawn at the resolution given.</summary>
    /// <param name="dpi">The resolution of the figure.</param>
    protected Figure Chart(double dpi = 96) => ChartOf(Width, Height, dpi);

    /// <summary>A figure of the size given, drawn at the resolution given: the one the instance's <see cref="Chart"/> makes, for a
    /// row of data that stands before any instance.</summary>
    /// <param name="width">The width of the figure.</param>
    /// <param name="height">The height of the figure.</param>
    /// <param name="dpi">The resolution of the figure.</param>
    protected static Figure ChartOf(int width, int height, double dpi = 96) =>
        Plt.Create().WithSize(width, height).WithDpi(dpi).Plot([1.0, 2.0, 3.0], [3.0, 1.0, 2.0]).Build();

    /// <summary>The bytes the transform writes for <paramref name="figure"/>, with the options given, or without any.</summary>
    /// <param name="figure">The figure to write.</param>
    /// <param name="options">The save options; <see langword="null"/> writes with none.</param>
    protected static byte[] Make(Figure figure, SaveOptions? options)
    {
        using var stream = new MemoryStream();
        var transform = new TTransform();
        if (options is null) transform.Transform(figure, stream);
        else transform.Transform(figure, stream, options);
        return stream.ToArray();
    }

    /// <summary>The bytes of the file <paramref name="figure"/> is saved to, under the extension of the transform, with the options given.</summary>
    /// <param name="figure">The figure to save.</param>
    /// <param name="options">The save options.</param>
    protected byte[] Saved(Figure figure, SaveOptions options) => SavedAs(Extension, figure, options);

    /// <summary>The bytes of the file <paramref name="figure"/> is saved to under <paramref name="extension"/>, with the options given.</summary>
    /// <param name="extension">The extension of the file, with its dot.</param>
    /// <param name="figure">The figure to save.</param>
    /// <param name="options">The save options.</param>
    protected static byte[] SavedAs(string extension, Figure figure, SaveOptions options)
    {
        string path = Path.Combine(Path.GetTempPath(), $"s9_{Guid.NewGuid():N}{extension}");
        try
        {
            figure.Save(path, options);
            return File.ReadAllBytes(path);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void NoOptions_AndDefaultOptions_WriteTheSameBytes()
    {
        var figure = Chart(dpi: 120);

        Assert.Equal(Make(figure, options: null), Make(figure, new SaveOptions()));
    }

    [Fact]
    public void SvgOnlySettings_LeaveTheFileExactlyAsItWas()
    {
        var figure = Chart();

        Assert.Equal(
            Make(figure, options: null),
            Make(figure, new SaveOptions { PrettifySvg = true, SvgDecimalPrecision = 1 }));
    }
}
