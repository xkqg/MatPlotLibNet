// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>The scan an <see cref="OwnedRule"/> runs over source files, and the probes it carries: the lines it must find and the lines
/// it must pass, each read in the file the rule names for them.</summary>
public class OwnedRuleTests
{
    private static readonly OwnedRule Pad = new("a pad", @"\b1\.08\b", "Src/A/Owner.cs");

    [Fact]
    public void AFileThatOwnsTheRule_MayHoldIt()
    {
        var findings = Pad.Violations([new("Src/A/Owner.cs", "const double P = 1.08;")]);

        Assert.Empty(findings);
    }

    [Fact]
    public void AFileOutsideTheOwners_ThatSpellsTheRule_IsNamedWithItsLine()
    {
        var findings = Pad.Violations(
        [
            new("Src/A/Owner.cs", "const double P = 1.08;"),
            new("Src/B/Other.cs", "int a = 1;\nint b = 2;\nvar x = size * 1.08;"),
        ]);

        var finding = Assert.Single(findings);
        Assert.Contains("Src/B/Other.cs:3", finding, StringComparison.Ordinal);
        Assert.Contains("var x = size * 1.08;", finding, StringComparison.Ordinal);
    }

    [Fact]
    public void EveryLineOutsideTheOwners_IsNamed()
    {
        var findings = Pad.Violations(
        [
            new("Src/A/Owner.cs", "1.08"),
            new("Src/B/Other.cs", "1.08\n1.08"),
            new("Src/C/Third.cs", "1.08"),
        ]);

        Assert.Equal(3, findings.Count);
    }

    [Fact]
    public void AnOwnerThatNoLongerSpellsTheRule_IsNamedAsStale()
    {
        var findings = Pad.Violations([new("Src/A/Owner.cs", "const double P = 1.2;")]);

        var finding = Assert.Single(findings);
        Assert.Contains("Src/A/Owner.cs", finding, StringComparison.Ordinal);
        Assert.Contains("no longer", finding, StringComparison.Ordinal);
    }

