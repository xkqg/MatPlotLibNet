// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.EnumContract;

public sealed class AxisScaleOutputContractTests : EnumOutputContractTests<AxisScale>
{
    protected override Func<AxisScale, string> RenderDelegate => scale =>
    {
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
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot(x, y)).Build();
        figure.SubPlots[0].XAxis.Scale = scale;
        figure.SubPlots[0].YAxis.Scale = scale;
        return new MatPlotLibNet.Transforms.SvgTransform().Render(figure);
    };

    [Fact]
    public void AxisScale_Logit_DrawsOtherwiseThanLinear()
    {
        string linear = RenderDelegate(AxisScale.Linear);
        string logit  = RenderDelegate(AxisScale.Logit);
        Assert.NotEqual(linear, logit);
    }
}
