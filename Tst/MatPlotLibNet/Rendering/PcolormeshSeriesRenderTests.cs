// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies SVG output of <see cref="PcolormeshSeries"/> rendering.</summary>
public class PcolormeshSeriesRenderTests
{
    private static readonly double[] X = [0.0, 1.0, 2.0, 3.0];
    private static readonly double[] Y = [0.0, 1.0, 2.0];
    private static readonly double[,] C = { { 1.0, 2.0, 3.0 }, { 4.0, 5.0, 6.0 } };

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("Pcolormesh_SvgContainsRectangles", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Pcolormesh(X, Y, C))
            .ToSvg(), "<rect"),
        new DrawnToken("Pcolormesh_SingleCell_RendersWithoutError", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Pcolormesh([0.0, 1.0], [0.0, 1.0], new double[,] { { 5.0 } }))
            .ToSvg(), "<rect"),
        // A range of no width, opened one wide (ColorMapping.Opened), drawn by the renderer.
        new DrawnToken("Pcolormesh_AllEqualValues_DegenerateGuard", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Pcolormesh([0.0, 1.0, 2.0], [0.0, 1.0, 2.0],
                new double[,] { { 5.0, 5.0 }, { 5.0, 5.0 } }))
            .ToSvg(), "<rect"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    // Branch coverage — exercise each early-return guard in PcolormeshSeriesRenderer. The verb refuses edges that do not
    // match the cells, so each of these builds the series directly: the renderer's own guard is what is characterised.

    // ── the cells reach the spines ─────────────────────────────────────────────
    //
    // A heatmap fills its plot rectangle exactly; a pcolormesh, which is the same picture over coordinates the
    // caller chooses, did not. The axes added their usual 5 % breathing room around the data, so every mesh was
    // drawn inside a gutter with the spines standing off the cells. Measured against matplotlib's own output on
    // the sin/cos fixture before this changed: RMS 57.7 of 255 and SSIM 0.51 on the classic theme.

    [Fact]
    public void TheCells_FillThePlotAreaFromSpineToSpine()
    {
        string svg = Plt.Create().WithSize(640, 480)
            .AddSubPlot(1, 1, 1, ax => ax.Pcolormesh(X, Y, C))
            .ToSvg();

        var area = svg.PlotArea();
        var cells = svg.Elements("rect")
            .Select(rect => (X: rect.Number("x"), Y: rect.Number("y"), W: rect.Number("width"), H: rect.Number("height")))
            .Where(r => r.W < area.Right - area.X - 0.5)
            .ToArray();

        Assert.Equal(6, cells.Length);
        Assert.Equal(area.X, cells.Min(c => c.X), 1);
        Assert.Equal(area.Right, cells.Max(c => c.X + c.W), 1);
        Assert.Equal(area.Y, cells.Min(c => c.Y), 1);
        Assert.Equal(area.Bottom, cells.Max(c => c.Y + c.H), 1);
    }

    [Fact]
    public void TheAxisRange_IsTheEdgesTheCallerGave()
    {
        var series = new PcolormeshSeries(X, Y, C);

        var range = series.ComputeDataRange(new TestAxesContext());

        Assert.Equal(0.0, range.StickyXMin);
        Assert.Equal(3.0, range.StickyXMax);
        Assert.Equal(0.0, range.StickyYMin);
        Assert.Equal(2.0, range.StickyYMax);
    }

    [Fact]
    public void AMeshWithNoEdges_SticksToNothing()
    {
        var series = new PcolormeshSeries(Array.Empty<double>(), Array.Empty<double>(), new double[0, 0]);

        var range = series.ComputeDataRange(new TestAxesContext());

        Assert.Null(range.StickyXMin);
        Assert.Null(range.StickyYMax);
    }
}
