// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>Makes the controller of one figure from the context a native control's <see cref="InteractionHost"/> made for it.
/// The built-in one is <see cref="InteractionController.Create(InteractionContext)"/>; give a control another through its
/// <c>ControllerFactory</c> to change its gestures or to put a controller of your own in its place.</summary>
/// <param name="context">The figure, its layout, the sink every gesture goes into and the toolbar.</param>
/// <returns>The controller the control forwards its input to.</returns>
public delegate IInteractionController InteractionControllerFactory(InteractionContext context);
