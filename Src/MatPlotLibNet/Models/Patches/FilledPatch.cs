// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Patches;

/// <summary>A patch that encloses an area and fills it: a <see cref="Patch"/> with a face.</summary>
/// <example>
/// Every filled patch takes a face colour of its own; one given none is filled in the first colour of its theme's cycle:
/// <code>
/// ax.AddPatch(new Circle(new(0, 0), 1) { FaceColor = Colors.Tab10Orange, EdgeColor = Colors.Black });
/// </code>
/// </example>
public abstract class FilledPatch : Patch
{
    /// <summary>Only the library derives a patch (see <see cref="Patch"/>).</summary>
    private protected FilledPatch() { }

    /// <summary>The colour the enclosed area is filled with. Until one is set the patch is filled in the first colour of its theme's
    /// cycle (matplotlib's <c>C0</c>, a blue under the standard cycle), the same for every patch: a patch never moves the colour cycle
    /// on, as a series does. This reads that blue then, and a colour set, the blue included, is drawn as set under every theme.
    /// <see cref="Colors.Transparent"/> fills nothing.</summary>
    public Color FaceColor
    {
        get => GivenFaceColor ?? CyclePair.Standard.First;
        set => GivenFaceColor = (Color?)value;
    }

    /// <summary>The face colour the caller gave, <see langword="null"/> while none was. The JSON writes it, so a face given that equals
    /// the one a new patch reads is not read back as a face never named.</summary>
    internal Color? GivenFaceColor { get; private set => Change(ref field, value); }

    /// <inheritdoc />
    internal override Color DrawnFaceColorUnder(Theme theme) => WithTheOpacityGiven(GivenFaceColor ?? theme.FirstCycleColor);
}
