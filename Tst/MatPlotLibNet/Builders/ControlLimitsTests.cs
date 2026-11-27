// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The limits of a control chart given by the caller: three finite numbers in the order Lower, Center, Upper, refused
/// by name and by their values otherwise.</summary>
public class ControlLimitsTests
{
    [Theory]
    [InlineData(50.0, 49.0, 51.0)]
    [InlineData(5.0, 5.0, 5.0)]
    [InlineData(0.0, -1.0, 0.0)]
    public void LimitsInOrder_AreHandedBack(double center, double lower, double upper)
    {
        var limits = new ControlLimits(center, lower, upper);

        Assert.Equal(limits, limits.ThrowIfNotOrdered());
    }

    [Theory]
    [InlineData(48.0, 49.0, 51.0, "The centre line must be in [49, 51] (got 48).")]
    [InlineData(52.0, 49.0, 51.0, "The centre line must be in [49, 51] (got 52).")]
    [InlineData(50.0, 51.0, 49.0, "The upper limit must be >= 51 (got 49).")]
    [InlineData(double.NaN, 49.0, 51.0, "The centre line must be finite (got NaN).")]
    [InlineData(50.0, double.NegativeInfinity, 51.0, "The lower limit must be finite (got -Infinity).")]
    [InlineData(50.0, 49.0, double.PositiveInfinity, "The upper limit must be finite (got Infinity).")]
    public void LimitsOutOfOrderOrNotFinite_AreRefusedWithTheirValues(double center, double lower, double upper, string named)
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => new ControlLimits(center, lower, upper).ThrowIfNotOrdered());

        Assert.Equal("limits", error.ParamName);
        Assert.StartsWith(named, error.Message, StringComparison.Ordinal);
    }
}
