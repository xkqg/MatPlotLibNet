// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Animation;

/// <summary>Non-generic contract for any animation that can push <see cref="Figure"/> frames
/// to subscribers. Platform controls bind to this interface to trigger redraws on each frame.</summary>
public interface IAnimationSource
{
    /// <summary>Raised after each animation frame is ready. Subscribers update their visual surface.</summary>
    event EventHandler<Figure>? FrameReady;
}
