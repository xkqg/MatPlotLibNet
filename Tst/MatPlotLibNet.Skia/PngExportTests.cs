// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet;
using MatPlotLibNet.Models;
using MatPlotLibNet.Skia;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>What <see cref="PngTransform"/> writes: a PNG picture, to bytes, a stream or a file.</summary>
public class PngExportTests : TransformExportTests<PngTransform>
{
    /// <inheritdoc/>
    protected override byte[] Export(Figure figure) => figure.ToPng();

    /// <inheritdoc/>
    protected override byte[] Magic => [0x89, 0x50, 0x4E, 0x47];

    [Fact]
    public void FluentTransform_ToStream_WritesValidPng()
    {
        using var stream = new MemoryStream();
        TwoPoints().Transform(new PngTransform()).ToStream(stream);

        Assert.True(stream.Length > 0);
    }

    [Fact]
    public void FluentTransform_ToFile_CreatesValidPng()
    {
        var path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.png");

        try
        {
            TwoPoints().Transform(new PngTransform()).ToFile(path);

            Assert.True(File.Exists(path));
            var bytes = File.ReadAllBytes(path);
            Assert.Equal(Magic, bytes[..Magic.Length]);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
