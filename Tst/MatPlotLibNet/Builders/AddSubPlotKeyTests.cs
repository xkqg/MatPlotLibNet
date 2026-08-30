// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>Axis sharing resolves by KEY, and until now only the legacy <c>(rows, cols, index)</c> overload
/// could set one — a grid-position subplot could name a share target but never BE one. The
/// <see cref="GridPosition"/> overload takes the same optional key.</summary>
public class AddSubPlotKeyTests
{
    [Fact]
    public void AGridPositionSubplot_CanBeAShareTarget()
    {
        var figure = Plt.Create()
            .WithGridSpec(1, 2)
            .AddSubPlot(GridPosition.Single(0, 0), ax => ax.Plot([0.0, 1.0], [0.0, 1.0]), key: "left")
            .AddSubPlot(GridPosition.Single(0, 1), ax => ax.Plot([0.0, 1.0], [0.0, 1.0]).ShareY("left"))
            .Build();

        Assert.Same(figure.SubPlots[0], figure.SubPlots[1].ShareYWith);
    }
}
