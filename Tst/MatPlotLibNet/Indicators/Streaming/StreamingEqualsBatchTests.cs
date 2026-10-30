// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Indicators.Streaming;

/// <summary>The one rule every streaming form is held to, for every row of the classification map: after the bar at index
/// <c>i</c>, each output line holds what the batch indicator gives as its last value over the bars <c>0..i</c>, to
/// <c>1e-9 × max(1, |batch|)</c>. The comparison is made after every bar, on a batch run over that prefix each time, so a form
/// that agrees only once it has warmed up, or only on a series that never repeats a close, or only with the whole series in hand,
/// does not agree. Nothing is skipped: the lengths of the lines, the number of values compared and the warm-up the form states
/// are asserted as well.</summary>
public sealed class StreamingEqualsBatchTests
{
    public static TheoryData<string> Names() => StreamingRows.Names();

    [Theory]
    [MemberData(nameof(Names))]
    public void TheStreamingForm_SaysWhatTheBatchSays_AfterEveryBar(string name) =>
        StreamingEquality.AssertAgrees(StreamingRows.Of(name));

    [Fact]
    public void EveryRow_HasAnIndicatorOfItsOwn_AndTheMapHoldsNoneTwice()
    {
        var names = StreamingRows.All.Select(r => r.Name).ToList();

        Assert.Equal(names.Distinct(StringComparer.Ordinal).Count(), names.Count);
    }
}
