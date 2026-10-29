// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>Specifies how a text stands against the point it is placed at, vertically: the counterpart of
/// <see cref="TextAlignment"/> across the line instead of along it. The default, <see cref="Baseline"/>, is how every
/// text stood before this choice existed.</summary>
/// <remarks><b>Append-only ordinal contract:</b> never reorder, remove, or renumber.</remarks>
public enum TextVerticalAlignment
{
    /// <summary>The text's baseline stands on the point: the letters rise above it and only the descenders hang below.</summary>
    Baseline = 0,

    /// <summary>The text is centred on the point: the point lies halfway up the letters.</summary>
    Center = 1,

    /// <summary>The top of the text hangs from the point: the text lies below it.</summary>
    Top = 2,

    /// <summary>The bottom of the text stands on the point: the text lies above it.</summary>
    Bottom = 3,
}
