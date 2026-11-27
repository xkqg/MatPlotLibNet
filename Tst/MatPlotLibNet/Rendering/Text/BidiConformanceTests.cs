// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.IO.Compression;
using System.Text;
using MatPlotLibNet.Rendering.Text;
using MatPlotLibNet.Tests.Packaging;

namespace MatPlotLibNet.Tests.Rendering.Text;

/// <summary>Unicode's own conformance corpora, run in full. BidiTest.txt gives sequences of bidi CLASSES with the
/// expected levels and visual order for every paragraph direction; BidiCharacterTest.txt gives real code points,
/// including the bracket pairs BidiTest leaves out. Every line is a case; the first divergent lines are reported with
/// their line number, and the count of cases run is written to the test output so a silently shrunken corpus shows.</summary>
public sealed class BidiConformanceTests
{
    private const int ReportedFailures = 25;

    /// <summary>The corpora separate tokens with spaces or tabs.</summary>
    private static readonly char[] Separators = [' ', '	'];

    /// <summary>One representative character per bidi class, for the class-sequence corpus. The table itself is
    /// asserted to classify each sample as its class, so the map cannot drift from the data it stands in for.</summary>
    private static readonly Dictionary<string, char> Samples = new()
    {
        ["L"] = 'a', ["R"] = 'א', ["AL"] = 'ا', ["EN"] = '0', ["ES"] = '+', ["ET"] = '$',
        ["AN"] = '٠', ["CS"] = ',', ["NSM"] = '\u0300', ["BN"] = '\u200B', ["B"] = '\u2029', ["S"] = '\t',
        ["WS"] = ' ', ["ON"] = '!', ["LRE"] = '\u202A', ["LRO"] = '\u202D', ["RLE"] = '\u202B', ["RLO"] = '\u202E',
        ["PDF"] = '\u202C', ["LRI"] = '\u2066', ["RLI"] = '\u2067', ["FSI"] = '\u2068', ["PDI"] = '\u2069',
    };

    [Fact]
    public void EverySampleCharacter_HasTheClassItStandsFor()
    {
        foreach (var (name, sample) in Samples)
        {
            Assert.Equal(Enum.Parse<BidiClass>(name), BidiClassTable.GetClass(sample));
        }
    }

