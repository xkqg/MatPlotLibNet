// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.LegendRendering;

namespace MatPlotLibNet.Tests.Rendering.LegendRendering;

/// <summary>The legend-values text has one owner: <c>LegendValueTextExtensions</c>. A legend row asks it for the text of a
/// series row and an end-of-line label asks it to append a value, so the two write a value the same way.</summary>
public class LegendValueTextTests
{
    private static readonly double[] X = [0.0, 1.0, 2.0];

    [Fact]
    public void LegendValuesOff_TheRowIsTheBaseLabel()
    {
        var series = new LineSeries(X, [1.0, 2.0, 3.0]);

        Assert.Equal("Revenue", series.LegendLabel("Revenue", legendValues: false));
    }

    [Fact]
    public void LegendValuesOn_TheRowEndsWithTheLastValueInTwoDecimals()
    {
        var series = new LineSeries(X, [1.0, 2.0, 3.256]);

        Assert.Equal("Revenue = 3.26", series.LegendLabel("Revenue", legendValues: true));
    }

    [Fact]
    public void ATrailingGap_TakesThePreviousPointThatCanBeDrawn()
    {
        var series = new LineSeries(X, [1.0, 2.5, double.NaN]);

        Assert.Equal("Revenue = 2.50", series.LegendLabel("Revenue", legendValues: true));
    }

    [Fact]
    public void ALineWithNothingToDraw_IsTheBaseLabel()
    {
        var series = new LineSeries(X, [double.NaN, double.NaN, double.NaN]);

        Assert.Equal("Revenue", series.LegendLabel("Revenue", legendValues: true));
    }

    [Fact]
    public void ASeriesThatIsNotXY_IsTheBaseLabel()
    {
        var series = new PieSeries([1.0, 2.0, 3.0]);

        Assert.Equal("Share", series.LegendLabel("Share", legendValues: true));
    }

    [Fact]
    public void TheValueIsWrittenInTheInvariantCulture()
    {
        var before = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            Assert.Equal("Revenue = 1234.50", "Revenue".WithLegendValue(1234.5));
        }
        finally { CultureInfo.CurrentCulture = before; }
    }
}
