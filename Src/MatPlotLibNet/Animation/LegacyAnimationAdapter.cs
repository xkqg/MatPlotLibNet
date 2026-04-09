// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Animation;

/// <summary>Adapts the existing <see cref="AnimationBuilder"/> to the generic <see cref="IAnimation{TState}"/> interface.</summary>
public sealed class LegacyAnimationAdapter : IAnimation<int>
{
    private readonly AnimationBuilder _builder;

    public LegacyAnimationAdapter(AnimationBuilder builder) => _builder = builder;

    public int FrameCount => _builder.FrameCount;
    public TimeSpan Interval { get => _builder.Interval; set => _builder.Interval = value; }
    public bool Loop { get => _builder.Loop; set => _builder.Loop = value; }

    public int CreateInitialState() => 0;
    public int Advance(int currentState, int frameIndex) => frameIndex;
    public Figure GenerateFrame(int state, int frameIndex) => _builder.GenerateFrame(frameIndex);
}
