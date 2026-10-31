// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models;

/// <summary>Represents the top-level figure that contains one or more subplot axes.</summary>
public sealed class Figure
{
    /// <summary>The title centred across the top of the figure, above every subplot (matplotlib's suptitle); the layout
    /// makes room for it. <see langword="null"/> (the default) draws none. A title with a pair of <c>$</c> delimiters is
    /// typeset as math.</summary>
    public string? Title { get; set; }

    /// <summary>Short alternative text for the chart, rendered as the SVG <c>&lt;title&gt;</c> element (falls back to <see cref="Title"/>).</summary>
    public string? AltText { get; set; }

    /// <summary>Longer description rendered as the SVG <c>&lt;desc&gt;</c> element and linked via <c>aria-describedby</c>.</summary>
    public string? Description { get; set; }

    /// <summary>The width of the figure in layout units, which are pixels at 96 dpi (see <see cref="Dpi"/>). Default 800.</summary>
    public double Width { get; set; } = 800;

    /// <summary>The height of the figure in layout units, which are pixels at 96 dpi (see <see cref="Dpi"/>). Default 600.</summary>
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

    /// <summary>The colour the whole figure is filled with. <see langword="null"/> (the default) takes the theme's
    /// background colour.</summary>
    public Color? BackgroundColor { get; set; }

    /// <summary>The theme that styles the figure: its fonts, colours, grid, spines and default spacing. Default
    /// <see cref="Styling.Theme.MatplotlibV2"/>.</summary>
    public Theme Theme { get; set; } = Theme.MatplotlibV2;

    /// <summary>The margins around the subplots, the gaps between them and the automatic-layout switches. Left at its
    /// default, a theme that defines its own spacing supplies it.</summary>
    public SubPlotSpacing Spacing { get; set; } = new();

    /// <summary>The grid the subplots are laid out on, with its optional row and column ratios. <see langword="null"/>
    /// (the default) lays each subplot out by the rows-by-columns cell it names itself. Set by the first subplot added
    /// through a grid specification.</summary>
    public GridSpec? GridSpec { get; set; }

    /// <summary>A colour bar for the figure as a whole, drawn outside the area all the subplots cover.
    /// <see langword="null"/> (the default) draws none; a bar that is not <see cref="ColorBar.Visible"/> is not drawn
    /// either.</summary>
    public ColorBar? FigureColorBar { get; set; }

    /// <summary>The texts placed on the figure itself (matplotlib's <c>figtext</c>), in the order they were added; they are
    /// drawn above the subplots and the figure's colour bar, the later over the earlier. Empty by default.</summary>
    public IReadOnlyList<FigureText> Texts => _texts;
    private readonly List<FigureText> _texts = [];

    /// <summary>Adds a text to the figure at a place given as fractions of the figure, counted from its bottom-left corner,
    /// and returns it so its font, colour, alignment and rotation can be set.</summary>
    /// <param name="text">The text to draw.</param>
    /// <param name="x">The horizontal position as a fraction of the figure's width, from the left.</param>
    /// <param name="y">The vertical position as a fraction of the figure's height, from the bottom.</param>
    /// <returns>The new <see cref="FigureText"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
    public FigureText AddText(string text, double x, double y)
    {
        var figureText = new FigureText(text, x, y);
        _texts.Add(figureText);
        return figureText;
    }

    /// <summary>A legend for the figure as a whole (matplotlib's <c>figlegend</c>): it lists the labelled series of every subplot,
    /// in subplot order, each entry that looks the same once, however many subplots show it (the rule that decides which series
    /// of an axes have an entry decides here too). It stands in the rectangle that is the figure, at the place
    /// <see cref="Models.Legend.Position"/> names; <see cref="LegendPosition.Best"/> looks for room among the marks of one axes,
    /// which a figure has none of, so here it is the upper right. It reserves no room, draws above the subplots, the colour bar
    /// and the figure's texts, and is static: clicking an entry hides nothing. <see langword="null"/> (the default) draws none;
    /// a legend that is not <see cref="Models.Legend.Visible"/>, or that has no entry to list, is not drawn either.</summary>
    public Legend? Legend { get; set; }

    /// <summary>The images placed on the figure itself (matplotlib's <c>figimage</c>), in the order they were added; they are drawn
    /// under the subplots, the later over the earlier. Empty by default.</summary>
    public IReadOnlyList<FigureImage> Images => _images;
    private readonly List<FigureImage> _images = [];

