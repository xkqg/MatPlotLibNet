// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Rendering.Text;

namespace MatPlotLibNet.Tests.Rendering.Text;

/// <summary>The generated tables are a copy of the Unicode Character Database, and a copy drifts. These facts pin the
/// copy to its source: the class of a known character, the version both files claim, and a full regeneration from the
/// checked-in UCD text compared range for range with what the generator committed.</summary>
public sealed class BidiClassTableTests
{
    /// <summary>The repository root, found the way ReleaseContractTests finds it: walk up until CHANGELOG.md.</summary>
    internal static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "CHANGELOG.md")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return dir!.FullName;
    }

    [Theory]
    [InlineData(0x0041, "L")]    // A
    [InlineData(0x05D0, "R")]    // א
    [InlineData(0x0627, "AL")]   // ا
    [InlineData(0x0030, "EN")]   // 0
    [InlineData(0x0660, "AN")]   // ٠
    [InlineData(0x002C, "CS")]   // ,
    [InlineData(0x002B, "ES")]   // +
    [InlineData(0x0024, "ET")]   // $
    [InlineData(0x0020, "WS")]   // space
    [InlineData(0x000A, "B")]    // line feed
    [InlineData(0x0009, "S")]    // tab
    [InlineData(0x0028, "ON")]   // (
    [InlineData(0x05C1, "NSM")]  // Hebrew point shin dot
    [InlineData(0x200D, "BN")]   // zero width joiner
    [InlineData(0x202E, "RLO")]
    [InlineData(0x2068, "FSI")]
    [InlineData(0x2069, "PDI")]
    [InlineData(0x0924, "L")]    // Devanagari ta
    [InlineData(0x08A0, "AL")]   // Arabic Extended-A
    [InlineData(0x10800, "R")]   // Cypriot syllabary (astral, explicit R)
    [InlineData(0x1EC70, "AL")]  // unassigned inside an Arabic default block: the @missing default applies
    [InlineData(0xFDD0, "BN")]   // a noncharacter is explicitly BN
    [InlineData(0x10FFFF, "BN")] // the last noncharacter
    [InlineData(0xE0001, "BN")]  // a tag character
    public void GetClass_ReturnsTheUnicodeClass(int codePoint, string expected) =>
        Assert.Equal(Enum.Parse<BidiClass>(expected), BidiClassTable.GetClass(codePoint));

    [Fact]
    public void GetClass_OutsideUnicode_IsLeftToRight()
    {
        Assert.Equal(BidiClass.L, BidiClassTable.GetClass(-1));
        Assert.Equal(BidiClass.L, BidiClassTable.GetClass(0x110000));
    }

    [Fact]
    public void TheTables_CarryTheVersionOfTheirSourceFiles()
    {
        string root = RepoRoot();
        string classesHeader = File.ReadLines(Path.Combine(root, "tools", "unicode", "ucd", "DerivedBidiClass.txt")).First();
        string bracketsHeader = File.ReadLines(Path.Combine(root, "tools", "unicode", "ucd", "BidiBrackets.txt")).First();

        Assert.Equal($"# DerivedBidiClass-{BidiClassTable.UnicodeVersion}.txt", classesHeader);
        Assert.Equal($"# BidiBrackets-{BidiBrackets.UnicodeVersion}.txt", bracketsHeader);
        Assert.Equal(BidiClassTable.UnicodeVersion, BidiBrackets.UnicodeVersion);
    }

    [Fact]
    public void TheCommittedClassTable_MatchesARegenerationFromTheSource()
    {
        // The same painting rule the generator applies: every @missing default in file order, then every explicit
        // assignment on top. Compressed to ranges and compared entry for entry with what is compiled in.
        var expected = RangesFromSource(Path.Combine(RepoRoot(), "tools", "unicode", "ucd", "DerivedBidiClass.txt"));

        Assert.Equal(expected.Count, BidiClassTable.RangeCount);
        for (int i = 0; i < expected.Count; i++)
        {
            Assert.Equal(expected[i], BidiClassTable.Range(i));
        }
    }

    [Fact]
    public void TheCommittedBracketTable_MatchesARegenerationFromTheSource()
    {
        var expected = new List<(int CodePoint, int Pair, bool Opens)>();
        foreach (var raw in File.ReadLines(Path.Combine(RepoRoot(), "tools", "unicode", "ucd", "BidiBrackets.txt")))
        {
            string line = raw.Split('#', 2)[0].Trim();
            if (line.Length == 0)
            {
                continue;
            }

            string[] fields = line.Split(';');
            expected.Add((
                int.Parse(fields[0].Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                int.Parse(fields[1].Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                fields[2].Trim() == "o"));
        }

        expected.Sort((a, b) => a.CodePoint.CompareTo(b.CodePoint));
        Assert.Equal(expected.Count, BidiBrackets.Count);
        for (int i = 0; i < expected.Count; i++)
        {
            Assert.True(BidiBrackets.TryGetPair(expected[i].CodePoint, out int pair, out bool opens));
            Assert.Equal(expected[i].Pair, pair);
            Assert.Equal(expected[i].Opens, opens);
        }
    }

    [Theory]
    [InlineData(0x0028, 0x0029, true)]   // ( → )
    [InlineData(0x0029, 0x0028, false)]  // ) → (
    [InlineData(0x005B, 0x005D, true)]   // [ → ]
    [InlineData(0x3008, 0x3009, true)]   // 〈 → 〉
    public void TryGetPair_KnowsTheBrackets(int codePoint, int expectedPair, bool expectedOpens)
    {
        Assert.True(BidiBrackets.TryGetPair(codePoint, out int pair, out bool opens));
        Assert.Equal(expectedPair, pair);
        Assert.Equal(expectedOpens, opens);
    }

    [Fact]
    public void TryGetPair_ANonBracket_IsFalse() => Assert.False(BidiBrackets.TryGetPair('A', out _, out _));

    [Theory]
    [InlineData(0x2329, 0x3008)] // 〈 (deprecated) is canonically equivalent to 〈
    [InlineData(0x232A, 0x3009)]
    [InlineData(0x0028, 0x0028)] // everything else is its own canonical form
    public void Canonical_FoldsTheTwoEquivalentPairs(int codePoint, int expected) =>
        Assert.Equal(expected, BidiBrackets.Canonical(codePoint));

    private static List<BidiClassRange> RangesFromSource(string path)
    {
        var longNames = new Dictionary<string, BidiClass>
        {
            ["Left_To_Right"] = BidiClass.L, ["Right_To_Left"] = BidiClass.R, ["Arabic_Letter"] = BidiClass.AL,
            ["European_Number"] = BidiClass.EN, ["European_Separator"] = BidiClass.ES,
            ["European_Terminator"] = BidiClass.ET, ["Arabic_Number"] = BidiClass.AN,
            ["Common_Separator"] = BidiClass.CS, ["Nonspacing_Mark"] = BidiClass.NSM,
            ["Boundary_Neutral"] = BidiClass.BN, ["Paragraph_Separator"] = BidiClass.B,
            ["Segment_Separator"] = BidiClass.S, ["White_Space"] = BidiClass.WS, ["Other_Neutral"] = BidiClass.ON,
            ["Left_To_Right_Embedding"] = BidiClass.LRE, ["Left_To_Right_Override"] = BidiClass.LRO,
            ["Right_To_Left_Embedding"] = BidiClass.RLE, ["Right_To_Left_Override"] = BidiClass.RLO,
            ["Pop_Directional_Format"] = BidiClass.PDF, ["Left_To_Right_Isolate"] = BidiClass.LRI,
            ["Right_To_Left_Isolate"] = BidiClass.RLI, ["First_Strong_Isolate"] = BidiClass.FSI,
            ["Pop_Directional_Isolate"] = BidiClass.PDI,
        };

        var painted = new BidiClass[0x110000];
        var explicitLines = new List<(int Lo, int Hi, BidiClass Class)>();
        foreach (var raw in File.ReadLines(path))
        {
            if (raw.StartsWith("# @missing:", StringComparison.Ordinal))
            {
                string[] parts = raw["# @missing:".Length..].Split(';');
                var (lo, hi) = ParseRange(parts[0]);
                Array.Fill(painted, longNames[parts[1].Trim()], lo, hi - lo + 1);
                continue;
            }

            string line = raw.Split('#', 2)[0].Trim();
            if (line.Length == 0)
            {
                continue;
            }

            string[] fields = line.Split(';');
            var (start, end) = ParseRange(fields[0]);
            explicitLines.Add((start, end, Enum.Parse<BidiClass>(fields[1].Trim())));
        }

        foreach (var (lo, hi, cls) in explicitLines)
        {
            Array.Fill(painted, cls, lo, hi - lo + 1);
        }

        var ranges = new List<BidiClassRange>();
        int rangeStart = 0;
        for (int cp = 1; cp <= painted.Length; cp++)
        {
            if (cp == painted.Length || painted[cp] != painted[rangeStart])
            {
                ranges.Add(new BidiClassRange(rangeStart, cp - 1, painted[rangeStart]));
                rangeStart = cp;
            }
        }

        return ranges;
    }

    private static (int Lo, int Hi) ParseRange(string field)
    {
        field = field.Trim();
        int dots = field.IndexOf("..", StringComparison.Ordinal);
        if (dots < 0)
        {
            int single = int.Parse(field, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return (single, single);
        }

        return (int.Parse(field[..dots], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                int.Parse(field[(dots + 2)..], NumberStyles.HexNumber, CultureInfo.InvariantCulture));
    }
}
