// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Animation;

/// <summary>Adapts the existing <see cref="AnimationBuilder"/> to the generic <see cref="IAnimation{TState}"/> interface.</summary>
public sealed class LegacyAnimationAdapter : IAnimation<int>
{
    private readonly AnimationBuilder _builder;

    /// <summary>Initializes a new adapter wrapping the given <see cref="AnimationBuilder"/>.</summary>
    /// <param name="builder">The legacy animation builder to adapt.</param>
    public LegacyAnimationAdapter(AnimationBuilder builder) => _builder = builder;

    /// <summary>Gets the total number of frames in the animation.</summary>
    public int FrameCount => _builder.FrameCount;
    public TimeSpan Interval { get => _builder.Interval; set => _builder.Interval = value; }
    public bool Loop { get => _builder.Loop; set => _builder.Loop = value; }

    /// <inheritdoc />
    public int CreateInitialState() => 0;
    /// <inheritdoc />
    public int Advance(int currentState, int frameIndex) => frameIndex;
    /// <inheritdoc />
    public Figure GenerateFrame(int state, int frameIndex) => _builder.GenerateFrame(frameIndex);
}
