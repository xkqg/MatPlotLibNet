// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests;

/// <summary>How the tests say a line in runs is written in its one form: a gap stands between two values, never first, never last, never
/// next to another gap. The refusal names the gap and the line.</summary>
public sealed class GapAssertionsTests
{
    private const double Gap = double.NaN;

    [Theory]
    [InlineData(new double[0], null)]
    [InlineData(new double[] { 1, 2 }, null)]
    [InlineData(new double[] { 1, Gap, 2, Gap, 3 }, null)]
    [InlineData(new[] { Gap, 1 }, "gap at 0 of 2")]
    [InlineData(new[] { 1, Gap }, "gap at 1 of 2")]
    [InlineData(new[] { 1, Gap, Gap, 2 }, "gap at 1 of 4")]
    [InlineData(new[] { 1, 2, Gap, 3, Gap, Gap, 4 }, "gap at 4 of 7")]
    public void AGapStandsBetweenValues_OrIsRefusedByNameAndPlace_WhenFirstLastOrBesideAnotherGap(double[] line, string? refused)
    {
        var ex = Record.Exception(() => line.AssertGapsStandBetweenValues("the line"));

        Assert.Equal(refused is null, ex is null);
        Assert.True(refused is null || ex!.Message.Contains($"the line: a {refused}", StringComparison.Ordinal), ex?.Message);
    }
}
