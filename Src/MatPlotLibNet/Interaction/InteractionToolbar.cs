// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Interaction;

/// <summary>Manages the interactive toolbar state: which tool is active, which buttons are
/// available, and toolbar layout. Platform-agnostic — controls use <see cref="ToolbarState"/>
/// to draw the overlay.</summary>
public sealed class InteractionToolbar
{
    /// <summary>Active tool modes.</summary>
    /// <remarks><b>Append-only ordinal contract:</b> never reorder,
    /// remove, or renumber.</remarks>
    public enum ToolMode
    {
        /// <summary>Default: left-drag pans, scroll zooms.</summary>
        Pan = 0,
        /// <summary>Ctrl+drag rectangle zoom is the primary gesture.</summary>
        Zoom = 1,
        /// <summary>Right-drag rotates 3D view.</summary>
        Rotate3D = 2,
        /// <summary>Click pins a data annotation.</summary>
        DataCursor = 3,
        /// <summary>Alt+drag selects an X-range.</summary>
        SpanSelect = 4,
        /// <summary>Click two points to draw a trendline.</summary>
        Trendline = 5,
        /// <summary>Click to draw a horizontal price level.</summary>
        Level = 6,
        /// <summary>Click high then low to draw a Fibonacci retracement.</summary>
        Fibonacci = 7,
    }

    /// <summary>Button IDs used by <see cref="Activate"/> and <see cref="CreateDefault"/>.</summary>
    public static class ToolIds
    {
        /// <summary>The button ID of the pan tool (<see cref="ToolMode.Pan"/>).</summary>
        public const string Pan       = "pan";
        /// <summary>The button ID of the rectangle-zoom tool (<see cref="ToolMode.Zoom"/>).</summary>
        public const string Zoom      = "zoom";
        /// <summary>The button ID of the 3D rotation tool (<see cref="ToolMode.Rotate3D"/>); the default toolbar offers it only for a figure with a 3D axes.</summary>
        public const string Rotate3D  = "rotate3d";
        /// <summary>The button ID of the Reset View button. It is an action, not a tool mode: <see cref="Activate"/> ignores it and the active tool stays as it was.</summary>
        public const string Home      = "home";
        /// <summary>The button ID of the data-cursor tool (<see cref="ToolMode.DataCursor"/>).</summary>
        public const string Cursor    = "cursor";
        /// <summary>The button ID of the X-range selection tool (<see cref="ToolMode.SpanSelect"/>).</summary>
        public const string Span      = "span";
        /// <summary>The button ID of the trendline drawing tool (<see cref="ToolMode.Trendline"/>).</summary>
        public const string Trendline = "trendline";
        /// <summary>The button ID of the horizontal-level drawing tool (<see cref="ToolMode.Level"/>).</summary>
        public const string Level     = "level";
        /// <summary>The button ID of the Fibonacci retracement drawing tool (<see cref="ToolMode.Fibonacci"/>).</summary>
        public const string Fibonacci = "fibonacci";
    }

    private static readonly IReadOnlyDictionary<string, ToolMode> _idToMode =
        new Dictionary<string, ToolMode>
        {
            [ToolIds.Pan]       = ToolMode.Pan,
            [ToolIds.Zoom]      = ToolMode.Zoom,
            [ToolIds.Rotate3D]  = ToolMode.Rotate3D,
            [ToolIds.Cursor]    = ToolMode.DataCursor,
            [ToolIds.Span]      = ToolMode.SpanSelect,
            [ToolIds.Trendline] = ToolMode.Trendline,
            [ToolIds.Level]     = ToolMode.Level,
            [ToolIds.Fibonacci] = ToolMode.Fibonacci,
        };

    private static readonly IReadOnlyDictionary<ToolMode, string> _modeToId =
        new Dictionary<ToolMode, string>
        {
            [ToolMode.Pan]        = ToolIds.Pan,
            [ToolMode.Zoom]       = ToolIds.Zoom,
            [ToolMode.Rotate3D]   = ToolIds.Rotate3D,
            [ToolMode.DataCursor] = ToolIds.Cursor,
            [ToolMode.SpanSelect] = ToolIds.Span,
            [ToolMode.Trendline]  = ToolIds.Trendline,
            [ToolMode.Level]      = ToolIds.Level,
            [ToolMode.Fibonacci]  = ToolIds.Fibonacci,
        };

    private readonly List<ToolbarButton> _buttons = [];

    /// <summary>Currently active tool mode.</summary>
    public ToolMode ActiveTool { get; private set; } = ToolMode.Pan;

    /// <summary>The configured toolbar buttons.</summary>
    public IReadOnlyList<ToolbarButton> Buttons => _buttons;

    /// <summary>Creates a default toolbar for the given figure.</summary>
    public static InteractionToolbar CreateDefault(Figure figure)
    {
        var toolbar = new InteractionToolbar();
        toolbar._buttons.Add(new ToolbarButton(ToolIds.Pan,      "Pan (drag)",                        IsToggle: true));
        toolbar._buttons.Add(new ToolbarButton(ToolIds.Zoom,     "Rectangle Zoom (Ctrl+drag)",        IsToggle: true));

        bool has3D = false;
        foreach (var ax in figure.SubPlots)
            if (ax.CoordinateSystem == CoordinateSystem.ThreeD) { has3D = true; break; }

        if (has3D)
            toolbar._buttons.Add(new ToolbarButton(ToolIds.Rotate3D, "Rotate 3D (right-drag)", IsToggle: true));

        toolbar._buttons.Add(new ToolbarButton(ToolIds.Home,     "Reset View"));
        toolbar._buttons.Add(new ToolbarButton(ToolIds.Cursor,   "Data Cursor (click)",               IsToggle: true));
        toolbar._buttons.Add(new ToolbarButton(ToolIds.Trendline,"Trendline (2 clicks)",              IsToggle: true));
        toolbar._buttons.Add(new ToolbarButton(ToolIds.Level,    "Horizontal Level (click)",          IsToggle: true));
        toolbar._buttons.Add(new ToolbarButton(ToolIds.Fibonacci,"Fibonacci Retracement (2 clicks)",  IsToggle: true));

        return toolbar;
    }

    /// <summary>Activates a tool by its button ID. Unknown IDs leave the active tool unchanged.</summary>
    public void Activate(string toolId)
    {
        if (_idToMode.TryGetValue(toolId, out var mode))
            ActiveTool = mode;
    }

    /// <summary>Returns the ID of the currently active toggle tool.</summary>
    public string ActiveToolId => _modeToId[ActiveTool];
}
