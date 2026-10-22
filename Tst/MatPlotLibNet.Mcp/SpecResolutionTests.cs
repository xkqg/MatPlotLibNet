// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Buffers.Binary;

namespace MatPlotLibNet.Mcp.Tests;

/// <summary>A spec's <c>dpi</c> sizes the PNG, since the export now draws <c>dpi / 96</c> pixels for each unit of the
/// figure. The canvas ceiling exists because the picture goes into a context window and an allocation has to fit, so it
/// bounds the picture the spec draws, not the figure's units alone: a dpi the ceiling cannot hold is refused by name.</summary>
[Collection("ChartServicesGlobalState")]
public class SpecResolutionTests
{
    private static readonly RenderLimits Limits = RenderLimits.Default;
    private static readonly ChartSpecReader Reader = new(new ChartTypeCatalog(), Limits);
    private static readonly ChartRendering Rendering = new(Reader, new ChartSummarizer());

    private const string Line = """{"type":"line","xData":[1,2,3],"yData":[2,4,3]}""";

    private static ChartSpec Spec(string figure) => new($$"""{{{figure}},"subPlots":[{"series":[{{Line}}]}]}""");

    [Fact]
    public void ADpiAtTheCeiling_IsAccepted()
    {
        // 400 units at 960 dpi are 4000 pixels: exactly the ceiling.
        var figure = Reader.Read(Spec(""" "width":400,"height":300,"dpi":960 """.Trim()));

        Assert.Equal(960, figure.Dpi);
    }

    [Fact]
    public void ADpiBeyondTheCeiling_IsRefusedByName_AcrossTheWidth()
    {
        var refusal = Assert.Throws<ToolRefusalException>(
            () => Reader.Read(Spec(""" "width":400,"height":300,"dpi":961 """.Trim())));

        Assert.Contains("dpi", refusal.Message);
        Assert.Contains(Limits.MaxWidth.ToString(), refusal.Message);
    }

    [Fact]
    public void ADpiBeyondTheCeiling_IsRefusedByName_DownTheHeight()
    {
        var refusal = Assert.Throws<ToolRefusalException>(
            () => Reader.Read(Spec(""" "width":100,"height":400,"dpi":1000 """.Trim())));

        Assert.Contains("dpi", refusal.Message);
        Assert.Contains(Limits.MaxHeight.ToString(), refusal.Message);
    }

    [Fact]
    public void ADpiAbsurdForAnyCanvas_IsRefused_NotAllocated()
    {
        var refusal = Assert.Throws<ToolRefusalException>(
            () => Reader.Read(Spec(""" "width":800,"height":600,"dpi":2000000000 """.Trim())));

        Assert.Contains("dpi", refusal.Message);
    }

    [Theory]
    [InlineData(""" "width":400,"height":300,"dpi":0 """)]
    [InlineData(""" "width":400,"height":300,"dpi":-96 """)]
    public void ADpiBelowOne_IsStillRefused_WithTheField(string figure)
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Reader.Read(Spec(figure.Trim())));

        Assert.Contains("dpi", refusal.Message);
    }

    [Fact]
    public void ADpiBelowTheDefault_ShrinksThePicture_AndIsAccepted()
    {
        var figure = Reader.Read(Spec(""" "width":4000,"height":4000,"dpi":48 """.Trim()));

        Assert.Equal(48, figure.Dpi);
    }

    [Fact]
    public void ASpecWithoutADpi_TakesTheDefault_AtTheLargestCanvas()
    {
        var figure = Reader.Read(Spec(""" "width":4000,"height":4000 """.Trim()));

        Assert.Equal(96, figure.Dpi);
    }

    [Fact]
    public void ThePng_IsDrawnAtTheSpecsDpi()
    {
        var result = Rendering.Render(Spec(""" "width":400,"height":300,"dpi":192 """.Trim()), ChartFormat.Png);

        Assert.Equal(800, (int)BinaryPrimitives.ReadUInt32BigEndian(result.Bytes.AsSpan(16, 4)));
        Assert.Equal(600, (int)BinaryPrimitives.ReadUInt32BigEndian(result.Bytes.AsSpan(20, 4)));
    }

    [Fact]
    public void ThePng_OfASpecWithoutADpi_IsDrawnAtOnePixelPerUnit()
    {
        var result = Rendering.Render(Spec(""" "width":400,"height":300 """.Trim()), ChartFormat.Png);

        Assert.Equal(400, (int)BinaryPrimitives.ReadUInt32BigEndian(result.Bytes.AsSpan(16, 4)));
        Assert.Equal(300, (int)BinaryPrimitives.ReadUInt32BigEndian(result.Bytes.AsSpan(20, 4)));
    }

    [Fact]
    public void TheDescribedSpec_SaysWhatDpiDoes()
    {
        string description = new ChartSchemaDescription(new ChartTypeCatalog()).Describe(null);

        Assert.Contains("dpi", description);
        Assert.Contains("pixels", description[description.IndexOf("dpi", StringComparison.Ordinal)..]);
    }
}
