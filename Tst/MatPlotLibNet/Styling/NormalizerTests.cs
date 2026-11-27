// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling.ColorMaps;
using MatPlotLibNet.Tests.TestFixtures;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>Verifies <see cref="INormalizer"/> implementations: every normalizer maps its landmarks (its minimum to 0, its maximum to 1,
/// its centre to a half, a collapsed range to a half, a value outside the range to the nearest end) as one row of
/// <see cref="Landmarks"/>; the facts after it hold what a row cannot (a curve's direction, a symmetry, the settings a normalizer reports).</summary>
public class NormalizerTests
{
    /// <summary>One value read through one normalizer over a range, and the position it must come out at.</summary>
    public readonly record struct Landmark(INormalizer Normalizer, double Value, double Min, double Max, double Expected);

    /// <summary>A normalizer and the range a sweep of boundary values reads it over.</summary>
    public readonly record struct NormalizerOverRange(INormalizer Normalizer, double Min, double Max);

    private static Landmark At(INormalizer normalizer, double value, double min, double max, double expected) => new(normalizer, value, min, max, expected);

    public static IEnumerable<object[]> Landmarks()
    {
        INormalizer linear = LinearNormalizer.Instance;
        INormalizer log = new LogNormalizer();
        INormalizer noNorm = NoNormNormalizer.Instance;
        INormalizer twoSlope = new TwoSlopeNormalizer(center: 0);
        INormalizer twoSlopeAtFive = new TwoSlopeNormalizer(center: 5);
        INormalizer twoSlopeAtTen = new TwoSlopeNormalizer(center: 10);
        INormalizer centred = new CenteredNormNormalizer(vcenter: 0);
        INormalizer centredAtFive = new CenteredNormNormalizer(vcenter: 5);
        INormalizer symLog = new SymLogNormalizer();
        INormalizer symLogAtOne = new SymLogNormalizer(linthresh: 1.0);
        INormalizer powerOfTwo = new PowerNormNormalizer(gamma: 2.0);
        INormalizer powerOfHalf = new PowerNormNormalizer(gamma: 0.5);
        INormalizer bins = new BoundaryNormalizer([0, 10, 20, 50, 100]);
        INormalizer unitBins = new BoundaryNormalizer([0.0, 1.0, 2.0, 3.0, 4.0]);

        return new Landmark[]
        {
            // linear: the ends, the middle, and a value outside the range held to the nearest end
            At(linear, 0, 0, 100, 0.0), At(linear, 100, 0, 100, 1.0), At(linear, 50, 0, 100, 0.5),
            At(linear, -10, 0, 100, 0.0), At(linear, 200, 0, 100, 1.0),
            // a range of no width maps every value, however far off, to the middle
            At(linear, 7, 5, 5, 0.5), At(linear, 1e300, 5, 5, 0.5), At(linear, -1e300, 5, 5, 0.5),
            At(log, 5, 5, 5, 0.5), At(powerOfTwo, 9, 5, 5, 0.5), At(symLog, 1, 5, 5, 0.5), At(twoSlopeAtFive, 5, 5, 5, 0.5),
            // log: log(1 + x) over log(1 + range), a value outside the range held to the nearest end
            At(log, 0, 0, 100, 0.0), At(log, 100, 0, 100, 1.0), At(log, -1000, 0, 100, 0.0), At(log, 1000, 0, 100, 1.0),
            // two slopes about a centre: each half is its own linear scale
            At(twoSlope, 0, -10, 10, 0.5), At(twoSlope, -10, -10, 10, 0.0), At(twoSlope, 10, -10, 10, 1.0),
            At(twoSlope, -100, -10, 10, 0.0), At(twoSlope, 100, -10, 10, 1.0), At(twoSlope, 1000, -10, 10, 1.0),
            At(twoSlope, -1, -2, 10, 0.25),        // centre 0 over [-2, 10]: -1 is halfway down the lower half
            At(twoSlope, -5, -10, 10, 0.25), At(twoSlope, 5, -10, 10, 0.75),
            // a half with no width (the centre is the minimum or the maximum) maps to the middle, not to a division by nothing
            At(twoSlope, 0, 0, 10, 0.5), At(twoSlope, -5, 0, 10, 0.5),
            At(twoSlopeAtFive, 5, 5, 10, 0.5), At(twoSlopeAtFive, 3, 5, 10, 0.5), At(twoSlopeAtFive, 7, 0, 5, 0.5),
            At(twoSlopeAtTen, 20, 0, 10, 0.5), At(twoSlopeAtTen, 15, 0, 10, 0.5),
            At(twoSlopeAtFive, 7.5, 0, 10, 0.75),  // 0.5 + 0.5 * 2.5 / 5
            // boundaries: the bin a value falls in over the number of bins
            At(bins, 5, 0, 100, 0.0), At(bins, 75, 0, 100, 0.75), At(bins, 10, 0, 100, 0.25),
            At(unitBins, -100, -100, 100, 0.0), At(unitBins, 0, -100, 100, 0.0), At(unitBins, 0.5, -100, 100, 0.0),
            At(unitBins, 1, -100, 100, 0.25),      // on a boundary: the bin above it
            At(unitBins, 2.5, -100, 100, 0.5), At(unitBins, 3.5, -100, 100, 0.75),
            At(unitBins, 100, -100, 100, 0.75),    // above the last boundary: the last bin
            At(new BoundaryNormalizer([]), 5, 0, 10, 0.5),
            At(new BoundaryNormalizer([5.0]), 1, 0, 10, 0.5), At(new BoundaryNormalizer([5.0]), 7, 0, 10, 0.5),
            // symmetric log: zero is the middle of a symmetric range
            At(symLogAtOne, 0, -100, 100, 0.5),
            At(symLog, 0, -100, 100, 0.5), At(symLog, -100, -100, 100, 0.0), At(symLog, 100, -100, 100, 1.0),
            At(symLog, -1e6, -100, 100, 0.0), At(symLog, 1e6, -100, 100, 1.0),
            // power: gamma 1 is linear, the ends stay the ends
            At(powerOfTwo, 0, 0, 100, 0.0), At(powerOfTwo, 100, 0, 100, 1.0),
            At(powerOfTwo, -1000, 0, 100, 0.0), At(powerOfTwo, 2000, 0, 100, 1.0),
            At(powerOfHalf, 0, 0, 100, 0.0), At(powerOfHalf, 100, 0, 100, 1.0),
            At(new PowerNormNormalizer(gamma: 1.0), 50, 0, 100, 0.5),
            // centred about a value, with the lower half read over the width the data has below the centre
            At(centred, 0, -10, 10, 0.5), At(centred, -10, -10, 10, 0.0), At(centred, 10, -10, 10, 1.0),
            At(centred, -1, -2, 10, 0.25),
            At(new CenteredNormNormalizer(vcenter: 0, halfrange: 5), -2.5, -10, 10, 0.25),   // a half range of 5 holds the scale to [-5, 5]
            At(new CenteredNormNormalizer(vcenter: 0, halfrange: 5), 2.5, -10, 10, 0.75),
            // a half range of nothing collapses both halves: every value is held to the centre
            At(new CenteredNormNormalizer(vcenter: 5, halfrange: 0), 5, -100, 100, 0.5),
            At(new CenteredNormNormalizer(vcenter: 5, halfrange: 0), 1, -100, 100, 0.5),
            At(new CenteredNormNormalizer(vcenter: 5, halfrange: 0), 100, -100, 100, 0.5),
            At(centredAtFive, 5, 5, 100, 0.5),     // the centre is the minimum
            At(centredAtFive, 100, -100, 100, 1.0),
            // no normalization: the value is the position, held to 0..1, whatever the range says
            At(noNorm, 0.7, 0, 1, 0.7), At(noNorm, -0.1, 0, 1, 0.0), At(noNorm, 1.1, 0, 1, 1.0), At(noNorm, 0.5, -1000, 1000, 0.5),
        }.Select(landmark => new object[] { landmark });
    }

    public static IEnumerable<object[]> NormalizersOverTheirRanges() =>
        new NormalizerOverRange[]
        {
            new(LinearNormalizer.Instance, -1000, 1000), new(new LogNormalizer(), 0, 1000), new(new TwoSlopeNormalizer(center: 0), -100, 100),
            new(new CenteredNormNormalizer(vcenter: 0), -100, 100), new(new PowerNormNormalizer(gamma: 1.0), 0, 100), new(new SymLogNormalizer(), -100, 100),
        }.Select(normalizer => new object[] { normalizer });

    [Theory]
    [MemberData(nameof(Landmarks))]
    public void ANormalizer_MapsItsLandmarks(Landmark landmark)
    {
        Assert.Equal(landmark.Expected, landmark.Normalizer.Normalize(landmark.Value, landmark.Min, landmark.Max), 1e-9);
    }

    /// <summary>Whatever the value (zero, the smallest and the largest a double holds, the infinities and NaN), a normalizer answers without
    /// throwing; NaN is left out of the symmetric log, whose <c>Math.Sign(NaN)</c> throws by contract (a normalizer is never handed a missing
    /// value: a colour map and every renderer leave it out first), and a linear normalizer also keeps the answer in 0..1 or NaN.</summary>
    [Fact]
    public void ANormalizer_TakesABoundaryDoubleWithoutThrowing()
    {
        foreach (object[] row in NormalizersOverTheirRanges())
        {
            var (normalizer, min, max) = (NormalizerOverRange)row[0];
            foreach (double value in EdgeCaseData.BoundaryDoubles.Where(d => !double.IsNaN(d) || normalizer is not SymLogNormalizer))
            {
                double answer = 0;
                Assert.Null(Record.Exception(() => answer = normalizer.Normalize(value, min, max)));
                if (normalizer is LinearNormalizer) Assert.True(double.IsNaN(answer) || answer is >= 0 and <= 1, $"out of range: {answer}");
            }
        }
    }

    [Fact]
    public void LogNormalizer_CompressesHighValues()
    {
        var norm = new LogNormalizer();
        // In linear: 50/100 = 0.5. In log: should be > 0.5 (log compresses top)
        double result = norm.Normalize(50, 0, 100);
        Assert.True(result > 0.5, $"LogNormalizer at midpoint should be > 0.5, was {result}");
    }

    /// <summary>The log is monotonic: against the numpy reference values, 10 + 1 stands below 100 + 1, and both inside the range.</summary>
    [Fact]
    public void LogNormalizer_IsMonotonicOverTheNumpyReferenceValues()
    {
        var n = new LogNormalizer();
        double r10 = n.Normalize(NumpyReference.Log10.X10 + 1, 0, 100);
        double r100 = n.Normalize(NumpyReference.Log10.X100 + 1, 0, 100);
        Assert.True(r10 < r100, "log normaliser must be monotonic increasing");
        Assert.True(r10 > 0 && r100 < 1);
    }

    [Fact]
    public void SymLogNormalizer_Positive_MapsAboveHalf()
    {
        var norm = new SymLogNormalizer(linthresh: 1.0);
        Assert.True(norm.Normalize(50, -100, 100) > 0.5);
    }

    /// <summary>Sign symmetry: f(v) + f(-v) == 1 for any v in range.</summary>
    [Fact]
    public void SymLogNormalizer_IsSymmetricAcrossZero()
    {
        var n = new SymLogNormalizer(linthresh: 1.0);
        for (double v = 0.5; v <= 50; v += 5)
        {
            double pos = n.Normalize(v, -100, 100);
            double neg = n.Normalize(-v, -100, 100);
            Assert.Equal(1.0, pos + neg, 1e-9);
        }
    }

    [Fact]
    public void SymLogNormalizer_LinthreshRespected_LinearNearZero()
    {
        // Within linthresh, values should map nearly linearly around center
        var norm = new SymLogNormalizer(linthresh: 10.0);
        double small = norm.Normalize(1, -100, 100);
        double large = norm.Normalize(50, -100, 100);
        // small value closer to 0.5 than large
        Assert.True(Math.Abs(small - 0.5) < Math.Abs(large - 0.5));
    }

    /// <summary>Inside the threshold the transform is linear (so it rises), and beyond it the transform is logarithmic (so it still rises, and
    /// the numpy reference for a threshold of 100 puts 100 below 1000 below 10000).</summary>
    [Fact]
    public void SymLogNormalizer_RisesInsideAndBeyondTheThreshold()
    {
        var inside = new SymLogNormalizer(linthresh: 10.0, linScale: 1.0);
        double a = inside.Normalize(2, -100, 100);
        double b = inside.Normalize(5, -100, 100);
        double c = inside.Normalize(8, -100, 100);
        Assert.True(a < b && b < c, $"linear monotonic violated: {a}, {b}, {c}");

        var beyond = new SymLogNormalizer(linthresh: 100.0);
        double atLin = beyond.Normalize(NumpyReference.SymlogForward_Linthresh100.X100, -100000, 100000);
        double at1k = beyond.Normalize(1000.0, -100000, 100000);
        double at10k = beyond.Normalize(10000.0, -100000, 100000);
        Assert.True(atLin < at1k && at1k < at10k, "log region monotonic broken");
    }

    [Fact]
    public void PowerNormNormalizer_GammaLessThanOne_CompressesLow()
    {
        // gamma=0.5: sqrt(0.5) ≈ 0.707 — midpoint 50 maps above 0.5 (low values expanded)
        var norm = new PowerNormNormalizer(gamma: 0.5);
        Assert.True(norm.Normalize(50, 0, 100) > 0.5);
    }

    [Fact]
    public void PowerNormNormalizer_GammaGreaterThanOne_CompressesHigh()
    {
        // gamma=2: 0.5^2 = 0.25 — midpoint 50 maps < 0.5
        var norm = new PowerNormNormalizer(gamma: 2.0);
        Assert.True(norm.Normalize(50, 0, 100) < 0.5);
    }

    /// <summary>A normalizer reports what it was made with, and a centred one with no half range reads the data range (so its centre is a half).</summary>
    [Fact]
    public void ANormalizer_ReportsTheSettingsItWasMadeWith()
    {
        Assert.Equal(7.5, new TwoSlopeNormalizer(center: 7.5).Center);
        Assert.Equal(2.5, new PowerNormNormalizer(gamma: 2.5).Gamma);

        var centred = new CenteredNormNormalizer(vcenter: 3, halfrange: 5);
        Assert.Equal(3, centred.Vcenter);
        Assert.Equal(5, centred.Halfrange);
        Assert.Null(new CenteredNormNormalizer(vcenter: 0).Halfrange);

        var symLog = new SymLogNormalizer(linthresh: 50, @base: 2.5, linScale: 3.0);
        Assert.Equal(50, symLog.Linthresh);
        Assert.Equal(2.5, symLog.Base);
        Assert.Equal(3.0, symLog.LinScale);
    }

    /// <summary>The upper half of a two-slope scale is guarded as the lower half is, and as a centred scale guards both: a half with no
    /// width maps to the middle, not to the quotient of a division by nothing. The only value that reaches the upper half of a scale whose
    /// centre is its maximum is a missing one (every other value is held to the maximum first, which is the centre, so it reads the lower
    /// half).</summary>
    [Fact]
    public void AnUpperHalfOfNoWidth_MapsAMissingValueToTheMiddle_AsTheCentredScaleDoes()
    {
        double twoSlope = new TwoSlopeNormalizer(center: 10).Normalize(value: double.NaN, min: 0, max: 10);
        double centred = new CenteredNormNormalizer(vcenter: 10).Normalize(value: double.NaN, min: 0, max: 10);

        Assert.Equal(0.5, twoSlope);
        Assert.Equal(centred, twoSlope);
    }

    [Theory]
    [InlineData(-3.0, -10.0, 10.0)]
    [InlineData(0.0, -10.0, 10.0)]
    [InlineData(4.5, -10.0, 10.0)]
    [InlineData(40.0, -10.0, 10.0)]
    [InlineData(-5.0, 0.0, 10.0)]
    [InlineData(7.0, 5.0, 5.0)]
    public void TwoSlope_IsTheCentredScaleOverTheDataRange(double value, double min, double max)
    {
        double center = 0.0;
        double twoSlope = new TwoSlopeNormalizer(center).Normalize(value, min, max);
        double centred = new CenteredNormNormalizer(vcenter: center).Normalize(value, min, max);

        Assert.Equal(centred, twoSlope);
    }

    [Fact]
    public void NoNorm_IsSingleton()
    {
        Assert.Same(NoNormNormalizer.Instance, NoNormNormalizer.Instance);
    }
}
