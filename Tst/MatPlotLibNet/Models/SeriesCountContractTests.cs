// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models;

/// <summary>
/// The series count is a PUBLISHED number — it stands in the README, on the wiki's front page, in the package
/// map and in every awesome-list submission. A number that appears in a dozen places and is maintained in none
/// of them drifts, silently, in the direction that flatters: the count said 82 while the library shipped 83.
///
/// <para>So the count is pinned where it can be MEASURED rather than remembered. Adding a series turns this test
/// red, and the red says exactly which documents now carry a stale number. The alternative — noticing — is not a
/// mechanism.</para>
/// </summary>
public class SeriesCountContractTests
{
    /// <summary>Every concrete, public series the core package ships: what a caller can actually draw. Abstract
    /// bases (<see cref="ChartSeries"/>, <c>XYSeries</c>, <c>PolarSeries</c>, …) are scaffolding, not chart
    /// types, and nothing in the documents ever meant to count them.</summary>
    internal static Type[] ConcreteSeries() =>
        typeof(ChartSeries).Assembly.ConcretePublicSubtypesOf(typeof(ChartSeries));

    /// <summary>The number the documents publish. Changing a series count is a DOCUMENTED change: bump this,
    /// and the list in the failure message tells you which document to bump with it.</summary>
    private const int PublishedSeriesTypes = 91;

    /// <summary>Of those, the streaming family — called out separately wherever the count appears, because a
    /// streaming series is fed rather than plotted.</summary>
    private const int PublishedStreamingSeries = 4;

    [Fact]
    public void TheCorePackageShips_ExactlyTheNumberOfSeriesTheDocumentsClaim()
        => PublishedCounts.AssertAre(ConcreteSeries(), PublishedSeriesTypes);

    [Fact]
    public void TheStreamingFamilyIs_ExactlyTheNumberTheDocumentsCallOutSeparately()
        => PublishedCounts.AssertAre([.. ConcreteSeries().Where(t => t.Name.StartsWith("Streaming", StringComparison.Ordinal))], PublishedStreamingSeries);

    /// <summary>Every series stays under the one series namespace. The documents count CATEGORIES as well as
    /// types, and a series that escaped this root is exactly how a category nobody counted would appear.
    /// <para>Most of the series namespace is flat and the categories are FOLDERS, which the compiler does not
    /// keep — except <c>Streaming</c>, which is a real sub-namespace. So the assertion is the one the compiler
    /// can actually make: nothing sits outside the root.</para></summary>
    [Fact]
    public void EverySeriesStaysUnder_TheOneSeriesNamespace()
    {
        const string root = "MatPlotLibNet.Models.Series";

        foreach (var t in ConcreteSeries())
        {
            Assert.True(t.Namespace is not null && (t.Namespace == root || t.Namespace.StartsWith(root + ".", StringComparison.Ordinal)),
                $"{t.Name} sits in {t.Namespace}");
        }
    }
}
