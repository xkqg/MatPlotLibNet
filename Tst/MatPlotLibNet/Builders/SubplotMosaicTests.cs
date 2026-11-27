// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Builders;
using MatPlotLibNet.Diagnostics;
using MatPlotLibNet.Models;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Builders;

[Collection("ChartDiagnosticsGlobalState")]
public class SubplotMosaicTests
{
    // --- the pattern of a mosaic (MosaicFigureBuilder.Parse) ---

    [Fact]
    public void Parser_Equal2x2_FourSingleCells()
    {
        var result = MosaicFigureBuilder.Parse("AB\nCD");
        Assert.Equal(4, result.Count);
        Assert.Equal(new GridPosition(0, 1, 0, 1), result["A"]);
        Assert.Equal(new GridPosition(0, 1, 1, 2), result["B"]);
        Assert.Equal(new GridPosition(1, 2, 0, 1), result["C"]);
        Assert.Equal(new GridPosition(1, 2, 1, 2), result["D"]);
    }

    [Fact]
    public void Parser_AsymmetricSpan_ASpansTwoColumns()
    {
        // "AAB\nCCB" — A spans (0,0)-(0,1), B spans (0,2)-(1,2), C spans (1,0)-(1,1)
        var result = MosaicFigureBuilder.Parse("AAB\nCCB");
        Assert.Equal(new GridPosition(0, 1, 0, 2), result["A"]);
        Assert.Equal(new GridPosition(0, 2, 2, 3), result["B"]);
        Assert.Equal(new GridPosition(1, 2, 0, 2), result["C"]);
    }

    [Fact]
    public void Parser_SingleRow_Works()
    {
        var result = MosaicFigureBuilder.Parse("ABC");
        Assert.Equal(3, result.Count);
        Assert.Equal(new GridPosition(0, 1, 0, 1), result["A"]);
        Assert.Equal(new GridPosition(0, 1, 2, 3), result["C"]);
    }

    [Fact]
    public void Parser_FullSpan_SingleLabel()
    {
        var result = MosaicFigureBuilder.Parse("AAA\nAAA");
        Assert.Single(result);
        Assert.Equal(new GridPosition(0, 2, 0, 3), result["A"]);
    }

    /// <summary>An empty pattern, a whitespace-only one (the early exit differs from an empty string, so both are read), rows of unequal
    /// length, and a label with a hole in its bounding box (A has one at (0,1) in <c>ABA/AAA</c>) are refused.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("AB\nCCC")]
    [InlineData("ABA\nAAA")]
    public void Parser_APatternThatIsNoGrid_Throws(string pattern)
    {
        Assert.Throws<ArgumentException>(() => MosaicFigureBuilder.Parse(pattern));
    }

    /// <summary>The grid is the rows that survive trimming: an empty pattern is 0 by 0, and a whitespace-only row between two others is dropped.</summary>
    [Theory]
    [InlineData("AAB\nCCB", 2, 3)]
    [InlineData("", 0, 0)]
    [InlineData("AB   \n   \nCD   ", 2, 2)]
    public void Parser_GetDimensions_ReadsTheRowsThatSurviveTrimming(string pattern, int expectedRows, int expectedCols)
    {
        var (rows, cols) = MosaicFigureBuilder.GetDimensions(pattern);
        Assert.Equal(expectedRows, rows);
        Assert.Equal(expectedCols, cols);
    }

    // --- MosaicFigureBuilder ---

    [Fact]
    public void Builder_SvgContainsFourSubplots()
    {
        var svg = Plt.Mosaic("AB\nCD").ToSvg();
        // Each subplot emits one spine group per visible spine side (4 sides × 4 subplots = 16 minimum)
        int spineCount = svg.CountOf("class=\"spine\"");
        Assert.True(spineCount >= 4, $"Expected at least 4 spine groups, got {spineCount}");
    }

    /// <summary>Covers <see cref="MosaicFigureBuilder.Configure"/> — figure-level customisation.</summary>
    [Fact]
    public void Builder_Configure_AppliesToFigure()
    {
        var fb = Plt.Mosaic("AB")
            .Configure(f => f.WithTitle("Mosaic Title").WithSize(800, 400))
            .Build();

        var figure = fb.Build();
        Assert.Equal("Mosaic Title", figure.Title);
        Assert.Equal(800, figure.Width);
        Assert.Equal(400, figure.Height);
    }

    /// <summary>A mosaic built twice gives the same panels each time: every build starts a figure builder of its own.</summary>
    [Fact]
    public void Build_Twice_GivesEachPanelOnce()
    {
        var mosaic = Plt.Mosaic("AB");
        mosaic.Build();

        Assert.Equal(2, mosaic.Build().Build().SubPlots.Count);
    }

    /// <summary>A second <c>ToSvg</c> draws what the first drew, not every panel twice.</summary>
    [Fact]
    public void ToSvg_Twice_DrawsTheSameFigure()
    {
        var mosaic = Plt.Mosaic("AB", m => m.Panel("A", ax => ax.Plot([1.0, 2.0], [2.0, 1.0])));

        Assert.Equal(mosaic.ToSvg(), mosaic.ToSvg());
    }

