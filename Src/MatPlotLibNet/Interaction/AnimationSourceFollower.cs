// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Animation;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Interaction;

/// <summary>What a native chart control does with the <see cref="IAnimationSource"/> it is given, in one place: let go of the source
/// it had, keep the one it has now and listen to it, and move each frame, which arrives on the animation's own thread, to the
/// control's thread to be shown. A control hands it the two things that are its own: how it reaches its thread, and what showing a
/// figure is (setting it, and asking to be drawn again where setting it does not).
/// <para>Built the way <see cref="StreamingFigureFollower"/> is, for the same reason: the WPF, Avalonia, Uno and MAUI controls derive
/// from four different base classes and differ only in their thread and in how they show a figure.</para></summary>
internal sealed class AnimationSourceFollower
{
    private readonly UiThreadDispatch _post;
    private readonly Action<Figure> _show;

    /// <summary>Creates the follower of one control.</summary>
    /// <param name="post">How the control runs work on its UI thread.</param>
    /// <param name="show">What the control does with a frame on its UI thread.</param>
    internal AnimationSourceFollower(UiThreadDispatch post, Action<Figure> show)
    {
        _post = post;
        _show = show;
    }

    /// <summary>The source followed now, or <see langword="null"/>.</summary>
    internal IAnimationSource? Source { get; private set; }

    /// <summary>Follows <paramref name="source"/> and lets go of the one followed before; <see langword="null"/> only lets go.</summary>
    /// <param name="source">The source the control shows frames of now, or <see langword="null"/>.</param>
    internal void Follow(IAnimationSource? source)
    {
        if (Source is { } old) old.FrameReady -= OnFrameReady;
        Source = source;
        if (source is not null) source.FrameReady += OnFrameReady;
    }

    private void OnFrameReady(object? sender, Figure figure) => _post(() => _show(figure));
}
