// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MatPlotLibNet.Wiki.Tests;

/// <summary>
/// A call of the packages a document writes, as it writes it: a code span of a markdown line or table cell (<c>ax.Plot(x, y)</c>, a
/// signature <c>WithYAxisTitleGap(double pixels)</c>), a fenced csharp block of a repository document, or the <c>&lt;c&gt;</c> and
/// <c>&lt;code&gt;</c> of an XML comment. The document says the call compiles: <see cref="CallCompilation"/> compiles it.
/// </summary>
/// <param name="Document">The document's name, for the message.</param>
/// <param name="Line">The line the call stands on, from one; for a block, the line of its first line of code.</param>
/// <param name="Code">The code as the document writes it.</param>
/// <param name="IsBlock">Whether the code is a block of statements and declarations rather than one call.</param>
/// <param name="Owners">The types a call written without a receiver stands under (the type of a methods table, the type an XML comment
/// stands in), the first that has the member first; empty when the document names none.</param>
/// <param name="Returns">The type a table's Returns column gives for the call, or null.</param>
internal readonly partial record struct CallClaim(string Document, int Line, string Code, bool IsBlock, IReadOnlyList<string> Owners, string? Returns)
{
    /// <summary>The names the code may read that are declared around it, each as its declaration writes it (<c>Point a</c>): the
    /// parameters of the member an XML comment documents.</summary>
    public IReadOnlyList<string> Scope { get; init; } = [];

    /// <summary>What a block is compiled as: statements and declarations, a notebook's cell, or a razor fence.</summary>
    public CodeBlockKind Kind { get; init; }

    private static readonly string[] CSharpFences = ["csharp", "cs", "c#"];

    /// <summary>The calls a markdown document writes in its prose and its tables, with the type each stands under when its table or its
    /// sentence names one (<see cref="MemberClaim"/> reads that).</summary>
    /// <param name="document">The document's name.</param>
    /// <param name="text">The document's text, a line feed between lines.</param>
    /// <returns>The calls, in the order they stand.</returns>
    public static IReadOnlyList<CallClaim> InlineOf(string document, string text)
    {
        var named = MemberClaim.Of(document, text).Where(claim => claim.Code is not null && claim.Spelling != MemberClaimSpelling.Dotted)
            .ToLookup(claim => (claim.Line, claim.Code!));
        var lines = SourceFile.MarkdownLinesOf(text);
        var paragraphs = ParagraphOwners(lines);

        // A call that no table or list puts under a type stands under the types its paragraph names: `AxisTransform` ... `Forward(value)`.
        return
        [
            .. MemberClaim.TextsOf(lines)
                .SelectMany(each => SourceFile.CodeSpansOf(each.Text).Where(span => !Absent().IsMatch(each.Text[..span.Index]))
                    .Select(span => (each.Number, span.Code, Context: each.Text + " " + each.Column, Before: each.Text[..span.Index])))
                .Where(each => IsCall(each.Code, each.Context, each.Before))
                .Select(each => named[(each.Number, each.Code)].FirstOrDefault() is { Code: not null } claim
                    ? new CallClaim(document, each.Number, each.Code, false, claim.Owners, claim.Returns)
                    : new CallClaim(document, each.Number, each.Code, false, IsBare(each.Code) && paragraphs.TryGetValue(each.Number, out var owners) ? owners : [], null)),
        ];
    }

    // The words right before a span that say the document names a call that is not there: `There is no synchronous `Show()``,
    // `**No `TryGet(out Figure)`**`.
    [System.Text.RegularExpressions.GeneratedRegex(@"\b(?:[Nn]o|[Nn]ot|[Nn]ever|[Ww]ithout)\s+(?:[a-z]+\s+)?\**\s*$")]
    private static partial System.Text.RegularExpressions.Regex Absent();

    // Whether a span is a call written without a receiver: a name, then its arguments.
    private static bool IsBare(string code) => CallText.Expression(code) is InvocationExpressionSyntax { Expression: SimpleNameSyntax };

    // For each line of prose, the types of the packages its paragraph names in code: the owners of a call written bare in it.
    private static Dictionary<int, IReadOnlyList<string>> ParagraphOwners(IReadOnlyList<MarkdownLine> lines)
    {
        var owners = new Dictionary<int, IReadOnlyList<string>>();

        for (var at = 0; at < lines.Count;)
        {
            var end = at;

            while (end < lines.Count && !lines[end].InFence && lines[end].Text.Trim().Length > 0 && !lines[end].Text.TrimStart().StartsWith('|'))
            {
                end++;
            }

            if (end == at)
            {
                at++;
                continue;
            }

            IReadOnlyList<string> named = [.. lines.Skip(at).Take(end - at).SelectMany(line => SourceFile.CodeSpansOf(line.Text))
                .Select(span => span.Code.Split('.', '(', '<')[0]).Where(PackageSurface.Loaded.IsType).Distinct()];

            for (var line = at; line < end; line++)
            {
                owners[lines[line].Number] = named;
            }

            at = end;
        }

        return owners;
    }

    /// <summary>The fenced csharp blocks of a markdown document, each a block of code.</summary>
    /// <param name="document">The document's name.</param>
    /// <param name="text">The document's text.</param>
    /// <returns>The blocks, in the order they stand.</returns>
    public static IReadOnlyList<CallClaim> BlocksOf(string document, string text) =>
    [
        .. SourceFile.FenceBlocksOf(text)
            .Where(block => CSharpFences.Contains(block.Open.Info, StringComparer.OrdinalIgnoreCase))
            .Select(block => new CallClaim(document, block.OpenAt + 2, string.Join('\n', block.Body), true, [], null)
            {
                Kind = block.Body.Any(line => line.StartsWith("#r ", StringComparison.Ordinal)) ? CodeBlockKind.NotebookCell : CodeBlockKind.Program,
            }),
        .. RazorBlocksOf(document, text),
    ];

    /// <summary>The razor fences of a markdown document that hold C#, each a block of code.</summary>
    /// <param name="document">The document's name.</param>
    /// <param name="text">The document's text.</param>
    /// <returns>The blocks, in the order they stand.</returns>
    public static IReadOnlyList<CallClaim> RazorBlocksOf(string document, string text) =>
    [
        .. SourceFile.FenceBlocksOf(text)
            .Where(block => block.Open.Info == "razor" && CallText.RazorAsCSharp(string.Join('\n', block.Body)) is not null)
            .Select(block => new CallClaim(document, block.OpenAt + 2, string.Join('\n', block.Body), true, [], null) { Kind = CodeBlockKind.Razor }),
    ];

    /// <summary>The C# blocks of a wiki page that its own compile sets aside: the blocks that need another head or the repository's tests,
    /// and the notebook cells. Their calls of the packages are calls all the same.</summary>
    /// <param name="page">The page.</param>
    /// <returns>The blocks, in the order they stand.</returns>
    public static IReadOnlyList<CallClaim> BlocksOf(WikiPage page) =>
    [
        .. page.Needing.Select(block => new CallClaim(page.Name, block.Line, block.Code, true, [], null))
            .Concat(page.Cells.Select(block => new CallClaim(page.Name, block.Line, block.Code, true, [], null) { Kind = CodeBlockKind.NotebookCell }))
            .OrderBy(claim => claim.Line),
    ];

    /// <summary>The calls the XML comments of a source file write in <c>&lt;c&gt;</c>, each under the type its comment stands in.</summary>
    /// <param name="file">The source file.</param>
    /// <returns>The calls, in the order they stand.</returns>
    public static IReadOnlyList<CallClaim> InlineOf(SourceFile file) => IsFrozen(file) ? [] :
        [.. XmlDocumentationCode.Of(file).Where(code => !code.IsBlock && IsCall(code.Code, code.Context, code.Before)).Select(code => new CallClaim(file.Path, code.Line, code.Code, false, code.Owners, null) { Scope = code.Scope })];

    /// <summary>The <c>&lt;code&gt;</c> examples the XML comments of a source file hold, each a block of code.</summary>
    /// <param name="file">The source file.</param>
    /// <returns>The blocks, in the order they stand.</returns>
    public static IReadOnlyList<CallClaim> BlocksOf(SourceFile file) => IsFrozen(file) ? [] :
        [.. XmlDocumentationCode.Of(file).Where(code => code.IsBlock).Select(code => new CallClaim(file.Path, code.Line, code.Code, true, code.Owners, null))];

    // The frozen Notebooks package is built by nothing and referenced by no suite: its comments name its own members, which no compilation
    // here can see.
    private static bool IsFrozen(SourceFile file) => file.Path.StartsWith("Src/MatPlotLibNet.Notebooks/", StringComparison.Ordinal);

    /// <summary>Whether a code span is a C# call of a library: it parses as C# (the placeholders a document writes read as names), it
    /// calls or constructs something by a name with a capital and small letters, and it is no Python (<see cref="PythonCall.IsPython(string, string)"/>)
    /// and no formula of one-letter functions (<c>S(m)</c>).</summary>
    /// <param name="code">The code of a span.</param>
    /// <param name="context">The line or cell it stands in, and the header of its column: the words that put a call in matplotlib's hands.</param>
    /// <param name="before">The words right before the span (<c>matplotlib's</c> hands it to matplotlib).</param>
    /// <returns>True for a call the packages may have.</returns>
    public static bool IsCall(string code, string context = "", string before = "")
    {
        if (PythonCall.IsPython(code, context, before) || CallText.Expression(code) is not { } expression)
        {
            return false;
        }

        // A formula that sets a function's value, `Quantile(p) = -Quantile(1 - p)`, is no call.
        if (expression is AssignmentExpressionSyntax { Left: InvocationExpressionSyntax })
        {
            return false;
        }

        var called = expression.DescendantNodesAndSelf().Select(node => node switch
        {
            InvocationExpressionSyntax invocation => CallText.NameOf(invocation.Expression),
            BaseObjectCreationExpressionSyntax creation => creation is ObjectCreationExpressionSyntax { Type: var type } ? CallText.NameOf(type) : null,
            _ => null,
        }).OfType<string>().ToList();

        // A name all in capitals is the label of an indicator (`RSI(14)`), no member: a member's name has a capital and small letters.
        return called.Count > 0 && called.All(name => name.Length > 1 && char.IsAsciiLetterUpper(name[0]) && name.Any(char.IsAsciiLetterLower));
    }

    /// <inheritdoc />
    public override string ToString() => $"{Document}, line {Line}: `{(IsBlock ? SourceFile.LinesOf(Code)[0] : Code)}`";
}

