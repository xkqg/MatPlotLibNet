// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Animation;

/// <summary>Thread-safe animation playback controller.</summary>
/// <typeparam name="TState">The animation state type.</typeparam>
public sealed class AnimationController<TState> : IAsyncDisposable, IAnimationSource
{
    private readonly IAnimation<TState> _animation;
    private readonly Func<Figure, CancellationToken, Task> _publishFrame;
    private volatile AnimationPlaybackState _state = AnimationPlaybackState.Stopped;
    private CancellationTokenSource? _cts;
    private TaskCompletionSource? _pauseCompletion;
    private readonly object _pauseLock = new();

    /// <summary>Gets the current playback state.</summary>
    public AnimationPlaybackState State => _state;

    public int CurrentFrame { get; private set; }

    /// <summary>Fired after each frame is published. Subscribe to drive UI redraws.</summary>
    public event EventHandler<Figure>? FrameReady;

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
                    await WaitIfPausedAsync(token).ConfigureAwait(false);
                    token.ThrowIfCancellationRequested();
                    state = _animation.Advance(state, i);
                    var frame = _animation.GenerateFrame(state, i);
                    CurrentFrame = i;
                    await _publishFrame(frame, token).ConfigureAwait(false);
                    FrameReady?.Invoke(this, frame);
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

    /// <summary>Pauses playback. No-op if not currently playing.</summary>
    public void Pause()
    {
        lock (_pauseLock)
        {
            if (_state != AnimationPlaybackState.Playing) return;
            _state = AnimationPlaybackState.Paused;
            _pauseCompletion ??= new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }

    /// <summary>Resumes playback after a pause. No-op if not currently paused.</summary>
    public void Resume()
    {
        TaskCompletionSource? tcs;
        lock (_pauseLock)
        {
            if (_state != AnimationPlaybackState.Paused) return;
            _state = AnimationPlaybackState.Playing;
            tcs = _pauseCompletion;
            _pauseCompletion = null;
        }
        tcs?.TrySetResult();
    }

    /// <summary>Stops playback by cancelling the internal token.</summary>
    public void Stop()
    {
        TaskCompletionSource? tcs;
        lock (_pauseLock)
        {
            tcs = _pauseCompletion;
            _pauseCompletion = null;
        }
        tcs?.TrySetCanceled();
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

    private Task WaitIfPausedAsync(CancellationToken token)
    {
        TaskCompletionSource? tcs;
        lock (_pauseLock) tcs = _pauseCompletion;
        return tcs is null ? Task.CompletedTask : tcs.Task.WaitAsync(token);
    }
}
