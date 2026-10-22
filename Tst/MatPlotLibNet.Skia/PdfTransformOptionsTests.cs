// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;
using MatPlotLibNet.Models;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>What <see cref="PdfTransform"/> does with the save options it is handed. A PDF page is vector, so the
/// resolution has nothing to scale and is a documented no-op; the title and the author go into the document
/// information; the settings that only an SVG can use leave the file as it was.</summary>
public class PdfTransformOptionsTests
{
    /// <summary>Save routes .png and .pdf to the Skia transforms only once the Skia assembly's module initializer has
    /// run, which happens when something of the assembly is first used; a test that saves a file before any other
    /// Skia call would otherwise write an SVG under a .png name, depending on the order the tests run in.</summary>
    static PdfTransformOptionsTests() =>
        System.Runtime.CompilerServices.RuntimeHelpers.RunModuleConstructor(typeof(FigureSkiaExtensions).Module.ModuleHandle);

    private static Figure Chart(double dpi = 96) =>
        Plt.Create().WithSize(300, 200).WithDpi(dpi).Plot([1.0, 2.0, 3.0], [3.0, 1.0, 2.0]).Build();

    private static byte[] Pdf(Figure figure, SaveOptions? options)
    {
        using var stream = new MemoryStream();
        var transform = new PdfTransform();
        if (options is null) transform.Transform(figure, stream);
        else transform.Transform(figure, stream, options);
        return stream.ToArray();
    }

    private static string Text(byte[] pdf) => Encoding.Latin1.GetString(pdf);

    // ── title and author ─────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TitleAndAuthor_AreWrittenToTheDocumentInformation()
    {
        string pdf = Text(Pdf(Chart(), new SaveOptions { Title = "Annual Revenue", Author = "Finance Team" }));

        Assert.Contains("/Title (Annual Revenue)", pdf);
        Assert.Contains("/Author (Finance Team)", pdf);
    }

    [Fact]
    public void ATitleAlone_LeavesTheAuthorOut()
    {
        string pdf = Text(Pdf(Chart(), new SaveOptions { Title = "Annual Revenue" }));

        Assert.Contains("/Title (Annual Revenue)", pdf);
        Assert.DoesNotContain("/Author", pdf);
    }

    [Fact]
    public void AnAuthorAlone_LeavesTheTitleOut()
    {
        string pdf = Text(Pdf(Chart(), new SaveOptions { Author = "Finance Team" }));

        Assert.Contains("/Author (Finance Team)", pdf);
        Assert.DoesNotContain("/Title", pdf);
    }

    [Fact]
    public void TextOutsideAscii_IsWrittenAsUtf16()
    {
        const string title = "Résumé — 東京";
        string utf16 = Convert.ToHexString(Encoding.BigEndianUnicode.GetBytes(title));

        string pdf = Text(Pdf(Chart(), new SaveOptions { Title = title }));

        Assert.Contains($"/Title <FEFF{utf16}>", pdf);
    }

    [Fact]
    public void EmptyTitleAndAuthor_WriteTheSameFileAsNone()
    {
        Assert.Equal(Pdf(Chart(), options: null), Pdf(Chart(), new SaveOptions { Title = "", Author = "" }));
    }

    [Fact]
    public void ThePageSurvivesTheDocumentInformation()
    {
        string plain = Text(Pdf(Chart(), options: null));
        string titled = Text(Pdf(Chart(), new SaveOptions { Title = "T" }));

        Assert.Contains("/MediaBox [0 0 300 200]", plain);
        Assert.Contains("/MediaBox [0 0 300 200]", titled);
    }

    [Fact]
    public void Save_ToAPdfPath_WritesTheTitleAndAuthor()
    {
        string path = Path.Combine(Path.GetTempPath(), $"s9_{Guid.NewGuid():N}.pdf");
        try
        {
            Chart().Save(path, new SaveOptions { Title = "Annual Revenue", Author = "Finance Team" });

            string pdf = Text(File.ReadAllBytes(path));
            Assert.Contains("/Title (Annual Revenue)", pdf);
            Assert.Contains("/Author (Finance Team)", pdf);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    // ── what a PDF cannot use ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Resolution_IsANoOp_ForAVectorPage()
    {
        byte[] plain = Pdf(Chart(), options: null);

        Assert.Equal(plain, Pdf(Chart(), new SaveOptions { Dpi = 300 }));
        Assert.Equal(plain, Pdf(Chart(dpi: 300), options: null));
        Assert.Equal(plain, Pdf(Chart(dpi: 300), new SaveOptions { Dpi = 600 }));
    }

    [Fact]
    public void SvgOnlySettings_LeaveThePdfExactlyAsItWas()
    {
        Assert.Equal(
            Pdf(Chart(), options: null),
            Pdf(Chart(), new SaveOptions { PrettifySvg = true, SvgDecimalPrecision = 1 }));
    }

    [Fact]
    public void NoOptions_AndDefaultOptions_WriteTheSameBytes()
    {
        Assert.Equal(Pdf(Chart(), options: null), Pdf(Chart(), new SaveOptions()));
    }
}
