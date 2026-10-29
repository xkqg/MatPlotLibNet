// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>The regularised incomplete beta function I_x(a, b) against scipy (<c>betainc</c>): the piece Student's t
/// distribution is built from.</summary>
public class BetaTests
{
    [Theory]
    [InlineData(0.5, 0.5, 0.001, 0.020135041633377492)]
    [InlineData(0.5, 0.5, 0.3, 0.36901011956554536)]
    [InlineData(0.5, 0.5, 0.5, 0.5000000000000001)]
    [InlineData(0.5, 0.5, 0.9, 0.7951672353008665)]
    [InlineData(0.5, 0.5, 0.999, 0.9798649583666235)]
    [InlineData(2.0, 3.0, 0.001, 5.992003e-06)]
    [InlineData(2.0, 3.0, 0.3, 0.34829999999999994)]
    [InlineData(2.0, 3.0, 0.5, 0.6875)]
    [InlineData(2.0, 3.0, 0.9, 0.9963)]
    [InlineData(2.0, 3.0, 0.999, 0.999999996003)]
    [InlineData(0.5, 2.5, 0.001, 0.05365765469181354)]
    [InlineData(0.5, 2.5, 0.3, 0.7968893362799453)]
    [InlineData(0.5, 2.5, 0.5, 0.9244131815783876)]
    [InlineData(0.5, 2.5, 0.9, 0.9988855624584926)]
    [InlineData(0.5, 2.5, 0.999, 0.9999999892592646)]
    [InlineData(10.0, 10.0, 0.001, 9.162494537926629e-26)]
    [InlineData(10.0, 10.0, 0.3, 0.032553356881300934)]
    [InlineData(10.0, 10.0, 0.5, 0.5)]
    [InlineData(10.0, 10.0, 0.9, 0.9999960701176729)]
    [InlineData(10.0, 10.0, 0.999, 1.0)]
    [InlineData(1.0, 1.0, 0.001, 0.001)]
    [InlineData(1.0, 1.0, 0.3, 0.3)]
    [InlineData(1.0, 1.0, 0.5, 0.5)]
    [InlineData(1.0, 1.0, 0.9, 0.9)]
    [InlineData(1.0, 1.0, 0.999, 0.999)]
    public void TheRegularisedIncomplete_MatchesScipy(double a, double b, double x, double expected)
    {
        double actual = Beta.RegularisedIncomplete(a, b, x);
        Assert.True(Math.Abs(actual - expected) <= 1e-12 * Math.Max(1e-300, Math.Abs(expected)) + 1e-300, $"expected {expected:R}, got {actual:R}");
    }

    [Fact]
    public void TheRegularisedIncomplete_RunsFromZeroToOne()
    {
        Assert.Equal(0, Beta.RegularisedIncomplete(2, 3, 0));
        Assert.Equal(1, Beta.RegularisedIncomplete(2, 3, 1));
        Assert.Equal(0, Beta.RegularisedIncomplete(2, 3, -0.5));
        Assert.Equal(1, Beta.RegularisedIncomplete(2, 3, 1.5));
    }

    [Fact]
    public void TheRegularisedIncomplete_IsOneMinusTheMirror()
    {
        Assert.Equal(1 - Beta.RegularisedIncomplete(3, 2, 0.7), Beta.RegularisedIncomplete(2, 3, 0.3), 12);
    }

    [Theory]
    [InlineData(0.0, 1.0)]
    [InlineData(1.0, -1.0)]
    [InlineData(double.NaN, 1.0)]
    public void ANonPositiveShape_IsNaN(double a, double b) => Assert.True(double.IsNaN(Beta.RegularisedIncomplete(a, b, 0.5)));

    [Fact]
    public void AMissingPoint_IsNaN() => Assert.True(double.IsNaN(Beta.RegularisedIncomplete(2, 3, double.NaN)));
}
