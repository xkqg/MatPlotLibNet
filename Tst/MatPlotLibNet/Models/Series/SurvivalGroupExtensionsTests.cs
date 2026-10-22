// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>The product-limit (Kaplan-Meier) estimator a survival chart draws: S(t) = the product, over the event times t_i
/// at or before t, of (1 - d_i / n_i), where d_i is the number of events at t_i and n_i the number of subjects at risk just
/// before t_i. All the events at one time are counted together, and a subject censored at an event time is still at risk at
/// it (events before censorings). The curve starts at (0, 1), drops at each event time, and is extended to the last
/// follow-up time when that is a censoring. The arithmetic is integer counts, one subtraction, one division and a running
/// product: no logarithm and no exponential, so the levels are asserted as the exact fractions they are, to 1e-15.</summary>
public class SurvivalGroupExtensionsTests
{
    private const double Exact = 1e-15;

    private static SurvivalCurve Curve(double[] durations, bool[] observed) =>
        new SurvivalGroup("A", durations, observed).ProductLimit("groups");

    private static void AssertLevels(double[] expected, double[] actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.True(Math.Abs(expected[i] - actual[i]) <= Exact, $"[{i}]: expected {expected[i]:R}, got {actual[i]:R}");
        }
    }

    // ---- the worked example ---------------------------------------------------------------------------------------

    /// <summary>Six subjects with a tie of two events and a censoring at one time, and a censored last subject. The censored
    /// subject at 3 is listed FIRST, before the two events at 3, so an estimator that takes the subjects in the order given
    /// would drop it from the risk set before the events.
    /// <code>
    ///   subject  duration  observed
    ///      1        3      censored
    ///      2        1      event
    ///      3        3      event
    ///      4        6      censored
    ///      5        3      event
    ///      6        5      event
    ///
    ///   t   at risk n   events d   censored   S(t)
    ///   0       6          0          0       1
    ///   1       6          1          0       1 x (1 - 1/6) = 5/6
    ///   3       5          2          1       5/6 x (1 - 2/5) = 1/2     (the subject censored at 3 is at risk at 3)
    ///   5       2          1          0       1/2 x (1 - 1/2) = 1/4
    ///   6       1          0          1       1/4                       (no event: the curve is extended to 6)
    /// </code>
    /// Censorings before events would have counted 4 at risk at 3: 5/6 x (1 - 2/4) = 5/12.</summary>
    [Fact]
    public void TheWorkedExample_IsTheProductOfOneMinusEventsOverAtRisk()
    {
        var curve = Curve([3, 1, 3, 6, 3, 5], [false, true, true, false, true, true]);

        Assert.Equal([0.0, 1, 3, 5, 6], curve.Times);
        AssertLevels([1, 5.0 / 6, 1.0 / 2, 1.0 / 4, 1.0 / 4], curve.Survival);
    }

    /// <summary>A subject censored at an event time is at risk at that time: the events are counted first. With the
    /// censoring of the worked example taken first, the level at 3 would be 5/12, not 1/2.</summary>
    [Fact]
    public void ASubjectCensoredAtAnEventTime_IsAtRiskAtIt()
    {
        var curve = Curve([3, 3, 3], [false, true, false]);

        // 3 at risk, 1 event: 1 - 1/3 = 2/3 (not 1 - 1/2, which a censoring taken first would give).
        Assert.Equal([0.0, 3], curve.Times);
        AssertLevels([1, 2.0 / 3], curve.Survival);
    }

    /// <summary>All the events at one time are one step: the curve has one point per event time, never a second point at the
    /// same time for the second event of a tie.</summary>
    [Fact]
    public void TiedEvents_AreOneStep()
    {
        var curve = Curve([4, 2, 4, 4, 9], [true, true, true, true, false]);

        // 5 at risk at 2, 1 event: 4/5. 4 at risk at 4, 3 events: 4/5 x 1/4 = 1/5. Censored at 9: extended.
        Assert.Equal([0.0, 2, 4, 9], curve.Times);
        AssertLevels([1, 4.0 / 5, 1.0 / 5, 1.0 / 5], curve.Survival);
    }

    // ---- the ends of the curve ------------------------------------------------------------------------------------

    /// <summary>A group whose every subject is censored has no event: the curve is flat at 1, from 0 to the last
    /// follow-up.</summary>
    [Fact]
    public void AllCensored_IsFlatAtOne_ToTheLastFollowUp()
    {
        var curve = Curve([2, 7, 4], [false, false, false]);

        Assert.Equal([0.0, 7], curve.Times);
        Assert.Equal([1.0, 1], curve.Survival);
    }

    /// <summary>A group whose every subject has the event at one time drops from 1 to 0 at that time, and nothing follows.</summary>
    [Fact]
    public void AllEventsAtOnce_DropToZeroAtThatTime()
    {
        var curve = Curve([4, 4, 4], [true, true, true]);

        Assert.Equal([0.0, 4], curve.Times);
        Assert.Equal([1.0, 0], curve.Survival);
    }

    /// <summary>When the last follow-up is an event, the curve ends at that event's level: no point is added after it, also
    /// when a subject is censored at that same last time.</summary>
    [Fact]
    public void ALastEvent_EndsTheCurve_WithoutAnExtension()
    {
        var lastEvent = Curve([2, 5], [true, true]);
        var censoredWithIt = Curve([2, 5, 5], [true, true, false]);

        Assert.Equal([0.0, 2, 5], lastEvent.Times);
        AssertLevels([1, 1.0 / 2, 0], lastEvent.Survival);
        // 3 at risk at 2: 2/3; 2 at risk at 5 (the censored one too), 1 event: 2/3 x 1/2 = 1/3.
        Assert.Equal([0.0, 2, 5], censoredWithIt.Times);
        AssertLevels([1, 2.0 / 3, 1.0 / 3], censoredWithIt.Survival);
    }

    /// <summary>An event at time 0 drops the curve at 0: it starts at (0, 1) and its first step is a drop at 0.</summary>
    [Fact]
    public void AnEventAtZero_DropsTheCurveAtZero()
    {
        var curve = Curve([0, 2], [true, false]);

        Assert.Equal([0.0, 0, 2], curve.Times);
        AssertLevels([1, 1.0 / 2, 1.0 / 2], curve.Survival);
    }

    /// <summary>One subject is a curve: 1 until its event, 0 from it.</summary>
    [Fact]
    public void OneSubject_IsACurve()
    {
        Assert.Equal([1.0, 0], Curve([3], [true]).Survival);
        Assert.Equal([1.0, 1], Curve([3], [false]).Survival);
    }

    /// <summary>The order the subjects are given in does not matter: the same subjects in any order give the same curve.</summary>
    [Fact]
    public void TheOrderOfTheSubjects_DoesNotMatter()
    {
        var given = Curve([3, 1, 3, 6, 3, 5], [false, true, true, false, true, true]);
        var sorted = Curve([1, 3, 3, 3, 5, 6], [true, true, true, false, true, false]);

        Assert.Equal(sorted.Times, given.Times);
        Assert.Equal(sorted.Survival, given.Survival);
    }

    /// <summary>The estimate is a running product of fractions of whole numbers: the levels are exactly the bits of the same
    /// fractions multiplied in the same order (no logarithm and exponential, as some estimators use, which would leave a last
    /// bit to the platform's maths library).</summary>
    [Fact]
    public void TheLevels_AreTheRunningProductOfTheFractions_BitForBit()
    {
        var curve = Curve([1, 2, 3, 4, 5, 6, 7], [true, false, true, true, false, true, false]);

        double s1 = 6.0 / 7, s3 = s1 * (4.0 / 5), s4 = s3 * (3.0 / 4), s6 = s4 * (1.0 / 2);
        Assert.Equal([0.0, 1, 3, 4, 6, 7], curve.Times);
        Assert.Equal([1, s1, s3, s4, s6, s6], curve.Survival);
    }

    // ---- refusals -------------------------------------------------------------------------------------------------

    /// <summary>A group whose durations or flags are missing altogether is refused by name, not with a null reference.</summary>
    [Fact]
    public void AGroupWithoutDurationsOrFlags_IsRefused_NamingTheGroup()
    {
        var noDurations = Assert.Throws<ArgumentException>(() => new SurvivalGroup("North", null!, [true]).ProductLimit("groups"));
        var noFlags = Assert.Throws<ArgumentException>(() => new SurvivalGroup("North", [1.0], null!).ProductLimit("groups"));

        Assert.Equal("groups", noDurations.ParamName);
        Assert.Equal("Group 'North' carries no durations. (Parameter 'groups')", noDurations.Message);
        Assert.Equal("groups", noFlags.ParamName);
        Assert.Equal("Group 'North' carries no Observed flags. (Parameter 'groups')", noFlags.Message);
    }

    /// <summary>One flag per duration: arrays of different lengths are refused, naming the group and both counts.</summary>
    [Fact]
    public void DurationsAndFlagsOfDifferentLengths_AreRefused()
    {
        var error = Assert.Throws<ArgumentException>(() => new SurvivalGroup("North", [1.0, 2, 3], [true, false]).ProductLimit("groups"));

        Assert.Equal("groups", error.ParamName);
        Assert.Equal("Group 'North' has 3 durations and 2 Observed flags; there must be one flag per duration. (Parameter 'groups')", error.Message);
    }

    /// <summary>A group of no subjects has no curve to estimate.</summary>
    [Fact]
    public void AGroupOfNoSubjects_IsRefused()
    {
        var error = Assert.Throws<ArgumentException>(() => new SurvivalGroup("North", [], []).ProductLimit("groups"));

        Assert.Equal("groups", error.ParamName);
        Assert.Equal("Group 'North' has no subjects; a survival curve is estimated from 1 or more. (Parameter 'groups')", error.Message);
    }

    /// <summary>A duration is a finite time at or above 0: a negative one, a missing one (NaN) and an infinity are refused,
    /// naming the group, the place and the value. A subject without a time cannot be counted at risk.</summary>
    [Theory]
    [InlineData(-1.0, "-1")]
    [InlineData(double.NaN, "NaN")]
    [InlineData(double.PositiveInfinity, "Infinity")]
    [InlineData(double.NegativeInfinity, "-Infinity")]
    public void ADurationThatIsNotAFiniteTimeAtOrAboveZero_IsRefused(double duration, string written)
    {
        var error = Assert.Throws<ArgumentException>(() => new SurvivalGroup("North", [2, duration, 5], [true, true, false]).ProductLimit("groups"));

        Assert.Equal("groups", error.ParamName);
        Assert.Equal($"Group 'North' has a duration of {written} at index 1; a duration is a finite time at or above 0 (a subject without a time cannot be counted at risk). (Parameter 'groups')", error.Message);
    }

    /// <summary>A duration of 0 is a time: a subject censored or with its event at the start.</summary>
    [Fact]
    public void ADurationOfZero_IsATime() =>
        Assert.Equal([0.0, 3], Curve([0, 3], [false, false]).Times);
}
