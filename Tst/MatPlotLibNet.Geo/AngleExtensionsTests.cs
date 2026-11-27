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

    /// <summary>Each conversion matches the formula it stands for, at one angle.</summary>
    public static TheoryData<double, Func<double, double>, Func<double, double>> Formulas => new()
    {
        { 37.5, degrees => degrees.ToRadians(), degrees => degrees * Math.PI / 180.0 },
        { 0.654, radians => radians.ToDegrees(), radians => radians * 180.0 / Math.PI },
    };

    [Theory]
    [MemberData(nameof(Formulas))]
    public void Conversion_MatchesFormula(double angle, Func<double, double> convert, Func<double, double> formula) =>
        Assert.Equal(formula(angle), convert(angle), precision: 15);

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
