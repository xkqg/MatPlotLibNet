// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MatPlotLibNet.Wiki.Tests;

/// <summary>
/// The calls of documents compiled against the packages as built, each in a method of its own. A document leaves names undefined that
/// stand for any value (<c>x</c>, <c>labels</c>, a type written as an argument, <c>…</c>), so a call is compiled again after each such
/// name is given what the compiler asks for: a value of no type (<c>default</c>), a receiver of the type the document's name is
/// conventionally (<c>ax</c> an <see cref="AxesBuilder"/>) or of a type that has the member, one overload picked where several fit a
/// value of no type. What is left is an error of the call itself: no such member, no overload that takes so many arguments, no
/// parameter of that name, a member of no type. A call that compiles is then read for what the document says beside it: the type it
/// writes for a parameter, the type it returns, the default it writes. Every name is visible, internal ones too: the architecture map
/// and the XML comments name the library's own members.
/// </summary>
internal static partial class CallCompilation
{
    private const int Rounds = 16;

    private const int MostReceivers = 24;

    private const string Method = "__Call";

    private const string Calls = "__Calls";

    private static readonly SyntaxAnnotation Placeholder = new("written-placeholder");

    private static readonly SyntaxAnnotation LeftOutBody = new("written-left-out-body");

    private const string WrittenType = "written-type";

    private const string WrittenDefault = "written-default";

    private const string WrittenReturns = "written-returns";

    private const string Open = "written-open";

    private const string Optional = "written-optional";

    // The type a name a document gives a receiver is conventionally, as C# writes it: the axes a callback hands over, the figure a chain
    // builds, the frame of the DataFrame package, the application and services of ASP.NET, a random generator; a hub connection of SignalR,
    // a package the suite does not reference, is a value of no type.
    private static readonly Dictionary<string, string[]> ConventionalReceivers = new(StringComparer.Ordinal)
    {
        ["ax"] = ["global::MatPlotLibNet.AxesBuilder", "global::MatPlotLibNet.Models.Axes"],
        ["axes"] = ["global::MatPlotLibNet.AxesBuilder", "global::MatPlotLibNet.Models.Axes"],
        ["figure"] = ["global::MatPlotLibNet.FigureBuilder", "global::MatPlotLibNet.Models.Figure"],
        ["fig"] = ["global::MatPlotLibNet.FigureBuilder", "global::MatPlotLibNet.Models.Figure"],
        ["df"] = ["global::Microsoft.Data.Analysis.DataFrame"],
        ["app"] = ["global::Microsoft.AspNetCore.Builder.WebApplication"],
        ["builder"] = ["global::Microsoft.AspNetCore.Builder.WebApplicationBuilder"],
        ["services"] = ["global::Microsoft.Extensions.DependencyInjection.IServiceCollection"],
        ["theme"] = ["global::MatPlotLibNet.Styling.Theme"],
        ["rng"] = ["global::System.Random"],
        ["random"] = ["global::System.Random"],
        ["hub"] = [Dynamic],
        ["connection"] = [Dynamic],
        ["hubConnection"] = [Dynamic],
    };

    // The errors a placeholder causes where it stands, which say nothing of the call: a value of no type with no target type, a block
    // that returns nothing, a type parameter the placeholder fills that breaks a constraint.
    private static readonly HashSet<string> ConstraintErrors = ["CS0311", "CS0314", "CS0315", "CS0452", "CS0453"];

    private static readonly Lazy<string> Usings = new(() => string.Concat(
        PackageSurface.Loaded.Namespaces.Where(name => name.StartsWith("MatPlotLibNet", StringComparison.Ordinal))
            .Concat(["Microsoft.Data.Analysis", "Microsoft.AspNetCore.Builder", "Microsoft.AspNetCore.Http", "Microsoft.Extensions.DependencyInjection"])
            .Select(name => $"using {name};\n")));

