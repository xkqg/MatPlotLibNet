// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>Where <c>Figure.Save</c> sends each extension once <c>MatPlotLibNet.Skia</c> is loaded: .png and .pdf to the
/// Skia exporters in place of the refusals core seeds, .gif nowhere, because an animation is saved from its builder and
/// Skia registers no figure transform for it.</summary>
public class SaveExtensionRoutingTests
{
    /// <summary>The module initializer runs when something of the assembly is first used; saving before that would hit the
    /// refusals core seeds, so the test forces it the way <c>ChartRendering</c> does.</summary>
    static SaveExtensionRoutingTests() =>
        System.Runtime.CompilerServices.RuntimeHelpers.RunModuleConstructor(typeof(FigureSkiaExtensions).Module.ModuleHandle);

    private static Figure Chart() =>
        Plt.Create().WithSize(400, 300).Plot([0.0, 1.0, 2.0], [1.0, 3.0, 2.0]).Build();

    private static string TempPath(string fileName) =>
        Path.Combine(Path.GetTempPath(), $"s10_{Guid.NewGuid():N}_{fileName}");

    [Fact]
    public void APngSave_WithSkiaLoaded_WritesAPng()
    {
        string path = TempPath("a.png");
        try
        {
            Chart().Save(path);

            Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, File.ReadAllBytes(path)[..4]);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public void APdfSave_WithSkiaLoaded_WritesAPdf()
    {
        string path = TempPath("a.pdf");
        try
        {
            Chart().Save(path);

            Assert.Equal("%PDF", Encoding.ASCII.GetString(File.ReadAllBytes(path)[..4]));
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public void AGifSave_WithSkiaLoaded_IsStillRefused_NamingTheAnimationPath()
    {
        string path = TempPath("a.gif");
        try
        {
            var ex = Assert.Throws<InvalidOperationException>(() => Chart().Save(path));

            Assert.Contains("SaveGif", ex.Message);
            Assert.False(File.Exists(path));
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }
}
