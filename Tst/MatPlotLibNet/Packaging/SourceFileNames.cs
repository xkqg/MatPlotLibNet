// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>How a source file is named: after a type it declares. The name of a file, up to its first dot (<c>Owner.Part.cs</c> is a part
/// of <c>Owner</c>, <c>Table.g.cs</c> the generated half of <c>Table</c>), is the name of one of the types the file declares at its top
/// level, whatever the kind of the type (a class, a struct, a record, an interface, an enum, a delegate) and the arity of its name
/// (<c>Foo&lt;T&gt;</c> is in <c>Foo.cs</c>). A file that declares no type at all holds the assembly's attributes and nothing else: it
/// has no type to be named after, and it is named for what it holds.</summary>
internal static class SourceFileNames
{
    /// <summary>Every file of <paramref name="files"/> that is named after no type it declares, and every file that declares no type
    /// and holds something but the attributes of its assembly.</summary>
    /// <param name="files">The C# files to read, each with its path from the repository root.</param>
    /// <returns>One sentence per file, with the types it declares; empty when every file is named after one of its types.</returns>
    internal static IReadOnlyList<string> Unnamed(IEnumerable<SourceFile> files)
    {
        var findings = new List<string>();
        foreach (var file in files)
        {
            var unit = CSharpSyntaxTree.ParseText(file.Text).GetCompilationUnitRoot();
            var declared = DeclaredAtTopLevel(unit);
            string stem = StemOf(file.Path);
            if (declared.Count > 0)
            {
                if (!declared.Contains(stem, StringComparer.Ordinal))
                {
                    findings.Add($"{file.Path}: declares {string.Join(", ", declared)}, and none is named {stem}");
                }
            }
            else if (!HoldsOnlyAssemblyAttributes(unit))
            {
                findings.Add($"{file.Path}: declares no type, and holds more than the attributes of its assembly");
            }
        }

        return findings;
    }

    /// <summary>The part of the file name before its first dot.</summary>
    private static string StemOf(string path)
    {
        string name = path[(path.LastIndexOf('/') + 1)..];
        int dot = name.IndexOf('.', StringComparison.Ordinal);
        return dot < 0 ? name : name[..dot];
    }

    /// <summary>The names of the types declared in the unit or in its namespaces, not those nested in another type.</summary>
    private static List<string> DeclaredAtTopLevel(CompilationUnitSyntax unit) =>
    [
        .. unit.DescendantNodes(node => node is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax)
            .Select(node => node switch
            {
                BaseTypeDeclarationSyntax type => type.Identifier.ValueText,
                DelegateDeclarationSyntax declared => declared.Identifier.ValueText,
                _ => null,
            })
            .OfType<string>(),
    ];

    private static bool HoldsOnlyAssemblyAttributes(CompilationUnitSyntax unit) =>
        unit.Members.Count == 0
        && unit.AttributeLists.Count > 0
        && unit.AttributeLists.All(list => list.Target?.Identifier.IsKind(SyntaxKind.AssemblyKeyword) == true);
}
