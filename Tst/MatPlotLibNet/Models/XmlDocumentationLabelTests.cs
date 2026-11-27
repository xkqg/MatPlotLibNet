// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Tests.Packaging;
using MatPlotLibNet.Tests.Rendering.Svg;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace MatPlotLibNet.Tests.Models;

/// <summary>The XML documentation of the library is what IntelliSense shows at the call and what the API pages are built
/// from, so it says what a member does today. A sentence that names the phase of a plan, a date, a pull request, a release
/// number used as a label, the test class that pins a rule, or a feature that is "reserved" and "activated later" is the
/// project's history, and it goes stale the day the plan moves: <c>ForceDirected</c> was documented as reserved and falling
/// back to Manual for releases after it was implemented.
/// This reads the generated documentation file of the core package and every <c>///</c> line of every package under
/// <c>Src</c>, so a member added tomorrow, public or internal, is held to the same without an edit here.</summary>
public class XmlDocumentationLabelTests
{
    /// <summary>One place whose documentation carries an internal label, and the sentence that shows it.</summary>
    /// <param name="Place">The documentation id of the member (<c>T:</c>, <c>M:</c>, <c>P:</c> ...) or the file and line of
    /// the first documentation line of the block.</param>
    /// <param name="Kind">What the words are.</param>
    /// <param name="Text">The words around the label, one line.</param>
    private readonly record struct LabelFinding(string Place, string Kind, string Text);

    /// <summary>The text of one run of consecutive <c>///</c> lines, as a reader sees it (no tags, one line), and where it starts.</summary>
    private readonly record struct DocBlock(string Place, string Text);

    private static readonly LabelRule[] Rules =
    [
        .. InternalLabelExtensions.SharedKinds,
        new("a council or plan reference", new Regex(@"\bfloor plan\b|\bstrict-90\b|\bv\d+\.\d+(?:\.\d+)? plan\b|interaction-hardening|\bPR \d|\btranche\b|\bcouncil\b|\brijloop\b|\bpath manifest\b", RegexOptions.CultureInvariant)),
        new("a claim that a feature is still to come", new Regex(@"\bactivated in v|\buntil activated\b|\bReserved —|\breserved at ordinal\b|\breserved for PR\b|\blands in PR\b", RegexOptions.CultureInvariant)),
        new("a private note", new Regex(@"\bfeedback_\w+", RegexOptions.CultureInvariant)),
        new("a section label of an earlier refactor", new Regex(@"\b[A-Z]\.\d+(?:\.[a-z])?\b", RegexOptions.CultureInvariant)),
    ];

