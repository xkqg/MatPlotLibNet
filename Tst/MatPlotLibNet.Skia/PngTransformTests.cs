// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;
using SkiaSharp;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>Adding text chunks to a finished PNG: where they go, what they hold, what they must not disturb.</summary>
public class PngTransformTests
{
    private static byte[] SmallPng()
    {
        using var bitmap = new SKBitmap(3, 2);
        bitmap.Erase(SKColors.CornflowerBlue);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    // ── the CRC the standard asks for ────────────────────────────────────────────────────────────────

    [Fact]
    public void Crc32_OfTheCheckString_IsTheStandardCheckValue()
    {
        // The value every CRC-32 implementation publishes for the ASCII digits 1 to 9.
        Assert.Equal(0xCBF43926u, PngTransform.Crc32("123456789"u8));
    }

    [Fact]
    public void Crc32_OfTheTypeOfTheEndChunk_IsTheCrcEveryPngEndsWith()
    {
        // An IEND chunk has no data, so its CRC covers just the four letters; every PNG file ends with these bytes.
        Assert.Equal(0xAE426082u, PngTransform.Crc32("IEND"u8));
    }

    [Fact]
    public void Crc32_OfNothing_IsZero()
    {
        Assert.Equal(0u, PngTransform.Crc32([]));
    }

    // ── the chunks ───────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void WithNoEntries_ThePngIsReturnedAsItIs()
    {
        byte[] png = SmallPng();

        Assert.Same(png, PngTransform.WithTextChunks(png, []));
    }

    [Fact]
    public void EntriesAreWrittenInOrder_RightAfterTheHeader()
    {
        byte[] png = SmallPng();

        byte[] withText = PngTransform.WithTextChunks(png, [new PngTransform.PngTextEntry("Title", "One"), new PngTransform.PngTextEntry("Author", "Two")]);

        var chunks = withText.PngChunks();
        Assert.Equal(new[] { "IHDR", "iTXt", "iTXt" }, chunks.Take(3).Select(c => c.Type));
        Assert.Equal(new PngText("Title", "One"), chunks[1].InternationalText());
        Assert.Equal(new PngText("Author", "Two"), chunks[2].InternationalText());
    }

    [Fact]
    public void EveryOtherChunk_IsUntouched()
    {
        byte[] png = SmallPng();

        byte[] withText = PngTransform.WithTextChunks(png, [new PngTransform.PngTextEntry("Title", "One")]);

        var before = png.PngChunks();
        var after = withText.PngChunks().Where(c => c.Type != "iTXt").ToList();
        Assert.Equal(before.Count, after.Count);
        for (int i = 0; i < before.Count; i++)
        {
            Assert.Equal(before[i].Type, after[i].Type);
            Assert.Equal(before[i].Data, after[i].Data);
            Assert.Equal(before[i].StoredCrc, after[i].StoredCrc);
        }
    }

    [Fact]
    public void TheFileGrowsByExactlyTheChunks()
    {
        byte[] png = SmallPng();

        byte[] withText = PngTransform.WithTextChunks(png, [new PngTransform.PngTextEntry("Title", "abc")]);

        // length (4) + type (4) + keyword "Title" (5) + five zero bytes + text (3) + crc (4)
        Assert.Equal(png.Length + 4 + 4 + 5 + 5 + 3 + 4, withText.Length);
    }

    [Fact]
    public void ANonAsciiTextIsStoredAsUtf8()
    {
        byte[] withText = PngTransform.WithTextChunks(SmallPng(), [new PngTransform.PngTextEntry("Title", "Café 東京")]);

        Assert.Equal(new PngText("Title", "Café 東京"), withText.PngChunks()[1].InternationalText());
    }

    [Fact]
    public void AnEmptyText_IsAnEmptyChunkText()
    {
        byte[] withText = PngTransform.WithTextChunks(SmallPng(), [new PngTransform.PngTextEntry("Title", "")]);

        Assert.Equal(new PngText("Title", ""), withText.PngChunks()[1].InternationalText());
    }

    [Fact]
    public void TheCrcOfEachChunk_CoversItsTypeAndData()
    {
        byte[] withText = PngTransform.WithTextChunks(SmallPng(), [new PngTransform.PngTextEntry("Title", "One")]);

        foreach (var chunk in withText.PngChunks())
        {
            byte[] covered = [.. Encoding.ASCII.GetBytes(chunk.Type), .. chunk.Data];
            Assert.Equal(PngTransform.Crc32(covered), chunk.StoredCrc);
        }
    }

    [Fact]
    public void TheEncodedFile_StillDecodesToTheSamePixels()
    {
        byte[] png = SmallPng();
        using var plain = SKBitmap.Decode(png);

        using var withText = SKBitmap.Decode(PngTransform.WithTextChunks(png, [new PngTransform.PngTextEntry("Title", "One")]));

        Assert.Equal(plain.Pixels, withText.Pixels);
    }

    // ── what is refused ──────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void BytesThatAreNotAPng_AreRefused()
    {
        var refusal = Assert.Throws<ArgumentException>(
            () => PngTransform.WithTextChunks(new byte[] { 1, 2, 3 }, [new PngTransform.PngTextEntry("Title", "One")]));

        Assert.Equal("png", refusal.ParamName);
    }

    [Fact]
    public void APngSignatureWithoutAHeaderChunk_IsRefused()
    {
        byte[] png = SmallPng();
        png[12] = (byte)'X'; // IHDR -> XHDR

        var refusal = Assert.Throws<ArgumentException>(() => PngTransform.WithTextChunks(png, [new PngTransform.PngTextEntry("Title", "One")]));

        Assert.Equal("png", refusal.ParamName);
    }

    /// <summary>A header that is not thirteen bytes long (byte 11 is the length of IHDR) and a wrong signature (byte 0) are refused alike.</summary>
    [Theory]
    [InlineData(11, 12)]
    [InlineData(0, 0)]
    public void AHeaderThatIsNotThirteenBytesLong_OrAWrongSignature_IsRefused(int index, byte value)
    {
        byte[] png = SmallPng();
        png[index] = value;

        Assert.Throws<ArgumentException>(() => PngTransform.WithTextChunks(png, [new PngTransform.PngTextEntry("Title", "One")]));
    }

    [Theory]
    [InlineData("")]
    [InlineData("a keyword that is a good deal longer than the seventy-nine characters the standard allows for one")]
    [InlineData("Tab\there")]
    [InlineData("Ti\u0000tle")]
    [InlineData("Titel 東京")]
    public void AKeywordTheStandardDoesNotAllow_IsRefused(string keyword)
    {
        var refusal = Assert.Throws<ArgumentException>(
            () => PngTransform.WithTextChunks(SmallPng(), [new PngTransform.PngTextEntry(keyword, "text")]));

        Assert.Contains("keyword", refusal.Message);
    }

    [Fact]
    public void ALatin1Keyword_IsAccepted()
    {
        byte[] withText = PngTransform.WithTextChunks(SmallPng(), [new PngTransform.PngTextEntry("Auteur ¿", "x")]);

        Assert.Equal("Auteur ¿", withText.PngChunks()[1].InternationalText().Keyword);
    }
}
