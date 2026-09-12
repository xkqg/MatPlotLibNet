// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Extensions;

/// <summary>A figure has ONE accessible name, and everything that names the figure to a reader — the SVG
/// <c>&lt;title&gt;</c>, the data table's caption — reads it from the same place. Three arms: the alt text an
/// author wrote, the title, and for a tile row that deliberately carries no title, the tiles' labels.</summary>
public class FigureAccessibleNameTests
{
    [Fact]
    public void TheAltText_Wins()
    {
        var figure = Plt.Create().WithTitle("Title").WithAltText("Alt").Plot([1.0], [2.0]).Build();

        Assert.Equal("Alt", figure.AccessibleName());
    }

    [Fact]
    public void WithoutAltText_TheTitleNamesTheFigure()
    {
        var figure = Plt.Create().WithTitle("Title").Plot([1.0], [2.0]).Build();

        Assert.Equal("Title", figure.AccessibleName());
    }

    [Fact]
    public void AnUntitledTileRow_IsNamedByItsTiles()
    {
        var figure = Plt.Create().WithSize(300, 100)
            .AddSubPlot(1, 2, 1, ax => ax.StatTile(1, t => t.Label = "Processes"))
            .AddSubPlot(1, 2, 2, ax => ax.StatTile(2, t => t.Label = "Threads"))
            .Build();

        Assert.Equal("Processes · Threads", figure.AccessibleName());
    }

    [Fact]
    public void AnUntitledFigure_WithoutTiles_HasAnEmptyName()
    {
        var figure = Plt.Create().Plot([1.0], [2.0]).Build();

        Assert.Equal("", figure.AccessibleName());
    }

    [Fact]
    public void ATileWithoutALabel_ContributesNothing()
    {
        var figure = Plt.Create().WithSize(300, 100)
            .AddSubPlot(1, 2, 1, ax => ax.StatTile(1))
            .AddSubPlot(1, 2, 2, ax => ax.StatTile(2, t => t.Label = "Threads"))
            .Build();

        Assert.Equal("Threads", figure.AccessibleName());
    }

    [Fact]
    public void TheSvgTitle_IsTheAccessibleName_ForEveryArm()
    {
        // The lift is behaviour-preserving: what the <title> said before, it says now, through the one rule.
        var byAlt = Plt.Create().WithTitle("T").WithAltText("A").Plot([1.0], [2.0]).Build();
        var byTitle = Plt.Create().WithTitle("T").Plot([1.0], [2.0]).Build();
        var byTiles = Plt.Create().WithSize(200, 100).AddSubPlot(1, 1, 1, ax => ax.StatTile(1, t => t.Label = "P")).Build();

        foreach (var figure in new[] { byAlt, byTitle, byTiles })
        {
            Assert.Contains($"<title id=\"chart-title\">{figure.AccessibleName()}</title>", figure.ToSvg());
        }
    }
}
