// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>A unit test says what the code does, never how long it took on a box that is running other work: the same line of
/// code took 38 ms on a quiet box and 2,295 ms on a busy one, and a verdict that depends on which was the case is not one
/// about the code. A test that wants a speed measures it in the benchmark project, where it is read and compared rather than
/// asserted, or asserts a fact that makes the speed (the work that was done, the bytes that were allocated). This holds that no
/// test in the repository asserts on a stopwatch, or on a number taken from one.</summary>
public class WallClockContractTests
{
    /// <summary>A name that holds an elapsed time: <c>double avgMs = sw.Elapsed.TotalMilliseconds / n;</c>.</summary>
    private static readonly Regex Declaration = new(
        @"\b(?:var|double|long|int|TimeSpan)\s+(?<name>\w+)\s*=[^;]*\b(?:Elapsed|ElapsedMilliseconds|Stopwatch)\b",
        RegexOptions.CultureInvariant);

    /// <summary>The lines of a file that assert on a clock: an assertion naming <c>Elapsed</c>, a stopwatch, or a name that was
    /// declared from one, on its own line or on the line the statement continues on.</summary>
    internal static IReadOnlyList<string> AssertionsOnAClock(IReadOnlyList<string> lines)
    {
        var names = lines.Select(line => Declaration.Match(line))
            .Where(m => m.Success)
            .Select(m => m.Groups["name"].Value)
            .ToHashSet(StringComparer.Ordinal);

        var found = new List<string>();
        for (int i = 0; i < lines.Count; i++)
        {
            if (!Regex.IsMatch(lines[i], @"\bAssert\.\w+\(")) continue;
            string statement = lines[i] + (i + 1 < lines.Count && !lines[i].TrimEnd().EndsWith(';') ? lines[i + 1] : "");
            if (Regex.IsMatch(statement, @"\.Elapsed\b|\bStopwatch\b") || NamesAny(statement, names))
            {
                found.Add($"{i + 1}: {lines[i].Trim()}");
            }
        }

        return found;
    }

    private static bool NamesAny(string line, HashSet<string> names) =>
        names.Any(name => Regex.IsMatch(line, $@"\b{Regex.Escape(name)}\b"));

    [Fact]
    public void NoTestAssertsOnAClock()
    {
        var around = new List<string>();
        foreach (var file in RepositoryTree.TestFiles().Where(f => !string.Equals(Path.GetFileName(f.Path), nameof(WallClockContractTests) + ".cs", StringComparison.OrdinalIgnoreCase)))
        {
            foreach (var line in AssertionsOnAClock(file.Lines))
            {
                around.Add($"{file.Path}:{line}");
            }
        }

        Assert.True(around.Count == 0,
            $"{around.Count} assertion(s) judge a time, which a busy box changes without the code changing:\n" + string.Join("\n", around));
    }

    [Fact]
    public void TheScan_SeesAnAssertionOnADerivedName_AndNotOneOnAFact()
    {
        string[] clocked =
        [
            "var sw = Stopwatch.StartNew();",
            "double avgMs = sw.Elapsed.TotalMilliseconds / 20;",
            "double budget = IsCi ? 7500 : 1500;",
            "Assert.True(avgMs < budget,",
            "    \"slow\");",
        ];
        string[] factual =
        [
            "var sw = Stopwatch.StartNew();",
            "Console.WriteLine($\"{sw.Elapsed.TotalMilliseconds:F1}ms\");",
            "Assert.Equal(20, count);",
        ];

        Assert.Single(AssertionsOnAClock(clocked));
        Assert.Empty(AssertionsOnAClock(factual));
    }
}
