// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Threading;
using MatPlotLibNet.Animation;

namespace MatPlotLibNet.Avalonia;

/// <summary><see cref="IAnimationTimer"/> that fires on the Avalonia UI thread via
/// <see cref="DispatcherTimer"/>. Use this when tick handlers must touch Avalonia controls.</summary>
public sealed class AvaloniaAnimationTimer : IAnimationTimer
{
    private readonly DispatcherTimer _timer;
    private bool _running;

    /// <inheritdoc />
    public TimeSpan Interval
    {
        get => _timer.Interval;
        set => _timer.Interval = value;
    }

    /// <inheritdoc />
    public event EventHandler? Tick;

    /// <summary>Creates a stopped timer on the Avalonia UI thread with a tick every 16 ms (about 60 frames a second);
    /// call <see cref="Start"/> to begin ticking.</summary>
    public AvaloniaAnimationTimer()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _timer.Tick += (_, _) => RaiseTick();
    }

    /// <summary>Says to whoever listens that a frame is due.</summary>
    internal void RaiseTick() => Tick?.Invoke(this, EventArgs.Empty);

    /// <inheritdoc />
    public void Start()
    {
        if (_running) return;
        _running = true;
        _timer.Start();
    }

    /// <inheritdoc />
    public void Stop()
    {
        _running = false;
        _timer.Stop();
    }

    /// <inheritdoc />
    public void Dispose() => Stop();
}
