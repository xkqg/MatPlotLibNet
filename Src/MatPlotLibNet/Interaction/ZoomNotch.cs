// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>How far one notch of the wheel zooms a range, on every surface: a native control, a pinch on a touch surface, the
/// page's zoom script and the server-interaction script. It is matplotlib's: <c>backend_bases.scroll_handler</c> scales the
/// range by <c>0.85 ** event.step</c>, a step up shrinking it to 0.85 and a step down widening it by the inverse.</summary>
internal static class ZoomNotch
{
    /// <summary>What one notch in scales a range by.</summary>
    internal const double In = 0.85;

    /// <summary>What one notch out scales a range by: the inverse of a notch in, so a notch out undoes a notch in.</summary>
    internal const double Out = 1 / In;

    /// <summary>The scale of one notch of a wheel that turned by <paramref name="deltaY"/>: in when it turned up (a negative
    /// delta), out otherwise.</summary>
    /// <param name="deltaY">The wheel's vertical delta.</param>
    /// <returns><see cref="In"/> or <see cref="Out"/>.</returns>
    internal static double ScaleOf(double deltaY) => deltaY < 0 ? In : Out;
}
