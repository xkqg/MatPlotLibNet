// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>How a streaming indicator pins the pane it is attached to: through the range its values live in
/// (<see cref="IPinsItsPane"/>), read here and nowhere else.</summary>
internal static class StreamingPaneExtensions
{
    extension(IStreamingIndicator indicator)
    {
        /// <summary>Pins the Y axis of <paramref name="axes"/> to the range the indicator's values live in, as its batch indicator
        /// pins its own pane; an indicator of the caller's, or one whose values have no fixed range, leaves the axis to its data.</summary>
        /// <param name="axes">The pane the indicator is attached to.</param>
        internal void PinPane(Axes axes)
        {
            if (indicator is IPinsItsPane { PinnedRange: { } range }) range.Pin(axes);
        }
    }
}