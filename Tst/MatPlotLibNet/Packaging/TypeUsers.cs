// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>An internal type of the library and the types that use it.</summary>
/// <param name="Type">The type, by its full name.</param>
/// <param name="File">The repository path and line of its declaration.</param>
/// <param name="Users">The types that use it, each the outermost type its use is written in, by full name.</param>
/// <param name="Members">The members of it a caller calls, by name: its methods, properties, events and operators, the members of its
/// extension blocks among them; a field, a constant, a nested type, a constructor and what the compiler declares for it are not.</param>
/// <param name="Shape">What kind of type it is.</param>
/// <param name="Readers">The members whose code uses it (a method, a property, a field, a constructor, or the declaration of a type
/// when its base list or its primary constructor names it), each as the type it is written in and its name, in order.</param>
internal readonly record struct TypeUse(string Type, string File, IReadOnlyList<string> Users, IReadOnlyList<string> Members, TypeShape Shape,
    IReadOnlyList<string> Readers);

/// <summary>What kind of type a <see cref="TypeUse"/> is.</summary>
internal enum TypeShape
{
    /// <summary>A static class a type of the library could own the members of: a plain static member, or an extension of a class or
    /// struct the library declares (a type parameter by its class constraint).</summary>
    Owned,

    /// <summary>A static class whose members all extend a type the library cannot give a member: a type of the framework or of a
    /// package, an enum, an interface.</summary>
    Extension,

    /// <summary>A class, record or struct that a call reaches by dispatch: it derives from a base, implements an interface, or another
    /// type of the library derives from it.</summary>
    Polymorphic,

    /// <summary>A class, record or struct of its own that holds one value or none: one field (the field behind an auto-property or a
    /// record's positional property among them), or one parameter of its primary constructor.</summary>
    Wrapper,

    /// <summary>A class, record or struct of its own that holds more than one value.</summary>
    Value,

    /// <summary>An interface.</summary>
    Interface,

    /// <summary>An enum.</summary>
    Enum,
}

