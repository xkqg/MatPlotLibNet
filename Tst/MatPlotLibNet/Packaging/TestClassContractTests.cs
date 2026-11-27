// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>One test class per class. The tests of a type of the library are one class, named for the type, alone in a file of its
/// own name: a second class for the same type, named with a word for the kind of test it holds (<c>Edge</c>, <c>Generic</c>,
/// <c>Unit</c>, <c>Integration</c>, ...), is where the tests of one type drift apart and repeat each other,
/// and a file that holds several classes hides all but the one its name gives. The scans read every test project; a class named
/// twice in one project is a second class too.</summary>
public class TestClassContractTests
{
    /// <summary>The words that name a kind of test rather than a behaviour, longest first so a compound word is read whole. A word that
    /// names the round of work that wrote a class (Coverage, Branch, EdgeCase, Baseline, ...) is refused in every class name by a rule of
    /// <see cref="OwnedRules.TestCodeRules"/>, so it is not repeated here.</summary>
    private static readonly string[] KindWords = ["Edge", "Generic", "Precision", "GlobalUse", "Global", "Integration", "Unit"];

    private static readonly Regex TopLevelTestClass = new(
        @"^(?:public |internal )?(?:sealed |abstract |static )*(?:partial )?class (\w+Tests)\b", RegexOptions.Multiline | RegexOptions.CultureInvariant);

    private static readonly Regex DeclaredType = new(
        @"\b(?:class|record|struct|interface|enum)\s+(?:struct\s+|class\s+)?([A-Z]\w*)", RegexOptions.CultureInvariant);

    /// <summary>How many types of each name the library declares, read from its source (two namespaces may each hold a type of one
    /// name, and then each has its test class of that name).</summary>
    private static IReadOnlyDictionary<string, int> LibraryTypes(IEnumerable<SourceFile> sources) =>
        sources.SelectMany(file => DeclaredType.Matches(file.Text).Select(match => match.Groups[1].Value))
            .GroupBy(name => name, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

    /// <summary>Every test class of <paramref name="tests"/> that is a second class: a type of <paramref name="libraryTypes"/> with a
    /// word for the kind of test after its name, and a class named in one test project more times than the library has types of
    /// that name.</summary>
    /// <param name="tests">The test files to read.</param>
    /// <param name="libraryTypes">How many types of each name the library declares.</param>
    /// <returns>One sentence per second class; empty when every type has one class.</returns>
    internal static IReadOnlyList<string> SecondClasses(IEnumerable<SourceFile> tests, IReadOnlyDictionary<string, int> libraryTypes)
    {
        var findings = new List<string>();
        var byProject = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var file in tests)
        {
            string project = file.Path.Split('/')[1];
            foreach (Match match in TopLevelTestClass.Matches(file.Text))
            {
                string name = match.Groups[1].Value;
                string key = $"{project}/{name}";
                if (!byProject.TryGetValue(key, out var places))
                {
                    byProject[key] = places = [];
                }

                places.Add(file.Path);
                string stem = name[..^"Tests".Length];
                if (libraryTypes.ContainsKey(stem))
                {
                    continue;
                }

                string? kind = KindWords.FirstOrDefault(word => stem.EndsWith(word, StringComparison.Ordinal) && libraryTypes.ContainsKey(stem[..^word.Length]));
                if (kind is not null)
                {
                    findings.Add($"{file.Path}: {name} is a second class for {stem[..^kind.Length]}, named for the kind of test ({kind}): its tests belong in {stem[..^kind.Length]}Tests");
                }
            }
        }

        findings.AddRange(byProject.Where(pair => pair.Value.Count > Math.Max(1, libraryTypes.GetValueOrDefault(pair.Key[(pair.Key.IndexOf('/') + 1)..^"Tests".Length])))
            .Select(pair => $"{pair.Key} is declared {pair.Value.Count} times: {string.Join(", ", pair.Value)}"));
        return findings;
    }

