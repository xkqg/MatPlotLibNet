// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A ROW OF TILES HAS ONE ANATOMY. The number, its label and its caption sit at the same height in every
/// tile of a row, whether or not that tile carries a sparkline — a tile system whose stack moves when a trend is
/// added is not a system, and on a wall the eye reads the row as a line of numbers, not as eight separate
/// pictures (reported from the Ait ops wall 2026-08-31: the two tiles without a trend sat visibly lower).
/// So the trend strip is ALWAYS reserved; a tile without one leaves it empty rather than growing into it.</summary>
public class StatTileRowAlignmentTests
{
    private static (double Value, double Label) Baselines(bool withTrend, string? caption = null)
    {
        string svg = Plt.Create().WithSize(200, 220)
            .AddSubPlot(1, 1, 1, ax => ax.StatTile(42, t =>
            {
                t.Label = "Processes";
                t.Format = "0";
                t.Caption = caption;
                if (withTrend)
                {
                    t.Trend = [1, 4, 2, 6, 3];
                }
            }))
            .ToSvg();

        double Y(string text)
        {
            var m = Regex.Match(svg, "<text[^>]*y=\"([-0-9.]+)\"[^>]*>" + Regex.Escape(text) + "<");
            Assert.True(m.Success, $"'{text}' was not drawn");
            return double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
        }

        return (Y("42"), Y("Processes"));
    }

    [Fact]
    public void ATileWithASparkline_PutsItsNumberWhereATileWithoutOneDoes()
    {
        var with = Baselines(withTrend: true);
        var without = Baselines(withTrend: false);

        Assert.Equal(with.Value, without.Value, 3);
        Assert.Equal(with.Label, without.Label, 3);
    }

    /// <summary>And a tile with a two-line caption puts its number where a tile with none does. Measured on the
    /// Ait wall 2026-08-31 the row's numbers sat at y=65, 72 and 79 depending on how many caption lines each
    /// tile carried, because the stack was CENTRED in the tile: the owner reads the row as one line of numbers
    /// (*"ik zou de getallen en de tekst daaronder op dezelfde hoogte plaatsen"*), so the stack is anchored at
    /// the top and the captions grow downward into the room that is left.</summary>
    [Fact]
    public void HoweverManyCaptionLines_TheNumberAndItsLabelDoNotMove()
    {
        var none = Baselines(withTrend: true);
        var one = Baselines(withTrend: true, caption: "threshold 250");
        var two = Baselines(withTrend: true, caption: "threshold 250" + Environment.NewLine + "2081 msg - 9 s");

        Assert.Equal(none.Value, one.Value, 3);
        Assert.Equal(none.Value, two.Value, 3);
        Assert.Equal(none.Label, one.Label, 3);
        Assert.Equal(none.Label, two.Label, 3);
    }
}
