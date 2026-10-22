// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.RegularExpressions;

namespace MatPlotLibNet.Wiki.Tests;

/// <summary>
/// The wiki's code is what a person copies after the README's, so every C# block on every page is compiled against the
/// packages as built here, with the usings a console program has without asking, and fails on a warning as a build that
/// treats warnings as errors does: an obsolete member included. A block is a program of its own unless the line above it
/// says otherwise, in a comment the page does not show: <c>&lt;!-- example: name --&gt;</c> names a block others go on
/// from, <c>&lt;!-- continues: name --&gt;</c> is compiled after that block and every block that went on from it,
/// <c>&lt;!-- a C# cell of a notebook --&gt;</c> is a notebook's cell (a <c>#r "nuget:"</c> line is no C#), and
/// <c>&lt;!-- needs: Wpf --&gt;</c> (or Uno, or Maui) is a block for a head the ubuntu runner cannot build, and
/// <c>&lt;!-- needs: Tests --&gt;</c> a block that belongs to the repository's own test project, whose types this suite
/// cannot reference. The last two kinds are counted, not compiled, and the count of the blocks that need another head is
/// held under a ceiling. Any other comment is refused.
/// </summary>
public sealed class WikiExampleTests
{
    /// <summary>
    /// How many blocks may be passed over because they need a Windows or workload head, or the repository's own test
    /// project. Measured, not chosen: a page that
    /// adds such a block raises this number in the same change, where a reviewer sees it.
    /// </summary>
    private const int MostBlocksForAnotherHead = 1;

    /// <summary>How many blocks the wiki compiles at least: a wiki read from the wrong place, or read by a scanner that
    /// swallows blocks, holds far fewer.</summary>
    private const int FewestPrograms = 219;

    [Fact]
    public void EveryCSharpBlockOfTheWiki_CompilesAgainstThePackagesAsBuilt_WithoutAWarning()
    {
        var pages = Wiki.BesideTheRepository.Pages();
        var faults = new List<string>();
        var summary = new StringBuilder();
        var byId = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var page in pages)
        {
            var failing = 0;

            faults.AddRange(page.Faults);

            foreach (var program in page.Programs)
            {
                var diagnostics = program.Compiled();

                if (diagnostics.Count > 0)
                {
                    failing++;
                }

                foreach (var diagnostic in diagnostics)
                {
                    byId[diagnostic.Id] = byId.GetValueOrDefault(diagnostic.Id) + 1;
                    faults.Add($"{page.Name}, the block at line {program.Line}: {diagnostic}");
                }
            }

            if (page.BlockCount > 0)
            {
                summary.Append($"{page.Name}: {page.Programs.Count} compiled, {page.Programs.Count - failing} clean, {failing} failing, {page.Cells.Count} cells, {page.Needing.Count} for another head, {page.Faults.Count} fence faults\n");
            }
        }

