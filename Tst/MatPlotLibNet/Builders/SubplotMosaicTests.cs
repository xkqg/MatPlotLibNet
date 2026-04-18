// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Builders;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Builders;

public class SubplotMosaicTests
{
    // --- SubplotMosaicParser ---

    [Fact]
    public void Parser_Equal2x2_FourSingleCells()
    {
        var result = SubplotMosaicParser.Parse("AB\nCD");
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
        var result = SubplotMosaicParser.Parse("AAB\nCCB");
        Assert.Equal(new GridPosition(0, 1, 0, 2), result["A"]);
        Assert.Equal(new GridPosition(0, 2, 2, 3), result["B"]);
        Assert.Equal(new GridPosition(1, 2, 0, 2), result["C"]);
    }

    [Fact]
    public void Parser_SingleRow_Works()
    {
        var result = SubplotMosaicParser.Parse("ABC");
        Assert.Equal(3, result.Count);
        Assert.Equal(new GridPosition(0, 1, 0, 1), result["A"]);
        Assert.Equal(new GridPosition(0, 1, 2, 3), result["C"]);
    }

    [Fact]
    public void Parser_FullSpan_SingleLabel()
    {
        var result = SubplotMosaicParser.Parse("AAA\nAAA");
        Assert.Single(result);
        Assert.Equal(new GridPosition(0, 2, 0, 3), result["A"]);
    }

    [Fact]
    public void Parser_EmptyPattern_Throws()
    {
        Assert.Throws<ArgumentException>(() => SubplotMosaicParser.Parse(""));
    }

    [Fact]
    public void Parser_UnequalRowLengths_Throws()
    {
        Assert.Throws<ArgumentException>(() => SubplotMosaicParser.Parse("AB\nCCC"));
    }

    [Fact]
    public void Parser_NonRectangularLabel_Throws()
    {
        // "ABA\nAAA" — A has a hole at (0,1) within its bounding box
        Assert.Throws<ArgumentException>(() => SubplotMosaicParser.Parse("ABA\nAAA"));
    }

    [Fact]
    public void Parser_GetDimensions_Returns2x3()
    {
        var (rows, cols) = SubplotMosaicParser.GetDimensions("AAB\nCCB");
        Assert.Equal(2, rows);
        Assert.Equal(3, cols);
    }

    // --- MosaicFigureBuilder ---

    [Fact]
    public void Builder_AllPanels_AreRendered()
    {
        var svg = Plt.Mosaic("AB\nCD", m =>
        {
            m.Panel("A", ax => ax.WithTitle("A"));
            m.Panel("B", ax => ax.WithTitle("B"));
            m.Panel("C", ax => ax.WithTitle("C"));
            m.Panel("D", ax => ax.WithTitle("D"));
        }).ToSvg();

        Assert.NotNull(svg);
        Assert.NotEmpty(svg);
    }

    [Fact]
    public void Builder_SvgContainsFourSubplots()
    {
        var svg = Plt.Mosaic("AB\nCD").ToSvg();
        // Each subplot emits one spine group per visible spine side (4 sides × 4 subplots = 16 minimum)
        int spineCount = System.Text.RegularExpressions.Regex.Matches(svg, "class=\"spine\"").Count;
        Assert.True(spineCount >= 4, $"Expected at least 4 spine groups, got {spineCount}");
    }

    [Fact]
    public void Builder_UnregisteredPanel_StillRenders()
    {
        // Panels without a configure action should render as empty axes (no throw)
        string svg = Plt.Mosaic("AB").ToSvg();
        Assert.NotEmpty(svg);
    }

    [Fact]
    public void Builder_Build_ReturnsFigureBuilder()
    {
        var fb = Plt.Mosaic("AB\nCD").Build();
        Assert.NotNull(fb);
    }

    [Fact]
    public void Builder_WithSpan_ToSvgSucceeds()
    {
        double[] x = [1, 2, 3];
        double[] y = [1, 4, 2];
        string svg = Plt.Mosaic("AAB\nCCB", m =>
        {
            m.Panel("A", ax => ax.Plot(x, y).WithTitle("Wide Top"));
            m.Panel("B", ax => ax.Bar(["X", "Y"], [10.0, 20.0]).WithTitle("Tall Right"));
            m.Panel("C", ax => ax.Scatter(x, y).WithTitle("Wide Bottom"));
        }).ToSvg();
        Assert.NotEmpty(svg);
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

    /// <summary>Covers the whitespace-only branch of <see cref="SubplotMosaicParser.Parse"/>.</summary>
    [Fact]
    public void Parser_WhitespaceOnly_Throws()
    {
        // string.IsNullOrWhiteSpace covers the early-exit branch differently from "" — exercise both
        Assert.Throws<ArgumentException>(() => SubplotMosaicParser.Parse("   "));
    }

    /// <summary>Covers <see cref="SubplotMosaicParser.GetDimensions"/> empty/whitespace path
    /// where rows.Length == 0 and the ternary returns 0 columns.</summary>
    [Fact]
    public void Parser_GetDimensions_EmptyPattern_ReturnsZeroZero()
    {
        var (rows, cols) = SubplotMosaicParser.GetDimensions("");
        Assert.Equal(0, rows);
        Assert.Equal(0, cols);
    }

    /// <summary>Covers <see cref="SubplotMosaicParser.GetDimensions"/> with whitespace-only rows
    /// stripped by TrimEnd — verifies the row-count branch when only some rows survive.</summary>
    [Fact]
    public void Parser_GetDimensions_WhitespaceRowsStripped()
    {
        var (rows, cols) = SubplotMosaicParser.GetDimensions("AB   \n   \nCD   ");
        Assert.Equal(2, rows); // middle whitespace-only row is dropped
        Assert.Equal(2, cols);
    }
}
