// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>The bodies of the wire hold declarations only. A record of the figure JSON (a file of <c>Serialization</c>) and the two
/// halves of a series' wire (<c>ToSeriesDto</c>, <c>FromSeriesDto</c>) are lists: one line per field, naming the field once and calling
/// the one owner of its rule (<c>OrNullAtRest</c> against the <c>Rest</c> it is measured by, to write; <c>?? Rest.X</c> in an
/// initialiser or <c>if (dto.X is { } x)</c>, to read; <c>WireKind</c>, <c>WireEnum</c>, <c>StrokeOver</c>, <c>RestoreColorScale</c>
/// for names, strokes and colour scales). Two such lists look alike to a clone detector that ignores names, because they are the
/// same idiom over other fields: there the detector's finding is a declaration. Any other line repeated in a wire body is a rule
/// stated twice. This test runs the detector of the DRY measure over the sources (windows of 8 lines, each line normalised: names,
/// numbers and strings replaced, a window counted only when at least 6 of its lines are longer than 12 characters) and refuses a
/// repeated line in a wire body that is not one of the shapes of a declaration.</summary>
public class WireDeclarationContractTests
{
    private const int Window = 8;

    private static readonly Regex Text = new(@"""(?:[^""\\]|\\.)*""", RegexOptions.CultureInvariant);
    private static readonly Regex Number = new(@"\b\d[\d_\.]*[fdmulFDMUL]*\b", RegexOptions.CultureInvariant);
    private static readonly Regex Name = new(@"[A-Za-z_]\w*", RegexOptions.CultureInvariant);
    private static readonly Regex Space = new(@"\s+", RegexOptions.CultureInvariant);
    private static readonly Regex WireMember = new(@"\b(?:ToSeriesDto\(\)|FromSeriesDto\()", RegexOptions.CultureInvariant);

    private static readonly HashSet<string> Keywords = new(
        ("abstract as base bool break byte case catch char checked class const continue decimal default delegate do double else enum event " +
         "explicit extern false finally fixed float for foreach goto if implicit in int interface internal is lock long namespace new null " +
         "object operator out override params private protected public readonly ref return sbyte sealed short sizeof stackalloc static " +
         "string struct switch this throw true try typeof uint ulong unchecked unsafe ushort using virtual void volatile while var async " +
         "await record init get set value with when yield S N").Split(' '), StringComparer.Ordinal);

    private static readonly string[] Skipped = [".g.cs", ".Designer.cs", "MatplotlibColorTables.Data", "matplotlib-mathtext"];

    /// <summary>The shapes of a declaration in a wire body, each one line.</summary>
    private static readonly Regex[] Declarations =
    [
        // a field of a record: public double? MarginLeft { get; init; }
        new(@"^(?:public|internal|private) (?:required )?[\w.<>?,\[\] ]+ \w+ \{ get; (?:init|set); \}(?: = [^;]+;)?$"),
        // one or more fields written or read in an initialiser: MarginLeft = spacing.MarginLeft.OrNullAtRest(Rest.MarginLeft),
        new(@"^(?:\w+\s*=\s*[^?:;=,]+(?:\?\?\s*[\w.]+)?\s*,\s*)*\w+\s*=\s*[^?:;=,]+(?:\?\?\s*[\w.]+)?\s*[,;]?$"),
        // a field the document carries, applied: if (dto.Alpha is { } alpha) s.Alpha = alpha;
        new(@"^if \(dto\.\w+(?:\.WireEnum<\w+>\(\))? is \{ \} \w+\) s\.\w+ = \w+;$"),
        // a nullable field copied as it is: s.Color = dto.Color;
        new(@"^s\.\w+ = dto\.\w+;$"),
        // a field read through its owner into a local: var root = dto.TreeRoot.TreeOrBareRoot();
        new(@"^var \w+ = dto\.\w+(?:\.\w+\(\))?;$"),
        // the series made by its verb, and handed back
        new(@"^var s = axes\.\w+\(.*\);$"), new(@"^return s;$"),
        // the stroke and the colour scale, each through its owner
        new(@"^s\.Stroke = dto\.StrokeOver\(s\.Stroke\);$"), new(@"^s\.RestoreColorScale\(dto\);$"),
        // the heads of the members and of the kind
        new(@"^internal static \w+ FromSeriesDto\(Axes axes, SeriesDto dto\)$"), new(@"^public override SeriesDto ToSeriesDto\(\) => new\(\)$"),
        new(@"^internal static \w+\?? From\(.*\) => .*new\(\)$"), new(@"^static string ISeriesKind<\w+>\.Kind => ""[\w-]+"";$"),
        new(@"^private static readonly \w+ Rest = new\(\);$"), new(@"^\}\)?;?$"),
    ];

    [Fact]
    public void ARepeatedLineInAWireBody_IsADeclaration()
    {
        var findings = Findings(RepositoryTree.SourceFiles()).ToList();

        Assert.True(findings.Count == 0, $"{findings.Count} repeated lines in a wire body state a rule a second time:\n" + string.Join('\n', findings));
    }

