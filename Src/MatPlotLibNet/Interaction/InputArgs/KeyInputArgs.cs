// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>Platform-neutral data for a key-down event.</summary>
/// <param name="Key">Normalized key name (e.g. <c>"Home"</c>, <c>"Escape"</c>, <c>"r"</c>).
/// Use the platform input adapter to normalise platform-specific key identifiers.</param>
/// <param name="Modifiers">Keyboard modifier keys held at the time of the event.</param>
public readonly record struct KeyInputArgs(
    string Key,
    ModifierKeys Modifiers = ModifierKeys.None);
