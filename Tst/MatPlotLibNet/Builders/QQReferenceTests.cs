// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The distribution a Q-Q plot compares a sample with: the normal distribution when none is named, or one of the
/// few the library ships (exponential, uniform, log-normal, Student's t), or one the caller supplies as a quantile function.
/// Every expected number below was computed by scipy 1.18.1 (<c>norm.ppf</c>, <c>expon.ppf</c>, <c>lognorm.ppf</c>,
/// <c>t.ppf</c>) and is compared by tolerance, because the quantiles come from a logarithm, an exponential or a root.</summary>
public class QQReferenceTests
{
    private const double Tolerance = 1e-9;

    private static readonly double[] Five = [3.1, 1.2, 4.8, 2.0, 2.9];

    // ---- the shipped references ------------------------------------------------------------------------------------------

    /// <summary>Nothing named is the normal distribution: <c>default</c> and <see cref="QQReference.Normal"/> are one value, and
    /// a Q-Q plot that is given no reference at all is the plot it always was.</summary>
    [Fact]
    public void Normal_IsTheDefault()
    {
        Assert.Equal(default, QQReference.Normal);
        Assert.Equal("normal", QQReference.Normal.Name);
        Assert.Equal(1.959963984540054, QQReference.Normal.Quantile(0.975), Tolerance);
        Assert.Equal(0.0, QQReference.Normal.Quantile(0.5));
    }

    [Fact]
    public void Exponential_IsTheRateOneDistribution()
    {
        var reference = QQReference.Exponential;

        Assert.Equal("exponential", reference.Name);
        Assert.Equal(0.6931471805599453, reference.Quantile(0.5), Tolerance);
        Assert.Equal(3.6888794541139354, reference.Quantile(0.975), Tolerance);
        // The logarithm of 1 - p keeps none of p's digits for a small p; the quantile does (scipy: 1.0000000005000001e-09).
        Assert.Equal(1.0000000005000001e-09, reference.Quantile(1e-9), 1e-24);
    }

    [Fact]
    public void Uniform_IsTheIdentity_OnZeroToOne()
    {
        var reference = QQReference.Uniform;

        Assert.Equal("uniform", reference.Name);
        Assert.Equal(0.3136067101584343, reference.Quantile(0.3136067101584343));
        Assert.Equal(0.0, reference.Quantile(0.0));
        Assert.Equal(1.0, reference.Quantile(1.0));
    }

    [Theory]
    [InlineData(0.5, "log-normal, σ = 0.5", 2.664408261552898)]
    [InlineData(1.0, "log-normal, σ = 1", 7.099071384231335)]
    public void LogNormal_IsTheExponentialOfAScaledNormalQuantile(double sigma, string name, double atNinetySevenAndAHalfPercent)
    {
        var reference = QQReference.LogNormal(sigma);

        Assert.Equal(name, reference.Name);
        Assert.Equal(atNinetySevenAndAHalfPercent, reference.Quantile(0.975), Tolerance);
        Assert.Equal(1.0, reference.Quantile(0.5), Tolerance);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void LogNormal_RefusesAShapeThatIsNotAFinitePositiveNumber(double sigma)
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => QQReference.LogNormal(sigma));

