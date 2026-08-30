// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>An SVG root with <c>role="img"</c> makes every descendant presentational, so a hyperlink inside
/// it is never announced — the one attribute the tile link exists for, neutralised by the root (council M16).
/// A figure that contains a link is a GROUP; a figure that contains none stays an image.</summary>
public class SvgRootRoleTests
{
    private static string Render(Action<StatTileSeries>? configure = null, string? title = null)
    {
        var b = Plt.Create().WithSize(200, 100).AddSubPlot(1, 1, 1, ax => ax.StatTile(1, configure));
        if (title is not null)
        {
            b.WithTitle(title);
        }
        return b.ToSvg();
    }

    [Fact]
    public void AFigureWithoutALink_IsAnImage()
    {
        Assert.Contains("role=\"img\"", Render());
    }

    [Fact]
    public void AFigureWithALink_IsAGroup()
    {
        string svg = Render(t => t.Url = "/?panel=x");

        Assert.Contains("role=\"group\"", svg);
        Assert.DoesNotContain("role=\"img\"", svg);
    }

    /// <summary>An untitled tile row still has an accessible name: the tile labels, joined.</summary>
    [Fact]
    public void AnUntitledFigure_TakesItsNameFromItsTiles()
    {
        string svg = Render(t => t.Label = "Processes");

        Assert.Contains("<title id=\"chart-title\">Processes</title>", svg);
    }

    [Fact]
    public void ATitle_StillWins()
    {
        string svg = Render(t => t.Label = "Processes", title: "Ait — fleet");

        Assert.Contains("<title id=\"chart-title\">Ait — fleet</title>", svg);
    }
}
