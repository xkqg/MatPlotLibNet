// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The labels of a linear axis in scientific notation. matplotlib 3.11.2, printed under each style:
/// <c>rcParams['axes.formatter.limits']</c> is [-5, 6] in the default style and [-7, 7] in the classic one, and
/// <c>ScalarFormatter._set_order_of_magnitude</c> takes the order of magnitude of the largest tick in view (of the width of the view when an
/// offset is taken off) and divides every label by that power when it is at or below the lower limit or at or above the upper one;
/// <c>get_offset</c> writes the power once, as <c>1e6</c>, then the offset with its sign. The library's own themes write every label in full.
/// An axis with a formatter of its own, a category axis and a log axis are not scaled.</summary>
public class ScientificNotationTests
{
    private static readonly Theme V2 = Theme.MatplotlibV2;
    private static readonly Theme Classic = Theme.MatplotlibClassic;

    private static List<string> TextsOf(Theme theme, double[] x, Action<AxesBuilder>? more = null) =>
        [.. Plt.Create().WithTheme(theme).WithSize(600, 400)
            .AddSubPlot(1, 1, 1, ax => { ax.Plot(x, [0.0, 1.0]); more?.Invoke(ax); }).ToSvg().Each("text").Select(e => e.Value)];

    [Fact]
    public void UnderV2_AnAxisInTheMillions_DividesItsLabelsByAMillion_AndWritesTheOnceAtItsEnd()
    {
        var texts = TextsOf(V2, [0.0, 2_000_000.0]);

        Assert.Contains("1e6", texts);
        Assert.Contains("1.0", texts);
        Assert.DoesNotContain(texts, t => t.Contains("000000"));
    }

    [Fact]
    public void UnderV2_AnAxisOfTinyValues_IsDividedByTheirPower()
    {
        var texts = TextsOf(V2, [0.0, 0.00002]);

        Assert.Contains("1e−5", texts);   // the true minus sign of axes.unicode_minus
        Assert.DoesNotContain(texts, t => t.Contains("0.00001"));
    }

    [Fact]
    public void UnderClassic_TheLimitsAreSevenAndMinusSeven()
    {
        Assert.DoesNotContain("1e6", TextsOf(Classic, [0.0, 2_000_000.0]));
        Assert.Contains("1e7", TextsOf(Classic, [0.0, 20_000_000.0]));
    }

    [Fact]
    public void UnderTheLibrarysThemes_EveryLabelIsWrittenInFull()
    {
        var texts = TextsOf(Theme.Default, [0.0, 2_000_000.0]);

        Assert.DoesNotContain(texts, t => t.StartsWith("1e"));
    }

    [Fact]
    public void AnAxisThatIsNotInTheRange_IsNotScaled()
    {
        Assert.DoesNotContain(TextsOf(V2, [0.0, 2_000.0]), t => t.StartsWith("1e"));
        Assert.DoesNotContain(TextsOf(V2, [0.0, 0.5]), t => t.StartsWith("1e"));
    }

    [Fact]
    public void AnAxisWithAFormatterOfItsOwn_IsNotScaled()
    {
        var texts = TextsOf(V2, [0.0, 2_000_000.0], ax => ax.SetXTickFormatter(new MatPlotLibNet.Rendering.TickFormatters.NumericTickFormatter()));

        Assert.DoesNotContain("1e6", texts);
    }

    [Fact]
    public void TheOrderOfMagnitude_FollowsTheLargestTickInView_OrTheWidthOfTheViewWhenAnOffsetIsTakenOff()
    {
        double[] ticks = [0.0, 1_000_000, 2_000_000, 3_000_000];
        var limits = new PowerLimits(-5, 6);

        Assert.Equal(6, ticks.OrderOfMagnitudeIn(new Range1D(0, 3_000_000), 0, limits));
        Assert.Equal(0, ticks.OrderOfMagnitudeIn(new Range1D(0, 3_000_000), 0, new PowerLimits(-5, 7)));
        Assert.Equal(0, ticks.OrderOfMagnitudeIn(new Range1D(0, 500), 0, limits));            // only 0 is in view
        Assert.Equal(6, ticks.OrderOfMagnitudeIn(new Range1D(0, 3_500_000), 1, limits));       // an offset: the width of the view, 3.5e6
        Assert.Equal(-5, new[] { 0.0, 0.00001, 0.00002 }.OrderOfMagnitudeIn(new Range1D(0, 0.00002), 0, limits));
        Assert.Equal(0, new[] { 0.0, 0.0 }.OrderOfMagnitudeIn(new Range1D(0, 1), 0, limits));
    }

    [Fact]
    public void TheScaleText_IsThePowerThenTheOffsetWithItsSign()
    {
        Assert.Equal("1e6", TickLabelExtensions.ScaleText(6, 0));
        Assert.Equal("1e-5", TickLabelExtensions.ScaleText(-5, 0));
        Assert.Equal("+5", TickLabelExtensions.ScaleText(0, 5));
        Assert.Equal("1e3+5e6", TickLabelExtensions.ScaleText(3, 5_000_000));
        Assert.Equal("", TickLabelExtensions.ScaleText(0, 0));
    }

    [Fact]
    public void TheLimits_AreTheMatplotlibThemes_AndSurviveTheThemeJson()
    {
        Assert.Equal(new PowerLimits(-5, 6), Theme.MatplotlibV2.Looks.TickLabels.FormatterLimits);
        Assert.Equal(new PowerLimits(-7, 7), Theme.MatplotlibClassic.Looks.TickLabels.FormatterLimits);
        Assert.Null(Theme.Default.Looks.TickLabels.FormatterLimits);
        Assert.Equal(new PowerLimits(-7, 7), Theme.MatplotlibClassic.ThroughDto().Looks.TickLabels.FormatterLimits);
        Assert.Null(Theme.Default.ThroughDto().Looks.TickLabels.FormatterLimits);
        Assert.Equal(new PowerLimits(-5, 6), Theme.MatplotlibV2.Cloned().Looks.TickLabels.FormatterLimits);
    }
}
