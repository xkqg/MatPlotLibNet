// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>The architecture document is a map of the source tree, and a map is true or it is worse than none: a file it names is a
/// file of the repository, in the folder the map puts it in. A file that is moved, renamed, split or folded into another moves its
/// line in the map in the same change (<see cref="ArchitectureMap"/> reads the document).</summary>
public class ArchitectureMapContractTests
{
    private const string Document = "Src/MatPlotLibNet/ARCHITECTURE.md";

    private static readonly string[] Source =
    [
        "Src/Lib/Rendering/Categorical/BarSeriesRenderer.cs",
        "Src/Lib/Rendering/ColorBarRendering/ResolvedColorBar.cs",
        "Src/Lib/Rendering/Layout/FigureGrid.cs",
        "Src/Lib/Builders/GridAxesExtensions.cs",
        "Src/Lib/Builders/GridAxesExtensions.CalendarGrid.cs",
        "Src/Lib/Indicators/EhlersSuperSmoother.cs",
        "Src/Lib/Indicators/Steps/HilbertStep.cs",
        "Src/Lib/Fonts/DejaVuSans-Bold.ttf",
    ];

    private static readonly string[] Repository = [.. Source, "Tst/Lib/Indicators/Ehlers/SuperSmootherTests.cs", "tools/coverage/Gate.cs"];

    private static IReadOnlyList<string> Read(params string[] lines) =>
        ArchitectureMap.Misplaced(new SourceFile(Document, string.Join('\n', lines)), Source, Repository);

    [Fact]
    public void EveryFileAndFolderTheMapNames_IsInTheRepository_WhereTheMapPutsIt()
    {
        var findings = ArchitectureMap.Misplaced(
            RepositoryTree.FileAt("Src", "MatPlotLibNet", "ARCHITECTURE.md"), RepositoryTree.WalkOf("Src").Files, RepositoryTree.WalkOf(string.Empty).Files);

        Assert.True(findings.Count == 0, $"{findings.Count} names the repository does not hold:\n" + string.Join('\n', findings));
    }

    /// <summary>Each row is one way the map named what was not there: a file that was deleted, a file name made from a class name, a
    /// file left under the folder it moved out of, a folder that is gone, a type named by a folder that no longer holds it, a file
    /// named in prose, in a table and after the first file of a line.</summary>
    [Theory]
    [InlineData("```", "Rendering/", "    Categorical/", "        BarShapes.cs — the bars of a series as drawn", "```")]
    [InlineData("```", "    ColorBarRendering/", "      ResolvedColorBarExtensions.cs (the provider's members) — internal", "```")]
    [InlineData("```", "  Layout/ — where an axes stands", "    GridAxesExtensions.cs — internal static: the whole-number ticks", "```")]
    [InlineData("```", "Indicators/", "    Ehlers/ — internal DSP", "```")]
    [InlineData("```", "    EhlersSuperSmoother.cs — a wrapper over Steps/SuperSmoother", "```")]
    [InlineData("The gradient of a bar is made in BarShapes.cs, once.")]
    [InlineData("| the bars as drawn | `Rendering/Layout/BarSeriesRenderer.cs` | the bar renderer |")]
    [InlineData("```", "    Layout/", "      FigureGrid.cs, GridEdges.cs — the cells of a figure", "```")]
    [InlineData("```", "    FigureGrid.cs: the cells", "        of a figure", "    Layout/", "```", "```", "Categorical/", "  FigureGrid.cs — the cells", "```")]
    public void ANameTheRepositoryDoesNotHold_IsFound(params string[] lines) => Assert.Single(Read(lines));

    /// <summary>A folder that only the map still names is a folder to the reader of the prose too: the type the prose puts in it is
    /// found beside the folder line, so the line that is left when the folder line is taken out is not passed over.</summary>
    [Fact]
    public void ATypeNamedByAFolderOnlyTheMapStillNames_IsFoundBesideThatFolder()
    {
        var findings = Read("```", "Indicators/", "    Ehlers/", "```", "The smoother of Ehlers/SuperSmoother is read by two indicators.");

        Assert.Equal(2, findings.Count);
    }

    /// <summary>What the map may say: a file under its folder, a part of a split file, two files on a line, a file of the tests or the
    /// tools in prose, a file by its folders, a type by its folder and a member of it, a font by the start of its name, a pattern of
    /// file names, a file before any folder line, and two words joined by a stroke that are no folder and name.</summary>
    [Theory]
    [InlineData("```", "Rendering/", "    Categorical/ — bars", "        BarSeriesRenderer.cs — the bars", "    Layout/", "        FigureGrid.cs (the cells) — internal", "```")]
    [InlineData("```", "Builders/", "  GridAxesExtensions.cs, GridAxesExtensions.CalendarGrid.cs — the grid verbs", "```")]
    [InlineData("The gate is tools/coverage/Gate.cs; SuperSmootherTests.cs holds the smoother.")]
    [InlineData("| the cells | `Rendering/Layout/FigureGrid.cs` | `Layout/FigureGrid.EdgeIn` |")]
    [InlineData("```", "    EhlersITrend — folds Steps/HilbertStep; fonts are Fonts/DejaVuSans*.ttf", "```")]
    [InlineData("A verb family is AxesBuilder<Family>Verbs.cs and a generated table is *.g.cs.")]
    [InlineData("```", "FigureGrid.cs — the cells of a figure", "```")]
    [InlineData("The Left/Right spines and a Treemap/Sunburst pair share one rule.")]
    public void ANameTheRepositoryHolds_IsNotFound(params string[] lines) => Assert.Empty(Read(lines));
}
