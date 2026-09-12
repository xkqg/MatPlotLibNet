// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Diagnostics;
using MatPlotLibNet.Styling;
using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>The font door of the Skia package. A user can already choose a font by NAME; these facts cover what was
/// missing: a font FILE, a clear refusal when the file is not a font, a registration that a font resolved earlier
/// still picks up, and a diagnostic instead of silence when a requested family is not there at all.</summary>
[Collection("SkiaFontsGlobalState")]
public sealed class SkiaFontsTests
{
    private static readonly string NotoSansHebrewPath =
        Path.Combine(AppContext.BaseDirectory, "Fonts", "NotoSansHebrew-Regular.ttf");

    private static readonly string DejaVuBoldPath = FindDejaVuBold();

    private static string FindDejaVuBold()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "CHANGELOG.md")))
        {
            dir = dir.Parent;
        }

        return Path.Combine(dir!.FullName, "Src", "MatPlotLibNet.Skia", "Fonts", "DejaVuSans-Bold.ttf");
    }

    public SkiaFontsTests() => SkiaFonts.ResetForTests();

    [Fact]
    public void Register_FromFile_ReturnsTheFamilyTheFileDeclares()
    {
        var registered = SkiaFonts.Register(NotoSansHebrewPath);

        Assert.Equal(new RegisteredFont("Noto Sans Hebrew", FontWeight.Normal, FontSlant.Normal), registered);
    }

    [Fact]
    public void Register_FromStream_ReturnsTheFamilyTheStreamDeclares()
    {
        using var stream = File.OpenRead(NotoSansHebrewPath);

        var registered = SkiaFonts.Register(stream);

        Assert.Equal("Noto Sans Hebrew", registered.Family);
    }

    [Fact]
    public void Resolve_AfterRegister_ReturnsTheRegisteredFont()
    {
        SkiaFonts.Register(NotoSansHebrewPath);

        var resolved = SkiaFonts.Resolve("Noto Sans Hebrew", SKFontStyleWeight.Normal, SKFontStyleSlant.Upright);

        Assert.Equal("Noto Sans Hebrew", resolved.Typeface.FamilyName);
        Assert.NotEqual(0, resolved.Typeface.GetGlyph('ש'));
    }

    [Fact]
    public void Resolve_TakesTheWeightAndSlantOfTheFont()
    {
        var bold = SkiaFonts.Resolve(new Font { Family = "DejaVu Sans", Weight = FontWeight.Bold });
        var italic = SkiaFonts.Resolve(new Font { Family = "DejaVu Sans", Slant = FontSlant.Italic });

        Assert.True(bold.Typeface.IsBold);
        Assert.False(bold.Typeface.IsItalic);
        Assert.True(italic.Typeface.IsItalic);
        Assert.False(italic.Typeface.IsBold);
    }

    [Fact]
    public void Resolve_ReturnsOneSharedShaperPerTypeface()
    {
        var first = SkiaFonts.Resolve("DejaVu Sans", SKFontStyleWeight.Normal, SKFontStyleSlant.Upright);
        var second = SkiaFonts.Resolve("DejaVu Sans", SKFontStyleWeight.Normal, SKFontStyleSlant.Upright);

        Assert.Same(first.Typeface, second.Typeface);
        Assert.Same(first.Shaper, second.Shaper);
    }

    [Fact]
    public void AFamilyResolvedBeforeItWasRegistered_IsPickedUpAfterTheRegistration()
    {
        // Row B's natural order: a chart rendered first (the family falls back to whatever the OS has), then the
        // font file registered. The registration must reach every later render, cache or no cache.
        var before = SkiaFonts.Resolve("Noto Sans Hebrew", SKFontStyleWeight.Normal, SKFontStyleSlant.Upright);
        Assert.NotEqual("Noto Sans Hebrew", before.Typeface.FamilyName);

        SkiaFonts.Register(NotoSansHebrewPath);
        var after = SkiaFonts.Resolve("Noto Sans Hebrew", SKFontStyleWeight.Normal, SKFontStyleSlant.Upright);

        Assert.Equal("Noto Sans Hebrew", after.Typeface.FamilyName);
    }

    [Fact]
    public void Register_TheSameFamilyTwice_Throws()
    {
        SkiaFonts.Register(NotoSansHebrewPath);

        var ex = Assert.Throws<InvalidOperationException>(() => SkiaFonts.Register(NotoSansHebrewPath));

        Assert.Contains("Noto Sans Hebrew", ex.Message);
    }

    [Fact]
    public void Register_ABundledFamily_ReplacesIt_AndSaysSo()
    {
        var diagnostics = new List<ChartDiagnostic>();
        Action<ChartDiagnostic> handler = diagnostics.Add;
        ChartDiagnostics.Emitted += handler;
        try
        {
            var registered = SkiaFonts.Register(DejaVuBoldPath);

            Assert.Equal(new RegisteredFont("DejaVu Sans", FontWeight.Bold, FontSlant.Normal), registered);
            var diagnostic = Assert.Single(diagnostics, d => d.Message.Contains("DejaVu Sans", StringComparison.Ordinal));
            Assert.Equal("SkiaFonts", diagnostic.Source);
            Assert.Contains("bundled", diagnostic.Message);
        }
        finally
        {
            ChartDiagnostics.Emitted -= handler;
        }
    }

    [Fact]
    public void Register_AMissingFile_Throws()
    {
        var ex = Assert.Throws<FileNotFoundException>(() => SkiaFonts.Register(@"C:\no\such\font.ttf"));

        Assert.Contains("font.ttf", ex.Message);
    }

    [Fact]
    public void Register_AFileThatIsNotAFont_Throws()
    {
        string path = Path.GetTempFileName();
        File.WriteAllText(path, "not a font");
        try
        {
            var ex = Assert.Throws<ArgumentException>(() => SkiaFonts.Register(path));

            Assert.Contains(path, ex.Message);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Register_AnEmptyStream_Throws() =>
        Assert.Throws<ArgumentException>(() => SkiaFonts.Register(new MemoryStream()));

    [Fact]
    public void Resolve_AnUnknownFamily_SaysSoOnce_AndUsesWhatTheOsHas()
    {
        var diagnostics = new List<ChartDiagnostic>();
        Action<ChartDiagnostic> handler = diagnostics.Add;
        ChartDiagnostics.Emitted += handler;
        try
        {
            var first = SkiaFonts.Resolve("No Such Family XYZ", SKFontStyleWeight.Normal, SKFontStyleSlant.Upright);
            var second = SkiaFonts.Resolve("No Such Family XYZ", SKFontStyleWeight.Normal, SKFontStyleSlant.Upright);
            var bold = SkiaFonts.Resolve("No Such Family XYZ", SKFontStyleWeight.Bold, SKFontStyleSlant.Upright);

            Assert.NotNull(first.Typeface);
            Assert.Same(first.Typeface, second.Typeface);
            Assert.NotNull(bold.Typeface);
            var diagnostic = Assert.Single(diagnostics, d => d.Message.Contains("No Such Family XYZ", StringComparison.Ordinal));
            Assert.Equal("SkiaFonts", diagnostic.Source);
            Assert.Contains(first.Typeface.FamilyName, diagnostic.Message);
        }
        finally
        {
            ChartDiagnostics.Emitted -= handler;
        }
    }

    [Fact]
    public void Resolve_AnUnknownFamily_FromManyThreadsAtOnce_StillSaysSoOnce()
    {
        int emitted = 0;
        Action<ChartDiagnostic> handler = d =>
        {
            if (d.Message.Contains("Another Missing Family", StringComparison.Ordinal)) Interlocked.Increment(ref emitted);
        };
        ChartDiagnostics.Emitted += handler;
        try
        {
            Parallel.For(0, 64, _ => SkiaFonts.Resolve("Another Missing Family", SKFontStyleWeight.Normal, SKFontStyleSlant.Upright));

            Assert.Equal(1, emitted);
        }
        finally
        {
            ChartDiagnostics.Emitted -= handler;
        }
    }

    [Fact]
    public void Resolve_ABundledFamily_SaysNothing()
    {
        var diagnostics = new List<ChartDiagnostic>();
        Action<ChartDiagnostic> handler = diagnostics.Add;
        ChartDiagnostics.Emitted += handler;
        try
        {
            SkiaFonts.Resolve("DejaVu Sans, sans-serif", SKFontStyleWeight.Normal, SKFontStyleSlant.Upright);

            Assert.DoesNotContain(diagnostics, d => d.Message.Contains("DejaVu Sans", StringComparison.Ordinal));
        }
        finally
        {
            ChartDiagnostics.Emitted -= handler;
        }
    }

    [Theory]
    [InlineData(SKFontStyleWeight.Normal, SKFontStyleSlant.Upright, "DejaVu Sans")]
    [InlineData(SKFontStyleWeight.Bold, SKFontStyleSlant.Upright, "DejaVu Sans|Bold")]
    [InlineData(SKFontStyleWeight.Normal, SKFontStyleSlant.Italic, "DejaVu Sans|Italic")]
    [InlineData(SKFontStyleWeight.Bold, SKFontStyleSlant.Italic, "DejaVu Sans|BoldItalic")]
    public void BuildKey_NamesTheFamilyAndItsStyle(SKFontStyleWeight weight, SKFontStyleSlant slant, string expected)
    {
        using var style = new SKFontStyle(weight, SKFontStyleWidth.Normal, slant);

        Assert.Equal(expected, SkiaFonts.BuildKey("DejaVu Sans", style));
    }

    [Fact]
    public void Resolve_ACssFontStack_FindsTheFirstBundledFamily()
    {
        var resolved = SkiaFonts.Resolve("Helvetica, DejaVu Sans, sans-serif", SKFontStyleWeight.Normal, SKFontStyleSlant.Upright);

        Assert.Equal("DejaVu Sans", resolved.Typeface.FamilyName);
    }

    [Fact]
    public void Resolve_NullOrEmptyFamily_FallsBackToTheOs()
    {
        Assert.NotNull(SkiaFonts.Resolve(null, SKFontStyleWeight.Normal, SKFontStyleSlant.Upright).Typeface);
        Assert.NotNull(SkiaFonts.Resolve("", SKFontStyleWeight.Normal, SKFontStyleSlant.Upright).Typeface);
    }
}
