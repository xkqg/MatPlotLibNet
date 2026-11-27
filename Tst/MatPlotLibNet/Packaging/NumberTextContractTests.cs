// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>A number, a date or a time becomes text in the invariant culture, through its one owner (<c>InvariantTextExtensions</c>):
/// <c>InvariantText</c> for one value, <c>string.Invariant($"...")</c> for a text with holes. The build refuses a <c>ToString()</c>,
/// <c>Parse</c>, <c>string.Format</c> or <c>Convert</c> without a culture (CA1305, <c>Src/.editorconfig</c>); this test refuses the
/// spellings the analyzer does not see, which write the value in the culture of the machine the code runs on: a hole of an
/// interpolated text that <c>string.Invariant</c> does not compose, a value joined to a text with <c>+</c>, and a value appended to a
/// builder or a writer. It compiles the sources of every package with the compiler and reads the type of each value, so a hole of a
/// count reads the same as a hole of a price.</summary>
/// <remarks>Not read: the frozen Notebooks package (never edited), and the files other steps run beside this one are rewriting (the
/// SVG render context in the surface split; the native controls, Wpf, Avalonia, Uno and
/// Maui, in their own step), which take the owner with those steps.</remarks>
public class NumberTextContractTests
{
    private static readonly string[] NotRead =
    [
        "Src/MatPlotLibNet.Notebooks/",
        "Src/MatPlotLibNet/Rendering/Svg/SvgRenderContext.cs",
        "Src/MatPlotLibNet.Wpf/",
        "Src/MatPlotLibNet.Avalonia/",
        "Src/MatPlotLibNet.Uno/",
        "Src/MatPlotLibNet.Maui/",
    ];

    private static readonly string[] CultureTypes =
    [
        "System.DateTimeOffset", "System.TimeSpan", "System.DateOnly", "System.TimeOnly", "System.Half", "System.Int128", "System.UInt128",
        "System.Numerics.BigInteger",
    ];

    private static readonly string[] Appends = ["Append", "AppendLine", "Insert", "Write", "WriteLine"];

    [Fact]
    public void EveryNumberDateAndTime_BecomesTextThroughTheOwner()
    {
        var packages = RepositoryTree.SourceFiles()
            .Where(file => !NotRead.Any(prefix => file.Path.StartsWith(prefix, StringComparison.Ordinal)))
            .GroupBy(file => file.Path.Split('/')[1]);

        var findings = packages.SelectMany(package => Findings(package.Key, package)).ToList();

        Assert.True(findings.Count == 0, $"{findings.Count} values become text in the culture of the machine:\n" + string.Join('\n', findings));
    }

    [Theory]
    [InlineData("string M(double d) => $\"at {d}\";")]
    [InlineData("string M(int count) => $\"{count} points\";")]
    [InlineData("string M(DateTime t) => $\"{t:yyyy}\";")]
    [InlineData("string M(double d) => \"at \" + d;")]
    [InlineData("string M(long n) => n + \"%\";")]
    [InlineData("void M(System.Text.StringBuilder sb, float f) => sb.Append(f);")]
    [InlineData("void M(System.Text.StringBuilder sb, int i) => sb.Append($\"#{i}\");")]
    [InlineData("void M(TextWriter w, decimal m) => w.Write(m);")]
    public void ASpellingThatWritesTheMachinesCulture_IsFound(string member) =>
        Assert.NotEmpty(Findings("MatPlotLibNet.Mcp", [new("Src/MatPlotLibNet.Mcp/Any.cs", $"namespace MatPlotLibNet.Mcp; class Any {{ {member} }}")]));

    [Theory]
    [InlineData("string M(double d) => string.Invariant($\"at {d:0.##}\");")]
    [InlineData("string M(double d) => d.InvariantText(\"0.##\");")]
    [InlineData("string M(string name, char c, bool b) => $\"{name}{c}{b}\" + name + c;")]
    [InlineData("string M(double a, double b) => (a + b).InvariantText();")]
    [InlineData("void M(System.Text.StringBuilder sb, string s, char c) => sb.Append(s).Append(c).Append(\"x\");")]
    public void TheOwnersVerbs_AndTextWithoutANumber_AreNoFinding(string member) =>
        Assert.Empty(Findings("MatPlotLibNet.Mcp", [new("Src/MatPlotLibNet.Mcp/Any.cs", $"namespace MatPlotLibNet.Mcp; class Any {{ {member} }}")]));

