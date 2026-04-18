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
    /// <remarks><b>Append-only ordinal contract (Phase O of v1.7.2):</b> never reorder,
    /// remove, or renumber. See <c>EnumOrdinalContractTests</c>.</remarks>
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
    }

    private readonly List<ToolbarButton> _buttons = [];

    /// <summary>Currently active tool mode.</summary>
    public ToolMode ActiveTool { get; private set; } = ToolMode.Pan;

    /// <summary>The configured toolbar buttons.</summary>
    public IReadOnlyList<ToolbarButton> Buttons => _buttons;

    /// <summary>Creates a default toolbar for the given figure.</summary>
    public static InteractionToolbar CreateDefault(Figure figure)
    {
        var toolbar = new InteractionToolbar();
        toolbar._buttons.Add(new ToolbarButton("pan", "Pan (drag)", IsToggle: true));
        toolbar._buttons.Add(new ToolbarButton("zoom", "Rectangle Zoom (Ctrl+drag)", IsToggle: true));

        bool has3D = false;
        foreach (var ax in figure.SubPlots)
            if (ax.CoordinateSystem == CoordinateSystem.ThreeD) { has3D = true; break; }

        if (has3D)
            toolbar._buttons.Add(new ToolbarButton("rotate3d", "Rotate 3D (right-drag)", IsToggle: true));

        toolbar._buttons.Add(new ToolbarButton("home", "Reset View"));
        toolbar._buttons.Add(new ToolbarButton("cursor", "Data Cursor (click)", IsToggle: true));

        return toolbar;
    }

    /// <summary>Activates a tool by its button ID.</summary>
    public void Activate(string toolId)
    {
        ActiveTool = toolId switch
        {
            "pan" => ToolMode.Pan,
            "zoom" => ToolMode.Zoom,
            "rotate3d" => ToolMode.Rotate3D,
            "cursor" => ToolMode.DataCursor,
            _ => ActiveTool,
        };
    }

    /// <summary>Returns the ID of the currently active toggle tool.</summary>
    public string ActiveToolId => ActiveTool switch
    {
        ToolMode.Pan => "pan",
        ToolMode.Zoom => "zoom",
        ToolMode.Rotate3D => "rotate3d",
        ToolMode.DataCursor => "cursor",
        ToolMode.SpanSelect => "span",
        _ => "pan",
    };
}
