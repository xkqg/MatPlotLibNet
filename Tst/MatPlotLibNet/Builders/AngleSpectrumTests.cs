// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The <c>AngleSpectrum</c> verb (matplotlib's <c>angle_spectrum</c>): the angle of each frequency in a whole signal,
/// WRAPPED into (−π, π] (numpy's <c>angle</c>), as the transform gives it. Drawn as ONE line of (frequency, radians). The numbers
/// marked "matplotlib" were printed by matplotlib 3.11.2.</summary>
public class AngleSpectrumTests
{
    private const double Tolerance = 1e-12;

    /// <summary>A unit impulse at sample 3 of 7: its angle falls by 6π/7 per bin and wraps back past −π.</summary>
    private static readonly double[] Impulse7 = [0, 0, 0, 1, 0, 0, 0];

    private static readonly double[] Eleven = [3, -1, 4, 1, -5, 9, 2, -6, 5, 3, -5];

    private static Figure Of(double[] signal, SpectrumOptions options = default, Action<LineSeries>? configure = null) =>
        Plt.Create().WithSize(800, 400).AddSubPlot(1, 1, 1, ax => ax.AngleSpectrum(signal, options, configure)).Build();

    private static LineSeries OnlySeriesOf(Figure figure) => Assert.IsType<LineSeries>(Assert.Single(figure.SubPlots[0].Series));

    private static void AssertClose(IReadOnlyList<double> expected, IReadOnlyList<double> actual)
    {
        Assert.Equal(expected.Count, actual.Count);
        for (int i = 0; i < expected.Count; i++)
        {
            Assert.True(Math.Abs(expected[i] - actual[i]) <= Tolerance, $"[{i}]: expected {expected[i]:R}, got {actual[i]:R}");
        }
    }

    [Fact]
    public void TheVerb_DrawsExactlyWhatTheHandRecipeDraws()
    {
        Assert.Equal(ComposedChartShapes.AngleSpectrumByHand(false).ToSvg(), ComposedChartShapes.Named("angle-spectrum").Build(false).ToSvg());
        Assert.Equal(ComposedChartShapes.AngleSpectrumByHand(true).ToSvg(), ComposedChartShapes.Named("angle-spectrum").Build(true).ToSvg());
    }

    [Fact]
    public void TheChart_IsOneLineSeries_OfTheFrequenciesAndTheWrappedAngle_AsMatplotlibPlotsIt()
    {
        var line = OnlySeriesOf(Of(Impulse7));

        AssertClose([0, 2 / 7.0, 4 / 7.0, 6 / 7.0], line.XData);
        AssertClose([0, -2.6927937030769655, 0.8975979010256553, -1.7951958020513104], line.YData);
    }

    [Fact]
    public void TheAngle_StaysWithinMinusPiAndPi_AndANegativeRealBinIsPlusPi()
    {
        // matplotlib: angle_spectrum(x, window=np.hamming(11), pad_to=14, Fs=1) ends at the Nyquist bin, real and negative: +pi.
        var line = OnlySeriesOf(Of(Eleven, new SpectrumOptions { SampleRate = 1, Window = SpectralWindow.Hamming, PadTo = 14 }));

        AssertClose(
            [0, -2.163700435605824, 1.6126056059677956, -0.7647854269807199, 3.095851653265369, 0.704217416526966, -1.4985695578332388, Math.PI],
            line.YData);
        Assert.All(line.YData, angle => Assert.InRange(angle, -Math.PI, Math.PI));
        Assert.Equal(Math.PI, line.YData[^1]);
    }

    [Fact]
    public void TheOptions_ReachTheTransform()
    {
        var options = new SpectrumOptions { SampleRate = 70, CenterFrequency = -10 };

        var line = OnlySeriesOf(Of(Impulse7, options));

        AssertClose([-10, 0, 10, 20], line.XData);
        AssertClose(Spectrum.Of(Impulse7, options).Angles(), line.YData);
    }

    [Fact]
    public void TheAxes_AreTitledWithWhatTheyMeasure_AndTheVerbSetsNoLimits()
    {
        var axes = Of(Eleven).SubPlots[0];

        Assert.Equal("Frequency", axes.XAxis.Label);
        Assert.Equal("Angle (radians)", axes.YAxis.Label);
        Assert.False(axes.HasExplicitLimit);
        Assert.Null(axes.YAxis.Min);
    }

    [Fact]
    public void AMissingSample_LeavesNothingToDraw_ButTheFrequencies()
    {
        double[] gappy = [.. Eleven];
        gappy[4] = double.NegativeInfinity;

        var figure = Of(gappy);
        var line = OnlySeriesOf(figure);

        Assert.Equal(6, line.XData.Length);
        Assert.All(line.YData, value => Assert.True(double.IsNaN(value)));
        Assert.DoesNotContain("NaN", figure.ToSvg(), StringComparison.Ordinal);
    }

    [Fact]
    public void TheLine_IsOneLegendEntry_AndTheCallbackRunsOnceAndWins()
    {
        var seen = new List<LineSeries>();
        var figure = Plt.Create().WithSize(800, 400).AddSubPlot(1, 1, 1, ax => ax
            .AngleSpectrum(Eleven, default, line => { seen.Add(line); line.Label = "signal"; line.Color = Colors.Red; })
            .WithLegend()).Build();
        figure.EnableLegendToggle = true;

        Assert.Single(Regex.Matches(figure.ToSvg(), "data-legend-index="));
        Assert.Same(OnlySeriesOf(figure), Assert.Single(seen));
        Assert.Equal(Colors.Red, OnlySeriesOf(figure).Color);
    }

    [Fact]
    public void TheChart_SurvivesTheJson_AndDrawsTheSamePicture()
    {
        var figure = Of(Eleven, configure: line => line.Label = "signal");

        Assert.Equal(figure.ToSvg(), ChartSerializerDefault.Current.FromJson(figure.ToJson()).ToSvg());
    }

    [Fact]
    public void WhatCannotBeTransformed_IsRefusedByName_AndLeavesTheAxesAsItWas()
    {
        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax =>
        {
            Assert.Equal("signal", Assert.Throws<ArgumentNullException>(() => ax.AngleSpectrum(null!)).ParamName);
            Assert.Equal("signal", Assert.Throws<ArgumentException>(() => ax.AngleSpectrum([])).ParamName);
            Assert.Equal("options", Assert.Throws<ArgumentException>(() => ax.AngleSpectrum(Eleven, new SpectrumOptions { PadTo = 4 })).ParamName);
        }).Build().SubPlots[0];

        Assert.Empty(axes.Series);
        Assert.Null(axes.YAxis.Label);
    }

    [Fact]
    public void ANullBuilder_IsRefused()
    {
        AxesBuilder axes = null!;

        Assert.Equal("axes", Assert.Throws<ArgumentNullException>(() => axes.AngleSpectrum(Eleven)).ParamName);
    }
}