/// <summary>Which types of the library use which internal type, read by the compiler. A type uses another when its code names it or
/// reaches one of its members (a call, a property, an extension member, an operator, an indexer, the enumerator a <c>foreach</c>
/// takes and the type whose <c>MoveNext</c> it calls, the <c>Deconstruct</c> a positional pattern or a deconstruction calls, the
/// handler the compiler builds from an interpolated string written where a member takes one); a call of a member of the library's own base or interface also uses every type that overrides or implements that member,
/// and a type deriving from a base (or implementing an interface) of the library is used by every user of that base. A use written
/// inside the type itself is not counted; a use inside a nested type counts for the outermost type it is written in.</summary>
internal static class TypeUsers
{
    /// <summary>Every type declared in <paramref name="compilations"/> that cannot be seen outside its assembly (a class, record,
    /// struct, interface or enum, internal or private to the type it is nested in) with the types that use it.</summary>
    /// <param name="compilations">The packages, each compiled against the ones it references.</param>
    /// <returns>One row per internal type, its users in order.</returns>
    internal static IReadOnlyList<TypeUse> Of(IReadOnlyList<Compilation> compilations)
    {
        var declared = new Dictionary<string, INamedTypeSymbol>(StringComparer.Ordinal);
        var derivedFrom = new HashSet<string>(StringComparer.Ordinal);
        var landings = new Dictionary<string, List<INamedTypeSymbol>>(StringComparer.Ordinal);
        var extensions = new Dictionary<string, List<INamedTypeSymbol>>(StringComparer.Ordinal);
        foreach (var (model, declaration) in Declarations(compilations))
        {
            if (model.GetDeclaredSymbol(declaration) is not INamedTypeSymbol type) continue;
            if (!declared.TryAdd(Key(type), type)) continue;
            for (var below = type.BaseType; below is not null; below = below.BaseType) derivedFrom.Add(Key(below.OriginalDefinition));
            AddLandings(type, landings);
            AddExtensions(type, extensions);
        }

        var users = new Dictionary<string, SortedSet<string>>(StringComparer.Ordinal);
        var readers = new Dictionary<string, SortedSet<string>>(StringComparer.Ordinal);
        foreach (var compilation in compilations)
        {
            foreach (var tree in compilation.SyntaxTrees)
            {
                var model = compilation.GetSemanticModel(tree);
                foreach (var node in tree.GetRoot().DescendantNodes())
                {
                    var symbol = SymbolOf(model, node);
                    if (symbol is null && !IsUnboundMember(model, node)) continue;
                    var writer = node.Ancestors().OfType<BaseTypeDeclarationSyntax>().FirstOrDefault(IsType) is { } around
                        ? model.GetDeclaredSymbol(around) as INamedTypeSymbol
                        : null;
                    if (writer is null) continue;
                    var use = new Writer(writer, ReaderOf(node, writer));

                    if (symbol is null)
                    {
                        if (extensions.TryGetValue(((SimpleNameSyntax)node).Identifier.ValueText, out var named))
                        {
                            foreach (var type in named) Use(type, use, users, readers);
                        }

                        continue;
                    }

                    Use(OwnerOf(symbol), use, users, readers);
                    if (node is ForEachStatementSyntax loop) Use(model.GetForEachStatementInfo(loop).MoveNextMethod?.ContainingType, use, users, readers);
                    if (symbol is IMethodSymbol or IPropertySymbol or IEventSymbol
                        && landings.TryGetValue(Key(((symbol as IMethodSymbol)?.ReducedFrom ?? symbol).OriginalDefinition), out var landing))
                    {
                        foreach (var type in landing) Use(type, use, users, readers);
                    }
                }
            }
        }

        return
        [
            .. declared.Values
                .Where(IsNotPublic)
                .Select(type => new TypeUse(Name(type), Where(type), [.. UsersOf(type, users)], [.. MembersOf(type)], ShapeOf(type, derivedFrom),
                    [.. readers.TryGetValue(Key(type), out var read) ? read : []]))
                .OrderBy(use => use.Type, StringComparer.Ordinal),
        ];
    }

    /// <summary>The member whose code <paramref name="node"/> stands in, as the type it is written in and its name: the declaration of
    /// <paramref name="writer"/> itself when the node stands in its base list, its primary constructor or an attribute of it.</summary>
    private static string ReaderOf(SyntaxNode node, INamedTypeSymbol writer)
    {
        var member = node.Ancestors().OfType<MemberDeclarationSyntax>().First();
        string name = member switch
        {
            BaseTypeDeclarationSyntax => "(declaration)",
            MethodDeclarationSyntax method => method.Identifier.ValueText,
            PropertyDeclarationSyntax property => property.Identifier.ValueText,
            EventDeclarationSyntax @event => @event.Identifier.ValueText,
            BaseFieldDeclarationSyntax field => field.Declaration.Variables[0].Identifier.ValueText,
            ConstructorDeclarationSyntax => ".ctor",
            _ => member.Kind().ToString(),
        };
        return $"{Name(writer)}.{name}:{member.GetLocation().GetLineSpan().StartLinePosition.Line + 1}";
    }

    /// <summary>The members of <paramref name="type"/> a caller calls, by name, the members of its extension blocks among them.</summary>
    private static IEnumerable<string> MembersOf(INamedTypeSymbol type) =>
        type.GetMembers().Where(IsCalled)
            .Concat(type.GetTypeMembers().Where(block => block.IsExtension).SelectMany(block => block.GetMembers().Where(IsCalled)))
            .Select(member => member.Name)
            .Distinct(StringComparer.Ordinal);

    private static bool IsCalled(ISymbol member) => !member.IsImplicitlyDeclared && member switch
    {
        IMethodSymbol method => method.MethodKind is MethodKind.Ordinary or MethodKind.UserDefinedOperator or MethodKind.Conversion,
        _ => member is IPropertySymbol or IEventSymbol,
    };

