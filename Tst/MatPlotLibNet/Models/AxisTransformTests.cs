// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Tests.Serialization;

namespace MatPlotLibNet.Tests.Models;

/// <summary>The transform a linked axis writes the primary axis' values through (matplotlib's <c>functions=(forward, inverse)</c>
/// of <c>secondary_xaxis</c> and <c>secondary_yaxis</c>): the named ones the library ships (a linear scale and offset, a
/// reciprocal, a logarithm, a power) and a pair of functions of the caller's own. A linear transform is pinned exactly; the
/// others come from a division, a logarithm or a root and are compared by tolerance.</summary>
public class AxisTransformTests
{
    private const double Tolerance = 1e-12;

    // ---- the named transforms ------------------------------------------------------------------------------------------

    /// <summary>Degrees Celsius to Fahrenheit, matplotlib's own example of a secondary axis: exact both ways.</summary>
    [Theory]
    [InlineData(0.0, 32.0)]
    [InlineData(100.0, 212.0)]
    [InlineData(-40.0, -40.0)]
    [InlineData(37.5, 99.5)]
    public void Linear_MapsByItsScaleAndOffset_Exactly(double celsius, double fahrenheit)
    {
        var transform = AxisTransform.Linear(1.8, 32);

        Assert.Equal(fahrenheit, transform.Forward(celsius));
        Assert.Equal(celsius, transform.Inverse(fahrenheit));
    }

    [Fact]
    public void Linear_WithoutAnOffset_OnlyScales()
    {
        var transform = AxisTransform.Linear(2);

        Assert.Equal(6.0, transform.Forward(3));
        Assert.Equal(3.0, transform.Inverse(6));
    }

    /// <summary>Frequency to period (and wavelength to frequency, with the speed of light as the numerator): the transform is
    /// its own inverse.</summary>
    [Fact]
    public void Reciprocal_DividesItsNumeratorByTheValue_AndIsItsOwnInverse()
    {
        var period = AxisTransform.Reciprocal();
        var wavelength = AxisTransform.Reciprocal(299_792_458);

        Assert.Equal(0.25, period.Forward(4));
        Assert.Equal(4.0, period.Inverse(0.25));
        Assert.Equal(0.299792458, wavelength.Forward(1e9), Tolerance);
        Assert.Equal(1e9, wavelength.Inverse(0.299792458), 1e-3);
    }

    [Theory]
    [InlineData(10.0, 1000.0, 3.0)]
    [InlineData(10.0, 0.01, -2.0)]
    [InlineData(2.0, 8.0, 3.0)]
    [InlineData(Math.E, 1.0, 0.0)]
    [InlineData(0.5, 4.0, -2.0)]
    public void Logarithm_IsTheLogarithmInItsBase_AndItsInverseThePower(double logBase, double value, double logarithm)
    {
        var transform = AxisTransform.Logarithm(logBase);

        Assert.Equal(logarithm, transform.Forward(value), Tolerance);
        Assert.Equal(value, transform.Inverse(logarithm), Tolerance * value);
    }

    [Fact]
    public void Logarithm_WithoutABase_IsTheDecimalLogarithm()
    {
        var transform = AxisTransform.Logarithm();

        Assert.Equal(3.0, transform.Forward(1000));
        Assert.Equal(1000.0, transform.Inverse(3));
    }

    [Theory]
    [InlineData(2.0, 3.0, 9.0)]
    [InlineData(0.5, 16.0, 4.0)]
    [InlineData(-1.0, 4.0, 0.25)]
    [InlineData(3.0, 0.0, 0.0)]
    public void Power_RaisesToItsExponent_AndItsInverseTakesTheRoot(double exponent, double value, double power)
    {
        var transform = AxisTransform.Power(exponent);

        Assert.Equal(power, transform.Forward(value), Tolerance);
        Assert.Equal(value, transform.Inverse(power), Tolerance);
    }

    /// <summary>A power is monotone on the values of 0 and more only: below 0 it is not a number either way, so an axis whose
    /// range reaches below 0 cannot be placed through it (<see cref="AxisTransform.ImageOf"/>).</summary>
    [Theory]
    [InlineData(2.0)]
    [InlineData(3.0)]
    [InlineData(0.5)]
    [InlineData(-1.0)]
    public void Power_BelowZero_IsNotANumber_EitherWay(double exponent)
    {
        var transform = AxisTransform.Power(exponent);

        Assert.True(double.IsNaN(transform.Forward(-2)));
        Assert.True(double.IsNaN(transform.Inverse(-2)));
    }

    // ---- the caller's own ------------------------------------------------------------------------------------------------

