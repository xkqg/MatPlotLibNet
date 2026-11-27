// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using Xunit.Sdk;

namespace MatPlotLibNet.Tests;

/// <summary>How the tests say two numbers are the same to within a tolerance: one sequence member (absolute), one scalar member
/// (relative, with a floor under the scale), and the same two for a point and a rectangle.</summary>
public sealed class NumberAssertionsTests
{
    [Fact]
    public void ASequence_WithinTheTolerance_PassesElementByElement()
    {
        double[] expected = [1.0, 2.0, 3.0];

        expected.AssertCloseTo([1.0 + 1e-10, 2.0 - 1e-10, 3.0], 1e-9);
        new double[] { 1.0 }.AssertCloseTo([1.0 + 1e-10], 1e-9);
    }

    [Fact]
    public void ASequence_ThatDiffersAtAnIndex_NamesTheIndexAndBothValues()
    {
        var ex = Assert.ThrowsAny<XunitException>(() => new double[] { 1.0, 2.0, 3.0 }.AssertCloseTo([1.0, 2.5, 3.0], 1e-9));

        Assert.Contains("[1]: expected 2, got 2.5", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ASequence_OfAnotherLength_IsRefused()
    {
        Assert.ThrowsAny<XunitException>(() => new double[] { 1.0, 2.0 }.AssertCloseTo([1.0], 1e-9));
    }

    [Fact]
    public void ANaN_EqualsANaN_AndNothingElse()
    {
        new double[] { double.NaN, 1.0 }.AssertCloseTo([double.NaN, 1.0], 1e-9);

        var ex = Assert.ThrowsAny<XunitException>(() => new double[] { double.NaN }.AssertCloseTo([0.0], 1e-9));
        Assert.Contains("[0]: expected NaN, got 0", ex.Message, StringComparison.Ordinal);
        Assert.ThrowsAny<XunitException>(() => new double[] { 0.0 }.AssertCloseTo([double.NaN], 1e-9));
    }

    [Fact]
    public void ATolerance_IsInclusive()
    {
        new double[] { 0.0 }.AssertCloseTo([0.5], 0.5);
    }

    [Fact]
    public void AScalar_WithinItsRelativeTolerance_Passes_AndTheScaleNeverFallsBelowTheFloor()
    {
        1000.0.AssertCloseTo(1000.5, relative: 1e-3, floor: 1.0, what: "big");
        0.0.AssertCloseTo(0.0005, relative: 1e-3, floor: 1.0, what: "small");   // the scale is the floor, 1: 1e-3 wide
        0.0.AssertCloseTo(0.0, relative: 1e-3, floor: 0.0, what: "zero");
    }

    [Fact]
    public void AScalar_OutsideItsRelativeTolerance_NamesWhatItWasAndBothValues()
    {
        var ex = Assert.ThrowsAny<XunitException>(() => 1000.0.AssertCloseTo(1002.0, relative: 1e-3, floor: 1.0, what: "the right edge"));

        Assert.Contains("the right edge: expected 1000, got 1002", ex.Message, StringComparison.Ordinal);
        Assert.ThrowsAny<XunitException>(() => 0.0.AssertCloseTo(0.002, relative: 1e-3, floor: 1.0));
        Assert.ThrowsAny<XunitException>(() => 0.0.AssertCloseTo(1e-9, relative: 1e-3, floor: 0.0));
    }

    [Fact]
    public void AScalar_WithoutAName_StillNamesBothValues()
    {
        var ex = Assert.ThrowsAny<XunitException>(() => 2.0.AssertCloseTo(3.0, relative: 1e-9, floor: 1.0));

        Assert.Contains("expected 2, got 3", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void APointAndARect_AreCloseWhenEveryCoordinateIsWithinTheTolerance()
    {
        new Point(1, 2).AssertCloseTo(new Point(1 + 1e-4, 2 - 1e-4), 1e-3);
        new Rect(1, 2, 3, 4).AssertCloseTo(new Rect(1.0001, 2, 3, 4.0001), 1e-3);
        Assert.True(new Point(1, 2).IsCloseTo(new Point(1.0001, 2), 1e-3));
        Assert.False(new Point(1, 2).IsCloseTo(new Point(1.1, 2), 1e-3));
        Assert.True(new Rect(1, 2, 3, 4).IsCloseTo(new Rect(1, 2, 3, 4.0001), 1e-3));
        Assert.False(new Rect(1, 2, 3, 4).IsCloseTo(new Rect(1, 2, 3.1, 4), 1e-3));
        Assert.ThrowsAny<XunitException>(() => new Point(1, 2).AssertCloseTo(new Point(1, 2.1), 1e-3));
        Assert.ThrowsAny<XunitException>(() => new Rect(1, 2, 3, 4).AssertCloseTo(new Rect(1, 2, 3, 4.1), 1e-3));
    }

    [Fact]
    public void ASequence_ToARelativeTolerance_ComparesEachElementAgainstItsOwnSize_NeverBelowTheFloor()
    {
        new double[] { 1000.0, 0.0 }.AssertCloseTo([1000.5, 0.0005], relative: 1e-3, floor: 1.0);

        var ex = Assert.ThrowsAny<XunitException>(() => new double[] { 1000.0, 0.0 }.AssertCloseTo([1000.5, 0.002], relative: 1e-3, floor: 1.0));
        Assert.Contains("[1]: expected 0, got 0.002", ex.Message, StringComparison.Ordinal);
        Assert.ThrowsAny<XunitException>(() => new double[] { 1.0 }.AssertCloseTo([1.0, 2.0], relative: 1e-3, floor: 1.0));
    }

    [Fact]
    public void ARange_IsCloseWhenBothEndsAre()
    {
        new Range1D(0, 6).AssertCloseTo(new Range1D(1e-10, 6 - 1e-10), 1e-9);

        var ex = Assert.ThrowsAny<XunitException>(() => new Range1D(0, 6).AssertCloseTo(new Range1D(0, 6.1), 1e-9));
        Assert.Contains("expected 0..6, got 0..6.1", ex.Message, StringComparison.Ordinal);
    }
}
