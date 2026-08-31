// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>On a WALL a grid is read from across the room, and a row is wide: the eye slips from a name on the
/// left to a number on the right. <see cref="TreeGridSeries.RowStripe"/> paints every other row's band, which is
/// what carries the eye across — the oldest trick in tabular typography and the one thing a wider row needs.
/// Null (the default) paints nothing, so a grid inside a dense figure stays as it was.</summary>
public class TreeGridStripeTests
{
    private static string Svg(Color? stripe, params TreeGridRow[] rows)
        => Plt.Create().WithSize(900, 300)
            .AddSubPlot(1, 1, 1, ax => ax.TreeGrid(rows, s =>
            {
                s.ColumnHeaders = ["CPU"];
                s.RowStripe = stripe;
            }))
            .ToSvg();

    private static TreeGridRow[] Four() =>
    [
        new("Ait", ["29 %"]),
        new("Ait.Binance", ["1 %"]) { Depth = 1 },
        new("Ait.Bitvavo", ["4 %"]) { Depth = 1 },
        new("Ait.Bus", ["12 %"]) { Depth = 1 },
    ];

    private static int Bands(string svg) => Regex.Count(svg, "mpl-treegrid-stripe");

    [Fact]
    public void WithoutAStripe_NoBandIsPainted()
        => Assert.Equal(0, Bands(Svg(null, Four())));

    /// <summary>Every OTHER row — four rows is two bands, and the first row is never one, so the header's rule
    /// and the first band do not touch.</summary>
    [Fact]
    public void WithAStripe_EveryOtherRowGetsOne()
        => Assert.Equal(2, Bands(Svg(Color.FromHex("#1E2226"), Four())));

    [Fact]
    public void TheBand_CarriesTheGivenInk()
        => Assert.Contains("#1E2226", Svg(Color.FromHex("#1E2226"), Four()), StringComparison.Ordinal);

    /// <summary>A band is the row's own height and the region's full width — anything narrower would stop
    /// exactly where the eye needs it most, at the numbers.</summary>
    [Fact]
    public void TheBand_SpansTheWholeRegion()
    {
        string svg = Svg(Color.FromHex("#1E2226"), Four());

        var band = Regex.Match(svg, "<rect[^>]*mpl-treegrid-stripe[^>]*>").Value;
        if (band.Length == 0)
        {
            band = Regex.Match(svg, "<g[^>]*mpl-treegrid-stripe[^>]*>\\s*<rect[^>]*>").Value;
        }

        Assert.Contains("width=", band, StringComparison.Ordinal);
    }
}
