// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>Phase X.4 follow-up (v1.7.2, 2026-04-19) — drives every StepPosition arm
/// (Post / Pre / Mid) in <see cref="MatPlotLibNet.Rendering.SeriesRenderers.StepSeriesRenderer"/>.
/// Pre-X the renderer was at 63.6%L / 35.7%B because only the default Post arm
/// was exercised; Pre + Mid (lines 33-44) were untouched.</summary>
public class StepSeriesRendererTests
{
    private static string Render(StepSeries s) =>
        Plt.Create()
            .WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(s))
            .Build()
            .ToSvg();

    [Theory]
    [InlineData(StepPosition.Post)]
    [InlineData(StepPosition.Pre)]
    [InlineData(StepPosition.Mid)]
    public void Render_EachStepPosition_ProducesPolyline(StepPosition pos)
    {
        var s = new StepSeries([0.0, 1, 2, 3, 4], [1.0, 2, 1, 3, 2]) { StepPosition = pos };
        var svg = Render(s);
        Assert.Contains("<polyline", svg);
    }
}
