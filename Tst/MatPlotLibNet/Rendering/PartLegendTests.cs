// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Builders;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A pie, a donut and a funnel list a row for each part they name, as matplotlib's legend lists a pie's wedges. matplotlib
/// 3.11.2, printed: <c>ax.pie([5, 3, 2], labels=['a', 'b', 'c'])</c> then <c>ax.legend()</c> has the rows a, b and c; a line labelled L
/// plotted before it makes the rows L, a, b, c; a label of None has no row (<c>['a', None, 'c']</c> gives a and c); with
/// <c>colors=['r', 'g', 'b']</c> the handles are red, green and blue (the wedges' colours); a pie with no labels has no row. The
/// library listed one row for the series, under its own name, whatever the parts were called, and none for a pie that was not given a
/// <c>Label</c>, so a legend asked for a pie of named slices was empty. A part is a row under every theme.</summary>
public sealed class PartLegendTests
{
    private static readonly Theme V2 = Theme.MatplotlibV2;

    private static Figure Of(Action<AxesBuilder> draw, Theme? theme = null) =>
        Plt.Create().WithSize(500, 400).WithTheme(theme ?? V2).AddSubPlot(1, 1, 1, ax => { draw(ax); ax.WithLegend(); }).Build();

    private static string Slot(Theme theme, int index) => theme.CycleColors[index].ToHex();

    public static TheoryData<string, Action<AxesBuilder>, string> NamedParts => new()
    {
        { "pie", ax => ax.Pie([5.0, 3, 2], ["a", "b", "c"]), "v2" },
        { "donut", ax => ax.Donut([5.0, 3, 2], ["a", "b", "c"]), "v2" },
        { "funnel", ax => ax.Funnel(["a", "b", "c"], [5.0, 3, 2]), "v2" },
        { "pie under the library's own theme", ax => ax.Pie([5.0, 3, 2], ["a", "b", "c"]), "default" },
    };

    /// <summary>A pie, a donut and a funnel list a row for each slice or stage they name, in the colour of the part, under every theme.</summary>
    [Theory]
    [MemberData(nameof(NamedParts))]
    public void ASeriesOfNamedParts_ListsARowForEachPart_InItsColour(string series, Action<AxesBuilder> draw, string theme)
    {
        var look = theme.AsTheme();

        var rows = LegendPartColourTests.LegendRows(Of(draw, look));

        Assert.Equal([("a", Slot(look, 0)), ("b", Slot(look, 1)), ("c", Slot(look, 2))], rows);
        _ = series;
    }

    [Fact]
    public void ThePartsOfASeriesDrawnAfterALine_StandOnTheColoursOfTheCycleItsCallAsks()
    {
        // matplotlib 3.11.2: a pie after a plot takes the patch cycle's C0, C1, C2; the library's own themes keep one cycle.
        var rows = LegendPartColourTests.LegendRows(Of(ax => ax.Plot([0.0, 1], [0.0, 1], s => s.Label = "L").Pie([5.0, 3, 2], ["a", "b", "c"])));
        var library = LegendPartColourTests.LegendRows(Of(ax => ax.Plot([0.0, 1], [0.0, 1], s => s.Label = "L").Pie([5.0, 3, 2], ["a", "b", "c"]), Theme.Default));

        Assert.Equal([("L", Slot(V2, 0)), ("a", Slot(V2, 0)), ("b", Slot(V2, 1)), ("c", Slot(V2, 2))], rows);
        Assert.Equal([("L", Slot(Theme.Default, 0)), ("a", Slot(Theme.Default, 1)), ("b", Slot(Theme.Default, 2)), ("c", Slot(Theme.Default, 3))], library);
    }

    [Fact]
    public void ASliceThatWasGivenAColour_IsListedInIt()
    {
        var rows = LegendPartColourTests.LegendRows(Of(ax => ax.Pie([5.0, 3, 2], ["a", "b", "c"], s => s.Colors = [Colors.Red, Colors.Green])));

        Assert.Equal([("a", Colors.Red.ToHex()), ("b", Colors.Green.ToHex()), ("c", Slot(V2, 2))], rows);
    }

    [Fact]
    public void ASliceWithNoName_HasNoRow_AndTheOnesAfterItKeepTheirColours()
    {
        var rows = LegendPartColourTests.LegendRows(Of(ax => ax.Pie([5.0, 3, 2], ["a", null!, "c"])));

        Assert.Equal([("a", Slot(V2, 0)), ("c", Slot(V2, 2))], rows);
    }

    [Fact]
    public void APieWhoseSlicesAreNotNamed_HasTheRowOfTheSeriesUnderItsOwnName()
    {
        var rows = LegendPartColourTests.LegendRows(Of(ax => ax.Pie([5.0, 3, 2], null, s => s.Label = "Sales")));

        Assert.Equal([("Sales", Slot(V2, 0))], rows);
    }

    [Fact]
    public void APieWithNamedSlices_HasTheirRowsAlone_NotASecondRowForTheSeries()
    {
        var rows = LegendPartColourTests.LegendRows(Of(ax => ax.Pie([5.0, 3, 2], ["a", "b", "c"], s => s.Label = "Sales")));

        Assert.Equal(["a", "b", "c"], rows.Select(r => r.Text));
    }

    [Fact]
    public void ANameBeyondTheLastSlice_HasNoRow()
    {
        var rows = LegendPartColourTests.LegendRows(Of(ax => ax.Pie([5.0, 3], ["a", "b", "c"])));

        Assert.Equal(["a", "b"], rows.Select(r => r.Text));
    }
}
