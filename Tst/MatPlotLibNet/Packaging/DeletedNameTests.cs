// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>A name a step deleted (<see cref="DeletedName"/>) is named nowhere in the repository: not in the library's code, not in a
/// test, not in a comment or XML documentation of either, not in a document, not in a sample, a benchmark, a page, a component or a
/// script. The wiki's pages are read by the wiki suite.</summary>
public class DeletedNameTests
{
    /// <summary>The two files that name the deleted names on purpose: the list itself and the cases that prove the scan finds them.</summary>
    private static readonly string[] TheScansOwnFiles =
    [
        "Tst/MatPlotLibNet/Packaging/DeletedName.cs",
        "Tst/MatPlotLibNet/Packaging/DeletedNameTests.cs",
    ];

    /// <summary>The kinds of file a person writes, by name: code (of the library, the tests, the samples and the benchmarks), a
    /// component, a page, a window, the script of a binding and the scripts of the tools. What a tool writes is left out, since it
    /// records the names of its day and nobody words it: the suppressions of the package validation, the coverage baseline, the
    /// pinned surface, the fixtures and the pictures.</summary>
    private static readonly string[] WrittenByHand =
        ["*.cs", "*.razor", "*.html", "*.xaml", "*.axaml", "*.ts", "*.tsx", "*.js", "*.py", "*.ps1", "*.sh"];

    /// <summary>Every file the scan reads: what a person writes anywhere in the repository (<see cref="WrittenByHand"/>: the library's
    /// code, the tests of every test project, the samples, the benchmarks, the bindings and the tools), its documents and its build files.</summary>
    private static IEnumerable<SourceFile> ScannedFiles() =>
        RepositoryTree.SourceFiles()
            .Concat(RepositoryTree.TestFiles())
            .Concat(WrittenByHand.SelectMany(pattern => RepositoryTree.Files("", pattern)))
            .Where(file => !TheScansOwnFiles.Contains(file.Path, StringComparer.Ordinal))
            .Concat(RepositoryTree.Documents())
            .Concat(RepositoryTree.BuildFiles())
            .DistinctBy(file => file.Path, StringComparer.Ordinal);

    /// <summary>A name a step deleted is named nowhere: not in a line of the library's code or of a test, not in a comment or its XML
    /// documentation, not in a document of the repository (the architecture map, the change log, a read-me, the docs site). One case
    /// for the whole list, which names every mention it finds.</summary>
    [Fact]
    public void NoDeletedName_IsNamedInCodeTestCommentOrDocument()
    {
        var files = ScannedFiles().ToList();

        var findings = DeletedName.All.SelectMany(deleted => files.SelectMany(deleted.MentionsIn)).ToList();

        Assert.True(findings.Count == 0, $"{findings.Count} mentions of a deleted name:\n" + string.Join('\n', findings));
    }

    [Fact]
    public void TheScan_ReadsTheTestsOfEveryTestProject_ButTheListAndItsOwnCases()
    {
        var files = ScannedFiles().Select(f => f.Path).ToList();

        Assert.Contains("Tst/MatPlotLibNet/Packaging/OneOwnerSourceContractTests.cs", files);
        Assert.Contains(files, path => path.StartsWith("Tst/MatPlotLibNet.Wiki/", StringComparison.Ordinal));
        Assert.Contains(files, path => path.StartsWith("Src/MatPlotLibNet/", StringComparison.Ordinal));
        Assert.All(TheScansOwnFiles, own => Assert.DoesNotContain(own, files));
        Assert.All(TheScansOwnFiles, own => Assert.Contains(RepositoryTree.TestFiles(), f => f.Path == own));
    }

    /// <summary>A comment of a build file names a test that holds a rule beside it (the version, the baseline, the frozen package): when
    /// that class is renamed or split, the sweep reads those files too, or the comment keeps naming a class that is gone.</summary>
    [Fact]
    public void TheScan_ReadsTheBuildFilesToo_TheSharedPropertiesTheProjectsAndTheWorkflows()
    {
        var files = ScannedFiles().Select(f => f.Path).ToList();

        Assert.Contains("Directory.Build.props", files);
        Assert.Contains("Directory.Build.targets", files);
        Assert.Contains("Src/MatPlotLibNet.Notebooks/MatPlotLibNet.Notebooks.csproj", files);
        Assert.Contains(files, path => path.StartsWith(".github/workflows/", StringComparison.Ordinal));
    }

