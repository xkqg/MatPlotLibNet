// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;

namespace MatPlotLibNet.Wiki.Tests;

/// <summary>
/// What the packages as built offer a caller: the public types by the name a document writes, and for each the members a call can
/// reach on it, which are the ones it declares, the ones it inherits and the ones an extension adds. A document that names a member
/// under a type is read against this.
/// </summary>
internal sealed class PackageSurface
{
    private const BindingFlags Reachable = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;

    private const BindingFlags Anywhere = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    /// <summary>The packages this suite loads: every assembly of the library it runs with.</summary>
    public static PackageSurface Loaded { get; } = new(LibraryAssemblies());

    /// <summary>The assemblies of the packages, each beside the XML documentation the build wrote for it.</summary>
    public IReadOnlyList<Assembly> Assemblies { get; }

    /// <summary>Every type of the packages, nested and internal ones included.</summary>
    public IReadOnlyList<Type> Types => _types;

    private readonly ILookup<string, Type> _byName;

    private readonly MethodInfo[] _extensions;

    private readonly ExtensionBlock[] _blocks;

    private readonly Lazy<ILookup<string, Type>> _having;

    private readonly Type[] _types;

    private PackageSurface(Assembly[] assemblies)
    {
        Assemblies = assemblies;
        var types = assemblies.SelectMany(PublicTypesOf).ToArray();
        _types = types;

        _byName = types.ToLookup(NameOf, StringComparer.Ordinal);

        var statics = types.Where(type => type is { IsAbstract: true, IsSealed: true }).ToArray();

        // An extension written with `this`: a static method whose first parameter is the receiver.
        _extensions =
        [
            .. statics.SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                .Where(method => method.IsDefined(typeof(ExtensionAttribute), inherit: false)),
        ];

        // An extension block of C# 14: the compiler writes it as a nested type with a bracketed name that holds the block's members, and
        // inside it a marker type whose <Extension>$ method has the receiver as its parameter.
        _blocks =
        [
            .. statics.SelectMany(type => type.GetNestedTypes(Anywhere))
                .Where(block => block.Name.StartsWith('<') && block.Name.Contains("G>$", StringComparison.Ordinal))
                .Select(block => new ExtensionBlock(
                    block.GetNestedTypes(Anywhere).SelectMany(marker => marker.GetMethods(Anywhere | BindingFlags.DeclaredOnly))
                        .FirstOrDefault(method => method.Name.StartsWith('<') && method.Name.Contains("Extension>$", StringComparison.Ordinal))?.GetParameters().FirstOrDefault()?.ParameterType,
                    block))
                .Where(block => block.Receiver is not null),
        ];

        _having = new(() => HavingIndex(types));
        Namespaces = [.. types.Select(type => type.Namespace).OfType<string>().Distinct().Order(StringComparer.Ordinal)];
    }

    /// <summary>The namespaces the types of the packages stand in, in order: the usings a call of a document is compiled with.</summary>
    public IReadOnlyList<string> Namespaces { get; }

    /// <summary>The types a value of which reaches a member of the name: those that declare it, and the receiver of every extension that
    /// adds it (a type that inherits it is reached through the one that declares it). A call written on a value whose type the document
    /// leaves unsaid (<c>table.ToHtml()</c>) is tried on these.</summary>
    /// <param name="member">The member's name.</param>
    /// <returns>The types, closed ones only: an extension of an open generic receiver names no type a value has.</returns>
    public IReadOnlyList<Type> TypesHaving(string member) => [.. _having.Value[member]];

    /// <summary>Whether a static class of the packages holds an extension of the name: a method written with <c>this</c>, or a member of
    /// one of its extension blocks.</summary>
    /// <param name="holder">The static class's name.</param>
    /// <param name="member">The member's name.</param>
    /// <returns>True when the class extends a type with the member.</returns>
    public bool IsExtensionIn(string holder, string member) =>
        _extensions.Any(method => method.DeclaringType?.Name == holder && MemberName(method.Name) == member)
        || _blocks.Any(block => block.Holder.DeclaringType?.Name == holder && block.Holder.GetMembers(Anywhere | BindingFlags.DeclaredOnly).Any(each => MemberName(each.Name) == member));

