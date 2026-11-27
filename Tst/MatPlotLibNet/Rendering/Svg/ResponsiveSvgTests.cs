// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Rendering.Svg;

/// <summary>
/// L.1 / L.2 + Phase P fix — Responsive SVG by default.
///
/// <para>Rationale: without any CSS helper, the SVG's fixed pixel <c>width</c>
/// and <c>height</c> attributes pin the element to its natural size. Phase L.2
/// first used <c>max-width:100%;height:auto</c> — that scaled only DOWN,
/// which meant charts in a WIDER viewport stayed at their intrinsic pixel width
/// with left/right whitespace. Phase P (2026-04-18) corrected this to
/// <c>width:100%;height:auto</c> so the chart fills the container in BOTH
/// directions. The pixel <c>width</c>/<c>height</c> HTML attributes are
/// preserved for <c>naturalWidth</c> (PNG export reads the attribute, not CSS).</para>
///
/// <para>Opt-out via <c>FigureBuilder.WithResponsiveSvg(false)</c> retains
/// byte-identical pre-Phase-L output for callers that pin the raw SVG string
/// (e.g., pixel-diff fixture tests).</para>
/// </summary>
public class ResponsiveSvgTests
{
    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        // Phase P: width:100% (not max-width:100%) so the chart also extends
        // to fill a wider viewport — previously it stayed at intrinsic size
        // with whitespace when the container was wider than the natural width.
        new DrawnToken("Default_EmitsInlineStyle_ScalesInBothDirections", () => Plt.Create().Plot([0.0, 1.0], [0.0, 1.0]).ToSvg(), "style=\"width:100%;height:auto\"") { Absent = ["max-width:100%"] },
        // Regression guard for the playground's client-side PNG export path
        // (reads img.naturalWidth / naturalHeight). Dropping these attributes
        // would make the canvas rasterisation yield a 0×0 image.
        new DrawnToken("Default_StillEmitsPixelWidthAndHeight_ForNaturalWidth", () => Plt.Create().WithSize(640, 480).Plot([0.0, 1.0], [0.0, 1.0]).ToSvg(), "width=\"640\"", "height=\"480\""),
        new DrawnToken("WithResponsiveSvg_False_OmitsInlineStyle", () => Plt.Create()
            .WithResponsiveSvg(false)
            .Plot([0.0, 1.0], [0.0, 1.0])
            .ToSvg()) { Absent = ["style=\"width:100%;height:auto\""] },
        // Explicit opt-in path (equivalent to the default).
        new DrawnToken("WithResponsiveSvg_True_AddsInlineStyle", () => Plt.Create()
            .WithResponsiveSvg(true)
            .Plot([0.0, 1.0], [0.0, 1.0])
            .ToSvg(), "style=\"width:100%;height:auto\""),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    [Theory]
    [InlineData(600, 500)]
    [InlineData(800, 600)]
    [InlineData(1280, 720)]
    public void ViewBox_Unchanged_MatchesWithSize(int w, int h)
    {
        string svg = Plt.Create().WithSize(w, h).Plot([0.0, 1.0], [0.0, 1.0]).ToSvg();
        Assert.Contains($"viewBox=\"0 0 {w} {h}\"", svg);
    }
}
