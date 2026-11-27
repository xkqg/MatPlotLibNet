// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>The first two colours of the standard cycle, as the colours of a part whose colour is chosen before any theme is known:
/// the value a part rests at that the JSON always writes (a progress bar's fill, a waterfall's total, the face a patch reads at rest),
/// the pair a verb gives its series and its axes (a Pareto chart's bars and line), the colour a verb chooses and leaves to the theme's
/// cycle under the matplotlib themes (the indicators, a cartogram), the colour a cycler that names none reads. A part given no colour
/// that is drawn under a theme does not read this: it wears its theme's cycle (<c>Theme.FirstCycleColor</c>). The pair is named here
/// once, so the standard cycle's first two colours are not typed at every part that rests at them.</summary>
/// <param name="First">The first colour of the standard cycle (matplotlib's <c>C0</c> under the default style): blue.</param>
/// <param name="Second">The second colour of the standard cycle (<c>C1</c>): orange.</param>
internal readonly record struct CyclePair(Color First, Color Second)
{
    /// <summary>The pair the library rests at where a part's colour is a choice made before the theme is known.</summary>
    internal static CyclePair Standard { get; } = new(Colors.Tab10Blue, Colors.Tab10Orange);
}
