// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>Handles one class of user gesture and produces <see cref="FigureInteractionEvent"/> records.
/// Modifiers are composable: the <see cref="InteractionController"/> tries each in order until
/// one claims the event via <c>Handles*</c>, then delegates the remaining sequence to it.</summary>
public interface IInteractionModifier
{
    /// <summary>Returns <c>true</c> if this modifier wants to handle the pointer-pressed event.</summary>
    bool HandlesPointerPressed(PointerInputArgs args);
    /// <summary>Called when a pointer-pressed event is claimed by this modifier.</summary>
    void OnPointerPressed(PointerInputArgs args);
    /// <summary>Called on every pointer-moved event while this modifier is active, or always for passive modifiers (hover).</summary>
    void OnPointerMoved(PointerInputArgs args);
    /// <summary>Called on pointer-released. Modifier should release any captured state.</summary>
    void OnPointerReleased(PointerInputArgs args);

    /// <summary>Returns <c>true</c> if this modifier wants to handle the scroll event.</summary>
    bool HandlesScroll(ScrollInputArgs args);
    /// <summary>Called when a scroll event is claimed by this modifier.</summary>
    void OnScroll(ScrollInputArgs args);

    /// <summary>Returns <c>true</c> if this modifier wants to handle the key-down event.</summary>
    bool HandlesKeyDown(KeyInputArgs args);
    /// <summary>Called when a key-down event is claimed by this modifier.</summary>
    void OnKeyDown(KeyInputArgs args);
}
