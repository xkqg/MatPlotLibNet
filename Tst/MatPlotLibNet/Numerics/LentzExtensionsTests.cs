// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>The guard of the modified Lentz method, which <see cref="Gamma"/> and <see cref="Beta"/> share: a term whose
/// magnitude is below 1e-300 is replaced by 1e-300 (whatever its sign), so the division that follows it is finite; any other
/// term, a missing number included, is left as it is. The guard is one rule held in one place, so a term that no input of a
/// distribution reaches is still held to it here.</summary>
public class LentzExtensionsTests
{
    [Theory]
    [InlineData(0.0)]
    [InlineData(1e-301)]
    [InlineData(-1e-301)]
    public void ATermBelowTheTinyNumber_IsReplacedByIt_WhateverItsSign(double term) =>
        Assert.Equal(LentzExtensions.Tiny, term.AwayFromZero());

    [Theory]
    [InlineData(1e-299)]
    [InlineData(-1e-299)]
    [InlineData(1.0)]
    [InlineData(-2.5)]
    public void AnyOtherTerm_IsKept(double term) => Assert.Equal(term, term.AwayFromZero());

    [Fact]
    public void AMissingTerm_StaysMissing() => Assert.True(double.IsNaN(double.NaN.AwayFromZero()));

    [Fact]
    public void TheTinyNumber_IsOneEMinusThreeHundred() => Assert.Equal(1e-300, LentzExtensions.Tiny);
}
