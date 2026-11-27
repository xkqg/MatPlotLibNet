// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Animation;
using MatPlotLibNet.Models;
using MatPlotLibNet.Transforms.Animation;

namespace MatPlotLibNet.Skia.Tests.Animation;

/// <summary>One mechanism makes an animation, <see cref="AnimationBuilder"/>: it writes a GIF, an SVG that steps through its
/// frames, and one PNG per frame, and the
/// animation contract (<see cref="IAnimationTransform"/>) lives in the core, where a type that was compiled against this package
/// still finds it.</summary>
public class AnimationBuilderSkiaTests
{
    private static Figure Frame(int i) => new FigureBuilder().WithSize(20, 20).Plot([0.0, 1.0], [0.0, i + 1.0]).Build();

    private static string NewDirectory() => Path.Combine(Path.GetTempPath(), $"anim_{Guid.NewGuid():N}");

    [Fact]
    public void TheAnimationContract_IsForwardedToTheCore()
    {
        Assert.Contains(typeof(IAnimationTransform), typeof(GifTransform).Assembly.GetForwardedTypes());
        Assert.Same(typeof(Plt).Assembly, typeof(IAnimationTransform).Assembly);
    }

    [Fact]
    public void SaveFrames_WritesOnePngPerFrame_NamedByThePrefixAndTheFrameNumber()
    {
        var directory = NewDirectory();
        try
        {
            new AnimationBuilder(3, Frame).SaveFrames(directory, "step");

            Assert.Equal(["step_0001.png", "step_0002.png", "step_0003.png"],
                Directory.GetFiles(directory).Select(Path.GetFileName).Order(StringComparer.Ordinal));
            Assert.Equal(Frame(1).ToPng(), File.ReadAllBytes(Path.Combine(directory, "step_0002.png")));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void SaveFrames_WithoutAPrefix_NamesTheFramesFrame()
    {
        var directory = NewDirectory();
        try
        {
            new AnimationBuilder(1, Frame).SaveFrames(directory);

            Assert.True(File.Exists(Path.Combine(directory, "frame_0001.png")));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }
}
