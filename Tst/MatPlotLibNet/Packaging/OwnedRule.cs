// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>A rule the library works out in one place: how it reads in source (a pattern over one line of code) and the only files
/// allowed to write it that way. <see cref="OneOwnerSourceContractTests"/> holds a row of these per rule.
/// <para>A comment is not a spelling: a line that starts with <c>//</c> (so every XML-doc line too) is not read, unless the rule is
/// one of the documentation (<see cref="ReadsDocumentation"/>), which reads those lines and nothing else.</para></summary>
internal sealed record OwnedRule
{
    private readonly Regex _pattern;

    /// <summary>Makes the rule <paramref name="rule"/>, spelled as <paramref name="pattern"/>, owned by the files <paramref name="owners"/>.</summary>
    /// <param name="rule">What the rule says, in a sentence.</param>
    /// <param name="pattern">A regular expression over one line of code.</param>
    /// <param name="owners">The repository paths of the files that may spell it, with forward slashes.</param>
    /// <exception cref="ArgumentException">The rule has no name or no pattern, or no file owns it.</exception>
    public OwnedRule(string rule, string pattern, params string[] owners)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rule);
        ArgumentException.ThrowIfNullOrEmpty(pattern);
        if (owners.Length == 0)
        {
            throw new ArgumentException($"The rule '{rule}' has no owner: a rule that every file may spell is no rule.", nameof(owners));
        }

        Rule = rule;
        Owners = owners;
        _pattern = new Regex(pattern, RegexOptions.CultureInvariant);
    }

    private OwnedRule(string rule, string pattern, string under)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rule);
        ArgumentException.ThrowIfNullOrEmpty(pattern);
        Rule = rule;
        Owners = [];
        Under = under;
        _pattern = new Regex(pattern, RegexOptions.CultureInvariant);
    }

    /// <summary>A rule no file writes by hand: the rule is a call of the standard library or a member of an owner whose own text does
    /// not spell it, so a line that matches <paramref name="pattern"/> anywhere is a second owner.</summary>
    /// <param name="rule">What the rule says, in a sentence.</param>
    /// <param name="pattern">A regular expression over one line of code.</param>
    /// <param name="under">The folder (from the repository root, forward slashes) the rule is read in; empty reads every file given.</param>
    /// <returns>The rule.</returns>
    /// <exception cref="ArgumentException">The rule has no name or no pattern.</exception>
    public static OwnedRule Nowhere(string rule, string pattern, string under = "") => new(rule, pattern, under);

    /// <summary>A rule of the XML documentation no file may spell: a sentence that types a number a constant owns. Only the lines that
    /// start with <c>///</c> are read.</summary>
    /// <param name="rule">What the rule says, in a sentence.</param>
    /// <param name="pattern">A regular expression over one line of documentation.</param>
    /// <param name="under">The folder or file (from the repository root, forward slashes) the rule is read in; empty reads every file given.</param>
    /// <returns>The rule.</returns>
    /// <exception cref="ArgumentException">The rule has no name or no pattern.</exception>
    public static OwnedRule InDocumentation(string rule, string pattern, string under = "") => new(rule, pattern, under) { ReadsDocumentation = true };

    /// <summary>A rule of the comments no file may spell: the text of a comment (a line that starts with <c>//</c> or <c>///</c>, or the
    /// part of a code line after one) is read, starting at the comment mark, and the code of the line is not.</summary>
    /// <param name="rule">What the rule says, in a sentence.</param>
    /// <param name="pattern">A regular expression over the comment of one line, which starts with its <c>//</c>.</param>
    /// <param name="under">The folder or file (from the repository root, forward slashes) the rule is read in; empty reads every file given.</param>
    /// <returns>The rule.</returns>
    /// <exception cref="ArgumentException">The rule has no name or no pattern.</exception>
    public static OwnedRule InComments(string rule, string pattern, string under = "") => new(rule, pattern, under) { ReadsComments = true };

    /// <summary>What the rule says.</summary>
    public string Rule { get; }

    /// <summary>The files that may spell the rule.</summary>
    public IReadOnlyList<string> Owners { get; }

    /// <summary>Whether the rule is read in the XML documentation instead of the code: a rule that holds what the prose of a setting
    /// says (a default typed in a sentence beside the constant that owns it) reads only the lines that start with <c>///</c>, and a
    /// rule of the code never reads them.</summary>
    public bool ReadsDocumentation { get; init; }

    /// <summary>Whether the rule is read in the comments of every line, the trailing ones included, instead of the code: the pattern is
    /// read over the text from the comment mark to the end of the line, and a line with no comment is not read.</summary>
    public bool ReadsComments { get; init; }

    /// <summary>The folder (from the repository root, with forward slashes) the rule is read in: a rule about the core's own types
    /// is not read in a package that has types of its own for the same sums. Empty (the default) reads every file given.</summary>
    public string Under { get; init; } = string.Empty;

    /// <summary>Whether the pattern is read over the whole file instead of one line at a time: a spelling that runs over a line break (a
    /// member whose type is named on one line and made by a target-typed <c>new(</c> on the next) is read with the comment lines blanked,
    /// and a finding names the line the spelling starts on. <see langword="false"/> (the default) reads one line at a time.</summary>
    public bool AcrossLines { get; init; }

    /// <summary>Lines the rule was written to refuse, each read in <see cref="ProbeFile"/>: a spelling the rule must find, so a pattern
    /// that drifts away from what it guards goes red here before it goes green on a tree it no longer reads. Every rule carries one.</summary>
    public IReadOnlyList<string> Finds { get; init; } = [];

    /// <summary>Lines that look like a spelling of the rule and are not one (another rule, the owner asked, a value named), each read in
    /// <see cref="ProbeFile"/>: the rule must pass them.</summary>
    public IReadOnlyList<string> Ignores { get; init; } = [];

    /// <summary>The file a probe is read in: the one file the rule reads, or a file named <c>Any.cs</c> in the folder it reads, which no
    /// rule owns (<c>Src</c> for a rule that reads every file given).</summary>
    public string ProbeFile => Under.EndsWith(".cs", StringComparison.Ordinal) ? Under : $"{(Under.Length == 0 ? "Src" : Under.TrimEnd('/'))}/Any.cs";

    /// <summary>Every probe the rule reads wrongly: a line of <see cref="Finds"/> it does not find, a line of <see cref="Ignores"/> it finds.</summary>
    /// <returns>One sentence per probe read wrongly; empty when the rule reads all of them as they say.</returns>
    public IReadOnlyList<string> MisreadProbes() =>
    [
        .. Finds.Where(line => !Spells(line)).Select(line => $"does not find its probe: {line}"),
        .. Ignores.Where(Spells).Select(line => $"finds a line it is meant to pass: {line}"),
    ];

    /// <summary>Whether the rule finds <paramref name="line"/> read alone in the probe file (an owner's stale finding names no line).</summary>
    private bool Spells(string line) =>
        Violations([new(ProbeFile, line)]).Any(finding => finding.Length > ProbeFile.Length + 1
            && finding.StartsWith(ProbeFile + ":", StringComparison.Ordinal) && char.IsDigit(finding[ProbeFile.Length + 1]));

    /// <summary>Every line of <paramref name="sources"/> that spells the rule in a file that does not own it, and every owner that does
    /// not spell it any more (a row whose owner has moved on guards nothing).</summary>
    /// <param name="sources">The files to read.</param>
    /// <returns>One sentence per finding, naming the file and the line; empty when the rule has exactly its owners.</returns>
    public IReadOnlyList<string> Violations(IEnumerable<SourceFile> sources)
    {
        var findings = new List<string>();
        var spelling = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in sources)
        {
            if (!file.Path.StartsWith(Under, StringComparison.Ordinal))
            {
                continue;
            }

            bool owns = Owners.Contains(file.Path, StringComparer.Ordinal);
            if (AcrossLines)
            {
                ReadAcrossLines(file, owns, findings, spelling);
                continue;
            }

            var lines = file.Lines;
            for (int i = 0; i < lines.Count; i++)
            {
                bool isRead = ReadsDocumentation ? file.IsDocumentation(i) : !file.IsComment(i);
                string read = lines[i];
                if (ReadsComments)
                {
                    var comment = SourceFile.CommentOf(read);
                    isRead = comment is not null;
                    read = comment ?? string.Empty;
                }

                if (!isRead || !_pattern.IsMatch(read))
                {
                    continue;
                }

                if (owns)
                {
                    spelling.Add(file.Path);
                }
                else
                {
                    findings.Add($"{file.Path}:{i + 1}: spells the rule outside its owners: {lines[i].Trim()}");
                }
            }
        }

        findings.AddRange(Owners.Where(owner => !spelling.Contains(owner))
            .Select(owner => $"{owner}: owns the rule but no longer spells it (moved, renamed or deleted): update the row"));
        return findings;
    }

    /// <summary>Reads the code of <paramref name="file"/> as one text, every comment line emptied so a line keeps its number, and names the
    /// line each spelling starts on.</summary>
    private void ReadAcrossLines(SourceFile file, bool owns, List<string> findings, HashSet<string> spelling)
    {
        foreach (Match match in _pattern.Matches(file.Code))
        {
            if (owns)
            {
                spelling.Add(file.Path);
                continue;
            }

            int line = SourceFile.LineIndexAt(file.Code, match.Index);
            findings.Add($"{file.Path}:{line + 1}: spells the rule outside its owners: {file.Lines[line].Trim()}");
        }
    }
}
