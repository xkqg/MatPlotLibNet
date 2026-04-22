// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies the <see cref="MultivariateIndicator{TResult}"/> base class — null/empty
/// handling, rectangular validation, and property exposure. Exercised through a tiny
/// concrete subclass below.</summary>
public class MultivariateIndicatorTests
{
    private sealed class NoopMultivariate : MultivariateIndicator<SignalResult>
    {
        public NoopMultivariate(double[][] features) : base(features) { }
        public override SignalResult Compute() => Array.Empty<double>();
        public override void Apply(Axes axes) { }

        public int PublicBarCount => BarCount;
        public int PublicFeatureCount => FeatureCount;
        public double[][] PublicFeatures => Features;
    }

    [Fact]
    public void Constructor_NullFeatures_YieldsEmptyState()
    {
        var ind = new NoopMultivariate(null!);
        Assert.Empty(ind.PublicFeatures);
        Assert.Equal(0, ind.PublicBarCount);
        Assert.Equal(0, ind.PublicFeatureCount);
    }

    [Fact]
    public void Constructor_EmptyFeatures_YieldsEmptyState()
    {
        var ind = new NoopMultivariate([]);
        Assert.Empty(ind.PublicFeatures);
        Assert.Equal(0, ind.PublicBarCount);
        Assert.Equal(0, ind.PublicFeatureCount);
    }

    [Fact]
    public void Constructor_RectangularFeatures_StoresCorrectly()
    {
        var features = new[] { new[] { 1.0, 2.0, 3.0 }, new[] { 4.0, 5.0, 6.0 } };
        var ind = new NoopMultivariate(features);
        Assert.Same(features, ind.PublicFeatures);
        Assert.Equal(2, ind.PublicBarCount);
        Assert.Equal(3, ind.PublicFeatureCount);
    }

    [Fact]
    public void Constructor_NonRectangularFeatures_Throws()
    {
        var features = new[] { new[] { 1.0, 2.0 }, new[] { 3.0, 4.0, 5.0 } };
        Assert.Throws<ArgumentException>(() => new NoopMultivariate(features));
    }

    [Fact]
    public void Constructor_SingleBarSingleFeature_Valid()
    {
        var ind = new NoopMultivariate([[42.0]]);
        Assert.Equal(1, ind.PublicBarCount);
        Assert.Equal(1, ind.PublicFeatureCount);
    }
}
