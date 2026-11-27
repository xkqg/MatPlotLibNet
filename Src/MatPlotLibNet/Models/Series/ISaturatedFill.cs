// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>A series whose seaborn verb fills its shapes with the palette's colour desaturated (<c>saturation=.75</c>): a count plot. It
/// says how much saturation its unnamed colour keeps under a theme; the one colour rule of a series (<c>SeriesSlot.ColorOf</c>) applies
/// it, so the shapes and the legend's handle wear the same colour.</summary>
internal interface ISaturatedFill
{
    /// <summary>How much of its colour's saturation the series keeps under <paramref name="theme"/> where its author names no colour.</summary>
    /// <param name="theme">The theme the series is drawn under.</param>
    /// <returns>The share kept, from 0 to 1.</returns>
    double SaturationUnder(Theme theme);
}
