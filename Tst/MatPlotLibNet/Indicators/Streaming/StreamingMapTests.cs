// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Streaming;

namespace MatPlotLibNet.Tests.Indicators.Streaming;

/// <summary>The classification map holds the package to a question it can not forget to answer: for every indicator it
/// calculates, is there a streaming form, and if there is none, why. That the map holds every calculator once is the guard of
/// <see cref="IndicatorContractTests"/>; these hold what a line of the map says.</summary>
public sealed class StreamingMapTests
{
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

    /// <summary>The kind the map gives a form is the kind it is built as: a form that keeps a window holds its inputs in the one ring
    /// (<c>InputWindow</c>, directly, through <see cref="WindowedStreamingIndicator{TInput}"/> or through a step) or in the window of
    /// a rolling extreme; a form that carries a state (a recursion, a state machine, a running total) holds a step its batch indicator
    /// folds too; a multivariate form is given a row of values.</summary>
    [Theory]
    [MemberData(nameof(StreamingRows.Names), MemberType = typeof(StreamingRows))]
    public void EveryForm_IsBuiltAsTheKindTheMapGivesIt(string name)
    {
        var form = StreamingRows.Of(name).Make();
        var held = Held(form.GetType(), form, depth: 0).ToList();
        bool keepsAWindow = held.Any(t => t.IsGenericType && t.GetGenericTypeDefinition().Name is "InputWindow`1" or "RollingExtremeStep`1");
        bool carriesAStep = held.Any(t => t.Namespace == "MatPlotLibNet.Indicators.Steps");

        switch (StreamingMap.Of(name).Kind)
        {
            case StreamingKind.Window:
                Assert.True(keepsAWindow, $"{form.GetType().Name} is mapped as a window and keeps none");
                break;
            case StreamingKind.Multivariate:
                Assert.True(form is StreamingIndicator<double[]>, $"{form.GetType().Name} is mapped as multivariate and is not given rows");
                break;
            default:
                Assert.True(carriesAStep, $"{form.GetType().Name} is mapped as {StreamingMap.Of(name).Kind} and carries no step its batch folds");
                break;
        }
    }

    /// <summary>The types of every field a form holds, through the fields of the library's own structs and of its bases.</summary>
    private static IEnumerable<Type> Held(Type type, object? value, int depth)
    {
        if (depth > 4 || value is null) yield break;
        for (var t = type; t is not null && t != typeof(object); t = t.BaseType)
        {
            foreach (var field in t.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.DeclaredOnly))
            {
                yield return field.FieldType;
                if (field.FieldType.IsValueType && field.FieldType.Assembly == typeof(IStreamingIndicator).Assembly)
                {
                    foreach (var inner in Held(field.FieldType, field.GetValue(value), depth + 1)) yield return inner;
                }
            }
        }
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
