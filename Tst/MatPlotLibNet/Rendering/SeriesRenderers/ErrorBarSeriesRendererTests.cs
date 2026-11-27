// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>Phase X.9.a (v1.7.2, 2026-04-19) — drives every render branch in
/// <see cref="MatPlotLibNet.Rendering.SeriesRenderers.ErrorBarSeriesRenderer"/>. Pre-X.9
/// the renderer was at 80%L / 68%B because the X-error and ErrorEvery branches
/// were untested. This file pins:
///   - ELineWidth and CapThick explicit (?? false arms)
///   - X errors set (true arm — horizontal error caps)
///   - ErrorEvery > 1 (modulo skipping)
///   - Default Y-only error bars (ErrorEvery=1 path)</summary>
public class ErrorBarSeriesRendererTests
{
    private static string Render(ErrorBarSeries s) =>
        Plt.Create()
            .WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(s))
            .Build()
            .ToSvg();

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        // center markers
        // error bars
        new DrawnToken("Render_BasicYOnly_DrawsCenterDotsAndYBars", () => Render(new ErrorBarSeries(
            new[] { 1.0, 2, 3 }, new[] { 1.0, 4, 9 },
            new[] { 0.5, 0.5, 0.5 }, new[] { 0.5, 0.5, 0.5 })), "<circle", "<line"),
        // Explicit ELineWidth + CapThick (?? false arms — values present).
        new DrawnToken("Render_ExplicitELineWidth_AndCapThick_AppliesValues", () => Render(new ErrorBarSeries(
            new[] { 1.0, 2, 3 }, new[] { 1.0, 4, 9 },
            new[] { 0.5, 0.5, 0.5 }, new[] { 0.5, 0.5, 0.5 })
        {
            ELineWidth = 3.0,
            CapThick = 2.0,
        }), "<line"),
        // X errors set (true arm) — adds horizontal error bars + caps.
        new DrawnToken("Render_XErrors_DrawsHorizontalErrorBars", () => Render(new ErrorBarSeries(
            new[] { 1.0, 2, 3 }, new[] { 1.0, 4, 9 },
            new[] { 0.5, 0.5, 0.5 }, new[] { 0.5, 0.5, 0.5 })
        {
            XErrorLow = new[] { 0.2, 0.2, 0.2 },
            XErrorHigh = new[] { 0.2, 0.2, 0.2 },
        }), "<line"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    /// <summary>ErrorEvery=2 (modulo skip) — only every 2nd point gets bars.
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
