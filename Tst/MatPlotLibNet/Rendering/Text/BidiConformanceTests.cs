// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.IO.Compression;
using System.Text;
using MatPlotLibNet.Rendering.Text;

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
            foreach (var (bit, direction) in new (int, BidiDirection?)[] { (1, null), (2, BidiDirection.LeftToRight), (4, BidiDirection.RightToLeft) })
            {
                if ((bitset & bit) == 0)
                {
                    continue;
                }

                cases++;
                var paragraph = BidiAlgorithm.Resolve(text, direction);
                string[] actualLevels = paragraph.Levels.Select(FormatLevel).ToArray();
                int[] actualOrder = paragraph.VisualOrder();
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
            BidiDirection? direction = fields[1].Trim() switch
            {
                "0" => BidiDirection.LeftToRight,
                "1" => BidiDirection.RightToLeft,
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

            int[] actualOrder = paragraph.VisualOrder().Where(unitToCodePoint.ContainsKey).Select(u => unitToCodePoint[u]).ToArray();
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

    private static string FormatLevel(byte level) =>
        level == BidiParagraph.Removed ? "x" : level.ToString(CultureInfo.InvariantCulture);

    private static IEnumerable<string> ReadCorpus(string fileName)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Rendering", "Text", "Ucd", fileName);
        using var file = File.OpenRead(path);
        using var gzip = new GZipStream(file, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip, Encoding.UTF8);
        while (reader.ReadLine() is { } line)
        {
            yield return line;
        }
    }
}
