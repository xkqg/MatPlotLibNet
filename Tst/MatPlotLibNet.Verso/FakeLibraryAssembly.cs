// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;

namespace MatPlotLibNet.Verso.Tests;

/// <summary>A second assembly that calls itself MatPlotLibNet, built while the test runs.</summary>
/// <remarks>
/// This is the situation the formatter exists for, made reproducible. Verso loads an extension in its own
/// collectible load context, so the <c>Figure</c> a cell makes and a <c>Figure</c> shipped beside the extension
/// would be two CLR types with one name. The formatter therefore matches by name and looks for the rendering
/// verb on the value's own assembly — and the only way to show that it really does is to hand it a value from an
/// assembly the charting library has never seen. It is also the only way to reach what a chart that misbehaves
/// does to a cell, because the real renderer cannot be made to answer with nothing.
/// </remarks>
internal static class FakeLibraryAssembly
{
    /// <summary>A value of <paramref name="fullName"/> whose type has no rendering verb at all.</summary>
    internal static object ValueWithNoRenderer(string fullName) =>
        Build(fullName, renderer: null);

    /// <summary>A value of <paramref name="fullName"/> whose <c>ToSvg()</c> answers <paramref name="svg"/>.</summary>
    /// <param name="fullName">The type name to claim, e.g. <c>MatPlotLibNet.FigureBuilder</c>.</param>
    /// <param name="svg">What it renders — <see langword="null"/> for a renderer that answers with nothing.</param>
    internal static object ValueRendering(string fullName, string? svg) =>
        Build(fullName, renderer: il =>
        {
            if (svg is null)
            {
                il.Emit(OpCodes.Ldnull);
            }
            else
            {
                il.Emit(OpCodes.Ldstr, svg);
            }

            il.Emit(OpCodes.Ret);
        });

    private static object Build(string fullName, Action<ILGenerator>? renderer)
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("MatPlotLibNet"), AssemblyBuilderAccess.Run);
        var type = assembly.DefineDynamicModule("MatPlotLibNet")
                           .DefineType(fullName, TypeAttributes.Public | TypeAttributes.Class);

        renderer?.Invoke(type.DefineMethod("ToSvg", MethodAttributes.Public, typeof(string), Type.EmptyTypes)
                             .GetILGenerator());

        return Activator.CreateInstance(type.CreateType())!;
    }
}