    /// <summary>The types of the packages that reach a member of the name, the ones that inherit it from a type of .NET included (a hub of
    /// the packages has the <c>Clients</c> of SignalR's hub): a name in capitals a document reads without a receiver is a member of one.</summary>
    /// <param name="member">The member's name.</param>
    /// <returns>The types that declare it first, then the types that inherit it.</returns>
    public IReadOnlyList<Type> TypesReaching(string member) =>
        _reaching.GetOrAdd(member, name => [.. TypesHaving(name).Concat(_types.Where(type => !type.ContainsGenericParameters && type.GetMember(name, Anywhere | BindingFlags.FlattenHierarchy).Length > 0)).Distinct()]);

    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, IReadOnlyList<Type>> _reaching = new(StringComparer.Ordinal);

    private ILookup<string, Type> HavingIndex(Type[] types) =>
        types.SelectMany(type => type.GetMembers(Anywhere | BindingFlags.DeclaredOnly).Select(each => (Name: MemberName(each.Name), Type: (Type?)type)))
            .Concat(_extensions.SelectMany(method => ReceiversOf(method.GetParameters().FirstOrDefault()?.ParameterType).Select(receiver => (Name: MemberName(method.Name), Type: (Type?)receiver))))
            .Concat(_blocks.SelectMany(block => block.Holder.GetMembers(Anywhere | BindingFlags.DeclaredOnly).Select(each => (Name: MemberName(each.Name), Type: block.Receiver))))
            .Where(each => each.Type is { ContainsGenericParameters: false, IsByRef: false } && !each.Name.StartsWith('<'))
            .Distinct()
            .ToLookup(each => each.Name, each => each.Type!, StringComparer.Ordinal);

    // The types a value of which an extension takes: its receiver, or for a generic receiver (`TControl`) each type its constraints name.
    private static IEnumerable<Type> ReceiversOf(Type? receiver) =>
        receiver is null ? [] : receiver.IsGenericParameter ? receiver.GetGenericParameterConstraints().Where(each => !each.ContainsGenericParameters) : [receiver];

    // A property's accessors are called get_ and set_ after it; the name a call writes is the property's.
    private static string MemberName(string name) => name.StartsWith("get_", StringComparison.Ordinal) || name.StartsWith("set_", StringComparison.Ordinal) ? name[4..] : name;

    /// <summary>Whether a name is a type of the packages: one the packages as built have, or one the source of a package this suite cannot
    /// build declares (<see cref="UnbuiltPackages"/>).</summary>
    /// <param name="name">The name as a document writes it.</param>
    /// <returns>True for a type of any package.</returns>
    public bool IsType(string name) => _byName.Contains(name) || UnbuiltPackages.Sources.Declares(name);

    /// <summary>The public types that go by a name, whatever their generic arity and whatever namespace or type they stand in.</summary>
    /// <param name="name">The name as a document writes it, without type arguments.</param>
    /// <returns>The types; none when no package has one of the name.</returns>
    public IReadOnlyList<Type> TypesNamed(string name) => [.. _byName[name]];

    /// <summary>The public types whose name ends with a word: every builder of the packages for <c>Builder</c>.</summary>
    /// <param name="suffix">The end of the name.</param>
    /// <returns>The types; none when no name ends so.</returns>
    public IReadOnlyList<Type> TypesEndingWith(string suffix) => [.. _byName.Where(group => group.Key.EndsWith(suffix, StringComparison.Ordinal)).SelectMany(group => group)];

    /// <summary>Whether a call can reach the member on one of the types: declared by it, inherited by it, or an extension of it.</summary>
    /// <param name="types">The types that go by one name.</param>
    /// <param name="member">The member's name.</param>
    /// <returns>True when one of the types has the member.</returns>
    public bool Reaches(IReadOnlyList<Type> types, string member) => types.Any(type => Members(type, member).Any());

