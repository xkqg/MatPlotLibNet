// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Packaging;

public class SourceFileTests
{
    private const string Text = "int a;\r\n    // note\r\n/// doc\nint b; // tail\n";

    [Fact]
    public void TheLines_AreTheTextCutAtEveryLineEnd_WithNoCarriageReturn()
    {
        var file = new SourceFile("Src/A.cs", Text);

        Assert.Equal(["int a;", "    // note", "/// doc", "int b; // tail", ""], file.Lines);
    }

    [Fact]
    public void ALineStartsWhereTheTextCountsIt_CarriageReturnsIncluded()
    {
        var file = new SourceFile("Src/A.cs", Text);

        Assert.Equal([0, 8, 21, 29, 44], Enumerable.Range(0, file.Lines.Count).Select(file.StartOf));
        Assert.All(Enumerable.Range(0, 4), i => Assert.StartsWith(file.Lines[i], file.Text[file.StartOf(i)..], StringComparison.Ordinal));
    }

    [Fact]
    public void TheCode_IsTheLinesWithEveryCommentLineEmptied_SoALineKeepsItsNumber()
    {
        var file = new SourceFile("Src/A.cs", Text);

        Assert.Equal(["int a;", "", "", "int b; // tail", ""], file.CodeLines);
        Assert.Equal("int a;\n\n\nint b; // tail\n", file.Code);
    }

    [Theory]
    [InlineData("// note", true, false)]
    [InlineData("    // note", true, false)]
    [InlineData("\t/// <summary>doc</summary>", true, true)]
    [InlineData("int a; // tail", false, false)]
    [InlineData("var url = \"http://host\";", false, false)]
    [InlineData("", false, false)]
    public void ALine_IsACommentOrDocumentationLine_OnlyWhenItsFirstCharactersSayIt(string line, bool comment, bool documentation)
    {
        var file = new SourceFile("Src/A.cs", line);

        Assert.Equal(comment, SourceFile.IsComment(line));
        Assert.Equal(documentation, SourceFile.IsDocumentation(line));
        Assert.Equal(comment, file.IsComment(0));
        Assert.Equal(documentation, file.IsDocumentation(0));
    }

    [Theory]
    [InlineData("// note", "// note")]
    [InlineData("int b; // tail", "// tail")]
    [InlineData("var url = \"http://host\"; // see", "// see")]
    [InlineData("var url = \"http://host\";", null)]
    [InlineData("int c;", null)]
    public void TheCommentOfALine_RunsFromItsMarkToTheEnd_AndAnAddressIsNoMark(string line, string? comment) =>
        Assert.Equal(comment, SourceFile.CommentOf(line));

    [Fact]
    public void ATextThatIsNoFile_IsCutAtItsLineEnds_ByTheMemberThatNamesItsKind()
    {
        string csv = "a,\"b\nc\"\r\n1,2\r\n";

        Assert.Equal(["a", "", "b", ""], SourceFile.LinesOf("a\r\n\r\nb\n"));
        Assert.Equal(["<svg>", "<g>", "<rect />"], SourceFile.ElementLinesOf("<svg>\r\n  <g>\n    <rect />  \n\n \t\n"));
        Assert.Equal(["a,\"b\nc\"", "1,2", ""], SourceFile.CrlfLinesOf(csv));
        Assert.Equal(["a,\"b\nc\"", "1,2"], SourceFile.CsvRecordsOf(csv));
        Assert.Equal("a\nb\n", SourceFile.LineFeedTextOf("a\r\nb\r\n"));
    }

    [Fact]
    public void AnOffsetInATextIsPlacedOnItsLine_ByTheOwnerOfTheLineEnd()
    {
        string text = "ab\r\ncd\nef";

        Assert.Equal(["ab", "cd", "ef"], [SourceFile.LineAt(text, 0), SourceFile.LineAt(text, 5), SourceFile.LineAt(text, 7)]);
        Assert.Equal([3, 6, 9], [SourceFile.LineEndAt(text, 0), SourceFile.LineEndAt(text, 5), SourceFile.LineEndAt(text, 7)]);
        Assert.Equal([0, 1, 2], [SourceFile.LineIndexAt(text, 1), SourceFile.LineIndexAt(text, 5), SourceFile.LineIndexAt(text, 8)]);
    }

    [Fact]
    public void AFileOutsideTheTree_IsReadFromItsPath_WhateverLineEndItHolds()
    {
        string path = System.IO.Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "a\r\nb\r\n");

            var file = SourceFile.ReadFrom(path);

            Assert.Equal(path, file.Path);
            Assert.Equal(["a", "b", ""], file.Lines);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void TheLineFeedText_IsTheLinesJoinedWithALineFeed_WhatEveryLineEndWasWritten() =>
        Assert.Equal("a\n\nb\n", new SourceFile("docs/a.md", "a\r\n\nb\r\n").LineFeedText);

