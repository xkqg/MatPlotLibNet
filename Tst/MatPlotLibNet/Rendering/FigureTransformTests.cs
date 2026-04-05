// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet.Tests.Rendering;

public class FigureTransformTests
{
    [Fact]
    public void SvgTransform_ImplementsIFigureTransform()
    {
        Assert.IsAssignableFrom<IFigureTransform>(new SvgTransform());
    }

    [Fact]
    public void SvgTransform_ImplementsISvgRenderer()
    {
        Assert.IsAssignableFrom<ISvgRenderer>(new SvgTransform());
    }

    [Fact]
    public void SvgTransform_ExtendsFigureTransform()
    {
        Assert.IsAssignableFrom<FigureTransform>(new SvgTransform());
    }

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

    [Fact]
    public void TransformResult_ToBytes_ReturnsNonEmpty()
    {
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();

        byte[] bytes = figure.Transform(new SvgTransform()).ToBytes();

        Assert.NotEmpty(bytes);
    }

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

    [Fact]
    public void ChartServices_SvgRenderer_StillWorks()
    {
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();
        string svg = ChartServices.SvgRenderer.Render(figure);
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void TransformResult_IsRecord()
    {
        var figure = Plt.Create().Build();
        var result = figure.Transform(new SvgTransform());
        Assert.IsType<TransformResult>(result);
    }
}
