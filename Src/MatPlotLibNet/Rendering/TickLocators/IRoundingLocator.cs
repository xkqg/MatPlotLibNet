// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.TickLocators;

/// <summary>A tick locator that says where an axis it places ends when the theme rounds an axis out to its ticks
/// (<c>axes.autolimit_mode = 'round_numbers'</c>, the classic theme): matplotlib's <c>view_limits</c> of a locator.
/// <see cref="MultipleLocator"/>, <see cref="MaxNLocator"/>, <see cref="AutoLocator"/>, <see cref="LogLocator"/> and
/// <see cref="SymlogLocator"/> round out; a locator without it keeps the range, as matplotlib's base <c>Locator.view_limits</c> does
/// (a <see cref="FixedLocator"/>, a date locator, a locator an application writes). The range of an axis reads it through
/// <see cref="TickLocatorExtensions"/>' <c>RangeRoundedOut</c> on any <see cref="ITickLocator"/>.</summary>
/// <remarks>A separate internal interface, not a member of the public <see cref="ITickLocator"/>, so that an application can still
/// implement, proxy or mock the public interface.</remarks>
internal interface IRoundingLocator : ITickLocator
{
    /// <summary>The range rounded out to where this locator ends an axis that shows <paramref name="range"/>.</summary>
    /// <param name="range">The range, its low end below its high end.</param>
    /// <returns>The rounded range.</returns>
    Range1D RoundedOut(Range1D range);
}
