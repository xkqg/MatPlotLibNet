// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.EnumContract;

/// <summary>
/// Phase N.2 — <see cref="TickDirection"/> (In / Out / InOut) must produce
/// distinct SVG output per value. The renderer draws tick marks at different
/// y-offsets relative to the axis spine based on the direction; byte-distinct
/// SVG is the minimal proof that the branch is honoured.
/// </summary>
public class TickDirectionContractTests
{
    [Fact]
    public void EveryTickDirection_ProducesDistinctSvg()
    {
        EnumOutputContract.EveryValueRendersDistinctOutput<TickDirection>(RenderWithDirection);
    }

    private static string RenderWithDirection(TickDirection dir)
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2.0, 3.0], [1.0, 2.0, 3.0]))
            .Build();
        var axes = figure.SubPlots[0];
        axes.XAxis.MajorTicks = axes.XAxis.MajorTicks with { Direction = dir };
        axes.YAxis.MajorTicks = axes.YAxis.MajorTicks with { Direction = dir };
        return new MatPlotLibNet.Transforms.SvgTransform().Render(figure);
    }
}
