// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>Phase L.1b (v1.7.2, 2026-04-21) — TDD red phase for
/// <see cref="PolarTransformRenderer{TSeries}"/>.
/// PrepareTransform is a pure static helper that removes the duplicate rMax setup
/// from PolarLineSeriesRenderer and PolarScatterSeriesRenderer.</summary>
public class PolarTransformRendererTests
{
    private static readonly Rect Bounds = new(0, 0, 200, 200);
    // cx = 100, cy = 100, maxRadius = min(200,200)/2 * 0.85 = 85

    [Fact]
    public void PrepareTransform_NonEmptyR_RMaxIsArrayMax()
    {
        // r=[1,2,5] → rMax=5. PolarToPixel(5, 0) should map exactly to the outer edge.
        double[] r = [1, 2, 5];
        var transform = PolarTransformRenderer<PolarLineSeries>.PrepareTransform(r, Bounds);

        var edge = transform.PolarToPixel(5, 0);
        Assert.Equal(transform.CenterX + transform.MaxRadius, edge.X, 1e-6);
    }

    [Fact]
    public void PrepareTransform_EmptyR_DefaultsRMaxToOne()
    {
        // Empty r → rMax defaults to 1. PolarToPixel(1, 0) should map to the outer edge.
        var transform = PolarTransformRenderer<PolarLineSeries>.PrepareTransform([], Bounds);

        var edge = transform.PolarToPixel(1, 0);
        Assert.Equal(transform.CenterX + transform.MaxRadius, edge.X, 1e-6);
    }
}
