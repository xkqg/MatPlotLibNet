// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Animation;

/// <summary>Thread-safe animation playback controller.</summary>
/// <typeparam name="TState">The animation state type.</typeparam>
public sealed class AnimationController<TState> : IAsyncDisposable
{
    private readonly IAnimation<TState> _animation;
    private readonly Func<Figure, CancellationToken, Task> _publishFrame;
    private volatile AnimationPlaybackState _state = AnimationPlaybackState.Stopped;
    private CancellationTokenSource? _cts;

    /// <summary>Gets the current playback state.</summary>
    public AnimationPlaybackState State => _state;

    public int CurrentFrame { get; private set; }

    /// <summary>Creates a controller for the given animation with a frame publish callback.</summary>
    public AnimationController(IAnimation<TState> animation, Func<Figure, CancellationToken, Task> publishFrame)
    {
        _animation = animation;
        _publishFrame = publishFrame;
    }

    /// <summary>Plays the animation, pushing each frame through the publish callback. Returns when animation completes or is cancelled.</summary>
    public async Task PlayAsync(CancellationToken ct = default)
    {
        Stop();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _state = AnimationPlaybackState.Playing;
        var token = _cts.Token;

        try
        {
            do
            {
                var state = _animation.CreateInitialState();
                for (int i = 0; i < _animation.FrameCount; i++)
                {
                    token.ThrowIfCancellationRequested();
                    state = _animation.Advance(state, i);
                    var frame = _animation.GenerateFrame(state, i);
                    CurrentFrame = i;
                    await _publishFrame(frame, token).ConfigureAwait(false);
                    await Task.Delay(_animation.Interval, token).ConfigureAwait(false);
                }
            }
            while (_animation.Loop);
        }
        catch (OperationCanceledException) { }
        finally
        {
            _state = AnimationPlaybackState.Stopped;
        }
    }

    /// <summary>Stops playback by cancelling the internal token.</summary>
    public void Stop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _state = AnimationPlaybackState.Stopped;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        Stop();
        return default;
    }
}
