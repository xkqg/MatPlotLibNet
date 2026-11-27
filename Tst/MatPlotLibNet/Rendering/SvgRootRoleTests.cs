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

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("AFigureWithALink_IsAGroup", () => Render(t => t.Url = "/?panel=x"), "role=\"group\"") { Absent = ["role=\"img\""] },
        // An untitled tile row still has an accessible name: the tile labels, joined.
        new DrawnToken("AnUntitledFigure_TakesItsNameFromItsTiles", () => Render(t => t.Label = "Processes"), "<title id=\"chart-title\">Processes</title>"),
        new DrawnToken("ATitle_StillWins", () => Render(t => t.Label = "Processes", title: "Order service — fleet"), "<title id=\"chart-title\">Order service — fleet</title>"),
        new DrawnToken("AFigureWithoutALink_IsAnImage", () => Render(), "role=\"img\""),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();
}
