// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Animation;

namespace MatPlotLibNet.Maui;

/// <summary><see cref="IAnimationTimer"/> for MAUI backed by <see cref="System.Threading.Timer"/>.
/// Tick handlers run on the thread pool; use <see cref="MainThread.BeginInvokeOnMainThread"/>
/// inside the handler when touching UI elements.</summary>
public sealed class MauiAnimationTimer : IAnimationTimer
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
            if (_running) _timer?.Change(_interval, _interval);
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
