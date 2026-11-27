// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Buffers.Binary;
using static MatPlotLibNet.Mcp.Tests.SpecFixtures;

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

    /// <summary>A dpi at the ceiling is accepted (400 units at 960 dpi are 4000 pixels, exactly the ceiling), a dpi below the default
    /// shrinks the picture and is accepted, and a spec without a dpi takes the default at the largest canvas.</summary>
    [Theory]
    [InlineData(""" "width":400,"height":300,"dpi":960 """, 960)]
    [InlineData(""" "width":4000,"height":4000,"dpi":48 """, 48)]
    [InlineData(""" "width":4000,"height":4000 """, 96)]
    public void ADpiWithinTheCeiling_IsAccepted_AndAMissingOneTakesTheDefault(string fields, int dpi)
    {
        var figure = Reader.Read(Spec(fields.Trim()));

        Assert.Equal(dpi, figure.Dpi);
    }

    public static TheoryData<string, int> DpiBeyondTheCeiling => new()
    {
        { """ "width":400,"height":300,"dpi":961 """, Limits.MaxWidth },  // across the width
        { """ "width":100,"height":400,"dpi":1000 """, Limits.MaxHeight }, // down the height
    };

    [Theory]
    [MemberData(nameof(DpiBeyondTheCeiling))]
    public void ADpiBeyondTheCeiling_IsRefusedByName(string fields, int limit)
    {
        var refusal = Assert.Throws<ToolRefusalException>(
            () => Reader.Read(Spec(fields.Trim())));

        Assert.Contains("dpi", refusal.Message);
        Assert.Contains(limit.ToString(), refusal.Message);
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
        string description = Description.Describe(null);

        Assert.Contains("dpi", description);
        Assert.Contains("pixels", description[description.IndexOf("dpi", StringComparison.Ordinal)..]);
    }
}
