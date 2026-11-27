// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Tests.Packaging;
using System.Reflection;
using System.Text.RegularExpressions;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet.Tests.Rendering.Svg;

/// <summary>
/// An interactive SVG carries its scripts, comments and all, into whatever page or file it lands in. A comment that
/// names the phase of a plan, a version, a date or the test that found a bug is the project's history, not the
/// reader's: these tests hold every script the library writes to describing what it does.
/// </summary>
public class EmbeddedScriptLabelTests
{
    /// <summary>The internal labels found in one embedded script.</summary>
    private readonly record struct ScriptFinding(string Source, IReadOnlyList<InternalLabelHit> Hits);

    private static string Describe(IEnumerable<InternalLabelHit> hits) =>
        string.Join(Environment.NewLine, hits.Select(h => $"  {h.Kind}: {h.Text}"));

    /// <summary>Every script the library embeds in an SVG: each <c>Svg*Script</c> class's string constants and the
    /// text its script methods return, with every switch of a method turned on so no branch of the script is skipped. A script is read as
    /// a file whose path names the member it comes from.</summary>
    private static List<SourceFile> EmbeddedScripts()
    {
        const BindingFlags Members = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        var scripts = new List<SourceFile>();
        foreach (var type in ScriptTypes())
        {
            foreach (var field in type.GetFields(Members).Where(f => f.FieldType == typeof(string)))
            {
                scripts.Add(new SourceFile($"{type.Name}.{field.Name}", (string)field.GetValue(null)!));
            }

            // A method that picks a script for a figure hands out one of the texts its options value composes, read below.
            foreach (var method in type.GetMethods(Members).Where(m => m.ReturnType == typeof(string) && !m.IsSpecialName
                         && !m.GetParameters().Any(p => p.ParameterType == typeof(MatPlotLibNet.Models.Figure))))
            {
                var arguments = method.GetParameters()
                    .Select(p => p.ParameterType == typeof(bool)
                        ? true
                        : p.ParameterType == typeof(SvgTransform.ServerScriptBranches)
                        ? SvgTransform.ServerScriptBranches.All
                        : p.HasDefaultValue
                            ? p.DefaultValue
                            : throw new NotSupportedException($"{type.Name}.{method.Name}: teach this test to call parameter '{p.Name}'"))
                    .ToArray();
                scripts.Add(new SourceFile($"{type.Name}.{method.Name}()", (string)method.Invoke(null, arguments)!));
            }
        }

        return scripts;
    }

    private static Type[] ScriptTypes() =>
        [.. typeof(SvgTransform).Assembly.GetTypes()
            .Where(t => t.Namespace == "MatPlotLibNet.Rendering.Svg" && t.Name.StartsWith("Svg", StringComparison.Ordinal)
                        && t.Name.EndsWith("Script", StringComparison.Ordinal))
            .OrderBy(t => t.Name, StringComparer.Ordinal)];

    [Fact]
    public void EveryScriptClassInTheSvgFolder_IsHeldToTheCheck()
    {
        // The reflection above finds a script by its name. A script class it cannot see would be a script nobody
        // holds to the check, so the folder and the reflection must name the same classes.
        var folder = Path.Combine(RepositoryRoot.Folder, "Src", "MatPlotLibNet", "Rendering", "Svg");
        var onDisk = Directory.EnumerateFiles(folder, "Svg*Script.cs").Select(Path.GetFileNameWithoutExtension).Order(StringComparer.Ordinal);

        Assert.Equal(onDisk, ScriptTypes().Select(t => t.Name));
    }

