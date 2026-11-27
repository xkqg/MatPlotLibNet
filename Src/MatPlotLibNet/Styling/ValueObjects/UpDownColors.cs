// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>The two colours of a rise and a fall: the colour of an up bar and of a down bar, of a gain and of a loss, of a long and of a
/// short position. A series, a verb, a renderer fallback or an indicator that draws a rise and a fall and is given neither colour reads
/// <see cref="Standard"/>, so the pair is typed once and a part that is meant to agree with another cannot differ from it.</summary>
/// <param name="Up">The colour of a rise.</param>
/// <param name="Down">The colour of a fall.</param>
internal readonly record struct UpDownColors(Color Up, Color Down)
{
    /// <summary>The pair the library draws a rise and a fall in when it is given none: green and red, the CSS colours of those names
    /// (not the greener and redder members of the standard cycle).</summary>
    internal static UpDownColors Standard { get; } = new(Colors.Green, Colors.Red);
}
