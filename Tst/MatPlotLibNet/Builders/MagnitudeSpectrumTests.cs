// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The <c>MagnitudeSpectrum</c> verb (matplotlib's <c>magnitude_spectrum</c>): the size of each frequency in a whole
/// signal, one transform of it (<c>Spectrum</c>, held in <c>SpectrumTests</c>) drawn as ONE line of (frequency, |X| / Σw), on the
/// linear scale or in decibels (20·log10). This holds what the verb does with the spectrum: the two scales and their labels, a
/// zero magnitude in decibels as a gap, the options reaching the transform, and the refusals by the verb's own parameter names.
/// The numbers marked "matplotlib" were printed by matplotlib 3.11.2.</summary>
public class MagnitudeSpectrumTests
{
    private const double Tolerance = 1e-12;

    private static readonly double[] Ones8 = [1, 1, 1, 1, 1, 1, 1, 1];

    /// <summary>Eleven integers: no bin of their spectrum is zero, so every bin has decibels.</summary>
    private static readonly double[] Eleven = [3, -1, 4, 1, -5, 9, 2, -6, 5, 3, -5];

    /// <summary>matplotlib's <c>magnitude_spectrum(ones(8))</c>: Hann by default, divided by the sum of the window.</summary>
    private static readonly double[] OnesMagnitude = [1, 0.5942915774844614, 0.06100063442391807, 0.015469346261788391, 0];

    private static Figure Of(double[] signal, SpectrumOptions options = default, MagnitudeScale scale = MagnitudeScale.Linear, Action<LineSeries>? configure = null) =>
        Plt.Create().WithSize(800, 400).AddSubPlot(1, 1, 1, ax => ax.MagnitudeSpectrum(signal, options, scale, configure)).Build();

    private static LineSeries OnlySeriesOf(Figure figure) => Assert.IsType<LineSeries>(Assert.Single(figure.SubPlots[0].Series));

    private static void AssertClose(IReadOnlyList<double> expected, IReadOnlyList<double> actual)
    {
        Assert.Equal(expected.Count, actual.Count);
        for (int i = 0; i < expected.Count; i++)
        {
            Assert.True(Math.Abs(expected[i] - actual[i]) <= Tolerance, $"[{i}]: expected {expected[i]:R}, got {actual[i]:R}");
        }
    }

    // ---- the shape the contracts hold ---------------------------------------------------------------------------------

    [Fact]
    public void TheVerb_DrawsExactlyWhatTheHandRecipeDraws()
    {
        Assert.Equal(ComposedChartShapes.MagnitudeSpectrumByHand(false).ToSvg(), ComposedChartShapes.Named("magnitude-spectrum").Build(false).ToSvg());
        Assert.Equal(ComposedChartShapes.MagnitudeSpectrumByHand(true).ToSvg(), ComposedChartShapes.Named("magnitude-spectrum").Build(true).ToSvg());
    }

    // ---- what is drawn --------------------------------------------------------------------------------------------------

    [Fact]
    public void TheChart_IsOneLineSeries_OfTheFrequenciesAndTheMagnitudes_AsMatplotlibPlotsThem()
    {
        var line = OnlySeriesOf(Of(Ones8));

        AssertClose([0, 0.25, 0.5, 0.75, 1], line.XData);
        AssertClose(OnesMagnitude, line.YData);
    }

    [Fact]
    public void InDecibels_EachMagnitudeIsTwentyTimesItsLogarithm()
    {
        double[] linear = OnlySeriesOf(Of(Eleven)).YData;

        var line = OnlySeriesOf(Of(Eleven, scale: MagnitudeScale.Decibel));

        AssertClose([.. linear.Select(magnitude => 20 * Math.Log10(magnitude))], line.YData);
    }

    [Fact]
    public void InDecibels_DoublingTheSignal_AddsTwentyTimesTheLogarithmOfTwo_NotTen()
    {
        var once = OnlySeriesOf(Of(Eleven, scale: MagnitudeScale.Decibel));
        var twice = OnlySeriesOf(Of([.. Eleven.Select(v => 2 * v)], scale: MagnitudeScale.Decibel));

        for (int i = 0; i < once.YData.Length; i++)
        {
            Assert.Equal(20 * Math.Log10(2), twice.YData[i] - once.YData[i], 1e-9);
        }
    }

    [Fact]
    public void InDecibels_TheFirstBinsOfOnes_AreMatplotlibs()
    {
        // matplotlib: magnitude_spectrum(ones(8), scale='dB'); its last bin is an exact zero there (-inf), a gap here.
        var line = OnlySeriesOf(Of(Ones8, scale: MagnitudeScale.Decibel));

        AssertClose([0, -4.520008493538224, -24.293312963596456, -36.21056078599582], line.YData[..4]);
    }

