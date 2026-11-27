// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;
using MatPlotLibNet.Animation;
using MatPlotLibNet.Models;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet.Tests;

/// <summary>Every writer of a file follows one rule: the whole output is made first, and only then does it replace the file
/// that is there, so a save that fails leaves that file as it was, and a save over a longer file leaves nothing of it.</summary>
public sealed class FileReplaceTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("mpl-replace-").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private string Existing(string name, int bytes)
    {
        var path = Path.Combine(_dir, name);
        File.WriteAllBytes(path, Enumerable.Repeat((byte)'x', bytes).ToArray());
        return path;
    }

    private static Figure Simple() => Plt.Create().Plot([1.0, 2.0], [1.0, 2.0]).Build();

    [Fact]
    public void TransformResult_ToFile_ThatFails_LeavesTheFileThatWasThere()
    {
        var path = Existing("a.out", 64);

        Assert.Throws<InvalidOperationException>(() => Simple().Transform(new RecordingFigureTransform("partial", new InvalidOperationException("transform failed"))).ToFile(path));

        Assert.Equal(64, File.ReadAllBytes(path).Length);
    }

    [Fact]
    public void TransformResult_ToFile_OverALongerFile_WritesExactlyTheOutput()
    {
        var path = Existing("a.svg", 1 << 20);
        var result = Simple().Transform(new SvgTransform());

        result.ToFile(path);

        Assert.Equal(result.ToBytes(), File.ReadAllBytes(path));
    }

    [Fact]
    public void SaveStepThroughSvg_ThatFails_LeavesTheFileThatWasThere()
    {
        var path = Existing("anim.svg", 64);
        var animation = new AnimationBuilder(3, i => i < 2 ? Simple() : throw new InvalidOperationException("frame failed"));

        Assert.Throws<InvalidOperationException>(() => animation.SaveStepThroughSvg(path));

        Assert.Equal(64, File.ReadAllBytes(path).Length);
    }

    [Fact]
    public void SaveStepThroughSvg_OverALongerFile_WritesExactlyTheOutput()
    {
        var path = Existing("anim.svg", 1 << 20);
        var animation = new AnimationBuilder(2, _ => Simple());

        animation.SaveStepThroughSvg(path);

        Assert.Equal(animation.ToStepThroughSvg(), File.ReadAllText(path));
    }

    [Fact]
    public void SaveSvg_And_Save_OverALongerFile_WriteExactlyTheOutput()
    {
        var figure = Simple();
        var svgPath = Existing("f.svg", 1 << 20);
        var jsonPath = Existing("f.json", 1 << 20);
        var bare = Path.Combine(_dir, "bare");
        File.WriteAllBytes(bare + ".svg", new byte[1 << 20]);

        figure.SaveSvg(svgPath);
        figure.Save(jsonPath);
        figure.Save(bare);

        Assert.Equal(figure.ToSvg(), File.ReadAllText(svgPath));
        Assert.Equal(figure.ToJson(), File.ReadAllText(jsonPath));
        Assert.Equal(figure.ToSvg(), File.ReadAllText(bare + ".svg"));
        Assert.False(File.ReadAllBytes(svgPath).AsSpan().StartsWith(Encoding.UTF8.Preamble), "no byte order mark");
    }
}
