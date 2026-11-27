// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Fidelity;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Fidelity.Charts;

/// <summary>A linked axis against matplotlib's <c>secondary_yaxis</c> and <c>secondary_xaxis</c>: Fahrenheit along the right of a
/// plot in Celsius, and the period along the top of a plot over frequency on a log scale. The references are made by
/// <c>tools/mpl_reference/generate.py</c> from the same numbers. The primary axes has set limits: the library rounds an automatic
/// range to a step chosen by a target count, matplotlib to one that depends on the size of the axes, a difference in every chart
/// that has nothing to do with a linked axis.</summary>
public class LinkedAxisFidelityTests : FidelityTest
{
    [Theory]
    [FidelityRows]
    [FidelityTolerance(Rms = 15, Ssim = 0.97, DeltaE = 3, V2Rms = 18, V2DeltaE = 4)]
    public void LinkedYAxis_FahrenheitBesideCelsius_MatchesMatplotlib(string themeId)
    {
        double[] hours = [0, 3, 6, 9, 12, 15, 18, 21, 24];
        double[] celsius = [8.0, 6.5, 7.5, 13.0, 19.5, 22.0, 18.0, 12.5, 9.0];
        AssertFidelity(themeId, "linked_yaxis_fahrenheit", "Celsius and Fahrenheit", ax => ax
            .Plot(hours, celsius)
            .SetXLim(0, 24)
            .SetYLim(5, 25)
            .SetXLabel("hour")
            .SetYLabel("temperature (°C)")
            .WithAddRightYAxis(right => right
                .SetYLabel("temperature (°F)")
                .Link(AxisTransform.Linear(1.8, 32))));
    }

    [Theory]
    [FidelityRows]
    [FidelityTolerance(Rms = 13, Ssim = 0.97, DeltaE = 3, V2Rms = 14, V2DeltaE = 6)]
    public void LinkedXAxis_PeriodAboveALogFrequency_MatchesMatplotlib(string themeId)
    {
        // No title: matplotlib stands a title where the top axes' own labels already are, as it does for any secondary axis.
        double[] frequency = [1, 2, 5, 10, 20, 50, 100, 200, 500, 1000];
        double[] power = [0.8, 1.6, 3.0, 4.2, 3.1, 1.9, 1.1, 0.6, 0.25, 0.1];
        var figure = FidelityFigure(themeId)
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot(frequency, power)
                .SetXScale(AxisScale.Log)
                .SetXLim(1, 1000)
                .SetYLim(0, 5)
                .SetXLabel("frequency (Hz)")
                .SetYLabel("power")
                .WithSecondaryXAxis(top => top
                    .SetXLabel("period (s)")
                    .Link(AxisTransform.Reciprocal())))
            .Build();
        AssertFidelity(figure, "linked_xaxis_period");
    }
}