    /// <summary>The figure-level configuration reaches every build, and an edit to the figure builder one build returned stays
    /// in that builder.</summary>
    [Fact]
    public void Configure_ReachesEveryBuild_AndAnEditToOneBuildStaysThere()
    {
        var mosaic = Plt.Mosaic("AB").Configure(f => f.WithTitle("Mosaic"));

        mosaic.Build().WithTitle("edited");
        var figure = mosaic.Build().Build();

        Assert.Equal("Mosaic", figure.Title);
        Assert.Equal(2, figure.SubPlots.Count);
    }

    /// <summary>A label the pattern does not hold configures nothing and the build goes on, and the build reports each such label
    /// through <see cref="ChartDiagnostics"/>, the library's one channel for input it cannot use, naming the label and the pattern.</summary>
    [Fact]
    public void APanelThePatternDoesNotHold_ConfiguresNothing_AndIsReportedOncePerLabel()
    {
        var mosaic = Plt.Mosaic("AB", m => m
            .Panel("Z", ax => ax.WithTitle("never"))
            .Panel("A", ax => ax.WithTitle("kept"))
            .Panel("Y", ax => ax.WithTitle("never either")));

        var (figure, diagnostics) = DiagnosticsCapture.Of(() => mosaic.Build().Build());

        Assert.Equal(2, figure.SubPlots.Count);
        Assert.Equal("kept", figure.SubPlots[0].Title);
        Assert.Null(figure.SubPlots[1].Title);
        Assert.Equal(2, diagnostics.Count);
        Assert.All(diagnostics, d => Assert.Equal(nameof(MosaicFigureBuilder), d.Source));
        Assert.Contains("'Z'", diagnostics[0].Message, StringComparison.Ordinal);
        Assert.Contains("'Y'", diagnostics[1].Message, StringComparison.Ordinal);
        Assert.Contains("\"AB\"", diagnostics[0].Message, StringComparison.Ordinal);
    }

    /// <summary>A mosaic whose every panel is in the pattern reports nothing.</summary>
    [Fact]
    public void AMosaicWhoseLabelsAreAllInThePattern_ReportsNothing()
    {
        var diagnostics = DiagnosticsCapture.During(() => Plt.Mosaic("AB", m => m.Panel("A", ax => ax.WithTitle("a")).Panel("B", ax => ax.WithTitle("b"))).Build());

        Assert.Empty(diagnostics);
    }

    /// <summary>Covers <see cref="MosaicFigureBuilder.Save"/> — round-trips through the underlying
    /// FigureBuilder.Save path. Writes to a temp file then asserts a non-empty SVG file exists.</summary>
    [Fact]
    public void Builder_Save_WritesFile()
    {
        string tmp = Path.Combine(Path.GetTempPath(), $"mpln_mosaic_{Guid.NewGuid():N}.svg");
        try
        {
            Plt.Mosaic("AB").Save(tmp);
            Assert.True(File.Exists(tmp));
            Assert.True(new FileInfo(tmp).Length > 0);
        }
        finally
        {
            if (File.Exists(tmp)) File.Delete(tmp);
        }
    }

    // --- Parser branch coverage ---

    /// <summary>The cells <see cref="Builders.MosaicFigureBuilder.Parse"/> places and the grid <see cref="Builders.MosaicFigureBuilder.GetDimensions"/>
    /// reports come from the same rows: whatever the pattern's line endings, blank lines and trailing blanks, the last row
    /// and column a label reaches is inside the grid, and the grid is no larger than the cells reach.</summary>
    [Theory]
    [InlineData("AAB\nCCB")]
    [InlineData("AAB\r\nCCB\r\n")]
    [InlineData("\n\nAB  \n   \n\nCD\n\n")]
    [InlineData("A")]
    [InlineData("AB\nAB   \n")]
    public void Parse_AndGetDimensions_ReadTheSameRows(string pattern)
    {
        var cells = MosaicFigureBuilder.Parse(pattern);
        var (rows, cols) = MosaicFigureBuilder.GetDimensions(pattern);

        Assert.Equal(cells.Values.Max(p => p.RowEnd), rows);
        Assert.Equal(cells.Values.Max(p => p.ColEnd), cols);
    }

    /// <summary>A pattern with no row left once blanks are stripped has no grid: the dimensions are 0 by 0, the rule
    /// <see cref="Builders.MosaicFigureBuilder.Parse"/> refuses by throwing.</summary>
    [Theory]
    [InlineData("\n \n   \n")]
    [InlineData("")]
    public void APatternOfOnlyBlanks_HasNoGrid(string pattern)
    {
        var (rows, cols) = MosaicFigureBuilder.GetDimensions(pattern);

        Assert.Equal(0, rows);
        Assert.Equal(0, cols);
        Assert.Throws<ArgumentException>(() => MosaicFigureBuilder.Parse(pattern));
    }
}
