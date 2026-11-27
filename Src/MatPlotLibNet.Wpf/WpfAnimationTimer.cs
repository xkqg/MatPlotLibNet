// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Windows.Threading;
using MatPlotLibNet.Animation;

namespace MatPlotLibNet.Wpf;

/// <summary>An <see cref="AnimationTimer"/> that ticks on the WPF UI thread, through <see cref="DispatcherTimer"/>. Use it when tick
/// handlers must touch WPF controls.</summary>
public sealed class WpfAnimationTimer : AnimationTimer
{
    private readonly DispatcherTimer _timer = new() { Interval = DefaultInterval };

    /// <summary>Creates a timer that ticks every 16 milliseconds (about 60 times a second) on the dispatcher, once it is started.</summary>
    public WpfAnimationTimer() => _timer.Tick += RaiseTick;

    private protected override void Begin() => _timer.Start();

    private protected override void End() => _timer.Stop();

    private protected override void Retime(TimeSpan interval) => _timer.Interval = interval;
}
