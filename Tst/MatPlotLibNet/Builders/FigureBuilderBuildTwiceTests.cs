// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>A builder built twice gives two figures that share nothing either can change, the default axes (the one the
/// figure-level verbs draw on) included.</summary>
public class FigureBuilderBuildTwiceTests
{
    [Fact]
    public void TheDefaultAxes_IsAnAxesOfItsOwnInEachBuild()
    {
        var builder = Plt.Create().Plot([1.0, 2.0], [1.0, 2.0]);

        var first = builder.Build();
        var second = builder.Build();

        Assert.NotSame(first.SubPlots[0], second.SubPlots[0]);
        Assert.NotSame(first.SubPlots[0].Series[0], second.SubPlots[0].Series[0]);
    }

    [Fact]
    public void AChangeToOneBuild_DoesNotReachTheOther()
    {
        var builder = Plt.Create().Plot([1.0, 2.0], [1.0, 2.0]).AxHLine(1.5);

        var first = builder.Build();
        first.SubPlots[0].XAxis.Min = 100;
        first.SubPlots[0].AddSeries(new LineSeries([0.0], [0.0]));
        var second = builder.Build();

        Assert.Null(second.SubPlots[0].XAxis.Min);
        Assert.Single(second.SubPlots[0].Series);
        Assert.Single(second.SubPlots[0].ReferenceLines);
    }

    [Fact]
    public void EveryVerbOnTheDefaultAxes_ReachesEachBuild()
    {
        var builder = Plt.Create()
            .Plot([DateTime.UnixEpoch, DateTime.UnixEpoch.AddDays(1)], [1.0, 2.0])
            .WithCamera(10, 20)
            .WithLighting(1, 1, 1)
            .WithLegendValues()
            .Annotate("a", 1, 1)
            .AxVSpan(0, 1);

        foreach (var figure in new[] { builder.Build(), builder.Build() })
        {
            var axes = figure.SubPlots[0];
            Assert.Single(axes.Series);
            Assert.Equal(10, axes.Elevation);
            Assert.Equal(20, axes.Azimuth);
            Assert.NotNull(axes.LightSource);
            Assert.True(axes.Legend.LegendValues);
            Assert.Single(axes.Annotations);
            Assert.Single(axes.Spans);
        }
    }

    [Fact]
    public void ADefaultAxesWithoutSeries_IsLeftOutBesideSubplots()
    {
        // A figure-level setting such as the camera touches the default axes; beside real subplots it must not add an
        // empty one, in every build.
        var builder = Plt.Create().WithCamera(10, 20).AddSubPlot(1, 1, 1, ax => ax.Plot([1.0], [1.0]));

        Assert.Single(builder.Build().SubPlots);
        Assert.Single(builder.Build().SubPlots);
    }
}
