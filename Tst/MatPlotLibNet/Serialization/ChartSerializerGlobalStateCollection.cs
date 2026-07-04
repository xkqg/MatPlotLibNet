// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Xunit;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>
/// Serializes every test that touches the process-global <see cref="MatPlotLibNet.Serialization.SeriesRegistry"/>
/// factory table. <see cref="SeriesRegistryTests"/> temporarily overwrites and clears entries
/// (including the built-in <c>"line"</c> factory) via <c>Register</c> / <c>ResetForTests</c> to
/// verify the reset hook; if that mutation runs concurrently with any test that calls
/// <c>ChartSerializer.FromJson</c> — which dispatches through the very same backing
/// <c>System.Collections.Concurrent.ConcurrentDictionary</c> — the reader can observe the table
/// mid-mutation (e.g. <c>"line"</c> momentarily missing or rebound to a stub factory), producing
/// a non-deterministic, environment-dependent test failure. Tagging every SeriesRegistry
/// reader/writer test class into this <see cref="CollectionDefinitionAttribute.DisableParallelization"/>
/// collection makes them run one at a time — no reader ever observes a registry mid-mutation.
/// (Mirrors the sibling <c>ChartServicesGlobalState</c> collection for the ChartServices statics.)
/// </summary>
[CollectionDefinition("ChartSerializerGlobalState", DisableParallelization = true)]
public sealed class ChartSerializerGlobalStateCollection
{
}
