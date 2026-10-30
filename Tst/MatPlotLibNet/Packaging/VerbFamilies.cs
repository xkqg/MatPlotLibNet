// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>The classes that hold the library's composed-chart verbs, found rather than listed. Three test files used to
/// keep the same list of four classes by hand, so a fifth family had to be added in all three or it escaped two of them. The
/// Mcp test project links this very file.</summary>
public static class VerbFamilies
{
    /// <summary>Every public static class of the library that holds at least one public extension method on the axes builder,
    /// ordered by name.</summary>
    public static IReadOnlyList<Type> All { get; } =
    [
        .. typeof(AxesBuilder).Assembly.GetExportedTypes()
            .Where(type => type.IsAbstract && type.IsSealed && ExtensionsOf(type).Any(method => method.GetParameters()[0].ParameterType == typeof(AxesBuilder)))
            .OrderBy(type => type.Name, StringComparer.Ordinal),
    ];

    private static IEnumerable<MethodInfo> ExtensionsOf(Type type) =>
        type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => method.IsDefined(typeof(ExtensionAttribute), inherit: false));

    /// <summary>The verbs a family declares: its public extension methods, ordered by name.</summary>
    /// <param name="family">A class of <see cref="All"/>.</param>
    public static MethodInfo[] VerbsOf(Type family) => [.. ExtensionsOf(family).OrderBy(method => method.Name, StringComparer.Ordinal)];

    /// <summary>The verbs of every family.</summary>
    public static MethodInfo[] AllVerbs => [.. All.SelectMany(VerbsOf)];

    /// <summary>The types the verbs take or hand to their callbacks that belong to this library and are not series: the named
    /// data (a group, a sample, the limits of a chart) and the parts of a chart. Found through the parameters, so a type a
    /// verb starts taking tomorrow is found without an edit.</summary>
    public static Type[] TypesTheVerbsIntroduce()
    {
        static IEnumerable<Type> Unwrapped(Type type)
        {
            yield return type;
            if (type.IsArray)
            {
                foreach (var inner in Unwrapped(type.GetElementType()!)) yield return inner;
            }

            foreach (var argument in type.IsGenericType ? type.GetGenericArguments() : [])
            {
                foreach (var inner in Unwrapped(argument)) yield return inner;
            }
        }

        return
        [.. AllVerbs
            .SelectMany(verb => verb.GetParameters().Skip(1))
            .SelectMany(parameter => Unwrapped(parameter.ParameterType))
            .Where(type => type.IsValueType && !type.IsEnum && type.Assembly == typeof(AxesBuilder).Assembly)
            .Distinct()
            .OrderBy(type => type.Name, StringComparer.Ordinal)];
    }
}