        Assert.True(
            faults.Count == 0,
            summary
            + "By diagnostic: " + string.Join(", ", byId.OrderByDescending(each => each.Value).Select(each => $"{each.Key} {each.Value}")) + "\n"
            + string.Join('\n', faults));
    }

    [Fact]
    public void EveryCSharpFenceOfAPage_IsReadAsExactlyOneBlock()
    {
        // Counted apart from the scanner, line by line: a scanner that ends a block at the wrong fence swallows the
        // block after it, and a page then shows code that no test ever compiled.
        var wiki = Wiki.BesideTheRepository;
        var swallowed = wiki.Pages()
            .Select(page => (page.Name, Fences: Regex.Count(wiki.Text(page.Name), @"^\s*```csharp\s*$", RegexOptions.Multiline), Read: page.BlockCount))
            .Where(each => each.Fences != each.Read)
            .Select(each => $"{each.Name}: {each.Fences} csharp fences, {each.Read} blocks read")
            .ToArray();

        Assert.True(swallowed.Length == 0, string.Join('\n', swallowed));
    }

    [Fact]
    public void TheWiki_HoldsAtLeastTheProgramsItIsKnownToHold()
    {
        var programs = Wiki.BesideTheRepository.Pages().Sum(page => page.Programs.Count);

        Assert.True(
            programs >= FewestPrograms,
            $"The wiki's pages hold {programs} blocks that compile as programs, fewer than {FewestPrograms}: the wiki this reads is not the one the pages are.");
    }

    [Fact]
    public void TheBlocksPassedOverForAnotherHead_StayUnderTheirCeiling()
    {
        var needing = Wiki.BesideTheRepository.Pages()
            .SelectMany(page => page.Needing.Select(block => $"{page.Name}, line {block.Line}"))
            .ToArray();

        Assert.True(
            needing.Length <= MostBlocksForAnotherHead,
            $"{needing.Length} blocks are passed over for another head, more than the {MostBlocksForAnotherHead} measured: " + string.Join("; ", needing));
    }

    [Fact]
    public void ABlockCallingAVerbTheLibraryHasNot_FailsToCompile()
    {
        var diagnostics = Only("using MatPlotLibNet;\n\nPlt.Create().NoSuchVerb();\n").Compiled();

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "CS1061");
    }

    [Fact]
    public void ABlockCallingAnObsoleteMember_FailsAsAWarning()
    {
        var diagnostics = Only("Old.Call();\n\nstatic class Old\n{\n    [System.Obsolete(\"gone\")]\n    public static void Call() { }\n}\n").Compiled();

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "CS0618");
    }

    [Fact]
    public void ABlockUsingTheCoreAndEverySatelliteCiBuilds_CompilesClean()
    {
        // The usings prove every satellite is a reference of the compilation: a namespace the compiler cannot find is
        // CS0246, an error. The calls prove a block is a console program with the SDK's implicit usings.
        var diagnostics = Only("""
            using MatPlotLibNet;
            using MatPlotLibNet.AspNetCore;
            using MatPlotLibNet.Avalonia;
            using MatPlotLibNet.Blazor;
            using MatPlotLibNet.DataFrame;
            using MatPlotLibNet.Geo;
            using MatPlotLibNet.GraphQL;
            using MatPlotLibNet.Interactive;
            using MatPlotLibNet.Mcp;
            using MatPlotLibNet.Skia;
            using MatPlotLibNet.Verso;

            var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();
            byte[] png = figure.ToPng();
            Console.WriteLine(figure.ToSvg().Length + png.Length);

            """).Compiled();

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void ACommentTheSuiteDoesNotKnow_IsRefused()
    {
        var refused = Assert.Throws<InvalidOperationException>(() => WikiPage.Read("Page.md", "<!-- skip -->\n```csharp\nvar a = 1;\n```\n"));

        Assert.Contains("Page.md, line 3", refused.Message, StringComparison.Ordinal);
        Assert.Contains("does not know", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ABlockContinuingAnExampleNobodyNamed_IsRefused()
    {
        var refused = Assert.Throws<InvalidOperationException>(() => WikiPage.Read("Page.md", "<!-- continues: none -->\n```csharp\nvar a = 1;\n```\n"));

        Assert.Contains("no example", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AnExampleNamedTwice_IsRefused()
    {
        var refused = Assert.Throws<InvalidOperationException>(() => WikiPage.Read(
            "Page.md",
            "<!-- example: one -->\n```csharp\nvar a = 1;\n```\n<!-- example: one -->\n```csharp\nvar b = 1;\n```\n"));

        Assert.Contains("twice", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AMissingWiki_FailsWithTheCommandThatClonesIt()
    {
        var absent = new Wiki(Path.Join(Path.GetTempPath(), "no-wiki-" + Guid.NewGuid().ToString("N")));

        var refused = Assert.Throws<DirectoryNotFoundException>(() => absent.Pages());

        Assert.Contains(Wiki.CloneCommand, refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TheKnownMarkers_ReadAsExamples_Cells_AndBlocksForAnotherHead()
    {
        var page = WikiPage.Read(
            "Page.md",
            "<!-- example: one -->\n```csharp\nvar a = 1;\n```\n\n<!-- continues: one -->\n```csharp\nvar b = a;\n```\n\n"
            + "<!-- a C# cell of a notebook -->\n```csharp\n#r \"nuget: X\"\n```\n\n<!-- needs: Wpf -->\n```csharp\nvar w = 1;\n```\n\n"
            + "<!-- needs: Tests -->\n```csharp\nvar t = 1;\n```\n\n```csharp\nvar c = 1;\n```\n");

        Assert.Equal([1, 2, 1], page.Programs.Select(program => program.Blocks.Count));
        Assert.Equal([3, 8, 27], page.Programs.Select(program => program.Line));
        Assert.Single(page.Cells);
        Assert.Equal([18, 23], page.Needing.Select(block => block.Line));
        Assert.Empty(page.Faults);
        Assert.Equal(6, page.BlockCount);
    }

    [Fact]
    public void AFenceNeverClosed_IsAFaultNamingPageAndLine_AndTheBlockAfterItIsStillRead()
    {
        // The wiki's Styling page had this: a block opened and never closed, so a scanner that ends a block at the next
        // fence took the prose after it for code and lost the block that followed.
        var page = WikiPage.Read("Page.md", "```csharp\nvar a = 1;\n\nSome prose.\n```csharp\nvar b = 2;\n```\n");

        Assert.Contains("Page.md, line 1", Assert.Single(page.Faults), StringComparison.Ordinal);
        Assert.Equal([2, 6], page.Programs.Select(program => program.Line));
        Assert.Equal("var b = 2;\n", page.Programs[1].Blocks[0].Code);
    }

    [Fact]
    public void AFenceOpenWhenThePageEnds_IsAFault()
    {
        var page = WikiPage.Read("Page.md", "text\n```csharp\nvar a = 1;\n");

        Assert.Contains("Page.md, line 2", Assert.Single(page.Faults), StringComparison.Ordinal);
        Assert.Single(page.Programs);
    }

    [Fact]
    public void ACSharpFenceInsideALongerFence_IsTextOfThatFence()
    {
        // A page may show markdown that holds a fence of its own: the inner fence is text, never a block.
        var page = WikiPage.Read("Page.md", "````markdown\n```csharp\nvar a = 1;\n```\n````\n");

        Assert.Equal(0, page.BlockCount);
        Assert.Empty(page.Faults);
    }

    private static WikiProgram Only(string code) => Assert.Single(WikiPage.Read("Live.md", "```csharp\n" + code + "```\n").Programs);
}
