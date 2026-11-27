// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Numerics;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests;

/// <summary>The axes and series the tests draw, and the transform they check an FFT against.</summary>
public sealed class SampleSeriesTests
{
    [Fact]
    public void ASurfaceAxes_IsThreeDimensional_HoldsOneSurface_AndTheCallerMayChangeIt()
    {
        var axes = SampleSeries.SurfaceAxes(s => s.ShowWireframe = true);

        Assert.Equal(CoordinateSystem.ThreeD, axes.CoordinateSystem);
        Assert.True(Assert.IsType<SurfaceSeries>(Assert.Single(axes.Series)).ShowWireframe);
    }

    [Fact]
    public void ATrisurfAxes_HoldsOneTriangulatedSurface_AndTheCallerMayChangeIt()
    {
        var axes = SampleSeries.TrisurfAxes(s => s.Alpha = 0.5);

        Assert.Equal(CoordinateSystem.ThreeD, axes.CoordinateSystem);
        Assert.Equal(0.5, Assert.IsType<Trisurf3DSeries>(Assert.Single(axes.Series)).Alpha);
    }

    [Fact]
    public void TheStreamAxis_RunsFromMinusThreeToExactlyThree()
    {
        var axis = SampleSeries.StreamAxis();

        Assert.Equal(40, axis.Length);
        Assert.Equal(-3.0, axis[0]);
        Assert.Equal(3.0, axis[^1]);
    }

    [Fact]
    public void TheRotationalField_HoldsItsDensity_OverTheStreamAxis()
    {
        var field = SampleSeries.RotationalField(2.0);

        Assert.Equal(2.0, field.Density);
        Assert.Equal(1.0, SampleSeries.RotationalField().Density);
    }

    [Fact]
    public void TheDirectDft_OfAnImpulse_IsFlat_AndOfAConstant_IsOneSpike()
    {
        Assert.All(new double[] { 1, 0, 0, 0 }.DirectDft(), c => Assert.Equal(1.0, c.Real, 12));
        var spike = new double[] { 2, 2, 2, 2 }.DirectDft();
        Assert.Equal(8.0, spike[0].Real, 12);
        Assert.All(spike.Skip(1), c => Assert.Equal(0.0, c.Magnitude, 12));
        Assert.Equal(8.0, new Complex[] { 2, 2, 2, 2 }.DirectDft()[0].Real, 12);
    }

    /// <summary>A kind is drawn by the one row the sample switch has for it, under the registry's discriminator: the series it adds
    /// writes that discriminator, and a name no row has is refused. The rows themselves are held byte for byte by the golden corpus.</summary>
    [Fact]
    public void ASampleSeries_IsTheKindItNames_AndANameNoRowHasIsRefused()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.AddSampleSeries("signal-xy")).Build();

        Assert.Equal("signal-xy", Assert.Single(figure.SubPlots[0].Series).ToSeriesDto().Type);
        Assert.Throws<ArgumentOutOfRangeException>(() => Plt.Create().AddSubPlot(1, 1, 1, ax => ax.AddSampleSeries("no-such-kind")).Build());
    }
}
