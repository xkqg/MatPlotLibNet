// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.


namespace MatPlotLibNet.Tests.Builders;

public sealed class TightMarginsTests
{
    [Fact]
    public void WithTightMargins_SetsBothMarginsToZero()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0])
                .WithTightMargins())
            .Build();

        Assert.Equal(0.0, fig.SubPlots[0].XAxis.Margin);
        Assert.Equal(0.0, fig.SubPlots[0].YAxis.Margin);
    }

}