        Assert.Equal("sigma", error.ParamName);
    }

    [Theory]
    [InlineData(5.0, 0.975, "Student t, 5 df", 2.5705818356363146)]
    [InlineData(2.5, 0.9, "Student t, 2.5 df", 1.7302509288071766)]
    public void StudentT_HasTheQuantilesOfItsDegreesOfFreedom(double degrees, double probability, string name, double expected)
    {
        var reference = QQReference.StudentT(degrees);

        Assert.Equal(name, reference.Name);
        Assert.Equal(expected, reference.Quantile(probability), Tolerance);
    }

    [Fact]
    public void StudentT_IsOddAroundTheMedian_AndHeavyTailedForOneDegree()
    {
        var cauchy = QQReference.StudentT(1);

        Assert.Equal(-12.706204736174705, cauchy.Quantile(0.025), 1e-8);
        Assert.Equal(12.706204736174705, cauchy.Quantile(0.975), 1e-8);
        Assert.Equal(0.0, cauchy.Quantile(0.5));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-3.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void StudentT_RefusesDegreesOfFreedomThatAreNotAFinitePositiveNumber(double degrees)
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => QQReference.StudentT(degrees));

        Assert.Equal("degreesOfFreedom", error.ParamName);
    }

    // ---- fitted to the sample ---------------------------------------------------------------------------------------------

    /// <summary>The log-normal fitted to a sample has the standard deviation of the sample's logarithms (divided by n, the
    /// maximum-likelihood estimate; scipy's <c>lognorm.fit(x, floc=0)</c> gives the same 0.4651114228896636 for these five).</summary>
    [Fact]
    public void FittedLogNormal_TakesTheSpreadOfTheLogarithms()
    {
        var reference = QQReference.FittedLogNormal(Five);

        Assert.Equal("log-normal, σ = 0.465", reference.Name);
        Assert.Equal(2.4883047060662413, reference.Quantile(0.975), Tolerance);
    }

    /// <summary>A missing observation (NaN) and an infinity are left out, as in the plot: the fit is that of the other
    /// observations.</summary>
    [Fact]
    public void FittedLogNormal_LeavesOutWhatCannotBeDrawn()
    {
        double[] gappy = [double.NaN, 3.1, 1.2, double.PositiveInfinity, 4.8, 2.0, double.NegativeInfinity, 2.9];

        Assert.Equal(QQReference.FittedLogNormal(Five).Quantile(0.975), QQReference.FittedLogNormal(gappy).Quantile(0.975));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-2.5)]
    public void FittedLogNormal_RefusesAnObservationThatIsNotAbove0_NamingTheParameter(double bad)
    {
        var error = Assert.Throws<ArgumentException>(() => QQReference.FittedLogNormal([3.1, bad, 4.8]));

        Assert.Equal("sample", error.ParamName);
        Assert.StartsWith($"The sample has an observation of {bad.ToString(System.Globalization.CultureInfo.InvariantCulture)} that is not above 0;", error.Message, StringComparison.Ordinal);
    }

    /// <summary>With no spread in the logarithms (one observation, or every value the same) there is no log-normal to fit.</summary>
    [Theory]
    [InlineData(new[] { 4.2 })]
    [InlineData(new[] { 4.2, 4.2, 4.2 })]
    [InlineData(new double[0])]
    public void FittedLogNormal_RefusesASampleWithoutASpread(double[] sample)
    {
        var error = Assert.Throws<ArgumentException>(() => QQReference.FittedLogNormal(sample));

        Assert.Equal("sample", error.ParamName);
        Assert.StartsWith("The sample's logarithms have no spread", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void FittedLogNormal_RefusesAMissingSample_ByName() =>
        Assert.Equal("sample", Assert.Throws<ArgumentNullException>(() => QQReference.FittedLogNormal(null!)).ParamName);

    // ---- the caller's own ------------------------------------------------------------------------------------------------

    [Fact]
    public void Of_IsTheCallersQuantileFunction_UnderTheCallersName()
    {
        var reference = QQReference.Of("Rayleigh", p => Math.Sqrt(-2 * Math.Log(1 - p)));

        Assert.Equal("Rayleigh", reference.Name);
        Assert.Equal(Math.Sqrt(-2 * Math.Log(0.5)), reference.Quantile(0.5), 1e-15);
    }

    [Fact]
    public void Of_RefusesAMissingName_AndAMissingFunction_ByName()
    {
        Assert.Equal("name", Assert.Throws<ArgumentException>(() => QQReference.Of(" ", p => p)).ParamName);
        Assert.Equal("name", Assert.Throws<ArgumentNullException>(() => QQReference.Of(null!, p => p)).ParamName);
        Assert.Equal("quantile", Assert.Throws<ArgumentNullException>(() => QQReference.Of("x", null!)).ParamName);
    }

    // ---- what a reader of the plot is told --------------------------------------------------------------------------------

    /// <summary>The normal reference keeps the title the Q-Q plot always had; every other reference names itself in the title,
    /// so a reader of the picture can tell what the sample is plotted against.</summary>
    [Fact]
    public void TheXAxisTitle_NamesTheReference_UnlessItIsTheNormalDistribution()
    {
        Assert.Equal("Theoretical quantiles", QQReference.Normal.XAxisTitle);
        Assert.Equal("Theoretical quantiles", default(QQReference).XAxisTitle);
        Assert.Equal("Theoretical quantiles (exponential)", QQReference.Exponential.XAxisTitle);
        Assert.Equal("Theoretical quantiles (Student t, 5 df)", QQReference.StudentT(5).XAxisTitle);
        Assert.Equal("Theoretical quantiles (Rayleigh)", QQReference.Of("Rayleigh", p => p).XAxisTitle);
    }

    [Fact]
    public void ARecordStruct_ComparesByValue()
    {
        Assert.Equal(QQReference.Exponential, QQReference.Exponential);
        Assert.NotEqual(QQReference.Exponential, QQReference.Uniform);
        Assert.NotEqual(QQReference.Normal, QQReference.Exponential);
    }
}
