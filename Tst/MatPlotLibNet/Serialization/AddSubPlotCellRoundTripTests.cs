// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json;
using MatPlotLibNet.Models;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>A subplot added with <c>AddSubPlot(rows, cols, index)</c> stands in one cell of a rows-by-columns grid, and the
/// JSON keeps that cell: read back, the figure lays its subplots out where they stood before it was written, in the default
/// layout and in the constrained one. Without it every subplot read back stood in one row (the default layout) or touched
/// every edge of the figure (the constrained layout's measure), so a 2 by 1 stack came back side by side. A figure of one
/// subplot in one cell writes no cell, so it writes the bytes it wrote before the cell reached the JSON.
/// <para>Tagged into the <c>ChartSerializerGlobalState</c> collection: the lines read back through the process-global series
/// registry, which <see cref="SeriesRegistryTests"/> briefly rewrites.</para></summary>
[Collection("ChartSerializerGlobalState")]
public class AddSubPlotCellRoundTripTests
{
    private static readonly ChartSerializer Serializer = new();

    private static Figure RoundTrip(Figure figure) => Serializer.FromJson(Serializer.ToJson(figure));

    /// <summary>One cell of a grid: the grid's rows and columns and the cell's one-based index.</summary>
    private readonly record struct Cell(int Rows, int Cols, int Index);

    /// <summary>The grids the round trip is held to, by name: every cell of a row and of a column, three of four cells of a
    /// square, and the two ends of a row of three (the middle left empty).</summary>
    private static readonly IReadOnlyDictionary<string, Cell[]> Grids = new Dictionary<string, Cell[]>(StringComparer.Ordinal)
    {
        ["1x2"] = [new(1, 2, 1), new(1, 2, 2)],
        ["2x1"] = [new(2, 1, 1), new(2, 1, 2)],
        ["2x2 cells 1, 2 and 4"] = [new(2, 2, 1), new(2, 2, 2), new(2, 2, 4)],
        ["1x3 cells 1 and 3"] = [new(1, 3, 1), new(1, 3, 3)],
    };

    /// <summary>Every grid, in the default layout and in the constrained one.</summary>
    public static TheoryData<string, bool> GridsAndLayouts { get; } = new(
        from grid in Grids.Keys
        from constrained in new[] { false, true }
        select (grid, constrained));

    /// <summary>A figure with one titled, labelled line in each of <paramref name="cells"/>, so each cell has bands on every
    /// edge that the layout measures.</summary>
    private static Figure FigureOf(IEnumerable<Cell> cells, bool constrained)
    {
        var builder = Plt.Create().WithSize(640, 480);
        if (constrained)
        {
            builder.ConstrainedLayout();
        }

        foreach (var cell in cells)
        {
            builder.AddSubPlot(cell.Rows, cell.Cols, cell.Index, ax => ax
                .Plot([1.0, 2, 3], [cell.Index, 3.0, 1.5])
                .WithTitle($"cell {cell.Index}")
                .SetXLabel("x")
                .SetYLabel("y"));
        }

        return builder.Build();
    }

    [Theory]
    [MemberData(nameof(GridsAndLayouts))]
    public void ACellOfAGrid_DrawsTheSameAfterARoundTrip(string grid, bool constrained)
    {
        var figure = FigureOf(Grids[grid], constrained);
        string drawn = figure.ToSvg();

        string readBack = RoundTrip(figure).ToSvg();

        Assert.True(drawn == readBack, $"{grid} ({(constrained ? "constrained" : "default")} layout): the picture changed in the round trip, first difference at char {FirstDifference(drawn, readBack)}.");
    }

    /// <summary>A cell that stands alone keeps its place in the grid it names: the right half of a row of two, the left half
    /// left empty.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ACellThatStandsAlone_DrawsTheSameAfterARoundTrip(bool constrained)
    {
        var figure = FigureOf([new(1, 2, 2)], constrained);

        Assert.Equal(figure.ToSvg(), RoundTrip(figure).ToSvg());
    }

    /// <summary>Two subplots added to the one cell of a 1 by 1 grid stand over each other; read back they still do, rather than
    /// side by side as two subplots that name no cell are laid out.</summary>
    [Fact]
    public void TwoSubplotsInTheOneCellOfTheirGrid_DrawTheSameAfterARoundTrip()
    {
        var figure = FigureOf([new(1, 1, 1), new(1, 1, 1)], constrained: false);

        Assert.Equal(figure.ToSvg(), RoundTrip(figure).ToSvg());
    }

    /// <summary>The cell is written as matplotlib's <c>add_subplot(rows, cols, index)</c> names it.</summary>
    [Fact]
    public void ACellOfAGrid_IsWrittenAsItsRowsColumnsAndIndex()
    {
        var figure = FigureOf([new(2, 3, 5)], constrained: false);

        using var json = JsonDocument.Parse(Serializer.ToJson(figure));

        var cell = json.RootElement.GetProperty("subPlots")[0].GetProperty("subplot");
        Assert.Equal(2, cell.GetProperty("rows").GetInt32());
        Assert.Equal(3, cell.GetProperty("cols").GetInt32());
        Assert.Equal(5, cell.GetProperty("index").GetInt32());
    }

    /// <summary>Read back, the subplot stands in the cell it was written with.</summary>
    [Fact]
    public void ACellOfAGrid_ReadsBackIntoThatCell()
    {
        var axes = RoundTrip(FigureOf([new(2, 3, 5)], constrained: false)).SubPlots.Single();

        Assert.Equal(2, axes.GridRows);
        Assert.Equal(3, axes.GridCols);
        Assert.Equal(5, axes.GridIndex);
    }

    /// <summary>The figures a figure of one cell is made by: one subplot added to the one cell of a 1 by 1 grid, and the
    /// subplot the figure builder makes for a series drawn on the figure itself.</summary>
    public static TheoryData<string> OneCellFigures { get; } = new("AddSubPlot(1, 1, 1)", "a series drawn on the figure");

    private static Figure OneCellFigure(string name) => name == "AddSubPlot(1, 1, 1)"
        ? Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2], [1.0, 2])).Build()
        : Plt.Create().Plot([1.0, 2], [1.0, 2]).Build();

    /// <summary>A figure of one subplot in one cell draws the same whether it names its cell or not, so it writes no cell and
    /// keeps the bytes it wrote before the cell reached the JSON (the golden files are all such figures).</summary>
    [Theory]
    [MemberData(nameof(OneCellFigures))]
    public void AOneCellFigure_WritesNoSubplotField(string figure)
    {
        string json = Serializer.ToJson(OneCellFigure(figure));

        Assert.DoesNotContain("\"subplot\"", json);
    }

    /// <summary>A subplot placed by a grid specification names its cells there (<c>gridPosition</c>), not as a cell of a
    /// rows-by-columns grid, so it writes no cell.</summary>
    [Fact]
    public void ASubplotPlacedByAGridSpecification_WritesNoSubplotField()
    {
        var figure = Plt.Create().WithGridSpec(2, 2)
            .AddSubPlot(new GridPosition(0, 1, 0, 2), ax => ax.Plot([1.0, 2], [1.0, 2]))
            .AddSubPlot(GridPosition.Single(1, 1), ax => ax.Plot([1.0, 2], [2.0, 1]))
            .Build();

        string json = Serializer.ToJson(figure);

        Assert.DoesNotContain("\"subplot\"", json);
        Assert.Contains("\"gridPosition\"", json);
    }

    private static int FirstDifference(string a, string b)
    {
        int i = 0;
        while (i < a.Length && i < b.Length && a[i] == b[i]) i++;
        return i;
    }
}
