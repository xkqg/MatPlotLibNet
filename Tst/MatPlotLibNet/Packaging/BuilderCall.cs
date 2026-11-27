// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>One method a caller writes on a builder: one the builder declares, or an extension member the library adds to it. Both
/// read the same at the call (<c>ax.SetXLabel("x")</c>), so the rules of a builder's methods (its pinned surface, the budget of
/// parameters, the shape of a container verb, one default at every layer) hold over both.</summary>
/// <param name="Method">An instance method of the builder, or the static method an extension block compiles to.</param>
public readonly record struct BuilderCall(MethodInfo Method)
{
    /// <summary>Whether the call is an extension member: its first parameter is the builder it is called on.</summary>
    public bool IsExtension => Method.IsStatic;

    /// <summary>The parameters the caller writes: the builder an extension member is called on is not one of them.</summary>
    public ParameterInfo[] Parameters => IsExtension ? Method.GetParameters()[1..] : Method.GetParameters();

    /// <summary>The call as the pinned surface writes it: the return type, the name and the parameters the caller writes, as
    /// reflection writes a method, after <c>Method</c>.</summary>
    public string Signature
    {
        get
        {
            string written = Method.ToString()!;
            if (!IsExtension) return $"Method {written}";
            string receiver = Method.GetParameters()[0].ParameterType.ToString();
            return "Method " + written.Replace($"({receiver}, ", "(", StringComparison.Ordinal).Replace($"({receiver})", "()", StringComparison.Ordinal);
        }
    }
}

/// <summary>The calls of a builder, found rather than listed.</summary>
public static class BuilderCallExtensions
{
    extension(Type builder)
    {
        /// <summary>Every public method a caller writes on the builder: the instance methods it declares (not an accessor) and the
        /// public extension members its assembly adds to it, the composed-chart verbs of <see cref="VerbFamilies"/> aside (their
        /// shape is <c>VerbPlacementContractTests</c>' to hold).</summary>
        public IReadOnlyList<BuilderCall> Calls() =>
        [
            .. builder.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName)
                .Select(method => new BuilderCall(method)),
            .. builder.Assembly.GetExportedTypes()
                .Where(type => type.IsAbstract && type.IsSealed && !VerbFamilies.All.Contains(type))
                .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
                .Where(method => !method.IsSpecialName && method.IsDefined(typeof(ExtensionAttribute), inherit: false)
                    && method.GetParameters()[0].ParameterType == builder)
                .Select(method => new BuilderCall(method)),
        ];
    }
}