    /// <summary>Adds an image to the figure at a place given in pixels from its bottom-left corner, and returns it so its colour
    /// map, normalizer, colour range and opacity can be set.</summary>
    /// <param name="data">The pixel values, indexed [row, column]; the first row is the top row of the image.</param>
    /// <param name="xOffset">The distance from the left edge of the figure to the left edge of the image, in pixels.</param>
    /// <param name="yOffset">The distance from the bottom edge of the figure to the bottom edge of the image, in pixels.</param>
    /// <returns>The new <see cref="FigureImage"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="data"/> is <see langword="null"/>.</exception>
    public FigureImage AddImage(double[,] data, double xOffset = 0, double yOffset = 0)
    {
        var image = new FigureImage(data, xOffset, yOffset);
        _images.Add(image);
        return image;
    }

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

    /// <summary>When true, the SVG output embeds a script that zooms with the mouse wheel or keyboard and pans by dragging
    /// or with the arrow keys. Off by default.</summary>
    public bool EnableZoomPan { get; set; }

    /// <summary>When true, the SVG output embeds a script that hides or shows a series when its legend entry is clicked,
    /// and lets the legend be dragged. Off by default.</summary>
    public bool EnableLegendToggle { get; set; }

    /// <summary>When true, the SVG output embeds a script that shows styled HTML tooltips in place of the browser's native
    /// ones. Off by default.</summary>
    public bool EnableRichTooltips { get; set; }

    /// <summary>When true, the SVG output embeds a script that dims the other series while the pointer or keyboard focus is
    /// on one. Off by default.</summary>
    public bool EnableHighlight { get; set; }

    /// <summary>When true, the SVG output embeds a script that draws a selection rectangle when the reader drags with
    /// Shift held down. Off by default.</summary>
    public bool EnableSelection { get; set; }

    private string? _selectionGroup;
    private IReadOnlyList<string>? _selectedRows;

    /// <summary>The name that links this chart to the others of the same data on a page: a brush on one chart lights the same rows
    /// (<c>XYSeries.RowKeys</c>) on every chart that shares the name, through the window (<c>mpl:rows</c>), with no server in
    /// between. <see langword="null"/> (the default) links to nothing. The brush that selects the rows is
    /// <see cref="EnableSelection"/>, which <c>FigureBuilder.LinkSelection</c> switches on together with the group.</summary>
    /// <exception cref="ArgumentException">The name is blank.</exception>
    public string? SelectionGroup
    {
        get => _selectionGroup;
        set => _selectionGroup = value is null || !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new ArgumentException("A selection group needs a name; null links to nothing, a blank name is a mistake.", nameof(SelectionGroup));
    }

    /// <summary>The rows (<c>XYSeries.RowKeys</c>) this chart starts with selected: the application's own state, restored with the
    /// chart and carried in the JSON. What the reader brushes afterwards is theirs and stays in their browser.
    /// <see langword="null"/> (the default) starts with nothing selected.</summary>
    /// <exception cref="ArgumentException">A key is blank; the rows the figure had are kept.</exception>
    public IReadOnlyList<string>? SelectedRows
    {
        get => _selectedRows;
        set => _selectedRows = value is null || value.All(key => !string.IsNullOrWhiteSpace(key))
            ? value
            : throw new ArgumentException("A selected row is the key of a row, and no key is blank.", nameof(SelectedRows));
    }

    /// <summary>The view each axes had before an interaction first moved it: where Home goes back to
    /// (<see cref="FigureHome"/>). Session state, not part of the drawing, so it is neither settable nor serialised.</summary>
    internal FigureHome Home { get; } = new();

    /// <summary>Whether a click on a data point is reported to the server as a data-cursor event. Server
    /// interaction only; the native controls raise their own <c>DataPointClicked</c> event in process.</summary>
    public bool EnableDataCursor { get; set; }

    /// <summary>Whether the viewers of a server-interactive chart see each other's pointers: the page sends where its
    /// pointer is, the server relays it to the other viewers of the chart, and the page draws theirs with the name the
    /// chart gave them. Needs <see cref="ServerInteraction"/>; off by default, because a moving pointer is a message
    /// every fifty milliseconds to every other viewer.</summary>
    public bool EnableViewerCursors { get; set; }

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
    public bool HasInteractivity => EnableLegendToggle || EnableRichTooltips || EnableHighlight || EnableSelection || SelectionGroup is not null || EnableDataCursor || EnableViewerCursors || Enable3DRotation || EnableTreemapDrilldown || EnableSankeyHover || ServerInteraction;

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
