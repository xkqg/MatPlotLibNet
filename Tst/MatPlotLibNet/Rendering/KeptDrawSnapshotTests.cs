// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A kept draw is handed out again only when nothing it was drawn from changed, so the copy of the data it is compared
/// against has to be the data it was drawn FROM: taken together with its key, before the draw. Data that changes while the axes
/// is drawn (a renderer of the caller's that writes to it, another thread that appends to it) is then seen by the next draw,
/// which draws the axes again.</summary>
[Collection("ChartServicesGlobalState")]
public sealed class KeptDrawSnapshotTests
{
    /// <summary>Writes the first value of the first series as text, then moves that value on: its own draw changes its data.</summary>
    private sealed class WritesWhatItDraws(Axes axes, Rect plotArea, IRenderContext ctx, Theme theme)
        : AxesRenderer(axes, plotArea, ctx, theme)
    {
        public override void Render()
        {
            var line = (LineSeries)Axes.Series[0];
            Ctx.DrawText(line.YData[0].ToString(CultureInfo.InvariantCulture), new Point(10, 10),
                Theme.TickFont(), TextAlignment.Left);
            line.YData[0] += 1;
        }
    }

    [Fact]
    public void DataThatChangesDuringADraw_IsDrawnAgainNextTime()
    {
        AxesRenderer.RegisterRenderer(CoordinateSystem.Polar, (a, p, c, t) => new WritesWhatItDraws(a, p, c, t));
        try
        {
            var figure = Plt.Create().WithSize(300, 200).Plot([0.0, 1, 2], [1.0, 2, 3]).Build();
            figure.SubPlots[0].CoordinateSystem = CoordinateSystem.Polar;

            string first = figure.ToSvg();
            string second = figure.ToSvg();
            string third = figure.ToSvg();

            Assert.Contains(">1<", first);
            Assert.Contains(">2<", second);
            Assert.Contains(">3<", third);
        }
        finally
        {
            AxesRenderer.RegisterRenderer(CoordinateSystem.Polar, (a, p, c, t) => new PolarAxesRenderer(a, p, c, t));
        }
    }
}
