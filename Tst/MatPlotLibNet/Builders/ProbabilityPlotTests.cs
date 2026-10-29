// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Tools;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The Q-Q plot against a distribution of the caller's choice (<c>scipy.stats.probplot</c> with <c>dist=</c>): the
/// sample sorted up the Y axis against the quantiles of the reference at Filliben's plotting positions along the X axis, and
/// the least-squares line through those points. <c>QQPlot</c> is this verb against the normal distribution and draws the very
/// same picture. Every expected number below was computed by scipy 1.18.1 (<c>probplot(x, dist=..., sparams=...)</c>) and is
/// compared by tolerance, because the quantiles come from a logarithm, an exponential or a root.</summary>
public class ProbabilityPlotTests
{
    private const double Tolerance = 1e-9;

    private static readonly double[] Five = [3.1, 1.2, 4.8, 2.0, 2.9];

    private static readonly double[] Twelve = [12.5, 9.8, 11.2, 14.1, 10.4, 13.3, 8.7, 12.0, 10.9, 11.6, 15.2, 9.3];

    /// <summary>What scipy draws for one reference: the theoretical quantiles (<c>osm</c>) and the two ends of the reference
    /// line (the least-squares fit evaluated at the first and the last quantile).</summary>
    private readonly record struct Pin(QQReference Reference, double[] Quantiles, double LineStart, double LineEnd);

    private static readonly Dictionary<string, Pin> OnFive = new()
    {
        ["exponential"] = new(QQReference.Exponential,
            [0.13862943611198908, 0.3763045067280136, 0.6931471805599453, 1.1596155935459158, 2.0444649242511774],
            1.4961843599559812, 4.836932484340963),
        ["uniform"] = new(QQReference.Uniform,
            [0.12944943670387588, 0.3136067101584343, 0.5, 0.6863932898415657, 0.8705505632961241],
            1.1426848220473433, 4.4573151779526565),
        ["log-normal 1"] = new(QQReference.LogNormal(1),
            [0.32335724822804385, 0.6152954582782602, 1.0, 1.6252354645981502, 3.0925547686958357],
            1.5965533067794686, 4.902911059052075),
        ["t 5"] = new(QQReference.StudentT(5),
            [-1.273290994733032, -0.5169857554919894, 0.0, 0.5169857554919892, 1.273290994733032],
            1.0630344621538277, 4.5369655378461715),
        ["t 1"] = new(QQReference.StudentT(1),
            [-2.3218737823149675, -0.6631611655154838, 0.0, 0.6631611655154837, 2.3218737823149675],
            0.9905214708944063, 4.609478529105593),
        ["fitted log-normal"] = new(QQReference.FittedLogNormal(Five),
            [0.5914905038295427, 0.7978117422066915, 1.0, 1.253428530938978, 1.6906442174905696],
            1.3140049153096038, 4.751274087151636),
    };

    private static readonly Dictionary<string, Pin> OnTwelve = new()
    {
        ["t 5"] = new(QQReference.StudentT(5),
            [-1.9246921908119934, -1.2337048364217458, -0.8504794996600875, -0.5663461661503257, -0.32634599717933443, -0.10676540226635303,
             0.10676540226635288, 0.32634599717933443, 0.5663461661503254, 0.8504794996600874, 1.2337048364217456, 1.9246921908119934],
            8.118643242272933, 15.048023424393735),
        ["exponential"] = new(QQReference.Exponential,
            [0.05776226504666208, 0.14626301243733658, 0.24454976856660934, 0.35356039533728745, 0.4759248859508769, 0.6153777319424426,
             0.7774786242288182, 0.971037745637273, 1.211278031618731, 1.5281207054506627, 1.9945891184366336, 2.8801616867887967],
            9.609295790432169, 15.938799011792444),
        ["fitted log-normal"] = new(QQReference.FittedLogNormal(Twelve),
            [0.7743479703697831, 0.8379218131971653, 0.88160403653722, 0.9180944713228424, 0.9514723333226116, 0.983783331400521,
             1.0164839839036435, 1.051002709146493, 1.0892125279429508, 1.1342960768734882, 1.1934287713365654, 1.2914090799804883],
            8.529177812611472, 15.223283790336463),
    };