    [Fact]
    public void ARuleStatedInTwoWireBodies_IsFound()
    {
        string Reader(string kind) =>
            $"    internal static {kind}Series FromSeriesDto(Axes axes, SeriesDto dto)\n    {{\n" +
            "        var root = dto.TreeRoot is { } treeRoot ? treeRoot.DtoToTreeNode() : new TreeNode { Label = \"Root\" };\n" +
            $"        var s = axes.{kind}(root);\n" +
            "        if (dto.Padding is { } padding) s.Padding = padding;\n" +
            "        if (dto.LabelFit is { } labelFit) s.LabelFit = labelFit;\n" +
            "        if (dto.HeadlineFontSize is { } headlineFontSize) s.HeadlineFontSize = headlineFontSize;\n" +
            "        if (dto.Alpha is { } alpha) s.Alpha = alpha;\n" +
            "        if (dto.Color is { } color) s.Color = color;\n" +
            "        if (dto.Fill is { } fill) s.Fill = fill;\n" +
            "        return s;\n    }\n";

        var findings = Findings([new("Src/MatPlotLibNet/Models/Series/A.cs", Reader("Treemap")), new("Src/MatPlotLibNet/Models/Series/B.cs", Reader("Icicle"))]).ToList();

        Assert.Equal(2, findings.Count);
        Assert.All(findings, finding => Assert.Contains("var root = dto.TreeRoot is", finding, StringComparison.Ordinal));
    }

    /// <summary>Every repeated line of a wire body in <paramref name="sources"/> that is not a declaration.</summary>
    private static IEnumerable<string> Findings(IEnumerable<SourceFile> sources)
    {
        var files = sources.Where(file => !Skipped.Any(part => file.Path.Contains(part, StringComparison.Ordinal)))
            .Select(file => (file.Path, file.Lines))
            .ToList();
        var normalised = files.ToDictionary(file => file.Path, file => file.Lines
            .Select((line, index) => (Number: index + 1, Text: Normalised(line))).Where(line => line.Text is not null).ToArray());

        var windows = new Dictionary<string, List<(string Path, int Start)>>(StringComparer.Ordinal);
        foreach (var (path, lines) in normalised)
        {
            for (int k = 0; k + Window <= lines.Length; k++)
            {
                var window = lines.AsSpan(k, Window).ToArray();
                if (window.Count(line => line.Text!.Length > 12) < Window - 2) continue;
                string key = string.Join('\n', window.Select(line => line.Text));
                if (!windows.TryGetValue(key, out var at)) windows[key] = at = [];
                at.Add((path, k));
            }
        }

        var repeated = new Dictionary<string, SortedSet<int>>(StringComparer.Ordinal);
        foreach (var at in windows.Values.Where(at => at.Count > 1))
        {
            foreach (var (path, start) in at)
            {
                if (!repeated.TryGetValue(path, out var numbers)) repeated[path] = numbers = [];
                for (int j = 0; j < Window; j++) numbers.Add(normalised[path][start + j].Number);
            }
        }

        foreach (var (path, lines) in files.Where(file => repeated.ContainsKey(file.Path)))
        {
            var wire = WireLines(path, lines);
            foreach (int number in repeated[path].Where(wire.Contains))
            {
                string line = lines[number - 1].Trim();
                if (!Declarations.Any(shape => shape.IsMatch(line)))
                {
                    yield return $"{path}:{number}: {line}";
                }
            }
        }
    }

    /// <summary>The normalised form of a line the detector compares, or <see langword="null"/> for a line it skips (blank, a brace, a
    /// comment, a using, a namespace, a directive, an attribute).</summary>
    private static string? Normalised(string line)
    {
        string trimmed = line.Trim();
        if (trimmed.Length == 0 || trimmed is "{" or "}" or ");" or "(" or ")" or "};"
            || SourceFile.IsComment(trimmed) || trimmed.StartsWith("using ", StringComparison.Ordinal)
            || trimmed.StartsWith("namespace ", StringComparison.Ordinal) || trimmed.StartsWith('#') || trimmed.StartsWith('['))
        {
            return null;
        }

        string text = Number.Replace(Text.Replace(Space.Replace(trimmed, " "), "S"), "N");
        return Name.Replace(text, name => Keywords.Contains(name.Value) ? name.Value : "I");
    }

    /// <summary>The numbers of the lines of a wire body: every line of a file of <c>Serialization</c>, and in a series, the lines of
    /// its <c>ToSeriesDto</c> and <c>FromSeriesDto</c>, from the head of the member to the brace or the semicolon that ends it.</summary>
    private static HashSet<int> WireLines(string path, IReadOnlyList<string> lines)
    {
        if (path.Contains("/Serialization/", StringComparison.Ordinal)) return [.. Enumerable.Range(1, lines.Count)];
        var wire = new HashSet<int>();
        if (!path.Contains("/Models/Series/", StringComparison.Ordinal)) return wire;
        for (int i = 0; i < lines.Count; i++)
        {
            if (!WireMember.IsMatch(lines[i]) || SourceFile.IsComment(lines[i])) continue;
            int depth = 0;
            bool braced = false;
            for (int j = i; j < lines.Count; j++)
            {
                wire.Add(j + 1);
                string code = Text.Replace(lines[j], "S");
                depth += code.Count(c => c == '{') - code.Count(c => c == '}');
                braced |= code.Contains('{');
                if ((braced && depth <= 0) || (!braced && code.TrimEnd().EndsWith(';')))
                {
                    i = j;
                    break;
                }
            }
        }

        return wire;
    }
}
