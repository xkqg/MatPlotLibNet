// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MatPlotLibNet.Wiki.Tests;

/// <summary>
/// The code the XML comments of a source file write: each <c>&lt;c&gt;</c> span and each <c>&lt;code&gt;</c> example, its entities read
/// (<c>=&gt;</c> is <c>=&gt;</c>), with the line it starts on and the types the comment stands in, the innermost first (a member of an
/// extension block stands in the type the block extends).
/// </summary>
/// <param name="Line">The line the code starts on, from one.</param>
/// <param name="Code">The code.</param>
/// <param name="IsBlock">Whether it is a <c>&lt;code&gt;</c> example.</param>
/// <param name="Owners">The types the comment stands in, the innermost first.</param>
/// <param name="Context">The text of the whole comment, for the words around the code.</param>
/// <param name="Before">The words of the comment right before the code.</param>
/// <param name="Scope">The parameters of the member the comment documents, each as its declaration writes it (<c>Point a</c>): a name
/// the code reads is the member's parameter.</param>
internal readonly record struct XmlDocumentationCode(int Line, string Code, bool IsBlock, IReadOnlyList<string> Owners, string Context, string Before, IReadOnlyList<string> Scope)
{
    private static readonly ConcurrentDictionary<string, (IReadOnlyList<XmlDocumentationCode> Code, IReadOnlyList<DocumentText> Words)> Read = new(StringComparer.Ordinal);

    private static readonly CSharpParseOptions Options = new(LanguageVersion.Preview, DocumentationMode.Parse);

    /// <summary>The code of every XML comment of a file, read once per process.</summary>
    /// <param name="file">The source file.</param>
    /// <returns>The code, in the order it stands.</returns>
    public static IReadOnlyList<XmlDocumentationCode> Of(SourceFile file) => Read.GetOrAdd(file.Path, _ => Parse(file.Text)).Code;

    /// <summary>The words of every XML comment of a file outside its <c>&lt;c&gt;</c> and <c>&lt;code&gt;</c>, a line of a comment at a
    /// time: what a comment says of a call without marking it as code.</summary>
    /// <param name="file">The source file.</param>
    /// <returns>Each line's words with the number of the line.</returns>
    public static IReadOnlyList<DocumentText> WordsOf(SourceFile file) => Read.GetOrAdd(file.Path, _ => Parse(file.Text)).Words;

    private static (IReadOnlyList<XmlDocumentationCode> Code, IReadOnlyList<DocumentText> Words) Parse(string text)
    {
        if (!text.Contains("///", StringComparison.Ordinal))
        {
            return ([], []);
        }

        var root = CSharpSyntaxTree.ParseText(text, Options).GetRoot();
        var found = new List<XmlDocumentationCode>();
        var words = new List<DocumentText>();

        foreach (var trivia in root.DescendantTrivia().Where(each => each.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)))
        {
            if (trivia.GetStructure() is not DocumentationCommentTriviaSyntax comment)
            {
                continue;
            }

            var owners = OwnersOf(trivia.Token.Parent);
            // A parameter whose type is a type parameter of the member or of its type (`TS series`) has no type the code can name here.
            var documented = trivia.Token.Parent?.AncestorsAndSelf().OfType<BaseMethodDeclarationSyntax>().FirstOrDefault();
            var generic = (trivia.Token.Parent?.AncestorsAndSelf() ?? []).SelectMany(each => each switch
            {
                MethodDeclarationSyntax method => method.TypeParameterList?.Parameters ?? [],
                TypeDeclarationSyntax type => type.TypeParameterList?.Parameters ?? [],
                _ => [],
            }).Select(parameter => parameter.Identifier.ValueText).ToHashSet(StringComparer.Ordinal);
            IReadOnlyList<string> scope = documented?.ParameterList.Parameters
                .Where(parameter => parameter.Type is { } type && !type.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().Any(name => generic.Contains(name.Identifier.ValueText)))
                .Select(parameter => $"{parameter.Type} {parameter.Identifier.ValueText}").ToList() ?? [];
            var context = comment.ToString();

            words.AddRange(comment.DescendantNodes().OfType<XmlTextSyntax>()
                .Where(node => !node.Ancestors().OfType<XmlElementSyntax>().Any(element => element.StartTag.Name.LocalName.ValueText is "c" or "code"))
                .SelectMany(node => node.TextTokens).Where(token => !token.IsKind(SyntaxKind.XmlTextLiteralNewLineToken))
                .GroupBy(token => token.GetLocation().GetLineSpan().StartLinePosition.Line + 1)
                .Select(line => new DocumentText(line.Key, string.Join(' ', line.Select(token => token.ValueText)), null)));

            foreach (var element in comment.DescendantNodes().OfType<XmlElementSyntax>())
            {
                var name = element.StartTag.Name.LocalName.ValueText;

                if (name is not ("c" or "code") || element.Content.Any(node => node is XmlElementSyntax))
                {
                    continue;
                }

                var all = element.Content.OfType<XmlTextSyntax>().SelectMany(node => node.TextTokens).ToList();
                var first = all.FirstOrDefault(token => !token.IsKind(SyntaxKind.XmlTextLiteralNewLineToken) && token.ValueText.Trim().Length > 0);

                if (first == default)
                {
                    continue;
                }

                // The code starts on the line of its first word, so a line of the code is that line plus its index.
                var tokens = all.SkipWhile(token => token.GetLocation().GetLineSpan().StartLinePosition.Line < first.GetLocation().GetLineSpan().StartLinePosition.Line).ToList();
                var code = string.Concat(tokens.Select(token => token.IsKind(SyntaxKind.XmlTextLiteralNewLineToken) ? "\n" : token.ValueText));

                var before = element.Parent is { } parent ? string.Concat(parent.ChildNodes().TakeWhile(node => node != element).Select(node => node.ToString())) : string.Empty;

                found.Add(new XmlDocumentationCode(first.GetLocation().GetLineSpan().StartLinePosition.Line + 1, name == "c" ? code.Trim() : code, name == "code", owners, context, before, scope));
            }
        }

        return (found, words);
    }

    // The types a comment stands in, the innermost first: a type declaration by its name, an extension block by the type it extends.
    private static IReadOnlyList<string> OwnersOf(SyntaxNode? node) =>
    [
        .. (node?.AncestorsAndSelf() ?? []).Select(each => each switch
        {
            TypeDeclarationSyntax { ParameterList.Parameters: [{ Type: { } receiver }] } when each.IsKind(SyntaxKind.ExtensionBlockDeclaration) => CallText.NameOf(receiver) ?? receiver.ToString(),
            BaseTypeDeclarationSyntax type => type.Identifier.ValueText,
            _ => null,
        }).OfType<string>().Where(name => name.Length > 0),
    ];
}
