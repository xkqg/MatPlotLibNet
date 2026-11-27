// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.FluentApi;

/// <summary>The subplots of a <see cref="FigureBuilder"/>: where each stands in the grid, and the scales they share by key.</summary>
public class FigureBuilderTests
{
    /// <summary>Verifies that AddSubPlot creates multiple axes in the figure.</summary>
    [Fact]
    public void AddSubPlot_CreatesMultipleAxes()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 2, 1, ax => ax.Plot([1.0], [2.0]))
            .AddSubPlot(1, 2, 2, ax => ax.Scatter([3.0], [4.0]))
            .Build();
        Assert.Equal(2, figure.SubPlots.Count);
    }

    /// <summary>Verifies that AddSubPlot applies the configure callback to the axes.</summary>
    [Fact]
    public void AddSubPlot_ConfigureCallback_IsApplied()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.WithTitle("Sub 1")
                  .SetXLabel("X")
                  .SetYLabel("Y")
                  .Plot([1.0], [2.0]);
            })
            .Build();

        var axes = figure.SubPlots[0];
        Assert.Equal("Sub 1", axes.Title);
        Assert.Equal("X", axes.XAxis.Label);
        Assert.Equal("Y", axes.YAxis.Label);
        Assert.Single(axes.Series);
    }

    /// <summary>Verifies that AddSubPlot with GridPosition creates correct Axes.</summary>
    [Fact]
    public void AddSubPlot_WithGridPosition_CreatesCorrectAxes()
    {
        var figure = Plt.Create()
            .WithGridSpec(2, 2)
            .AddSubPlot(GridPosition.Single(0, 0), ax => ax.Plot([1.0], [2.0]))
            .AddSubPlot(GridPosition.Single(0, 1), ax => ax.Scatter([3.0], [4.0]))
            .Build();

        Assert.Equal(2, figure.SubPlots.Count);
        Assert.NotNull(figure.SubPlots[0].GridPosition);
        Assert.Equal(0, figure.SubPlots[0].GridPosition!.Value.ColStart);
        Assert.Equal(1, figure.SubPlots[1].GridPosition!.Value.ColStart);
    }

    /// <summary>Verifies that AddSubPlot with span coordinates creates spanning Axes.</summary>
    [Fact]
    public void AddSubPlot_WithSpanCoordinates_CreatesCorrectAxes()
    {
        var figure = Plt.Create()
            .WithGridSpec(3, 3)
            .AddSubPlot(new GridPosition(0, 1, 0, 3), ax => ax.Plot([1.0], [2.0]))
            .Build();

        var pos = figure.SubPlots[0].GridPosition!.Value;
        Assert.Equal(0, pos.RowStart);
        Assert.Equal(1, pos.RowEnd);
        Assert.Equal(0, pos.ColStart);
        Assert.Equal(3, pos.ColEnd);
    }

    // --- Shared axes builder tests ---

    /// <summary>A scale shared by the key of a subplot resolves to that subplot at Build time, on either axis.</summary>
    [Fact]
    public void AddSubPlot_WithAShareKey_ResolvesAtBuild_OnEitherAxis()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 2, 1, ax => ax.Plot([1.0], [2.0]), key: "main")
            .AddSubPlot(1, 2, 2, ax => ax.ShareX("main").ShareY("main").Plot([3.0], [4.0]))
            .Build();

        Assert.Same(figure.SubPlots[0], figure.SubPlots[1].ShareXWith);
        Assert.Same(figure.SubPlots[0], figure.SubPlots[1].ShareYWith);
    }

    /// <summary>A scale shared with a key that no subplot carries is shared with nothing, as when the key names no subplot of
    /// several keyed ones.</summary>
    [Fact]
    public void AddSubPlot_WithAShareKeyNoSubplotCarries_SharesNothing()
    {
        var unkeyed = Plt.Create()
            .AddSubPlot(1, 2, 1, ax => ax.Plot([1.0], [2.0]))
            .AddSubPlot(1, 2, 2, ax => ax.ShareX("absent").ShareY("absent").Plot([3.0], [4.0]))
            .Build();
        var keyed = Plt.Create()
            .AddSubPlot(1, 2, 1, ax => ax.Plot([1.0], [2.0]), key: "main")
            .AddSubPlot(1, 2, 2, ax => ax.ShareX("absent").ShareY("absent").Plot([3.0], [4.0]))
            .Build();

        Assert.Null(unkeyed.SubPlots[1].ShareXWith);
        Assert.Null(unkeyed.SubPlots[1].ShareYWith);
        Assert.Null(keyed.SubPlots[1].ShareXWith);
        Assert.Null(keyed.SubPlots[1].ShareYWith);
    }
}
