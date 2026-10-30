// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;

namespace MatPlotLibNet.Tests.Indicators.Streaming;

/// <summary>The batch indicators that fold over a step (<c>CyberCycle</c>, <c>MamaFama</c>, <c>Adx</c>, …) have the steps' arithmetic
/// pinned here, at two places each, on <see cref="OracleBars.Walk"/> of 300 bars. The values are what these indicators gave before
/// the step was taken out of their loop (the same program run against the earlier build, 19,613 numbers, byte for byte). The
/// equality theory cannot see a change to a step, because the streaming form and the batch indicator share it: this can.</summary>
public sealed class StepArithmeticPinsTests
{
    private static readonly OracleBars Bars = OracleBars.Walk(300);

    private static double[] Series(string name)
    {
        var b = Bars;
        var hlc = OhlcvBars.Hlc(b.High, b.Low, b.Close);
        return name switch
        {
            "Decycler60" => new Decycler(b.Close, 60).Compute(),
            "Roof" => new RoofingFilter(b.Close, 48, 10).Compute(),
            "SS10" => new EhlersSuperSmoother(b.Close, 10).Compute(),
            "CyberCycle" => new CyberCycle(b.Close, 0.07).Compute(),
            "Laguerre" => new LaguerreRsi(b.Close, 0.2).Compute(),
            "Force13" => new ForceIndex(OhlcvBars.Cv(b.Close, b.Volume), 13).Compute(),
            "Force1" => new ForceIndex(OhlcvBars.Cv(b.Close, b.Volume), 1).Compute(),
            "Twiggs" => new TwiggsMoneyFlow(OhlcvBars.Hlcv(b.High, b.Low, b.Close, b.Volume), 21).Compute(),
            "Mama" => new MamaFama(b.Close, 0.5, 0.05).Compute().Mama,
            "Fama" => new MamaFama(b.Close, 0.5, 0.05).Compute().Fama,
            "ITrend" => new EhlersITrend(b.Close).Compute(),
            "Sine" => new EhlersSineWave(b.Close).Compute().SineWave,
            "Lead" => new EhlersSineWave(b.Close).Compute().LeadSine,
            "AdaptStoch3" => new AdaptiveStochastic(hlc, 3).Compute(),
            "AdaptStoch1" => new AdaptiveStochastic(hlc, 1).Compute(),
            "Adx" => new Adx(hlc, 14).ComputeFull().Adx,
            "PlusDi" => new Adx(hlc, 14).ComputeFull().PlusDi,
            "MinusDi" => new Adx(hlc, 14).ComputeFull().MinusDi,
            "KcM" => new KeltnerChannels(hlc, 20, 1.5).Compute().Middle,
            "KcU" => new KeltnerChannels(hlc, 20, 1.5).Compute().Upper,
            "KcL" => new KeltnerChannels(hlc, 20, 1.5).Compute().Lower,
            "CuP" => new Cusum(b.Close, 0.02, 0.0).Compute().SPos,
            "CuN" => new Cusum(b.Close, 0.02, 0.0).Compute().SNeg,
            "KvK" => new KlingerVolumeOscillator(OhlcvBars.Hlcv(b.High, b.Low, b.Close, b.Volume), 34, 55, 13).Compute().Kvo,
            "KvS" => new KlingerVolumeOscillator(OhlcvBars.Hlcv(b.High, b.Low, b.Close, b.Volume), 34, 55, 13).Compute().Signal,
            "Kv2K" => new KlingerVolumeOscillator(OhlcvBars.Hlcv(b.High, b.Low, b.Close, b.Volume), 3, 6, 4).Compute().Kvo,
            "SuL" => new Supertrend(hlc, 10, 3.0).Compute().Line,
            "SuD" => [.. new Supertrend(hlc, 10, 3.0).Compute().Direction.Select(d => (double)d)],
            "Sar" => new ParabolicSar(b.High, b.Low, 0.02, 0.2).Compute().Sar,
            "Bocpd" => new Bocpd(b.Close, 0.01, 1.0, 100).Compute(),
            "Bocpd2" => new Bocpd(b.Close, 0.05, 4.0, 30).Compute(),
            "DrawDown" => new DrawDown(b.Close).Compute(),
            "Equity" => new EquityCurve(b.Close, 10000).Compute(),
            "Disp" => new DispersionIndex(StreamingRows.FeatureRows(b)).Compute(),
            "Turb20" => new TurbulenceIndex(StreamingRows.FeatureRows(b), 20, 1e-6).Compute(),
            _ => throw new ArgumentException(name),
        };
    }

