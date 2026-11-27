// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Animation;

/// <summary>What every animation timer of the library does, in one place: it ticks every 16 milliseconds (about 60 times a second)
/// until it is told another interval, a second start is the same timer, a stop of a timer that never ran is harmless, and a tick is
/// said to whoever listens with the timer itself as the sender. The ticks come from <see cref="System.Threading.Timer"/> on the thread
/// pool, where a tick runs is the listener's to settle, unless the platform's own timer gives them (the dispatcher of WPF or
/// Avalonia). Only the library derives from it: <see cref="SystemThreadingAnimationTimer"/> and the timer of each native package.</summary>
public abstract class AnimationTimer : IAnimationTimer
{
    /// <summary>The interval of a timer until it is told another: 16 milliseconds.</summary>
    private protected static readonly TimeSpan DefaultInterval = TimeSpan.FromMilliseconds(16);

    private TimeSpan _interval = DefaultInterval;
    private bool _running;
    private Timer? _pooled;

    /// <summary>Only the library makes a timer.</summary>
    private protected AnimationTimer() { }

    /// <inheritdoc />
    public TimeSpan Interval
    {
        get => _interval;
        set
        {
            _interval = value;
            Retime(value);
        }
    }

    /// <inheritdoc />
    public event EventHandler? Tick;

    /// <summary>Starts ticking; a timer that already runs goes on as it is.</summary>
    public void Start()
    {
        if (_running) return;
        _running = true;
        Begin();
    }

    /// <summary>Stops ticking; a timer that never ran stays as it is.</summary>
    public void Stop()
    {
        _running = false;
        End();
    }

    /// <summary>Stops the timer.</summary>
    public void Dispose() => Stop();

    /// <summary>Starts the platform's tick at <see cref="Interval"/>: on the thread pool, unless the platform's timer gives it.</summary>
    private protected virtual void Begin() => _pooled = new Timer(_ => RaiseTick(this, EventArgs.Empty), null, _interval, _interval);

    /// <summary>Stops the platform's tick.</summary>
    private protected virtual void End()
    {
        _pooled?.Dispose();
        _pooled = null;
    }

    /// <summary>Gives the platform's tick a new period, as soon as the interval is set.</summary>
    /// <param name="interval">The new time between two ticks.</param>
    private protected virtual void Retime(TimeSpan interval) => _pooled?.Change(interval, interval);

    /// <summary>Says to whoever listens that a frame is due, with this timer as the sender, whichever timer of the platform ticked:
    /// it has the shape of an <see cref="EventHandler"/>, so a platform's timer is subscribed to it as it is.</summary>
    /// <param name="sender">The platform's timer that ticked; not passed on.</param>
    /// <param name="e">Not read.</param>
    private protected void RaiseTick(object? sender, EventArgs e) => Tick?.Invoke(this, EventArgs.Empty);
}
