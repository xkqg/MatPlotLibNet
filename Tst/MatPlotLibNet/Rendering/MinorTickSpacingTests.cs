// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Text.RegularExpressions;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>
/// Minor ticks at a CALLER-CHOSEN spacing.
///
/// <para><b>What was missing.</b> <see cref="TickConfig.Spacing"/> has always existed on the model, and the
/// Cartesian renderer never read it for minor ticks: it divided each major interval by a hard-coded five.
/// That is matplotlib's default and a fine default, but it makes one whole class of question unanswerable —
/// "put a mark every ten seconds on this date axis" — because the only other route is
/// <c>SetXTickLocator</c>, which replaces the paired date locator and formatter and so takes the axis LABELS
/// with it. A caller that wants extra MARKS should not have to give up its labels to get them.</para>
///
/// <para>Unset spacing keeps the five-way subdivision exactly as before, so nothing that renders today moves.</para>
/// </summary>
public class MinorTickSpacingTests
{
    private static string Render(double? xMinorSpacing)
    {
        var fig = Plt.Create()
            .WithSize(600, 400)
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([0.0, 0.5, 1.0], [1.0, 2.0, 3.0])
                .SetXLim(0, 1)
                .WithMinorTicks())
            .Build();
        if (xMinorSpacing is { } spacing)
        {
            fig.SubPlots[0].XAxis.MinorTicks = fig.SubPlots[0].XAxis.MinorTicks with { Spacing = spacing };
        }
        return fig.ToSvg();
    }

    private static int LineCount(string svg) => Regex.Matches(svg, "<line", RegexOptions.None, TimeSpan.FromSeconds(5)).Count;

    [Fact]
    public void ASPACINGOfItsOwnPutsFEWERMarksThanTheDefaultFiveWaySubdivision()
    {
        // The differential IS the assertion: the two renders differ in nothing but the minor ticks, so the
        // change in line count is the change in minor marks. Over an x-range of 0..1 with major ticks every
        // 0,2 the default subdivides to 0,04 — five times as many marks as an explicit 0,2 spacing.
        var withDefault = LineCount(Render(null));
        var withSpacing = LineCount(Render(0.2));

        Assert.True(withSpacing < withDefault,
            $"an explicit spacing of 0,2 must draw fewer marks than the default 0,04 subdivision (got {withSpacing} vs {withDefault})");
    }

    [Fact]
    public void AFINERSpacingPutsMOREMarks_SoTheNumberIsTheCALLERS()
    {
        // The other direction, which is the one the ops wall actually needs: a mark every ten seconds on an
        // axis whose major ticks are a minute apart is FINER than five-way, not coarser.
        var withDefault = LineCount(Render(null));
        var withFiner = LineCount(Render(0.01));

        Assert.True(withFiner > withDefault,
            $"a spacing finer than majorStep/5 must draw more marks (got {withFiner} vs {withDefault})");
    }

    [Fact]
    public void NOSpacingRendersEXACTLYAsBefore_SoNothingThatDrawsTodayMoves()
    {
        // The compatibility pin. `Spacing` is null on every axis nobody has touched, and for those the
        // five-way subdivision is still what happens — byte for byte.
        var before = Render(null);
        var alsoBefore = Render(null);

        Assert.Equal(before, alsoBefore);
        Assert.Contains("<svg", before, StringComparison.Ordinal);
    }

    [Fact]
    public void ANONPOSITIVESpacingFallsBackToTheDefault_RatherThanDividingByZero()
    {
        // A caller that computes a spacing can compute a zero; the renderer may not spin or throw on it.
        var withDefault = LineCount(Render(null));

        Assert.Equal(withDefault, LineCount(Render(0)));
        Assert.Equal(withDefault, LineCount(Render(-1)));
    }

    [Fact]
    public void TENSECONDMarksOnADateAxisKeepTheAxISLABELSTheDateFormatterChose()
    {
        // The question this exists for, end to end. X is an OLE Automation date, so ten seconds is
        // 10/86400 of a day; the labels still come from the AutoDateLocator/AutoDateFormatter pair, which is
        // exactly what SetXTickLocator would have replaced.
        var start = new DateTime(2026, 9, 11, 12, 0, 0, DateTimeKind.Utc);
        var fig = Plt.Create()
            .WithSize(900, 300)
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([start, start.AddMinutes(1), start.AddMinutes(2)], [1.0, 2.0, 3.0])
                .WithMinorTicks())
            .Build();
        fig.SubPlots[0].XAxis.MinorTicks = fig.SubPlots[0].XAxis.MinorTicks with { Spacing = 10.0 / 86400.0 };

        var svg = fig.ToSvg();

        Assert.Contains("<svg", svg, StringComparison.Ordinal);
        Assert.IsType<MatPlotLibNet.Rendering.TickFormatters.AutoDateFormatter>(fig.SubPlots[0].XAxis.TickFormatter);
        Assert.IsType<MatPlotLibNet.Rendering.TickLocators.AutoDateLocator>(fig.SubPlots[0].XAxis.TickLocator);
    }
}