    private static TypeShape ShapeOf(INamedTypeSymbol type, HashSet<string> derivedFrom) => type switch
    {
        { TypeKind: TypeKind.Interface } => TypeShape.Interface,
        { TypeKind: TypeKind.Enum } => TypeShape.Enum,
        { IsStatic: true } => type.GetMembers().Where(IsCalled).Concat(type.GetTypeMembers().Where(block => block.IsExtension)).All(IsExtensionOfOther)
            ? TypeShape.Extension
            : TypeShape.Owned,
        _ when type.Interfaces.Any(contract => !IsRecordEquality(type, contract))
            || type.BaseType is { SpecialType: not (SpecialType.System_Object or SpecialType.System_ValueType) }
            || derivedFrom.Contains(Key(type.OriginalDefinition)) => TypeShape.Polymorphic,
        _ => StateOf(type) <= 1 ? TypeShape.Wrapper : TypeShape.Value,
    };

    /// <summary>Whether <paramref name="contract"/> is the <c>IEquatable</c> of itself the compiler gives every record.</summary>
    private static bool IsRecordEquality(INamedTypeSymbol type, INamedTypeSymbol contract) =>
        type.IsRecord && contract is { Name: nameof(IEquatable<int>), TypeArguments: [var of] } && SymbolEqualityComparer.Default.Equals(of, type);

    /// <summary>Whether <paramref name="member"/> (an extension block, or a <c>this</c> method) extends a type no member of the library
    /// can be added to: a type of the framework or of a package, an enum, an interface.</summary>
    private static bool IsExtensionOfOther(ISymbol member) => member switch
    {
        INamedTypeSymbol { IsExtension: true, ExtensionParameter: { } receiver } => !IsLibraryValue(receiver.Type),
        IMethodSymbol { IsExtensionMethod: true } method => !IsLibraryValue(method.Parameters[0].Type),
        _ => false,
    };

    /// <summary>Whether <paramref name="type"/> is a class or struct the library declares, a type parameter by its class constraint.</summary>
    private static bool IsLibraryValue(ITypeSymbol type) => type switch
    {
        ITypeParameterSymbol parameter => parameter.ConstraintTypes.Any(IsLibraryValue),
        INamedTypeSymbol { TypeKind: TypeKind.Class or TypeKind.Struct } named => named.Locations.Any(location => location.IsInSource),
        _ => false,
    };

