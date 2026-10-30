// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Indicators.Streaming;

/// <summary>The classification map holds the package to a question it can not forget to answer: for every indicator it
/// calculates, is there a streaming form, and if there is none, why. An indicator added to the package without a line in
/// <see cref="StreamingMap"/> turns this red, and the red names it.</summary>
public sealed class StreamingMapTests
{
    [Fact]
    public void TheMap_ClassifiesEveryIndicatorOfThePackage_ExactlyOnce()
    {
        var calculated = IndicatorCountContractTests.Calculators().Select(t => t.Name).Order(StringComparer.Ordinal).ToList();
        var mapped = StreamingMap.All.Select(c => c.Name).Order(StringComparer.Ordinal).ToList();

        var missing = calculated.Except(mapped).ToList();
        var unknown = mapped.Except(calculated).ToList();
        Assert.True(missing.Count == 0 && unknown.Count == 0,
            $"not in the map: [{string.Join(", ", missing)}]; in the map but not calculated: [{string.Join(", ", unknown)}]");
        Assert.Equal(mapped.Count, mapped.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void AnIndicatorThatIsNotStreamed_SaysWhy_AndOneThatIsStreamedSaysHow()
    {
        foreach (var line in StreamingMap.All)
        {
            if (line.IsStreamed)
            {
                Assert.Null(line.Reason);
            }
            else
            {
                Assert.False(string.IsNullOrWhiteSpace(line.Reason), $"{line.Name} is not streamed and does not say why");
            }
        }
    }

    /// <summary>The rule that holds every form to its batch indicator is run on a row for each of them; an indicator the map streams
    /// without a row has a form nothing compares with the batch.</summary>
    [Fact]
    public void EveryIndicatorTheMapStreams_HasARowInTheEqualityTheory()
    {
        var rows = StreamingRows.All.Select(r => r.Name).ToHashSet(StringComparer.Ordinal);

        foreach (var line in StreamingMap.All.Where(c => c.IsStreamed))
            Assert.True(rows.Contains(line.Name), $"{line.Name} is streamed and has no row in the equality theory");
    }

    [Fact]
    public void EveryRowOfTheEqualityTheory_IsAnIndicatorTheMapStreams()
    {
        foreach (var row in StreamingRows.All)
        {
            Assert.True(StreamingMap.Of(row.Name).IsStreamed, $"{row.Name} has a row in the theory but the map says it is not streamed");
        }
    }
}
