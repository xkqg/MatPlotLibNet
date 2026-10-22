// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Transforms;
using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>What <see cref="PngTransform"/> does with the save options it is handed: the resolution scales the picture,
/// the title and the author travel in the file as text chunks, and the settings that only an SVG can use leave the
/// PNG exactly as it was.</summary>
public class PngTransformOptionsTests
{
    /// <summary>Save routes .png and .pdf to the Skia transforms only once the Skia assembly's module initializer has
    /// run, which happens when something of the assembly is first used; a test that saves a file before any other
    /// Skia call would otherwise write an SVG under a .png name, depending on the order the tests run in.</summary>
    static PngTransformOptionsTests() =>
        System.Runtime.CompilerServices.RuntimeHelpers.RunModuleConstructor(typeof(FigureSkiaExtensions).Module.ModuleHandle);

    private static Figure Chart(double dpi = 96) =>
        Plt.Create().WithSize(200, 150).WithDpi(dpi).Plot([1.0, 2.0, 3.0], [3.0, 1.0, 2.0]).Build();

    private static byte[] Png(Figure figure, SaveOptions? options)
    {
        using var stream = new MemoryStream();
        var transform = new PngTransform();
        if (options is null) transform.Transform(figure, stream);
        else transform.Transform(figure, stream, options);
        return stream.ToArray();
    }

    [Fact]
    public void ATransformMadeWithARenderer_DrawsWithIt_AtTheOptionsResolution()
    {
        using var stream = new MemoryStream();

        new PngTransform(new MatPlotLibNet.Rendering.ChartRenderer()).Transform(Chart(), stream, new SaveOptions { Dpi = 192 });

        Assert.Equal(new PngSize(400, 300), stream.ToArray().PngPixelSize());
    }

    // ── resolution ───────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TransformWithOptions_UsesTheOptionsResolution()
    {
        Assert.Equal(new PngSize(400, 300), Png(Chart(), new SaveOptions { Dpi = 192 }).PngPixelSize());
    }

    [Fact]
    public void TransformWithOptions_ThatSayNothingOfResolution_UsesTheFigureResolution()
    {
        Assert.Equal(new PngSize(400, 300), Png(Chart(dpi: 192), new SaveOptions()).PngPixelSize());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-300)]
    public void OptionsDpi_ThatIsNotPositive_IsRefused(int dpi)
    {
        var refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Png(Chart(), new SaveOptions { Dpi = dpi }));

        Assert.Equal("dpi", refusal.ParamName);
    }

    [Fact]
    public void NoOptions_AndDefaultOptions_WriteTheSameBytes()
    {
        var figure = Chart(dpi: 120);

        Assert.Equal(Png(figure, options: null), Png(figure, new SaveOptions()));
    }

    // ── title and author ─────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void WithoutATitleOrAuthor_TheFileCarriesNoTextChunk()
    {
        var chunks = Png(Chart(), new SaveOptions { Dpi = 120 }).PngChunks();

        Assert.DoesNotContain(chunks, c => c.Type is "iTXt" or "tEXt" or "zTXt");
    }

    [Fact]
    public void TitleAndAuthor_AreWrittenAsTextChunks_AfterTheHeader()
    {
        var chunks = Png(Chart(), new SaveOptions { Title = "Annual Revenue", Author = "Finance Team" }).PngChunks();

        Assert.Equal("IHDR", chunks[0].Type);
        Assert.Equal("iTXt", chunks[1].Type);
        Assert.Equal("iTXt", chunks[2].Type);
        Assert.Equal(new PngText("Title", "Annual Revenue"), chunks[1].InternationalText());
        Assert.Equal(new PngText("Author", "Finance Team"), chunks[2].InternationalText());
        Assert.Equal("IEND", chunks[^1].Type);
    }

    [Fact]
    public void ATitleAlone_WritesOneChunk()
    {
        var chunks = Png(Chart(), new SaveOptions { Title = "Only a title" }).PngChunks();

        var text = Assert.Single(chunks, c => c.Type == "iTXt");
        Assert.Equal(new PngText("Title", "Only a title"), text.InternationalText());
    }

    [Fact]
    public void AnAuthorAlone_WritesOneChunk()
    {
        var chunks = Png(Chart(), new SaveOptions { Author = "Only an author" }).PngChunks();

        var text = Assert.Single(chunks, c => c.Type == "iTXt");
        Assert.Equal(new PngText("Author", "Only an author"), text.InternationalText());
    }

    [Fact]
    public void TextOutsideLatin1_SurvivesAsUtf8()
    {
        const string title = "Résumé — 東京 ☂";

        var chunks = Png(Chart(), new SaveOptions { Title = title }).PngChunks();

        Assert.Equal(new PngText("Title", title), Assert.Single(chunks, c => c.Type == "iTXt").InternationalText());
    }

    [Fact]
    public void EveryChunkCrc_IsTheCrcOfItsTypeAndData()
    {
        var chunks = Png(Chart(), new SaveOptions { Title = "Annual Revenue", Author = "Finance Team" }).PngChunks();

        foreach (var chunk in chunks)
        {
            byte[] covered = [.. System.Text.Encoding.ASCII.GetBytes(chunk.Type), .. chunk.Data];
            Assert.Equal(PngTextChunkExtensions.Crc32(covered), chunk.StoredCrc);
        }
    }

    [Fact]
    public void ThePictureSurvivesTheTextChunks()
    {
        var figure = Chart();
        using var plain = SKBitmap.Decode(Png(figure, options: null));

        using var withText = SKBitmap.Decode(Png(figure, new SaveOptions { Title = "T", Author = "A" }));

        Assert.NotNull(withText);
        Assert.Equal(plain.Width, withText.Width);
        Assert.Equal(plain.Height, withText.Height);
        Assert.Equal(plain.Pixels, withText.Pixels);
    }

    [Fact]
    public void AnEmptyTitle_WritesNoChunk()
    {
        var chunks = Png(Chart(), new SaveOptions { Title = "", Author = "" }).PngChunks();

        Assert.DoesNotContain(chunks, c => c.Type == "iTXt");
    }

    [Fact]
    public void Save_ToAPngPath_WritesTheResolutionTheTitleAndTheAuthor()
    {
        string path = Path.Combine(Path.GetTempPath(), $"s9_{Guid.NewGuid():N}.png");
        try
        {
            Chart().Save(path, new SaveOptions { Dpi = 192, Title = "Annual Revenue", Author = "Finance Team" });

            byte[] png = File.ReadAllBytes(path);
            Assert.Equal(new PngSize(400, 300), png.PngPixelSize());
            Assert.Equal(new PngText("Title", "Annual Revenue"), png.PngChunks()[1].InternationalText());
            Assert.Equal(new PngText("Author", "Finance Team"), png.PngChunks()[2].InternationalText());
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    // ── what a PNG cannot use ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SvgOnlySettings_LeaveThePngExactlyAsItWas()
    {
        var figure = Chart();

        var plain = Png(figure, options: null);
        var withSvgSettings = Png(figure, new SaveOptions { PrettifySvg = true, SvgDecimalPrecision = 1 });

        Assert.Equal(plain, withSvgSettings);
    }
}