    /// <summary>The values <paramref name="type"/> holds: its instance fields (the field behind an auto-property, a record's positional
    /// property among them), and the parameters of its primary constructor.</summary>
    private static int StateOf(INamedTypeSymbol type) =>
        type.GetMembers().OfType<IFieldSymbol>().Count(field => !field.IsStatic && !field.IsConst
            && (!field.IsImplicitlyDeclared || field.AssociatedSymbol is IPropertySymbol))
        + type.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax()).OfType<TypeDeclarationSyntax>()
            .Where(declaration => declaration is not RecordDeclarationSyntax)
            .Sum(declaration => declaration.ParameterList?.Parameters.Count ?? 0);

    private static IEnumerable<(SemanticModel Model, BaseTypeDeclarationSyntax Declaration)> Declarations(IReadOnlyList<Compilation> compilations) =>
        from compilation in compilations
        from tree in compilation.SyntaxTrees
        let model = compilation.GetSemanticModel(tree)
        from declaration in tree.GetRoot().DescendantNodes().OfType<BaseTypeDeclarationSyntax>().Where(IsType)
        select (model, declaration);

    private static bool IsType(BaseTypeDeclarationSyntax declaration) =>
        declaration is ClassDeclarationSyntax or RecordDeclarationSyntax or StructDeclarationSyntax or InterfaceDeclarationSyntax or EnumDeclarationSyntax;

    /// <summary>Notes, for each member of a base or an interface of the library that <paramref name="type"/> overrides or implements,
    /// that a call of that member lands in <paramref name="type"/>.</summary>
    private static void AddLandings(INamedTypeSymbol type, Dictionary<string, List<INamedTypeSymbol>> landings)
    {
        foreach (var member in type.GetMembers())
        {
            for (var overridden = Overridden(member); overridden is not null; overridden = Overridden(overridden))
            {
                Land(overridden, type, landings);
            }
        }

        foreach (var contract in type.AllInterfaces)
        {
            foreach (var member in contract.GetMembers())
            {
                if (SymbolEqualityComparer.Default.Equals(type.FindImplementationForInterfaceMember(member)?.ContainingType?.OriginalDefinition, type.OriginalDefinition))
                {
                    Land(member, type, landings);
                }
            }
        }
    }

    /// <summary>Notes the names of the extension members <paramref name="type"/> declares (in an extension block or as a <c>this</c>
    /// method), so a call of one on a value whose type the compilation cannot bind (a type of a package of nuget.org) still counts.</summary>
    private static void AddExtensions(INamedTypeSymbol type, Dictionary<string, List<INamedTypeSymbol>> extensions)
    {
        if (!type.IsStatic) return;
        var names = type.GetTypeMembers().Where(block => block.IsExtension).SelectMany(block => block.GetMembers())
            .Concat(type.GetMembers().Where(member => member is IMethodSymbol { IsExtensionMethod: true }))
            .Select(member => member.Name)
            .Distinct(StringComparer.Ordinal);
        foreach (string name in names)
        {
            if (!extensions.TryGetValue(name, out var types)) extensions[name] = types = [];
            types.Add(type);
        }
    }

    /// <summary>Whether <paramref name="node"/> is the name of a member taken on a value the compiler could not bind, with no candidate.</summary>
    private static bool IsUnboundMember(SemanticModel model, SyntaxNode node) =>
        node is SimpleNameSyntax name && name.Parent is MemberAccessExpressionSyntax access && access.Name == name
        && model.GetSymbolInfo(name) is { Symbol: null, CandidateSymbols.IsEmpty: true };

    private static ISymbol? Overridden(ISymbol member) => member switch
    {
        IMethodSymbol method => method.OverriddenMethod,
        IPropertySymbol property => property.OverriddenProperty,
        IEventSymbol @event => @event.OverriddenEvent,
        _ => null,
    };

    private static void Land(ISymbol member, INamedTypeSymbol type, Dictionary<string, List<INamedTypeSymbol>> landings)
    {
        if (!member.Locations.Any(location => location.IsInSource)) return;
        string key = Key(member.OriginalDefinition);
        if (!landings.TryGetValue(key, out var types)) landings[key] = types = [];
        types.Add(type);
    }

    /// <summary>The symbol <paramref name="node"/> stands for, when it is a node that names or reaches a member.</summary>
    private static ISymbol? SymbolOf(SemanticModel model, SyntaxNode node)
    {
        switch (node)
        {
            case SimpleNameSyntax or ImplicitObjectCreationExpressionSyntax or BinaryExpressionSyntax or PrefixUnaryExpressionSyntax
                or ElementAccessExpressionSyntax or CastExpressionSyntax:
                var info = model.GetSymbolInfo(node);
                return info.Symbol ?? info.CandidateSymbols.FirstOrDefault();
            case ForEachStatementSyntax loop:
                return model.GetForEachStatementInfo(loop).GetEnumeratorMethod;
            case InterpolatedStringExpressionSyntax text:
                return model.GetTypeInfo(text).ConvertedType;
            case RecursivePatternSyntax or ParenthesizedVariableDesignationSyntax:
                return model.GetOperation(node) is IRecursivePatternOperation { DeconstructSymbol: { } deconstruct } ? deconstruct : null;
            case AssignmentExpressionSyntax { Left: DeclarationExpressionSyntax or TupleExpressionSyntax } deconstruction:
                return model.GetDeconstructionInfo(deconstruction).Method;
            default:
                return null;
        }
    }

    /// <summary>The type a use of <paramref name="symbol"/> uses: the type itself, or the type that declares the member.</summary>
    private static INamedTypeSymbol? OwnerOf(ISymbol symbol) => symbol switch
    {
        INamedTypeSymbol type => type,
        IMethodSymbol { ReducedFrom: { } extension } => extension.ContainingType,
        IAliasSymbol alias => alias.Target as INamedTypeSymbol,
        INamespaceSymbol or ITypeParameterSymbol or ILocalSymbol or IParameterSymbol or IRangeVariableSymbol or ILabelSymbol or IDiscardSymbol => null,
        _ => symbol.ContainingType,
    };

    /// <summary>Where a use is written: the innermost type around it, and the member of that type whose code it stands in.</summary>
    private readonly record struct Writer(INamedTypeSymbol Type, string Member);

    /// <summary>Notes that <paramref name="writer"/> uses <paramref name="used"/>, unless it is no type of the library or the use is
    /// written inside the type itself.</summary>
    private static void Use(INamedTypeSymbol? used, Writer writer, Dictionary<string, SortedSet<string>> users,
        Dictionary<string, SortedSet<string>> readers)
    {
        if (used is null) return;
        used = Declared(used);
        if (!used.Locations.Any(location => location.IsInSource)) return;
        for (var around = writer.Type; around is not null; around = around.ContainingType)
        {
            if (SymbolEqualityComparer.Default.Equals(Declared(around), used)) return;
        }

        string key = Key(used);
        if (!users.TryGetValue(key, out var set)) users[key] = set = new(StringComparer.Ordinal);
        set.Add(Name(Outermost(Declared(writer.Type))));
        if (!readers.TryGetValue(key, out var read)) readers[key] = read = new(StringComparer.Ordinal);
        read.Add(writer.Member);
    }

    /// <summary>The users of <paramref name="type"/>, and the users of every base and interface of the library it derives from.</summary>
    private static IEnumerable<string> UsersOf(INamedTypeSymbol type, Dictionary<string, SortedSet<string>> users)
    {
        var all = new SortedSet<string>(users.TryGetValue(Key(type), out var own) ? own : [], StringComparer.Ordinal);
        foreach (var family in Bases(type))
        {
            if (users.TryGetValue(Key(family), out var shared)) all.UnionWith(shared);
        }

        all.Remove(Name(type));
        return all;
    }

    private static IEnumerable<INamedTypeSymbol> Bases(INamedTypeSymbol type)
    {
        for (var below = type.BaseType; below is not null; below = below.BaseType)
        {
            if (below.Locations.Any(location => location.IsInSource)) yield return below.OriginalDefinition;
        }

        foreach (var contract in type.AllInterfaces.Where(contract => contract.Locations.Any(location => location.IsInSource)))
        {
            yield return contract.OriginalDefinition;
        }
    }

    /// <summary>The type <paramref name="type"/> stands for: its definition, and the static class an extension block is written in.</summary>
    private static INamedTypeSymbol Declared(INamedTypeSymbol type)
    {
        type = type.OriginalDefinition;
        while (type.IsExtension && type.ContainingType is not null) type = type.ContainingType.OriginalDefinition;
        return type;
    }

    private static INamedTypeSymbol Outermost(INamedTypeSymbol type)
    {
        while (type.ContainingType is not null) type = type.ContainingType;
        return type;
    }

    /// <summary>Whether no code outside the assembly sees <paramref name="type"/>: an internal type, or a private one nested in another.</summary>
    private static bool IsNotPublic(INamedTypeSymbol type)
    {
        for (var around = type; around is not null; around = around.ContainingType)
        {
            if (around.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Protected or Accessibility.ProtectedOrInternal)) return true;
        }

        return false;
    }

    private static string Key(ISymbol symbol) => symbol.GetDocumentationCommentId() ?? symbol.ToDisplayString();

    private static string Name(INamedTypeSymbol type) => type.ToDisplayString();

    private static string Where(INamedTypeSymbol type) =>
        type.Locations.FirstOrDefault(location => location.IsInSource) is { } location
            ? $"{location.SourceTree!.FilePath}:{location.GetLineSpan().StartLinePosition.Line + 1}"
            : string.Empty;
}
