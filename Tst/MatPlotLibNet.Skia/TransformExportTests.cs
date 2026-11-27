// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>What a Skia export transform is, held for the PDF and the PNG transform alike: a figure transform, whose export of a
/// figure with data is a non-empty file that starts with the magic bytes of its format, whether asked for by the figure's own
/// extension or by the fluent transform. A transform's own tests derive from this and add what is its own (pages, a stream, a file).</summary>
/// <typeparam name="TTransform">The transform under test.</typeparam>
public abstract class TransformExportTests<TTransform> where TTransform : FigureTransform, new()
{
    /// <summary>The bytes the figure's own export verb (<c>ToPdf</c>, <c>ToPng</c>) writes for <paramref name="figure"/>.</summary>
    /// <param name="figure">The figure to export.</param>
    protected abstract byte[] Export(Figure figure);

    /// <summary>The first bytes of every file of the format.</summary>
    protected abstract byte[] Magic { get; }

    /// <summary>A figure with two points and no title.</summary>
    protected static Figure TwoPoints() => Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();

    [Theory]
    [InlineData(typeof(IFigureTransform))]
    [InlineData(typeof(FigureTransform))]
    public void ATransform_IsAFigureTransform(Type type)
    {
        Assert.IsAssignableFrom(type, new TTransform());
    }

    [Fact]
    public void TheExportOfAFigureWithData_IsNotEmpty()
    {
        var figure = Plt.Create().WithTitle("Test").Plot([1.0, 2.0, 3.0], [4.0, 5.0, 6.0]).Build();

        Assert.NotEmpty(Export(figure));
    }

    [Fact]
    public void TheExport_StartsWithTheMagicBytesOfItsFormat()
    {
        Assert.Equal(Magic, Export(TwoPoints())[..Magic.Length]);
    }

    [Fact]
    public void TheFluentTransform_ToBytes_StartsWithTheMagicBytesOfItsFormat()
    {
        byte[] bytes = TwoPoints().Transform(new TTransform()).ToBytes();

        Assert.Equal(Magic, bytes[..Magic.Length]);
    }
}
