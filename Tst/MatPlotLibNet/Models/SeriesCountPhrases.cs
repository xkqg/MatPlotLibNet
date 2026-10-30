// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;

namespace MatPlotLibNet.Tests.Models;

/// <summary>The sentences in which a document says how many series types the library ships: "86 chart types",
/// "all 86 series", "Two of the 86 series types". One reader for every place that has to agree with the count, so the
/// repository's documents and the wiki's pages are held to the same wording. A count written in words ("eighty-six")
/// cannot be read, so a document states it in digits. A number under seventy is not the total: it is a family ("12 series types"
/// of the 3-D charts, "4 chart types" in a sample) or a figure from the history of a design note, and the library has shipped more
/// than seventy for many releases.</summary>
internal static class SeriesCountPhrases
{
    private static readonly Regex[] Patterns =
    [
        new(@"(?<n>[7-9]\d|\d{3}) (?:series|chart) types", RegexOptions.CultureInvariant),
        new(@"\b[Aa]ll (?<n>[7-9]\d|\d{3}) series\b", RegexOptions.CultureInvariant),
    ];

    /// <summary>Every count <paramref name="text"/> states for the series types, with the phrase it stands in.</summary>
    internal static IEnumerable<(string Phrase, int Count)> In(string text) =>
        Patterns.SelectMany(p => p.Matches(text).Select(m =>
            (m.Value, int.Parse(m.Groups["n"].Value, CultureInfo.InvariantCulture))));
}
