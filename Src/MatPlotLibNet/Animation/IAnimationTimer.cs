// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Animation;

/// <summary>Platform-neutral animation tick source. Platform controls implement this
/// with their native timer (DispatcherTimer, requestAnimationFrame, etc.).</summary>
public interface IAnimationTimer : IDisposable
{
    /// <summary>Desired tick interval. Implementations should honour this on the
    /// next <see cref="Start"/> call or update it immediately if already running.</summary>
    TimeSpan Interval { get; set; }

    /// <summary>Fired on each tick. Subscribers update their figure and request invalidate.</summary>
    event EventHandler Tick;

    /// <summary>Starts the timer.</summary>
    void Start();

    /// <summary>Stops the timer. Calling Stop before Start is a no-op.</summary>
    void Stop();
}
