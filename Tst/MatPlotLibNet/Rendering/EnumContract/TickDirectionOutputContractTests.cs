// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.EnumContract;

public sealed class TickDirectionOutputContractTests : EnumOutputContractTests<TickDirection>
{
    protected override Func<TickDirection, string> RenderDelegate => dir =>
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2.0, 3.0], [1.0, 2.0, 3.0]))
            .Build();
        var axes = figure.SubPlots[0];
        axes.XAxis.MajorTicks = axes.XAxis.MajorTicks with { Direction = dir };
        axes.YAxis.MajorTicks = axes.YAxis.MajorTicks with { Direction = dir };
        return new MatPlotLibNet.Transforms.SvgTransform().Render(figure);
    };
}
