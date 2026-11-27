// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series.Streaming;

/// <summary>A series whose data comes in while it is shown, counted by the version of its <see cref="StreamRing{TSample}"/>: every
/// streaming kind (a line, a scatter, a candlestick, a sampled signal) answers through it, whatever public contract it is appended
/// through, so a streaming figure asks one question of every series to learn whether a frame is due
/// (<see cref="Models.Streaming.StreamingFigure.DataVersion"/>).</summary>
/// <remarks>It sits beside the public <see cref="IStreamingSeries"/> and <see cref="IStreamingOhlcSeries"/>, which keep their own
/// <c>Version</c>: what the library asks of every streaming kind is added here, not to a public interface.</remarks>
internal interface IVersionedSeries
{
    /// <summary>How many times the data of the series has changed: it rises by one at every append, batch and clear.</summary>
    long Version { get; }
}
