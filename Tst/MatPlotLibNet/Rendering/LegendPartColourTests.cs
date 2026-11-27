// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A series that colours and names its own parts (a waffle's categories, a chord's groups) lists a row for each part in the
/// colour that part is drawn in: the colour its caller gave it (<c>Colors</c>), else the colour the theme hands out at the part's
/// own position. The row had the theme's colour whatever the caller gave, so a waffle drawn in red, green and blue named its
/// categories in blue, orange and green.</summary>
public sealed class LegendPartColourTests
{
    private static readonly Theme V2 = Theme.MatplotlibV2;

    /// <summary>The rows of the first legend: each row's text and the colour of its handle (the fill of a patch, the stroke of a line), in the
    /// order they are listed.</summary>
    internal static List<(string Text, string Fill)> LegendRows(Figure figure)
    {
        var legend = figure.ToSvg().Xml().Descendants().First(e => (string?)e.Attribute("class") == "legend");
        var rows = new List<(string, string)>();
        string? fill = null;
        foreach (var child in legend.Elements())
        {
            if (child.Is("rect")) fill = (string?)child.Attribute("fill");
            else if (child.Is("line")) fill = (string?)child.Attribute("stroke");
            else if (child.Is("text")) rows.Add((child.Value, fill ?? ""));
        }
        return rows;
    }

    private static Figure Waffle(Color[]? colours) =>
        Plt.Create().WithSize(500, 400).AddSubPlot(1, 1, 1, ax => ax.Waffle(["a", "b", "c"], [5.0, 3, 2], w => w.Colors = colours).WithLegend()).Build();

    [Fact]
    public void AWaffleLegend_WearsTheColoursItsCellsWear()
    {
        var rows = LegendRows(Waffle([Colors.Red, Colors.Green, Colors.Blue]));

        Assert.Equal([("a", Colors.Red.ToHex()), ("b", Colors.Green.ToHex()), ("c", Colors.Blue.ToHex())], rows);
    }

    [Fact]
    public void AWaffleLegend_GivenSomeColours_WearsTheThemesColourAtThePositionOfTheRest()
    {
        var rows = LegendRows(Waffle([Colors.Red]));

        Assert.Equal([("a", Colors.Red.ToHex()), ("b", V2.CycleColors[1].ToHex()), ("c", V2.CycleColors[2].ToHex())], rows);
    }

    [Fact]
    public void AWaffleLegend_GivenNoColours_WearsTheThemesColours()
    {
        var rows = LegendRows(Waffle(null));

        Assert.Equal([("a", V2.CycleColors[0].ToHex()), ("b", V2.CycleColors[1].ToHex()), ("c", V2.CycleColors[2].ToHex())], rows);
    }

    [Fact]
    public void AChordLegend_WearsTheColoursOfItsArcs()
    {
        var figure = Plt.Create().WithSize(500, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Chord(["x", "y", "z"], new double[,] { { 0, 2, 1 }, { 2, 0, 3 }, { 1, 3, 0 } }, c => c.Colors = [Colors.Red, Colors.Green, Colors.Blue]).WithLegend())
            .Build();

        Assert.Equal([("x", Colors.Red.ToHex()), ("y", Colors.Green.ToHex()), ("z", Colors.Blue.ToHex())], LegendRows(figure));
    }
}
