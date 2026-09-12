// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using Xunit;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>
/// The indicator count is a PUBLISHED number, exactly like the series count, and it drifted for the same reason:
/// it was maintained by remembering. The README and the wiki's front page both said 53 while the assembly carried
/// 58, so this pins it where it can be MEASURED. Adding an indicator turns this test red, and the red names the
/// documents that now carry a stale number.
/// </summary>
public class IndicatorCountContractTests
{
    /// <summary>An indicator is a thing a caller can CALCULATE. The namespace also holds the records that carry a
    /// result (<c>MacdResult</c>, <c>AdxResult</c>, …), the interfaces and the generic bases; those are
    /// scaffolding around an indicator, never one themselves, and no document ever meant to count them.</summary>
    private static Type[] Calculators() =>
        [.. typeof(MatPlotLibNet.Indicators.Sma).Assembly
            .GetTypes()
            .Where(t => t.IsPublic && t.Namespace == "MatPlotLibNet.Indicators" && !t.IsGenericTypeDefinition
                        && t.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                            .Any(m => m.Name is "Calculate" or "Compute"))
            .OrderBy(t => t.Name, StringComparer.Ordinal)];

    /// <summary>The streaming tier, called out separately wherever the count appears, because a streaming
    /// indicator is fed one bar at a time instead of handed an array.</summary>
    private static Type[] StreamingIndicators() =>
        [.. typeof(MatPlotLibNet.Indicators.Sma).Assembly
            .GetTypes()
            .Where(t => t.IsPublic && t.IsClass && !t.IsAbstract
                        && t.Namespace == "MatPlotLibNet.Indicators.Streaming"
                        && t.Name.StartsWith("Streaming", StringComparison.Ordinal))
            .OrderBy(t => t.Name, StringComparer.Ordinal)];

    /// <summary>The number the documents publish. Changing it is a DOCUMENTED change: bump this, and bump
    /// README.md and the wiki's Home page with it.</summary>
    private const int PublishedIndicators = 58;

    /// <summary>Of those, the ones that also run incrementally.</summary>
    private const int PublishedStreamingIndicators = 11;

    [Fact]
    public void TheCorePackageShips_ExactlyTheNumberOfIndicatorsTheDocumentsClaim()
    {
        var indicators = Calculators();

        Assert.True(indicators.Length == PublishedIndicators,
            $"the published count is {PublishedIndicators}; the assembly carries {indicators.Length}: " +
            string.Join(", ", indicators.Select(t => t.Name)));
    }

    [Fact]
    public void TheStreamingTierIs_ExactlyTheNumberTheDocumentsCallOutSeparately()
    {
        var streaming = StreamingIndicators();

        Assert.True(streaming.Length == PublishedStreamingIndicators,
            $"expected {PublishedStreamingIndicators}; found {streaming.Length}: " +
            string.Join(", ", streaming.Select(t => t.Name)));
    }

    /// <summary>Every streaming indicator answers to an indicator that can also be calculated over an array, so a
    /// reader who meets one in the streaming list can look it up in the other. A streaming type with no
    /// batch counterpart would be a name that appears in one document and nowhere else.</summary>
    [Fact]
    public void EveryStreamingIndicator_HasABatchCounterpartOfTheSameName()
    {
        var batch = Calculators().Select(t => t.Name).ToHashSet(StringComparer.Ordinal);

        foreach (var t in StreamingIndicators())
        {
            string bare = t.Name["Streaming".Length..];
            Assert.True(batch.Contains(bare) || batch.Any(b => b.StartsWith(bare, StringComparison.Ordinal)),
                $"{t.Name} has no batch indicator called {bare}");
        }
    }
}