    /// <summary>A sample is code a reader copies and a script is run by hand: a comment in either names the library's types as a
    /// document does, so the scan reads every folder of the repository, not the library and its tests alone, and every kind of file
    /// a person writes there.</summary>
    [Theory]
    [InlineData("Samples/MatPlotLibNet.Samples.AspNetCore/Program.cs")]
    [InlineData("Samples/MatPlotLibNet.Samples.AspNetCore/wwwroot/index.html")]
    [InlineData("Samples/MatPlotLibNet.Samples.Blazor/Components/Pages/Home.razor")]
    [InlineData("Samples/MatPlotLibNet.Samples.Wpf/MainWindow.xaml")]
    [InlineData("Samples/MatPlotLibNet.Samples.Avalonia/MainWindow.axaml")]
    [InlineData("Benchmarks/MatPlotLibNet.Benchmarks/IndicatorBenchmarks.cs")]
    [InlineData("Src/matplotlibnet-client/src/index.ts")]
    [InlineData("tools/mpl_reference/generate.py")]
    [InlineData("tools/prepush.ps1")]
    [InlineData("tools/coverage/run.sh")]
    public void TheScan_ReadsWhatIsWrittenOutsideTheLibraryAndItsTests_OnceEach(string path)
    {
        var files = ScannedFiles().Select(f => f.Path).ToList();

        Assert.Single(files, path);
    }

    [Fact]
    public void TheDocumentScan_ReadsTheMapTheLogAndTheSite_AndNoRecord()
    {
        var files = RepositoryTree.Documents().Select(f => f.Path).ToList();

        Assert.Contains("Src/MatPlotLibNet/ARCHITECTURE.md", files);
        Assert.Contains("CHANGELOG.md", files);
        Assert.Contains("README.md", files);
        Assert.Contains(files, path => path.StartsWith("docs/", StringComparison.Ordinal));
        Assert.DoesNotContain(files, path => path.StartsWith(".path-manifests/", StringComparison.Ordinal) || path.StartsWith("out/", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("| the tick marks' `SpineHalf` (`EdgeTerms.Facing`, `YSideTerms.Hang`) |")]
    [InlineData("    /// <summary>The terms of a side (<see cref=\"YSideTerms\"/>).</summary>")]
    [InlineData("var ink = FacingInk.Of(side);")]
    [InlineData("the ink (MatPlotLibNet.Rendering.FacingInk.Of) faces the labels")]
    [InlineData("/// (<see cref=\"AxisTickExtensions.BottomAxisLabelOffset\"/>); the layout reserves the title")]
    public void AMentionOfADeletedName_IsFound_InACommentOrAProseLineToo(string line)
    {
        var findings = DeletedName.All.SelectMany(d => d.MentionsIn(new SourceFile("Src/MatPlotLibNet/ARCHITECTURE.md", line)));

        Assert.Single(findings);
    }

    /// <summary>A name a release published stays in the records of what each release published and removed (the change log, the
    /// roadmap and its wiki copy, the wiki page of the migration, the recipe of a fixture drawn by an old package); a name that was never published stands nowhere.</summary>
    [Theory]
    [InlineData("CHANGELOG.md", true, 0)]
    [InlineData("ROADMAP.md", true, 0)]
    [InlineData("Roadmap.md", true, 0)]
    [InlineData("Migrating-to-1.30.0.md", true, 0)]
    [InlineData("README.md", true, 1)]
    [InlineData("Src/MatPlotLibNet/ARCHITECTURE.md", true, 1)]
    [InlineData("CHANGELOG.md", false, 1)]
    public void APublishedName_StandsOnlyInTheRecords(string path, bool published, int findings)
    {
        var deleted = new DeletedName("OldVerb", "NewVerb", published);

        Assert.Equal(findings, deleted.MentionsIn(new SourceFile(path, "- `OldVerb` -> `NewVerb`")).Count);
    }

    [Theory]
    [InlineData("internal readonly record struct FacingInk(double Facing, double Outward)")]
    [InlineData("Ink = new FacingInk(FacingInk.Ascent, FacingInk.Descent),")]
    [InlineData("var font = FacingInk.Offset(size);")]
    [InlineData("private sealed class MyYSideTermsCache { }")]
    public void ANameThatOnlyLooksLikeADeletedOne_IsNotFound(string line)
    {
        Assert.Empty(DeletedName.All.SelectMany(d => d.MentionsIn(new SourceFile("Src/Any.cs", line))));
    }
}
