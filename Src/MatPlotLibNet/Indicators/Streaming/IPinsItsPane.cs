// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>What the library's streaming indicators add to <see cref="IStreamingIndicator"/>: the range their values live in, which
/// the pane they are attached to is pinned to, as their batch indicator pins its own. An indicator of the caller's does not
/// implement it, and its pane scales to its data.</summary>
internal interface IPinsItsPane
{
    /// <summary>The range the indicator's values live in; <see langword="null"/> for one whose values have no fixed range.</summary>
    PaneRange? PinnedRange { get; }
}