// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Geo.Tests;

/// <summary>This project's own copy of the core suite's <c>ChartSerializerGlobalStateCollection</c> — xUnit resolves a
/// collection definition only within the assembly that declares it. The series registry is process-global (every
/// <see cref="MatPlotLibNet.Serialization.ChartSerializer"/> dispatches through one table), and one test resets it, so every test
/// that reads a figure back through it joins this collection and none runs beside the reset.</summary>
[CollectionDefinition("ChartSerializerGlobalState", DisableParallelization = true)]
public sealed class ChartSerializerGlobalStateCollection
{
}