    [Fact]
    public void AnOwnerMissingFromTheTree_IsNamedAsStale()
    {
        var findings = Pad.Violations([new("Src/B/Other.cs", "int a = 1;")]);

        var finding = Assert.Single(findings);
        Assert.Contains("Src/A/Owner.cs", finding, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("// scale by 1.08")]
    [InlineData("    /// <summary>1.08 times the font size</summary>")]
    public void AMentionInAComment_IsNotASpelling(string line)
    {
        var findings = Pad.Violations([new("Src/A/Owner.cs", "double p = 1.08;"), new("Src/B/Other.cs", line)]);

        Assert.Empty(findings);
    }

    [Fact]
    public void ACommentInTheOwner_DoesNotKeepTheOwnerAlive()
    {
        var findings = Pad.Violations([new("Src/A/Owner.cs", "// 1.08 was here")]);

        Assert.Single(findings);
    }

    [Fact]
    public void ARule_NoFileMaySpell_IsReadOnlyUnderItsFolder()
    {
        var scoped = OwnedRule.Nowhere("scoped", "Foo", under: "Src/A/");

        Assert.Empty(scoped.Violations([new("Src/B/Other.cs", "Foo")]));
        Assert.Single(scoped.Violations([new("Src/A/Other.cs", "Foo")]));
    }

    [Fact]
    public void ARule_NoFileMaySpell_IsCleanWhenNoFileDoes()
    {
        var banned = OwnedRule.Nowhere("a pad", @"\b1\.08\b");

        Assert.Empty(banned.Violations([new("Src/A/One.cs", "var p = 1.2;"), new("Src/B/Two.cs", "// 1.08 in a comment")]));
    }

    [Fact]
    public void TwoOwners_MayBothSpellTheRule()
    {
        var shared = new OwnedRule("two", "Foo", "Src/A/One.cs", "Src/A/Two.cs");

        var findings = shared.Violations([new("Src/A/One.cs", "Foo"), new("Src/A/Two.cs", "Foo")]);

        Assert.Empty(findings);
    }

    [Fact]
    public void ARuleOfTheDocumentation_ReadsTheDocumentationAndNoCode()
    {
        var docs = OwnedRule.InDocumentation("a pad", @"\b1\.08\b", "Src/A/");

        Assert.Single(docs.Violations([new("Src/A/One.cs", "    /// <summary>The pad is 1.08.</summary>")]));
        Assert.Empty(docs.Violations([new("Src/A/One.cs", "    var pad = 1.08;"), new("Src/A/Two.cs", "    // the pad is 1.08")]));
    }

    [Fact]
    public void ARuleOfTheComments_ReadsTheCommentOfALineWhereverItStands_AndNoCode()
    {
        var comments = OwnedRule.InComments("a pad", @"\b1\.08\b", "Src/A/");

        Assert.Single(comments.Violations([new("Src/A/One.cs", "    // the pad is 1.08")]));
        Assert.Single(comments.Violations([new("Src/A/One.cs", "    /// <summary>The pad is 1.08.</summary>")]));
        Assert.Single(comments.Violations([new("Src/A/One.cs", "    var x = 1; // the pad is 1.08")]));
        Assert.Empty(comments.Violations([new("Src/A/One.cs", "    var pad = 1.08;")]));
        Assert.Empty(comments.Violations([new("Src/A/One.cs", "    var url = \"http://host/1.08\";")]));
    }

    [Fact]
    public void ARuleOfTheComments_StartsItsPatternAtTheCommentMark()
    {
        var comments = OwnedRule.InComments("a pad", @"^//\s*pad 1\.08", "Src/A/");

        Assert.Single(comments.Violations([new("Src/A/One.cs", "    var x = 1; // pad 1.08")]));
        Assert.Empty(comments.Violations([new("Src/A/One.cs", "    var x = 1; // size 2 // pad 1.08")]));
    }

    [Fact]
    public void ARuleOfTheCode_ReadsNoDocumentation()
    {
        var code = OwnedRule.Nowhere("a pad", @"\b1\.08\b", "Src/A/");

        Assert.Empty(code.Violations([new("Src/A/One.cs", "    /// <summary>The pad is 1.08.</summary>")]));
    }

    [Fact]
    public void ARule_WithNoOwner_IsRefused()
    {
        Assert.Throws<ArgumentException>(() => new OwnedRule("none", "x"));
    }

    [Fact]
    public void ARule_WithAnEmptyPattern_IsRefused()
    {
        Assert.Throws<ArgumentException>(() => new OwnedRule("empty", "", "Src/A/Owner.cs"));
    }

    [Theory]
    [InlineData("Src/A/", "Src/A/Any.cs")]
    [InlineData("Src/MatPlotLibNet", "Src/MatPlotLibNet/Any.cs")]
    [InlineData("Src/A/Range1D.cs", "Src/A/Range1D.cs")]
    [InlineData("", "Src/Any.cs")]
    [InlineData("Tst/", "Tst/Any.cs")]
    public void AProbe_IsReadInAFileOfTheFolderTheRuleReads_OrInTheOneFileItReads(string under, string probeFile) =>
        Assert.Equal(probeFile, OwnedRule.Nowhere("a pad", @"\b1\.08\b", under).ProbeFile);

    [Fact]
    public void ARuleThatFindsWhatItMustFind_AndPassesWhatItMustPass_MisreadsNoProbe()
    {
        var rule = new OwnedRule("a pad", @"\b1\.08\b", "Src/A/Owner.cs") { Under = "Src/A/", Finds = ["var x = size * 1.08;"], Ignores = ["var x = size * 1.2;"] };

        Assert.Empty(rule.MisreadProbes());
    }

    /// <summary>A line of the lines the rule must find that it does not find, and a line of the lines it must pass that it finds, is named.</summary>
    [Theory]
    [InlineData(true, "var x = size * 1.2;")]
    [InlineData(false, "var x = size * 1.08;")]
    public void ALineOfFindsTheRuleDoesNotFind_OrOfIgnoresTheRuleFinds_IsNamed(bool finds, string named)
    {
        string[] lines = finds ? ["var x = size * 1.08;", "var x = size * 1.2;"] : ["var x = size * 1.2;", "var x = size * 1.08;"];
        var rule = finds
            ? new OwnedRule("a pad", @"\b1\.08\b", "Src/A/Owner.cs") { Finds = lines }
            : new OwnedRule("a pad", @"\b1\.08\b", "Src/A/Owner.cs") { Ignores = lines };

        Assert.Contains(named, Assert.Single(rule.MisreadProbes()), StringComparison.Ordinal);
    }

    /// <summary>A probe is read in the rule's probe file: a rule that reads one file finds its probe there, and a rule read across lines
    /// finds a probe that runs over a line break.</summary>
    [Fact]
    public void AProbe_IsReadInTheFileTheRuleReads_AndAcrossLinesWhereTheRuleIs()
    {
        var oneFile = OwnedRule.Nowhere("a pad", @"\b1\.08\b", "Src/A/Range1D.cs") with { Finds = ["var x = size * 1.08;"] };
        var acrossLines = new OwnedRule("a made shape", @"\bShapeStyle\s+\w+\s*=>\s*new\s*\(", "Src/A/ShapeStyle.cs") { AcrossLines = true, Finds = ["internal ShapeStyle Fill =>\n    new(colour, null, 0);"] };

        Assert.Empty(oneFile.MisreadProbes().Concat(acrossLines.MisreadProbes()));
    }
}
