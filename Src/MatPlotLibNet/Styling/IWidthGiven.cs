// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>A kind whose line keeps whether its author gave it a width, because the theme decides it when they did not: what it was
/// given, and the rule its kind is drawn wide by (<see cref="LineWidthRule"/>). The slot reads both through here
/// (<see cref="Rendering.SeriesSlot.LineWidthOf(IWidthGiven)"/>), as the legend swatch and the reported width do, so no site picks
/// them apart.</summary>
internal interface IWidthGiven
{
    /// <summary>The width its author gave, in pixels, <see langword="null"/> while none was.</summary>
    double? GivenLineWidth { get; }

    /// <summary>The rule a line of the kind is drawn wide by: a plain line's (<see cref="LineWidthRule.Line"/>) unless the kind names
    /// another.</summary>
    LineWidthRule WidthRule => LineWidthRule.Line;
}

/// <summary>What a kind reports as its width without a theme: the one rule of every <see cref="IWidthGiven"/>.</summary>
internal static class WidthGivenExtensions
{
    extension(IWidthGiven series)
    {
        /// <summary>The width the kind reports: the one its author gave, else the resting width of its rule.</summary>
        internal double ReportedLineWidth => series.GivenLineWidth ?? series.WidthRule.Resting;
    }
}
