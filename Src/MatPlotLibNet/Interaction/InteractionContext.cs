// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Interaction;

/// <summary>What a controller of one figure is made from: the figure, the chart its events are addressed to, the layout of the
/// latest paint, the sink every gesture goes into and the toolbar of the drawing tools. A native control's
/// <see cref="InteractionHost"/> makes one for each figure it is shown and hands it to its
/// <see cref="InteractionHost.ControllerFactory"/>, so the built-in <see cref="InteractionController"/> and a controller written
/// outside the library are made from the same thing.
/// <para>Everything a controller sends goes through <see cref="Sink"/>. The sink hands the event on (to the figure, which it
/// changes in place, or to a server), then raises the clicked point for a click on a data point and then tells the host's
/// <see cref="IChartEventHandler"/>. That is why a controller of your own needs nothing more than the sink for the control's
/// <c>DataPointClicked</c> and its handler to work.</para></summary>
public sealed class InteractionContext
{
    /// <summary>The layout the modifiers read: the one of the latest paint, replaced in place by <see cref="UpdateLayout"/>.</summary>
    private readonly LayoutSlot _layout;

    /// <summary>Creates the context of a figure whose gestures are handed to <paramref name="sink"/>, which applies or sends
    /// them.</summary>
    /// <param name="figure">The figure the gestures act on.</param>
    /// <param name="layout">The layout of the paint the figure was drawn with.</param>
    /// <param name="sink">Where every gesture goes: it applies the event to the figure, sends it to a server, or records it.</param>
    public InteractionContext(Figure figure, IChartLayout layout, Action<FigureInteractionEvent> sink)
    {
        Figure = figure;
        ChartId = figure.ChartId ?? string.Empty;
        _layout = new LayoutSlot(layout);
        Sink = evt =>
        {
            sink(evt);
            if (evt is DataCursorEvent pinned)
                DataPointClicked?.Invoke(pinned.Annotation);

            EventHandled?.Invoke(evt);
        };
    }

    /// <summary>The context a host makes for a figure: its gestures are sent through <paramref name="serverEventSink"/> when there is
    /// one, and otherwise applied to the figure in place, with <paramref name="repaint"/> called after each one that changed it.</summary>
    /// <param name="figure">The figure that was painted.</param>
    /// <param name="layout">The layout of that paint.</param>
    /// <param name="serverEventSink">Where the gestures are sent, or <see langword="null"/> to apply them to the figure.</param>
    /// <param name="repaint">How the figure is drawn again after a gesture changed it.</param>
    internal static InteractionContext For(Figure figure, IChartLayout layout, Action<FigureInteractionEvent>? serverEventSink,
        Action repaint) =>
        new(figure, layout, serverEventSink ?? (evt =>
        {
            evt.ApplyTo(figure);
            // A notification (a hover, a brush, a clicked point) changes nothing, so it asks for no repaint.
            if (evt is not FigureNotificationEvent)
                repaint();
        }));

    /// <summary>The figure the gestures act on.</summary>
    public Figure Figure { get; }

    /// <summary>The chart every event is addressed to: the figure's <see cref="Models.Figure.ChartId"/>, or empty when it has none.</summary>
    public string ChartId { get; }

    /// <summary>The layout of the latest paint: where the axes are and which range they show. A modifier holds this one, and a
    /// repaint replaces what it reads (<see cref="UpdateLayout"/>), so a gesture in progress carries on across the repaint.</summary>
    public IChartLayout Layout => _layout;

    /// <summary>Where every gesture of the controller goes. A modifier is made with it and sends its events into it.</summary>
    public Action<FigureInteractionEvent> Sink { get; }

    /// <summary>The toolbar that says which drawing tool is active; the drawing modifiers read it.</summary>
    public InteractionToolbar Toolbar { get; } = new();

    /// <summary>Hands the context the layout of the latest paint: every modifier made with <see cref="Layout"/> reads it from now
    /// on, without being made again.</summary>
    /// <param name="layout">The layout of the latest paint.</param>
    public void UpdateLayout(IChartLayout layout) => _layout.Current = layout;

    /// <summary>Raised, after the sink has handed the event on, for a click on a data point.</summary>
    internal event Action<PinnedAnnotation>? DataPointClicked;

    /// <summary>Raised for every event, after the sink has handed it on and after <see cref="DataPointClicked"/>.</summary>
    internal event Action<FigureInteractionEvent>? EventHandled;
}