/// <summary>What a block of a document is compiled as.</summary>
internal enum CodeBlockKind
{
    /// <summary>Statements and declarations, as a program has them.</summary>
    Program,

    /// <summary>A notebook's cell: its <c>#r</c> lines no code, the members its kernel gives every value its own.</summary>
    NotebookCell,

    /// <summary>A razor fence: the component it compiles to (<see cref="CallText.RazorAsCSharp"/>).</summary>
    Razor,
}

/// <summary>A call of a document that the packages do not have, or that says another thing than the call: what the compiler or the
/// reading of the bound call says.</summary>
/// <param name="Document">The document's name.</param>
/// <param name="Line">The line of the call.</param>
/// <param name="Code">The call, or for a block the line of it the compiler points at.</param>
/// <param name="Id">The compiler's id for the error (CS1501), or what the bound call contradicts: <c>Type</c> (a parameter's type),
/// <c>Returns</c> (the type the call returns), <c>Default</c> (a parameter's default).</param>
/// <param name="Message">What is wrong, in words.</param>
internal readonly record struct CallMismatch(string Document, int Line, string Code, string Id, string Message)
{
    /// <inheritdoc />
    public override string ToString() => $"{Document}, line {Line}: `{Code}`: {Id}: {Message}";
}

/// <summary>The calls of documents as the compiler read them: the ones the packages do not have, and the ones read as foreign with the
/// reason, each a <see cref="CallMismatch"/> whose <see cref="CallMismatch.Id"/> is <c>Foreign</c>.</summary>
/// <param name="Mismatches">Every call the packages as built do not have, one per call at most, in the order the calls were given.</param>
/// <param name="Foreign">Every call that says nothing of the packages, with why: what it is read on is no type of the packages.</param>
internal readonly record struct CallReading(IReadOnlyList<CallMismatch> Mismatches, IReadOnlyList<CallMismatch> Foreign);

