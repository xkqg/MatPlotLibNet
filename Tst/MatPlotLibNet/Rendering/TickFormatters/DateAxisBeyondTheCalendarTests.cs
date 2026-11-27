// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.TickFormatters;

namespace MatPlotLibNet.Tests.Rendering.TickFormatters;

/// <summary>Verifies that a date axis whose range runs past the dates a <see cref="DateTime"/> can hold is still drawn: a tick
/// no date can be written for is written as the number it is, as a data table and a hover text write such a value, instead of
/// failing the whole draw.</summary>
public class DateAxisBeyondTheCalendarTests
{
    [Fact]
    public void ADateFormat_OnNumbersPastTheLastDate_TicksOnlyTheDates()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1e7], [0.0, 1.0]).SetXDateFormat()).Build();

        var texts = TextsOf(figure.ToSvg());

        // The date locator places its ticks where there are dates: the X axis is labelled with dates, and with no value past
        // 9999-12-31 (the Y axis' labels are the small numbers 0.0 to 1.0).
        Assert.Contains(texts, text => DateTime.TryParseExact(text, "yyyy-MM-dd",
            System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _));
        Assert.DoesNotContain(texts, text => double.TryParse(text, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out double value) && value >= 2958466);
    }

    [Fact]
    public void AnAutoDateAxis_AtTheLastDate_DrawsWithoutFailing()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot([new DateTime(9999, 12, 1), new DateTime(9999, 12, 31)], [1.0, 2.0])).Build();

        var texts = TextsOf(figure.ToSvg());

        Assert.Contains(texts, text => text.StartsWith("Dec", StringComparison.Ordinal));
    }

    /// <summary>The text of every <c>&lt;text&gt;</c> element of <paramref name="svg"/>.</summary>
    private static List<string> TextsOf(string svg) =>
        [.. svg.Elements("text").Select(text => text.Text)];

    [Fact]
    public void TheFormatter_WritesAValueNoDateCanBe_AsTheNumber()
    {
        var formatter = new DateTickFormatter();

        Assert.Equal("3000000", formatter.Format(3_000_000));
        Assert.Equal("NaN", formatter.Format(double.NaN));
    }
}
