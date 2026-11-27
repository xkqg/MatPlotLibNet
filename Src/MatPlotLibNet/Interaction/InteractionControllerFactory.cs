// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Interaction;

/// <summary>How an <see cref="InteractionHost"/> gets the controller for a figure it is shown. The host's own is
/// <see cref="InteractionController.For"/>.</summary>
/// <param name="figure">The figure that was painted.</param>
/// <param name="layout">The layout of that paint.</param>
/// <param name="serverEventSink">Where the gestures are sent, or <see langword="null"/> to apply them to the figure in place.</param>
internal delegate IHostedInteractionController InteractionControllerFactory(
    Figure figure, IChartLayout layout, Action<FigureInteractionEvent>? serverEventSink);