/// <summary>The calls of documents, compiled.</summary>
internal static class CallClaimExtensions
{
    extension(IEnumerable<CallClaim> claims)
    {
        /// <summary>The calls of the claims compiled against the packages as built.</summary>
        /// <returns>The mismatches, and the calls read as foreign.</returns>
        public CallReading Compiled() => CallCompilation.Read([.. claims]);
    }
}

/// <summary>The C# a document writes, read as C#: the placeholders a document writes for any value (<c>…</c>, <c>...</c>), an optional
/// argument (<c>name?</c>, <c>[, name]</c>), a signature (<c>Name(Type name)</c>) turned into names the parser takes.</summary>
internal static class CallText
{
    /// <summary>The name a placeholder for any further arguments, a body or a value is read as.</summary>
    public const string Rest = "__rest__";

    /// <summary>The start of the name an optional argument is read as: <c>options?</c> is <c>__opt__options</c>.</summary>
    public const string Optional = "__opt__";

    /// <summary>The start of the name a typed parameter of a signature is read as: <c>double pixels</c> is <c>pixels: __type0__</c>.</summary>
    public const string Typed = "__type";

    private static readonly System.Text.RegularExpressions.Regex OptionalMark = new(@"(?<=[(,]\s*)([A-Za-z_]\w*)\?(?=\s*[,)])");

