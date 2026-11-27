// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>What matplotlib 3.11.2 takes from its STIX fonts (<c>BakomaFonts</c> falls back to <c>StixFonts</c>, <c>DejaVuFonts</c> to
/// <c>StixSansFonts</c>) is set in a STIX face by the sets that compose <see cref="StixFallback"/>, as matplotlib sets it: for every
/// character the grammar can put in math, under every font switch, the face and the code matplotlib's parse set (the reference
/// <c>matplotlib-mathtext.json</c> that <c>tools/mpl_reference/generate_mathtext.py</c> writes from the parse, beside the table). A
/// character and switch matplotlib sets in a face of the set itself is not set in a STIX face here.</summary>
public class StixFallbackContractTests
{
    private static readonly Font Label = new() { Family = "DejaVu Sans", Size = 10 };

    /// <summary>The font switches in the order of <see cref="FontVariant"/>, as the reference lists a row.</summary>
    private static readonly FontVariant[] Variants =
        [FontVariant.Default, FontVariant.Roman, FontVariant.Bold, FontVariant.Italic, FontVariant.Calligraphic, FontVariant.BlackboardBold];

    private static readonly JsonElement Whole = LoadReference();

    private static readonly JsonElement Reference = Whole.GetProperty("stixFallback");

    private static JsonElement LoadReference()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "Rendering", "MathText", "matplotlib-mathtext.json")));
        return doc.RootElement.Clone();
    }

    /// <summary>The family, weight and slant of a face as the reference names it.</summary>
    private static (string Family, FontWeight Weight, FontSlant Slant) FaceNamed(string face) => face switch
    {
        "Regular" => ("STIXGeneral", FontWeight.Normal, FontSlant.Normal),
        "Italic" => ("STIXGeneral", FontWeight.Normal, FontSlant.Italic),
        "Bold" => ("STIXGeneral", FontWeight.Bold, FontSlant.Normal),
        "NonUnicode" => ("STIXNonUnicode", FontWeight.Normal, FontSlant.Normal),
        "NonUnicodeItalic" => ("STIXNonUnicode", FontWeight.Normal, FontSlant.Italic),
        _ => throw new ArgumentException($"the reference names a STIX face this test does not know: {face}", nameof(face)),
    };

    public static TheoryData<string> Hosts { get; } = new("cm", "dejavusans");

    [Theory]
    [MemberData(nameof(Hosts))]
    public void EveryCharacterAndSwitch_IsSetInTheSTIXGlyphMatplotlibSetsItIn(string host)
    {
        var set = MathFontSet.Named(host);
        var wrong = new List<string>();
        int rows = 0;
        foreach (var row in Reference.GetProperty(host).EnumerateObject())
        {
            rows++;
            char c = (char)int.Parse(row.Name, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            var cells = row.Value.GetString()!.Split(',');
            for (int v = 0; v < Variants.Length; v++)
            {
                var runs = set.RunsOf(c.ToString(), Variants[v], false);
                var fonts = Enumerable.Range(0, runs.Count).Select(i => (Font: runs[i].Face.In(Label), runs[i].Text)).ToArray();
                if (cells[v].Length == 0)
                {
                    if (fonts.Any(f => f.Font.Family.StartsWith("STIX", StringComparison.Ordinal)))
                    {
                        wrong.Add($"{host} U+{(int)c:X4} {Variants[v]}: set in STIX, matplotlib sets it in a face of the set");
                    }

                    continue;
                }

                var (face, code) = (cells[v][..cells[v].IndexOf(':')], int.Parse(cells[v][(cells[v].IndexOf(':') + 1)..], NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                var (family, weight, slant) = FaceNamed(face);
                if (fonts.Length != 1 || fonts[0].Font.Family != family || fonts[0].Font.Weight != weight || fonts[0].Font.Slant != slant
                    || fonts[0].Text != char.ConvertFromUtf32(code))
                {
                    wrong.Add($"{host} U+{(int)c:X4} {Variants[v]}: matplotlib {face} U+{code:X}, here {string.Join('|', fonts.Select(f => $"{f.Font.Family}/{f.Font.Weight}/{f.Font.Slant}/{f.Text}"))}");
                }
            }
        }

        Assert.True(rows >= 600, $"the reference holds {rows} characters for {host}; the table is read from matplotlib and holds more than 600");
        Assert.True(wrong.Count == 0, $"{wrong.Count} glyphs differ:\n" + string.Join('\n', wrong.Take(20)));
    }

    /// <summary>The family, weight and slant of a face as the reference names it by font family and style.</summary>
    private static (string Family, FontWeight Weight, FontSlant Slant) SizeFaceNamed(string family, string style) =>
        (family, style.Contains("Bold", StringComparison.Ordinal) ? FontWeight.Bold : FontWeight.Normal,
            style.Contains("Italic", StringComparison.Ordinal) || style.Contains("Oblique", StringComparison.Ordinal) ? FontSlant.Italic : FontSlant.Normal);

    [Theory]
    [MemberData(nameof(Hosts))]
    public void EveryDelimiter_IsChosenAmongTheSizesMatplotlibChoosesAmong(string host)
    {
        var set = MathFontSet.Named(host);
        var wrong = new List<string>();
        int rows = 0;
        foreach (var row in Whole.GetProperty("sizedAlternatives").GetProperty(host).EnumerateObject())
        {
            rows++;
            char c = (char)int.Parse(row.Name, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            var expectedRaw = row.Value.EnumerateArray().Select(e => e.GetString()!).ToArray();
            bool natural = expectedRaw[0].StartsWith('*');
            var expected = expectedRaw.Select(e => e.TrimStart('*')).ToArray();
            var sizes = set.SizedAlternatives(c.ToString());
            var actual = sizes.Glyphs;
            if (sizes.FirstIsNatural != natural)
            {
                wrong.Add($"{host} U+{(int)c:X4}: matplotlib {(natural ? "keeps" : "scales")} its first size, here {(sizes.FirstIsNatural ? "keeps" : "scales")} it");
            }

            if (actual.Length != expected.Length)
            {
                wrong.Add($"{host} U+{(int)c:X4}: matplotlib chooses among {expected.Length} sizes, here {actual.Length}");
                continue;
            }

            for (int i = 0; i < expected.Length; i++)
            {
                string face = expected[i][..expected[i].IndexOf(':')];
                int code = int.Parse(expected[i][(expected[i].IndexOf(':') + 1)..], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                var (family, weight, slant) = SizeFaceNamed(face[..face.IndexOf('/')], face[(face.IndexOf('/') + 1)..]);
                var font = actual[i].Face.In(Label);
                if (font.Family != family || font.Weight != weight || font.Slant != slant || actual[i].Code != code)
                {
                    wrong.Add($"{host} U+{(int)c:X4} size {i}: matplotlib {expected[i]}, here {font.Family}/{font.Weight}/{font.Slant}:{actual[i].Code:X}");
                }
            }
        }

        Assert.True(rows >= 25, $"the reference holds {rows} delimiters for {host}");
        Assert.True(wrong.Count == 0, $"{wrong.Count} sizes differ:\n" + string.Join('\n', wrong.Take(20)));
    }

    [Fact]
    public void TheLibrarysOwnSet_HasNoSizesOfItsOwn_AndAnUnlistedCharacterHasNone()
    {
        Assert.Empty(MathFontSet.Library.SizedAlternatives("(").Glyphs);
        Assert.Empty(MathFontSet.DejaVuSans.SizedAlternatives("x").Glyphs);
        Assert.Empty(MathFontSet.ComputerModern.SizedAlternatives(string.Empty).Glyphs);
    }

    [Theory]
    [InlineData("\u05D0")]
    [InlineData("\U0001D465")]
    public void ACharacterNoTableHolds_IsSetInTheLabelsFace_NotInASTIXFace(string text)
    {
        // A Hebrew letter and a character beyond the Basic Multilingual Plane: matplotlib sets a dummy for them under 'cm'. The label's
        // own face is the one that may hold them. (DejaVu Sans sets any character itself: the Skia package falls back for a glyph it lacks.)
        var runs = MathFontSet.ComputerModern.RunsOf(text, FontVariant.Roman, false);

        Assert.Equal(1, runs.Count);
        Assert.Same(Label, runs[0].Face.In(Label));
        Assert.Equal(text, runs[0].Text);
    }

    [Fact]
    public void ALoneSurrogate_IsSetAsWritten_WithoutFailing()
    {
        // A code above U+FFFF becomes two UTF-16 units; a lone surrogate of a broken text is no code point and is kept as written.
        var runs = MathFontSet.ComputerModern.RunsOf("2\uD800", FontVariant.Default, false);

        Assert.Equal("2\uD800", string.Concat(Enumerable.Range(0, runs.Count).Select(i => runs[i].Text)));
    }
}
