// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Which finite values an axis has a place for: every one, except 0 and the negative values on a log axis. The
/// mapping into the drawn space, the range an axis folds and the clipping of a mark's base all ask this one rule.</summary>
public class AxisScaleExtensionsTests
{
    [Theory]
    [InlineData(AxisScale.Log, 1e-300, true)]
    [InlineData(AxisScale.Log, 10.0, true)]
    [InlineData(AxisScale.Log, 0.0, false)]
    [InlineData(AxisScale.Log, -3.0, false)]
    [InlineData(AxisScale.SymLog, -3.0, true)]
    [InlineData(AxisScale.SymLog, 0.0, true)]
    [InlineData(AxisScale.Linear, -3.0, true)]
    [InlineData(AxisScale.Logit, 0.0, true)]   // the Cartesian renderer draws a logit axis linearly
    [InlineData(AxisScale.Date, -3.0, true)]
    public void AScale_HasAPlaceForEveryFiniteValue_ButZeroAndBelowOnALogAxis(AxisScale scale, double value, bool expected) =>
        Assert.Equal(expected, scale.HasPlaceFor(value));

    [Fact]
    public void ALogScale_HasNoPlaceForNegativeZero() => Assert.False(AxisScale.Log.HasPlaceFor(-0.0));

    /// <summary>The mapping says the same: a value the scale has no place for maps to NaN, and only such a value does.</summary>
    [Theory]
    [InlineData(AxisScale.Log, 0.0)]
    [InlineData(AxisScale.Log, -2.0)]
    [InlineData(AxisScale.Log, 5.0)]
    [InlineData(AxisScale.SymLog, -2.0)]
    [InlineData(AxisScale.Linear, -2.0)]
    public void TheForwardMapping_IsNaN_ExactlyWhereTheScaleHasNoPlace(AxisScale scale, double value) =>
        Assert.Equal(!scale.HasPlaceFor(value), double.IsNaN(scale.Forward(value, 1)));
}
