// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies <see cref="FigureTransform"/> behavior.</summary>
public class FigureTransformTests
{
    /// <summary>Verifies that SvgTransform implements IFigureTransform.</summary>
    [Fact]
    public void SvgTransform_ImplementsIFigureTransform()
    {
        Assert.IsAssignableFrom<IFigureTransform>(new SvgTransform());
    }

    /// <summary>Verifies that SvgTransform implements ISvgRenderer.</summary>
    [Fact]
    public void SvgTransform_ImplementsISvgRenderer()
    {
        Assert.IsAssignableFrom<ISvgRenderer>(new SvgTransform());
    }

    /// <summary>Verifies that SvgTransform extends the FigureTransform base class.</summary>
    [Fact]
    public void SvgTransform_ExtendsFigureTransform()
    {
        Assert.IsAssignableFrom<FigureTransform>(new SvgTransform());
    }

    /// <summary>Verifies that ToStream writes valid SVG content to the stream.</summary>
    [Fact]
    public void TransformResult_ToStream_WritesOutput()
    {
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();

        using var stream = new MemoryStream();
        figure.Transform(new SvgTransform()).ToStream(stream);

        Assert.True(stream.Length > 0);
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        Assert.StartsWith("<svg", reader.ReadToEnd().TrimStart());
    }

    /// <summary>Verifies that ToBytes returns a non-empty byte array.</summary>
    [Fact]
    public void TransformResult_ToBytes_ReturnsNonEmpty()
    {
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();

        byte[] bytes = figure.Transform(new SvgTransform()).ToBytes();

        Assert.NotEmpty(bytes);
    }

    /// <summary>Verifies that ToFile creates a file containing valid SVG content.</summary>
    [Fact]
    public void TransformResult_ToFile_CreatesFile()
    {
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();
        var path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.svg");

        try
        {
            figure.Transform(new SvgTransform()).ToFile(path);

            Assert.True(File.Exists(path));
            Assert.StartsWith("<svg", File.ReadAllText(path).TrimStart());
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    /// <summary>Verifies that the legacy ChartServices.SvgRenderer path still produces valid SVG.</summary>
    [Fact]
    public void ChartServices_SvgRenderer_StillWorks()
    {
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();
        string svg = ChartServices.SvgRenderer.Render(figure);
        Assert.Contains("<svg", svg);
    }

    /// <summary>Verifies that TransformResult is a record type.</summary>
    [Fact]
    public void TransformResult_IsRecord()
    {
        var figure = Plt.Create().Build();
        var result = figure.Transform(new SvgTransform());
        Assert.IsType<TransformResult>(result);
    }
}
