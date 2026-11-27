// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Extensions;

/// <summary>Verifies that a text the library writes with a number in it reads the same on every machine: a message with a fraction
/// under a culture that writes a comma for the decimals, and a negative tick under a culture whose minus is not the hyphen.</summary>
public class CultureFreeTextTests
{
    private static T Under<T>(string culture, Func<T> make)
    {
        var before = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            return make();
        }
        finally
        {
            CultureInfo.CurrentCulture = before;
        }
    }

    [Fact]
    public void AnOpacityOutOfRange_IsNamedWithAPointForTheDecimals_InEveryCulture()
    {
        var error = Under("nl-NL", () => Assert.Throws<ArgumentOutOfRangeException>(() => 1.5.ThrowIfNotOpacity("alpha")));

        Assert.Contains("(got 1.5)", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ANegativeWholePercent_IsWrittenWithTheHyphen_InEveryCulture()
    {
        Assert.NotEqual("-", CultureInfo.GetCultureInfo("sv-SE").NumberFormat.NegativeSign);

        string text = Under("sv-SE", () => new PercentFormatter(1.0).Format(-0.5));

        Assert.Equal("-50%", text);
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("AProgressBarsShare_IsWrittenWithThePercentSignStraightAfterTheNumber_InEveryCulture", () =>
        {
            var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.ProgressBar(0.625)).Build();
            return Under("", () => figure.ToSvg());
        }, ">63%<"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();
}
