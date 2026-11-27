// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Tests.Packaging;

namespace MatPlotLibNet.Tests.Models;

/// <summary><see cref="SeriesCountContractTests"/> pins the number the assembly ships; this holds the repository's
/// documents to it. The count stands in the README, the architecture notes, the cookbook, the package readmes and the
/// roadmap, and the only thing that kept those in step was a red message that named the documents to look at.
/// The changelog is history and says what each release counted then, so it is not read.</summary>
public class SeriesCountInDocumentsContractTests
{
    [Fact]
    public void EveryDocument_StatesTheNumberOfSeriesTheAssemblyShips()
    {
        int shipped = SeriesCountContractTests.ConcreteSeries().Length;

        var stale = RepositoryTree.CurrentStateDocuments()
            .SelectMany(f => SeriesCountPhrases.In(f.Text)
                .Where(p => p.Count != shipped)
                .Select(p => $"{f.Path}: \"{p.Phrase}\""))
            .ToArray();

        Assert.True(stale.Length == 0,
            $"the assembly ships {shipped} series types; these documents say otherwise:\n" + string.Join("\n", stale));
    }

    [Fact]
    public void TheDocuments_StateTheSeriesCountAtAll()
    {
        // A reader that finds nothing would pass the test above for any count; the README states it, and it is read.
        Assert.Contains(RepositoryTree.CurrentStateDocuments().SelectMany(f => SeriesCountPhrases.In(f.Text)),
            p => p.Count == SeriesCountContractTests.ConcreteSeries().Length);
    }
}
