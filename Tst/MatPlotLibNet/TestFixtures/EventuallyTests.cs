// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests;

/// <summary>The wait every test that watches another thread uses: it returns when the condition holds and fails when it does not
/// within the time given, instead of letting the test go on to assert on a state it never reached.</summary>
public sealed class EventuallyTests
{
    [Fact]
    public async Task AConditionThatAlreadyHolds_ReturnsAtOnce()
    {
        await Eventually.HoldsAsync(() => true, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task AConditionAnotherThreadMakesTrue_ReturnsWhenItHolds()
    {
        int count = 0;
        _ = Task.Run(async () =>
        {
            await Task.Delay(30, TestContext.Current.CancellationToken);
            Interlocked.Exchange(ref count, 3);
        }, TestContext.Current.CancellationToken);

        await Eventually.HoldsAsync(() => Volatile.Read(ref count) == 3, TimeSpan.FromSeconds(5));

        Assert.Equal(3, count);
    }

    [Fact]
    public async Task AConditionThatNeverHolds_FailsWithATimeout_AfterTheTimeGiven()
    {
        var thrown = await Assert.ThrowsAsync<TimeoutException>(() => Eventually.HoldsAsync(() => false, TimeSpan.FromMilliseconds(50)));

        Assert.Contains("50", thrown.Message);
    }
}
