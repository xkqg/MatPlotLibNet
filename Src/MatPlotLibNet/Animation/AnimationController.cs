// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Animation;

/// <summary>Thread-safe animation playback controller.</summary>
/// <typeparam name="TState">The animation state type.</typeparam>
/// <remarks>Each <see cref="PlayAsync"/> is a run of its own, with its own cancellation. Starting a play while another runs
/// ends the earlier one, and what the earlier run does as it winds down (a publish that did not listen to its token returns
/// late) never touches the run that replaced it: a run puts the controller back to <see cref="AnimationPlaybackState.Stopped"/>
/// only while it is still the current one.
/// <para>A pause is whole: once <see cref="Pause"/> has returned no frame is generated, published or announced until
/// <see cref="Resume"/>. A frame whose publish was under way when the pause landed finishes publishing, and its
/// <see cref="FrameReady"/> is raised at the resume (a stop drops it). A <see cref="FrameReady"/> handler that has already
/// started when the pause lands runs to its end, as any call that began before another returns does.</para></remarks>
public sealed class AnimationController<TState> : IAsyncDisposable, IAnimationSource
{
    private readonly IAnimation<TState> _animation;
    private readonly Func<Figure, CancellationToken, Task> _publishFrame;
    private volatile AnimationPlaybackState _state = AnimationPlaybackState.Stopped;
    private CancellationTokenSource? _current;
    private TaskCompletionSource? _pauseCompletion;
    private readonly object _pauseLock = new();

    /// <summary>Gets the current playback state.</summary>
    public AnimationPlaybackState State => _state;

    /// <summary>The index of the frame most recently published, counted from 0. It stays at its last value after playback
    /// stops, and is 0 before the first frame.</summary>
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
        var run = CancellationTokenSource.CreateLinkedTokenSource(ct);
        CancellationTokenSource? earlier;
        TaskCompletionSource? pause;
        lock (_pauseLock)
        {
            earlier = _current;
            pause = _pauseCompletion;
            _pauseCompletion = null;
            _current = run;
            _state = AnimationPlaybackState.Playing;
        }
        End(earlier, pause);
        var token = run.Token;

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

                    // A pause that landed while the frame was being published holds its notice until the resume: after Pause
                    // returns no new FrameReady begins. A notice whose gate was passed before the pause is already under way.
                    await WaitIfPausedAsync(token).ConfigureAwait(false);
                    token.ThrowIfCancellationRequested();
                    FrameReady?.Invoke(this, frame);
                    await Task.Delay(_animation.Interval, token).ConfigureAwait(false);
                }
            }
            while (_animation.Loop);
        }
        catch (OperationCanceledException) { }
        finally
        {
            bool stillCurrent;
            lock (_pauseLock)
            {
                stillCurrent = ReferenceEquals(_current, run);
                if (stillCurrent)
                {
                    _current = null;
                    _state = AnimationPlaybackState.Stopped;
                }
            }

            // Whoever takes a run out of the controller ends it; a run that was replaced or stopped was ended by that.
            if (stillCurrent) run.Dispose();
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
        CancellationTokenSource? run;
        TaskCompletionSource? pause;
        lock (_pauseLock)
        {
            run = _current;
            pause = _pauseCompletion;
            _current = null;
            _pauseCompletion = null;
            _state = AnimationPlaybackState.Stopped;
        }
        End(run, pause);
    }

    /// <summary>Ends a run taken out of the controller: releases a pause it waits on, cancels it and lets go of its token.</summary>
    private static void End(CancellationTokenSource? run, TaskCompletionSource? pause)
    {
        pause?.TrySetCanceled();
        if (run is null) return;
        run.Cancel();
        run.Dispose();
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