    private static readonly System.Text.RegularExpressions.Regex BracketedOptional = new(@"\[\s*,\s*([A-Za-z_]\w*)\s*\]");

    private static readonly System.Text.RegularExpressions.Regex SignatureHead = new(@"^(?<head>(?:[A-Za-z_][\w<>, ]*\.)*\.?[A-Z]\w*(?:<[^()]*>)?)\((?<parameters>.*)\)$");

    /// <summary>The code with every placeholder for any value written as <see cref="Rest"/>, outside string literals and comments: what
    /// a literal and a comment are is what the compiler's own reading of the code says.</summary>
    /// <param name="code">C# as a document writes it.</param>
    /// <returns>The code the parser takes.</returns>
    public static string WithoutEllipses(string code)
    {
        var kept = SyntaxFactory.ParseTokens(code)
            .SelectMany(token => token.LeadingTrivia.Concat(token.TrailingTrivia).Where(IsComment).Select(trivia => trivia.FullSpan)
                .Concat(IsLiteral(token) ? [token.Span] : []))
            .ToList();
        var text = new System.Text.StringBuilder(code.Length);

        for (var at = 0; at < code.Length; at++)
        {
            var dots = code[at] == '…' ? 1 : string.CompareOrdinal(code, at, "...", 0, 3) == 0 && (at + 3 >= code.Length || code[at + 3] != '.') ? 3 : 0;

            if (dots > 0 && !kept.Any(span => span.Contains(at)))
            {
                text.Append(' ').Append(Rest).Append(' ');
                at += dots - 1;
            }
            else
            {
                text.Append(code[at]);
            }
        }

        return text.ToString();
    }