    [Fact]
    public void InDecibels_AZeroMagnitude_IsAGap_AndNeverMinusInfinity()
    {
        // Two equal samples under the rectangular window: everything is in the first bin, the Nyquist bin is exactly zero.
        var figure = Of([1, 1], new SpectrumOptions { Window = SpectralWindow.Rectangular }, MagnitudeScale.Decibel);

        var line = OnlySeriesOf(figure);

        Assert.Equal(0.0, line.YData[0], Tolerance);
        Assert.True(double.IsNaN(line.YData[1]), $"expected a gap, got {line.YData[1]:R}");
        Assert.DoesNotContain("Infinity", figure.ToSvg(), StringComparison.Ordinal);
        Assert.DoesNotContain("NaN", figure.ToSvg(), StringComparison.Ordinal);
    }

    [Fact]
    public void OnTheLinearScale_AZeroMagnitude_IsDrawnAsZero()
    {
        var line = OnlySeriesOf(Of([1, 1], new SpectrumOptions { Window = SpectralWindow.Rectangular }));

        Assert.Equal([1.0, 0.0], line.YData);
    }

    [Fact]
    public void TheScale_ChoosesTheLabelOfTheYAxis()
    {
        Assert.Equal("Magnitude (energy)", Of(Ones8).SubPlots[0].YAxis.Label);
        Assert.Equal("Magnitude (dB)", Of(Ones8, scale: MagnitudeScale.Decibel).SubPlots[0].YAxis.Label);
    }

    [Fact]
    public void TheScale_KeepsItsOrdinals()
    {
        // Append-only: the numbers a stored value means never change.
        Assert.Equal(0, (int)MagnitudeScale.Linear);
        Assert.Equal(1, (int)MagnitudeScale.Decibel);
    }

    // ---- missing samples ----------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void AMissingSample_LeavesNothingToDraw_ButTheFrequencies(double missing)
    {
        double[] gappy = [.. Eleven];
        gappy[4] = missing;

        foreach (var scale in new[] { MagnitudeScale.Linear, MagnitudeScale.Decibel })
        {
            var figure = Of(gappy, scale: scale);
            var line = OnlySeriesOf(figure);

            Assert.Equal(6, line.XData.Length);
            Assert.All(line.YData, value => Assert.True(double.IsNaN(value)));
            Assert.DoesNotContain("NaN", figure.ToSvg(), StringComparison.Ordinal);
        }
    }

    // ---- the options reach the transform ---------------------------------------------------------------------------------

    /// <summary>Options that each change the spectrum, by name: xunit carries the name, never the options (they hold functions).</summary>
    private static readonly Dictionary<string, SpectrumOptions> OptionsThatChangeTheSpectrum = new()
    {
        ["sample rate"] = new SpectrumOptions { SampleRate = 1000 },
        ["window"] = new SpectrumOptions { Window = SpectralWindow.Hamming },
        ["padding"] = new SpectrumOptions { PadTo = 16 },
        ["centre frequency"] = new SpectrumOptions { CenterFrequency = 100 },
    };

    public static TheoryData<string> OptionNames { get; } = new(OptionsThatChangeTheSpectrum.Keys);

    [Theory]
    [MemberData(nameof(OptionNames))]
    public void TheOptions_ReachTheTransform(string name)
    {
        var options = OptionsThatChangeTheSpectrum[name];
        OneSidedSpectrum spectrum = Spectrum.Of(Eleven, options);

        var line = OnlySeriesOf(Of(Eleven, options));
        var plain = OnlySeriesOf(Of(Eleven));

        AssertClose(spectrum.Frequencies, line.XData);
        AssertClose(spectrum.Magnitudes(), line.YData);
        Assert.False(line.XData.SequenceEqual(plain.XData) && line.YData.SequenceEqual(plain.YData), $"{name}: the options drew nothing the defaults do not");
    }

    [Fact]
    public void WithoutOptions_TheSampleRateIsMatplotlibs2()
    {
        Assert.Equal(1.0, OnlySeriesOf(Of(Ones8)).XData[^1]);   // the Nyquist frequency of a sample rate of 2
    }

    // ---- the axes -------------------------------------------------------------------------------------------------------

    [Fact]
    public void TheAxes_AreTitledWithWhatTheyMeasure_AndATitleSetAfterTheCallWins()
    {
        var axes = Of(Ones8).SubPlots[0];
        var retitled = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.MagnitudeSpectrum(Ones8).SetXLabel("Frequency (Hz)").SetYLabel("Size")).Build().SubPlots[0];

