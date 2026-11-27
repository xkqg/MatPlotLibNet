// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies SVG output of <see cref="ContourfSeries"/> rendering.</summary>
public class ContourfRenderTests
{
    // 3x3 grid with clear gradient for visible contour bands
    private static readonly double[] X = [0.0, 1.0, 2.0];
    private static readonly double[] Y = [0.0, 1.0, 2.0];
    private static readonly double[,] Z = { { 0, 1, 2 }, { 1, 2, 3 }, { 2, 3, 4 } };

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        // SVG output contains polygon elements (filled bands).
        new DrawnToken("Contourf_SvgContainsTheBandsAsPaths", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Contourf(X, Y, Z))
            .ToSvg(), "<path"),
        // ShowLines=true adds polyline iso-line overlays.
        new DrawnToken("Contourf_ShowLines_AddsPolylines", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Contourf(X, Y, Z, s => s.ShowLines = true))
            .ToSvg(), "<polyline"),
        // ShowLines=false omits polyline iso-lines but polygons still present.
        new DrawnToken("Contourf_ShowLinesFalse_BandsPresent", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Contourf(X, Y, Z, s => s.ShowLines = false))
            .ToSvg(), "<path"),
        // Alpha < 1 produces an opacity attribute in the SVG.
        new DrawnToken("Contourf_Alpha_ProducesOpacityAttribute", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Contourf(X, Y, Z, s => s.Alpha = 0.5))
            .ToSvg(), "opacity"),
        // A filled contour can hatch its bands — and a hatch array shorter than the band count leaves the remaining bands plain rather
        // than throwing.
        new DrawnToken("ContourFill_HatchesTheBandsItWasGiven_AndLeavesTheRestPlain", () =>
        {
            double[] x = [0, 1, 2];
            double[] y = [0, 1, 2];
            double[,] z = { { 0, 1, 2 }, { 1, 2, 3 }, { 2, 3, 4 } };
            return Plt.Create()
                .AddSubPlot(1, 1, 1, ax => ax.Contourf(x, y, z, configure: s =>
                    s.Hatches = [HatchPattern.ForwardDiagonal]))    // one hatch, many bands
                .ToSvg();
        }, "<pattern"),
        // Without hatches a filled contour paints flat bands — no pattern machinery at all.
        new DrawnToken("ContourFill_WithoutHatches_PaintsFlatBands", () =>
        {
            double[] x = [0, 1, 2];
            double[] y = [0, 1, 2];
            double[,] z = { { 0, 1, 2 }, { 1, 2, 3 }, { 2, 3, 4 } };
            return Plt.Create()
                .AddSubPlot(1, 1, 1, ax => ax.Contourf(x, y, z))
                .ToSvg();
        }) { Absent = ["<pattern"] },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

}
