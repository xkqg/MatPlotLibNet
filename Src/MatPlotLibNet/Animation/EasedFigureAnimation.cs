// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Animation;

/// <summary>An <see cref="IAnimation{TState}"/> whose state is the eased progress
/// <c>t ∈ [0, 1]</c>. The caller supplies a frame generator <c>Func&lt;double, Figure&gt;</c>
/// that receives the eased <c>t</c> and returns the figure for that moment.</summary>
public sealed class EasedFigureAnimation : IAnimation<double>
{
    private readonly int _frameCount;
    private readonly Func<double, Figure> _frameGenerator;
    private readonly EasingKind _easing;

    /// <summary>Creates an eased animation.</summary>
    /// <param name="frameCount">Total number of frames.</param>
    /// <param name="frameGenerator">Receives eased progress <c>t ∈ [0, 1]</c>, returns a figure.</param>
    /// <param name="easing">Easing curve applied to the linear frame progress.</param>
    /// <param name="intervalMs">Delay between frames in milliseconds.</param>
    /// <param name="loop">Whether the animation loops.</param>
    public EasedFigureAnimation(
        int frameCount,
        Func<double, Figure> frameGenerator,
        EasingKind easing = EasingKind.Linear,
        int intervalMs = 16,
        bool loop = false)
    {
        _frameCount = frameCount;
        _frameGenerator = frameGenerator;
        _easing = easing;
        Interval = TimeSpan.FromMilliseconds(intervalMs);
        Loop = loop;
    }

    /// <inheritdoc />
    public int FrameCount => _frameCount;
    /// <inheritdoc />
    public TimeSpan Interval { get; set; }
    /// <inheritdoc />
    public bool Loop { get; set; }

    /// <inheritdoc />
    public double CreateInitialState() => 0.0;

    /// <inheritdoc />
    public double Advance(double state, int frameIndex)
    {
        double t = _frameCount <= 1 ? 1.0 : (double)frameIndex / (_frameCount - 1);
        return EasingFunction.Apply(_easing, t);
    }

    /// <inheritdoc />
    public Figure GenerateFrame(double state, int frameIndex) => _frameGenerator(state);
}
