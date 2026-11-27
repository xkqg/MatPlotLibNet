// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;
using MatPlotLibNet.Models;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>What <see cref="PdfTransform"/> does with the save options it is handed. A PDF page is vector, so the
/// resolution has nothing to scale and is a documented no-op; the title and the author go into the document
/// information; the settings that only an SVG can use leave the file as it was.</summary>
public class PdfTransformOptionsTests : TransformOptionsTests<PdfTransform>
{
    /// <inheritdoc/>
    protected override int Width => PageWidth;

    /// <inheritdoc/>
    protected override int Height => PageHeight;

    /// <inheritdoc/>
    protected override string Extension => PageExtension;

    private const int PageWidth = 300;
    private const int PageHeight = 200;
    private const string PageExtension = ".pdf";

    private static Figure Page() => ChartOf(PageWidth, PageHeight);

    private static string Text(byte[] pdf) => Encoding.Latin1.GetString(pdf);

    // ── title and author ─────────────────────────────────────────────────────────────────────────────

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("TitleAndAuthor_AreWrittenToTheDocumentInformation", () => Text(Make(Page(), new SaveOptions { Title = "Annual Revenue", Author = "Finance Team" })), "/Title (Annual Revenue)", "/Author (Finance Team)"),
        new DrawnToken("ATitleAlone_LeavesTheAuthorOut", () => Text(Make(Page(), new SaveOptions { Title = "Annual Revenue" })), "/Title (Annual Revenue)") { Absent = ["/Author"] },
        new DrawnToken("AnAuthorAlone_LeavesTheTitleOut", () => Text(Make(Page(), new SaveOptions { Author = "Finance Team" })), "/Author (Finance Team)") { Absent = ["/Title"] },
        new DrawnToken("ThePageSurvivesTheDocumentInformation (plain)", () => Text(Make(Page(), options: null)), "/MediaBox [0 0 300 200]"),
        new DrawnToken("ThePageSurvivesTheDocumentInformation (titled)", () => Text(Make(Page(), new SaveOptions { Title = "T" })), "/MediaBox [0 0 300 200]"),
        new DrawnToken("Save_ToAPdfPath_WritesTheTitleAndAuthor", () => Text(SavedAs(PageExtension, Page(), new SaveOptions { Title = "Annual Revenue", Author = "Finance Team" })), "/Title (Annual Revenue)", "/Author (Finance Team)"),
        //  A date in the document information would make two PDFs of one figure differ whenever they are made in
        //  different seconds. Skia writes none unless it is handed one; this holds that.
        new DrawnToken("TextOutsideAscii_IsWrittenAsUtf16", () => Text(Make(Page(), new SaveOptions { Title = NonAsciiTitle })),
            $"/Title <FEFF{Convert.ToHexString(Encoding.BigEndianUnicode.GetBytes(NonAsciiTitle))}>"),
        new DrawnToken("TheDocumentInformation_HoldsNoDate", () => Text(Make(Page(), new SaveOptions { Title = "T", Author = "A" }))) { Absent = ["/CreationDate", "/ModDate"] },
    };

    private const string NonAsciiTitle = "Résumé — 東京";

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    [Fact]
    public void EmptyTitleAndAuthor_WriteTheSameFileAsNone()
    {
        Assert.Equal(Make(Chart(), options: null), Make(Chart(), new SaveOptions { Title = "", Author = "" }));
    }

    // ── the same figure writes the same bytes ────────────────────────────────────────────────────────

    [Fact]
    public void PdfsMadeOnManyThreadsAtOnce_AreTheBytesOfTheOneMadeAlone()
    {
        // Skia asks the typeface which character each glyph stands for, and the typeface is one the threads share: PDFs
        // made at the same moment came out with a text map of U+0000 for every glyph in about one of ten (a length of 247
        // against 259 in the map's stream), so the same figure wrote different bytes and its text could not be copied out.
        byte[] alone = Make(Chart(), options: null);
        var made = new byte[64][];

        Parallel.For(0, made.Length, new ParallelOptions { MaxDegreeOfParallelism = 8 }, i => made[i] = Make(Chart(), options: null));

        int differing = made.Count(pdf => !pdf.AsSpan().SequenceEqual(alone));
        Assert.True(differing == 0, $"{differing} of {made.Length} PDFs made at the same moment differ from the one made alone");
    }

    [Fact]
    public void MultiPagePdfsMadeOnManyThreadsAtOnce_AreTheBytesOfTheOneMadeAlone()
    {
        byte[] Write()
        {
            using var stream = new MemoryStream();
            new PdfTransform().TransformMultiPage([Chart(), Chart(dpi: 300)], stream);
            return stream.ToArray();
        }

        byte[] alone = Write();
        var made = new byte[40][];

        Parallel.For(0, made.Length, new ParallelOptions { MaxDegreeOfParallelism = 8 }, i => made[i] = Write());

        int differing = made.Count(pdf => !pdf.AsSpan().SequenceEqual(alone));
        Assert.True(differing == 0, $"{differing} of {made.Length} PDFs made at the same moment differ from the one made alone");
    }

    // ── what a PDF cannot use ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Resolution_IsANoOp_ForAVectorPage()
    {
        byte[] plain = Make(Chart(), options: null);

        Assert.Equal(plain, Make(Chart(), new SaveOptions { Dpi = 300 }));
        Assert.Equal(plain, Make(Chart(dpi: 300), options: null));
        Assert.Equal(plain, Make(Chart(dpi: 300), new SaveOptions { Dpi = 600 }));
    }
}
