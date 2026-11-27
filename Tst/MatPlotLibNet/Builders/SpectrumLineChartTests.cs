// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The two whole-signal spectra that read each frequency as an angle, matplotlib's <c>angle_spectrum</c> (WRAPPED into (−π, π],
/// numpy's <c>angle</c>) and <c>phase_spectrum</c> (UNWRAPPED along the frequencies, numpy's <c>unwrap</c>, so a delay reads as a straight
/// line instead of a saw), are each drawn as ONE line of (frequency, radians). The numbers were printed by matplotlib 3.11.2.</summary>
public class SpectrumLineChartTests
{
    private const double Tolerance = 1e-12;

    /// <summary>Each verb with the radians its line stands on over the seven-sample impulse.</summary>
    public static TheoryData<Action<AxesBuilder>, double[]> TheAngleOfEachFrequency => new()
    {
        { ax => ax.AngleSpectrum(SpectralSignals.Impulse7, default, null), [0, -2.6927937030769655, 0.8975979010256553, -1.7951958020513104] },
        { ax => ax.PhaseSpectrum(SpectralSignals.Impulse7, default, null), [0, -2.6927937030769655, -5.385587406153931, -8.078381109230897] },
    };

    [Theory]
    [MemberData(nameof(TheAngleOfEachFrequency))]
    public void TheChart_IsOneLineSeries_OfTheFrequenciesAndTheirAngle_AsMatplotlibPlotsIt(Action<AxesBuilder> verb, double[] radians)
    {
        var line = TestFigures.OfOneVerb(verb).OnlySeries<LineSeries>();

        (new double[] { 0, 2 / 7.0, 4 / 7.0, 6 / 7.0 }).AssertCloseTo(line.XData, Tolerance);
        radians.AssertCloseTo(line.YData, Tolerance);
    }
}
