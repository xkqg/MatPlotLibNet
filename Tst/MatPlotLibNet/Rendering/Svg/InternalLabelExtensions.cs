// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using MatPlotLibNet.Tests.Packaging;

namespace MatPlotLibNet.Tests.Rendering.Svg;

/// <summary>One internal label found in a piece of emitted text: what kind it is and the words that show it.</summary>
/// <param name="Kind">What the words are: a phase label, a version number, a date, a history note, a test or tool name, a roadmap or source-tree path.</param>
/// <param name="Text">The line of the text that carries the label, trimmed.</param>
public readonly record struct InternalLabelHit(string Kind, string Text);

/// <summary>A kind of internal label and the pattern that finds it.</summary>
/// <param name="Kind">What the words are.</param>
/// <param name="Pattern">The words that show it.</param>
public readonly record struct LabelRule(string Kind, Regex Pattern);

/// <summary>
/// Finds the labels of the project's own history in text a reader of the product receives: the phase of a plan, a
/// version number, a calendar date, a "pre-fix" note, a test class or a tool the bug was found with, a row of the
/// roadmap, a path in the source tree. A picture, a script or a page is the product; how it came about is not.
/// </summary>
public static class InternalLabelExtensions
{
    /// <summary>The kinds of label the documentation of the library refuses as well as every page, script and picture it emits: the word
    /// Phase followed by a letter or a number (the word alone is the phase of a Hilbert transform), a calendar date of this project's years,
    /// a version number, the name of a test class or of a tool the bug was found with, and the follow-on of a plan. Each is decided here once; the documentation test
    /// reads these rows and adds its own.</summary>
    public static IReadOnlyList<LabelRule> SharedKinds { get; } =
    [
        new("a phase label", new Regex(@"\bPhase[ -](?:[A-Z]\b|\d)", RegexOptions.CultureInvariant)),
        new("a version number", new Regex(@"\bv\d+\.\d+", RegexOptions.CultureInvariant)),
        new("a date", new Regex(@"\b20\d{2}-\d{2}-\d{2}\b", RegexOptions.CultureInvariant)),
        new("a test or tool name", new Regex(@"\b[A-Z]\w*Tests\w*\b|\bPlaywright\b|\bJint\b", RegexOptions.CultureInvariant)),
        new("a plan reference", new Regex(@"follow-on", RegexOptions.CultureInvariant)),
    ];

    private static readonly LabelRule[] Rules =
    [
        .. SharedKinds,
        new("a promise of later work", new Regex(@"follow-up", RegexOptions.CultureInvariant)),
        new("a history note", new Regex(@"\bPre-(fix|F|Phase)\b|\b[Uu]ser[- ]reported|\b[Uu]ser explicitly approved|\b[Rr]egression tests?\b", RegexOptions.CultureInvariant)),
        new("a roadmap or source-tree path", new Regex(@"\bROADMAP\b|\bSrc/", RegexOptions.CultureInvariant)),
    ];

    /// <summary>The text of every <c>&lt;script&gt;</c> element of an SVG, one after the other; empty when it has none.
    /// A picture's own words (a title, a tick label) may be a date or a version, so the pictures are held to this part.</summary>
    /// <param name="svg">An SVG document.</param>
    public static string ScriptText(this string svg) =>
        string.Join('\n', svg.Scripts());

    /// <summary>Every line of <paramref name="text"/> that carries an internal label, with the kind of label it is.</summary>
    /// <param name="text">An SVG, a script or a page as the library writes it.</param>
    /// <returns>One hit per label found; empty when the text is free of them.</returns>
    public static IReadOnlyList<InternalLabelHit> InternalLabels(this string text)
    {
        var hits = new List<InternalLabelHit>();
        foreach (var line in SourceFile.LinesOf(text))
        {
            foreach (var rule in Rules)
            {
                if (rule.Pattern.IsMatch(line))
                {
                    hits.Add(new InternalLabelHit(rule.Kind, line.Trim()));
                }
            }
        }

        return hits;
    }
}