    private static readonly XDocument Documentation = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "MatPlotLibNet.xml"));

    private static readonly Regex Whitespace = new(@"\s+", RegexOptions.CultureInvariant);

    private static readonly Regex Markup = new(@"</?[A-Za-z][^>]*>", RegexOptions.CultureInvariant);

    private static string TextOf(XElement member) =>
        Whitespace.Replace(string.Concat(member.DescendantNodes().OfType<XText>().Select(text => text.Value)), " ").Trim();

    private static IEnumerable<LabelFinding> Apply(string place, string text)
    {
        foreach (LabelRule rule in Rules)
        {
            Match hit = rule.Pattern.Match(text);
            if (hit.Success)
            {
                yield return new LabelFinding(place, rule.Kind,
                    text[Math.Max(0, hit.Index - 50)..Math.Min(text.Length, hit.Index + hit.Length + 50)]);
            }
        }
    }

    private static LabelFinding[] Findings() =>
        [.. Documentation.Descendants("member")
            .SelectMany(member => Apply((string?)member.Attribute("name") ?? "?", TextOf(member)))
            .OrderBy(finding => finding.Place, StringComparer.Ordinal)];

    private static IEnumerable<DocBlock> DocBlocks()
    {
        foreach (var file in RepositoryTree.SourceFiles().Concat(RepositoryTree.ComponentFiles()).OrderBy(file => file.Path, StringComparer.Ordinal))
        {
            string name = file.Path;
            var lines = file.Lines;
            var block = new StringBuilder();
            int first = 0;
            for (int i = 0; i <= lines.Count; i++)
            {
                if (i < lines.Count && file.IsDocumentation(i))
                {
                    string line = lines[i].TrimStart();
                    if (block.Length == 0)
                    {
                        first = i + 1;
                    }

                    block.Append(' ').Append(line, 3, line.Length - 3);
                }
                else if (block.Length > 0)
                {
                    yield return new DocBlock($"{name}:{first}", Whitespace.Replace(Markup.Replace(block.ToString(), " "), " ").Trim());
                    block.Clear();
                }
            }
        }
    }

    private static LabelFinding[] SourceFindings() =>
        [.. DocBlocks().SelectMany(block => Apply(block.Place, block.Text))];

    private static string Describe(LabelFinding[] findings, string what) =>
        $"{findings.Length} {what} carry the project's own history:{Environment.NewLine}"
        + string.Join(Environment.NewLine, findings.Select(finding => $"  {finding.Place} ({finding.Kind}): ...{finding.Text}..."));

    /// <summary>Liveness: the file holds the library's documentation, so a green run is not a run that read nothing.</summary>
    [Fact]
    public void TheDocumentationIsFound()
    {
        Assert.True(Documentation.Descendants("member").Count() > 1000, "the documentation file holds fewer than 1000 members");
        Assert.NotNull(Documentation.Descendants("member").FirstOrDefault(member => TextOf(member).Length > 0));
    }

    /// <summary>Liveness of the source scan: it reads the documentation lines of the core package and of the satellite
    /// packages, so a green run is not a run that found no file.</summary>
    [Fact]
    public void TheDocumentationLinesOfEveryPackageAreFound()
    {
        var blocks = DocBlocks().ToArray();

        Assert.True(blocks.Length > 3000, $"only {blocks.Length} runs of documentation lines were found under Src");
        foreach (string package in new[] { "Src/MatPlotLibNet/", "Src/MatPlotLibNet.AspNetCore/", "Src/MatPlotLibNet.Blazor/", "Src/MatPlotLibNet.Skia/" })
        {
            Assert.Contains(blocks, block => block.Place.StartsWith(package, StringComparison.Ordinal) && block.Text.Length > 0);
        }
    }

    /// <summary>Each rule finds the label it is named for, and leaves alone the words that only look like it (the Phase of a
    /// Hilbert transform, a Unix epoch, colour reserved for an alarm, the success criteria of the accessibility guidelines),
    /// so a rule cannot go green by matching nothing.</summary>
    [Theory]
    [InlineData("a phase label", "a rewrite (Phase P)", true)]
    [InlineData("a phase label", "Pre-Phase-M, line renderers drew", true)]
    [InlineData("a phase label", "Phase W (v1.7.2) sizes", true)]
    [InlineData("a phase label", "the current Phase (degrees)", false)]
    [InlineData("a date", "measured 2026-08-30", true)]
    [InlineData("a date", "milliseconds since 1970-01-01 UTC", false)]
    [InlineData("a council or plan reference", "Phase B.3 of the strict-90 floor plan", true)]
    [InlineData("a plan reference", "wired up by the v1.7.2 follow-on plan", true)]
    [InlineData("a council or plan reference", "is reserved for PR 2", true)]
    [InlineData("a council or plan reference", "the finding of the council", true)]
    [InlineData("a council or plan reference", "the second tranche of verbs", true)]
    [InlineData("a council or plan reference", "a plan of the page, drawn by hand", false)]
    [InlineData("a claim that a feature is still to come", "Reserved — activated in v1.10 PR 2", true)]
    [InlineData("a claim that a feature is still to come", "the colours reserved for states that need attention", false)]
    [InlineData("a private note", "per user convention: feedback_no_magic_strings", true)]
    [InlineData("a test or tool name", "verified by the Playwright harness", true)]
    [InlineData("a version number", "introduced in v1.2.2 as an option", true)]
    [InlineData("a version number", "the name it has carried since v1.7", true)]
    [InlineData("a version number", "WCAG 1.1.1, formula 26.2.23 and matplotlib 3.11.1", false)]
    [InlineData("a test or tool name", "See EnumOrdinalContractTests.", true)]
    [InlineData("a test or tool name", "pinned by ColorBarRendererBaselineTests and the suite", true)]
    [InlineData("a test or tool name", "a test fails when an ordinal moves", false)]
    [InlineData("a section label of an earlier refactor", "the code the renderer held before (pre-B.4).", true)]
    [InlineData("a section label of an earlier refactor", "the flip-over (B.1.e) verifies", true)]
    [InlineData("a section label of an earlier refactor", "WCAG 1.4.1 and formula 26.2.23 of Abramowitz and Stegun", false)]
    public void EachRule_FindsItsLabel_AndNotTheWordsThatOnlyLookLikeIt(string kind, string text, bool found) =>
        Assert.Equal(found, Rules.Single(rule => rule.Kind == kind).Pattern.IsMatch(text));

    /// <summary>The kinds of label that the documentation and the text a page emits both refuse (a phase, a date, a version number, a test or
    /// tool name) are decided once, by <see cref="InternalLabelExtensions.SharedKinds"/>: this reads each with that row, not with a regex of its own.</summary>
    [Fact]
    public void TheKindsTheEmittedTextAlsoRefuses_AreReadByTheSharedRows()
    {
        Assert.All(InternalLabelExtensions.SharedKinds, shared => Assert.Equal(shared, Assert.Single(Rules, rule => rule.Kind == shared.Kind)));
    }

    /// <summary>No documentation line names a phase, a date, a plan or a feature still to come. The generated file holds the core package
    /// only, so the satellite packages, a documentation block that no member carries and a line of an internal type are read from the
    /// source: no <c>///</c> line is left unread.</summary>
    [Theory]
    [InlineData(false, "documented member(s)")]
    [InlineData(true, "run(s) of documentation lines")]
    public void NoDocumentation_NamesAPhaseADateAPlanOrAFeatureStillToCome(bool fromSource, string what)
    {
        var findings = fromSource ? SourceFindings() : Findings();

        Assert.True(findings.Length == 0, Describe(findings, what));
    }
}