    [Fact]
    public void AScript_TellsItsCommentLinesByTheHash_AndAFileOfCSharpByTheSlashes()
    {
        var script = new SourceFile("tools/prepush.ps1", "Copy-Item a\n  # note\n#!shebang\nRemove-Item b # tail\n// not a comment here\n");
        var source = new SourceFile("Src/A.cs", "#nullable enable\n# not a comment here\n// note\n");

        Assert.Equal(["Copy-Item a", "", "", "Remove-Item b # tail", "// not a comment here", ""], script.CodeLines);
        Assert.Equal(["#nullable enable", "# not a comment here", "", ""], source.CodeLines);
        Assert.False(script.IsDocumentation(1));
    }

    [Fact]
    public void TheFrontMatterAndTheFencedBlocksOfAMarkdownDocument_AreAskedOfTheFile_WhateverLineEndWasWritten()
    {
        const string page = "---\r\ntitle: A\r\n---\r\n\r\nafter\r\n```json\r\n{\"a\": 1}\r\n```\r\n  ```json\r\n[]\r\n```\r\n```csharp\r\nvar x;\r\n```\r\n";

        Assert.Equal(["title: A"], SourceFile.FrontMatterOf(page));
        Assert.Empty(SourceFile.FrontMatterOf("no header\n---\n"));
        Assert.Empty(SourceFile.FrontMatterOf("---\nnever closed\n"));
        Assert.Equal(["{\"a\": 1}", "[]"], SourceFile.FencedBlocksOf(page, "json"));
    }

    [Fact]
    public void TheChildrenOfAYamlKey_AreTheBlocksOneLevelBelowIt_ACommentAndABlankLineStayingWithTheBlockBeforeThem()
    {
        const string yaml = "name: x\r\njobs:\r\n  build:\r\n    steps:\r\n      - name: one\r\n        run: a\r\n\r\n      # note\r\n      - name: two\r\n  test:\r\n# margin\r\n    runs-on: x\r\nafter: 1\r\n";
        string build = "  build:\n    steps:\n      - name: one\n        run: a\n\n      # note\n      - name: two";

        Assert.Equal([build, "  test:\n# margin\n    runs-on: x"], SourceFile.YamlChildrenOf(yaml, "jobs"));
        Assert.Equal(["      - name: one\n        run: a\n\n      # note", "      - name: two"], SourceFile.YamlChildrenOf(build, "steps"));
        Assert.Throws<Xunit.Sdk.TrueException>(() => SourceFile.YamlChildrenOf(yaml, "services"));
    }

    [Fact]
    public void ASectionOfAMarkdownDocument_RunsFromItsHeadingLineToTheNextHeadingOfItsLevel_WhateverLineEndWasWritten()
    {
        const string page = "intro\r\n\r\n## One\r\nfirst\r\n### Sub\r\ndeep\r\n## Two\r\nsecond\r\n## Three\r\nlast";

        Assert.Equal("## One\nfirst\n### Sub\ndeep", SourceFile.SectionOf(page, "One"));
        Assert.Equal("## Three\nlast", SourceFile.SectionOf(page, "Three"));
        Assert.Equal("intro\n", SourceFile.IntroductionOf(page));
        Assert.Equal(["One", "Two", "Three"], SourceFile.SectionsOf(page).Select(section => section.Heading));
        Assert.Equal("## Two\nsecond", SourceFile.SectionsOf(page)[1].Text);
        Assert.Throws<Xunit.Sdk.TrueException>(() => SourceFile.SectionOf(page, "Four"));
        Assert.Empty(SourceFile.SectionsOf("no heading at all\n### only a sub"));
        Assert.Equal("only text", SourceFile.IntroductionOf("only text"));
    }

    [Fact]
    public void TheHeadingsOfAMarkdownDocument_AreEveryHeadingOutsideAFence_InTheOrderTheyStand()
    {
        const string page = "intro\r\n# Title\r\n## One\r\n```\r\n# a comment in code\r\n```\r\n#hashtag, no heading\r\n###### Six  \r\n## Two `x`";

        Assert.Equal(["Title", "One", "Six", "Two `x`"], SourceFile.HeadingsOf(page));
        Assert.Empty(SourceFile.HeadingsOf("no heading"));
        Assert.Equal(["real"], SourceFile.HeadingsOf("````\n```\n# inner\n```\n# still the outer fence\n````\n# real"));
    }

    [Fact]
    public void TheFencesOfAMarkdownDocument_AreReadOnce_ALongerFenceHoldsTheShorterOnes_AndAFenceWithWordsNeverCloses()
    {
        const string page = "````markdown\r\n```csharp\r\nvar a;\r\n```\r\n````\r\ntext\r\n```csharp\r\nvar b;\r\n```csharp\r\nvar c;\r\n```\r\n  ```\r\nopen";

        var blocks = SourceFile.FenceBlocksOf(page);

        Assert.Equal([0, 6, 8, 11], blocks.Select(block => block.OpenAt));
        Assert.Equal(["markdown", "csharp", "csharp", ""], blocks.Select(block => block.Open.Info));
        Assert.Equal([true, false, true, false], blocks.Select(block => block.Closed));
        Assert.Equal(["```csharp", "var a;", "```"], blocks[0].Body);
        Assert.Equal(["var b;"], blocks[1].Body);
        Assert.Equal(8, blocks[1].EndAt);
        Assert.Equal(["open"], blocks[3].Body);
        Assert.Null(blocks[3].Met);
        Assert.Equal(["var c;"], SourceFile.FencedBlocksOf(page, "csharp"));
        Assert.Empty(SourceFile.FenceBlocksOf("no fence at all ``` in a sentence"));
        Assert.Null(MarkdownFence.Of("``` `inline` ```"));
    }

