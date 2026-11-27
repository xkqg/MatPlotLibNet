// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>A series whose share in the range of its axes depends on the theme it is drawn under: a quiver that the matplotlib themes
/// autoscale to the width of the axes ranges over its tails, where it ranges over its tails and heads in data units. The axes read
/// it through <c>DataRangeUnder</c> on any <see cref="ISeries"/>, where a series without it gives
/// <see cref="ISeries.ComputeDataRange"/>.</summary>
/// <remarks>A separate internal interface, not a member of the public <see cref="ISeries"/>, so that an application can still
/// implement, proxy or mock the public interface.</remarks>
internal interface IThemedRange : ISeries
{
    /// <summary>What the series contributes to the range of its axes when it is drawn under <paramref name="theme"/>.</summary>
    /// <param name="context">The axes the series is drawn on.</param>
    /// <param name="theme">The theme of the figure.</param>
    /// <returns>The contribution.</returns>
    DataRangeContribution RangeUnder(IAxesContext context, Theme theme);
}
