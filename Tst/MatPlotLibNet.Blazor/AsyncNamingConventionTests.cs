// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;

namespace MatPlotLibNet.Blazor.Tests;

/// <summary>Enforces the project-wide convention: every async method must have a name
/// ending in "Async". Detected via <see cref="AsyncStateMachineAttribute"/> which the
/// compiler attaches to every async state-machine method.</summary>
public class AsyncNamingConventionTests
{
    [Fact]
    public void AllAsyncMethods_InBlazorAssembly_HaveAsyncSuffix()
    {
        var assembly = typeof(ChartSubscriptionClient).Assembly;
        var violations = assembly.GetTypes()
            .SelectMany(t => t.GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.Static |
                BindingFlags.DeclaredOnly))
            .Where(m => m.GetCustomAttribute<AsyncStateMachineAttribute>() is not null)
            .Where(m => !m.Name.EndsWith("Async", StringComparison.Ordinal))
            .Select(m => $"{m.DeclaringType!.FullName}.{m.Name}")
            .ToList();

        Assert.Empty(violations);
    }
}
