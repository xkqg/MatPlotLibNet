// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Transforms.Animation;

namespace MatPlotLibNet.Skia.Tests.Animation;

/// <summary>Branch coverage for <see cref="FuncAnimation"/>.
/// Covers SaveGif (stream output) and SaveFrames (directory output).</summary>
public class FuncAnimationTests
{
    private static FuncAnimation TwoFrameAnim() =>
        new(2, i => new FigureBuilder()
            .WithSize(20, 20)
            .Plot([0.0, 1.0], [0.0, (double)(i + 1)])
            .Build());

    [Fact]
    public void SaveGif_WritesNonEmptyGifStream()
    {
        var anim = TwoFrameAnim();
        using var ms = new MemoryStream();
        anim.SaveGif(ms);
        Assert.True(ms.Length > 6);
        ms.Position = 0;
        // GIF89a magic bytes
        Assert.Equal(0x47, ms.ReadByte()); // G
        Assert.Equal(0x49, ms.ReadByte()); // I
        Assert.Equal(0x46, ms.ReadByte()); // F
    }

    [Fact]
    public void Save_ToFilePath_WritesGifFile()
    {
        var anim = TwoFrameAnim();
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".gif");
        try
        {
            anim.Save(path);
            Assert.True(File.Exists(path));
            Assert.True(new FileInfo(path).Length > 6);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void SaveFrames_WritesOnePngPerFrame()
    {
        var anim = TwoFrameAnim();
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            anim.SaveFrames(dir, "frm");
            var files = Directory.GetFiles(dir, "frm_*.png");
            Assert.Equal(2, files.Length);
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }
}
