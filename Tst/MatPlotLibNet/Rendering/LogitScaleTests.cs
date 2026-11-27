// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A logit axis is drawn as matplotlib 3.11.2's logit scale draws it, where the library drew it as a linear one.
/// matplotlib, read with <c>inspect</c>: <c>LogitTransform</c> maps a value to <c>log10(p / (1 - p))</c> and
/// <c>LogisticTransform</c> back by <c>1 / (1 + 10 ** -v)</c>; <c>LogitScale.limit_range_for_scale</c> takes an end at or below 0 to
/// <c>minpos</c> (the smallest positive data value, 1e-7 with none) and one at or above 1 to <c>1 - minpos</c>; the margins are taken
/// in the logit space. Printed with <c>ax.plot(p, p); ax.set_yscale('logit')</c> over <c>p = 0.09, 0.18, .., 0.9</c>:
/// <c>get_ylim()</c> = (0.07315689602168207, 0.918547048311118) in the default style and (0.09, 0.9) in classic, the labels
/// <c>10^{-2}</c>, <c>10^{-1}</c>, <c>\frac{1}{2}</c>, <c>1-10^{-1}</c>, <c>1-10^{-2}</c> on the majors and 0.2, 0.3, 0.4, 0.6, 0.7,
/// 0.8 on the minor ticks its formatter names.</summary>
public sealed class LogitScaleTests
{
    private static readonly double[] Probabilities = [.. Enumerable.Range(1, 10).Select(i => i * 0.09)];

    private static Axes LogitAxes(double[] y)
    {
        var axes = new Axes();
        axes.AddSeries(new LineSeries(y, y));
        axes.XAxis.Scale = AxisScale.Logit;
        axes.YAxis.Scale = AxisScale.Logit;
        return axes;
    }

    [Fact]
    public void UnderTheDefaultStyle_ALogitAxisIsPaddedInTheLogitSpace()
    {
        var range = LogitAxes(Probabilities).ComputeDataRanges(Theme.MatplotlibV2);

        Assert.Equal(0.07315689602168207, range.YMin, 12);
        Assert.Equal(0.918547048311118, range.YMax, 12);
        Assert.Equal(0.07315689602168207, range.XMin, 12);
    }

    [Fact]
    public void UnderTheClassicStyle_ALogitAxisEndsAtItsData()
    {
        var range = LogitAxes(Probabilities).ComputeDataRanges(Theme.MatplotlibClassic);

        Assert.Equal(0.09, range.YMin, 12);
        Assert.Equal(0.9, range.YMax, 12);
    }

    [Fact]
    public void ALimitAtZeroAndOne_EndsAtTheSmallestDataValue_AndOneLessIt()
    {
        // ax.set_ylim(0, 1) over p: (0.09, 0.91).
        var axes = LogitAxes(Probabilities);
        axes.YAxis.Min = 0;
        axes.YAxis.Max = 1;

        var range = axes.ComputeDataRanges(Theme.MatplotlibV2);

        Assert.Equal(0.09, range.YMin, 12);
        Assert.Equal(0.91, range.YMax, 12);
    }

    [Fact]
    public void AnEmptyLogitAxis_SpansTheStandardRange_PaddedInTheLogitSpace()
    {
        // An axes with nothing on it, its Y scale logit: (1.9952624945845213e-08, 0.9999999800473751).
        var axes = new Axes();
        axes.YAxis.Scale = AxisScale.Logit;

        var range = axes.ComputeDataRanges(Theme.MatplotlibV2);

        Assert.Equal(1.9952624945845213e-08, range.YMin, tolerance: 1e-20);
        Assert.Equal(0.9999999800473751, range.YMax, 12);
    }

    [Fact]
    public void DataAtZeroAndOne_IsLeftOut_AndAViewWithNoWidthOpensAsLogitLocatorsNonsingularDoes()
    {
        // ax.plot([0, .5, 1], [0, .5, 1]) on a logit Y: 0 and 1 are masked; (0.03772842671007305, 0.962271573289927).
        var range = LogitAxes([0.0, 0.5, 1.0]).ComputeDataRanges(Theme.MatplotlibV2);

        Assert.Equal(0.03772842671007305, range.YMin, 12);
        Assert.Equal(0.962271573289927, range.YMax, 12);
    }

    [Fact]
    public void ALogitAxisPlacesAPoint_ByItsLogit()
    {
        // Over 0.01 .. 0.99 (logit -log10(99) .. log10(99)) on 100 pixels, 0.1 (logit -log10(9)) stands 100 * (1 - log10(9) /
        // log10(99)) / 2 above the bottom: 26.09, where a linear axis put it 9.18 above.
        var transform = DataTransform.Of(new DataRange(0, 1, 0.01, 0.99), new Rect(0, 0, 100, 100),
            new AxisMapping(ScaleRule.Linear, []), new AxisMapping(ScaleRule.Logit, []));

        double y = transform.DataToPixel(0, 0.1).Y;

        Assert.Equal(100 - 100 * (1 - Math.Log10(9) / Math.Log10(99)) / 2, y, 9);
        Assert.Equal(0.1, transform.PixelToData(new Point(0, y)).Y, 9);
    }

    [Fact]
    public void ALogitYAxis_LabelsItsIdealTicks_AndTheMinorTicksItsFormatterNames()
    {
        var svg = Plt.Create().WithSize(640, 480).WithTheme(Theme.MatplotlibV2)
            .AddSubPlot(1, 1, 1, ax => ax.Plot(Probabilities, Probabilities).SetYScale(AxisScale.Logit)).Build().ToSvg().Xml();

        var labels = svg.Each("text").Where(e => (string?)e.Attribute("text-anchor") == "end")
            .Select(e => e.Value).ToList();
        Assert.Equal(["10⁻¹", "0.2", "0.3", "0.4", "½", "0.6", "0.7", "0.8", "1−10⁻¹"], labels);
    }

    [Fact]
    public void AZoomedLogitAxis_LabelsMaxNLocatorsTicks_WithTheDigitsTheirStepNeeds()
    {
        // set_ylim(0.2, 0.6): 0.20, 0.25, .., 0.45, \frac{1}{2}, 0.55, 0.60 in both styles.
        var svg = Plt.Create().WithSize(640, 480).WithTheme(Theme.MatplotlibV2)
            .AddSubPlot(1, 1, 1, ax => ax.Plot(Probabilities, Probabilities).SetYScale(AxisScale.Logit).SetYLim(0.2, 0.6)).Build().ToSvg().Xml();

        var labels = svg.Each("text").Where(e => (string?)e.Attribute("text-anchor") == "end")
            .Select(e => e.Value).ToList();
        Assert.Equal(["0.20", "0.25", "0.30", "0.35", "0.40", "0.45", "½", "0.55", "0.60"], labels);
    }
}
