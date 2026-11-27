// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>Resets the axes to the limits they had when the modifier was made, on a double-click or <c>Home</c> key
/// press: the ranges of the layout are snapshotted at construction, and the event it sends carries them.
/// <para>An <see cref="InteractionController"/> does not use this one: a control repaints after every change, and a
/// snapshot taken from the layout of a repaint is the zoomed range by then. The controller uses a modifier that reads
/// the layout when the key is pressed, and the figure remembers the view the reader started from
/// (see <see cref="ResetEvent"/>). This class is for a host that builds its own modifiers and wants the range of a
/// moment it chose.</para></summary>
public sealed class ResetModifier : InteractionModifier
{
    private readonly HomeModifier _home;

    /// <summary>Creates a reset modifier. The original axis limits are snapshotted at construction time.</summary>
    public ResetModifier(string chartId, IChartLayout layout, Action<FigureInteractionEvent> sink)
        : base(chartId, new SnapshotLayout(layout), sink) =>
        _home = new HomeModifier(chartId, Layout, sink);

    /// <inheritdoc />
    public override bool HandlesPointerPressed(PointerInputArgs args) => _home.HandlesPointerPressed(args);

    /// <inheritdoc />
    public override void OnPointerPressed(PointerInputArgs args) => _home.OnPointerPressed(args);

    /// <inheritdoc />
    public override bool HandlesKeyDown(KeyInputArgs args) => _home.HandlesKeyDown(args);

    /// <inheritdoc />
    public override void OnKeyDown(KeyInputArgs args) => _home.OnKeyDown(args);
}
