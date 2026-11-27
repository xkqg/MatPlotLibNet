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

    private static readonly string DejaVuBoldPath =
        Path.Combine(RepositoryRoot.Folder, "Src", "MatPlotLibNet.Skia", "Fonts", "DejaVuSans-Bold.ttf");

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
        var oblique = SkiaFonts.Resolve(new Font { Family = "DejaVu Sans", Slant = FontSlant.Oblique });

        Assert.True(bold.Typeface.IsBold);
        Assert.False(bold.Typeface.IsItalic);
        Assert.True(italic.Typeface.IsItalic);
        Assert.False(italic.Typeface.IsBold);
        Assert.Same(italic.Typeface, oblique.Typeface);   // an oblique font is set in the leaning face, as matplotlib sets it
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
        using var capture = new DiagnosticsCapture();
        var registered = SkiaFonts.Register(DejaVuBoldPath);

        Assert.Equal(new RegisteredFont("DejaVu Sans", FontWeight.Bold, FontSlant.Normal), registered);
        var diagnostic = Assert.Single(capture.Seen, d => d.Message.Contains("DejaVu Sans", StringComparison.Ordinal));
        Assert.Equal("SkiaFonts", diagnostic.Source);
        Assert.Contains("bundled", diagnostic.Message);
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
        using var capture = new DiagnosticsCapture();
        var first = SkiaFonts.Resolve("No Such Family XYZ", SKFontStyleWeight.Normal, SKFontStyleSlant.Upright);
        var second = SkiaFonts.Resolve("No Such Family XYZ", SKFontStyleWeight.Normal, SKFontStyleSlant.Upright);
        var bold = SkiaFonts.Resolve("No Such Family XYZ", SKFontStyleWeight.Bold, SKFontStyleSlant.Upright);

        Assert.NotNull(first.Typeface);
        Assert.Same(first.Typeface, second.Typeface);
        Assert.NotNull(bold.Typeface);
        var diagnostic = Assert.Single(capture.Seen, d => d.Message.Contains("No Such Family XYZ", StringComparison.Ordinal));
        Assert.Equal("SkiaFonts", diagnostic.Source);
        Assert.Contains(first.Typeface.FamilyName, diagnostic.Message);
    }

    [Fact]
    public void Resolve_AnUnknownFamily_FromManyThreadsAtOnce_StillSaysSoOnce()
    {
        var said = DiagnosticsCapture.During(() =>
        {
            Parallel.For(0, 64, _ => SkiaFonts.Resolve("Another Missing Family", SKFontStyleWeight.Normal, SKFontStyleSlant.Upright));
        });

        Assert.Equal(1, said.Count(d => d.Message.Contains("Another Missing Family", StringComparison.Ordinal)));
    }

    [Theory]
    [InlineData("DejaVu Sans, sans-serif", "DejaVu Sans")] // a bundled family
    [InlineData("sans-serif", "sans-serif")]               // the default family
    public void Resolve_ABundledOrTheDefaultFamily_SaysNothing(string family, string name)
    {
        using var capture = new DiagnosticsCapture();
        SkiaFonts.Resolve(family, SKFontStyleWeight.Normal, SKFontStyleSlant.Upright);

        Assert.DoesNotContain(capture.Seen, d => d.Message.Contains(name, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(SKFontStyleWeight.Normal, SKFontStyleSlant.Upright, "DejaVu Sans")]
    [InlineData(SKFontStyleWeight.Bold, SKFontStyleSlant.Upright, "DejaVu Sans|Bold")]
    [InlineData(SKFontStyleWeight.Normal, SKFontStyleSlant.Italic, "DejaVu Sans|Italic")]
    [InlineData(SKFontStyleWeight.Bold, SKFontStyleSlant.Italic, "DejaVu Sans|BoldItalic")]
    [InlineData(SKFontStyleWeight.Normal, SKFontStyleSlant.Oblique, "DejaVu Sans|Italic")]
    [InlineData(SKFontStyleWeight.SemiBold, SKFontStyleSlant.Oblique, "DejaVu Sans|BoldItalic")]
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

    [Theory]
    [InlineData(FontWeight.Normal, FontSlant.Normal)]
    [InlineData(FontWeight.Bold, FontSlant.Normal)]
    [InlineData(FontWeight.Normal, FontSlant.Italic)]
    [InlineData(FontWeight.Bold, FontSlant.Italic)]
    public void Resolve_TheDefaultFamily_IsTheBundledDejaVuSans_OnEveryMachine(FontWeight weight, FontSlant slant)
    {
        // matplotlib's default family is "sans-serif", and its first sans-serif face is DejaVu Sans. The package
        // carries that face, so the default text must not depend on what the machine has installed.
        var bundled = SkiaFonts.Resolve(new Font { Family = "DejaVu Sans", Weight = weight, Slant = slant });

        var resolved = SkiaFonts.Resolve(new Font { Weight = weight, Slant = slant });

        Assert.Equal("sans-serif", new Font().Family);
        Assert.Equal("DejaVu Sans", resolved.Typeface.FamilyName);
        Assert.Same(bundled.Typeface, resolved.Typeface);
    }

    [Fact]
    public void Resolve_ANamedInstalledFamilyBeforeSansSerif_StillWins()
    {
        // "sans-serif" is the last resort of a stack, as in CSS: a family the machine has, named before it, is used.
        string? installed = SKFontManager.Default.FontFamilies.FirstOrDefault(name =>
        {
            if (string.Equals(name, "DejaVu Sans", StringComparison.OrdinalIgnoreCase)) return false;
            using var face = SKTypeface.FromFamilyName(name);
            return face is not null && string.Equals(face.FamilyName, name, StringComparison.OrdinalIgnoreCase);
        });
        if (installed is null)
        {
            Assert.Skip("This machine has no installed font family other than DejaVu Sans.");
        }

        var resolved = SkiaFonts.Resolve($"{installed}, sans-serif", SKFontStyleWeight.Normal, SKFontStyleSlant.Upright);

        Assert.Equal(installed, resolved.Typeface.FamilyName, ignoreCase: true);
    }

    [Fact]
    public void Resolve_SansSerif_FollowsAFontFileThatReplacesTheBundledFace()
    {
        var registered = SkiaFonts.Register(DejaVuBoldPath);

        var named = SkiaFonts.Resolve("DejaVu Sans", SKFontStyleWeight.Bold, SKFontStyleSlant.Upright);
        var generic = SkiaFonts.Resolve("sans-serif", SKFontStyleWeight.Bold, SKFontStyleSlant.Upright);

        Assert.Equal("DejaVu Sans", registered.Family);
        Assert.Same(named.Typeface, generic.Typeface);
    }

    [Fact]
    public void Resolve_NullOrEmptyFamily_FallsBackToTheOs()
    {
        Assert.NotNull(SkiaFonts.Resolve(null, SKFontStyleWeight.Normal, SKFontStyleSlant.Upright).Typeface);
        Assert.NotNull(SkiaFonts.Resolve("", SKFontStyleWeight.Normal, SKFontStyleSlant.Upright).Typeface);
    }
}