    private static readonly System.Text.RegularExpressions.Regex RazorComment = new(@"@\*.*?\*@", System.Text.RegularExpressions.RegexOptions.Singleline);

    // An expression of the markup: `@(...)` (brackets nested three deep), or `@name` read on with `.member` and `(...)`; never a directive,
    // a statement keyword, an escaped `@@` or the `@` of an address.
    private static readonly System.Text.RegularExpressions.Regex RazorExpression = new(
        @"(?<![\w@])@(?:\((?<explicit>(?:[^()]|\((?:[^()]|\((?:[^()]|\([^()]*\))*\))*\))*)\)|(?!(?:code|using|page|inject|implements|inherits|attribute|layout|rendermode|namespace|if|else|foreach|for|while|switch|do|try|lock|section)\b)(?<implicit>[A-Za-z_]\w*(?:\.[A-Za-z_]\w*|\((?:[^()]|\((?:[^()]|\([^()]*\))*\))*\))*))");

    private const string RazorComponent = "__Component";

    /// <summary>A razor fence read as the C# component it compiles to, line for line, so a line of the C# is the line of the fence: an
    /// <c>@using</c> is a using, the first other line opens a component class, an <c>@inject</c> is a member of it, the code of
    /// <c>@code { }</c> its members, and each expression of the markup (<c>@(...)</c>, <c>@figure.ToSvg()</c>) a property of it. Null for a
    /// fence that holds no C#: markup alone, a component written with its attributes.</summary>
    /// <param name="razor">The fence's text.</param>
    /// <returns>The C#, or null.</returns>
    public static string? RazorAsCSharp(string razor)
    {
        // A comment is blanked line for line, so the lines after it keep their place.
        var lines = SourceFile.LinesOf(RazorComment.Replace(razor, comment => string.Join('\n', SourceFile.LinesOf(comment.Value).Select(line => new string(' ', line.Length)))));
        var implemented = lines.Select(line => line.Trim()).Where(line => line.StartsWith("@implements ", StringComparison.Ordinal)).Select(line => ", " + line["@implements ".Length..].Trim());
        var header = $"public partial class {RazorComponent} : global::Microsoft.AspNetCore.Components.ComponentBase{string.Concat(implemented)} {{ ";
        var written = new string[lines.Length];
        var members = new bool[lines.Length];
        var (code, depth, held) = (false, 0, false);

        for (var at = 0; at < lines.Length; at++)
        {
            var line = lines[at].Trim();

            if (code)
            {
                depth += line.Count(each => each == '{') - line.Count(each => each == '}');
                written[at] = depth < 0 ? lines[at][..lines[at].LastIndexOf('}')] : lines[at];
                code = depth >= 0;
            }
            else if (line.StartsWith("@using ", StringComparison.Ordinal))
            {
                written[at] = "using " + line["@using ".Length..].TrimEnd(';').Trim() + ";";
            }
            else if (line.StartsWith("@code", StringComparison.Ordinal) && line.EndsWith('{'))
            {
                (written[at], members[at], code, depth, held) = (string.Empty, true, true, 0, true);
            }
            else if (line.StartsWith("@inject ", StringComparison.Ordinal) && line["@inject ".Length..].Trim() is var injected && injected.LastIndexOf(' ') is > 0 and var split)
            {
                (written[at], members[at]) = ($"private {injected[..split]} {injected[(split + 1)..]};", true);
            }
            else if (line.StartsWith('@') && !line.StartsWith("@(", StringComparison.Ordinal) && RazorExpression.Match(line) is not { Success: true, Index: 0 })
            {
                // A directive (`@page`, `@implements`) is no code of the class.
                written[at] = string.Empty;
            }
            else
            {
                var expressions = RazorExpression.Matches(lines[at]).Select(match => match.Groups["explicit"].Success ? match.Groups["explicit"].Value : match.Groups["implicit"].Value).ToList();

                held |= expressions.Count > 0;
                (written[at], members[at]) = (string.Concat(expressions.Select((expression, index) => $"private object __Markup{at}_{index} => {expression}; ")), line.Length > 0);
            }
        }

        // The class opens on the first line of markup or members, after the usings and directives above it.
        var opens = Array.IndexOf(members, true);

        if (opens >= 0)
        {
            written[opens] = header + written[opens];
        }

        return held ? string.Join('\n', written) + (opens >= 0 ? "\n}" : "\n" + header + "}") : null;
    }

