// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Threading.Channels;
using MatPlotLibNet.Interaction;

namespace MatPlotLibNet.AspNetCore;

/// <summary>A chart the <see cref="FigureRegistry"/> holds, still (<see cref="ChartSession"/>) or streaming
/// (<see cref="StreamingChartSession"/>): it takes the viewers' gestures, and it ends when disposed.</summary>
internal interface IRegisteredChart : IAsyncDisposable
{
    /// <summary>Where the gestures of the viewers are written; the session applies them in order.</summary>
    ChannelWriter<FigureInteractionEvent> Gestures { get; }
}
