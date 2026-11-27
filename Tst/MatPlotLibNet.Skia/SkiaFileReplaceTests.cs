// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Animation;
using MatPlotLibNet.Models;
using MatPlotLibNet.Transforms;
using MatPlotLibNet.Transforms.Animation;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>The files the Skia package writes follow the library's one rule: the whole output is made first and then replaces
/// the file at the path, so nothing of a longer file is left after it, and a save that fails leaves that file as it was.</summary>
public sealed class SkiaFileReplaceTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("mpl-skia-replace-").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private string Existing(string name, int bytes)
    {
        var path = Path.Combine(_dir, name);
        File.WriteAllBytes(path, Enumerable.Repeat((byte)'x', bytes).ToArray());
        return path;
    }

    private static Figure Small(int i) => new FigureBuilder().WithSize(20, 20).Plot([0.0, 1.0], [0.0, i + 1.0]).Build();

    [Fact]
    public void SaveGif_OverALongerFile_LeavesNothingOfIt()
    {
        // File.OpenWrite does not truncate, so the old file's tail stayed after the end of the GIF.
        var path = Existing("anim.gif", 1 << 20);
        var animation = new AnimationBuilder(2, Small);

        animation.SaveGif(path);

        Assert.Equal(animation.ToGif(), File.ReadAllBytes(path));
    }

    [Fact]
    public void SaveGif_ThatFails_LeavesTheFileThatWasThere()
    {
        var path = Existing("anim.gif", 64);
        var animation = new AnimationBuilder(3, i => i < 2 ? Small(i) : throw new InvalidOperationException("frame failed"));

        Assert.Throws<InvalidOperationException>(() => animation.SaveGif(path));

        Assert.Equal(64, File.ReadAllBytes(path).Length);
    }

    [Fact]
    public void SaveFrames_OverLongerFiles_WritesExactlyEachFrame()
    {
        var first = Existing("frame_0001.png", 1 << 20);
        var animation = new AnimationBuilder(1, Small);

        animation.SaveFrames(_dir);

        Assert.Equal(Small(0).ToPng(), File.ReadAllBytes(first));
    }

    [Fact]
    public void TransformMultiPage_ThatFails_LeavesTheFileThatWasThere()
    {
        // The second page cannot be drawn (there is no figure for it), after the first was written.
        var path = Existing("pages.pdf", 64);

        Assert.Throws<NullReferenceException>(() => new PdfTransform().TransformMultiPage([Small(0), null!], path));

        Assert.Equal(64, File.ReadAllBytes(path).Length);
    }
}