    public static TheoryData<string> FiveReferences { get; } = new(OnFive.Keys);

    public static TheoryData<string> TwelveReferences { get; } = new(OnTwelve.Keys);

    private static Axes AxesOf(Action<AxesBuilder> configure) =>
        Plt.Create().AddSubPlot(1, 1, 1, configure).Build().SubPlots[0];

    private static Axes Probability(double[] sample, QQReference reference, Action<QQPlotParts>? configure = null) =>
        AxesOf(ax => ax.ProbabilityPlot(sample, reference, configure));

    private static ScatterSeries Points(Axes axes) => Assert.IsType<ScatterSeries>(Assert.Single(axes.Series));

    private static Trendline Reference(Axes axes) => Assert.Single(axes.Trendlines);

    private static void AssertClose(IReadOnlyList<double> expected, IReadOnlyList<double> actual)
    {
        Assert.Equal(expected.Count, actual.Count);
        for (int i = 0; i < expected.Count; i++)
        {
            Assert.True(Math.Abs(expected[i] - actual[i]) <= Tolerance, $"[{i}]: expected {expected[i]:R}, got {actual[i]:R}");
        }
    }

    // ---- scipy's numbers --------------------------------------------------------------------------------------------------

    /// <summary>For five observations and each reference the points stand at scipy's <c>osm</c> and the reference line runs
    /// between the ends scipy's least-squares fit gives; the sample is sorted up the Y axis whatever the reference.</summary>
    [Theory]
    [MemberData(nameof(FiveReferences))]
    public void FiveObservations_StandAtScipysQuantiles_WithScipysLine(string name)
    {
        var pin = OnFive[name];

        var axes = Probability(Five, pin.Reference);

        AssertClose(pin.Quantiles, Points(axes).XData);
        Assert.Equal([1.2, 2.0, 2.9, 3.1, 4.8], Points(axes).YData);
        var line = Reference(axes);
        Assert.Equal(Points(axes).XData[0], line.X1);
        Assert.Equal(Points(axes).XData[^1], line.X2);
        AssertClose([pin.LineStart, pin.LineEnd], [line.Y1, line.Y2]);
    }

    [Theory]
    [MemberData(nameof(TwelveReferences))]
    public void TwelveObservations_StandAtScipysQuantiles_WithScipysLine(string name)
    {
        var pin = OnTwelve[name];

        var axes = Probability(Twelve, pin.Reference);

        AssertClose(pin.Quantiles, Points(axes).XData);
        var line = Reference(axes);
        AssertClose([pin.LineStart, pin.LineEnd], [line.Y1, line.Y2]);
    }

    /// <summary>The reference is asked for the quantile of every observation's plotting position, in ascending order and once:
    /// Filliben's positions for five observations (the same the Q-Q plot against the normal distribution stands at).</summary>
    [Fact]
    public void TheReference_IsAskedForFillibensPositions_OneForEachObservation_InOrder()
    {
        var asked = new List<double>();

        Probability(Five, QQReference.Of("recorder", p => { asked.Add(p); return p; }));

        AssertClose([0.12944943670387588, 0.3136067101584343, 0.5, 0.6863932898415657, 0.8705505632961241], asked);
    }

    /// <summary>A caller's own quantile function is a reference like the others: the identity is the uniform distribution.</summary>
    [Fact]
    public void ACallersOwnQuantileFunction_IsAReferenceLikeTheOthers()
    {
        var own = Probability(Five, QQReference.Of("Uniform(0, 1)", p => p));
        var shipped = Probability(Five, QQReference.Uniform);

        Assert.Equal(Points(shipped).XData, Points(own).XData);
        Assert.Equal(Reference(shipped).Y1, Reference(own).Y1);
    }

    // ---- the Q-Q plot is this verb against the normal distribution --------------------------------------------------------

