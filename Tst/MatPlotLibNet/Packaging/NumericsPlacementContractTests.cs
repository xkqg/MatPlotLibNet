// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>An algorithm the library works out on numbers is declared in the Numerics folder and namespace, beside the other
/// calculations; each calculation it needs more than once has one owner, held by the rows of <c>OwnedRules.NumericsRules</c>.</summary>
public class NumericsPlacementContractTests
{
    

    

    

    

    /// <summary>The algorithms the library works out on numbers (the contour lines and their levels, the squarified layout of a tree, the Haar
    /// wavelet) are declared in the Numerics folder and namespace, beside the other calculations, and not in a folder of the renderer or of
    /// the indicators that happen to call them.</summary>
    public static TheoryData<string> Algorithms { get; } = ["MarchingSquares", "ContourLevels", "SquarifiedLayout", "HaarDwt", "DwtResult"];

    [Theory]
    [MemberData(nameof(Algorithms))]
    public void AnAlgorithm_IsDeclaredInTheNumericsFolderAndNamespace(string type)
    {
        var declaration = new System.Text.RegularExpressions.Regex(@"^\s*internal\s+(?:static\s+class|readonly\s+record\s+struct)\s+" + type + @"\b",
            System.Text.RegularExpressions.RegexOptions.Multiline);

        var declaring = RepositoryTree.SourceFiles()
            .Where(file => file.Path.StartsWith("Src/MatPlotLibNet/", StringComparison.Ordinal) && declaration.IsMatch(file.Text))
            .ToList();

        var file = Assert.Single(declaring);
        Assert.StartsWith("Src/MatPlotLibNet/Numerics/", file.Path, StringComparison.Ordinal);
        Assert.Contains("namespace MatPlotLibNet.Numerics;", file.Text, StringComparison.Ordinal);
    }

    /// <summary>The folder and the namespace say the same thing: a file under <c>Numerics</c> is in the <c>MatPlotLibNet.Numerics</c>
    /// namespace and a file in that namespace is under <c>Numerics</c>, so a calculation is found where its namespace says it is.</summary>
    [Fact]
    public void TheNumericsNamespaceAndTheNumericsFolder_HoldTheSameFiles()
    {
        const string Folder = "Src/MatPlotLibNet/Numerics/";
        var declaration = new System.Text.RegularExpressions.Regex(@"^namespace\s+MatPlotLibNet\.Numerics\s*;", System.Text.RegularExpressions.RegexOptions.Multiline);

        var apart = RepositoryTree.SourceFiles()
            .Where(file => file.Path.StartsWith("Src/MatPlotLibNet/", StringComparison.Ordinal)
                           && file.Path.StartsWith(Folder, StringComparison.Ordinal) != declaration.IsMatch(file.Text))
            .Select(file => file.Path);

        Assert.Empty(apart);
    }

    [Fact]
    public void TheFoldersTheAlgorithmsLeft_HoldNoSourceFile()
    {
        var left = RepositoryTree.SourceFiles()
            .Where(file => file.Path.StartsWith("Src/MatPlotLibNet/Rendering/Algorithms/", StringComparison.Ordinal)
                           || file.Path.StartsWith("Src/MatPlotLibNet/Indicators/Wavelet/", StringComparison.Ordinal))
            .Select(file => file.Path);

        Assert.Empty(left);
    }
}
