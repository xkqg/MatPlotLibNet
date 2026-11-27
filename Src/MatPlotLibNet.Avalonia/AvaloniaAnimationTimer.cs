// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Threading;
using MatPlotLibNet.Animation;

namespace MatPlotLibNet.Avalonia;

/// <summary>An <see cref="AnimationTimer"/> that ticks on the Avalonia UI thread, through <see cref="DispatcherTimer"/>. Use it when
/// tick handlers must touch Avalonia controls.</summary>
public sealed class AvaloniaAnimationTimer : AnimationTimer
{
    private readonly DispatcherTimer _timer = new() { Interval = DefaultInterval };

    /// <summary>Creates a stopped timer on the Avalonia UI thread with a tick every 16 ms (about 60 frames a second); call
    /// <see cref="AnimationTimer.Start"/> to begin ticking.</summary>
    public AvaloniaAnimationTimer() => _timer.Tick += RaiseTick;

    private protected override void Begin() => _timer.Start();

    private protected override void End() => _timer.Stop();

    private protected override void Retime(TimeSpan interval) => _timer.Interval = interval;
}
