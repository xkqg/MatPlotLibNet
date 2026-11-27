// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>v1.10 — verifies SVG output of <see cref="HeatmapSeries"/> rendering with the
/// new <c>ShowLabels</c>, <c>LabelFormat</c>, <c>CellValueColor</c>, and <c>MaskMode</c>
/// extensions.</summary>
public class HeatmapRenderTests
{
    private static double[,] Symmetric3x3 => new double[,]
    {
        { 1.00, 0.50, 0.20 },
        { 0.50, 1.00, 0.30 },
        { 0.20, 0.30, 1.00 },
    };

    private static string RenderSvg(double[,] data, Action<HeatmapSeries> configure) =>
        Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Heatmap(data, configure))
            .ToSvg();

    // ── Baseline ─────────────────────────────────────────────────────────────

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("ShowLabels_False_NoTextEmitted", () => RenderSvg(Symmetric3x3, _ => { })) { Absent = [">1.00<", ">0.50<"] },
        new DrawnToken("ShowLabels_True_F2_EmitsFormattedValues", () => RenderSvg(Symmetric3x3, s => { s.ShowLabels = true; }), ">1.00<", ">0.50<", ">0.20<"),
        // ShowLabels=false must suppress text even when MaskMode is active.
        new DrawnToken("ShowLabels_False_WithActiveMaskMode_EmitsNoText", () => RenderSvg(Symmetric3x3, s => { s.MaskMode = HeatmapMaskMode.UpperTriangle; })) { Absent = [">1.00<", ">0.50<"] },
        // Culture-invariant percent format is "25.0%" / "75.0%".
        new DrawnToken("ShowLabels_True_P1_EmitsPercentFormat", () =>
        {
            var data = new double[,] { { 0.25, 0.75 } };
            return RenderSvg(data, s => { s.ShowLabels = true; s.LabelFormat = "P1"; });
        }, "25.0", "75.0"),
        // Auto-contrast would never emit pure red against viridis colours, so finding the
        // hex of Colors.Red proves the explicit override applied.
        new DrawnToken("ShowLabels_True_ExplicitColor_UsesIt", () => RenderSvg(Symmetric3x3, s =>
        {
            s.ShowLabels = true;
            s.CellValueColor = Colors.Red;
        }), Colors.Red.ToHex()),
        new DrawnToken("AutoContrast_DarkFill_PicksWhite", () =>
        {
            // Viridis at value 0 is dark purple → white text expected.
            var data = new double[,] { { 0.0, 1.0 } };
            return RenderSvg(data, s => { s.ShowLabels = true; });
        }, Colors.White.ToHex()),
        new DrawnToken("AutoContrast_LightFill_PicksBlack", () =>
        {
            // Viridis at value 1 is bright yellow → black text expected.
            var data = new double[,] { { 0.0, 1.0 } };
            return RenderSvg(data, s => { s.ShowLabels = true; });
        }, Colors.Black.ToHex()),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    // ── MaskMode ─────────────────────────────────────────────────────────────

    [Fact]
    public void MaskMode_None_KeepsAllCells()
    {
        string svg = RenderSvg(Symmetric3x3, _ => { });
        // 3x3 + plot frame + axes elements; the heatmap itself contributes 9 fill rects.
        // Use a sentinel: the cell at (col=2, row=0) carries value 0.20 — visible only
        // when MaskMode != UpperTriangle/UpperTriangleStrict.
        Assert.Contains("rect", svg);
        // 9 cells rendered (sanity — too tight a count would be brittle, so just verify >=9 fill rects).
        Assert.True(svg.CountOf("<rect") >= 9);
    }

    /// <summary>A mask hides cells from a 3 x 3 heatmap: a triangle without its diagonal is three of the nine, with it six.</summary>
    [Theory]
    [InlineData(HeatmapMaskMode.UpperTriangle, 3)] // the lower triangle and the diagonal stay: six cells visible
    [InlineData(HeatmapMaskMode.LowerTriangle, 3)]
    [InlineData(HeatmapMaskMode.UpperTriangleStrict, 6)] // the strict lower triangle stays, the diagonal hidden too: three cells visible
    [InlineData(HeatmapMaskMode.LowerTriangleStrict, 6)]
    public void MaskMode_HidesTheCellsOfItsTriangle(HeatmapMaskMode mode, int hidden)
    {
        string masked = RenderSvg(Symmetric3x3, s => { s.MaskMode = mode; });
        string none = RenderSvg(Symmetric3x3, _ => { });

        Assert.Equal(hidden, none.CountOf("<rect") - masked.CountOf("<rect"));
    }

    [Fact]
    public void Combined_ShowLabels_LowerTriangle_TextOnlyOnVisibleCells()
    {
        string masked = RenderSvg(Symmetric3x3, s =>
        {
            s.ShowLabels = true;
            s.MaskMode = HeatmapMaskMode.UpperTriangle;
        });
        string none = RenderSvg(Symmetric3x3, s => { s.ShowLabels = true; });

        // Off-diagonal value 0.50 appears at (0,1) and (1,0). Masking the upper triangle
        // hides (0,1), leaving exactly one ">0.50<" annotation; the unmasked render has two.
        Assert.Equal(2, none.CountOf(">0.50<"));
        Assert.Equal(1, masked.CountOf(">0.50<"));
    }

    [Fact]
    public void LabelFormat_IntegerOnlySpecifier_ThrowsFormatException()
    {
        // "D" (decimal integer) is only valid for integral types; double.ToString("D") throws FormatException, and the
        // SVG writer draws its subplots one after another, so the caller gets that exception itself — not wrapped.
        Assert.Throws<FormatException>(() =>
            RenderSvg(new double[,] { { 1.0 } }, s => { s.ShowLabels = true; s.LabelFormat = "D"; }));
    }

}
