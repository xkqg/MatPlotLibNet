// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Styling;

/// <summary>How a colour bar takes its room and is drawn: as matplotlib's <c>Colorbar</c> under the matplotlib themes.</summary>
internal sealed record ColorBarLook : IThemeKeys<ColorBarLook>
{
    /// <summary>The library's own colour bar: in the margin, 50 steps, a rectangle for an extended end, labels alone.</summary>
    internal static ColorBarLook Library { get; } = new();

    /// <inheritdoc />
    public void WriteKeys(ThemeDto keys)
    {
        keys.ColorBarTakesRoom = TakesRoom.OrNullAtRest(false);
        keys.ColorBarStepPerColour = StepPerColour.OrNullAtRest(false);
        keys.ColorBarExtendTriangles = ExtendTriangles.OrNullAtRest(false);
        keys.ColorBarTickMarks = TickMarks.OrNullAtRest(false);
    }

    /// <inheritdoc />
    public static ColorBarLook ReadKeys(ThemeDto keys) => new()
    {
        TakesRoom = keys.ColorBarTakesRoom ?? false,
        StepPerColour = keys.ColorBarStepPerColour ?? false,
        ExtendTriangles = keys.ColorBarExtendTriangles ?? false,
        TickMarks = keys.ColorBarTickMarks ?? false,
    };

    /// <summary>Whether an axes' colour bar takes its room from the axes, as <c>fig.colorbar(mappable, ax=ax)</c> does (the axes
    /// keep 0.80 of their cell, the bar stands in the slot beside them); under the constrained layout the bar stands in the margin
    /// as matplotlib's constrained layout does. Otherwise the axes keep their cell and the bar is drawn in the margin.
    /// <c>ColorBarRoomExtensions</c> holds the rule.</summary>
    public bool TakesRoom { get; init; }

    /// <summary>Whether the gradient is drawn in one step for each colour of its map (<c>_process_values</c>), the bar of a filled
    /// contour in one block per band with the ticks on the levels, and the bar of a boundary normaliser naming at most ten
    /// boundaries. Otherwise 50 steps, a gradient for a contour and every boundary named.</summary>
    public bool StepPerColour { get; init; }

    /// <summary>Whether a bar that extends past its range ends in a triangle outside its interior (<c>extendrect=False</c>, each
    /// triangle 0.05 of the interior long). Otherwise a rectangle a tenth of the bar long, inside it. <c>ColorBarStrip</c> holds
    /// the rule.</summary>
    public bool ExtendTriangles { get; init; }

    /// <summary>Whether the bar draws a tick mark at each of its ticks on its outer side (and, for matplotlib's log norm, a minor
    /// mark between the powers), with its labels the pad beyond the marks. Otherwise the labels alone, 4 pixels from the bar.
    /// <c>ColorBarMarks</c> holds the rule.</summary>
    public bool TickMarks { get; init; }
}
