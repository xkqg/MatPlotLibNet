// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>A test project that compiles a file of the core suite as a link brings every file of the core suite that file names a type
/// of: moving a type out of a linked file into a file of its own leaves each project that linked the old file without it, and the
/// core suite, which holds all of them, still builds. Only a build of every project of the filter shows the break, so this holds it.</summary>
public partial class LinkedTestFilesContractTests
{
    private const string CoreFolder = "Tst/MatPlotLibNet/";

    [GeneratedRegex(@"<Compile\s+Include=""\.\.\\MatPlotLibNet\\(?<path>[^""]+\.cs)""", RegexOptions.CultureInvariant)]
    private static partial Regex Link();

    [GeneratedRegex(@"^(?:(?:public|internal|static|sealed|abstract|partial|readonly|file)\s+)*(?:class|struct|record(?:\s+(?:class|struct))?|interface|enum)\s+(?<name>[A-Z]\w*)", RegexOptions.CultureInvariant | RegexOptions.Multiline)]
    private static partial Regex Declaration();

    [GeneratedRegex(@"\b[A-Z]\w*\b", RegexOptions.CultureInvariant)]
    private static partial Regex Word();

    /// <summary>A string literal, so a name a test only writes in a string (a list of deleted names) is no use of the type.</summary>
    [GeneratedRegex(@"@""(?:""""|[^""])*""|""(?:\\.|[^""\\\n])*""", RegexOptions.CultureInvariant)]
    private static partial Regex Literal();

    /// <summary>The files of the core suite, by the type each declares at the top of a line (one file per type, so a name has one).</summary>
    private static readonly Lazy<Dictionary<string, string>> Declaring = new(() =>
    {
        var map = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var file in RepositoryTree.TestFiles().Where(f => f.Path.StartsWith(CoreFolder, StringComparison.Ordinal)))
        {
            foreach (Match declared in Declaration().Matches(file.Code))
            {
                if (!map.TryGetValue(declared.Groups["name"].Value, out var files))
                {
                    map[declared.Groups["name"].Value] = files = new(StringComparer.Ordinal);
                }

                files.Add(file.Path);
            }
        }

        return map.Where(pair => pair.Value.Count == 1).ToDictionary(pair => pair.Key, pair => pair.Value.Single(), StringComparer.Ordinal);
    });

    [Fact]
    public void AProjectThatLinksAFileOfTheCoreSuite_LinksTheFilesThatFileNamesATypeOf()
    {
        var core = RepositoryTree.TestFiles().Where(f => f.Path.StartsWith(CoreFolder, StringComparison.Ordinal)).ToDictionary(f => f.Path, StringComparer.Ordinal);
        var missing = new List<string>();

        foreach (var project in RepositoryTree.BuildFiles().Where(f => f.Path.StartsWith("Tst/", StringComparison.Ordinal)
            && f.Path.EndsWith(".csproj", StringComparison.Ordinal) && !f.Path.StartsWith(CoreFolder, StringComparison.Ordinal)))
        {
            var linked = Link().Matches(project.Text).Select(m => CoreFolder + m.Groups["path"].Value.Replace('\\', '/')).ToHashSet(StringComparer.Ordinal);
            foreach (string path in linked.Where(core.ContainsKey))
            {
                foreach (string name in Word().Matches(Literal().Replace(core[path].Code, "\"\"")).Select(m => m.Value).Distinct(StringComparer.Ordinal))
                {
                    if (Declaring.Value.TryGetValue(name, out var declaredIn) && declaredIn != path && !linked.Contains(declaredIn))
                    {
                        missing.Add($"{project.Path} links {path}, which names {name}, declared in {declaredIn}, which it does not link");
                    }
                }
            }
        }

        Assert.True(missing.Count == 0, $"{missing.Count} link(s) without what they use:\n" + string.Join('\n', missing));
    }
}
