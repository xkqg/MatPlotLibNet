// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Tests.Rendering;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>The sources of a package compiled the way the build compiles them, for a contract test that reads what the compiler knows
/// (the type of a value, the symbol a name stands for): the language version, the symbols of the net10.0 build, the implicit usings
/// every package takes. <see cref="Packages"/> compiles every package of <c>Src</c> once per process, each against the compilations of
/// the packages it references, so a type of the core read from a satellite package is the same symbol the core declares.</summary>
internal static class SourceCompilation
{
    private static readonly string[] Symbols = ["NET", "NETCOREAPP", "NET10_0", "NET10_0_OR_GREATER", "NET9_0_OR_GREATER", "NET8_0_OR_GREATER", "RELEASE"];

    private const string ImplicitUsings =
        "global using System; global using System.Collections.Generic; global using System.IO; global using System.Linq; " +
        "global using System.Net.Http; global using System.Threading; global using System.Threading.Tasks;";

    private static readonly CSharpParseOptions Options = new(LanguageVersion.Preview, preprocessorSymbols: Symbols);

    private static readonly Lazy<IReadOnlyList<CSharpCompilation>> All = new(CompileAll);

    /// <summary>The files of the assemblies the test runs on: the framework, and what the test process loads beside it.</summary>
    internal static IReadOnlyList<string> FrameworkPaths { get; } = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);

    /// <summary>The assemblies of the framework the test runs on (no package of the library and none of its suites): the same on every
    /// machine of one runtime, so a reading that needs nothing beyond the framework gives one answer on Windows and on Linux.</summary>
    internal static IReadOnlyList<MetadataReference> Framework { get; } =
    [
        .. FrameworkPaths
            .Where(path => !Path.GetFileName(path).StartsWith("MatPlotLibNet", StringComparison.OrdinalIgnoreCase))
            .Select(path => MetadataReference.CreateFromFile(path)),
    ];

    /// <summary>Every package of <c>Src</c> but the frozen Notebooks, compiled from its sources against the framework and the
    /// compilations of the packages it references (no package of nuget.org: a name of one stays unbound, a name of the library binds).</summary>
    internal static IReadOnlyList<CSharpCompilation> Packages => All.Value;

    /// <summary>The package <paramref name="package"/> made of <paramref name="files"/>, compiled against <paramref name="references"/>.</summary>
    /// <param name="package">The assembly name.</param>
    /// <param name="files">The source files, by repository path.</param>
    /// <param name="references">The assemblies the package compiles against.</param>
    /// <returns>The compilation, with the implicit usings added as a tree of their own.</returns>
    internal static CSharpCompilation Create(string package, IEnumerable<SourceFile> files, IEnumerable<MetadataReference> references)
    {
        var trees = files.Select(file => CSharpSyntaxTree.ParseText(file.Text, Options, file.Path)).ToList();
        trees.Add(CSharpSyntaxTree.ParseText(ImplicitUsings, Options, "GlobalUsings.cs"));
        return CSharpCompilation.Create(package, trees, references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable, allowUnsafe: true));
    }

    private static IReadOnlyList<CSharpCompilation> CompileAll()
    {
        var sources = RepositoryTree.SourceFiles()
            .Where(file => !file.Path.StartsWith("Src/MatPlotLibNet.Notebooks/", StringComparison.Ordinal))
            .GroupBy(file => file.Path.Split('/')[1])
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);
        var referenced = RepositoryTree.Files("Src", "*.csproj")
            .Where(project => sources.ContainsKey(Path.GetFileNameWithoutExtension(project.Path)))
            .ToDictionary(project => Path.GetFileNameWithoutExtension(project.Path), project => project.Text.Xml().Descendants("ProjectReference")
                .Select(reference => Path.GetFileNameWithoutExtension((reference.Attribute("Include")?.Value ?? string.Empty).Replace('\\', '/')))
                .ToList(), StringComparer.Ordinal);

        var compiled = new Dictionary<string, CSharpCompilation>(StringComparer.Ordinal);
        foreach (string package in sources.Keys.Order(StringComparer.Ordinal)) Compile(package);
        return [.. compiled.Values];

        CSharpCompilation Compile(string package)
        {
            if (compiled.TryGetValue(package, out var done)) return done;
            var below = new HashSet<string>(StringComparer.Ordinal);
            Gather(package);
            var compilation = Create(package, sources[package],
                Framework.Concat(below.Order(StringComparer.Ordinal).Select(name => Compile(name).ToMetadataReference())));
            compiled[package] = compilation;
            return compilation;

            void Gather(string name)
            {
                foreach (string reference in referenced[name])
                {
                    if (below.Add(reference)) Gather(reference);
                }
            }
        }
    }
}
