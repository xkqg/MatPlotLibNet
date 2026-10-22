// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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

    /// <summary>The resolution at which one unit of <see cref="Width"/> and <see cref="Height"/> is one pixel: the figure is
    /// laid out in these units, and a raster export draws <c>Dpi / 96</c> pixels for each of them.</summary>
    internal const int ReferenceDpi = 96;

    /// <summary>The resolution of the figure in dots per inch, 96 unless changed. <see cref="Width"/> and <see cref="Height"/>
    /// are pixels at 96 dpi, so a raster export (PNG, and the frames of an animated GIF) is drawn with <c>Dpi / 96</c>
    /// pixels for each of them: a 500 by 350 figure at 192 dpi is a 1000 by 700 picture with the same layout, text and
    /// line widths, drawn finer. The size of the figure in layout units does not change, so what fits on the page stays
    /// where it was. The pixel count is rounded to a whole number. An SVG, a PDF page and a JSON document have no pixels,
    /// so for them the resolution changes nothing; it is written to the JSON so a figure keeps it. A resolution that is
    /// not a positive, finite number is refused when a raster image is exported. <see cref="SaveOptions.Dpi"/> overrides
    /// it for one save.</summary>
    public double Dpi { get; set; } = ReferenceDpi;

    public Color? BackgroundColor { get; set; }

    public Theme Theme { get; set; } = Theme.MatplotlibV2;

    public SubPlotSpacing Spacing { get; set; } = new();

    public GridSpec? GridSpec { get; set; }

    public ColorBar? FigureColorBar { get; set; }

    /// <summary>Optional chart identifier used by the bidirectional SignalR interaction pipeline.
    /// When set together with <see cref="ServerInteraction"/>, the dispatcher script embedded in
    /// the SVG output routes interaction events back to the server for this id via the
    /// <c>MatPlotLibNet.AspNetCore.FigureRegistry</c> service. Null for pure static output
    /// (the default).</summary>
    public string? ChartId { get; set; }

    /// <summary>When <see langword="true"/>, the SVG output embeds the SignalR interaction
    /// dispatcher script instead of the local client-side <c>Svg*Script</c> IIFEs, so zoom / pan /
    /// reset / legend-toggle events flow to the server for authoritative mutation + re-render.
    /// Default <see langword="false"/> — existing behaviour is byte-identical.</summary>
    public bool ServerInteraction { get; set; }

    public bool EnableZoomPan { get; set; }

    public bool EnableLegendToggle { get; set; }

    public bool EnableRichTooltips { get; set; }

    public bool EnableHighlight { get; set; }

    public bool EnableSelection { get; set; }

    /// <summary>Whether a click on a data point is reported to the server as a data-cursor event. Server
    /// interaction only; the native controls raise their own <c>DataPointClicked</c> event in process.</summary>
    public bool EnableDataCursor { get; set; }

    /// <summary>When true, embeds a JavaScript 3D rotation handler in the SVG output.</summary>
    public bool Enable3DRotation { get; set; }

    /// <summary>When true, embeds a JavaScript drilldown handler so clicking a treemap
    /// rectangle zooms into its subtree; Escape zooms out. Data attributes are emitted on
    /// every treemap rect so the script can navigate the hierarchy.</summary>
    public bool EnableTreemapDrilldown { get; set; }

    /// <summary>When true, embeds the Sankey hover emphasis script in the SVG output.
    /// Hovering a node dims every link not reachable upstream or downstream from that node
    /// (mirrors ECharts' <c>focus: adjacency</c> behaviour). <c>SankeySeriesRenderer</c>
    /// emits <c>data-sankey-node-id</c>, <c>data-sankey-link-source</c>, and
    /// <c>data-sankey-link-target</c> attributes so the script can traverse the topology
    /// without re-parsing the SVG structure.</summary>
    public bool EnableSankeyHover { get; set; }

    /// <summary>Returns <see langword="true"/> if any interactive JS feature is enabled.</summary>
    public bool HasInteractivity => EnableLegendToggle || EnableRichTooltips || EnableHighlight || EnableSelection || EnableDataCursor || Enable3DRotation || EnableTreemapDrilldown || EnableSankeyHover || ServerInteraction;

    /// <summary>Configurable opacity / transition tokens for the embedded interaction
    /// scripts. Defaults to <see cref="InteractionTheme.Default"/> (the values the scripts hard-coded
    /// before they were configurable). Set it through <c>FigureBuilder.WithInteractionTheme</c>.</summary>
    public InteractionTheme InteractionTheme { get; set; } = InteractionTheme.Default;

    /// <summary>When <see langword="true"/> (default) the SVG root element carries an
    /// inline <c>style="max-width:100%;height:auto"</c> declaration so the chart resizes
    /// fluidly with its container while the <c>viewBox</c> preserves aspect ratio. The
    /// pixel <c>width</c> / <c>height</c> attributes stay on the element so
    /// <c>naturalWidth</c> / <c>naturalHeight</c> (needed for client-side PNG export)
    /// remain accurate.
    /// <para>Set to <see langword="false"/> to leave the declaration out, for the same bytes the
    /// library wrote before it carried one — e.g., test fixtures that pixel-diff the SVG string.</para></summary>
    public bool ResponsiveSvg { get; set; } = true;

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
