// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Animation;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Animation;

/// <summary>Whether an animation plays again when it ends, before anyone says so: the builder's frames loop, the eased animation's do not.</summary>
public class AnimationLoopTests
{
    public static TheoryData<Func<bool>, bool> LoopOfAFreshAnimation => new()
    {
        { () => new AnimationBuilder(5, i => new Figure()).Loop, true },
        { () => new EasedFigureAnimation(5, _ => new Figure()).Loop, false },
    };

    [Theory]
    [MemberData(nameof(LoopOfAFreshAnimation))]
    public void Loop_Defaults(Func<bool> loopOfAFreshAnimation, bool expected) => Assert.Equal(expected, loopOfAFreshAnimation());
}
