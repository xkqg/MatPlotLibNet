// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MatPlotLibNet.Wiki.Tests;

/// <summary>
/// A program of the wiki, compiled as a person's console project would compile it: its blocks put together as one, against
/// the packages as built here, with the usings a console program has without asking, and a warning counted as a failure.
/// </summary>
internal static class WikiProgramExtensions
{
    // What a console project has without a using of its own: the SDK's implicit usings.
    private static readonly SyntaxTree ImplicitUsings = CSharpSyntaxTree.ParseText("""
        global using System;
        global using System.Collections.Generic;
        global using System.IO;
        global using System.Linq;
        global using System.Net.Http;
        global using System.Threading;
        global using System.Threading.Tasks;
        """);

    // What the .NET SDK leaves unsaid in every project it builds: that one version of an assembly is taken for another the
    // runtime unifies it with.
    private static readonly Dictionary<string, ReportDiagnostic> SdkQuiets = new()
    {
        ["CS1701"] = ReportDiagnostic.Suppress,
        ["CS1702"] = ReportDiagnostic.Suppress,
    };

    // Every assembly this suite runs with, the packages as built among them: read once per run, because each of the
    // wiki's two hundred compilations would otherwise read the same few hundred files again.
    private static readonly Lazy<MetadataReference[]> References = new(() =>
    [
        .. ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path)),
    ]);

    /// <summary>
    /// The program compiled: its blocks put together as one (every block's usings first, then every block's statements in
    /// the order they stand, then every type they declare) against every assembly this suite runs with.
    /// </summary>
    /// <param name="program">The program.</param>
    /// <returns>Every warning and error.</returns>
    public static IReadOnlyList<WikiDiagnostic> Compiled(this WikiProgram program)
    {
        var units = program.Blocks.Select(block => CSharpSyntaxTree.ParseText(block.Code).GetCompilationUnitRoot()).ToArray();
        var statements = units.SelectMany(unit => unit.Members.OfType<GlobalStatementSyntax>()).ToArray();
        var source = CSharpSyntaxTree.ParseText(string.Join(
            '\n',
            [
                .. units.SelectMany(unit => unit.Usings).Select(each => each.ToString()).Distinct(StringComparer.Ordinal),
                .. statements.Select(statement => statement.ToFullString()),
                .. units.SelectMany(unit => unit.Members.Where(member => member is not GlobalStatementSyntax)).Select(member => member.ToFullString()),
            ]));
        var compilation = CSharpCompilation.Create(
            $"Wiki{program.Line}",
            [ImplicitUsings, source],
            References.Value,
            new CSharpCompilationOptions(
                    statements.Length > 0 ? OutputKind.ConsoleApplication : OutputKind.DynamicallyLinkedLibrary,
                    nullableContextOptions: NullableContextOptions.Enable)
                .WithSpecificDiagnosticOptions(SdkQuiets));

        using var image = new MemoryStream();

        return [.. compilation.Emit(image).Diagnostics
            .Where(each => each.Severity >= DiagnosticSeverity.Warning)
            .Select(each => new WikiDiagnostic(each.Id, each.GetMessage(System.Globalization.CultureInfo.InvariantCulture), CodeAt(each)))];
    }

    // The line of the program a diagnostic points at, so a failure reads as the code a person sees on the page.
    private static string CodeAt(Diagnostic diagnostic) =>
        diagnostic.Location.SourceTree is { } tree
            ? tree.GetText().Lines[diagnostic.Location.GetLineSpan().StartLinePosition.Line].ToString().Trim()
            : string.Empty;
}

/// <summary>A warning or error of a compiled program.</summary>
/// <param name="Id">The compiler's id for it, such as CS1061.</param>
/// <param name="Message">What the compiler says.</param>
/// <param name="Code">The line of code it points at, trimmed.</param>
internal readonly record struct WikiDiagnostic(string Id, string Message, string Code)
{
    /// <inheritdoc />
    public override string ToString() => $"{Id} {Message} -- at: {Code}";
}
