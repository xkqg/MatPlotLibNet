// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;

namespace MatPlotLibNet.Mcp.Tests;

/// <summary>Rendering and saving differ only in where the bytes go, so they are one owner with two sinks: the
/// format is resolved once, the transform is chosen explicitly, and the path never gets a second vote. The
/// library's own <c>Save</c> decides format from the file extension and falls through to SVG for anything it does
/// not recognise — writing SVG bytes into a file called <c>.jpg</c> and reporting success.</summary>
public class ChartRenderingTests : IDisposable
{
    private readonly DirectoryInfo _output = Directory.CreateTempSubdirectory("mcp-render-tests");
    private readonly ChartRendering _rendering = new(
        new ChartSpecReader(new ChartTypeCatalog(), RenderLimits.Default), new ChartSummarizer());

    private static readonly ChartSpec Line = new(
        """{"width":400,"height":300,"title":"Revenue","subPlots":[{"series":[{"type":"line","xData":[1,2,3],"yData":[2,4,3]}]}]}""");

    public void Dispose()
    {
        _output.Delete(recursive: true);
        GC.SuppressFinalize(this);
    }

    private OutputPathResolver Resolver => new(_output.FullName);

    [Fact]
    public void RenderingAChart_ReturnsPngBytes()
    {
        var result = _rendering.Render(Line, ChartFormat.Png);

        Assert.Equal(ChartFormat.Png, result.Format);
        Assert.Equal("image/png", result.MediaType);
        // The PNG signature, asserted the way the Skia suite asserts it.
        Assert.Equal(0x89, result.Bytes[0]);
        Assert.Equal("PNG", Encoding.ASCII.GetString(result.Bytes, 1, 3));
    }

    [Fact]
    public void TheSummary_TravelsBesideThePicture()
    {
        var result = _rendering.Render(Line, ChartFormat.Png);

        Assert.Equal("Revenue", result.Summary.Title);
        Assert.Equal(3, Assert.Single(result.Summary.Series).PointCount);
    }

    [Theory]
    [InlineData(ChartFormat.Svg)]
    [InlineData(ChartFormat.Pdf)]
    public void AskingForSvgOrPdfInline_IsRefused_AndPointsAtSaveChart(ChartFormat format)
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => _rendering.Render(Line, format));

        Assert.Contains("save_chart", refusal.Message);
    }

    [Fact]
    public void ABadSpec_NeverReachesTheRenderer()
    {
        var refusal = Assert.Throws<ToolRefusalException>(() =>
            _rendering.Render(new ChartSpec("""{"subPlots":[{"series":[{"type":"lien","xData":[1],"yData":[1]}]}]}"""), ChartFormat.Png));

        Assert.Contains("'lien'", refusal.Message);
    }

    [Fact]
    public void TheSvgThisServerProduces_CarriesGlyphPaths_NotTextElements()
    {
        // The PNG backend is loaded in this process, and its module initializer installs a glyph-path provider
        // process-wide. That is a promise the package README makes; it breaks the day the Skia reference goes.
        string svg = File.ReadAllText(_rendering.Save(Line, ChartFormat.Svg, "glyphs.svg", overwrite: false, Resolver).Path);

        Assert.Contains("<path", svg);
        Assert.DoesNotContain("<text", svg);
    }

    [Fact]
    public void TheGlyphProvider_IsInstalledBeforeTheFirstRender() =>
        Assert.NotNull(ChartServices.GlyphPathProvider);

    [Fact]
    public void SavingAChart_WritesTheFileAndReturnsThePathItWrote()
    {
        var result = _rendering.Save(Line, ChartFormat.Png, "chart.png", overwrite: false, Resolver);

        Assert.True(File.Exists(result.Path));
        Assert.Equal(new FileInfo(result.Path).Length, result.ByteCount);
        Assert.Equal(ChartFormat.Png, result.FormatWritten);
        Assert.Equal(Path.Combine(_output.FullName, "chart.png"), result.Path);
    }

    [Fact]
    public void SavingAPdf_ProducesAFileThatStartsWithThePdfHeader()
    {
        var result = _rendering.Save(Line, ChartFormat.Pdf, "chart.pdf", overwrite: false, Resolver);

        Assert.Equal("%PDF", Encoding.ASCII.GetString(File.ReadAllBytes(result.Path), 0, 4));
    }

    [Fact]
    public void SavingAnSvg_ProducesAFileThatParsesAsXml()
    {
        var result = _rendering.Save(Line, ChartFormat.Svg, "chart.svg", overwrite: false, Resolver);

        var document = System.Xml.Linq.XDocument.Load(result.Path);
        Assert.Equal("svg", document.Root!.Name.LocalName);
    }

    [Theory]
    [InlineData(ChartFormat.Png, "chart.svg")]
    [InlineData(ChartFormat.Svg, "chart.png")]
    [InlineData(ChartFormat.Pdf, "chart.png")]
    public void TheFormatAndTheExtension_NeverDisagree(ChartFormat format, string path)
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => _rendering.Save(Line, format, path, overwrite: false, Resolver));

        Assert.Contains(Path.GetExtension(path), refusal.Message);
        Assert.Contains(format.ToString().ToLowerInvariant(), refusal.Message.ToLowerInvariant());
    }

    [Fact]
    public void AnExistingFile_IsNotTruncated()
    {
        string path = Path.Combine(_output.FullName, "keep.png");
        File.WriteAllText(path, "precious");

        var refusal = Assert.Throws<ToolRefusalException>(() => _rendering.Save(Line, ChartFormat.Png, "keep.png", overwrite: false, Resolver));

        Assert.Contains("exists", refusal.Message);
        Assert.Equal("precious", File.ReadAllText(path));
    }

    [Fact]
    public void AnExistingFile_IsReplacedWhenTheCallerSaysSo()
    {
        string path = Path.Combine(_output.FullName, "replace.png");
        File.WriteAllText(path, "old");

        _rendering.Save(Line, ChartFormat.Png, "replace.png", overwrite: true, Resolver);

        Assert.Equal(0x89, File.ReadAllBytes(path)[0]);
    }
}
