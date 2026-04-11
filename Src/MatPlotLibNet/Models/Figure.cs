// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models;

/// <summary>Represents the top-level figure that contains one or more subplot axes.</summary>
public sealed class Figure
{
    public string? Title { get; set; }

    /// <summary>Short alternative text for the chart, rendered as the SVG <c>&lt;title&gt;</c> element (falls back to <see cref="Title"/>).</summary>
    public string? AltText { get; set; }

    /// <summary>Longer description rendered as the SVG <c>&lt;desc&gt;</c> element and linked via <c>aria-describedby</c>.</summary>
    public string? Description { get; set; }

    public double Width { get; set; } = 800;

    public double Height { get; set; } = 600;

    public double Dpi { get; set; } = 96;

    public Color? BackgroundColor { get; set; }

    public Theme Theme { get; set; } = Theme.Default;

    public SubPlotSpacing Spacing { get; set; } = new();

    public GridSpec? GridSpec { get; set; }

    public ColorBar? FigureColorBar { get; set; }

    public bool EnableZoomPan { get; set; }

    public bool EnableLegendToggle { get; set; }

    public bool EnableRichTooltips { get; set; }

    public bool EnableHighlight { get; set; }

    public bool EnableSelection { get; set; }

    /// <summary>Returns <see langword="true"/> if any interactive JS feature is enabled.</summary>
    public bool HasInteractivity => EnableLegendToggle || EnableRichTooltips || EnableHighlight || EnableSelection;

    /// <summary>Gets the collection of subplot axes contained in this figure.</summary>
    public IReadOnlyList<Axes> SubPlots => _subPlots;
    private readonly List<Axes> _subPlots = [];

    /// <summary>Adds a new subplot axes to the figure and returns it.</summary>
    /// <returns>The newly created <see cref="Axes"/> instance.</returns>
    public Axes AddSubPlot()
    {
        var axes = new Axes();
        _subPlots.Add(axes);
        return axes;
    }

    /// <summary>Adds a new subplot axes at the specified grid position and returns it.</summary>
    /// <param name="rows">The number of rows in the subplot grid.</param>
    /// <param name="cols">The number of columns in the subplot grid.</param>
    /// <param name="index">The one-based index of this subplot within the grid.</param>
    /// <param name="sharex">Optional axes whose X range this subplot shares.</param>
    /// <param name="sharey">Optional axes whose Y range this subplot shares.</param>
    /// <returns>The newly created <see cref="Axes"/> instance.</returns>
    public Axes AddSubPlot(int rows, int cols, int index, Axes? sharex = null, Axes? sharey = null)
    {
        var axes = new Axes
        {
            GridRows = rows,
            GridCols = cols,
            GridIndex = index,
            ShareXWith = sharex,
            ShareYWith = sharey
        };
        _subPlots.Add(axes);
        return axes;
    }

    /// <summary>Adds a new subplot axes at the specified grid position within a <see cref="Models.GridSpec"/> and returns it.</summary>
    /// <param name="gridSpec">The grid specification defining rows, columns, and optional ratios.</param>
    /// <param name="position">The cell position (and optional span) within the grid.</param>
    /// <param name="sharex">Optional axes whose X range this subplot shares.</param>
    /// <param name="sharey">Optional axes whose Y range this subplot shares.</param>
    /// <returns>The newly created <see cref="Axes"/> instance.</returns>
    public Axes AddSubPlot(GridSpec gridSpec, GridPosition position, Axes? sharex = null, Axes? sharey = null)
    {
        GridSpec ??= gridSpec;
        var axes = new Axes { GridPosition = position, ShareXWith = sharex, ShareYWith = sharey };
        _subPlots.Add(axes);
        return axes;
    }

    /// <summary>Adds a new subplot axes spanning the specified rows and columns within a <see cref="Models.GridSpec"/>.</summary>
    /// <param name="gridSpec">The grid specification defining rows, columns, and optional ratios.</param>
    /// <param name="rowStart">Starting row (0-based, inclusive).</param>
    /// <param name="rowEnd">Ending row (0-based, exclusive).</param>
    /// <param name="colStart">Starting column (0-based, inclusive).</param>
    /// <param name="colEnd">Ending column (0-based, exclusive).</param>
    /// <returns>The newly created <see cref="Axes"/> instance.</returns>
    public Axes AddSubPlot(GridSpec gridSpec, int rowStart, int rowEnd, int colStart, int colEnd) =>
        AddSubPlot(gridSpec, new GridPosition(rowStart, rowEnd, colStart, colEnd));

    /// <summary>Adds an <see cref="Axes"/> instance directly to the subplot list.</summary>
    internal void AddAxes(Axes axes) => _subPlots.Add(axes);
}
