// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Animation;

/// <summary>Default <see cref="IAnimationTimer"/> using <see cref="System.Threading.Timer"/>
/// at ~60fps (16ms). Works on all platforms; UI-thread marshalling is the caller's responsibility.</summary>
public sealed class SystemThreadingAnimationTimer : IAnimationTimer
{
    private Timer? _timer;
    private TimeSpan _interval = TimeSpan.FromMilliseconds(16);
    private bool _running;

    /// <inheritdoc />
    public TimeSpan Interval
    {
        get => _interval;
        set
        {
            _interval = value;
            // If the timer is live, update its period. When stopped, _timer is null
            // and the null-conditional short-circuits — no separate _running check needed.
            _timer?.Change(_interval, _interval);
        }
    }

    /// <inheritdoc />
    public event EventHandler? Tick;

    /// <inheritdoc />
    public void Start()
    {
        if (_running) return;
        _running = true;
        _timer = new Timer(_ => Tick?.Invoke(this, EventArgs.Empty), null, _interval, _interval);
    }

    /// <inheritdoc />
    public void Stop()
    {
        _running = false;
        _timer?.Dispose();
        _timer = null;
    }

    /// <inheritdoc />
    public void Dispose() => Stop();
}
