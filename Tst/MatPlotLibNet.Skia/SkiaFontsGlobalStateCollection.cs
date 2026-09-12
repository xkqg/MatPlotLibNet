// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Skia.Tests;

/// <summary>Serialises every test that touches the process-wide font registry or subscribes to the static
/// <c>ChartDiagnostics.Emitted</c> event. The registry is read by every render test in this assembly; a test that
/// registers, resets or observes it while a render test runs on another thread would make that render see a
/// registry mid-change. xUnit scopes collections per assembly, so the twin in the core test project does not reach
/// here — this is that guard for the Skia package.</summary>
[CollectionDefinition("SkiaFontsGlobalState", DisableParallelization = true)]
public sealed class SkiaFontsGlobalStateCollection
{
}
