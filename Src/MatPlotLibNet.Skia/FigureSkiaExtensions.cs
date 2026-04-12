// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet.Skia;

/// <summary>Convenience extension methods for PNG and PDF export.</summary>
public static class FigureSkiaExtensions
{
    private static readonly PngTransform Png = new();
    private static readonly PdfTransform Pdf = new();

    /// <summary>Exports the figure to a PNG byte array.</summary>
    public static byte[] ToPng(this Figure figure) => figure.Transform(Png).ToBytes();

    /// <summary>Exports the figure to a PDF byte array.</summary>
    public static byte[] ToPdf(this Figure figure) => figure.Transform(Pdf).ToBytes();
}
