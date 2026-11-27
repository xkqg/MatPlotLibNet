// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests;

/// <summary>The public concrete classes of an assembly that derive from a root, by name: what a published count counts.</summary>
public sealed class ConcreteSubtypesExtensionsTests
{
    public abstract class Root;

    public abstract class Scaffolding : Root;

    public sealed class Zeta : Scaffolding;

    public sealed class Alpha : Root;

    internal sealed class Hidden : Root;

    public sealed class Unrelated;

    public sealed class Counted<T> : Root;

    public abstract class Holder<T>;

    public class Held : Holder<int>;

    public sealed class Holding : Held;

    /// <summary>The abstract scaffolding, the class that is not public, the class of another root and a generic definition (which cannot be made
    /// without its arguments) are left out; the rest come in name order.</summary>
    [Fact]
    public void TheClassesThatDeriveFromTheRoot_AreTheConcretePublicOnes_InNameOrder()
    {
        var found = typeof(ConcreteSubtypesExtensionsTests).Assembly.ConcretePublicSubtypesOf(typeof(Root))
            .Where(type => type.DeclaringType == typeof(ConcreteSubtypesExtensionsTests)).Select(type => type.Name).ToList();

        Assert.Equal(["Alpha", "Zeta"], found);
    }

    /// <summary>A root that is an open generic type is met through the closed form a class derives from, however far down its chain of bases.</summary>
    [Fact]
    public void ARootThatIsAnOpenGenericType_IsMetByAnyClassThatDerivesFromAClosedFormOfIt()
    {
        var found = typeof(ConcreteSubtypesExtensionsTests).Assembly.ConcretePublicSubtypesOf(typeof(Holder<>))
            .Where(type => type.DeclaringType == typeof(ConcreteSubtypesExtensionsTests)).Select(type => type.Name).ToList();

        Assert.Equal(["Held", "Holding"], found);
    }
}
