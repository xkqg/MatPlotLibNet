// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>Verifies that an application can implement, proxy or mock every public interface of the library: none carries a
/// member only this assembly can see. A non-public member on a public interface (abstract or with a body) makes
/// <see cref="DispatchProxy"/> and the mocking libraries built on run-time type generation fail to build the type, which worked
/// for these interfaces on 1.29.0; what the library's own types add to a public interface lives on an internal interface
/// beside it.</summary>
public class PublicInterfaceContractTests
{
    /// <summary>Every public interface of the core assembly whose members include one that is not public.</summary>
    [Fact]
    public void NoPublicInterface_CarriesAMemberOnlyTheLibraryCanSee()
    {
        Assert.Empty(typeof(Figure).Assembly.HiddenInterfaceMembers());
    }

    /// <summary>The interfaces a caller hands its own surface, renderer or writer through, proxied the way a test double is.</summary>
    [Fact]
    public void TheDrawingInterfaces_CanBeProxied()
    {
        Assert.NotNull(DispatchProxy.Create<IRenderContext, Silent>());
        Assert.NotNull(DispatchProxy.Create<IChartRenderer, Silent>());
        Assert.NotNull(DispatchProxy.Create<ISvgRenderer, Silent>());
        Assert.NotNull(DispatchProxy.Create<IHasLabelPlacement, Silent>());
    }

    /// <summary>A proxy that answers every call with nothing.</summary>
    public class Silent : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => null;
    }
}

/// <summary>What an assembly's public interfaces show of themselves.</summary>
internal static class PublicInterfaceAssemblyExtensions
{
    extension(Assembly assembly)
    {
        /// <summary>"Interface.Member" for every method (accessors included) of a public interface of the assembly that only
        /// the assembly, or a type deriving from the interface, can see. A private member is the interface's own and asks
        /// nothing of an implementer.</summary>
        internal IReadOnlyList<string> HiddenInterfaceMembers() =>
            [.. assembly.GetExportedTypes()
                .Where(t => t.IsInterface)
                .SelectMany(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                    .Where(m => !m.IsPrivate)
                    .Select(m => $"{t.FullName}.{m.Name}"))];
    }
}