    /// <summary>The types a member gives: its type for a property or a field, what each overload returns for a method.</summary>
    /// <param name="types">The types that go by one name.</param>
    /// <param name="member">The member's name.</param>
    /// <returns>The types of the packages the member gives; none when it gives a type no package declares (a generic parameter, a framework type).</returns>
    public IReadOnlyList<Type> Gives(IReadOnlyList<Type> types, string member) =>
    [
        .. types.SelectMany(type => Members(type, member))
            .Select(each => each switch
            {
                PropertyInfo property => property.PropertyType,
                FieldInfo field => field.FieldType,
                MethodInfo method => method.ReturnType,
                Type nested => nested,
                _ => null,
            })
            .OfType<Type>()
            .Where(type => _byName.Contains(NameOf(type)))
            .DistinctBy(type => type.FullName),
    ];

    // What a call can reach by the name: what the type declares or inherits, whatever its access, and what an extension adds, written with
    // `this` or in an extension block. An extension property is a pair of accessors called get_ and set_. A member a type implements
    // explicitly is the member of its interface.
    private IEnumerable<MemberInfo> Members(Type type, string member)
    {
        var holders = type.GetInterfaces().Prepend(type)
            .SelectMany(each => each.GetNestedTypes(Anywhere).Where(nested => nested.Name.StartsWith('<')).Prepend(each));
        string[] names = [member, "get_" + member];

        return holders.SelectMany(each => names.SelectMany(name => each.GetMember(name, Reachable | BindingFlags.NonPublic)))
            .Concat(_extensions.Where(method => names.Contains(method.Name) && Receives(method.GetParameters().FirstOrDefault()?.ParameterType, type)))
            .Concat(_blocks.Where(block => Receives(block.Receiver, type)).SelectMany(block => names.SelectMany(name => block.Holder.GetMember(name, Anywhere))));
    }

    // Whether a value of the type can be the receiver of an extension whose first parameter has the given type.
    private static bool Receives(Type? receiver, Type type)
    {
        if (receiver is null)
        {
            return false;
        }

        if (receiver.IsByRef)
        {
            receiver = receiver.GetElementType()!;
        }

        if (receiver.IsGenericParameter)
        {
            return receiver.GetGenericParameterConstraints().All(constraint => Receives(constraint, type));
        }

        if (receiver.IsAssignableFrom(type))
        {
            return true;
        }

        var definition = receiver.IsGenericType ? receiver.GetGenericTypeDefinition() : null;

        return definition is not null
            && type.GetInterfaces().Concat(BaseTypesOf(type)).Prepend(type).Any(each => each.IsGenericType && each.GetGenericTypeDefinition() == definition);
    }

    private static IEnumerable<Type> BaseTypesOf(Type type)
    {
        for (var each = type.BaseType; each is not null; each = each.BaseType)
        {
            yield return each;
        }
    }

    private static string NameOf(Type type)
    {
        var name = type.Name;
        var tick = name.IndexOf('`', StringComparison.Ordinal);

        return tick < 0 ? name : name[..tick];
    }

    // The types of an assembly, nested and internal ones included, whatever a missing dependency hides of the rest.
    private static IEnumerable<Type> PublicTypesOf(Assembly assembly)
    {
        Type?[] types;

        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException partial)
        {
            types = partial.Types;
        }

        return types.OfType<Type>();
    }

    /// <summary>Whether an assembly is one of the packages: the library's own, no suite and no sample program.</summary>
    /// <param name="assembly">The assembly's name, or its file's.</param>
    /// <returns>True for an assembly of the packages.</returns>
    public static bool IsPackageAssembly(string assembly) =>
        assembly.StartsWith("MatPlotLibNet", StringComparison.Ordinal) && !assembly.Contains("Tests", StringComparison.Ordinal) && !assembly.Contains("Samples", StringComparison.Ordinal);

    // Every assembly this suite runs with that is the library's own: the packages it compiles the wiki against.
    private static Assembly[] LibraryAssemblies() =>
    [
        .. ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Where(path => IsPackageAssembly(Path.GetFileName(path)))
            .Select(Assembly.LoadFrom),
    ];

    /// <summary>An extension block of C# 14: the type it extends, and the nested type the compiler keeps its members in.</summary>
    /// <param name="Receiver">The type the block extends.</param>
    /// <param name="Holder">The type that holds the members of the block.</param>
    private readonly record struct ExtensionBlock(Type? Receiver, Type Holder);
}
