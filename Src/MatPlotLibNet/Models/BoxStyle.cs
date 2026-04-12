// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models;

/// <summary>Specifies the visual style of the background box drawn around an annotation's text.</summary>
public enum BoxStyle
{
    /// <summary>No background box is drawn. The annotation text is rendered without a surrounding box.</summary>
    None,

    /// <summary>A plain rectangle with sharp corners.</summary>
    Square,

    /// <summary>A rectangle with rounded Bezier-curved corners.</summary>
    Round,

    /// <summary>A rounded rectangle with a zigzag (saw-tooth) bottom edge.</summary>
    RoundTooth,

    /// <summary>A rectangle with sawtooth edges on all four sides.</summary>
    Sawtooth
}
