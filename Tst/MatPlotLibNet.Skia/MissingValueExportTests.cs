// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>A composed chart with a missing value (NaN) in its data exports to PNG and PDF. The renderers skip every
/// mark at a coordinate that cannot be drawn before it reaches a render context, so the raster and PDF backends are only
/// ever handed finite geometry.</summary>
public class MissingValueExportTests
{
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    [Theory]
    [MemberData(nameof(ComposedChartShapes.Names), MemberType = typeof(ComposedChartShapes))]
    public void AComposedChartWithAMissingValue_ExportsToPng(string name)
    {
        byte[] png = ComposedChartShapes.Named(name).Build(true).Build().ToPng();

        Assert.Equal(PngSignature, png[..PngSignature.Length]);
    }

    [Theory]
    [MemberData(nameof(ComposedChartShapes.Names), MemberType = typeof(ComposedChartShapes))]
    public void AComposedChartWithAMissingValue_ExportsToPdf(string name)
    {
        byte[] pdf = ComposedChartShapes.Named(name).Build(true).Build().ToPdf();

        Assert.Equal("%PDF"u8.ToArray(), pdf[..4]);
    }
}
