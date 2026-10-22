// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>How the save options reach a transform: through the interface, so a transform written before the options
/// existed keeps working, and through the base class, so a transform that reads them overrides one method.</summary>
public class TransformOptionsTests
{
    private static Figure Chart() =>
        Plt.Create().WithSize(300, 200).WithTitle("Revenue").Plot([0.0, 1.0, 2.0], [0.123456, 2.718281, 1.414213]).Build();

    private sealed class InterfaceOnly : IFigureTransform
    {
        public int TwoArgumentCalls { get; private set; }

        public void Transform(Figure figure, Stream output) => TwoArgumentCalls++;
    }

    private sealed class BaseOnly : FigureTransform
    {
        public int TwoArgumentCalls { get; private set; }

        public override void Transform(Figure figure, Stream output) => TwoArgumentCalls++;
    }

    private sealed class Reading : FigureTransform
    {
        public SaveOptions? Seen { get; private set; }

        public override void Transform(Figure figure, Stream output) => throw new InvalidOperationException("the options overload was bypassed");

        public override void Transform(Figure figure, Stream output, SaveOptions options) => Seen = options;
    }

    [Fact]
    public void AnInterfaceOnlyTransform_IsCalledTheOldWay_WhenOptionsAreGiven()
    {
        var transform = new InterfaceOnly();
        IFigureTransform viaInterface = transform;

        viaInterface.Transform(Chart(), Stream.Null, new SaveOptions { Dpi = 300, Title = "T" });

        Assert.Equal(1, transform.TwoArgumentCalls);
    }

    [Fact]
    public void ABaseClassTransform_ThatDoesNotOverride_IsCalledTheOldWay_WhenOptionsAreGiven()
    {
        var transform = new BaseOnly();
        IFigureTransform viaInterface = transform;

        viaInterface.Transform(Chart(), Stream.Null, new SaveOptions { Title = "T" });

        Assert.Equal(1, transform.TwoArgumentCalls);
    }

    [Fact]
    public void ABaseClassTransform_ThatOverrides_ReceivesTheOptions_ThroughTheInterface()
    {
        var transform = new Reading();
        IFigureTransform viaInterface = transform;
        var options = new SaveOptions { Author = "A" };

        viaInterface.Transform(Chart(), Stream.Null, options);

        Assert.Same(options, transform.Seen);
    }

    [Fact]
    public void SvgTransform_ThroughTheInterface_AppliesTheSvgSettings()
    {
        var figure = Chart();
        var options = new SaveOptions { PrettifySvg = true, SvgDecimalPrecision = 1, Title = "T", Author = "A" };
        using var stream = new MemoryStream();
        IFigureTransform transform = new SvgTransform();

        transform.Transform(figure, stream, options);

        Assert.Equal(figure.ToSvg().WithSaveOptions(options), Encoding.UTF8.GetString(stream.ToArray()));
    }

    [Fact]
    public void SvgTransform_WithDefaultOptions_WritesTheBytesOfTheTwoArgumentCall()
    {
        var figure = Chart();
        using var plain = new MemoryStream();
        using var withDefaults = new MemoryStream();

        new SvgTransform().Transform(figure, plain);
        new SvgTransform().Transform(figure, withDefaults, new SaveOptions());

        Assert.Equal(plain.ToArray(), withDefaults.ToArray());
    }

    [Fact]
    public void SvgTransform_RefusesMissingOptions_ByName()
    {
        var refusal = Assert.Throws<ArgumentNullException>(
            () => new SvgTransform().Transform(Chart(), Stream.Null, null!));

        Assert.Equal("options", refusal.ParamName);
    }

    [Fact]
    public void SvgTransform_RefusesADecimalCountOutsideTheRange_BeforeWritingAnything()
    {
        using var stream = new MemoryStream();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new SvgTransform().Transform(Chart(), stream, new SaveOptions { SvgDecimalPrecision = 99 }));

        Assert.Equal(0, stream.Length);
    }
}
