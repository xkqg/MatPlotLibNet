// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Xunit;

namespace MatPlotLibNet.Tests.Diagnostics;

/// <summary>
/// Serializes every test that subscribes to the process-global
/// <see cref="MatPlotLibNet.Diagnostics.ChartDiagnostics.Emitted"/> event. Because the event is a
/// single static, process-wide delegate, two tests that concurrently subscribe/emit could observe
/// each other's diagnostics (or race on add/remove ordering), producing a non-deterministic,
/// environment-dependent test failure. Tagging every ChartDiagnostics subscriber/emitter test class
/// into this <see cref="CollectionDefinitionAttribute.DisableParallelization"/> collection makes them
/// run one at a time. (Mirrors the sibling <c>ChartSerializerGlobalState</c> collection for the
/// SeriesRegistry statics.)
/// </summary>
[CollectionDefinition("ChartDiagnosticsGlobalState", DisableParallelization = true)]
public sealed class ChartDiagnosticsGlobalStateCollection
{
}
