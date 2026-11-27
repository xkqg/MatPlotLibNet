// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>What the <see cref="InteractionHost"/> hears from a controller beyond <see cref="IInteractionController"/>: the
/// point the reader clicked and every event once it was applied or sent. The host talks to its controller through these two
/// interfaces only, and is built with the factory that makes one, so a stand-in can take the controller's place.</summary>
internal interface IHostedInteractionController : IInteractionController
{
    /// <inheritdoc cref="InteractionHost.DataPointClicked"/>
    event Action<PinnedAnnotation>? DataPointClicked;

    /// <summary>Raised for every event the controller produced, after its sink applied or sent it.</summary>
    event Action<FigureInteractionEvent>? EventHandled;
}
