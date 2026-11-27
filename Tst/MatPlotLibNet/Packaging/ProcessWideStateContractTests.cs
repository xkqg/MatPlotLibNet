// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>State that every piece of code in the process can reach and write is allowed in this library in one shape only:
/// a slot or a registry the library documents as process-wide, each separated from every other and tested under its own
/// collection. The source scan finds each public static place a caller can write to (a settable static property, a static
/// event, a <c>Register…</c> or <c>Use…</c> method) and holds that the section "Process-wide state" of ARCHITECTURE.md names
/// the class that owns it, so a new global is a documented decision and never an accident. CONTRIBUTING.md's class-design
/// rules are held to say the same shapes.</summary>
public class ProcessWideStateContractTests
{
    private static readonly string Root = RepositoryRoot.Folder;

    /// <summary>A public static event, a public static property whose setter is public, or a public static method that
    /// registers or switches something process-wide, over several lines when the declaration does.</summary>
    private static readonly Regex[] Writable =
    [
        new(@"public static event\b", RegexOptions.CultureInvariant),
        new(@"public static [^;{}()=]+?\s+\w+\s*\{[^{}]*(?<!private\s)(?<!internal\s)(?<!protected\s)\bset\b", RegexOptions.CultureInvariant),
        new(@"public static [\w<>\[\]?,]+ (?:Register|Unregister|Use)\w*\(", RegexOptions.CultureInvariant),
    ];

    /// <summary>The class a declaration belongs to: the nearest class declared above it in its file.</summary>
    private static readonly Regex ClassDeclaration = new(@"\bclass\s+(\w+)", RegexOptions.CultureInvariant);

    /// <summary>Process-wide places the scan cannot see because nothing writes them from outside: a cache that fills itself on
    /// first use. The section still has to name them.</summary>
    private static readonly string[] SelfFilling = ["NaturalEarth110m"];

    /// <summary>The classes of <paramref name="code"/> that declare a place anyone can write.</summary>
    private static IEnumerable<string> OwnersOfWritablePlaces(string code)
    {
        foreach (var pattern in Writable)
        {
            foreach (Match match in pattern.Matches(code))
            {
                var owner = ClassDeclaration.Matches(code[..match.Index]).LastOrDefault();
                if (owner is not null)
                {
                    yield return owner.Groups[1].Value;
                }
            }
        }
    }

    private static string Read(params string[] path) => File.ReadAllText(Path.Combine([Root, .. path]));

    [Fact]
    public void EveryClassThatOwnsAProcessWidePlace_IsNamedInTheArchitectureSection()
    {
        var section = SourceFile.SectionOf(Read("Src", "MatPlotLibNet", "ARCHITECTURE.md"), "Process-wide state");
        var owners = RepositoryTree.SourceFiles()
            .SelectMany(file => OwnersOfWritablePlaces(file.Code))
            .Concat(SelfFilling)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        var unnamed = owners.Where(owner => !Regex.IsMatch(section, $@"\b{Regex.Escape(owner)}\b")).ToArray();

        Assert.True(owners.Length >= 12, $"The scan found only {owners.Length} owners; it has stopped seeing the places it exists for.");
        Assert.True(unnamed.Length == 0,
            $"ARCHITECTURE.md's section \"Process-wide state\" does not name: {string.Join(", ", unnamed)}.");
    }

    [Fact]
    public void TheScan_FindsAPlaceWhereOneIsWritten()
    {
        const string slot = "public static class Slot\n{\n    public static IThing Current\n    {\n        get => _c;\n        set => _c = value;\n    }\n}";
        const string readOnly = "public static class Fixed\n{\n    public static IThing Current { get; } = new();\n    public static int Count { get; private set; }\n}";
        const string register = "public static class Table\n{\n    public static RegisteredFont Register(string path) => null;\n}";
        const string raised = "public static class Bus\n{\n    public static event Action? Emitted;\n}";

        Assert.Equal(new[] { "Slot" }, OwnersOfWritablePlaces(slot));
        Assert.Empty(OwnersOfWritablePlaces(readOnly));
        Assert.Equal(new[] { "Table" }, OwnersOfWritablePlaces(register));
        Assert.Equal(new[] { "Bus" }, OwnersOfWritablePlaces(raised));
    }

    [Fact]
    public void ClassDesign_NamesEveryStaticThatIsAdmittedAndTheOneThatIsBanned()
    {
        var section = SourceFile.SectionOf(Read("CONTRIBUTING.md"), "Class design");

        foreach (var mustName in new[]
        {
            "extension(T value)", "factory", "domain noun", "process-wide", "global class", "`Helper`", "readonly record struct",
        })
        {
            Assert.True(section.Contains(mustName, StringComparison.Ordinal), $"CONTRIBUTING.md's section \"Class design\" does not name {mustName}.");
        }

        Assert.DoesNotContain("only legitimate exception", section, StringComparison.Ordinal);
    }

    [Fact]
    public void TheArchitectureSection_SaysWhatARegistryIsAndWhereTheSlotsAreTested()
    {
        var section = SourceFile.SectionOf(Read("Src", "MatPlotLibNet", "ARCHITECTURE.md"), "Process-wide state");

        foreach (var mustName in new[] { "GlobalState", "NamedTable", "RegisterRenderer", "ChartDiagnostics", "separated" })
        {
            Assert.True(section.Contains(mustName, StringComparison.Ordinal), $"The section \"Process-wide state\" does not name {mustName}.");
        }
    }
}
