// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>Loads the Skia assembly when the suite loads, so its module initializer has registered the exporters and the glyph
/// outlines before the first test runs. <c>Figure.Save</c> routes <c>.png</c> and <c>.pdf</c> to the Skia transforms only after that, and
/// that happens when something of the assembly is first used; a test that saved a file before any other Skia call would write the
/// refusal core seeds, depending on the order the tests run in.</summary>
internal static class SkiaSuiteInitializer
{
    [ModuleInitializer]
    internal static void LoadSkia() =>
        RuntimeHelpers.RunModuleConstructor(typeof(FigureSkiaExtensions).Module.ModuleHandle);
}
