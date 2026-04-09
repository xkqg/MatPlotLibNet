// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models;

/// <summary>Represents the top-level figure that contains one or more subplot axes.</summary>
public sealed class Figure
{
    /// <summary>Gets or sets the figure title.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets the figure width in pixels.</summary>
    public double Width { get; set; } = 800;

    /// <summary>Gets or sets the figure height in pixels.</summary>
    public double Height { get; set; } = 600;

    /// <summary>Gets or sets the dots-per-inch resolution of the figure.</summary>
    public double Dpi { get; set; } = 96;

    /// <summary>Gets or sets the background color of the figure.</summary>
    public Color? BackgroundColor { get; set; }

    /// <summary>Gets or sets the visual theme applied to the figure.</summary>
    public Theme Theme { get; set; } = Theme.Default;

    /// <summary>Gets or sets the subplot spacing configuration (margins and gaps).</summary>
    public SubPlotSpacing Spacing { get; set; } = new();

    /// <summary>Gets or sets the grid specification for advanced subplot layouts with ratios and spanning.</summary>
    public GridSpec? GridSpec { get; set; }

    /// <summary>Gets or sets whether interactive zoom and pan via JavaScript is enabled in SVG output.</summary>
    /// <remarks>When enabled, a <c>&lt;script&gt;</c> block is injected into the SVG document that handles
    /// mouse-wheel zoom and click-drag pan via viewBox manipulation. Has no effect on raster transforms (PNG, PDF).
    /// Double-click resets to the original view.</remarks>
    public bool EnableZoomPan { get; set; }

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
        AddSubPlot(gridSpec, GridPosition.Span(rowStart, rowEnd, colStart, colEnd));

    internal void AddAxes(Axes axes) => _subPlots.Add(axes);
}