    /// <summary>Every top-level test class of <paramref name="tests"/> that stands in a file named for another class.</summary>
    /// <param name="tests">The test files to read.</param>
    /// <returns>One sentence per class; empty when each test file holds the one class its name gives.</returns>
    internal static IReadOnlyList<string> ClassesInAFileOfAnotherName(IEnumerable<SourceFile> tests) =>
    [
        .. from file in tests
           let fileName = Path.GetFileNameWithoutExtension(file.Path)
           from Match match in TopLevelTestClass.Matches(file.Text)
           where match.Groups[1].Value != fileName
           select $"{file.Path}: holds {match.Groups[1].Value}, which belongs in a file of its own name",
    ];

    /// <summary>Every top-level test class of <paramref name="tests"/> named for a type the library deleted (<paramref name="deleted"/>,
    /// a name that is no type of <paramref name="libraryTypes"/> now): its tests are of code that moved into another type, and moving
    /// code moves its tests, into the test class of the type that owns that code now.</summary>
    /// <param name="tests">The test files to read.</param>
    /// <param name="deleted">The names the library deleted.</param>
    /// <param name="libraryTypes">How many types of each name the library declares.</param>
    /// <returns>One sentence per class; empty when no class is named for a deleted type.</returns>
    internal static IReadOnlyList<string> ClassesNamedForADeletedType(IEnumerable<SourceFile> tests, IEnumerable<string> deleted,
        IReadOnlyDictionary<string, int> libraryTypes)
    {
        var gone = deleted.Where(name => !name.Contains('.') && !libraryTypes.ContainsKey(name)).ToHashSet(StringComparer.Ordinal);
        return
        [
            .. from file in tests
               from Match match in TopLevelTestClass.Matches(file.Text)
               let stem = match.Groups[1].Value[..^"Tests".Length]
               where gone.Contains(stem)
               select $"{file.Path}: {match.Groups[1].Value} is named for {stem}, which the library deleted: its tests belong in the test class of the type that owns that code now",
        ];
    }