    private static bool IsComment(SyntaxTrivia trivia) => trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia);

    private static bool IsLiteral(SyntaxToken token) =>
        token.IsKind(SyntaxKind.StringLiteralToken) || token.IsKind(SyntaxKind.CharacterLiteralToken) || token.IsKind(SyntaxKind.InterpolatedStringStartToken)
        || token.IsKind(SyntaxKind.InterpolatedStringTextToken) || token.IsKind(SyntaxKind.SingleLineRawStringLiteralToken)
        || token.IsKind(SyntaxKind.MultiLineRawStringLiteralToken) || token.IsKind(SyntaxKind.InterpolatedStringToken);

    /// <summary>A span read as one C# expression: a call written with a leading dot is read on a receiver named <c>__r</c>, a signature
    /// as a call that names each parameter; null when the span is no C# expression.</summary>
    /// <param name="code">The code of a span.</param>
    /// <returns>The expression, or null.</returns>
    public static ExpressionSyntax? Expression(string code) => Expression(code, out _);

    /// <summary>A span read as one C# expression, with the types its signature writes for its parameters, in the order of their names
    /// <c>__type0__</c>, <c>__type1__</c>.</summary>
    /// <param name="code">The code of a span.</param>
    /// <param name="types">The written types of a signature; empty for a call.</param>
    /// <returns>The expression, or null.</returns>
    public static ExpressionSyntax? Expression(string code, out IReadOnlyList<string> types)
    {
        types = [];
        var text = BracketedOptional.Replace(OptionalMark.Replace(WithoutEllipses(code.Trim()), Optional + "$1"), ", " + Optional + "$1");

        if (text.StartsWith('.'))
        {
            text = "__r" + text;
        }

        if (Parsed(text) is { } call)
        {
            return call;
        }

        if (SignatureHead.Match(text) is not { Success: true } signature
            || SyntaxFactory.ParseParameterList("(" + signature.Groups["parameters"].Value + ")") is not { } parameters
            || parameters.ContainsDiagnostics
            || parameters.Parameters.Any(parameter => parameter.Type is null))
        {
            return null;
        }

        types = [.. parameters.Parameters.Select(parameter => parameter.Type!.ToString())];
        var arguments = parameters.Parameters.Select((parameter, index) =>
            $"{parameter.Identifier.ValueText}: {(parameter.Default is { } given ? given.Value.ToString() : $"{Typed}{index}__")}");

        return Parsed($"{signature.Groups["head"].Value}({string.Join(", ", arguments)})");
    }

    private static ExpressionSyntax? Parsed(string text)
    {
        var expression = SyntaxFactory.ParseExpression(text);

        return expression.ContainsDiagnostics || expression.FullSpan.Length != text.Length ? null : expression;
    }

    /// <summary>The name an expression calls: the last name of a member access, a name, a generic name, a qualified name.</summary>
    /// <param name="expression">What is invoked or constructed.</param>
    /// <returns>The name, or null for anything else.</returns>
    public static string? NameOf(SyntaxNode expression) => expression switch
    {
        MemberAccessExpressionSyntax access => access.Name.Identifier.ValueText,
        SimpleNameSyntax name => name.Identifier.ValueText,
        QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
        AliasQualifiedNameSyntax alias => alias.Name.Identifier.ValueText,
        _ => null,
    };
}
