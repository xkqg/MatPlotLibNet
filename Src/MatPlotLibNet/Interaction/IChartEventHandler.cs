// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>Receives everything a viewer does to a chart: every <see cref="FigureInteractionEvent"/>, whichever kind it
/// is. A chart used to offer a callback for each of three gestures (a brush, a hover, a click) and none for the rest, so a
/// zoom, a pan, a legend toggle or a reset could not be observed in .NET code at all. The same interface is taken by a
/// server session (<c>ChartSessionOptions.Handle</c>) and by a native control (its <c>ChartEventHandler</c>, which is <see cref="InteractionHost.Handler"/>
/// of the host the control owns), so one class reacts to a chart wherever it is shown.
/// <para>A handler runs inline, in the order the events arrive: the next event is not taken until it completes, which is
/// what lets it see a consistent figure. A handler that waits on something slow holds the chart up, so it should hand slow
/// work to a queue of its own. A handler that throws costs only the event it was given.</para></summary>
public interface IChartEventHandler
{
    /// <summary>Called once for each event, after the figure has taken it (or, for an event that only observes, after the
    /// callback written for its kind). An event the figure could not take, because it named a subplot the figure lacks, is
    /// not passed on.</summary>
    /// <param name="evt">What the viewer did.</param>
    /// <param name="context">Who did it and where.</param>
    /// <returns>A task that completes when the handler is done with the event.</returns>
    ValueTask OnEventAsync(FigureInteractionEvent evt, ChartEventContext context);
}