        Assert.Equal("Frequency", axes.XAxis.Label);
        Assert.Equal("Magnitude (energy)", axes.YAxis.Label);
        Assert.Equal("Frequency (Hz)", retitled.XAxis.Label);
        Assert.Equal("Size", retitled.YAxis.Label);
    }

    [Fact]
    public void TheVerb_SetsNoAxisLimits_AndKeepsTheCallersOwn()
    {
        var axes = Of(Ones8).SubPlots[0];
        var kept = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.SetYLim(0, 2).MagnitudeSpectrum(Ones8)).Build().SubPlots[0];

        Assert.False(axes.HasExplicitLimit);
        Assert.Null(axes.XAxis.Min);
        Assert.Null(axes.YAxis.Max);
        Assert.Equal(0.0, kept.YAxis.Min);
        Assert.Equal(2.0, kept.YAxis.Max);
    }

    // ---- the series -----------------------------------------------------------------------------------------------------

    [Fact]
    public void TheLine_IsOneLegendEntry()
    {
        var figure = Plt.Create().WithSize(800, 400).AddSubPlot(1, 1, 1, ax => ax
            .MagnitudeSpectrum(Eleven, default, MagnitudeScale.Linear, line => line.Label = "signal")
            .WithLegend()).Build();
        figure.EnableLegendToggle = true;

        string svg = figure.ToSvg();

        Assert.Single(Regex.Matches(svg, "data-legend-index="));
        Assert.Contains("signal", svg, StringComparison.Ordinal);
    }

    [Fact]
    public void TheLine_IsOneColourSlot_TheNextSeriesTakesTheNextColour()
    {
        var figure = Plt.Create().WithSize(800, 400).AddSubPlot(1, 1, 1, ax => ax
            .MagnitudeSpectrum(Eleven)
            .Plot([0.0, 0.5], [0.0, 1.0])).Build();
        var drawn = new RecordingRenderContext();
        new ChartRenderer().Render(figure, drawn);

        var colours = drawn.OfKind("DrawLines").Select(call => (Color)((dynamic)call.Args!).color).Distinct().ToList();

        Assert.Contains(Theme.Default.CycleColors[0], colours);
        Assert.Contains(Theme.Default.CycleColors[1], colours);
    }

    [Fact]
    public void TheCallback_RunsOnceOnTheSeriesTheVerbAdded_AndWhatItSetsWins()
    {
        var seen = new List<LineSeries>();

        var figure = Of(Eleven, configure: series => { seen.Add(series); series.Color = Colors.Red; series.LineWidth = 3; });

        var line = OnlySeriesOf(figure);
        Assert.Same(line, Assert.Single(seen));
        Assert.Equal(Colors.Red, line.Color);
        Assert.Equal(3.0, line.LineWidth);
    }

    [Fact]
    public void TheVerb_ChainsWithTheRestOfTheBuilder()
    {
        var builder = Plt.Create().AddSubPlot(1, 1, 1, ax => Assert.Same(ax, ax.MagnitudeSpectrum(Eleven)));

        Assert.Single(builder.Build().SubPlots[0].Series);
    }

    [Fact]
    public void TheChart_SurvivesTheJson_AndDrawsTheSamePicture()
    {
        var figure = Of(Eleven, configure: line => line.Label = "signal");
        var decibels = Of([1, 1], new SpectrumOptions { Window = SpectralWindow.Rectangular }, MagnitudeScale.Decibel);

        Assert.Equal(figure.ToSvg(), ChartSerializerDefault.Current.FromJson(figure.ToJson()).ToSvg());
        Assert.Equal(decibels.ToSvg(), ChartSerializerDefault.Current.FromJson(decibels.ToJson()).ToSvg());
        Assert.Contains("\"NaN\"", decibels.ToJson(), StringComparison.Ordinal);
    }

    // ---- what the verb refuses ----------------------------------------------------------------------------------------

    [Fact]
    public void ANullSignal_IsRefusedByTheNameOfTheSignal()
    {
        Assert.Equal("signal", Assert.Throws<ArgumentNullException>(() => Of(null!)).ParamName);
    }

    [Fact]
    public void AnEmptySignal_IsRefusedByTheNameOfTheSignal()
    {
        var refused = Assert.Throws<ArgumentException>(() => Of([]));

        Assert.Equal("signal", refused.ParamName);
        Assert.Contains("no samples", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void APaddingShorterThanTheSignal_IsRefusedByTheNameOfTheOptions()
    {
        Assert.Equal("options", Assert.Throws<ArgumentException>(() => Of(Eleven, new SpectrumOptions { PadTo = 8 })).ParamName);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    public void AScaleThatIsNotAMember_IsRefusedByTheNameOfTheScale(int scale)
    {
        var refused = Assert.Throws<ArgumentOutOfRangeException>(() => Of(Eleven, scale: (MagnitudeScale)scale));

        Assert.Equal("scale", refused.ParamName);
    }

    [Fact]
    public void ARefusedCall_LeavesTheAxesAsItWas()
    {
        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax =>
        {
            Assert.Throws<ArgumentException>(() => ax.MagnitudeSpectrum([]));
            Assert.Throws<ArgumentException>(() => ax.MagnitudeSpectrum(Eleven, new SpectrumOptions { PadTo = 8 }));
            Assert.Throws<ArgumentOutOfRangeException>(() => ax.MagnitudeSpectrum(Eleven, default, (MagnitudeScale)7));
            Assert.Throws<ArgumentNullException>(() => ax.MagnitudeSpectrum(null!));
        }).Build().SubPlots[0];

        Assert.Empty(axes.Series);
        Assert.Null(axes.XAxis.Label);
        Assert.Null(axes.YAxis.Label);
    }

    [Fact]
    public void ANullBuilder_IsRefused()
    {
        AxesBuilder axes = null!;

        Assert.Equal("axes", Assert.Throws<ArgumentNullException>(() => axes.MagnitudeSpectrum(Eleven)).ParamName);
    }
}
