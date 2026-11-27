// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests;

/// <summary>A number the documents publish (how many series, how many indicators, how many of them stream) held against what the assembly
/// carries: the failure names every type found, so the reader sees which document to bump with the count.</summary>
internal static class PublishedCounts
{
    /// <summary>Checks that the assembly carries exactly <paramref name="published"/> of <paramref name="found"/>.</summary>
    /// <param name="found">The types the assembly carries.</param>
    /// <param name="published">The number the documents publish.</param>
    internal static void AssertAre(Type[] found, int published) =>
        Assert.True(found.Length == published,
            $"the published count is {published}; the assembly carries {found.Length}: " + string.Join(", ", found.Select(t => t.Name)));
}
