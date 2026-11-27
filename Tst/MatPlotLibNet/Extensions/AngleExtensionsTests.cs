// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Extensions;

/// <summary>The one answer to "this angle in the other unit": degrees to radians and back. Every site that drew with
/// <c>angle * Math.PI / 180</c> keeps the picture it drew, so the conversion is held bit for bit to that expression (the two-operation
/// form the standard library's <c>double.DegreesToRadians</c> uses too), not to a multiply by a precomputed factor, which differs in
/// the last bit for about one input in four.</summary>
public sealed class AngleExtensionsTests
{
    /// <summary>Every whole degree over two turns each way, the angles a chart names, then values a draw produces.</summary>
    private static IReadOnlyList<double> Angles { get; } =
    [
        .. Enumerable.Range(-720, 1441).Select(degrees => (double)degrees),
        .. RandomAngles(new Random(20261116), 500),
        0.1, double.Epsilon, 1e300,
    ];

    private static IEnumerable<double> RandomAngles(Random random, int count)
    {
        for (int i = 0; i < count; i++) yield return (random.NextDouble() - 0.5) * 1440;
    }

    /// <summary>The two conversions, each with the two-operation expression it is held to.</summary>
    public static TheoryData<Func<double, double>, Func<double, double>> Conversions => new()
    {
        { degrees => degrees.ToRadians(), degrees => degrees * Math.PI / 180.0 },
        { radians => radians.ToDegrees(), radians => radians * 180.0 / Math.PI },
    };

    /// <summary>One sweep over every angle, each one that differs in a bit named.</summary>
    [Theory]
    [MemberData(nameof(Conversions))]
    public void Conversion_IsBitForBitTheTwoOperationExpression(Func<double, double> convert, Func<double, double> expression) =>
        Assert.All(Angles, angle => Assert.Equal(BitConverter.DoubleToInt64Bits(expression(angle)), BitConverter.DoubleToInt64Bits(convert(angle))));

    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(90.0, Math.PI / 2)]
    [InlineData(180.0, Math.PI)]
    [InlineData(-180.0, -Math.PI)]
    [InlineData(360.0, 2 * Math.PI)]
    public void ToRadians_NamesTheKnownAngles(double degrees, double radians)
    {
        Assert.Equal(radians, degrees.ToRadians(), 15);
    }

    [Fact]
    public void ToDegrees_UndoesToRadians_ToTheLastDigits()
    {
        Assert.Equal(37.5, 37.5.ToRadians().ToDegrees(), 12);
    }

    [Fact]
    public void ANumberThatIsNotAnAngle_StaysSo()
    {
        Assert.True(double.IsNaN(double.NaN.ToRadians()));
        Assert.True(double.IsPositiveInfinity(double.PositiveInfinity.ToRadians()));
        Assert.True(double.IsNaN(double.NaN.ToDegrees()));
    }
}