    /// <summary>The receivers of the types the library folded into the type that uses them: for each, the test file of the type that owns
    /// the code now and a member of that code its tests call. The tests of a deleted type moved with its code, into the one test class of
    /// the receiving type, never into a class renamed for the member, the part file or the type the code extends. That the folded name is
    /// gone is held by <see cref="DeletedName"/> and <see cref="NoTestClass_IsNamedForATypeTheLibraryDeleted"/>; a type whose code became
    /// the locals of a verb left no member to test and is held by its deleted name alone.</summary>
    internal static readonly (string TestFile, string Member)[] ReceiversOfFoldedTypes =
    [
        ("Tst/MatPlotLibNet.Avalonia/MplChartSurfaceTests.cs", "ButtonPressedBy"),
        ("Tst/MatPlotLibNet.Geo/GeoFlowTests.cs", "Between"),
        ("Tst/MatPlotLibNet.Interactive/ChartServerTests.cs", "Generate"),
        ("Tst/MatPlotLibNet.Mcp/ChartSpecReaderTests.cs", "NormaliseColours"),
        ("Tst/MatPlotLibNet.Skia/Animation/GifTransformTests.cs", "BuildPalette"),
        ("Tst/MatPlotLibNet.Skia/Animation/GifTransformTests.cs", "LzwCompress"),
        ("Tst/MatPlotLibNet.Skia/PngTransformTests.cs", "WithTextChunks"),
        ("Tst/MatPlotLibNet/Extensions/ArrayLengthExtensionsTests.cs", "ThrowIfEmpty"),
        ("Tst/MatPlotLibNet/Indicators/EhlersSuperSmootherTests.cs", "SuperSmooth"),
        ("Tst/MatPlotLibNet/Builders/AxisVerbsTests.cs", "ApplyAxisColor"),
        ("Tst/MatPlotLibNet/Builders/AxisVerbsTests.cs", "SetLabel"),
        ("Tst/MatPlotLibNet/Builders/AxisVerbsTests.cs", "AutoDates"),
        ("Tst/MatPlotLibNet/Models/ColorBarTests.cs", "AskedFor"),
        ("Tst/MatPlotLibNet/Rendering/SeriesRenderers/PairGridSeriesRendererTests.cs", "ComputeCellRects"),
        ("Tst/MatPlotLibNet/Builders/DistributionAxesExtensionsTests.cs", "ProductLimit"),
        ("Tst/MatPlotLibNet/Models/Series/ViolinSeriesTests.cs", "Outline"),
        ("Tst/MatPlotLibNet/Models/Series/XY/RelativeRotationSeriesTests.cs", "ComputeRsData"),
        ("Tst/MatPlotLibNet/Rendering/SeriesRenderers/SwarmplotSeriesRendererTests.cs", "OnPixels"),
        ("Tst/MatPlotLibNet/Rendering/CartesianParts/CartesianAnnotationsPartTests.cs", "PlaceText"),
        ("Tst/MatPlotLibNet/Rendering/ChartRendererTests.cs", "RenderFigureOverlays"),
        ("Tst/MatPlotLibNet/Rendering/LegendRendering/LegendLayoutCalculatorTests.cs", "MeasureLegendBox"),
        ("Tst/MatPlotLibNet/Rendering/LegendRendering/LegendRendererTests.cs", "DrawMarker"),
        ("Tst/MatPlotLibNet/Rendering/SeriesRenderers/StreamplotSeriesRendererTests.cs", "StreamlinesOf"),
        ("Tst/MatPlotLibNet/Rendering/Svg/SvgWriterExtensionsTests.cs", "ToSvgName"),
        ("Tst/MatPlotLibNet/Rendering/Svg/SvgTransformTests.cs", "ServerInteractionScript"),
        ("Tst/MatPlotLibNet/Rendering/Svg/SvgWriterExtensionsTests.cs", "AppendView"),
        ("Tst/MatPlotLibNet/Rendering/TextMeasurement/DefaultFontMetricsTests.cs", "GetWidth"),
        ("Tst/MatPlotLibNet/Rendering/ThreeD/Normalized3DPointTests.cs", "FormatWire"),
        ("Tst/MatPlotLibNet/Builders/GridAxesExtensionsTests.cs", "StepOf"),
    ];

    /// <summary>Every row of <paramref name="receivers"/> whose tests do not stand in the test class of the type that owns the code now: the
    /// test file is missing or named for no type of the library, it does not call the member, or the type it is named for declares no such
    /// member (the code moved on, and its tests did not).</summary>
    /// <param name="sources">The library's source files.</param>
    /// <param name="tests">The test files.</param>
    /// <param name="receivers">The test file of each receiving type and a member the moved tests call.</param>
    /// <returns>One sentence per row out of place; empty when every moved test stands with its code.</returns>
    internal static IReadOnlyList<string> MovedTestsOutOfPlace(IReadOnlyList<SourceFile> sources, IEnumerable<SourceFile> tests,
        IEnumerable<(string TestFile, string Member)> receivers)
    {
        var declaring = sources.SelectMany(file => DeclaredType.Matches(file.Text).Select(match => (Type: match.Groups[1].Value, File: file)))
            .ToLookup(pair => pair.Type, pair => pair.File, StringComparer.Ordinal);
        var testFiles = tests.ToDictionary(file => file.Path, StringComparer.Ordinal);
        var findings = new List<string>();
        foreach (var (testFile, member) in receivers)
        {
            string receiver = Path.GetFileNameWithoutExtension(testFile)[..^"Tests".Length];
            if (!declaring.Contains(receiver))
            {
                findings.Add($"{testFile} is named for {receiver}, which is no type of the library: the tests of {member} belong in the test class of the type that owns it");
            }
            else if (!declaring[receiver].Any(file => Regex.IsMatch(file.Text, $@"\b(?:internal|public|private|protected)\b[^;=\n]*\b{member}\s*[<(]", RegexOptions.CultureInvariant)))
            {
                findings.Add($"{receiver} declares no {member}: the code moved on, and its tests belong in the test class of its owner now");
            }

            if (!testFiles.TryGetValue(testFile, out var file))
            {
                findings.Add($"{testFile} is missing: the tests of {member} belong in the test class of {receiver}");
            }
            else if (!Regex.IsMatch(file.Text, $@"\b{member}\s*[<(]", RegexOptions.CultureInvariant))
            {
                findings.Add($"{testFile} does not call {member}: its tests stand in another class than the one of {receiver}");
            }
        }

        return findings;
    }