    [Fact]
    public void Of_IsTheCallersPairOfFunctions_AndRoundTrips()
    {
        var transform = AxisTransform.Of(x => x * x * x, u => Math.Cbrt(u));

        Assert.Equal(8.0, transform.Forward(2));
        Assert.Equal(2.0, transform.Inverse(8));
        foreach (double value in new[] { -3.0, -0.5, 0, 0.25, 7 })
        {
            Assert.Equal(value, transform.Inverse(transform.Forward(value)), Tolerance);
        }
    }

    // ---- refused at construction -----------------------------------------------------------------------------------------

    [Theory]
    [InlineData(0.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Linear_RefusesAScaleThatIsZeroOrNotFinite(double scale) =>
        Assert.Equal("scale", Assert.Throws<ArgumentOutOfRangeException>(() => AxisTransform.Linear(scale, 1)).ParamName);

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Linear_RefusesAnOffsetThatIsNotFinite(double offset) =>
        Assert.Equal("offset", Assert.Throws<ArgumentOutOfRangeException>(() => AxisTransform.Linear(2, offset)).ParamName);

    [Theory]
    [InlineData(0.0)]
    [InlineData(double.NaN)]
    [InlineData(double.NegativeInfinity)]
    public void Reciprocal_RefusesANumeratorThatIsZeroOrNotFinite(double numerator) =>
        Assert.Equal("numerator", Assert.Throws<ArgumentOutOfRangeException>(() => AxisTransform.Reciprocal(numerator)).ParamName);

    [Theory]
    [InlineData(0.0)]
    [InlineData(-2.0)]
    [InlineData(1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Logarithm_RefusesABaseThatIsNotAboveZero_OrIsOne_OrIsNotFinite(double logBase) =>
        Assert.Equal("base", Assert.Throws<ArgumentOutOfRangeException>(() => AxisTransform.Logarithm(logBase)).ParamName);

    [Theory]
    [InlineData(0.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Power_RefusesAnExponentThatIsZeroOrNotFinite(double exponent) =>
        Assert.Equal("exponent", Assert.Throws<ArgumentOutOfRangeException>(() => AxisTransform.Power(exponent)).ParamName);

    [Fact]
    public void Of_RefusesAMissingFunction_ByName()
    {
        Assert.Equal("forward", Assert.Throws<ArgumentNullException>(() => AxisTransform.Of(null!, u => u)).ParamName);
        Assert.Equal("inverse", Assert.Throws<ArgumentNullException>(() => AxisTransform.Of(x => x, null!)).ParamName);
    }

    // ---- the range a linked axis shows ----------------------------------------------------------------------------------

    /// <summary>The range a linked axis shows is the image of the primary's range, low to high, with the way the transform runs
    /// over it: what the drawing needs to lay ticks out in the linked unit and stand each at the primary position of its inverse.</summary>
    [Fact]
    public void TheImageOfARange_IsItsEndsMapped_LowToHigh()
    {
        var image = AxisTransform.Linear(1.8, 32).ImageOf(new Range1D(0, 100));

        Assert.Equal(new LinkedRange(new Range1D(32, 212), Increasing: true), image);
    }

    /// <summary>A transform that runs the other way (a negative scale, a reciprocal, a logarithm of a base below 1, a negative
    /// power) still shows its range low to high, and says it runs down.</summary>
    [Fact]
    public void TheImageOfARange_ThroughATransformThatRunsDown_IsSortedAndSaysSo()
    {
        Assert.Equal(new LinkedRange(new Range1D(-6, -2), Increasing: false), AxisTransform.Linear(-2).ImageOf(new Range1D(1, 3)));
        Assert.Equal(new LinkedRange(new Range1D(0.1, 1), Increasing: false), AxisTransform.Reciprocal().ImageOf(new Range1D(1, 10)));
        Assert.Equal(new LinkedRange(new Range1D(-1, -0.1), Increasing: true), AxisTransform.Reciprocal(-1).ImageOf(new Range1D(1, 10)));
        Assert.Equal(new LinkedRange(new Range1D(-1, -0.1), Increasing: false), AxisTransform.Reciprocal().ImageOf(new Range1D(-10, -1)));
        Assert.Equal(new LinkedRange(new Range1D(0.5, 1), Increasing: false), AxisTransform.Power(-1).ImageOf(new Range1D(1, 2)));
        Assert.Equal(new LinkedRange(new Range1D(0, 9), Increasing: true), AxisTransform.Power(2).ImageOf(new Range1D(0, 3)));
    }

    /// <summary>A primary axis drawn high to low (inverted, or limits set high to low) shows the same values: its image is that of
    /// the interval.</summary>
    [Fact]
    public void TheImageOfAReversedRange_IsTheImageOfItsInterval()
    {
        var transform = AxisTransform.Linear(1.8, 32);

        Assert.Equal(transform.ImageOf(new Range1D(0, 100)), transform.ImageOf(new Range1D(100, 0)));
    }

    [Fact]
    public void TheImageOfARange_ThroughALogarithm_IsItsDecades()
    {
        var image = AxisTransform.Logarithm().ImageOf(new Range1D(1, 1000));

        Assert.NotNull(image);
        Assert.True(image.Value.Increasing);
        Assert.Equal(0.0, image.Value.Interval.Lo, Tolerance);
        Assert.Equal(3.0, image.Value.Interval.Hi, Tolerance);
        Assert.False(AxisTransform.Logarithm(0.5).ImageOf(new Range1D(1, 4))!.Value.Increasing);
    }

    /// <summary>A range the transform cannot place has no image, so the drawing can say so once and draw no ticks, never throw
    /// and never draw garbage: a reciprocal across 0 (its ends map to finite values, but the image between them runs through
    /// both infinities), an end on a pole (a reciprocal or a negative power at 0, a logarithm at 0), an end outside the domain
    /// (a logarithm or a power below 0), and a function of the caller's own that maps the two ends to the same value
    /// (x² over -5 to 5, which matplotlib draws as limits of 23.75 to 26.25) or to a value that is not a number.</summary>
    [Theory]
    [MemberData(nameof(RangesThatCannotBePlaced))]
    public void ARangeTheTransformCannotPlace_HasNoImage(string transform, double lo, double hi)
    {
        Assert.Null(Named(transform).ImageOf(new Range1D(lo, hi)));
    }

    public static TheoryData<string, double, double> RangesThatCannotBePlaced { get; } = new()
    {
        { "reciprocal", -1, 1 },
        { "reciprocal", 0, 1 },
        { "reciprocal", -1, 0 },
        { "power -1", 0, 2 },
        { "power 2", -1, 3 },
        { "power 0.5", -4, -1 },
        { "logarithm", 0, 10 },
        { "logarithm", -1, 10 },
        { "square of the caller's", -5, 5 },
        { "not a number of the caller's", 1, 2 },
    };

    /// <summary>A range that touches the pole of a reciprocal from one side only is placed: the pole is the only thing in the
    /// way.</summary>
    [Fact]
    public void ARangeOnOneSideOfTheReciprocalsPole_IsPlaced()
    {
        Assert.NotNull(AxisTransform.Reciprocal().ImageOf(new Range1D(0.001, 1)));
        Assert.NotNull(AxisTransform.Reciprocal().ImageOf(new Range1D(-1, -0.001)));
    }

    private static AxisTransform Named(string transform) => transform switch
    {
        "reciprocal" => AxisTransform.Reciprocal(),
        "power -1" => AxisTransform.Power(-1),
        "power 2" => AxisTransform.Power(2),
        "power 0.5" => AxisTransform.Power(0.5),
        "logarithm" => AxisTransform.Logarithm(),
        "square of the caller's" => AxisTransform.Of(x => x * x, Math.Sqrt),
        _ => AxisTransform.Of(_ => double.NaN, u => u),
    };

    // ---- the drawn-property contract can set a link ---------------------------------------------------------------------

    /// <summary>The JSON contract sets every settable property of an axis to a value that is not its default
    /// (<see cref="DrawnSeriesFixtures.NotTheDefault"/>), so the value generator knows a transform: a different one for every
    /// property index, which the axis takes and gives back, and which the contract's comparison tells apart.</summary>
    [Fact]
    public void TheContractsValueForALink_IsATransformTheAxisTakes_DifferentForEveryIndex()
    {
        var link = typeof(Axis).GetProperty(nameof(Axis.Link))!;
        var axis = new Axis();

        object first = DrawnSeriesFixtures.NotTheDefault(link, null, 0);
        object second = DrawnSeriesFixtures.NotTheDefault(link, null, 1);
        link.SetValue(axis, first);

        Assert.IsAssignableFrom<AxisTransform>(first);
        Assert.True(DrawnSeriesFixtures.SameValue(first, axis.Link), $"set {DrawnSeriesFixtures.Show(first)}, read {DrawnSeriesFixtures.Show(axis.Link)}.");
        Assert.False(DrawnSeriesFixtures.SameValue(first, second), $"{DrawnSeriesFixtures.Show(first)} and {DrawnSeriesFixtures.Show(second)} pass for each other.");
        Assert.False(DrawnSeriesFixtures.SameValue(first, null));
        Assert.StartsWith("Linear", DrawnSeriesFixtures.Show(first), StringComparison.Ordinal);
    }

    /// <summary>Two transforms of different kinds that happen to map the test values alike are still two transforms.</summary>
    [Fact]
    public void TheContractsComparison_TellsTwoKindsApart()
    {
        Assert.False(DrawnSeriesFixtures.SameValue(AxisTransform.Linear(1), AxisTransform.Of(x => x, u => u)));
        Assert.True(DrawnSeriesFixtures.SameValue(AxisTransform.Reciprocal(2), AxisTransform.Reciprocal(2)));
    }
}