    private static readonly Lazy<CSharpCompilationOptions> Options = new(() =>
    {
        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Disable, allowUnsafe: true)
            .WithMetadataImportOptions(MetadataImportOptions.All);

        // The binder's own switch for reading every member whatever its access (the one the debugger's evaluator turns on): the
        // documents of the library name its internal and private members too.
        var flags = typeof(CSharpCompilationOptions).GetProperty("TopLevelBinderFlags", BindingFlags.NonPublic | BindingFlags.Instance)!;
        flags.SetValue(options, Enum.Parse(flags.PropertyType, "IgnoreAccessibility"));

        return options;
    });

    /// <summary>The calls of the claims compiled: every one the packages do not have, one mismatch per call at most, in the order given; and
    /// every call read as foreign, with the reason. A member read on a value of a type of the packages is held to that type, whatever
    /// another library names; a call is foreign only where what it is read on is no type of the packages (a type of the reader's own,
    /// <c>MyIndicator</c>; a type of a package the suite does not reference, <c>HubConnection</c>) and its calls of the packages all compile.</summary>
    /// <param name="claims">The calls.</param>
    /// <returns>The mismatches and the foreign calls.</returns>
    public static CallReading Read(IReadOnlyList<CallClaim> claims)
    {
        var jobs = claims.SelectMany((claim, index) => VariantsOf(claim).Select(trees => new Job(index, claim, trees))).ToList();

        // Every tree a job may be is compiled at once, each with its rank (the order a reader would try them in): the job holds when one
        // compiles and says what the document says, names something foreign when one does, and fails with the mismatch of the first in
        // rank when every one has failed.
        var current = jobs.SelectMany(job => job.Trees.Select((tree, index) => new Attempt(job, tree, [index]))).ToList();
        var held = new HashSet<Job>();
        var foreign = new Dictionary<Job, string>();
        var failures = new Dictionary<Job, List<(IReadOnlyList<int> Rank, CallMismatch Mismatch)>>();

        for (var round = 0; round < Rounds && current.Count > 0; round++)
        {
            var compilation = CSharpCompilation.Create("MatPlotLibNet.Tests", [WikiProgramExtensions.ImplicitUsings, .. current.Select(attempt => attempt.Tree)], WikiProgramExtensions.References.Value, Options.Value);
            var errors = compilation.GetDiagnostics().Where(each => each.Severity == DiagnosticSeverity.Error && each.Location.SourceTree is not null)
                .ToLookup(each => each.Location.SourceTree!);
            var next = new List<Attempt>();

            foreach (var attempt in current.Where(attempt => !held.Contains(attempt.Job)))
            {
                var outcome = Judge(attempt, errors[attempt.Tree].ToList(), compilation.GetSemanticModel(attempt.Tree, ignoreAccessibility: true));

                if (outcome.Next is { } tree)
                {
                    next.Add(attempt with { Tree = tree, Rank = [.. attempt.Rank, 0] });
                    next.AddRange(outcome.Alternatives.Select((alternative, index) => new Attempt(attempt.Job, alternative, [.. attempt.Rank, index + 1])));
                }
                else if (outcome.Foreign is { } reason && attempt.Rank[0] == 0)
                {
                    foreign.TryAdd(attempt.Job, reason);
                }
                else if (outcome.Foreign is not null)
                {
                    // The member call tried after a constructor names nothing the packages have: the constructor's error is the call's.
                    (failures.TryGetValue(attempt.Job, out var others) ? others : failures[attempt.Job] = []).Add((attempt.Rank, Mismatch(attempt.Job, attempt.Tree, null, "Unknown", "no type of the packages has the member the call names")));
                }
                else if (outcome.Mismatch is { } mismatch)
                {
                    (failures.TryGetValue(attempt.Job, out var list) ? list : failures[attempt.Job] = []).Add((attempt.Rank, mismatch));
                }
                else
                {
                    held.Add(attempt.Job);
                }
            }

            // The trees of one job tried at once are bounded: the first in rank are kept.
            current = [.. next.Where(attempt => !held.Contains(attempt.Job)).GroupBy(attempt => attempt.Job)
                .SelectMany(group => group.OrderBy(attempt => attempt.Rank, RankOrder.Instance).Take(MostAttempts))];
        }

        foreach (var attempt in current.Where(attempt => !held.Contains(attempt.Job)))
        {
            (failures.TryGetValue(attempt.Job, out var list) ? list : failures[attempt.Job] = []).Add((attempt.Rank, Mismatch(attempt.Job, attempt.Tree, null, "Rounds", "the call was still being given names after every round")));
        }

        var failed = jobs.Where(job => !held.Contains(job) && !foreign.ContainsKey(job) && failures.ContainsKey(job))
            .GroupBy(job => job.Claim)
            .ToDictionary(group => group.Key, group => failures[group.First()].OrderBy(each => each.Rank, RankOrder.Instance).First().Mismatch);

        // A claim none of whose variants failed and one of which is foreign and never held is foreign once, under the reason of the first.
        var setAside = jobs.Where(job => foreign.ContainsKey(job) && !held.Contains(job) && !failed.ContainsKey(job.Claim)).GroupBy(job => job.Claim)
            .ToDictionary(group => group.Key, group => Mismatch(group.First(), group.First().Trees[0], null, "Foreign", foreign[group.First()]));

        return new CallReading([.. failed.OrderBy(each => each.Key).Select(each => each.Value)], [.. setAside.OrderBy(each => each.Key).Select(each => each.Value)]);
    }

    private const int MostAttempts = 32;

    /// <summary>The order of the ranks of the trees of a job: the way a reader tries them, the first choice at each step first.</summary>
    private sealed class RankOrder : IComparer<IReadOnlyList<int>>
    {
        public static RankOrder Instance { get; } = new();

        public int Compare(IReadOnlyList<int>? x, IReadOnlyList<int>? y)
        {
            for (var at = 0; at < Math.Min(x!.Count, y!.Count); at++)
            {
                if (x[at] != y[at])
                {
                    return x[at].CompareTo(y[at]);
                }
            }

            return x.Count.CompareTo(y.Count);
        }
    }

    // The trees a claim is compiled as, each with the trees to try when it fails: the call as written, and when it marks arguments as
    // optional, the call without them as well. A bare call of a name that is a type is its constructor first and a member call after.
    private static IEnumerable<IReadOnlyList<SyntaxTree>> VariantsOf(CallClaim claim)
    {
        if (claim.IsBlock)
        {
            return [[BlockTree(claim.Kind == CodeBlockKind.Razor ? CallText.RazorAsCSharp(claim.Code)! : claim.Code)]];
        }

        if (CallText.Expression(claim.Code, out var types) is not { } expression)
        {
            return [];
        }

        var marked = expression.DescendantNodes().OfType<ArgumentSyntax>().Any(argument => argument.Expression is IdentifierNameSyntax name && name.Identifier.ValueText.StartsWith(CallText.Optional, StringComparison.Ordinal));
        IReadOnlyList<SyntaxTree> Trees(bool keepOptional) =>
        [
            CallTree(Written(expression, claim, types, keepOptional, construct: true), claim),
            .. expression is InvocationExpressionSyntax { Expression: SimpleNameSyntax } ? [CallTree(Written(expression, claim, types, keepOptional, construct: false), claim)] : Array.Empty<SyntaxTree>(),
        ];

        return marked ? [Trees(true), Trees(false)] : [Trees(true)];
    }

    // The expression as the compiler is to read it: a call written without a receiver is read on one, every placeholder is a value of
    // no type marked as a placeholder, an argument marked optional is named (or left out), a typed parameter of a signature names its
    // type, the call is marked with the type a table says it returns.
    private static ExpressionSyntax Written(ExpressionSyntax expression, CallClaim claim, IReadOnlyList<string> types, bool keepOptional, bool construct)
    {
        var rewritten = (ExpressionSyntax)new PlaceholderRewriter(types, keepOptional).Visit(expression);

        // A call written bare is a call on a receiver of the type it stands under, `Plot(x, y)`, or the constructor of a type of the packages,
        // `BarGroup(label, values)`.
        if (rewritten is InvocationExpressionSyntax { Expression: SimpleNameSyntax bare } invocation)
        {
            rewritten = construct && ConstructedType(bare.Identifier.ValueText, claim.Owners) is { } type
                ? SyntaxFactory.ObjectCreationExpression(SyntaxFactory.Token(SyntaxKind.NewKeyword).WithTrailingTrivia(SyntaxFactory.Space), SyntaxFactory.ParseTypeName(type), invocation.ArgumentList, null)
                : invocation.WithExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("__r"), bare));
        }

        return claim.Returns is { } returns ? rewritten.WithAdditionalAnnotations(new SyntaxAnnotation(WrittenReturns, returns)) : rewritten;
    }

    // The type a call written bare constructs: a type of the packages of that name that is no static class, when no type the call
    // stands under has a member of the name.
    private static string? ConstructedType(string name, IReadOnlyList<string> owners)
    {
        var surface = PackageSurface.Loaded;

        if (owners.SelectMany(surface.TypesNamed).Any(owner => surface.Reaches([owner], name)))
        {
            return null;
        }

        return surface.TypesNamed(name).Where(type => !(type.IsAbstract && type.IsSealed)).Select(Syntax).OfType<string>().FirstOrDefault();
    }

    private static int _trees;

    // A namespace of its own for each tree, so the class every tree declares and the types a block declares meet no other tree's.
    private static string Namespace() => "__Written" + Interlocked.Increment(ref _trees);

    private static SyntaxTree CallTree(ExpressionSyntax expression, CallClaim claim)
    {
        var statement = expression is InvocationExpressionSyntax or ObjectCreationExpressionSyntax or AwaitExpressionSyntax or AssignmentExpressionSyntax
            ? SyntaxFactory.ExpressionStatement(expression)
            : SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, SyntaxFactory.IdentifierName("_"), expression));
        var modifiers = expression.DescendantNodesAndSelf().OfType<AwaitExpressionSyntax>().Any() ? "static async System.Threading.Tasks.Task" : "static void";
        var unit = SyntaxFactory.ParseCompilationUnit(Usings.Value + $"namespace {Namespace()} {{ static class __Calls {{ {modifiers} {Method}() {{ }} }} }}");
        var method = unit.DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
        var body = method.Body!.AddStatements(statement);

        // A name the code reads that the member its comment documents takes as a parameter is that parameter.
        var read = expression.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().Select(name => name.Identifier.ValueText).ToHashSet(StringComparer.Ordinal);
        var declared = claim.Scope.Select(each => SyntaxFactory.ParseParameterList("(" + each + ")").Parameters.FirstOrDefault())
            .OfType<ParameterSyntax>().Where(parameter => read.Contains(parameter.Identifier.ValueText) && parameter.Type is not null)
            .Select(parameter => parameter.WithDefault(null).WithModifiers([]).WithAttributeLists([]));

        return CSharpSyntaxTree.Create(unit.ReplaceNode(method, method.WithBody(body).AddParameterListParameters([.. declared])));
    }

    // A block as C# reads it: the directives of a notebook cell (`#r "nuget: ..."`) are no code of the program, and the last value of a
    // cell, or a block that is one value, may end without its semicolon.
    private static readonly CSharpParseOptions Script = new(LanguageVersion.Preview);

    private static CompilationUnitSyntax NotebookCellRead(string code)
    {
        var read = SyntaxFactory.ParseCompilationUnit(code, options: Script);

        // A directive of the cell is no code of the program: each stands as the line it was.
        var unit = read.ReplaceTrivia(
            read.DescendantTrivia(descendIntoTrivia: true).Where(trivia => trivia.IsKind(SyntaxKind.ReferenceDirectiveTrivia) || trivia.IsKind(SyntaxKind.ShebangDirectiveTrivia) || trivia.IsKind(SyntaxKind.LoadDirectiveTrivia)
                                                                   || trivia.IsKind(SyntaxKind.BadDirectiveTrivia)),
            (_, _) => SyntaxFactory.EndOfLine("\n"));

        // The last statement of a cell that ends on its value, written without a semicolon, is the statement it means; a name alone at the
        // end (`figure`) reads to the parser as a member it cannot finish.
        return unit.Members.LastOrDefault() switch
        {
            GlobalStatementSyntax { Statement: ExpressionStatementSyntax { SemicolonToken.IsMissing: true } last } =>
                unit.ReplaceNode(last, last.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))),
            { ContainsDiagnostics: true } shown when SyntaxFactory.ParseExpression(shown.ToString()) is { ContainsDiagnostics: false } value =>
                unit.ReplaceNode(shown, SyntaxFactory.GlobalStatement(SyntaxFactory.ExpressionStatement(value)).WithTriviaFrom(shown)),
            _ => unit,
        };
    }

    // The file name the line directives of a block tree map to: a position in it is a line of the block as the document writes it.
    private const string BlockLines = "block";

    // A block of statements and declarations: its usings first, its statements the body of the method, its declarations after the class;
    // each piece preceded by the line it stands on in the block, so an error names the line of the document.
    private static SyntaxTree BlockTree(string code)
    {
        var block = NotebookCellRead(CallText.WithoutEllipses(code));
        string At(SyntaxNode node) => $"\n#line {block.SyntaxTree.GetLineSpan(node.FullSpan).StartLinePosition.Line + 1} \"{BlockLines}\"\n";
        var statements = block.Members.OfType<GlobalStatementSyntax>().Select(each => each.Statement).ToList();
        var lines = statements.Select(At).ToList();

        // The last value of a notebook cell is shown, not discarded: as a statement it is the value given to a discard.
        if (statements.Count > 0 && statements[^1] is ExpressionStatementSyntax { Expression: var shown } last
            && shown is not (InvocationExpressionSyntax or AssignmentExpressionSyntax or ObjectCreationExpressionSyntax or AwaitExpressionSyntax))
        {
            statements[^1] = last.WithExpression(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, SyntaxFactory.IdentifierName("_ "), shown));
        }
        var scoped = block.Members.OfType<FileScopedNamespaceDeclarationSyntax>().FirstOrDefault();
        var others = block.Members.Where(member => member is not GlobalStatementSyntax and not FileScopedNamespaceDeclarationSyntax).Concat(scoped?.Members ?? [])
            .Select(member => At(member) + member.ToFullString());

        var text = Usings.Value
            + string.Concat(block.Usings.Select(each => At(each) + each.ToFullString() + "\n"))
            + $"#line default\nnamespace {Namespace()} {{\n"
            + (scoped is null ? string.Empty : $"namespace {scoped.Name} {{\n")
            + $"static class {Calls} {{ {(block.DescendantNodes().OfType<AwaitExpressionSyntax>().Any() ? "static async System.Threading.Tasks.Task" : "static void")} {Method}() {{\n"
            + string.Concat(statements.Select((each, index) => lines[index] + each.ToFullString()))
            + "\n#line default\n} }\n"
            + string.Concat(others)
            + "\n#line default\n"
            + (scoped is null ? "}\n" : "}\n}\n");

        var unit = SyntaxFactory.ParseCompilationUnit(text);
        var method = unit.DescendantNodes().OfType<MethodDeclarationSyntax>().First(each => each.Identifier.ValueText == Method);
        var rewritten = new PlaceholderRewriter([], keepOptional: true).Visit(unit)!;

        return CSharpSyntaxTree.Create((CompilationUnitSyntax)rewritten);
    }

    // What the errors of one attempt lead to: the tree compiled again with what the errors ask for (and the trees with the other
    // receivers or namespaces a name may mean, to try after it), or the mismatch they leave, or that the call names a type of neither the
    // packages nor .NET.
    private static Outcome Judge(Attempt attempt, IReadOnlyList<Diagnostic> errors, SemanticModel model)
    {
        var root = attempt.Tree.GetRoot();
        var method = root.DescendantNodes().OfType<MethodDeclarationSyntax>().First(each => each.Identifier.ValueText == Method);
        var choices = new List<IReadOnlyList<Edit>>();
        var typeParameters = new List<string>();
        var hard = new List<(Diagnostic Error, SyntaxNode Node)>();
        var foreignAt = new List<(SyntaxNode Node, string Reason)>();
        CallMismatch? missing = null;

        foreach (var error in errors)
        {
            var node = root.FindNode(error.Location.SourceSpan, getInnermostNodeForTie: true);

            if (Accepted(error, node, method))
            {
                continue;
            }

            if (choices.SelectMany(each => each).Any(edit => edit.Node is { } edited && edited.Span.IntersectsWith(node.Span)))
            {
                continue;
            }

            if (IsMemberOfAnUnseenPart(error, node))
            {
                foreignAt.Add((node, $"{node} is a member of the part of the type the document does not show"));
            }
            else if (IsKernelName(error, node, attempt))
            {
                foreignAt.Add((node, $"{node} is a member the notebook's kernel gives every value"));
            }
            else if (EditsFor(error, node, attempt, model) is { Count: > 0 } edits)
            {
                choices.Add(edits);
            }
            else if (error.Id == "CS0246" && node is IdentifierNameSyntax { Identifier.ValueText: var parameter } && IsTypeParameterName(parameter))
            {
                typeParameters.Add(parameter);
            }
            else if (MissingOnThePackages(error, node, model) is { } absent)
            {
                // What the member is read on decides: a type of the packages that lacks it is the document's error, whatever library
                // elsewhere has a member of the name and whatever foreign names the call holds; in a document about the tests, unless the
                // tests add it to that type (`figure.RoundTripped()`). The first such error stands once the names around it are given.
                if (AboutTests(attempt) && TestExtensions.Value[absent.Member].Contains(absent.Receiver))
                {
                    foreignAt.Add((node, $"{absent.Member} is a member the repository's tests add to {absent.Receiver}"));
                    continue;
                }

                missing ??= RemovedSinceBaseline.Names.Contains(absent.Member)
                    ? Mismatch(attempt.Job, attempt.Tree, error, "Removed", $"{absent.Member} was removed from the packages since the release before this one")
                    : Mismatch(attempt.Job, attempt.Tree, error, "Unknown", $"{absent.Receiver} has no member {absent.Member}");
            }
            else if (UndefinedName(error, node) is { } toolkit && node.FirstAncestorOrSelf<UsingDirectiveSyntax>() is not null && ForeignReason(toolkit, inCode: true, readersOwn: true, AboutTests(attempt)) is { } outside)
            {
                // A namespace of a toolkit the suite cannot load says nothing of the code after it: each name it would give is foreign
                // by itself, and every call of the packages beside them is held.
                foreignAt.Add((node, outside));
            }
            else if (UndefinedName(error, node) is { } name)
            {
                if (RemovedSinceBaseline.Names.Contains(name))
                {
                    return new Outcome(null, [], Mismatch(attempt.Job, attempt.Tree, error, "Removed", $"{name} was removed from the packages since the release before this one"), null);
                }

                // The reader writes code of their own in a block, and a value the document leaves undefined (`hub.SendAsync`) is theirs.
                var readersOwn = attempt.Job.Source.IsBlock || (node.Parent is MemberAccessExpressionSyntax { Expression: var read } && read == node && node is IdentifierNameSyntax { Identifier.ValueText: var value } && !value.StartsWith("__r", StringComparison.Ordinal));

                if (ForeignReason(name, readersOwn || attempt.Job.Source.Document.EndsWith(".cs", StringComparison.Ordinal), readersOwn, AboutTests(attempt)) is not { } reason)
                {
                    return new Outcome(null, [], Mismatch(attempt.Job, attempt.Tree, error, "Unknown", $"neither the packages nor .NET have {name}"), null);
                }

                // A foreign value handed to a call (`WithLegend(MyIndicator.Position)`, `Indicator(new MyIndicator(...))`) is any value: the
                // call it is handed to is compiled, which an argument of no type would leave unread.
                if (ForeignValue(node) is { } handed)
                {
                    choices.Add([new Edit(handed, Default().WithTriviaFrom(handed), null)]);
                }
                else
                {
                    foreignAt.Add((node, reason));
                }
            }
            else if (error.Id != "CS0121")
            {
                hard.Add((error, node));
            }
        }

        // An error of a call that names something foreign may be that name's (`calls.Of<DrawTextCall>()`, a type of the tests as its type
        // argument); an error of any other call is the document's, whatever foreign names stand around it.
        var wrong = hard.Where(each => !foreignAt.Any(name => CallAround(each.Node).Span.Contains(name.Node.Span))).Select(each => each.Error).FirstOrDefault();

        if (choices.Count > 0 || typeParameters.Count > 0)
        {
            SyntaxTree Built(int choice, int pick) =>
                CSharpSyntaxTree.Create((CompilationUnitSyntax)Build(root, [.. choices.Select((edits, index) => edits[index == choice ? pick : 0])], typeParameters));

            var alternatives = choices.SelectMany((edits, index) => Enumerable.Range(1, edits.Count - 1).Select(pick => (index, pick))).Select(each => Built(each.index, each.pick));

            return new Outcome(Built(-1, 0), [.. alternatives], null, null);
        }

        if (missing is not null)
        {
            return new Outcome(null, [], missing, null);
        }

        if (wrong is not null)
        {
            return new Outcome(null, [], Mismatch(attempt.Job, attempt.Tree, wrong, wrong.Id, wrong.GetMessage(System.Globalization.CultureInfo.InvariantCulture)), null);
        }

        // A call that compiles only on a value of no type the packages have says nothing of them.
        var foreign = foreignAt.Select(each => each.Reason).FirstOrDefault()
                      ?? (method.ParameterList.Parameters.Any(parameter => parameter.Type is IdentifierNameSyntax { Identifier.ValueText: Dynamic }) ? "it compiles only on a value of a type the document leaves unsaid and no type of the packages is" : null);

        // A block whose every call of the packages compiles holds, whatever names of the reader's own it uses around them; an inline call
        // that names one says nothing of the packages.
        return foreign is not null && !attempt.Job.Source.IsBlock ? new Outcome(null, [], null, foreign) : new Outcome(null, [], Said(attempt, model), null);
    }

    // The member that is not there and the type of the packages it is read on (`Color.FromRgb`, `Plt.Create().WithTitel(...)`, an
    // initializer's `{ Nope = 2 }`, an extension of another builder, `Plt.Create().WithLegend()`): what the compiler binds the receiver to,
    // a value's type or a type written by its name. Null for any other error, and where the receiver is a type of .NET, of a toolkit, of
    // the block itself or of no type at all.
    private static MissingMember? MissingOnThePackages(Diagnostic error, SyntaxNode node, SemanticModel model)
    {
        if (error.Id is not ("CS1061" or "CS0117" or "CS1955" or "CS1929"))
        {
            return null;
        }

        var (receiver, member) = (node, node.Parent) switch
        {
            (MemberAccessExpressionSyntax access, _) => (access.Expression, access.Name),
            (_, MemberAccessExpressionSyntax access) when access.Name == node || access.Expression == node => (access.Expression, access.Name),
            (_, MemberBindingExpressionSyntax binding) when binding.Name == node => (binding.FirstAncestorOrSelf<ConditionalAccessExpressionSyntax>()?.Expression, binding.Name),
            (SimpleNameSyntax name, AssignmentExpressionSyntax { Parent: InitializerExpressionSyntax { Parent: BaseObjectCreationExpressionSyntax created } } assigned) when assigned.Left == node => (created, name),
            _ => ((ExpressionSyntax?)null, (SimpleNameSyntax?)null),
        };

        // The value a bare call or a chain written from its dot is read on is the reader's stand-in, no receiver the document writes.
        if (receiver is null || member is null || receiver is IdentifierNameSyntax { Identifier.ValueText: var standIn } && standIn.StartsWith("__r", StringComparison.Ordinal))
        {
            return null;
        }

        var type = model.GetTypeInfo(receiver).Type is { TypeKind: not TypeKind.Error } value ? value : model.GetSymbolInfo(receiver).Symbol as ITypeSymbol;

        return type is { TypeKind: not TypeKind.Error and not TypeKind.Dynamic, ContainingAssembly: { } assembly }
               && !SymbolEqualityComparer.Default.Equals(assembly, model.Compilation.Assembly) && PackageSurface.IsPackageAssembly(assembly.Name)
            ? new MissingMember(type.Name, member.Identifier.ValueText)
            : null;
    }

    // The value a foreign name starts when it is handed to a call as an argument: the name with what is read or called on it, or the object
    // a foreign type constructs. Null where it is no argument (a receiver, a type argument, a declaration).
    private static ExpressionSyntax? ForeignValue(SyntaxNode node)
    {
        SyntaxNode value = node.Parent switch
        {
            MemberAccessExpressionSyntax access when access.Name == node => access,
            ObjectCreationExpressionSyntax { Type: var type } created when type == node => created,
            _ => node,
        };

        while (value.Parent is MemberAccessExpressionSyntax { Expression: var read } && read == value
               || value.Parent is InvocationExpressionSyntax { Expression: var called } && called == value
               || value.Parent is ElementAccessExpressionSyntax { Expression: var indexed } && indexed == value)
        {
            value = value.Parent;
        }

        return value is ExpressionSyntax expression && value.Parent is ArgumentSyntax ? expression : null;
    }

    /// <summary>A member a call reads on a type of the packages that the type does not have.</summary>
    /// <param name="Receiver">The name of the type it is read on.</param>
    /// <param name="Member">The member's name.</param>
    private readonly record struct MissingMember(string Receiver, string Member);

    // The errors a placeholder causes where it stands, which say nothing of the call: anything inside a placeholder, an operator or a
    // conversion applied to a value of no type, a lambda whose body the document leaves out, a constraint a type parameter the document
    // writes cannot be seen to meet.
    private static bool Accepted(Diagnostic error, SyntaxNode node, MethodDeclarationSyntax method) =>
        node.AncestorsAndSelf().Any(each => each.HasAnnotation(Placeholder))
        || (PlaceholderOperandErrors.Contains(error.Id) && node.DescendantNodesAndSelf().Any(each => each.HasAnnotation(Placeholder)))
        || (LeftOutBodyErrors.Contains(error.Id) && node.AncestorsAndSelf().Any(each => each.HasAnnotation(LeftOutBody)))
        || (ConstraintErrors.Contains(error.Id) && method.TypeParameterList is not null)
        || (error.Id == "CS0411" && node.FirstAncestorOrSelf<InvocationExpressionSyntax>() is { } call && call.DescendantNodesAndSelf().Any(each => each.HasAnnotation(Placeholder)))
        || error.GetMessage(System.Globalization.CultureInfo.InvariantCulture).Contains("dynamic", StringComparison.Ordinal)
        || (error.Id is "CS7036" or "CS1729" or "CS1501" && node.FirstAncestorOrSelf<BaseObjectCreationExpressionSyntax>()?.HasAnnotations(Open) == true)
        || error.Id == "CS8852";

    // A name in capitals that nothing defines, read in a partial type the block declares: a member of the part the document does not
    // show, the element a window's XAML names (`PositionText`), a component's markup. A type of the packages that has a member of the
    // name is no receiver of it.
    private static bool IsMemberOfAnUnseenPart(Diagnostic error, SyntaxNode node) =>
        error.Id == "CS0103" && node is IdentifierNameSyntax { Identifier.ValueText: [var first, ..] } && char.IsAsciiLetterUpper(first)
        && node.Ancestors().OfType<TypeDeclarationSyntax>().Any(type => type.Identifier.ValueText != Calls && type.Modifiers.Any(SyntaxKind.PartialKeyword));

    // The members the kernel of a notebook gives every value of a cell: .NET Interactive's `Display()`, of an assembly the suite does
    // not reference.
    private static readonly HashSet<string> KernelNames = ["Display"];

    private static bool IsKernelName(Diagnostic error, SyntaxNode node, Attempt attempt) =>
        attempt.Job.Source.Kind == CodeBlockKind.NotebookCell && error.Id == "CS1061" && node is SimpleNameSyntax { Identifier.ValueText: var name } && KernelNames.Contains(name);

    // The call an error stands in: the innermost invocation around it, or the node itself.
    private static SyntaxNode CallAround(SyntaxNode node) => node.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().FirstOrDefault() ?? node;

    private static readonly HashSet<string> PlaceholderOperandErrors = ["CS8310", "CS0019", "CS0023", "CS8716", "CS0815", "CS8754", "CS0029", "CS0266", "CS1662"];

    private static readonly HashSet<string> LeftOutBodyErrors = ["CS1643", "CS1662", "CS8030", "CS0201", "CS0161"];

    // The name an error says is no name of anything: a name in capitals no type of the packages is, a value in lower case no type of the
    // packages has the member read on it for (`builder.Services`). Null for any other error.
    private static string? UndefinedName(Diagnostic error, SyntaxNode node)
    {
        var name = node switch
        {
            SimpleNameSyntax simple => simple.Identifier.ValueText,
            QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
            _ => null,
        };

        if (error.Id == "CS0103" && node is IdentifierNameSyntax { Parent: MemberAccessExpressionSyntax access } value && access.Expression == value && value.Identifier.ValueText is { Length: > 0 } lower && !char.IsAsciiLetterUpper(lower[0]))
        {
            return access.Name.Identifier.ValueText;
        }

        // A type a package the suite cannot build declares (`MplChartView`) is no type of this compilation either.
        if (name is not { Length: > 0 } || !char.IsAsciiLetterUpper(name[0]) || PackageSurface.Loaded.TypesNamed(name).Count > 0)
        {
            return null;
        }

        return error.Id switch
        {
            "CS0103" or "CS0246" => name,
            "CS0234" when !error.GetMessage(System.Globalization.CultureInfo.InvariantCulture).Contains("'MatPlotLibNet", StringComparison.Ordinal) || UnbuiltPackages.Sources.Uses(name) => name,
            "CS1955" or "CS1061" or "CS0117" when node.Parent is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.ValueText: var bare } } && bare.StartsWith("__r", StringComparison.Ordinal) => name,
            "CS1061" or "CS0117" when PackageSurface.Loaded.TypesReaching(name).Count == 0 => name,
            _ => null,
        };
    }

    // Whether a name no type of the packages has stands for something of .NET, of the toolkits or the tests, or of the reader's own: a type
    // of an assembly the suite references beside the packages (`HubConnection`), a name the code of a package the runner cannot build uses
    // (`InvalidateVisual`, `DispatcherQueue`), a name of the repository's tests (a document about the tests names their helpers), a name an
    // example makes up (`MyIndicator`, `Foo`, `XOver` for any axis). A member of .NET counts only where the call is no call of the packages
    // written bare in prose: in a block (the reader's own code around the library's) or read on a type of .NET (`Dispatcher.UIThread.Post()`);
    // a bare `Show()` in a sentence about the library names a member of the library, so a name of .NET does not excuse it.
    // A name no type of the packages has is the reader's own only where the reader writes code of their own (a block, a value the document
    // leaves undefined); a span of an XML comment documents the library. Null where the name is none of these: then it is a member the
    // packages do not have. Never asked where what the name is read on is a type of the packages (MissingOnThePackages).
    private static string? ForeignReason(string name, bool inCode, bool readersOwn, bool aboutTests) =>
        IllustrativeName().IsMatch(name) ? $"{name} is a name an example makes up"
        : FrameworkTypes.Value.Contains(name) ? $"{name} is a type of .NET or of a library the suite references"
        : inCode && FrameworkNames.Value.Contains(name) ? $"{name} is a name of .NET or of a library the suite references"
        : UnbuiltPackages.Sources.Uses(name) ? $"{name} is a name the code of a package the suite cannot build uses"
        : aboutTests && TestNames.Value.Contains(name) ? $"{name} is a name of the repository's tests"
        : readersOwn && !PackageSurface.Loaded.TypesReaching(name).Any() ? $"{name} is a name of the reader's own code, which no type of the packages has"
        : null;

    // The documents about the repository's tests, which name the tests' own helpers beside the library's members: the contributor's
    // guides and the coverage notes. A name of the tests read on a value of a type of the packages (`figure.RoundTripped()`) is one of
    // those helpers there; in any other document it is a member the packages do not have.
    private static bool AboutTests(Attempt attempt) =>
        attempt.Job.Source.Document is "CONTRIBUTING.md" or "Contributing.md" or "CLAUDE.md" or "docs/COVERAGE.md" or "Src/MatPlotLibNet/ARCHITECTURE.md";

    [System.Text.RegularExpressions.GeneratedRegex(@"^(?:My[A-Z]\w*|Foo|Bar|Baz|[A-Z])$")]
    private static partial System.Text.RegularExpressions.Regex IllustrativeName();

    // What the repository's tests add to a type: for each member an extension block of the tests declares, the names of the types the
    // block extends.
    private static readonly Lazy<ILookup<string, string>> TestExtensions = new(() =>
        RepositoryTree.TestFiles().Where(file => file.Code.Contains("extension(", StringComparison.Ordinal))
            .SelectMany(file => CSharpSyntaxTree.ParseText(file.Code, Script).GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>()
                .Where(block => block.IsKind(SyntaxKind.ExtensionBlockDeclaration) && block.ParameterList is { Parameters: [{ Type: not null }] })
                .SelectMany(block => block.Members.Select(member => member switch
                {
                    MethodDeclarationSyntax method => method.Identifier.ValueText,
                    PropertyDeclarationSyntax property => property.Identifier.ValueText,
                    _ => null,
                }).OfType<string>().Select(member => (Member: member, Type: CallText.NameOf(block.ParameterList!.Parameters[0].Type!) ?? string.Empty))))
            .ToLookup(each => each.Member, each => each.Type, StringComparer.Ordinal));

    // The names the code of the repository's tests uses.
    private static readonly Lazy<HashSet<string>> TestNames = new(() =>
        [.. RepositoryTree.TestFiles().SelectMany(file => IdentifierWord().Matches(file.Code).Select(match => match.Value))]);

    [System.Text.RegularExpressions.GeneratedRegex(@"\b[A-Z]\w*\b")]
    private static partial System.Text.RegularExpressions.Regex IdentifierWord();

    // The names of the types of every assembly the suite references beside the packages.
    private static readonly Lazy<HashSet<string>> FrameworkTypes = new(() => FrameworkSymbols(types: true));

    // The names of the types and members of every assembly the suite references beside the packages.
    private static readonly Lazy<HashSet<string>> FrameworkNames = new(() => FrameworkSymbols(types: false));

    private static HashSet<string> FrameworkSymbols(bool types)
    {
        var compilation = CSharpCompilation.Create("names", references: WikiProgramExtensions.References.Value);
        var names = new HashSet<string>(StringComparer.Ordinal);
        var pending = new Stack<INamespaceOrTypeSymbol>();

        foreach (var assembly in compilation.References.Select(compilation.GetAssemblyOrModuleSymbol).OfType<IAssemblySymbol>()
                     .Where(assembly => !PackageSurface.IsPackageAssembly(assembly.Name)))
        {
            pending.Push(assembly.GlobalNamespace);
        }

        while (pending.TryPop(out var each))
        {
            foreach (var member in each.GetMembers())
            {
                if (member is INamespaceOrTypeSymbol inner && member.DeclaredAccessibility is Accessibility.Public or Accessibility.NotApplicable)
                {
                    if (member is ITypeSymbol || !types)
                    {
                        names.Add(member.Name);
                    }

                    pending.Push(inner);
                }
                else if (!types && member.DeclaredAccessibility is Accessibility.Public or Accessibility.Protected)
                {
                    names.Add(member.Name);
                }
            }
        }

        return names;
    }

    // The ways one error may be met, the likeliest first: each replaces a node or gives the method a parameter. None for an error of
    // the call itself.
    private static IReadOnlyList<Edit> EditsFor(Diagnostic error, SyntaxNode node, Attempt attempt, SemanticModel model)
    {
        switch (error.Id)
        {
            // A name the document leaves undefined: a value of a type that has the member read on it or called, or a value.
            // A type written as a call, `BarGroup(label, values)`: its constructor.
            case "CS0103" when node is IdentifierNameSyntax { Parent: InvocationExpressionSyntax { Expression: var invoked } call } type && invoked == type
                                                        && PackageSurface.Loaded.TypesNamed(type.Identifier.ValueText).Where(each => !(each.IsAbstract && each.IsSealed)).Select(Syntax).OfType<string>().FirstOrDefault() is { } created:
                return [new Edit(call, SyntaxFactory.ObjectCreationExpression(SyntaxFactory.Token(SyntaxKind.NewKeyword).WithTrailingTrivia(SyntaxFactory.Space), SyntaxFactory.ParseTypeName(created), call.ArgumentList, null).WithTriviaFrom(call), null)];

            // A value the document indexes, `y[i]`: an array of numbers.
            case "CS0103" when node is IdentifierNameSyntax { Parent: ElementAccessExpressionSyntax { Expression: var indexed } } array && indexed == array
                                                        && char.IsAsciiLetterLower(array.Identifier.ValueText[0]) && !ConventionalReceivers.ContainsKey(array.Identifier.ValueText):
                return [new Edit(null, null, Parameter("double[]", array.Identifier.ValueText))];

            case "CS0103" when node is IdentifierNameSyntax { Identifier.ValueText: var made } invented && IllustrativeName().IsMatch(made) && !IsValue(invented):
                return [];

            // A member the comment assigns, `ColorScale = ...`: a member of the type the comment stands in.
            case "CS0103" when node is IdentifierNameSyntax { Parent: AssignmentExpressionSyntax { Left: var assigned } } target && assigned == target
                                                        && char.IsAsciiLetterUpper(target.Identifier.ValueText[0]):
                return [.. PackageSurface.Loaded.TypesReaching(target.Identifier.ValueText)
                    .OrderBy(type => attempt.Job.Source.Owners.Contains(type.Name) ? 0 : 1)
                    .Select(ReceiverOf).OfType<Receiver>().Take(MostReceivers).Select(each => ReceiverEdit(target, each with { IsStatic = false }))];

            case "CS0103" when node is IdentifierNameSyntax name:
                var receivers = Receivers(name, attempt);

                if (receivers.Count > 0)
                {
                    return [.. receivers.Take(MostReceivers).Select(receiver => ReceiverEdit(name, receiver))];
                }

                return IsValue(name) ? [new Edit(name, Default().WithTriviaFrom(name), null)] : [];

            // A type written where a value goes: the document writes the parameter's type, and any value of it fits.
            case "CS0119" when node.FirstAncestorOrSelf<ArgumentSyntax>() is { } argument && argument.Expression.Span == node.Span:
                return [new Edit(argument, argument.WithExpression(Default()).WithAdditionalAnnotations(new SyntaxAnnotation(WrittenType, node.ToString())), null)];

            // `out Figure`: an argument the call writes out, of the type the document names.
            case "CS0118" when node.FirstAncestorOrSelf<ArgumentSyntax>() is { RefKindKeyword.ValueText: "out", Expression: TypeSyntax type } argument:
                return [new Edit(argument, argument.WithExpression(SyntaxFactory.DeclarationExpression(type.WithTrailingTrivia(SyntaxFactory.Space), SyntaxFactory.DiscardDesignation())), null)];

            // An instance member written on its type (`Figure.Save(path)`, `double.Saturate()`): a value of the type.
            case "CS0120" or "CS0117" when TypeAccess(node, model) is { } access && (error.Id == "CS0120" || ReachesAsInstance(model, access)):
                return [new Edit(access.Expression, SyntaxFactory.ParenthesizedExpression(SyntaxFactory.DefaultExpression((TypeSyntax)access.Expression.WithoutTrivia())).WithTriviaFrom(access.Expression), null)];

            // A static member reached through the receiver the claim stands under.
            case "CS0176" when node.FirstAncestorOrSelf<MemberAccessExpressionSyntax>() is { Expression: var value } access
                               && model.GetTypeInfo(value).Type is { } type:
                return [new Edit(access.Expression, SyntaxFactory.ParseTypeName(type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)).WithTriviaFrom(value), null)];

            // An extension written on the static class that holds it, with the arguments of the call (`GivenAlphaExtensions.OpacityUnder(theme)`):
            // a call on a value of a type the extension is for.
            case "CS7036" or "CS1501" or "CS1503" or "CS1929" when node.FirstAncestorOrSelf<InvocationExpressionSyntax>() is { Expression: MemberAccessExpressionSyntax { Expression: TypeSyntax holder, Name: var called } held } call
                                                     && model.GetSymbolInfo(holder).Symbol is INamedTypeSymbol { IsStatic: true } holding
                                                     && PackageSurface.Loaded.IsExtensionIn(holding.Name, called.Identifier.ValueText)
                                                     && !call.HasAnnotation(Pinned) && !call.HasAnnotations(Open):
                var onValue = "__r" + held.SpanStart;
                var replaced = held.WithExpression(SyntaxFactory.IdentifierName(onValue).WithTriviaFrom(holder));

                return [.. PackageSurface.Loaded.TypesHaving(called.Identifier.ValueText).Select(ReceiverOf).OfType<Receiver>().Where(each => !each.IsStatic)
                    .Take(MostReceivers).Select(each => new Edit(held, replaced, Parameter(each.Syntax, onValue)))];

            // A name that is a type, written as a call (`InsetBounds(X, Y, Width, Height)`, `AxisScaleMode.SlidingWindow(size)`): its constructor.
            case "CS1955" or "CS1061" when node.FirstAncestorOrSelf<InvocationExpressionSyntax>() is { Expression: MemberAccessExpressionSyntax access } invocation
                                           && (error.Id == "CS1955" || access.Expression is IdentifierNameSyntax { Identifier.ValueText: var bare } && bare.StartsWith("__r", StringComparison.Ordinal))
                                           && Constructed(model, access) is { } created:
                return [new Edit(invocation, SyntaxFactory.ObjectCreationExpression(SyntaxFactory.Token(SyntaxKind.NewKeyword).WithTrailingTrivia(SyntaxFactory.Space), created, invocation.ArgumentList, null).WithTriviaFrom(invocation), null)];

            // Several overloads take a value of no type: the one whose parameters the written types name, else the first.
            case "CS0121" when node.FirstAncestorOrSelf<InvocationExpressionSyntax>() is { } invocation
                               && !invocation.HasAnnotation(Pinned)
                               && Candidates(model, invocation) is { Count: > 0 } candidates:
                return [new Edit(invocation, Pin(invocation, candidates.FirstOrDefault(each => WrittenTypesFit(invocation, each)) ?? candidates[0], null), null)];

            // A call that writes `…` takes further arguments where it stands: an overload with at least as many parameters, the arguments
            // after the `…` given to the parameters at each place they may stand.
            case "CS1501" or "CS7036" or "CS0411" or "CS1660" or "CS1503" or "CS1739" when node.FirstAncestorOrSelf<InvocationExpressionSyntax>() is { } invocation
                                                                                     && invocation.HasAnnotations(Open)
                                                                                     && !invocation.HasAnnotation(Pinned):
                return OpenPins(invocation, model);

            // A name two namespaces declare: the call fits when it fits with one of them.
            case "CS0104" when node is SimpleNameSyntax ambiguous && model.GetSymbolInfo(ambiguous).CandidateSymbols.OfType<INamedTypeSymbol>().ToList() is { Count: > 0 } named:
                return [.. named.Select(type => new Edit(node, SyntaxFactory.ParseTypeName(type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)).WithTriviaFrom(node), null))];

            default:
                return [];
        }
    }

    // Whether the type an access is written on has the member read on a value of it: an instance member or an extension.
    private static bool ReachesAsInstance(SemanticModel model, MemberAccessExpressionSyntax access) =>
        model.GetSymbolInfo(access.Expression).Symbol is INamedTypeSymbol type
        && PackageSurface.Loaded.TypesNamed(type.Name).Concat(FrameworkType(type)).Any(each => PackageSurface.Loaded.Reaches([each], access.Name.Identifier.ValueText));

    private static IEnumerable<Type> FrameworkType(INamedTypeSymbol type) =>
        type.SpecialType != SpecialType.None && Type.GetType("System." + type.MetadataName) is { } known ? [known] : [];

    // The member access of an error that reads an instance member on a type that can have a value (no static class).
    private static MemberAccessExpressionSyntax? TypeAccess(SyntaxNode node, SemanticModel model) =>
        node.DescendantNodesAndSelf().Concat(node.Ancestors()).OfType<MemberAccessExpressionSyntax>()
            .FirstOrDefault(access => access.Expression is TypeSyntax && model.GetSymbolInfo(access.Expression).Symbol is INamedTypeSymbol { IsStatic: false });

    // The type a name written as a call stands for: the nested type the member is, or the type of the packages of that name.
    private static TypeSyntax? Constructed(SemanticModel model, MemberAccessExpressionSyntax access)
    {
        var symbol = model.GetSymbolInfo(access).Symbol ?? model.GetSymbolInfo(access).CandidateSymbols.FirstOrDefault();

        if (symbol is INamedTypeSymbol nested)
        {
            return SyntaxFactory.ParseTypeName(nested.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        }

        return PackageSurface.Loaded.TypesNamed(access.Name.Identifier.ValueText).Where(type => !(type.IsAbstract && type.IsSealed)).Select(Syntax).OfType<string>().FirstOrDefault() is { } named
            ? SyntaxFactory.ParseTypeName(named)
            : null;
    }

    // The pins of a call that writes `…`: for every overload long enough, every place the arguments after the `…` may stand.
    private static IReadOnlyList<Edit> OpenPins(InvocationExpressionSyntax invocation, SemanticModel model)
    {
        var at = int.Parse(invocation.GetAnnotations(Open).First().Data!, System.Globalization.CultureInfo.InvariantCulture);
        var given = invocation.ArgumentList.Arguments.Count;
        var after = given - at;

        return
        [
            .. Candidates(model, invocation).Where(method => method.Parameters.Length >= given)
                .SelectMany(method => Enumerable.Range(at, method.Parameters.Length - after - at + 1).Select(offset => (method, offset)))
                .Select(each => new Edit(invocation, Pin(invocation, each.method, after > 0 ? (at, each.offset) : null), null)),
        ];
    }

    // The edit that gives an undefined name a value of a receiver type: the name a parameter of the method, a bare call read on one, a
    // static class written by its name (its members are called on the type).
    private static Edit ReceiverEdit(IdentifierNameSyntax name, Receiver receiver)
    {
        var bare = !(name.Parent is MemberAccessExpressionSyntax access && access.Expression == name && !char.IsAsciiLetterUpper(name.Identifier.ValueText[0]));


        if (receiver.IsStatic)
        {
            var type = SyntaxFactory.ParseTypeName(receiver.Syntax);

            return bare
                ? new Edit(name, SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, type, name.WithoutTrivia()).WithTriviaFrom(name), null)
                : new Edit(name, type.WithTriviaFrom(name), null);
        }

        if (!bare)
        {
            return new Edit(null, null, Parameter(receiver.Syntax, name.Identifier.ValueText));
        }

        var value = "__r" + name.SpanStart;

        return new Edit(name, SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName(value), name.WithoutTrivia()).WithTriviaFrom(name), Parameter(receiver.Syntax, value));
    }

    // The tree with the edits made: each node replaced, each parameter given to the method, each type parameter declared.
    private static SyntaxNode Build(SyntaxNode root, IReadOnlyList<Edit> edits, IReadOnlyList<string> typeParameters)
    {
        var replaced = edits.Where(edit => edit.Node is not null).GroupBy(edit => edit.Node!).ToDictionary(group => group.Key, group => group.First().Replacement!);
        var rewritten = root.ReplaceNodes(replaced.Keys, (original, _) => replaced[original]);

        rewritten = edits.Select(edit => edit.Parameter).OfType<ParameterSyntax>().Aggregate(rewritten, WithParameter);
        rewritten = typeParameters.Distinct().Aggregate(rewritten, WithTypeParameter);

        // Each tree its own namespace, so the trees of one call tried at once declare no class twice.
        var own = rewritten.DescendantNodes().OfType<NamespaceDeclarationSyntax>().First(each => each.Name.ToString().StartsWith("__Written", StringComparison.Ordinal));

        return rewritten.ReplaceNode(own, own.WithName(SyntaxFactory.ParseName(Namespace()).WithTriviaFrom(own.Name)));
    }

    private static readonly SyntaxAnnotation Pinned = new("written-pinned");

    // What the compiled call says against what the document says beside it: the types it writes, the type it returns, the defaults.
    private static CallMismatch? Said(Attempt attempt, SemanticModel model)
    {
        var root = attempt.Tree.GetRoot();

        foreach (var argument in root.GetAnnotatedNodes(WrittenType).OfType<ArgumentSyntax>())
        {
            var written = argument.GetAnnotations(WrittenType).First().Data!;

            if (ParameterOf(model, argument) is { } parameter && !SameType(parameter.Type, written) && !DerivesFrom(written, parameter.Type)
                && !string.Equals(written, parameter.Name, StringComparison.OrdinalIgnoreCase))
            {
                return Mismatch(attempt.Job, attempt.Tree, null, "Type", $"the parameter {parameter.Name} is a {parameter.Type.Name}, the document writes {written}");
            }
        }

        foreach (var argument in root.GetAnnotatedNodes(WrittenDefault).OfType<ArgumentSyntax>())
        {
            if (ParameterOf(model, argument) is { } parameter && !SameDefault(parameter, model.GetConstantValue(argument.Expression)))
            {
                return Mismatch(attempt.Job, attempt.Tree, null, "Default", parameter.HasExplicitDefaultValue
                    ? $"the parameter {parameter.Name} defaults to {parameter.ExplicitDefaultValue ?? "null"}, the document writes {argument.Expression}"
                    : $"the parameter {parameter.Name} has no default, the document writes {argument.Expression}");
            }
        }

        foreach (var call in root.GetAnnotatedNodes(WrittenReturns).OfType<ExpressionSyntax>())
        {
            var written = call.GetAnnotations(WrittenReturns).First().Data!;

            if (model.GetTypeInfo(call).Type is { } type && !SameType(type, written))
            {
                return Mismatch(attempt.Job, attempt.Tree, null, "Returns", $"the call returns {type.Name}, the document says {written}");
            }
        }

        return null;
    }

    // Whether a type the document writes, a type of the packages, is or derives from the parameter's type (`WithProjection(Robinson)` for an
    // IGeoProjection).
    private static bool DerivesFrom(string written, ITypeSymbol parameter) =>
        PackageSurface.Loaded.TypesNamed(written.Split('<')[0].Trim()).Any(type =>
            type.GetInterfaces().Concat(Bases(type)).Any(each => each.Name.Split('`')[0] == parameter.Name));

    private static IEnumerable<Type> Bases(Type type)
    {
        for (var each = type.BaseType; each is not null; each = each.BaseType)
        {
            yield return each;
        }
    }

    private static IParameterSymbol? ParameterOf(SemanticModel model, ArgumentSyntax argument)
    {
        if (argument.Parent?.Parent is not { } call || model.GetSymbolInfo(call).Symbol is not IMethodSymbol method || argument.Parent is not ArgumentListSyntax list)
        {
            return null;
        }

        return argument.NameColon is { } named
            ? method.Parameters.FirstOrDefault(each => each.Name == named.Name.Identifier.ValueText)
            : method.Parameters.ElementAtOrDefault(list.Arguments.IndexOf(argument)) ?? method.Parameters.LastOrDefault(each => each.IsParams);
    }

    private static bool SameDefault(IParameterSymbol parameter, Optional<object?> written)
    {
        if (!parameter.HasExplicitDefaultValue || !written.HasValue)
        {
            return parameter.HasExplicitDefaultValue && !written.HasValue;
        }

        var held = parameter.ExplicitDefaultValue;

        return Equals(held, written.Value)
               || (held is IConvertible && written.Value is IConvertible && held is not string && written.Value is not string
                   && Convert.ToDouble(held, System.Globalization.CultureInfo.InvariantCulture) == Convert.ToDouble(written.Value, System.Globalization.CultureInfo.InvariantCulture));
    }

    // Whether a type is the type a document writes by name: the same name and the same array rank, type arguments read only where the
    // document writes them, a nullable value type the type it wraps, a keyword (double, string) the type it stands for.
    private static bool SameType(ITypeSymbol type, string written)
    {
        var text = written.Replace(" ", string.Empty, StringComparison.Ordinal).Replace("global::", string.Empty, StringComparison.Ordinal).TrimEnd('?');

        if (type is IArrayTypeSymbol array)
        {
            return text.EndsWith("[]", StringComparison.Ordinal) && SameType(array.ElementType, text[..^2]);
        }

        if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T, TypeArguments: [var wrapped] })
        {
            return SameType(wrapped, text);
        }

        var parsed = SyntaxFactory.ParseTypeName(text);
        var name = parsed switch
        {
            PredefinedTypeSyntax keyword => KeywordTypes.TryGetValue(keyword.Keyword.ValueText, out var full) ? full : keyword.Keyword.ValueText,
            _ => CallText.NameOf(parsed) ?? text,
        };

        if (type.Name != name || text.EndsWith("[]", StringComparison.Ordinal))
        {
            return false;
        }

        var arguments = (parsed switch { GenericNameSyntax generic => generic, QualifiedNameSyntax { Right: GenericNameSyntax right } => right, _ => null })?.TypeArgumentList.Arguments;

        return arguments is null || (type is INamedTypeSymbol named && named.TypeArguments.Length == arguments.Value.Count
                                     && named.TypeArguments.Zip(arguments.Value).All(pair => SameType(pair.First, pair.Second.ToString())));
    }

    private static readonly Dictionary<string, string> KeywordTypes = new(StringComparer.Ordinal)
    {
        ["bool"] = "Boolean", ["byte"] = "Byte", ["char"] = "Char", ["decimal"] = "Decimal", ["double"] = "Double", ["float"] = "Single",
        ["int"] = "Int32", ["long"] = "Int64", ["object"] = "Object", ["short"] = "Int16", ["string"] = "String", ["uint"] = "UInt32",
        ["ulong"] = "UInt64", ["void"] = "Void",
    };

    private static bool WrittenTypesFit(InvocationExpressionSyntax invocation, IMethodSymbol method) =>
        invocation.ArgumentList.Arguments.Select((argument, index) => (argument, index))
            .Where(each => each.argument.HasAnnotations(WrittenType))
            .All(each => (each.argument.NameColon is { } named ? method.Parameters.FirstOrDefault(p => p.Name == named.Name.Identifier.ValueText) : method.Parameters.ElementAtOrDefault(each.index)) is { } parameter
                         && SameType(parameter.Type, each.argument.GetAnnotations(WrittenType).First().Data!));

    private static List<IMethodSymbol> Candidates(SemanticModel model, InvocationExpressionSyntax invocation) =>
    [
        .. model.GetSymbolInfo(invocation).CandidateSymbols.OfType<IMethodSymbol>()
            .Concat(model.GetMemberGroup(invocation.Expression).OfType<IMethodSymbol>())
            .Distinct<IMethodSymbol>(SymbolEqualityComparer.Default),
    ];

    // The call with one overload's parameter types given to its placeholders, and, for a call that writes `…`, a value of each type for
    // the parameters the document leaves out that have no default, the arguments after the `…` named for the parameters from the offset
    // on: the compiler then binds that overload and reads what follows it.
    private static SyntaxNode Pin(InvocationExpressionSyntax invocation, IMethodSymbol method, (int At, int Offset)? shift)
    {
        var arguments = invocation.ArgumentList.Arguments;
        var given = new HashSet<string>(StringComparer.Ordinal);
        var pinned = new List<ArgumentSyntax>();

        for (var index = 0; index < arguments.Count; index++)
        {
            var argument = arguments[index];
            var position = shift is { } moved && index >= moved.At ? moved.Offset + index - moved.At : index;
            var parameter = argument.NameColon is { } named ? method.Parameters.FirstOrDefault(each => each.Name == named.Name.Identifier.ValueText) : method.Parameters.ElementAtOrDefault(position);

            if (parameter is not null)
            {
                given.Add(parameter.Name);

                if (shift is not null && argument.NameColon is null)
                {
                    argument = argument.WithNameColon(SyntaxFactory.NameColon(EscapedName(parameter.Name)));
                }
            }

            pinned.Add(parameter is not null && argument.Expression.HasAnnotation(Placeholder) && Typed(parameter.Type) is { } type
                ? argument.WithExpression(type)
                : argument);
        }

        if (invocation.HasAnnotations(Open))
        {
            pinned.AddRange(method.Parameters.Where(each => !given.Contains(each.Name) && !each.IsOptional && !each.IsParams)
                .Select(each => SyntaxFactory.Argument(SyntaxFactory.NameColon(EscapedName(each.Name)), default, Typed(each.Type) ?? Default())));
        }

        return invocation.WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(pinned))).WithAdditionalAnnotations(Pinned);
    }

    // A parameter's name as an argument names it: a keyword (`base`) after an @.
    private static IdentifierNameSyntax EscapedName(string name) =>
        SyntaxFactory.IdentifierName(SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None ? SyntaxFactory.Identifier(default, SyntaxKind.IdentifierToken, "@" + name, name, default) : SyntaxFactory.Identifier(name));

    // A value of a type, marked as a placeholder; null for a type the method's own type parameters make.
    private static ExpressionSyntax? Typed(ITypeSymbol type) =>
        type.DescendantTypes().Any(each => each.TypeKind == TypeKind.TypeParameter) || type.IsAnonymousType
            ? null
            : SyntaxFactory.DefaultExpression(SyntaxFactory.ParseTypeName(type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))).WithAdditionalAnnotations(Placeholder);

    private static ExpressionSyntax Default() => SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression).WithAdditionalAnnotations(Placeholder);

    // The types a name the document leaves undefined is tried as. A value a member is read on or called on (`ax.Plot`, the `__r` a bare
    // call of a table is read on): the type it stands under, the type its name conventionally is, every type that has the member, and
    // last the conventional type that does not (so the error names it). A call written bare or a name in capitals read on (`Clients`):
    // every type that has it as a member.
    private static IReadOnlyList<Receiver> Receivers(IdentifierNameSyntax name, Attempt attempt)
    {
        var variable = name.Identifier.ValueText;

        if (variable.Length == 0)
        {
            return [];
        }

        var capital = char.IsAsciiLetterUpper(variable[0]);
        var surface = PackageSurface.Loaded;

        if (name.Parent is InvocationExpressionSyntax invoked && invoked.Expression == name || capital && name.Parent is MemberAccessExpressionSyntax { Expression: var read } && read == name)
        {
            return capital
                ? [.. surface.TypesReaching(variable).OrderBy(type => attempt.Job.Source.Owners.Contains(type.Name) ? 0 : 1).Select(ReceiverOf).OfType<Receiver>().Distinct()]
                : [];
        }

        if (name.Parent is not MemberAccessExpressionSyntax access || access.Expression != name)
        {
            return [];
        }

        var member = access.Name.Identifier.ValueText;
        var receiver = variable == "__r";
        var owners = receiver ? attempt.Job.Source.Owners.SelectMany(surface.TypesNamed).Where(type => surface.Reaches([type], member)).Select(ReceiverOf).OfType<Receiver>() : [];
        var conventional = ConventionalReceivers.TryGetValue(variable, out var names) ? names : [];

        // A value is never a static class: only the receiver a bare call of a table is read on may stand for one (its members are called
        // on the type).
        var having = (receiver ? surface.TypesReaching(member) : surface.TypesHaving(member)).OrderBy(type => type.Name.EndsWith("Builder", StringComparison.Ordinal) ? 0 : 1).ThenBy(type => type.FullName, StringComparer.Ordinal)
            .Select(ReceiverOf).OfType<Receiver>().Where(each => receiver || !each.IsStatic);

        // A value read with what an array or a sequence has (`x.Length`, `values.Select(...)`) is the data of the example: an array of
        // numbers, or a sequence of values of no type when its items are read on.
        Receiver[] sequences = [new Receiver("double[]", false), new Receiver("global::System.Collections.Generic.IEnumerable<dynamic>", false)];
        var data = !receiver && conventional.Length == 0 && ArrayMembers.Value.Contains(member)
            ? ReadsItems(access) ? [.. sequences.Reverse()] : sequences
            : Array.Empty<Receiver>();

        // Last, a value of a type the document leaves unsaid and no type of the packages is: what compiles on it says nothing of them.
        var unsaid = !receiver && conventional.Length == 0 && (!surface.TypesHaving(member).Any() || AboutTests(attempt)) ? [new Receiver(Dynamic, false)] : Array.Empty<Receiver>();

        return [.. owners.Concat(conventional.Select(name => new Receiver(name, false))).Concat(data).Concat(having).Concat(unsaid).Distinct()];
    }

    // Whether a call on a sequence hands a lambda its items and reads a member of one (`processes.Select(p => p.Name)`): the items are
    // objects, no numbers.
    private static bool ReadsItems(MemberAccessExpressionSyntax access) =>
        access.Parent is InvocationExpressionSyntax call && call.ArgumentList.Arguments.Select(argument => argument.Expression).OfType<SimpleLambdaExpressionSyntax>()
            .Any(lambda => lambda.Body.DescendantNodesAndSelf().OfType<MemberAccessExpressionSyntax>()
                .Any(read => read.Expression is IdentifierNameSyntax item && item.Identifier.ValueText == lambda.Parameter.Identifier.ValueText));

    // What an array of numbers has to read or call: its own members and those LINQ gives a sequence.
    private static readonly Lazy<HashSet<string>> ArrayMembers = new(() =>
        [.. typeof(double[]).GetMembers().Select(member => member.Name), .. typeof(Enumerable).GetMethods().Select(method => method.Name)]);

    private static Receiver? ReceiverOf(Type type) => Syntax(type) is { } syntax ? new Receiver(syntax, type is { IsAbstract: true, IsSealed: true }) : null;

    private const string Dynamic = "dynamic";

    // Whether an undefined name stands for a value: a name nothing is read from or called on.
    private static bool IsValue(IdentifierNameSyntax name) =>
        !(name.Parent is MemberAccessExpressionSyntax access && access.Expression == name) && !(name.Parent is InvocationExpressionSyntax invocation && invocation.Expression == name)
        && name.Parent is not QualifiedNameSyntax;

    private static bool IsTypeParameterName(string name) => name.Length == 1 ? char.IsAsciiLetterUpper(name[0]) : name.Length > 1 && name[0] == 'T' && char.IsAsciiLetterUpper(name[1]);

    private static ParameterSyntax Parameter(string type, string name) =>
        SyntaxFactory.Parameter(SyntaxFactory.Identifier(name)).WithType(SyntaxFactory.ParseTypeName(type).WithTrailingTrivia(SyntaxFactory.Space));

    // A name given a value is a parameter of the method that holds the block's statements, and a field of each type the block declares
    // that reads it (a window class, a razor component), which a parameter cannot reach.
    private static SyntaxNode WithParameter(SyntaxNode root, ParameterSyntax parameter)
    {
        var name = parameter.Identifier.ValueText;
        var readers = root.DescendantNodes().OfType<IdentifierNameSyntax>().Where(each => each.Identifier.ValueText == name)
            .Select(each => each.Ancestors().OfType<TypeDeclarationSyntax>().LastOrDefault())
            .OfType<TypeDeclarationSyntax>().Where(type => type.Identifier.ValueText != Calls).Distinct().ToList();
        var field = SyntaxFactory.ParseMemberDeclaration($"private static {parameter.Type} {name};")!;

        root = root.ReplaceNodes(readers, (_, type) => type.Members.OfType<FieldDeclarationSyntax>().Any(each => each.Declaration.Variables.Any(variable => variable.Identifier.ValueText == name))
            ? type
            : type.AddMembers(field));

        var method = root.DescendantNodes().OfType<MethodDeclarationSyntax>().First(each => each.Identifier.ValueText == Method);

        return method.ParameterList.Parameters.Any(each => each.Identifier.ValueText == name)
            ? root
            : root.ReplaceNode(method, method.AddParameterListParameters(parameter));
    }

    private static SyntaxNode WithTypeParameter(SyntaxNode root, string name)
    {
        var method = root.DescendantNodes().OfType<MethodDeclarationSyntax>().First(each => each.Identifier.ValueText == Method);

        return method.TypeParameterList?.Parameters.Any(each => each.Identifier.ValueText == name) == true
            ? root
            : root.ReplaceNode(method, method.AddTypeParameterListParameters(SyntaxFactory.TypeParameter(name)));
    }

    // The C# a type is written as from anywhere: its full name with global::, nested types after a dot; null for a generic definition.
    private static string? Syntax(Type type)
    {
        if (type.IsGenericTypeDefinition || type.ContainsGenericParameters || type.FullName is null || type.IsPointer || type.FullName.Contains('<', StringComparison.Ordinal))
        {
            return null;
        }

        if (type.IsArray)
        {
            return Syntax(type.GetElementType()!) is { } element ? element + "[" + new string(',', type.GetArrayRank() - 1) + "]" : null;
        }

        if (!type.IsGenericType)
        {
            return "global::" + type.FullName.Replace('+', '.');
        }

        var definition = type.GetGenericTypeDefinition().FullName!;
        var arguments = type.GetGenericArguments().Select(Syntax).ToList();

        return arguments.Any(each => each is null) ? null : "global::" + definition[..definition.IndexOf('`')].Replace('+', '.') + "<" + string.Join(", ", arguments) + ">";
    }

    private static CallMismatch Mismatch(Job job, SyntaxTree tree, Diagnostic? error, string id, string message)
    {
        var line = job.Source.Line;
        var code = job.Source.Code;

        // An error in a block names the line of the block it stands on, as the document writes it; an error in the code the reader wrote
        // around the block names the line of that code.
        if (job.Source.IsBlock && error is not null)
        {
            var mapped = error.Location.GetMappedLineSpan();
            var written = SourceFile.LinesOf(job.Source.Code);

            if (mapped.HasMappedPath && mapped.StartLinePosition.Line < written.Length)
            {
                code = written[mapped.StartLinePosition.Line].Trim();
                line += mapped.StartLinePosition.Line;
            }
            else
            {
                code = tree.GetText().Lines[error.Location.GetLineSpan().StartLinePosition.Line].ToString().Trim();
            }
        }

        return new CallMismatch(job.Source.Document, line, code, id, message);
    }

    private static IEnumerable<Type> TypesOf(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException partial)
        {
            return partial.Types.OfType<Type>();
        }
    }

    /// <summary>One claim, as one tree to compile.</summary>
    private sealed record Job(int Claim, CallClaim Source, IReadOnlyList<SyntaxTree> Trees);

    /// <summary>A tree being compiled for a job, and its rank among the job's trees.</summary>
    private sealed record Attempt(Job Job, SyntaxTree Tree, IReadOnlyList<int> Rank);

    /// <summary>One way to meet an error: a node replaced, a parameter given to the method, or both.</summary>
    private readonly record struct Edit(SyntaxNode? Node, SyntaxNode? Replacement, ParameterSyntax? Parameter);

    /// <summary>What one compilation of an attempt leads to: a tree to compile next (and further ones to try), or what is wrong.</summary>
    private readonly record struct Outcome(SyntaxTree? Next, IReadOnlyList<SyntaxTree> Alternatives, CallMismatch? Mismatch, string? Foreign);

    /// <summary>A type an undefined name is tried as: how C# writes it from anywhere, and whether it is a static class (whose members are
    /// called on the type, not on a value).</summary>
    private readonly record struct Receiver(string Syntax, bool IsStatic);

    /// <summary>Reads the placeholders of written code as values of no type, an optional argument as a named one, a parameter of a
    /// signature as a named argument that writes its type, an argument written <c>name = value</c> as the default it states.</summary>
    private sealed class PlaceholderRewriter(IReadOnlyList<string> types, bool keepOptional) : CSharpSyntaxRewriter
    {
        public override SyntaxNode? VisitArgumentList(ArgumentListSyntax node)
        {
            var rest = node.Arguments.Select(argument => argument.Expression is IdentifierNameSyntax { Identifier.ValueText: CallText.Rest }).ToList();
            var visited = (ArgumentListSyntax)base.VisitArgumentList(node)!;
            var kept = visited.Arguments.Where((argument, index) => !rest[index] && !(argument.HasAnnotations(Optional) && !keepOptional));

            return visited.WithArguments(SyntaxFactory.SeparatedList(kept));
        }

        public override SyntaxNode? VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            var open = node.ArgumentList?.Arguments.Any(argument => argument.Expression is IdentifierNameSyntax { Identifier.ValueText: CallText.Rest }) == true;
            var visited = (ObjectCreationExpressionSyntax)base.VisitObjectCreationExpression(node)!;

            return open ? visited.WithAdditionalAnnotations(new SyntaxAnnotation(Open, "0")) : visited;
        }

        public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            var rest = node.ArgumentList.Arguments.IndexOf(argument => argument.Expression is IdentifierNameSyntax { Identifier.ValueText: CallText.Rest });
            var visited = (InvocationExpressionSyntax)base.VisitInvocationExpression(node)!;

            return rest >= 0 ? visited.WithAdditionalAnnotations(new SyntaxAnnotation(Open, rest.ToString(System.Globalization.CultureInfo.InvariantCulture))) : visited;
        }

        public override SyntaxNode? VisitArgument(ArgumentSyntax node)
        {
            if (node.Expression is IdentifierNameSyntax { Identifier.ValueText: var name })
            {
                if (name.StartsWith(CallText.Optional, StringComparison.Ordinal))
                {
                    return SyntaxFactory.Argument(SyntaxFactory.NameColon(name[CallText.Optional.Length..]), default, Default()).WithAdditionalAnnotations(new SyntaxAnnotation(Optional));
                }

                if (name.StartsWith(CallText.Typed, StringComparison.Ordinal) && int.TryParse(name[CallText.Typed.Length..^2], out var index))
                {
                    return node.WithExpression(Default()).WithAdditionalAnnotations(new SyntaxAnnotation(WrittenType, types[index]));
                }
            }

            // `this`, the value of the type a comment stands in, written as an argument.
            if (node.NameColon is null && node.Expression is ThisExpressionSyntax)
            {
                return node.WithExpression(Default());
            }

            // `base`, a keyword, written as a parameter's name: a value, or the parameter @base with its default.
            if (node.NameColon is null && node.Expression is BaseExpressionSyntax)
            {
                return node.WithExpression(Default());
            }

            if (node.NameColon is null && node.Expression is AssignmentExpressionSyntax { Left: BaseExpressionSyntax, Right: var given })
            {
                return SyntaxFactory.Argument(SyntaxFactory.NameColon(EscapedName("base")), default, given).WithAdditionalAnnotations(new SyntaxAnnotation(WrittenDefault));
            }

            // A signature's `offset = 0` is the parameter offset with its default.
            if (node.NameColon is null && node.Expression is AssignmentExpressionSyntax { Left: IdentifierNameSyntax target, Right: var value } && node.Parent?.Parent is InvocationExpressionSyntax)
            {
                return SyntaxFactory.Argument(SyntaxFactory.NameColon(target.Identifier.ValueText), default, value).WithAdditionalAnnotations(new SyntaxAnnotation(WrittenDefault));
            }

            if (node.NameColon is not null && node.Expression is IdentifierNameSyntax { Identifier.ValueText: var typed } && typed.StartsWith(CallText.Typed, StringComparison.Ordinal)
                && int.TryParse(typed[CallText.Typed.Length..^2], out var at))
            {
                return node.WithExpression(Default()).WithAdditionalAnnotations(new SyntaxAnnotation(WrittenType, types[at]));
            }

            return base.VisitArgument(node);
        }

        public override SyntaxNode? VisitThisExpression(ThisExpressionSyntax node) => SyntaxFactory.IdentifierName("__r").WithTriviaFrom(node);

        public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node) =>
            node.Identifier.ValueText == CallText.Rest ? Default().WithTriviaFrom(node) : base.VisitIdentifierName(node);

        public override SyntaxNode? VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node) =>
            node.ExpressionBody is IdentifierNameSyntax { Identifier.ValueText: CallText.Rest }
                ? node.WithExpressionBody(null).WithBody(SyntaxFactory.Block()).WithAdditionalAnnotations(LeftOutBody)
                : base.VisitSimpleLambdaExpression(node);

        public override SyntaxNode? VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node) =>
            node.ExpressionBody is IdentifierNameSyntax { Identifier.ValueText: CallText.Rest }
                ? node.WithExpressionBody(null).WithBody(SyntaxFactory.Block()).WithAdditionalAnnotations(LeftOutBody)
                : base.VisitParenthesizedLambdaExpression(node);

        public override SyntaxNode? VisitInitializerExpression(InitializerExpressionSyntax node)
        {
            var visited = (InitializerExpressionSyntax)base.VisitInitializerExpression(node)!;
            var kept = visited.Expressions.Where(each => !each.HasAnnotation(Placeholder)).ToList();

            // `{ … }` after a constructor leaves out what the object is given: an object initializer of nothing.
            return kept.Count == 0 && visited.Expressions.Count > 0 && visited.IsKind(SyntaxKind.CollectionInitializerExpression) && node.Parent is ObjectCreationExpressionSyntax
                ? SyntaxFactory.InitializerExpression(SyntaxKind.ObjectInitializerExpression)
                : visited.WithExpressions(SyntaxFactory.SeparatedList(kept));
        }
    }
}

/// <summary>The types a type is made of, itself first.</summary>
internal static class TypeSymbolExtensions
{
    extension(ITypeSymbol type)
    {
        /// <summary>The type, its element type, its type arguments, and theirs.</summary>
        public IEnumerable<ITypeSymbol> DescendantTypes()
        {
            yield return type;

            var parts = type switch
            {
                IArrayTypeSymbol array => [array.ElementType],
                INamedTypeSymbol named => named.TypeArguments,
                _ => [],
            };

            foreach (var part in parts.SelectMany(each => each.DescendantTypes()))
            {
                yield return part;
            }
        }
    }
}
