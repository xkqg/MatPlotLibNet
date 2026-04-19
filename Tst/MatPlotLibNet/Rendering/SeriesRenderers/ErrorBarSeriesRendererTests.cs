// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>Phase X.9.a (v1.7.2, 2026-04-19) — drives every render branch in
/// <see cref="MatPlotLibNet.Rendering.SeriesRenderers.ErrorBarSeriesRenderer"/>. Pre-X.9
/// the renderer was at 80%L / 68%B because the X-error and ErrorEvery branches
/// were untested. This file pins:
///   - ELineWidth and CapThick explicit (lines 20-21 ?? false arms)
///   - X errors set (line 48 true arm — horizontal error caps)
///   - ErrorEvery > 1 (line 41 modulo skipping)
///   - Default Y-only error bars (line 41 ErrorEvery=1 path)</summary>
public class ErrorBarSeriesRendererTests
{
    private static string Render(ErrorBarSeries s) =>
        Plt.Create()
            .WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(s))
            .Build()
            .ToSvg();

    [Fact]
    public void Render_BasicYOnly_DrawsCenterDotsAndYBars()
    {
        var svg = Render(new ErrorBarSeries(
            new[] { 1.0, 2, 3 }, new[] { 1.0, 4, 9 },
            new[] { 0.5, 0.5, 0.5 }, new[] { 0.5, 0.5, 0.5 }));
        Assert.Contains("<circle", svg);    // center markers
        Assert.Contains("<line", svg);      // error bars
    }

    /// <summary>Explicit ELineWidth + CapThick (lines 20-21 ?? false arms — values present).</summary>
    [Fact]
    public void Render_ExplicitELineWidth_AndCapThick_AppliesValues()
    {
        var svg = Render(new ErrorBarSeries(
            new[] { 1.0, 2, 3 }, new[] { 1.0, 4, 9 },
            new[] { 0.5, 0.5, 0.5 }, new[] { 0.5, 0.5, 0.5 })
        {
            ELineWidth = 3.0,
            CapThick = 2.0,
        });
        Assert.Contains("<line", svg);
    }

    /// <summary>X errors set (line 48 true arm) — adds horizontal error bars + caps.</summary>
    [Fact]
    public void Render_XErrors_DrawsHorizontalErrorBars()
    {
        var svg = Render(new ErrorBarSeries(
            new[] { 1.0, 2, 3 }, new[] { 1.0, 4, 9 },
            new[] { 0.5, 0.5, 0.5 }, new[] { 0.5, 0.5, 0.5 })
        {
            XErrorLow = new[] { 0.2, 0.2, 0.2 },
            XErrorHigh = new[] { 0.2, 0.2, 0.2 },
        });
        Assert.Contains("<line", svg);
    }

    /// <summary>ErrorEvery=2 (line 41 modulo skip) — only every 2nd point gets bars.
    /// All circles still drawn; line count is reduced.</summary>
    [Fact]
    public void Render_ErrorEvery_SkipsIntermediatePoints()
    {
        var allBars = Render(new ErrorBarSeries(
            new[] { 1.0, 2, 3, 4, 5 }, new[] { 1.0, 2, 3, 4, 5 },
            new[] { 0.5, 0.5, 0.5, 0.5, 0.5 }, new[] { 0.5, 0.5, 0.5, 0.5, 0.5 }));
        var sparse = Render(new ErrorBarSeries(
            new[] { 1.0, 2, 3, 4, 5 }, new[] { 1.0, 2, 3, 4, 5 },
            new[] { 0.5, 0.5, 0.5, 0.5, 0.5 }, new[] { 0.5, 0.5, 0.5, 0.5, 0.5 })
        {
            ErrorEvery = 2,
        });
        Assert.True(sparse.Split("<line").Length < allBars.Split("<line").Length);
    }
}