    [Fact]
    public void EveryEmbeddedScript_DescribesWhatItDoes_AndNamesNoPhaseVersionDateOrTest()
    {
        var offenders = EmbeddedScripts()
            .Select(s => new ScriptFinding(s.Path, s.Text.InternalLabels()))
            .Where(f => f.Hits.Count > 0)
            .Select(f => $"{f.Source}{Environment.NewLine}{Describe(f.Hits)}")
            .ToArray();

        Assert.True(offenders.Length == 0,
            $"{offenders.Length} embedded script(s) carry internal labels:{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }

    /// <summary>A type written before a member (<c>Projection3D.SelectFaces</c>).</summary>
    private static readonly Regex TypeAndMember = new(@"\b([A-Z][A-Za-z0-9]+)\.([A-Z][A-Za-z0-9_]+)\b", RegexOptions.CultureInvariant);

    /// <summary>A member of the server named after the word that says it is the server's (<c>the server's OutwardPerp</c>).</summary>
    private static readonly Regex ServerMember = new(@"\bserver(?:'s)? ([A-Z][a-z0-9]+(?:[A-Z][a-z0-9]+)+)\b", RegexOptions.CultureInvariant);

    private const BindingFlags AllMembers = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    private static IEnumerable<Type> WithNestedTypes(Type type) =>
        new[] { type }.Concat(type.GetNestedTypes(AllMembers).SelectMany(WithNestedTypes));

    /// <summary>The comments of a script are written for the reader of the page, and the ones that point at the server say which
    /// member it is. A member that was renamed, moved or deleted leaves a pointer to nothing, so every type and member a
    /// comment names is looked up in the library.</summary>
    [Fact]
    public void EveryMemberAnEmbeddedScriptNamesInItsComments_StillExists()
    {
        var types = typeof(SvgTransform).Assembly.GetTypes().SelectMany(WithNestedTypes).ToArray();
        var typesByName = types.GroupBy(t => t.Name).ToDictionary(g => g.Key, g => g.ToArray());
        var names = types.Select(t => t.Name).Concat(types.SelectMany(t => t.GetMembers(AllMembers)).Select(m => m.Name)).ToHashSet();

        var stale = new List<string>();
        foreach (var script in EmbeddedScripts())
        {
            foreach (var line in script.Lines)
            {
                if (SourceFile.CommentOf(line) is not { } words) continue;

                foreach (Match m in TypeAndMember.Matches(words))
                {
                    if (typesByName.TryGetValue(m.Groups[1].Value, out var named)
                        && !named.Any(t => t.GetMember(m.Groups[2].Value, AllMembers).Length > 0))
                        stale.Add($"{script.Path}: {m.Value} (no member {m.Groups[2].Value} on {m.Groups[1].Value})");
                }

                foreach (Match m in ServerMember.Matches(words))
                {
                    if (!names.Contains(m.Groups[1].Value))
                        stale.Add($"{script.Path}: the server's {m.Groups[1].Value} (no type or member of that name)");
                }
            }
        }

        Assert.True(stale.Count == 0, $"{stale.Count} name(s) in embedded scripts point at nothing:{Environment.NewLine}{string.Join(Environment.NewLine, stale)}");
    }

    [Fact]
    public void AnInteractive3DFigure_CarriesNoInternalLabelInItsSvg()
    {
        double[] x = [0, 5, 10];
        double[] y = [0, 5, 10];
        double[,] z = { { 0, 1, 2 }, { 3, 4, 5 }, { 6, 7, 8 } };
        var figure = Plt.Create().Surface(x, y, z).WithBrowserInteraction().Build();

        var svg = new SvgTransform().Render(figure);

        Assert.Contains("mpl-3d-scene", svg);
        var hits = svg.InternalLabels();
        Assert.True(hits.Count == 0, $"The interactive 3-D SVG carries {hits.Count} internal label(s):{Environment.NewLine}{Describe(hits)}");
    }

    [Fact]
    public void AFigureWithEveryBrowserScript_CarriesNoInternalLabelInItsSvg()
    {
        var figure = Plt.Create().Plot([1.0, 2.0, 3.0], [3.0, 1.0, 2.0], s => s.Label = "line").WithBrowserInteraction().Build();

        var svg = new SvgTransform().Render(figure);

        var hits = svg.InternalLabels();
        Assert.True(hits.Count == 0, $"The interactive SVG carries {hits.Count} internal label(s):{Environment.NewLine}{Describe(hits)}");
    }
}