    [Fact]
    public void BidiTest_EveryCase_ResolvesToTheReferenceLevelsAndOrder()
    {
        var failures = new List<string>();
        int cases = 0;
        string[] expectedLevels = [];
        int[] expectedOrder = [];
        int lineNumber = 0;

        foreach (string raw in ReadCorpus("BidiTest.txt.gz"))
        {
            lineNumber++;
            string line = raw.Split('#', 2)[0].Trim();
            if (line.Length == 0)
            {
                continue;
            }

            if (line.StartsWith("@Levels:", StringComparison.Ordinal))
            {
                expectedLevels = line["@Levels:".Length..].Split(Separators, StringSplitOptions.RemoveEmptyEntries);
                continue;
            }

            if (line.StartsWith("@Reorder:", StringComparison.Ordinal))
            {
                expectedOrder = line["@Reorder:".Length..]
                    .Split(Separators, StringSplitOptions.RemoveEmptyEntries)
                    .Select(v => int.Parse(v, CultureInfo.InvariantCulture)).ToArray();
                continue;
            }

            if (line[0] == '@')
            {
                continue; // forward-compatible: any other directive is ignored
            }

            string[] parts = line.Split(';');
            string[] classes = parts[0].Split(Separators, StringSplitOptions.RemoveEmptyEntries);
            int bitset = int.Parse(parts[1].Trim(), CultureInfo.InvariantCulture);
            var text = new string(classes.Select(c => Samples[c]).ToArray());

            // Bit 1 = auto (rules P2 and P3), bit 2 = left-to-right, bit 4 = right-to-left.
            foreach (var (bit, direction) in new (int, BidiAlgorithm.BidiDirection?)[] { (1, null), (2, BidiAlgorithm.BidiDirection.LeftToRight), (4, BidiAlgorithm.BidiDirection.RightToLeft) })
            {
                if ((bitset & bit) == 0)
                {
                    continue;
                }

                cases++;
                var paragraph = BidiAlgorithm.Resolve(text, direction);
                string[] actualLevels = paragraph.Levels.Select(FormatLevel).ToArray();
                int[] actualOrder = paragraph.Order;
                if (!actualLevels.SequenceEqual(expectedLevels) || !actualOrder.SequenceEqual(expectedOrder))
                {
                    if (failures.Count < ReportedFailures)
                    {
                        failures.Add($"line {lineNumber} [{parts[0].Trim()}; dir {direction?.ToString() ?? "auto"}]: "
                            + $"levels {string.Join(' ', actualLevels)} (expected {string.Join(' ', expectedLevels)}), "
                            + $"order {string.Join(' ', actualOrder)} (expected {string.Join(' ', expectedOrder)})");
                    }
                    else
                    {
                        failures.Add("…");
                        break;
                    }
                }
            }

            if (failures.Count > ReportedFailures)
            {
                break;
            }
        }

        Console.WriteLine($"BidiTest.txt: {cases} cases");
        Assert.True(cases > 400_000, $"the corpus shrank: {cases} cases");
        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    [Fact]
    public void BidiCharacterTest_EveryCase_ResolvesToTheReferenceLevelsAndOrder()
    {
        var failures = new List<string>();
        int cases = 0;
        int lineNumber = 0;

        foreach (string raw in ReadCorpus("BidiCharacterTest.txt.gz"))
        {
            lineNumber++;
            string line = raw.Split('#', 2)[0].Trim();
            if (line.Length == 0)
            {
                continue;
            }

            string[] fields = line.Split(';');
            int[] codePoints = fields[0].Split(Separators, StringSplitOptions.RemoveEmptyEntries)
                .Select(h => int.Parse(h, NumberStyles.HexNumber, CultureInfo.InvariantCulture)).ToArray();
            BidiAlgorithm.BidiDirection? direction = fields[1].Trim() switch
            {
                "0" => BidiAlgorithm.BidiDirection.LeftToRight,
                "1" => BidiAlgorithm.BidiDirection.RightToLeft,
                _ => null,
            };
            int expectedParagraphLevel = int.Parse(fields[2].Trim(), CultureInfo.InvariantCulture);
            string[] expectedLevels = fields[3].Split(Separators, StringSplitOptions.RemoveEmptyEntries);
            int[] expectedOrder = fields[4].Split(Separators, StringSplitOptions.RemoveEmptyEntries)
                .Select(v => int.Parse(v, CultureInfo.InvariantCulture)).ToArray();

            // The corpus counts in code points; the algorithm reports per UTF-16 unit. Map through the first
            // unit of each code point.
            var text = new StringBuilder();
            var firstUnit = new int[codePoints.Length];
            for (int i = 0; i < codePoints.Length; i++)
            {
                firstUnit[i] = text.Length;
                text.Append(char.ConvertFromUtf32(codePoints[i]));
            }

            cases++;
            var paragraph = BidiAlgorithm.Resolve(text.ToString(), direction);
            string[] actualLevels = firstUnit.Select(u => FormatLevel(paragraph.Levels[u])).ToArray();
            var unitToCodePoint = new Dictionary<int, int>();
            for (int i = 0; i < firstUnit.Length; i++)
            {
                unitToCodePoint[firstUnit[i]] = i;
            }

            int[] actualOrder = paragraph.Order.Where(unitToCodePoint.ContainsKey).Select(u => unitToCodePoint[u]).ToArray();
            if (paragraph.ParagraphLevel != expectedParagraphLevel
                || !actualLevels.SequenceEqual(expectedLevels)
                || !actualOrder.SequenceEqual(expectedOrder))
            {
                failures.Add($"line {lineNumber} [{fields[0].Trim()}; dir {fields[1].Trim()}]: "
                    + $"paragraph {paragraph.ParagraphLevel} (expected {expectedParagraphLevel}), "
                    + $"levels {string.Join(' ', actualLevels)} (expected {string.Join(' ', expectedLevels)}), "
                    + $"order {string.Join(' ', actualOrder)} (expected {string.Join(' ', expectedOrder)})");
                if (failures.Count >= ReportedFailures)
                {
                    break;
                }
            }
        }

        Console.WriteLine($"BidiCharacterTest.txt: {cases} cases");
        Assert.True(cases > 90_000, $"the corpus shrank: {cases} cases");
        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    [Fact]
    public void BidiCharacterTest_EveryAutoDirectionCase_TheParagraphDirectionAloneMatchesTheReference()
    {
        // The writer only asks which way a label's paragraph runs (rules P2 and P3), and asks it for every label it
        // draws. That answer, taken on its own, must agree with the full resolution for the text of every case (the
        // corpus holds 28 cases whose direction the text decides, too few to prove anything alone), and with the
        // reference paragraph level of each of those 28.
        var failures = new List<string>();
        int cases = 0;
        int autoCases = 0;
        int lineNumber = 0;

        foreach (string raw in ReadCorpus("BidiCharacterTest.txt.gz"))
        {
            lineNumber++;
            string line = raw.Split('#', 2)[0].Trim();
            if (line.Length == 0)
            {
                continue;
            }

            string[] fields = line.Split(';');
            var builder = new StringBuilder();
            foreach (string hex in fields[0].Split(Separators, StringSplitOptions.RemoveEmptyEntries))
            {
                builder.Append(char.ConvertFromUtf32(int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture)));
            }
            string text = builder.ToString();

            cases++;
            bool answer = BidiAlgorithm.IsRightToLeftParagraph(text);
            bool expected = (BidiAlgorithm.Resolve(text).ParagraphLevel == 1);
            if (fields[1].Trim() == "2")
            {
                autoCases++;
                expected = int.Parse(fields[2].Trim(), CultureInfo.InvariantCulture) == 1;
            }

            if (answer != expected)
            {
                failures.Add($"line {lineNumber} [{fields[0].Trim()}]: expected {(expected ? "right to left" : "left to right")}");
                if (failures.Count >= ReportedFailures)
                {
                    break;
                }
            }
        }

        Assert.True(cases > 90_000, $"the corpus shrank: {cases} cases");
        Assert.True(autoCases >= 28, $"the auto-direction cases shrank: {autoCases}");
        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    /// <summary>The classes that send a paragraph through the whole resolution. A paragraph with none of them, read left to
    /// right or left to decide for itself, resolves to level 0 in logical order, which the algorithm answers from one scan of
    /// its classes.</summary>
    private static readonly HashSet<BidiClass> RightToLeftOrExplicit =
    [
        BidiClass.R, BidiClass.AL, BidiClass.AN, BidiClass.LRE, BidiClass.LRO, BidiClass.RLE, BidiClass.RLO, BidiClass.PDF,
        BidiClass.LRI, BidiClass.RLI, BidiClass.FSI, BidiClass.PDI, BidiClass.BN,
    ];

    [Fact]
    public void BothCorpora_EveryCaseWithNothingRightToLeft_IsLevelZeroInLogicalOrder()
    {
        // The reference itself says level 0 in logical order for every such case, and the algorithm says the same: the
        // short answer agrees with the whole resolution over every case of Unicode's corpora it covers.
        var failures = new List<string>();
        int classCases = 0;
        int lineNumber = 0;
        string[] expectedLevels = [];
        int[] expectedOrder = [];
        foreach (string raw in ReadCorpus("BidiTest.txt.gz"))
        {
            lineNumber++;
            string line = raw.Split('#', 2)[0].Trim();
            if (line.Length == 0) continue;
            if (line.StartsWith("@Levels:", StringComparison.Ordinal))
            {
                expectedLevels = line["@Levels:".Length..].Split(Separators, StringSplitOptions.RemoveEmptyEntries);
                continue;
            }

            if (line.StartsWith("@Reorder:", StringComparison.Ordinal))
            {
                expectedOrder = line["@Reorder:".Length..].Split(Separators, StringSplitOptions.RemoveEmptyEntries)
                    .Select(v => int.Parse(v, CultureInfo.InvariantCulture)).ToArray();
                continue;
            }

            if (line[0] == '@') continue;
            string[] parts = line.Split(';');
            string[] classes = parts[0].Split(Separators, StringSplitOptions.RemoveEmptyEntries);
            if (classes.Any(c => RightToLeftOrExplicit.Contains(Enum.Parse<BidiClass>(c)))) continue;

            int bitset = int.Parse(parts[1].Trim(), CultureInfo.InvariantCulture);
            var text = new string(classes.Select(c => Samples[c]).ToArray());
            foreach (var (bit, direction) in new (int, BidiAlgorithm.BidiDirection?)[] { (1, null), (2, BidiAlgorithm.BidiDirection.LeftToRight) })
            {
                if ((bitset & bit) == 0) continue;
                classCases++;
                var paragraph = BidiAlgorithm.Resolve(text, direction);
                if (!expectedLevels.All(l => l == "0") || !expectedOrder.SequenceEqual(Enumerable.Range(0, classes.Length))
                    || paragraph.Levels.Any(l => l != 0) || !paragraph.Order.SequenceEqual(Enumerable.Range(0, text.Length)))
                {
                    failures.Add($"BidiTest line {lineNumber} [{parts[0].Trim()}]");
                }
            }
        }

        int characterCases = 0;
        lineNumber = 0;
        foreach (string raw in ReadCorpus("BidiCharacterTest.txt.gz"))
        {
            lineNumber++;
            string line = raw.Split('#', 2)[0].Trim();
            if (line.Length == 0) continue;
            string[] fields = line.Split(';');
            if (fields[1].Trim() == "1") continue;
            int[] codePoints = fields[0].Split(Separators, StringSplitOptions.RemoveEmptyEntries)
                .Select(h => int.Parse(h, NumberStyles.HexNumber, CultureInfo.InvariantCulture)).ToArray();
            if (codePoints.Any(c => RightToLeftOrExplicit.Contains(BidiClassTable.GetClass(c)))) continue;

            characterCases++;
            var text = string.Concat(codePoints.Select(char.ConvertFromUtf32));
            var paragraph = BidiAlgorithm.Resolve(text, fields[1].Trim() == "0" ? BidiAlgorithm.BidiDirection.LeftToRight : null);
            if (fields[2].Trim() != "0" || fields[3].Split(Separators, StringSplitOptions.RemoveEmptyEntries).Any(l => l != "0")
                || !fields[4].Split(Separators, StringSplitOptions.RemoveEmptyEntries).Select(v => int.Parse(v, CultureInfo.InvariantCulture))
                    .SequenceEqual(Enumerable.Range(0, codePoints.Length))
                || paragraph.Levels.Any(l => l != 0) || paragraph.ParagraphLevel != 0)
            {
                failures.Add($"BidiCharacterTest line {lineNumber} [{fields[0].Trim()}]");
            }
        }

        Console.WriteLine($"cases with nothing right to left: {classCases} of BidiTest.txt, {characterCases} of BidiCharacterTest.txt");
        Assert.True(classCases > 16_000, $"the class cases shrank: {classCases}");
        Assert.True(characterCases > 8_000, $"the character cases shrank: {characterCases}");
        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures.Take(ReportedFailures)));
    }

    private static string FormatLevel(byte level) =>
        level == BidiParagraph.Removed ? "x" : level.ToString(CultureInfo.InvariantCulture);

    private static IEnumerable<string> ReadCorpus(string fileName)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Rendering", "Text", "Ucd", fileName);
        using var file = File.OpenRead(path);
        using var gzip = new GZipStream(file, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip, Encoding.UTF8);
        foreach (string line in SourceFile.LinesOf(reader.ReadToEnd()))
        {
            yield return line;
        }
    }
}
