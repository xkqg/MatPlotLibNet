// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Models;

/// <summary>Verifies <see cref="Axes.TwinY"/> — secondary X-axis on the top edge.</summary>
public class TwinYTests
{
    private static double[] X => [1.0, 2.0, 3.0];
    private static double[] Y => [4.0, 5.0, 6.0];

    [Fact]
    public void TwinY_CreatesSecondaryXAxis()
    {
        var ax = new Axes();
        ax.TwinY();
        Assert.NotNull(ax.SecondaryXAxis);
    }

    [Fact]
    public void TwinY_CalledTwice_SameAxisInstance()
    {
        var ax = new Axes();
        ax.TwinY();
        var first = ax.SecondaryXAxis;
        ax.TwinY();
        Assert.Same(first, ax.SecondaryXAxis);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PlotXSecondaryAndScatterXSecondary_ImplicitlyCallTwinY(bool plot)
    {
        var ax = new Axes();
        if (plot) ax.PlotXSecondary(X, Y);
        else ax.ScatterXSecondary(X, Y);
        Assert.NotNull(ax.SecondaryXAxis);
    }

}
