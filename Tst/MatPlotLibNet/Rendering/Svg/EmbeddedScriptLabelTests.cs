// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
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
    /// <summary>One piece of script text the library embeds, and the member it comes from.</summary>
    private readonly record struct ScriptText(string Source, string Text);

    /// <summary>The internal labels found in one embedded script.</summary>
    private readonly record struct ScriptFinding(string Source, IReadOnlyList<InternalLabelHit> Hits);

    private static string Describe(IEnumerable<InternalLabelHit> hits) =>
        string.Join(Environment.NewLine, hits.Select(h => $"  {h.Kind}: {h.Text}"));

    /// <summary>Every script the library embeds in an SVG: each <c>Svg*Script</c> class's string constants and the
    /// text its script methods return, with every switch of a method turned on so no branch of the script is skipped.</summary>
    private static List<ScriptText> EmbeddedScripts()
    {
        const BindingFlags Members = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        var scripts = new List<ScriptText>();
        foreach (var type in ScriptTypes())
        {
            foreach (var field in type.GetFields(Members).Where(f => f.FieldType == typeof(string)))
            {
                scripts.Add(new ScriptText($"{type.Name}.{field.Name}", (string)field.GetValue(null)!));
            }

            foreach (var method in type.GetMethods(Members).Where(m => m.ReturnType == typeof(string) && !m.IsSpecialName))
            {
                var arguments = method.GetParameters()
                    .Select(p => p.ParameterType == typeof(bool)
                        ? true
                        : p.HasDefaultValue
                            ? p.DefaultValue
                            : throw new NotSupportedException($"{type.Name}.{method.Name}: teach this test to call parameter '{p.Name}'"))
                    .ToArray();
                scripts.Add(new ScriptText($"{type.Name}.{method.Name}()", (string)method.Invoke(null, arguments)!));
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
        var folder = Path.Combine(RepoRoot(), "Src", "MatPlotLibNet", "Rendering", "Svg");
        var onDisk = Directory.EnumerateFiles(folder, "Svg*Script.cs").Select(Path.GetFileNameWithoutExtension).Order(StringComparer.Ordinal);

        Assert.Equal(onDisk, ScriptTypes().Select(t => t.Name));
    }

    [Fact]
    public void EveryEmbeddedScript_DescribesWhatItDoes_AndNamesNoPhaseVersionDateOrTest()
    {
        var offenders = EmbeddedScripts()
            .Select(s => new ScriptFinding(s.Source, s.Text.InternalLabels()))
            .Where(f => f.Hits.Count > 0)
            .Select(f => $"{f.Source}{Environment.NewLine}{Describe(f.Hits)}")
            .ToArray();

        Assert.True(offenders.Length == 0,
            $"{offenders.Length} embedded script(s) carry internal labels:{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
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

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "CHANGELOG.md")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return dir!.FullName;
    }
}
