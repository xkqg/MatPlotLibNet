// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace MatPlotLibNet.Tests.Rendering.Svg;

/// <summary>One internal label found in a piece of emitted text: what kind it is and the words that show it.</summary>
/// <param name="Kind">What the words are: a phase label, a version number, a date, a history note, a test or tool name.</param>
/// <param name="Text">The line of the text that carries the label, trimmed.</param>
public readonly record struct InternalLabelHit(string Kind, string Text);

/// <summary>
/// Finds the labels of the project's own history in text a reader of the product receives: the phase of a plan, a
/// version number, a calendar date, a "pre-fix" note, a test class or a tool the bug was found with. A picture, a
/// script or a page is the product; how it came about is not.
/// </summary>
public static class InternalLabelExtensions
{
    private readonly record struct Rule(string Kind, Regex Pattern);

    private static readonly Regex ScriptElement = new(@"<script\b[^>]*>(?<body>.*?)</script>",
        RegexOptions.Singleline | RegexOptions.CultureInvariant);

    private static readonly Rule[] Rules =
    [
        new("a phase label", new Regex(@"\bPhase\b", RegexOptions.CultureInvariant)),
        new("a version number", new Regex(@"\bv\d+\.\d+", RegexOptions.CultureInvariant)),
        new("a date", new Regex(@"\b\d{4}-\d{2}-\d{2}\b", RegexOptions.CultureInvariant)),
        new("a plan reference", new Regex(@"follow-on|follow-up", RegexOptions.CultureInvariant)),
        new("a history note", new Regex(@"\bPre-(fix|F|Phase)\b|\b[Uu]ser[- ]reported|\b[Uu]ser explicitly approved|\b[Rr]egression tests?\b", RegexOptions.CultureInvariant)),
        new("a test or tool name", new Regex(@"\b[A-Z]\w*Tests\b|\bPlaywright\b|\bJint\b", RegexOptions.CultureInvariant)),
    ];

    /// <summary>The text of every <c>&lt;script&gt;</c> element of an SVG, one after the other; empty when it has none.
    /// A picture's own words (a title, a tick label) may be a date or a version, so the pictures are held to this part.</summary>
    /// <param name="svg">An SVG document.</param>
    public static string ScriptText(this string svg) =>
        string.Join('\n', ScriptElement.Matches(svg).Select(m => m.Groups["body"].Value));

    /// <summary>Every line of <paramref name="text"/> that carries an internal label, with the kind of label it is.</summary>
    /// <param name="text">An SVG, a script or a page as the library writes it.</param>
    /// <returns>One hit per label found; empty when the text is free of them.</returns>
    public static IReadOnlyList<InternalLabelHit> InternalLabels(this string text)
    {
        var hits = new List<InternalLabelHit>();
        foreach (var line in text.Split('\n'))
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
