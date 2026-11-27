// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>What a <see cref="TickInk"/> holds, asked of the value: the one place its flags are read, so the tick drawing, the
/// Cartesian renderer, a Y axis slot and an axis whose labels stand on the other side never decode them.</summary>
internal static class TickInkExtensions
{
    extension(TickInk ink)
    {
        /// <summary>Whether the labels (and an axis' offset text) are drawn.</summary>
        internal bool DrawsLabels => (ink & TickInk.Labels) != 0;

        /// <summary>Whether the major and minor tick marks are drawn.</summary>
        internal bool DrawsMarks => (ink & TickInk.Marks) != 0;

        /// <summary>The same ink without its labels: the marks alone, or nothing when it held only labels.</summary>
        internal TickInk WithoutLabels => ink & ~TickInk.Labels;

        /// <summary>What of this ink <paramref name="allowed"/> lets through: the labels and the marks both hold.</summary>
        /// <param name="allowed">What may be drawn.</param>
        /// <returns>The ink both hold; <see cref="TickInk.None"/> when they share nothing.</returns>
        internal TickInk Within(TickInk allowed) => ink & allowed;
    }
}
