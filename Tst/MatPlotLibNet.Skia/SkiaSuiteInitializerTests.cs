// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Skia.Tests;

/// <summary>The Skia assembly is loaded, and its module initializer has run, before any test of the suite does anything: a test that
/// saves a file or measures a text no longer forces it by hand, depending on the order the tests run in.</summary>
public sealed class SkiaSuiteInitializerTests
{
    /// <summary>This test names no type of the Skia assembly, so it reaches Skia only through the route <c>Save</c> takes for a
    /// <c>.png</c>: the Skia exporter once the assembly is loaded, the refusal core seeds before.</summary>
    [Fact]
    public void APngSave_IsRoutedToSkia_WhenNoTestHasNamedAnythingOfSkia()
    {
        string path = Path.Combine(Path.GetTempPath(), $"s10_{Guid.NewGuid():N}.png");
        try
        {
            Plt.Create().Plot([0.0, 1.0], [1.0, 2.0]).Build().Save(path);

            Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, File.ReadAllBytes(path)[..4]);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }
}