    [Fact]
    public void TheLinesOfAMarkdownDocument_SayWhichFencedBlockHoldsThem_AndLeaveTheFenceLinesOut()
    {
        const string page = "prose\n```\ncode\n```\n```\nmore\n```\nlast";

        var lines = SourceFile.MarkdownLinesOf(page);

        Assert.Equal([1, 3, 6, 8], lines.Select(line => line.Number));
        Assert.Equal(["prose", "code", "more", "last"], lines.Select(line => line.Text));
        Assert.Equal([-1, 0, 1, -1], lines.Select(line => line.Block));
        Assert.Equal([false, true, true, false], lines.Select(line => line.InFence));
    }

    [Fact]
    public void TheHeadingOfAMarkdownLine_IsWhatItSaysAfterItsHashes_AndNullInsideAFenceOrWithoutASpace()
    {
        var lines = SourceFile.MarkdownLinesOf("# Title\n```\n# comment\n```\n#hashtag\n### Three `x`  ");

        Assert.Equal(["Title", null, null, "Three `x`"], lines.Select(line => line.Heading));
        Assert.Equal([1, 0, 0, 3], lines.Select(line => line.HeadingLevel));
    }

    [Fact]
    public void ABulletOfAMarkdownList_IsItsLineAndTheLinesThatContinueIt_JoinedWithASpace()
    {
        const string section = "### Removed\r\nintro\r\n- `A` -> `B`.\r\n- `C` ->\r\n  `D`,\r\n  and more.\r\n- `E`";

        Assert.Equal(["- `A` -> `B`.", "- `C` -> `D`, and more.", "- `E`"], SourceFile.BulletsOf(section));
        Assert.Empty(SourceFile.BulletsOf("no list"));
    }

    [Fact]
    public void ASubsectionOfASection_RunsFromItsHeadingLineToTheNextHeadingOfItsLevel_WhateverLineEndWasWritten()
    {
        const string section = "## One\r\nfirst\r\n### Added\r\n- a\r\n### Removed\r\n- b\r\n- c\r\n### Fixed\r\n- d";

        Assert.Equal("### Removed\n- b\n- c", SourceFile.SubsectionOf(section, "Removed"));
        Assert.Equal("### Fixed\n- d", SourceFile.SubsectionOf(section, "Fixed"));
        Assert.Throws<Xunit.Sdk.TrueException>(() => SourceFile.SubsectionOf(section, "Changed"));
    }

    [Fact]
    public void TheCellsOfATableRow_AreTheTextBetweenItsBars_TrimmedAndWithABarInACodeSpanOrAfterABackslashKept()
    {
        Assert.Equal(["A", "`a|b`", "c \\| d", ""], SourceFile.TableCellsOf("| A | `a|b` | c \\| d | |"));
        Assert.Equal(["---", "---"], SourceFile.TableCellsOf("|---|---|"));
        Assert.Equal(["only"], SourceFile.TableCellsOf("  | only |  "));
        Assert.Empty(SourceFile.TableCellsOf("no bars"));
        Assert.Empty(SourceFile.TableCellsOf("| not closed"));
        Assert.Empty(SourceFile.TableCellsOf("a | b | c |"));
    }

    [Fact]
    public void TheCodeSpansOfALine_AreTheTextBetweenBackticks_EachWithWhereItStands()
    {
        const string line = "A `Plt.Create()` and `Foo`, an open `tick.";

        var spans = SourceFile.CodeSpansOf(line);

        Assert.Equal(["Plt.Create()", "Foo"], spans.Select(span => span.Code));
        Assert.All(spans, span => Assert.Equal($"`{span.Code}`", line.Substring(span.Index, span.Length)));
        Assert.Empty(SourceFile.CodeSpansOf("no code here"));
        Assert.True(SourceFile.OpensCodeSpan(line));
        Assert.False(SourceFile.OpensCodeSpan("A `Plt.Create()` and `Foo`."));
    }

    [Fact]
    public void ALineWithoutItsCodeSpans_AndTheSoleCodeOfACell()
    {
        Assert.Equal("A  and .", SourceFile.WithoutCodeSpans("A `x` and `y`."));
        Assert.Equal("Linear(scale, offset = 0)", SourceFile.SoleCodeOf(" `Linear(scale, offset = 0)` "));
        Assert.Null(SourceFile.SoleCodeOf("`A` and `B`"));
        Assert.Null(SourceFile.SoleCodeOf("`A` text"));
        Assert.Null(SourceFile.SoleCodeOf("plain"));
    }
}
