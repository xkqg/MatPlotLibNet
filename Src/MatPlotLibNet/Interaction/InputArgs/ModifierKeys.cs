// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>Keyboard modifier keys held during a pointer or key event.</summary>
[Flags]
public enum ModifierKeys
{
    /// <summary>No modifier keys held.</summary>
    None  = 0,
    /// <summary>Shift key held.</summary>
    Shift = 1,
    /// <summary>Control (Ctrl) key held.</summary>
    Ctrl  = 2,
    /// <summary>Alt key held.</summary>
    Alt   = 4,
}
