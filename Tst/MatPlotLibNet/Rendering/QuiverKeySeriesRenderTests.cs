// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies SVG output of <see cref="QuiverKeySeries"/> rendering.</summary>
public class QuiverKeySeriesRenderTests
{
    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("QuiverKey_SvgContainsLabelText", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.QuiverKey(0.8, 0.9, 1.0, "1 m/s"))
            .ToSvg(), "1 m/s"),
        //  QuiverKeySeriesRenderer — `dataRange > 0 ? width/dataRange : 50` false arm. Render a QuiverKey series in an axes whose
        // range collapsed to zero.
        new DrawnToken("QuiverKeySeriesRenderer_ZeroDataRange_FallsBackTo50PixelsPerUnit", () =>
        {
            // Single-point Plot collapses XAxis range to a single value; this is enough
            // to make Transform.DataXMax - Transform.DataXMin == 0 in the renderer.
            var fig = Plt.Create()
                .AddSubPlot(1, 1, 1, ax => ax
                    .Plot([1.0], [1.0])
                    .AddSeries(new QuiverKeySeries(0.5, 0.5, 1.0, "1 m/s")))
                .Build();
            return fig.ToSvg();
        }, ">1 m/s<"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

}
