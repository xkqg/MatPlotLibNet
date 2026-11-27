// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace MatPlotLibNet.AspNetCore.Tests;

/// <summary>Verifies that an application can implement, proxy or mock every public interface of the server package: none carries
/// a member only this assembly can see. What the library's publisher adds lives on an internal interface beside it
/// (<see cref="IKeepingChartPublisher"/>).</summary>
public class PublicInterfaceContractTests
{
    [Fact]
    public void NoPublicInterface_CarriesAMemberOnlyThePackageCanSee()
    {
        var hidden = typeof(IChartPublisher).Assembly.GetExportedTypes()
            .Where(t => t.IsInterface)
            .SelectMany(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsPrivate)
                .Select(m => $"{t.FullName}.{m.Name}"));

        Assert.Empty(hidden);
    }

    /// <summary>The publisher an application hands the hub, proxied the way a test double is.</summary>
    [Fact]
    public void ThePublisher_CanBeProxied()
    {
        Assert.NotNull(DispatchProxy.Create<IChartPublisher, Silent>());
    }

    /// <summary>A proxy that answers every call with nothing.</summary>
    public class Silent : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => null;
    }
}
