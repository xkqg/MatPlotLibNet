// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace MatPlotLibNet.Wiki.Tests;

/// <summary>
/// The packages this suite cannot build, read from their source: the WPF, Uno and MAUI controls need Windows or a workload that the
/// runner does not have, so reflection cannot see their types. A document that names a member under one of their types is read against
/// the files that declare the type instead: the member counts when a declaration line of those files names it.
/// </summary>
internal sealed partial class UnbuiltPackages
{
    private static readonly string[] Packages = ["Wpf", "Uno", "Maui"];

    /// <summary>The source of the packages.</summary>
    public static UnbuiltPackages Sources { get; } = new();

    private readonly ILookup<string, SourceFile> _declaring;

    private readonly HashSet<string> _names;

    private UnbuiltPackages()
    {
        var files = Packages.SelectMany(package => RepositoryTree.Files($"Src/MatPlotLibNet.{package}", "*.cs")).ToList();

        _declaring = files.SelectMany(file => TypeDeclaration().Matches(file.Code).Select(declared => (Name: declared.Groups["name"].Value, File: file)))
            .ToLookup(each => each.Name, each => each.File, StringComparer.Ordinal);
        _names = [.. files.SelectMany(file => Name().Matches(file.Code).Select(match => match.Value))];
    }

    [GeneratedRegex(@"\b[A-Za-z_]\w*\b")]
    private static partial Regex Name();

    /// <summary>Whether the code of the packages uses a name: a type or a member of the toolkit they are built on (WPF's
    /// <c>InvalidateVisual</c>, WinUI's <c>DispatcherQueue</c>), which the suite has no assembly of, or one of their own.</summary>
    /// <param name="name">The name.</param>
    /// <returns>True when their code names it.</returns>
    public bool Uses(string name) => _names.Contains(name);

    [GeneratedRegex(@"\b(?:class|struct|interface|record|enum)\s+(?<name>[A-Z]\w*)")]
    private static partial Regex TypeDeclaration();

    /// <summary>Whether a source file of the packages declares a type of the name.</summary>
    /// <param name="type">The type's name.</param>
    /// <returns>True when one does.</returns>
    public bool Declares(string type) => _declaring.Contains(type);

    /// <summary>Whether a file that declares the type has a declaration line that names the member: a modifier, then the member followed
    /// by a bracket, a brace, an arrow, an equals sign, a semicolon or a type argument; or a parameter of a record.</summary>
    /// <param name="type">The type's name.</param>
    /// <param name="member">The member's name.</param>
    /// <returns>True when a declaration names it.</returns>
    public bool Has(string type, string member)
    {
        var name = Regex.Escape(member);
        var declaration = new Regex($@"\b(?:public|internal|protected|private|static|override|virtual|abstract|const|event|readonly)\b[^;{{=(]*\b{name}\b\s*(?:\(|\{{|=>|=|;|<)|\b(?:record|class|struct)\b[^(]*\([^)]*\b{name}\b");

        return _declaring[type].Any(file => file.CodeLines.Any(declaration.IsMatch));
    }
}
