// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;
using MatPlotLibNet.Models;
using MatPlotLibNet.Transforms;
using MatPlotLibNet.Transforms.Animation;

namespace MatPlotLibNet.Tests.Animation;

/// <summary>An animation is written by one contract, <see cref="IAnimationTransform"/>, which lives where the frames are made: the
/// SVG that steps through its frames is written by it in the core, and the GIF in the Skia package.</summary>
public class AnimationTransformContractTests
{
    private static Figure Frame(int i) => Plt.Create().WithSize(40, 30).Plot([0.0, 1.0], [0.0, i + 1.0]).Build();

    [Fact]
    public void TheAnimationContract_LivesInTheCore()
    {
        Assert.Same(typeof(Plt).Assembly, typeof(IAnimationTransform).Assembly);
    }

    [Fact]
    public void AStepThroughSvg_IsWrittenThroughTheAnimationContract_AsItIsWrittenDirectly()
    {
        IAnimationTransform transform = new StepThroughSvgTransform();
        using var throughTheContract = new MemoryStream();
        using var direct = new MemoryStream();

        transform.Transform([Frame(0), Frame(1)], TimeSpan.FromMilliseconds(80), loop: false, throughTheContract);
        new StepThroughSvgTransform().Transform([Frame(0), Frame(1)], TimeSpan.FromMilliseconds(80), loop: false, direct);

        Assert.Equal(Encoding.UTF8.GetString(direct.ToArray()), Encoding.UTF8.GetString(throughTheContract.ToArray()));
    }
}
