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
    public void SecondaryXAxis_DefaultsToNull()
    {
        var ax = new Axes();
        Assert.Null(ax.SecondaryXAxis);
    }

    [Fact]
    public void XSecondarySeries_DefaultsToEmpty()
    {
        var ax = new Axes();
        Assert.Empty(ax.XSecondarySeries);
    }

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

    [Fact]
    public void PlotXSecondary_AddsToXSecondarySeries()
    {
        var ax = new Axes();
        ax.PlotXSecondary(X, Y);
        Assert.Single(ax.XSecondarySeries);
    }

    [Fact]
    public void PlotXSecondary_ImplicitlyCallsTwinY()
    {
        var ax = new Axes();
        ax.PlotXSecondary(X, Y);
        Assert.NotNull(ax.SecondaryXAxis);
    }

    [Fact]
    public void ScatterXSecondary_AddsToXSecondarySeries()
    {
        var ax = new Axes();
        ax.ScatterXSecondary(X, Y);
        Assert.Single(ax.XSecondarySeries);
    }

    [Fact]
    public void ScatterXSecondary_ImplicitlyCallsTwinY()
    {
        var ax = new Axes();
        ax.ScatterXSecondary(X, Y);
        Assert.NotNull(ax.SecondaryXAxis);
    }

    [Fact]
    public void AxesBuilder_WithSecondaryXAxis_ProducesSvg()
    {
        var svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .WithSecondaryXAxis(b => b
                    .SetXLabel("Secondary X")
                    .PlotXSecondary(X, Y)))
            .Build()
            .ToSvg();

        Assert.Contains("<svg", svg);
    }
}
