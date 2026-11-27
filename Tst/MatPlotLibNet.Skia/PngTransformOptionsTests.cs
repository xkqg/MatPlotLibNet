// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Transforms;
using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>What <see cref="PngTransform"/> does with the save options it is handed: the resolution scales the picture,
/// the title and the author travel in the file as text chunks, and the settings that only an SVG can use leave the
/// PNG exactly as it was.</summary>
public class PngTransformOptionsTests : TransformOptionsTests<PngTransform>
{
    /// <inheritdoc/>
    protected override int Width => 200;

    /// <inheritdoc/>
    protected override int Height => 150;

    /// <inheritdoc/>
    protected override string Extension => ".png";

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
        Assert.Equal(new PngSize(400, 300), Make(Chart(), new SaveOptions { Dpi = 192 }).PngPixelSize());
    }

    [Fact]
    public void TransformWithOptions_ThatSayNothingOfResolution_UsesTheFigureResolution()
    {
        Assert.Equal(new PngSize(400, 300), Make(Chart(dpi: 192), new SaveOptions()).PngPixelSize());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-300)]
    public void OptionsDpi_ThatIsNotPositive_IsRefused(int dpi)
    {
        var refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Make(Chart(), new SaveOptions { Dpi = dpi }));

        Assert.Equal("dpi", refusal.ParamName);
    }

    // ── title and author ─────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void WithoutATitleOrAuthor_TheFileCarriesNoTextChunk()
    {
        var chunks = Make(Chart(), new SaveOptions { Dpi = 120 }).PngChunks();

        Assert.DoesNotContain(chunks, c => c.Type is "iTXt" or "tEXt" or "zTXt");
    }

    [Fact]
    public void TitleAndAuthor_AreWrittenAsTextChunks_AfterTheHeader()
    {
        var chunks = Make(Chart(), new SaveOptions { Title = "Annual Revenue", Author = "Finance Team" }).PngChunks();

        Assert.Equal("IHDR", chunks[0].Type);
        Assert.Equal("iTXt", chunks[1].Type);
        Assert.Equal("iTXt", chunks[2].Type);
        Assert.Equal(new PngText("Title", "Annual Revenue"), chunks[1].InternationalText());
        Assert.Equal(new PngText("Author", "Finance Team"), chunks[2].InternationalText());
        Assert.Equal("IEND", chunks[^1].Type);
    }

    [Theory]
    [InlineData(true, "Title", "Only a title")]
    [InlineData(false, "Author", "Only an author")]
    public void ATitleOrAnAuthorAlone_WritesOneChunk(bool title, string key, string value)
    {
        var chunks = Make(Chart(), title ? new SaveOptions { Title = value } : new SaveOptions { Author = value }).PngChunks();

        var text = Assert.Single(chunks, c => c.Type == "iTXt");
        Assert.Equal(new PngText(key, value), text.InternationalText());
    }

    [Fact]
    public void TextOutsideLatin1_SurvivesAsUtf8()
    {
        const string title = "Résumé — 東京 ☂";

        var chunks = Make(Chart(), new SaveOptions { Title = title }).PngChunks();

        Assert.Equal(new PngText("Title", title), Assert.Single(chunks, c => c.Type == "iTXt").InternationalText());
    }

    [Fact]
    public void EveryChunkCrc_IsTheCrcOfItsTypeAndData()
    {
        var chunks = Make(Chart(), new SaveOptions { Title = "Annual Revenue", Author = "Finance Team" }).PngChunks();

        foreach (var chunk in chunks)
        {
            byte[] covered = [.. System.Text.Encoding.ASCII.GetBytes(chunk.Type), .. chunk.Data];
            Assert.Equal(PngTransform.Crc32(covered), chunk.StoredCrc);
        }
    }

    [Fact]
    public void ThePictureSurvivesTheTextChunks()
    {
        var figure = Chart();
        using var plain = SKBitmap.Decode(Make(figure, options: null));

        using var withText = SKBitmap.Decode(Make(figure, new SaveOptions { Title = "T", Author = "A" }));

        Assert.NotNull(withText);
        Assert.Equal(plain.Width, withText.Width);
        Assert.Equal(plain.Height, withText.Height);
        Assert.Equal(plain.Pixels, withText.Pixels);
    }

    [Fact]
    public void AnEmptyTitle_WritesNoChunk()
    {
        var chunks = Make(Chart(), new SaveOptions { Title = "", Author = "" }).PngChunks();

        Assert.DoesNotContain(chunks, c => c.Type == "iTXt");
    }

    [Fact]
    public void Save_ToAPngPath_WritesTheResolutionTheTitleAndTheAuthor()
    {
        byte[] png = Saved(Chart(), new SaveOptions { Dpi = 192, Title = "Annual Revenue", Author = "Finance Team" });

        Assert.Equal(new PngSize(400, 300), png.PngPixelSize());
        Assert.Equal(new PngText("Title", "Annual Revenue"), png.PngChunks()[1].InternationalText());
        Assert.Equal(new PngText("Author", "Finance Team"), png.PngChunks()[2].InternationalText());
    }
}
