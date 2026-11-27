// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Animation;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests;

/// <summary>The animation source a test hands a control or a follower: it says how many listen to it, and shows a frame to them when
/// the test says so. Every suite of a control links this one file.</summary>
internal sealed class FakeAnimationSource : IAnimationSource
{
    /// <inheritdoc />
    public event EventHandler<Figure>? FrameReady;

    /// <summary>How many handlers listen to <see cref="FrameReady"/> now.</summary>
    public int SubscriberCount => FrameReady?.GetInvocationList().Length ?? 0;

    /// <summary>Shows a frame to every listener, the way a running animation does.</summary>
    /// <param name="figure">The frame.</param>
    public void Raise(Figure figure) => FrameReady?.Invoke(this, figure);
}
