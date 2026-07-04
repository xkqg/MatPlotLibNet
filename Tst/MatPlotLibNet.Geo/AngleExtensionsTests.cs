// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Geo.Tests;

/// <summary>Pin tests for <see cref="AngleExtensions"/> — the degrees/radians
/// extension pair introduced to DRY out the <c>DegToRad</c>/<c>RadToDeg</c> private consts that
/// were duplicated across the projection files.</summary>
public class AngleExtensionsTests
{
    // ── ToRadians ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(180.0, Math.PI)]
    [InlineData(90.0, Math.PI / 2)]
    [InlineData(360.0, 2 * Math.PI)]
    [InlineData(-90.0, -Math.PI / 2)]
    public void ToRadians_ConvertsKnownDegreeValues(double degrees, double expectedRadians)
    {
        double actual = degrees.ToRadians();
        Assert.Equal(expectedRadians, actual, precision: 12);
    }

    [Fact]
    public void ToRadians_MatchesFormula()
    {
        double degrees = 37.5;
        double expected = degrees * Math.PI / 180.0;
        Assert.Equal(expected, degrees.ToRadians(), precision: 15);
    }

    // ── ToDegrees ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(Math.PI, 180.0)]
    [InlineData(Math.PI / 2, 90.0)]
    [InlineData(2 * Math.PI, 360.0)]
    [InlineData(-Math.PI / 2, -90.0)]
    public void ToDegrees_ConvertsKnownRadianValues(double radians, double expectedDegrees)
    {
        double actual = radians.ToDegrees();
        Assert.Equal(expectedDegrees, actual, precision: 12);
    }

    [Fact]
    public void ToDegrees_MatchesFormula()
    {
        double radians = 0.654;
        double expected = radians * 180.0 / Math.PI;
        Assert.Equal(expected, radians.ToDegrees(), precision: 15);
    }

    // ── Round-trip precision ────────────────────────────────────────────────

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    [InlineData(-1.0)]
    [InlineData(29.5)]
    [InlineData(45.5)]
    [InlineData(89.99)]
    [InlineData(-89.99)]
    [InlineData(180.0)]
    [InlineData(-180.0)]
    [InlineData(360.0)]
    [InlineData(123.456789)]
    public void RoundTrip_ToRadiansThenToDegrees_RecoversOriginalWithin1e12(double x)
    {
        double roundTripped = x.ToRadians().ToDegrees();
        Assert.Equal(x, roundTripped, precision: 12);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    [InlineData(-1.0)]
    [InlineData(Math.PI)]
    [InlineData(Math.PI / 2)]
    [InlineData(2 * Math.PI)]
    public void RoundTrip_ToDegreesThenToRadians_RecoversOriginalWithin1e12(double x)
    {
        double roundTripped = x.ToDegrees().ToRadians();
        Assert.Equal(x, roundTripped, precision: 12);
    }
}
