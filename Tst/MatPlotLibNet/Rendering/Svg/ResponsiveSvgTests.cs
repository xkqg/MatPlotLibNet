// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Rendering.Svg;

/// <summary>
/// L.1 / L.2 — Responsive SVG by default.
///
/// <para>Rationale: without any CSS helper, the SVG's fixed pixel <c>width</c>
/// and <c>height</c> attributes pin the element to its natural size even when
/// the browser window or parent container is much wider. By emitting an inline
/// <c>style="max-width:100%;height:auto"</c> we keep the <c>naturalWidth</c> /
/// <c>naturalHeight</c> DOM properties intact (needed for client-side PNG
/// export in the playground) while letting CSS govern the rendered box —
/// the chart fluidly scales to its container while the <c>viewBox</c>
/// preserves aspect ratio.</para>
///
/// <para>Opt-out via <c>FigureBuilder.WithResponsiveSvg(false)</c> retains
/// byte-identical pre-Phase-L output for callers that pin the raw SVG string
/// (e.g., pixel-diff fixture tests).</para>
/// </summary>
public class ResponsiveSvgTests
{
    [Fact]
    public void Default_EmitsInlineStyleWithMaxWidthAndAutoHeight()
    {
        string svg = Plt.Create().Plot([0.0, 1.0], [0.0, 1.0]).ToSvg();
        Assert.Contains("style=\"max-width:100%;height:auto\"", svg);
    }

    [Fact]
    public void Default_StillEmitsPixelWidthAndHeight_ForNaturalWidth()
    {
        // Regression guard for the playground's client-side PNG export path
        // (reads img.naturalWidth / naturalHeight). Dropping these attributes
        // would make the canvas rasterisation yield a 0×0 image.
        string svg = Plt.Create().WithSize(640, 480).Plot([0.0, 1.0], [0.0, 1.0]).ToSvg();
        Assert.Contains("width=\"640\"", svg);
        Assert.Contains("height=\"480\"", svg);
    }

    [Theory]
    [InlineData(600, 500)]
    [InlineData(800, 600)]
    [InlineData(1280, 720)]
    public void ViewBox_Unchanged_MatchesWithSize(int w, int h)
    {
        string svg = Plt.Create().WithSize(w, h).Plot([0.0, 1.0], [0.0, 1.0]).ToSvg();
        Assert.Contains($"viewBox=\"0 0 {w} {h}\"", svg);
    }

    [Fact]
    public void WithResponsiveSvg_False_OmitsInlineStyle()
    {
        string svg = Plt.Create()
            .WithResponsiveSvg(false)
            .Plot([0.0, 1.0], [0.0, 1.0])
            .ToSvg();
        Assert.DoesNotContain("style=\"max-width:100%;height:auto\"", svg);
    }

    [Fact]
    public void WithResponsiveSvg_True_AddsInlineStyle()
    {
        // Explicit opt-in path (equivalent to the default).
        string svg = Plt.Create()
            .WithResponsiveSvg(true)
            .Plot([0.0, 1.0], [0.0, 1.0])
            .ToSvg();
        Assert.Contains("style=\"max-width:100%;height:auto\"", svg);
    }
}
