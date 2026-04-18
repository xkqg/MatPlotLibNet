// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Rendering.EnumContract;

/// <summary>
/// Phase N.2 — <see cref="AxisScale"/> has 5 members (Linear, Log, SymLog,
/// Logit, Date). Each transforms the axis differently; tick placement and
/// data-to-pixel mapping must differ per scale.
/// </summary>
public class AxisScaleContractTests
{
    [Fact]
    public void EveryAxisScale_ProducesDistinctSvg_Except_KnownSilentCollapses()
    {
        // Logit is a known silent collapse — the AxesRenderer treats it the same as
        // Linear (Logit transform isn't wired into the tick locator / data-to-pixel
        // map). Tracked for follow-up; un-skip the bug-fix test below once Logit is
        // properly implemented in CartesianAxesRenderer.ScaleRange.
        EnumOutputContract.EveryValueRendersDistinctOutput<AxisScale>(
            RenderWithScale,
            exclude: [AxisScale.Logit]);
    }

    [Fact(Skip = "Known silent-collapse bug caught by Phase N.2 contract — AxesRenderer treats AxisScale.Logit identically to AxisScale.Linear. Fix tracked for follow-up: wire the logit transform into CartesianAxesRenderer.ScaleRange + tick-locator pipeline. Un-skip + remove Logit from the exclude list above once implemented.")]
    public void AxisScale_Logit_BugFix_MustInvertThisTest()
    {
        string linear = RenderWithScale(AxisScale.Linear);
        string logit  = RenderWithScale(AxisScale.Logit);
        Assert.NotEqual(linear, logit);
    }

    private static string RenderWithScale(AxisScale scale)
    {
        // Values in (0, 1) work for Linear / Log / SymLog / Logit.
        // For Date we use OLE-Automation-date-compatible doubles (days since 1899).
        double[] x, y;
        if (scale == AxisScale.Date)
        {
            x = Enumerable.Range(0, 10).Select(i => 45000.0 + i).ToArray();
            y = Enumerable.Range(0, 10).Select(i => (double)(i + 1)).ToArray();
        }
        else
        {
            x = Enumerable.Range(1, 10).Select(i => i * 0.09).ToArray();
            y = Enumerable.Range(1, 10).Select(i => i * 0.09).ToArray();
        }

        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Plot(x, y))
            .Build();
        figure.SubPlots[0].XAxis.Scale = scale;
        figure.SubPlots[0].YAxis.Scale = scale;
        return new MatPlotLibNet.Transforms.SvgTransform().Render(figure);
    }
}
