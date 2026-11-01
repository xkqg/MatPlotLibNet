// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Patches;

/// <summary>A patch that encloses an area and fills it: a <see cref="Patch"/> with a face.</summary>
/// <example>
/// Every filled patch takes a face colour of its own; one left at the default is the first colour of the cycle:
/// <code>
/// ax.AddPatch(new Circle(new(0, 0), 1) { FaceColor = Colors.Tab10Orange, EdgeColor = Colors.Black });
/// </code>
/// </example>
public abstract class FilledPatch : Patch
{
    /// <summary>Only the library derives a patch (see <see cref="Patch"/>).</summary>
    private protected FilledPatch() { }

    /// <summary>The colour the enclosed area is filled with. The first colour of the standard cycle, a blue (matplotlib's
    /// <c>C0</c>), by default, the same for every patch: a patch never moves the colour cycle on, as a series does.
    /// <see cref="Colors.Transparent"/> fills nothing.</summary>
    public Color FaceColor { get; set; } = Colors.Tab10Blue;

    /// <inheritdoc />
    internal override Color DrawnFaceColor => FaceColor.ScaleAlpha(Alpha);
}
