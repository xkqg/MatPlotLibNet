// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>What a native chart control says about itself so the library can wire it to a server: whether it is interactive, and the
/// <see cref="InteractionHost"/> its gestures go through. The WPF, Avalonia, Uno and MAUI controls (the chart and the streaming one of
/// each) implement it, and <see cref="ServerRouteExtensions.RouteToServer{TControl}"/> does the wiring once for all of them.</summary>
internal interface IServerInteractive
{
    /// <summary>Whether the reader can work the chart. The control's own property: it is a dependency or bindable property there.</summary>
    bool IsInteractive { get; set; }

    /// <summary>The host the control's gestures go through, and that carries the server sink.</summary>
    InteractionHost Host { get; }
}
