// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace MatPlotLibNet.Tests;

/// <summary>What a published count counts: the public classes of the package that can be made (no abstract base, no interface) and derive
/// from the root the count is named for. The series count and the indicator count both read the package this way, so a count and the
/// documents that state it say the same thing by one rule.</summary>
internal static class ConcreteSubtypesExtensions
{
    extension(Assembly assembly)
    {
        /// <summary>Every visible (public, at any nesting), non-abstract, non-generic class of the assembly that derives from
        /// <paramref name="root"/>, by name. A root that is an open generic type (<c>Indicator&lt;&gt;</c>) is met by any class whose
        /// chain of bases holds a closed form of it.</summary>
        /// <param name="root">The base class or interface the classes derive from.</param>
        /// <returns>The classes, ordered by name (ordinal).</returns>
        public Type[] ConcretePublicSubtypesOf(Type root) =>
            [.. assembly.GetTypes()
                .Where(type => type is { IsClass: true, IsAbstract: false, IsVisible: true, IsGenericTypeDefinition: false } && type.DerivesFrom(root))
                .OrderBy(type => type.Name, StringComparer.Ordinal)];
    }

    extension(Type type)
    {
        private bool DerivesFrom(Type root)
        {
            if (!root.IsGenericTypeDefinition) return root.IsAssignableFrom(type);

            for (Type? baseType = type.BaseType; baseType is not null; baseType = baseType.BaseType)
            {
                if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == root) return true;
            }

            return false;
        }
    }
}
