// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>The text of a value a series labels (a bar's value, a contour line's level, a heat map's cell) is one member of the series,
/// <c>LabelOf</c>: the format it was given, else the default of its kind (a bar <c>g6</c>, matplotlib's <c>bar_label</c> default
/// <c>%g</c> with its lower-case exponent marker; a contour <c>G4</c>; a heat map <c>F2</c>), written in the invariant culture. Each
/// renderer typed the format-or-default and the culture itself, and the bar's renderer lowered the marker of the default itself.</summary>
public class LabelFormatTests
{
    private static BarSeries Bar() => new(["a"], [1.0]);
    private static ContourSeries Contour() => new([0.0, 1.0], [0.0, 1.0], new double[,] { { 0, 1 }, { 1, 2 } });
    private static HeatmapSeries Heatmap() => new(new double[,] { { 0 } });

    [Fact]
    public void EachKind_HasItsOwnDefaultFormat()
    {
        Assert.Equal("g6", ((ILabelFormatted)Bar()).DefaultLabelFormat);   // matplotlib's %g: its exponent marker lower case
        Assert.Equal("G4", ((ILabelFormatted)Contour()).DefaultLabelFormat);
        Assert.Equal("F2", ((ILabelFormatted)Heatmap()).DefaultLabelFormat);
    }

    [Fact]
    public void ALabelGivenNoFormat_ReadsAsTheKindsDefaultFormat()
    {
        Assert.Equal("1.23457e+06", Bar().LabelOf(1234567));
        Assert.Equal("0.1235", Contour().LabelOf(0.123456));
        Assert.Equal("1.50", Heatmap().LabelOf(1.5));
    }

    /// <summary>The bar's default writes what its renderer wrote by hand, <c>G6</c> with its exponent marker lowered, for every kind of
    /// number: no picture moved when the rule went into the default.</summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(1e6)]
    [InlineData(-1234567.0)]
    [InlineData(0.0001)]
    [InlineData(1.23456789e-5)]
    [InlineData(999999.5)]
    [InlineData(double.MaxValue)]
    [InlineData(double.Epsilon)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void TheBarsDefault_WritesG6WithItsExponentMarkerLowered(double value)
    {
        Assert.Equal(value.ToString("G6", CultureInfo.InvariantCulture).Replace('E', 'e'), Bar().LabelOf(value));
    }

    [Fact]
    public void AFormatGiven_WinsOverTheKindsDefaultFormat()
    {
        var heatmap = Heatmap();
        heatmap.LabelFormat = "G3";
        var contour = Contour();
        contour.LabelFormat = "F1";
        var bar = Bar();
        bar.LabelFormat = "F0";

        Assert.Equal("1.23", heatmap.LabelOf(1.2345));
        Assert.Equal("0.1", contour.LabelOf(0.123456));
        Assert.Equal("2", bar.LabelOf(1.5));
    }

    [Fact]
    public void TheLabel_IsWrittenWithTheInvariantCulture()
    {
        var before = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("nl-NL");

            Assert.Equal("1.50", Heatmap().LabelOf(1.5));
        }
        finally
        {
            CultureInfo.CurrentCulture = before;
        }
    }

    /// <summary>A series that labels its values says its default format, except the clustermap, whose labels are its heat map panel's: it
    /// hands its format to that panel, which writes in the heat map's own default.</summary>
    [Fact]
    public void EveryLabelableKind_SaysItsDefaultFormat_ExceptTheClustermapThatHandsItsFormatOn()
    {
        var labelable = typeof(ILabelable).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(ILabelable).IsAssignableFrom(t))
            .Where(t => !typeof(ILabelFormatted).IsAssignableFrom(t))
            .Select(t => t.Name);

        Assert.Equal(["ClustermapSeries"], labelable);
    }
}
