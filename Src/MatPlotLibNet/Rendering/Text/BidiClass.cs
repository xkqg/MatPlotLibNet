// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Text;

/// <summary>The Unicode Bidi_Class property: the 23 values UAX #9 assigns to every character, in the
/// abbreviations the standard uses. Internal because the value set belongs to Unicode, not to this library.</summary>
internal enum BidiClass : byte
{
    /// <summary>Left-to-right letter.</summary>
    L,
    /// <summary>Right-to-left letter (Hebrew and others).</summary>
    R,
    /// <summary>Arabic letter.</summary>
    AL,
    /// <summary>European number.</summary>
    EN,
    /// <summary>European number separator.</summary>
    ES,
    /// <summary>European number terminator.</summary>
    ET,
    /// <summary>Arabic number.</summary>
    AN,
    /// <summary>Common number separator.</summary>
    CS,
    /// <summary>Nonspacing mark.</summary>
    NSM,
    /// <summary>Boundary neutral: removed by rule X9.</summary>
    BN,
    /// <summary>Paragraph separator.</summary>
    B,
    /// <summary>Segment separator.</summary>
    S,
    /// <summary>Whitespace.</summary>
    WS,
    /// <summary>Other neutral.</summary>
    ON,
    /// <summary>Left-to-right embedding.</summary>
    LRE,
    /// <summary>Left-to-right override.</summary>
    LRO,
    /// <summary>Right-to-left embedding.</summary>
    RLE,
    /// <summary>Right-to-left override.</summary>
    RLO,
    /// <summary>Pop directional format.</summary>
    PDF,
    /// <summary>Left-to-right isolate.</summary>
    LRI,
    /// <summary>Right-to-left isolate.</summary>
    RLI,
    /// <summary>First strong isolate.</summary>
    FSI,
    /// <summary>Pop directional isolate.</summary>
    PDI,
}

/// <summary>The paragraph direction a caller can impose instead of letting rules P2 and P3 find the first strong
/// character.</summary>
internal enum BidiDirection : byte
{
    /// <summary>The paragraph reads left to right (embedding level 0).</summary>
    LeftToRight = 0,

    /// <summary>The paragraph reads right to left (embedding level 1).</summary>
    RightToLeft = 1,
}
