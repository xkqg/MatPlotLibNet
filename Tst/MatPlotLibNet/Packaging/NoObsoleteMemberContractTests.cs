// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>No package of the library carries an <c>[Obsolete]</c> member. A member that has a better replacement is removed in the
/// release that brings the replacement, and the change log says what replaced it: a member kept beside its replacement is a second way
/// to do one thing, which every reader has to tell apart and every later change has to keep working.</summary>
public class NoObsoleteMemberContractTests
{
    /// <summary>An <c>Obsolete</c> attribute, with or without arguments and with or without its namespace.</summary>
    private static readonly Regex ObsoleteAttribute = new(
        @"\[\s*(?:System\s*\.\s*)?Obsolete(?:Attribute)?\s*(?:\(|\])", RegexOptions.CultureInvariant);

    /// <summary>Every line of <paramref name="file"/> that carries an <c>Obsolete</c> attribute, as <c>path:line</c>.</summary>
    /// <param name="file">The file to read.</param>
    /// <returns>One finding per line that carries the attribute.</returns>
    internal static IEnumerable<string> ObsoleteMarkersIn(SourceFile file) =>
        file.Lines
            .Select((line, index) => (line, index))
            .Where(row => ObsoleteAttribute.IsMatch(row.line))
            .Select(row => $"{file.Path}:{row.index + 1}: {row.line.Trim()}");

    [Fact]
    public void NoSourceFileOfAnyPackage_CarriesAnObsoleteMember()
    {
        var findings = RepositoryTree.SourceFiles().SelectMany(file => ObsoleteMarkersIn(file)).ToList();

        Assert.True(findings.Count == 0,
            $"{findings.Count} members are obsolete; remove each with its tests and name its replacement in the change log:\n"
            + string.Join('\n', findings));
    }

    [Fact]
    public void TheScan_ReadsTheSatellitePackagesToo()
    {
        var files = RepositoryTree.SourceFiles().Select(file => file.Path).ToList();

        Assert.Contains(files, path => path.StartsWith("Src/MatPlotLibNet/", StringComparison.Ordinal));
        Assert.Contains(files, path => path.StartsWith("Src/MatPlotLibNet.Skia/", StringComparison.Ordinal));
        Assert.Contains(files, path => path.StartsWith("Src/MatPlotLibNet.Geo/", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("    [Obsolete(\"Use the other one.\")]")]
    [InlineData("[Obsolete]")]
    [InlineData("[System.Obsolete(\"gone\", error: false)]")]
    [InlineData("    [ObsoleteAttribute(\"gone\")] public int Old => 0;")]
    public void AnObsoleteAttribute_IsFound_InEveryShape(string line)
    {
        Assert.Single(ObsoleteMarkersIn(new SourceFile("Src/Any.cs", line)));
    }

    [Theory]
    [InlineData("var obsolete = member.GetCustomAttribute<ObsoleteAttribute>();")]
    [InlineData("/// <remarks>The obsolete form went in an earlier release.</remarks>")]
    [InlineData("private static bool IsObsolete(MemberInfo m) => false;")]
    public void ALineThatOnlyNamesTheWord_IsNotFound(string line)
    {
        Assert.Empty(ObsoleteMarkersIn(new SourceFile("Src/Any.cs", line)));
    }
}
