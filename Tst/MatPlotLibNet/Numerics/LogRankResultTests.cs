// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>The line a log-rank result is read as on a chart: the statistic to two decimals with its degrees of freedom, and
/// the p-value to four decimals or, below that, "p &lt; 0.0001".</summary>
public class LogRankResultTests
{
    [Theory]
    [InlineData(16.792940989216547, 1, 4.168809109334511e-05, "Log-rank χ²(1) = 16.79, p < 0.0001")]
    [InlineData(7.854898685977314, 1, 0.005068319243038367, "Log-rank χ²(1) = 7.85, p = 0.0051")]
    [InlineData(4.981659715785285, 2, 0.08284119162569575, "Log-rank χ²(2) = 4.98, p = 0.0828")]
    [InlineData(0.0, 1, 1.0, "Log-rank χ²(1) = 0.00, p = 1.0000")]
    public void AResult_IsReadAsOneLine(double statistic, int degrees, double p, string expected) =>
        Assert.Equal(expected, new LogRankResult(statistic, degrees, p).Describe());

    /// <summary>The p-value is printed in full from one in ten thousand up, and below it as "p &lt; 0.0001": 0.0001 itself
    /// reads in full, the largest number below it does not.</summary>
    [Fact]
    public void TheLimit_IsOneInTenThousand_AndItsOwnNumberReadsInFull()
    {
        Assert.EndsWith("p = 0.0001", new LogRankResult(14.0, 1, 0.0001).Describe(), StringComparison.Ordinal);
        Assert.EndsWith("p < 0.0001", new LogRankResult(14.0, 1, 0.0001 - 1e-12).Describe(), StringComparison.Ordinal);
    }

    /// <summary>With no statistic there is nothing to read: the line says there is nothing to compare.</summary>
    [Fact]
    public void NoStatistic_ReadsThatThereIsNothingToCompare()
    {
        Assert.Equal("Log-rank test: no events to compare", new LogRankResult(double.NaN, 1, double.NaN).Describe());
        Assert.Equal("Log-rank test: no events to compare", new LogRankResult(double.NaN, 0, double.NaN).Describe());
    }

    /// <summary>A reader's culture changes nothing: the decimal point is a point.</summary>
    [Theory]
    [InlineData("nl-NL")]
    [InlineData("ar-SA")]
    public void TheLine_ReadsNoCulture(string culture)
    {
        var previous = CultureInfo.CurrentCulture;
        var previousUi = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo(culture);

            Assert.Equal("Log-rank χ²(1) = 7.85, p = 0.0051", new LogRankResult(7.854898685977314, 1, 0.005068319243038367).Describe());
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
            CultureInfo.CurrentUICulture = previousUi;
        }
    }
}
