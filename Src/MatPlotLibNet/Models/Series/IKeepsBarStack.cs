// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>An axes context that works the stack of its bars out once and hands the same stack to every bar that asks, the
/// context a draw asks its range through. A series reads it through <see cref="StackableExtensions"/> on any
/// <see cref="IAxesContext"/>, where a context that keeps no stack gets it worked out on the spot.</summary>
/// <remarks>A separate internal interface, not a member of the public <see cref="IAxesContext"/>, so that an application can
/// still implement, proxy or mock the public interface.</remarks>
internal interface IKeepsBarStack : IAxesContext
{
    /// <summary>The stack of the context's stacking series, worked out the first time it is asked.</summary>
    BarStack BarStack { get; }
}