    [Theory]
    [InlineData("Decycler60", 100, 85.8066388190933)]
    [InlineData("Decycler60", 299, 56.987949533707706)]
    [InlineData("Roof", 100, 0.6005767679602998)]
    [InlineData("Roof", 299, -3.362262804649461)]
    [InlineData("SS10", 100, 87.06336600646263)]
    [InlineData("SS10", 299, 54.6843300688906)]
    [InlineData("CyberCycle", 99, 0.877157008369233)]
    [InlineData("CyberCycle", 296, -3.8069910876787443)]
    [InlineData("Laguerre", 99, 0.0)]
    [InlineData("Laguerre", 298, 0.300200142156258)]
    [InlineData("Force13", 99, -154.24241856451795)]
    [InlineData("Force13", 298, -889.0249506547061)]
    [InlineData("Force1", 99, -730.0)]
    [InlineData("Force1", 298, -2442.0)]
    [InlineData("Twiggs", 99, -0.06727035381825308)]
    [InlineData("Twiggs", 298, -0.14143655747952025)]
    [InlineData("Mama", 98, 86.53186637513073)]
    [InlineData("Mama", 293, 54.89285038883333)]
    [InlineData("Fama", 98, 87.47800039735918)]
    [InlineData("Fama", 293, 57.41540354324749)]
    [InlineData("ITrend", 98, 87.31699346405229)]
    [InlineData("ITrend", 293, 57.02923976608187)]
    [InlineData("Sine", 98, -0.8380325262895895)]
    [InlineData("Sine", 293, 0.11701221085296075)]
    [InlineData("Lead", 98, -0.20676668553643046)]
    [InlineData("Lead", 293, 0.7849894220122506)]
    [InlineData("AdaptStoch3", 98, 30.33613919230199)]
    [InlineData("AdaptStoch3", 293, 9.90705980397014)]
    [InlineData("AdaptStoch1", 98, 35.714285714285715)]
    [InlineData("AdaptStoch1", 293, 8.695652173913043)]
    [InlineData("Adx", 90, 35.994055899946154)]
    [InlineData("Adx", 271, 14.416204645009786)]
    [InlineData("PlusDi", 90, 16.505097760093456)]
    [InlineData("PlusDi", 271, 12.631070118408172)]
    [InlineData("MinusDi", 90, 23.945574306239315)]
    [InlineData("MinusDi", 271, 26.051838565692826)]
    [InlineData("KcM", 93, 83.7164105657013)]
    [InlineData("KcM", 279, 57.38633040202218)]
    [InlineData("KcU", 93, 88.34187990773394)]
    [InlineData("KcU", 279, 62.412656884246545)]
    [InlineData("KcL", 93, 79.09094122366865)]
    [InlineData("KcL", 279, 52.36000391979782)]
    [InlineData("CuP", 99, 0.0)]
    [InlineData("CuP", 298, 0.0)]
    [InlineData("CuN", 99, -0.014388737452099556)]
    [InlineData("CuN", 298, 0.0)]
    [InlineData("KvK", 99, -1456.1209679693366)]
    [InlineData("KvK", 298, -6844.884870320653)]
    [InlineData("KvS", 99, -1924.0477749926274)]
    [InlineData("KvS", 298, -3531.131458801165)]
    [InlineData("Kv2K", 99, 11487.158231335365)]
    [InlineData("Kv2K", 298, -6907.835393321198)]
    [InlineData("SuL", 96, 93.52996442760575)]
    [InlineData("SuL", 289, 62.68202084897327)]
    [InlineData("SuD", 96, -1.0)]
    [InlineData("SuD", 289, -1.0)]
    [InlineData("Sar", 100, 90.2725)]
    [InlineData("Sar", 299, 57.915440000000004)]
    [InlineData("Bocpd", 99, 0.009999999999999998)]
    [InlineData("Bocpd", 298, 0.00999999999999999)]
    [InlineData("Bocpd2", 99, 0.050000000000000024)]
    [InlineData("Bocpd2", 298, 0.05000000000000002)]
    [InlineData("DrawDown", 100, 24.175824175824175)]
    [InlineData("DrawDown", 299, 55.16483516483517)]
    [InlineData("Equity", 100, 20185.25)]
    [InlineData("Equity", 300, 34018.0)]
    [InlineData("Disp", 100, 38.77951377839708)]
    [InlineData("Disp", 299, 21.048975166396)]
    [InlineData("Turb20", 93, 6.807969885324499)]
    [InlineData("Turb20", 279, 12.545356351000832)]
    public void TheBatchIndicator_GivesWhatItGaveBeforeTheStepWasTakenOutOfItsLoop(string name, int index, double expected)
    {
        double actual = Series(name)[index];

        Assert.True(Math.Abs(actual - expected) <= 1e-9 * Math.Max(1, Math.Abs(expected)),
            $"{name}[{index}]: expected {expected:R}, got {actual:R}");
    }
}