    /// <summary>Every place in <paramref name="files"/>, compiled as the package <paramref name="package"/>, where a value whose text
    /// depends on the culture becomes text without the owner.</summary>
    private static IEnumerable<string> Findings(string package, IEnumerable<SourceFile> files)
    {
        var compilation = SourceCompilation.Create(package, files, References(package));

        foreach (var tree in compilation.SyntaxTrees)
        {
            var model = compilation.GetSemanticModel(tree);
            foreach (var node in tree.GetRoot().DescendantNodes())
            {
                string? finding = node switch
                {
                    InterpolationSyntax hole when !ComposedByTheOwner(hole) && Culture(model, hole.Expression) =>
                        "a hole of an interpolated text that string.Invariant does not compose",
                    BinaryExpressionSyntax { RawKind: (int)SyntaxKind.AddExpression } add
                        when IsString(model, add) && (Culture(model, add.Left) || Culture(model, add.Right)) =>
                        "a value joined to a text with +",
                    InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax access } call
                        when Appends.Contains(access.Name.Identifier.Text) && AppendsAValue(model, call) =>
                        "a value appended to a builder or a writer",
                    _ => null,
                };
                if (finding is not null)
                {
                    var at = node.GetLocation().GetLineSpan().StartLinePosition;
                    yield return $"{tree.FilePath}:{at.Line + 1}:{at.Character + 1}: {finding}: {SourceFile.LinesOf(node.ToString())[0].Trim()}";
                }
            }
        }
    }

    /// <summary>Whether the interpolated text the hole stands in is the one argument of <c>string.Invariant</c>.</summary>
    private static bool ComposedByTheOwner(InterpolationSyntax hole) =>
        hole.Parent is InterpolatedStringExpressionSyntax { Parent: ArgumentSyntax { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax call } } }
        && call.Expression.ToString() is "string.Invariant";

    private static bool IsString(SemanticModel model, ExpressionSyntax expression) =>
        model.GetTypeInfo(expression).Type?.SpecialType == SpecialType.System_String;

    /// <summary>Whether the call is a builder's or a writer's overload that writes a value whose text depends on the culture (its
    /// <c>value</c> parameter), as <c>StringBuilder.Append(double)</c> and <c>TextWriter.Write(int)</c> do.</summary>
    private static bool AppendsAValue(SemanticModel model, InvocationExpressionSyntax call) =>
        model.GetSymbolInfo(call).Symbol is IMethodSymbol method && WritesText(method.ContainingType)
        && method.Parameters.Any(parameter => parameter.Name == "value" && Culture(parameter.Type));

    private static bool WritesText(INamedTypeSymbol? type)
    {
        for (; type is not null; type = type.BaseType)
        {
            if (type.ToDisplayString() is "System.Text.StringBuilder" or "System.IO.TextWriter") return true;
        }

        return false;
    }

    private static bool Culture(SemanticModel model, ExpressionSyntax expression) => Culture(model.GetTypeInfo(expression).Type);

    /// <summary>Whether a value of the type has a text that depends on the culture: a number of any kind, a date or a time, or a
    /// nullable one.</summary>
    private static bool Culture(ITypeSymbol? type)
    {
        if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable)
        {
            type = nullable.TypeArguments[0];
        }

        return type is not null && (type.SpecialType is >= SpecialType.System_SByte and <= SpecialType.System_Double
            or SpecialType.System_DateTime or SpecialType.System_IntPtr or SpecialType.System_UIntPtr
            || CultureTypes.Contains(type.ToDisplayString()));
    }

    /// <summary>The assemblies the package compiles against: the framework, and every assembly beside the test but the package's own.</summary>
    private static IEnumerable<MetadataReference> References(string package)
    {
        return SourceCompilation.FrameworkPaths
            .Concat(Directory.EnumerateFiles(AppContext.BaseDirectory, "*.dll"))
            .GroupBy(Path.GetFileNameWithoutExtension, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Where(path => !string.Equals(Path.GetFileNameWithoutExtension(path), package, StringComparison.OrdinalIgnoreCase)
                           && !Path.GetFileNameWithoutExtension(path).StartsWith("MatPlotLibNet.Tests", StringComparison.OrdinalIgnoreCase))
            .Select(path => MetadataReference.CreateFromFile(path));
    }
}
