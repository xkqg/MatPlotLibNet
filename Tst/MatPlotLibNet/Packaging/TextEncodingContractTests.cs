// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>A file holds its text as it was written: no character of it is the pair or the triple of characters that stand for it when
/// its UTF-8 bytes were read as Windows-1252 and saved again (a plus-minus sign that shows as a capital A with a circumflex and a
/// plus-minus sign, a box-drawing line that shows as three letters and marks). Every source file, razor component and document of the
/// repository is read, whichever folder it is in.</summary>
public class TextEncodingContractTests
{
    /// <summary>What a byte from 0x80 to 0xBF reads as in Windows-1252: the Latin-1 characters from U+0080 to U+00BF and the
    /// punctuation and letters Windows-1252 puts in the places Latin-1 keeps for controls.</summary>
    private const string Continuation = @"[\u0080-\u00BF\u0152\u0153\u0160\u0161\u0178\u017D\u017E\u0192\u02C6\u02DC\u2013\u2014\u2018-\u201A\u201C-\u201E\u2020-\u2022\u2026\u2030\u2039\u203A\u20AC\u2122]";

    /// <summary>The lead byte of a two-byte (0xC2 to 0xDF) or a three-byte (0xE0 to 0xEF) UTF-8 sequence, then the one or two bytes that
    /// continue it, each as Windows-1252 shows it.</summary>
    private static readonly Regex Misread = new(@"[\u00C2-\u00DF]" + Continuation + @"|[\u00E0-\u00EF]" + Continuation + "{2}", RegexOptions.CultureInvariant);

    [Fact]
    public void NoFileOfTheRepository_HoldsTextThatWasReadAsAnotherEncodingAndSavedAgain()
    {
        var findings = new List<string>();
        foreach (var file in RepositoryTree.Files("", "*.cs").Concat(RepositoryTree.Files("", "*.razor")).Concat(RepositoryTree.Documents()))
        {
            for (int i = 0; i < file.Lines.Count; i++)
            {
                if (Misread.IsMatch(file.Lines[i]))
                {
                    findings.Add($"{file.Path}:{i + 1}: {file.Lines[i].Trim()}");
                }
            }
        }

        Assert.True(findings.Count == 0, $"{findings.Count} lines hold text that was read in the wrong encoding and saved again:\n" + string.Join('\n', findings));
    }

    /// <summary>The first five rows are a character written as its UTF-8 bytes read in Windows-1252 (a plus-minus sign, a multiplication
    /// sign, a Greek letter, an em dash and a box-drawing line: two bytes, or three with a punctuation mark in the middle); the last five
    /// are what is written right: the characters themselves, two capital letters with an umlaut side by side, a lead letter followed by
    /// ASCII, and a lead letter followed by a letter that is no continuation byte.</summary>
    [Theory]
    [InlineData("a width of 5 \u00C2\u00B1 3", true)]
    [InlineData("cells of cols \u00C3\u2014 rows", true)]
    [InlineData("the spread \u00CF\u0192", true)]
    [InlineData("an index \u00E2\u20AC\u201D a slot", true)]
    [InlineData("// \u00E2\u201D\u20AC\u00E2\u201D\u20AC a banner", true)]
    [InlineData("a width of 5 \u00B1 3", false)]
    [InlineData("cells of cols \u00D7 rows", false)]
    [InlineData("\u2500\u2500 a banner \u2500\u2500", false)]
    [InlineData("\u00C4\u00D6\u00DC and \u00E9\u00E8", false)]
    [InlineData("\u00C3 and \u00C2 alone, \u00C3bc", false)]
    public void OnlyTextReadInWindows1252AndSavedAgain_IsFound(string line, bool misread) => Assert.Equal(misread, Misread.IsMatch(line));
}