    /// <summary>Against the normal distribution (named, or <c>default</c>) the verb draws the very picture <c>QQPlot</c> does,
    /// byte for byte, titles included.</summary>
    [Fact]
    public void AgainstTheNormalDistribution_ItDrawsTheQQPlot_ByteForByte()
    {
        string qq = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.QQPlot(Twelve, parts => parts.Points.Label = "sample").WithLegend()).ToSvg();

        string named = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.ProbabilityPlot(Twelve, QQReference.Normal, parts => parts.Points.Label = "sample").WithLegend()).ToSvg();
        string unnamed = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.ProbabilityPlot(Twelve, default, parts => parts.Points.Label = "sample").WithLegend()).ToSvg();

        Assert.Equal(qq, named);
        Assert.Equal(qq, unnamed);
    }

    // ---- axes --------------------------------------------------------------------------------------------------------------

    /// <summary>The X axis names the reference the sample is plotted against (the normal distribution, the plot's default, needs
    /// no name); the Y axis keeps its title; a title set after the call wins.</summary>
    [Fact]
    public void TheXAxis_NamesTheReference_AndATitleSetAfterTheCallWins()
    {
        var exponential = Probability(Five, QQReference.Exponential);
        var normal = Probability(Five, QQReference.Normal);
        var retitled = AxesOf(ax => ax.ProbabilityPlot(Five, QQReference.StudentT(5)).SetXLabel("t quantiles"));

        Assert.Equal("Theoretical quantiles (exponential)", exponential.XAxis.Label);
        Assert.Equal("Sample quantiles", exponential.YAxis.Label);
        Assert.Equal("Theoretical quantiles", normal.XAxis.Label);
        Assert.Equal("t quantiles", retitled.XAxis.Label);
    }

    /// <summary>The chart is one scatter series and one trendline, as the Q-Q plot's: the reference takes no colour slot, and the
    /// verb sets no axis limit.</summary>
    [Fact]
    public void TheChart_IsOneScatterSeriesAndOneTrendline_WithNoAxisLimits()
    {
        var axes = Probability(Five, QQReference.Exponential);

        Points(axes);
        Reference(axes);
        Assert.Empty(axes.Annotations);
        Assert.Empty(axes.ReferenceLines);
        Assert.False(axes.HasExplicitLimit);
        Assert.Equal(Colors.Red, Reference(axes).Color);
    }

    // ---- missing values -----------------------------------------------------------------------------------------------------

    /// <summary>A missing observation (NaN) and an infinity are left out for every reference: the plot is that of the other
    /// observations, at the positions for their number.</summary>
    [Fact]
    public void AMissingOrInfiniteObservation_IsLeftOut_WhateverTheReference()
    {
        double[] gappy = [double.NaN, 3.1, 1.2, double.PositiveInfinity, 4.8, 2.0, double.NegativeInfinity, 2.9];

        var clean = Probability(Five, QQReference.Exponential);
        var axes = Probability(gappy, QQReference.Exponential);

        Assert.Equal(Points(clean).XData, Points(axes).XData);
        Assert.Equal(Points(clean).YData, Points(axes).YData);
        Assert.Equal(Reference(clean).Y1, Reference(axes).Y1);
        Assert.DoesNotContain("NaN", Plt.Create().AddSubPlot(1, 1, 1, ax => ax.ProbabilityPlot(gappy, QQReference.StudentT(5))).ToSvg());
    }

    // ---- refusals -----------------------------------------------------------------------------------------------------------

    [Fact]
    public void MissingArguments_AreRefusedByName()
    {
        Assert.Equal("axes", Assert.Throws<ArgumentNullException>(() => ((AxesBuilder)null!).ProbabilityPlot(Five, QQReference.Normal)).ParamName);
        Assert.Equal("sample", Assert.Throws<ArgumentNullException>(() => Probability(null!, QQReference.Normal)).ParamName);
    }

    /// <summary>A line is fitted through two points or more, whatever the reference, and the refusal reads as the Q-Q plot's.</summary>
    [Fact]
    public void ASampleWithFewerThanTwoObservations_IsRefused_InTheQQPlotsWords()
    {
        var error = Assert.Throws<ArgumentException>(() => Probability([double.NaN, 4.2], QQReference.Exponential));

        Assert.Equal("sample", error.ParamName);
        Assert.Equal("The sample has 1 observation that can be drawn; a Q-Q plot is drawn from 2 or more (NaN and the infinities are left out). (Parameter 'sample')", error.Message);
    }

    /// <summary>A reference whose quantile is not a finite number at a plotting position cannot be drawn: it is refused naming
    /// the reference and the position, and nothing has been added to the axes.</summary>
    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void AReferenceWithoutAFiniteQuantile_IsRefused_AndLeavesTheAxesAsItWas(double quantile)
    {
        var builder = new AxesBuilder();
        var broken = QQReference.Of("broken", p => p > 0.4 && p < 0.6 ? quantile : p);

        var error = Assert.Throws<ArgumentException>(() => builder.ProbabilityPlot(Five, broken));
        var axes = builder.Build(1, 1, 1);

        Assert.Equal("reference", error.ParamName);
        Assert.StartsWith("The reference 'broken' has no finite quantile at the plotting position 0.5;", error.Message, StringComparison.Ordinal);
        Assert.Empty(axes.Series);
        Assert.Empty(axes.Trendlines);
        Assert.Null(axes.XAxis.Label);
        Assert.Null(axes.YAxis.Label);
    }

    // ---- the callback -------------------------------------------------------------------------------------------------------

    [Fact]
    public void TheCallback_RunsOnce_OnThePartsOnTheAxes_AfterTheVerbStyledThem()
    {
        int calls = 0;
        QQPlotParts seen = default;
        Color? referenceColourWhenCalled = null;

        var axes = Probability(Five, QQReference.Exponential, parts =>
        {
            calls++;
            seen = parts;
            referenceColourWhenCalled = parts.Reference.Color;
            parts.Points.Label = "waits";
        });

        Assert.Equal(1, calls);
        Assert.Equal(Colors.Red, referenceColourWhenCalled);
        Assert.Same(axes.Series[0], seen.Points);
        Assert.Same(axes.Trendlines[0], seen.Reference);
        Assert.Equal("waits", Points(axes).Label);
    }

    // ---- JSON ---------------------------------------------------------------------------------------------------------------

    /// <summary>The chart is its parts in JSON: read back it draws the same picture, the reference's name in the title with it.
    /// It is not fitted again; change the sample or the reference and call the verb again.</summary>
    [Fact]
    public void TheChart_SurvivesJson()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.ProbabilityPlot(Twelve, QQReference.StudentT(3))).Build();

        string drawn = figure.ToSvg();
        string readBack = ChartSerializerDefault.Current.FromJson(figure.ToJson()).ToSvg();

        Assert.Equal(drawn, readBack);
        Assert.Contains(">Theoretical quantiles (Student t, 3 df)<", readBack);
    }

    // ---- the example in the documentation -------------------------------------------------------------------------------------

    /// <summary>The example of the verb's documentation, as written there, compiles and draws.</summary>
    [Fact]
    public void TheDocumentationExample_Draws()
    {
        double[] waits = [2.1, 0.4, 5.6, 1.3, 0.9, 3.8, 0.2, 1.7, 2.6, 0.6, 8.4, 1.1];

        string svg = Plt.Create()
            .WithTitle("Minutes between arrivals against the exponential distribution")
            .AddSubPlot(1, 1, 1, ax => ax
                .ProbabilityPlot(waits, QQReference.Exponential, parts => parts.Points.Alpha = 0.7)
                .SetYLabel("Minutes between arrivals"))
            .ToSvg();

        Assert.Contains(">Theoretical quantiles (exponential)<", svg);
        Assert.Contains(">Minutes between arrivals<", svg);
        Assert.Contains("""stroke="#FF0000" """.TrimEnd(), svg);
    }
}
