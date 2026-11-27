// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using Xunit.Sdk;
using Xunit.v3;

namespace MatPlotLibNet.Fidelity;

/// <summary>The rows of a fidelity theory: the classic style and the v2 style of matplotlib, and the <c>Fidelity</c> category trait.
/// A fidelity test takes the theme id as its one parameter and draws the figure with <c>FidelityFigure</c>.</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class FidelityRowsAttribute : DataAttribute, ITraitAttribute
{
    /// <inheritdoc />
    public override ValueTask<IReadOnlyCollection<ITheoryDataRow>> GetData(MethodInfo testMethod, DisposalTracker disposalTracker) =>
        new([new TheoryDataRow("classic"), new TheoryDataRow("v2")]);

    /// <inheritdoc />
    public override bool SupportsDiscoveryEnumeration() => true;

    /// <inheritdoc />
    public IReadOnlyCollection<KeyValuePair<string, string>> GetTraits() => [new("Category", "Fidelity")];
}