    [Fact]
    public void TheTestsOfAFoldedType_StandInTheTestClassOfItsReceiver()
    {
        var findings = MovedTestsOutOfPlace(RepositoryTree.SourceFiles(), RepositoryTree.TestFiles(), ReceiversOfFoldedTypes);

        Assert.True(findings.Count == 0, $"{findings.Count} moved tests out of place:\n" + string.Join('\n', findings));
    }

    /// <summary>The scan passes a receiver whose test class calls the moved member, and finds a class named for a member, a missing class,
    /// a receiver's class that does not call the member, and a member that moved on to another type.</summary>
    [Fact]
    public void TheScan_FindsMovedTestsOutOfPlace()
    {
        SourceFile[] sources =
        [
            new("Src/A/Owner.cs", "internal sealed class Owner\n{\n    internal static int Moved(int x) => x;\n}\n"),
            new("Src/A/Other.cs", "internal sealed class Other\n{\n    internal int Away() => 1;\n}\n"),
        ];
        SourceFile[] tests =
        [
            new("Tst/A/OwnerTests.cs", "public class OwnerTests\n{\n    int A() => Owner.Moved(1);\n}\n"),
            new("Tst/A/MovedTests.cs", "public class MovedTests\n{\n    int A() => Owner.Moved(1);\n}\n"),
            new("Tst/A/OtherTests.cs", "public class OtherTests\n{\n}\n"),
        ];

        var found = MovedTestsOutOfPlace(sources, tests,
        [
            ("Tst/A/OwnerTests.cs", "Moved"),
            ("Tst/A/MovedTests.cs", "Moved"),
            ("Tst/B/OwnerTests.cs", "Moved"),
            ("Tst/A/OtherTests.cs", "Away"),
            ("Tst/A/OtherTests.cs", "Moved"),
        ]);

        Assert.Equal(
        [
            "Tst/A/MovedTests.cs is named for Moved, which is no type of the library: the tests of Moved belong in the test class of the type that owns it",
            "Tst/B/OwnerTests.cs is missing: the tests of Moved belong in the test class of Owner",
            "Tst/A/OtherTests.cs does not call Away: its tests stand in another class than the one of Other",
            "Other declares no Moved: the code moved on, and its tests belong in the test class of its owner now",
            "Tst/A/OtherTests.cs does not call Moved: its tests stand in another class than the one of Other",
        ], found);
    }

    [Fact]
    public void NoTestClass_IsNamedForATypeTheLibraryDeleted()
    {
        var findings = ClassesNamedForADeletedType(RepositoryTree.TestFiles(), DeletedName.All.Select(deleted => deleted.Name), LibraryTypes(RepositoryTree.SourceFiles()));

        Assert.True(findings.Count == 0, $"{findings.Count} test classes named for a deleted type:\n" + string.Join('\n', findings));
    }

