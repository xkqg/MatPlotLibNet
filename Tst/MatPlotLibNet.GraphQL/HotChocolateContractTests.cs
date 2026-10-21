// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace MatPlotLibNet.GraphQL.Tests;

/// <summary>
/// The GraphQL package holds HotChocolate on the major it was built against (see the comment on the reference in
/// the package's project file). That is only right while HotChocolate's types are part of the package's public
/// surface; this test keeps the premise visible, so the day the surface stops exposing them the pin shows up as
/// a reason that no longer holds instead of as a habit.
/// </summary>
public class HotChocolateContractTests
{
    private static IEnumerable<Type> TypesOnThePublicSurface() =>
        typeof(ChartEventSender).Assembly.GetExportedTypes()
            .SelectMany(type => type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .SelectMany(member => member switch
            {
                ConstructorInfo ctor => ctor.GetParameters().Select(p => p.ParameterType),
                MethodInfo method => method.GetParameters().Select(p => p.ParameterType).Append(method.ReturnType),
                PropertyInfo property => [property.PropertyType],
                FieldInfo field => [field.FieldType],
                _ => [],
            });

    /// <summary>Verifies that a HotChocolate type is in the public constructor of the event sender.</summary>
    [Fact]
    public void ThePublicSurface_ExposesHotChocolateTypes()
    {
        var exposed = TypesOnThePublicSurface()
            .Where(type => type.Namespace?.StartsWith("HotChocolate", StringComparison.Ordinal) == true)
            .Select(type => type.FullName)
            .Distinct()
            .ToArray();

        Assert.Contains("HotChocolate.Subscriptions.ITopicEventSender", exposed);
    }
}