    /// <summary>The scan finds a class named for a deleted type, and passes a class named for a living type of that name, a class named
    /// for a behaviour, and a deleted member (a dotted name names no class).</summary>
    [Fact]
    public void TheScan_FindsAClassNamedForADeletedType()
    {
        var types = new Dictionary<string, int>(StringComparer.Ordinal) { ["Live"] = 1 };
        SourceFile[] tests =
        [
            new("Tst/A/GoneTests.cs", "public class GoneTests\n{\n}\n"),
            new("Tst/A/LiveTests.cs", "public class LiveTests\n{\n}\n"),
            new("Tst/A/StrideTests.cs", "public class StrideTests\n{\n}\n"),
        ];

        var found = ClassesNamedForADeletedType(tests, ["Gone", "Live", "Stride.Of"], types);

        Assert.Equal(["Tst/A/GoneTests.cs: GoneTests is named for Gone, which the library deleted: its tests belong in the test class of the type that owns that code now"], found);
    }

    [Fact]
    public void ATypeOfTheLibrary_HasOneTestClass()
    {
        var findings = SecondClasses(RepositoryTree.TestFiles(), LibraryTypes(RepositoryTree.SourceFiles()));

        Assert.True(findings.Count == 0, $"{findings.Count} second classes:\n" + string.Join('\n', findings));
    }

    [Fact]
    public void ATestFile_HoldsOneTestClass_NamedAsTheFile()
    {
        var findings = ClassesInAFileOfAnotherName(RepositoryTree.TestFiles());

        Assert.True(findings.Count == 0, $"{findings.Count} classes in a file of another name:\n" + string.Join('\n', findings));
    }

    /// <summary>The scans find what they refuse and pass what they do not: a class named for the kind of test after a type's name, a
    /// class named twice in one project and a second class in a file are found; a class named for a behaviour of a type, a type whose own
    /// name ends in a kind word, the same name in two projects, and two classes of one name for the two types of that name pass.</summary>
    [Fact]
    public void TheScans_FindASecondClass_AndPassAClassNamedForABehaviour()
    {
        var types = new Dictionary<string, int>(StringComparer.Ordinal) { ["LogLocator"] = 1, ["PatchEdge"] = 1, ["Patch"] = 1, ["Fibonacci"] = 2 };
        SourceFile[] tests =
        [
            new("Tst/A/LogLocatorTests.cs", "public class LogLocatorTests\n{\n}\n"),
            new("Tst/A/LogLocatorEdgeTests.cs", "public class LogLocatorEdgeTests\n{\n}\n"),
            new("Tst/A/PatchEdgeTests.cs", "public class PatchEdgeTests\n{\n}\n"),
            new("Tst/A/LogLocatorStrideTests.cs", "public class LogLocatorStrideTests\n{\n}\n"),
            new("Tst/A/Other/LogLocatorTests.cs", "public class LogLocatorTests\n{\n}\n"),
            new("Tst/B/LogLocatorTests.cs", "public class LogLocatorTests\n{\n}\npublic class PatchUnitTests\n{\n}\n"),
            new("Tst/B/Indicators/FibonacciTests.cs", "public class FibonacciTests\n{\n}\n"),
            new("Tst/B/Tools/FibonacciTests.cs", "public class FibonacciTests\n{\n}\n"),
        ];

        var second = SecondClasses(tests, types);
        var misplaced = ClassesInAFileOfAnotherName(tests);

        Assert.Equal(3, second.Count);
        Assert.Contains(second, finding => finding.StartsWith("Tst/A/LogLocatorEdgeTests.cs: LogLocatorEdgeTests is a second class for LogLocator", StringComparison.Ordinal));
        Assert.Contains(second, finding => finding.StartsWith("Tst/B/LogLocatorTests.cs: PatchUnitTests is a second class for Patch", StringComparison.Ordinal));
        Assert.Contains("A/LogLocatorTests is declared 2 times: Tst/A/LogLocatorTests.cs, Tst/A/Other/LogLocatorTests.cs", second);
        Assert.Equal(["Tst/B/LogLocatorTests.cs: holds PatchUnitTests, which belongs in a file of its own name"], misplaced);
    }
}
