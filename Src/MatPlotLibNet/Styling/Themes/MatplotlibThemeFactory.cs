// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Styling.Themes;

/// <summary>
/// Builds the Matplotlib* themes that mimic Python matplotlib's iconic look. Centralizes the
/// shared font stack and grid defaults so the Classic and V2 themes only differ in the values
/// they actually disagree on (colour cycle, font size, margins and the classic style's own settings).
/// </summary>
internal static class MatplotlibThemeFactory
{
    // Matplotlib's pre-2.0 "classic" 7-color cycle (the legendary bgrcmyk).
    private static readonly Color[] ClassicCycleColors =
    [
        Color.FromHex("#0000FF"), // blue
        Color.FromHex("#008000"), // green
        Color.FromHex("#FF0000"), // red
        Color.FromHex("#00BFBF"), // cyan
        Color.FromHex("#BF00BF"), // magenta
        Color.FromHex("#BFBF00"), // yellow
        Color.FromHex("#000000"), // black
    ];

    // matplotlib states every size and width in POINTS (font.size, axes.linewidth, grid.linewidth, ...); this library draws in
    // pixels. Each value below is matplotlib's number in points turned into pixels by the one rule for it,
    // PointLengthExtensions.PointsInPixels (100 / 72 pixels to the point, the 100 dpi the references are drawn at), so text,
    // spines and grids come out the size matplotlib draws them in PNG/PDF/GIF and SVG output.
    //
    //   classic rcParams: font.size = 12 pt            → 16.67 px
    //                     axes.titlesize = 'large'     → 14.4 pt → 20 px (TitleScale)
    //   v2 (default):     font.size = 10 pt            → 13.89 px
    //                     axes.titlesize = 'large'     → 12 pt → 16.67 px
    //
    // axes.linewidth (matplotlib 3.11: style.use('classic') sets 1.0, the default style 0.8, and matplotlib.spines.Spine.__init__
    // gives every spine that width), so a spine that was given no width is drawn 1 pt or 0.8 pt wide: 1.39 px and 1.11 px, as
    // the references draw them.
    //
    // Agg, which draws matplotlib's pictures, puts a stroke of horizontal and vertical segments on the pixel grid (a whole pixel,
    // half a pixel on when the width rounds to an odd number of pixels, projecting caps on a spine), so both themes ask a raster
    // export for the same of the marks that are one (a spine, the ticks of a rug plot). The pictures of 3.11.2 show it:
    // classic/line.png's left spine is the columns #999999, #000000, #CDCDCD.
    private static readonly double ClassicSpineWidth = 1.0.PointsInPixels;
    private static readonly double V2SpineWidth = 0.8.PointsInPixels;

    // lines.linewidth (matplotlib 3.11: 1.0 in the classic style, 1.5 in the default one): a line made with no width of its own
    // is drawn that wide, 1.39 px and 2.08 px.
    private static readonly double ClassicLineWidth = 1.0.PointsInPixels;
    private static readonly double V2LineWidth = 1.5.PointsInPixels;

    // lines.dashed_pattern, lines.dotted_pattern and lines.dashdot_pattern (printed, plt.style.context): the default style
    // [3.7, 1.6], [1, 1.65] and [6.4, 1.6, 1, 1.6] with lines.scale_dashes True (each length times the width of the line); the classic
    // style [6, 6], [1, 3] and [3, 5, 1, 5] in points with lines.scale_dashes False.
    private static readonly DashLook V2Dashes = new()
    {
        Dashed = [3.7, 1.6],
        Dotted = [1.0, 1.65],
        DashDot = [6.4, 1.6, 1.0, 1.6],
        ScalesWithWidth = true,
    };

    private static readonly DashLook ClassicDashes = new()
    {
        Dashed = [6.0, 6.0],
        Dotted = [1.0, 3.0],
        DashDot = [3.0, 5.0, 1.0, 5.0],
        Unit = 1.0.PointsInPixels,
    };

    // errorbar.capsize (printed under each style: 3.0 in the classic one, 0.0 in the default one): the length in points of a cap beyond
    // its bar; Axes.errorbar draws the cap as a marker 2 * capsize across, so the cap reaches capsize points either side.
    private static readonly double ClassicErrorBarCap = 3.0.PointsInPixels;
    private const double V2ErrorBarCap = 0.0;

    // lines.markeredgewidth (printed under each style: 1.0 in the default one, 0.5 in the classic one), a width in points.
    private static readonly double V2MarkerEdgeWidth = 1.0.PointsInPixels;
    private static readonly double ClassicMarkerEdgeWidth = 0.5.PointsInPixels;

    // axes.titlepad and axes.labelpad (printed under each style: 6.0 / 4.0 pt in the default one, 5.0 / 5.0 pt in the classic one).
    private static readonly double V2TitlePad = 6.0.PointsInPixels;
    private static readonly double V2LabelPad = 4.0.PointsInPixels;
    private static readonly double ClassicTitlePad = 5.0.PointsInPixels;
    private static readonly double ClassicLabelPad = 5.0.PointsInPixels;

    // xtick.minor.width and xtick.minor.pad (printed under each style: 0.6 / 3.4 pt in the default one, 0.5 / 4.0 pt in the classic one).
    private static readonly double V2MinorTickWidth = 0.6.PointsInPixels;
    private static readonly double V2MinorTickPad = 3.4.PointsInPixels;
    private static readonly double ClassicMinorTickWidth = 0.5.PointsInPixels;
    private static readonly double ClassicMinorTickPad = 4.0.PointsInPixels;

    // axes.titlesize 'large' and figure.titlesize ('large' under the default style, 'medium' under the classic one) are sizes
    // relative to font.size (font_manager.font_scalings: 'large' 1.2, 'medium' 1.0); both titles are normal weight
    // (axes.titleweight, figure.titleweight).
    private static readonly MatplotlibFontStack ClassicFontStack =
        new("DejaVu Sans, Bitstream Vera Sans, sans-serif", BaseSize: 12.0.PointsInPixels, SupTitleScale: 1.0);

    private static readonly MatplotlibFontStack V2FontStack =
        new("DejaVu Sans, sans-serif", BaseSize: 10.0.PointsInPixels, SupTitleScale: 1.2);

    private const double TitleScale = 1.2;

    /// <summary><c>Figure.tight_layout</c>'s default <c>pad</c>, in multiples of <c>font.size</c>; its <c>h_pad</c> and <c>w_pad</c> default to it.</summary>
    private const double TightLayoutPad = 1.08;

    // matplotlib's default subplot params (rcParams['figure.subplot.*']): left 0.125, right 0.9, bottom 0.11, top 0.88,
    // so the margins are 0.125, 0.1, 0.12 and 0.11 of the figure; wspace and hspace 0.2, each the gap as a share of the average
    // axes (GridSpecBase.get_grid_positions: cell_w = tot_width / (ncols + wspace * (ncols - 1)), sep_w = wspace * cell_w).
    private static readonly Models.SubPlotSpacing V2Spacing =
        Models.SubPlotSpacing.FromFractions(left: 0.125, right: 0.10, top: 0.12, bottom: 0.11)
            with { HorizontalGapFraction = 0.2, VerticalGapFraction = 0.2 };

    // The classic style's subplot params (plt.style.use('classic')): left 0.125, right 0.9, bottom 0.1, top 0.9, wspace and
    // hspace 0.2.
    private static readonly Models.SubPlotSpacing ClassicSpacing =
        Models.SubPlotSpacing.FromFractions(left: 0.125, right: 0.10, top: 0.10, bottom: 0.10)
            with { HorizontalGapFraction = 0.2, VerticalGapFraction = 0.2 };

    // Matplotlib classic style sets `figure.facecolor = 0.75`, which is RGB(191,191,191) = #BFBFBF.
    // Axes background (axes.facecolor) remains white in classic style.
    private static readonly Color ClassicFigureBackground = Color.FromHex("#BFBFBF");

    // matplotlib classic: patch.edgecolor=k, violin bodies use 'y' (#BFBF00) at alpha=0.3,
    // violin stats lines use 'r' (#FF0000) — empirically confirmed against matplotlib 3.10.8.
    private static readonly Color ClassicPatchEdge   = Color.FromHex("#000000"); // 'k'
    private static readonly Color ClassicViolinBody  = Color.FromHex("#BFBF00"); // 'y'
    private static readonly Color ClassicViolinStats = Color.FromHex("#FF0000"); // 'r'

    /// <summary>The theme of matplotlib's classic style (<c>classic</c>).</summary>
    /// <returns>The theme of matplotlib's classic style.</returns>
    internal static Theme CreateClassic() => new(
        name: "matplotlib-classic",
        background: ClassicFigureBackground,
        foregroundText: Colors.Black,
        axesBackground: Colors.White,
        cycleColors: ClassicCycleColors,
        defaultFont: BuildFont(ClassicFontStack, Colors.Black),
        defaultGrid: ClassicGrid,
        defaultSpacing: ClassicSpacing)
    {
        ViolinBodyColor = ClassicViolinBody,
        ViolinStatsColor = ClassicViolinStats,
        // axes.xmargin = 0, axes.ymargin = 0: data touches the spines exactly.
        AxisXMargin = 0.0,
        AxisYMargin = 0.0,
        // legend.loc = 'upper right' (the default style's is 'best').
        DefaultLegendPosition = LegendPosition.UpperRight,
        Looks = ClassicLooks,
    };

    // text.color, axes.edgecolor, axes.labelcolor, xtick.color and ytick.color: 'black' in the default style; axes.xmargin and
    // axes.ymargin 0.05, legend.loc 'best', the theme's own.
    /// <summary>The theme of matplotlib's default style.</summary>
    /// <returns>The theme of matplotlib's default style.</returns>
    internal static Theme CreateV2() => new(
        name: "matplotlib-v2",
        background: Colors.White,
        foregroundText: Colors.Black,
        axesBackground: Colors.White,
        cycleColors: QualitativeColorMaps.Tab10Cycle(),
        defaultFont: BuildFont(V2FontStack, Colors.Black),
        defaultGrid: V2Grid,
        defaultSpacing: V2Spacing)
    {
        Looks = V2Looks,
    };

    // Axis._set_axisbelow / Axes.set_axisbelow: True gives the axis zorder 0.5, 'line' 1.5, False 2.5.
    private const double V2AxisZOrder = 1.5;
    private const double ClassicAxisZOrder = 2.5;

    // The classic style's 2-D ticks: xtick.direction / ytick.direction 'in', xtick.major.size 4 pt, xtick.major.width 0.5 pt,
    // xtick.major.pad 4 pt, and xtick.top / ytick.right on (marks on the top and right edges, labels off there). The default
    // style's are the library's resting ticks (out, 3.5 pt, 0.8 pt, 3.5 pt, bottom and left only). xtick.minor.width and
    // xtick.minor.pad are 0.5 pt and 4.0 pt in the classic style and 0.6 pt and 3.4 pt in the default one (the minor size, 2 pt, is
    // the same in both): the ticks look's minor width and pad, which a minor tick given none reads.
    private static readonly double ClassicTickSize = 4.0.PointsInPixels;
    private static readonly double ClassicTickWidth = 0.5.PointsInPixels;
    private static readonly double ClassicTickPad = 4.0.PointsInPixels;

    // matplotlib's 3-D axes in classic mode. Axis.__init__ in classic mode: 'axisline': {'linewidth': 0.75, 'color': (0, 0, 0, 1)},
    // a Line2D width in points, where the default style takes axes.linewidth and axes.edgecolor (the spine's width).
    private static readonly double ClassicCubeAxisLine = 0.75.PointsInPixels;

    // axes3d.py, plot_surface, keeps the strides over the counts and defaults them to 10: rstride = kwargs.pop('rstride', 10);
    // compute_strides = has_count.
    private const int ClassicSurfaceStride = 10;

    // axis3d.py, Axis.__init__, does not draw the walls in the style's grid in classic mode:
    // 'grid': {'color': (0.9, 0.9, 0.9, 1), 'linewidth': 1.0, 'linestyle': '-'}, a width in points like every other.
    private static readonly GridStyle ClassicWallGrid = new()
    {
        Visible = false,
        Color = Color.FromHex("#E6E6E6"),
        LineWidth = 1.0.PointsInPixels,
    };

    // axis.Tick.__init__: grid_linewidth = mpl._val_or_rc(grid_linewidth, f"grid.{major_minor}.linewidth", "grid.linewidth"), and
    // the same for the colour, line style and alpha; every grid.minor.* is None in both styles, so a minor line wears grid.*.
    // mpl_toolkits/mplot3d/axes3d.py: plot_surface and plot_trisurf build an art3d.Poly3DCollection(polys, **kwargs) and give
    // it no alpha of their own, so a surface is opaque in both styles unless the call names one.
    private const double OpaqueSurface = 1.0;

    // plot_surface and plot_trisurf name no edge colour, so collections.Collection._set_edgecolor(None) takes
    // patch.edgecolor when rcParams['patch.force_edgecolor'] is on (the classic style) and 'none' when it is off (the
    // default style): a surface is outlined at rest where the theme names a patch edge colour, and in patch.edgecolor (black
    // in both styles) when its author asks for an outline. patch.linewidth is 1.0 pt in both styles.
    private static readonly double PatchLineWidth = 1.0.PointsInPixels;

    // Axes.scatter: s = (20 if mpl.rcParams['_internal.classic_mode'] else rcParams['lines.markersize'] ** 2.0); Axes3D.scatter(…, s=20, …)
    // in every style. A scatter's marker is its edge's width wide beyond the path, so the area is the path's.
    private const double ClassicScatterArea = 20;
    private const double ScatterArea3D = 20;

    // Axes.bxp: capwidths = 0.5 * widths; every boxplot.*props.linewidth is 1.0 pt and a marker is 6 pt across (boxplot.*props.markersize).
    private const double BoxCapShare = 0.5;
    private static readonly double BoxLineWidth = 1.0.PointsInPixels;
    private const double BoxMarkerSize = 6;

    // The default style's colour cycle, which the median (C1) and the mean (C2) of a box name (the theme's own cycle is a copy of it).
    private static readonly Color[] V2Cycle = QualitativeColorMaps.Tab10Cycle();

    // axes3d.depthshade_minalpha: the least opacity depth shading leaves a marker of a 3-D scatter (0.3 in both styles).
    private const double DepthShadeMinAlpha = 0.3;

    private static Font BuildFont(MatplotlibFontStack stack, Color color) => new()
    {
        Family = stack.PrimaryFamily,
        Size = stack.BaseSize,
        Weight = FontWeight.Normal,
        Slant = FontSlant.Normal,
        Color = color,
    };

    // Matplotlib historically ships with the grid OFF; users opt in via plt.grid(True).
    // We mirror that opt-in behaviour so themes are visually faithful out of the box. What each draws when asked for is that
    // style's own grid, which a polar chart's always-drawn rings and spokes wear as well.

    /// <summary>matplotlib's <c>grid.alpha</c>, the same under the classic and the default style: the opacity both of its grids are drawn
    /// with, and the style sheet's default for that parameter (<c>RcParams</c>).</summary>
    internal const double GridAlpha = 1.0;

    // classic rcParams: grid.color black, grid.linestyle ':', grid.linewidth 0.5 pt, grid.alpha (GridAlpha).
    private static readonly GridStyle ClassicGrid = new()
    {
        Visible = false,
        Color = Color.FromHex("#000000"),
        LineStyle = LineStyle.Dotted,
        LineWidth = 0.5.PointsInPixels,
        Alpha = GridAlpha,
    };

    // v2 rcParams: grid.color, grid.linestyle and grid.linewidth (in points) are the plain grid's, grid.alpha GridAlpha. The 3-D walls wear it
    // too (axis3d.py, not in classic mode: 'grid': {'color': rcParams['grid.color'], 'linewidth': rcParams['grid.linewidth'], ...}).
    private static readonly GridStyle V2Grid = GridStyle.Plain with
    {
        LineWidth = GridStyle.Plain.LineWidth.PointsInPixels,
        Alpha = GridAlpha,
    };

    // The looks of the two styles, stated once. They stand last because a static field is initialised in the order it is written,
    // and they read every value above.

    // The default style's looks; the classic style's are these with what classic mode changes.
    private static readonly ThemeLooks V2Looks = new()
    {
        Ticks = new AxisTickLook
        {
            // AutoLocator outside classic mode: nbins = 'auto' (the axis' tick space, at most 9), steps = [1, 2, 2.5, 5, 10].
            Ladder = TickLadder.DefaultStyle,
            MinorWidth = V2MinorTickWidth,
            MinorPad = V2MinorTickPad,
            MinorsByLocator = true,
            // AutoMinorLocator: xtick.minor.ndivs / ytick.minor.ndivs = 'auto' in both styles.
            MinorSplit = AutoMinorLocator.Auto,
            MinorGridLikeMajor = true,
        },
        TickLabels = new TickLabelLook
        {
            // axes.formatter.limits [-5, 6]; axes.formatter.offset_threshold = 4; axes.unicode_minus is True in both styles.
            FormatterLimits = new PowerLimits(-5, 6),
            OffsetThreshold = 4,
            UnicodeMinus = true,
        },
        Range = new AxisRangeLook
        {
            // axes.autolimit_mode = 'data': an axis ends where its data and margin end.
            Autolimit = AutolimitMode.Data,
            // SymmetricalLogScale(linthresh=2): set_yscale('symlog') is linear within 2 of zero under both styles (printed).
            SymLogLinThresh = 2,
            // Axes.add_patch updates the data limits and asks for no autoscale; _update_patch_limits skips a Rectangle of no size
            // (printed: a patch-only axes draws at xlim (0, 1) under both styles).
            PatchesLikeAddPatch = true,
        },
        Frame = new AxesFrameLook
        {
            SpineWidth = V2SpineWidth,
            // axes.axisbelow 'line' in the default style: the axis (its grid) at zorder 1.5 (printed: ax.xaxis.get_zorder() 1.5).
            AxisZOrder = V2AxisZOrder,
            // Every artist is clipped to the axes, limit or none (printed under both styles for plot, bar, scatter and fill_between on an
            // axes that scaled itself: clip_on True, the clip box the axes' bbox; Axes.add_line: line.set_clip_path(self.patch)).
            ClipsEveryArtist = true,
        },
        // The 3-D box's axis lines take axes.linewidth under the default style.
        Cube = new CubeLook { AxisLineWidth = V2SpineWidth },
        Text = new TextLook
        {
            TitleScale = TitleScale,
            SupTitleScale = V2FontStack.SupTitleScale,
            TitleWeight = FontWeight.Normal,
            TitlePad = V2TitlePad,
            LabelPad = V2LabelPad,
            // axes.titley None: _update_title_position lifts the title until its box clears what stands below it (the top axis, a
            // polar axes' labels, the spine).
            TitleOverWhatIsBelow = true,
            SeriesTextAtFontSize = true,
            // text.hinting 'default' (default style) and 'auto' (classic, carried by the classic look's copy of this one): Agg sets a
            // glyph on whole pixels; Text._get_layout takes at least the font's OS/2 sTypoAscender and sTypoDescender for a line.
            MeasuresLikeAgg = true,
            // mathtext.fontset 'dejavusans' in the default style (printed: rcParams['mathtext.fontset']).
            MathFonts = Rendering.MathText.MathFontSet.DejaVuSans,
        },
        // fig.legend() gathers the handles of every axes and leaves none out (printed: two axes that each plot a line labelled L give
        // ['L', 'L'] under both styles); the fixed subplot parameters keep no pad above a title.
        // tight_layout(pad=1.08) in both styles: _auto_adjust_subplotpars keeps pad * font.size at each edge and once in a gap.
        Figure = new FigureLook { LegendListsEveryEntry = true, NoPadAboveTitle = true, TightPad = TightLayoutPad },
        // patch.force_edgecolor off, patch.edgecolor black, patch.linewidth 1 pt; plot_surface gives its faces no alpha (opaque);
        // hatch.color 'edge': the hatch follows the edge of its shape, and a shape with none takes patch.edgecolor (black).
        Patch = new PatchLook
        {
            LikeCollections = true,
            ForceEdge = false,
            EdgeColor = Colors.Black,
            EdgeWidth = PatchLineWidth,
            Alpha = OpaqueSurface,
            Hatch = new HatchColorRule(FollowsEdge: true, Colors.Black),
        },
        // Colorbar: one step per colour, a contour's bar in bands, extendfrac=None and extendrect=False (each extended end a triangle
        // 0.05 of the interior long, outside it), tick marks on the outer side (tick2line, ytick.major.*, ytick.direction).
        ColorBar = new ColorBarLook { TakesRoom = true, StepPerColour = true, ExtendTriangles = true, TickMarks = true },
        ColorMap = new ColorMapLook { LikeLogNorm = true, CountsFromTheLeastDrawn = true },
        // Agg puts a stroke of horizontal and vertical segments on the pixel grid in both styles.
        Raster = new RasterLook { StrokesOnPixelGrid = true },
        // Axes.__clear: self._get_lines and self._get_patches_for_fill, a cycle each.
        Cycle = new CycleLook { Rule = CycleRule.Matplotlib, GroupsWearTheCycle = true },
        Lines = new SeriesLineLook { Width = V2LineWidth, ErrorBarCapSize = V2ErrorBarCap },
        Strokes = new StrokeLook { Dashes = V2Dashes, Join = JoinStyle.Round },
        // matplotlib.markers: '^' fills the unit box, 'D' the unit square turned a quarter. lines.markeredgewidth 1.0 pt, and 'auto'
        // is the colour of the line.
        Markers = new MarkerLook { FillTheirBox = true, LineEdge = new LineMarkerLook(V2MarkerEdgeWidth, null) },
        // scatter.edgecolors = 'face' at patch.linewidth: a marker is stroked in its face colour; s is lines.markersize ** 2 = 36, the
        // resting size, so none; axes3d.depthshade is on, down to axes3d.depthshade_minalpha.
        Scatter = new ScatterLook(Area: null, Area3D: ScatterArea3D, Face: null, EdgeWidth: PatchLineWidth, EdgeColor: null, DepthShade: DepthShadeMinAlpha),
        // boxplot.*props under the default style: a black box, whiskers and caps, a median in C1, open-circle fliers, a C2 triangle mean.
        Box = BoxLook.Matplotlib(box: Colors.Black, median: V2Cycle[1], whisker: Colors.Black, whiskerStyle: LineStyle.Solid, cap: Colors.Black,
            capShare: BoxCapShare, lineWidth: BoxLineWidth, flierMarker: MarkerStyle.Circle, flierFace: null, flierEdge: Colors.Black,
            meanMarker: MarkerStyle.Triangle, meanFace: V2Cycle[2], meanEdge: V2Cycle[2], markerSize: BoxMarkerSize),
        // legend.fontsize 'medium', fancybox True, framealpha 0.8, edgecolor '0.8' (#CCCCCC), numpoints 1, scatterpoints 1.
        Legend = LegendLook.Matplotlib(fontScale: 1.0, fancyBox: true, frameAlpha: 0.8, edgeColor: LegendLook.RestingEdgeColor, numPoints: 1, scatterPoints: 1),
        // seaborn's stripplot and swarmplot pass ax.scatter no alpha (opaque) and a size of 5 points.
        Dots = DotLook.Seaborn,
        // Axes.violinplot draws every line over a body with hlines / vlines in the stats colour, solid, lines.linewidth wide (printed:
        // violinplot(showmeans=True, showmedians=True)['cmeans'] and ['cmedians'] get_linewidth() [1.5] default, [1.0] classic).
        Violin = ViolinLook.Matplotlib,
        // Axes.pie(startangle=0, counterclock=True) and pie_label's 'left' if xt > 0 else 'right' (a donut is a pie with a wedge width).
        Wedges = WedgeLook.Matplotlib,
        // seaborn 0.13.2's verbs over matplotlib: countplot and kdeplot name their value axis 'count' and 'Density' (_add_axis_labels).
        Verbs = SeabornVerbLook.Seaborn,
        // violinplot (mlab.GaussianKDE) and seaborn's kdeplot (scipy's gaussian_kde) smooth with bw_method='scott': std * n ** -0.2.
        Density = DensityLook.Scipy,
        // ax.table: Cell(edgecolor='k', facecolor='w'), cellLoc 'right', rowLoc 'left', Cell.PAD 0.1, a row Table.FONTSIZE * 1.2 pt
        // (printed: 16.67 px at 100 dpi under both styles).
        Table = TableLook.Matplotlib,
        // squarify.plot lays its tiles out with ax.bar from y = 0, in a Y that runs up: the largest tile at the bottom left.
        Treemap = new TreemapLook { LikeSquarify = true },
        // AxesImage.set_extent: the extent (-0.5, cols - 0.5, rows - 0.5, -0.5) of image.origin 'upper', sticky, and set_xlim /
        // set_ylim to it (printed for 170 x 220 under both styles: xlim (-0.5, 219.5), ylim (169.5, -0.5)); image.interpolation 'auto'.
        Image = ImageLook.Imshow with { Interpolation = "auto" },
        // Axes.specgram: the one-sided density of each window (mode='psd'), drawn by imshow on (t[0] - pad, t[-1] + pad, freqs[0], freqs[-1]),
        // pad = (NFFT - noverlap) / Fs / 2, origin 'upper', then axis('auto') (printed for 2 000 samples, NFFT 256, Fs 1000: extent
        // (0.064, 1.856, 0, 500), xlim (0.064, 1.856) default, (0, 2) classic).
        Spectrogram = SpectrogramLook.Specgram,
        // ThetaTick.update_position: a spoke label centred pad + 7 pt out; axes.titley None: the title over the theta labels.
        Polar = PolarLook.Matplotlib,
        // cartopy's GeoAxes: ax.patch is the projection's outline and ax.spines['geo'] draws it (printed for a Robinson map:
        // linewidth 0.8 under the default style, 1.0 under classic, edgecolor black, zorder 2.5).
        Map = new MapLook { LikeCartopy = true },
        // quiver.Quiver.__init__(..., color='k', ...): kwargs.setdefault('facecolors', color); quiver.Barbs.__init__: "Set to facecolor
        // passed in or default to black", kwargs.setdefault('facecolors', 'k'): defaults of the calls, so both styles draw them black. quiver.Barbs:
        // length=7 drawn at sizes = length ** 2 / 4, linewidth 1 (printed under both styles: get_transforms()[0][0, 0] = 4.861 px per
        // unit at 100 dpi). streamplot: FancyArrowPatch(arrowstyle='-|>', mutation_scale=10 * arrowsize), shrinkA = shrinkB = 2.
        VectorField = new VectorFieldLook { Color = Colors.Black, QuiverAutoScale = true, BarbsLikeMatplotlib = true, StreamArrowsLikeMatplotlib = true },
    };

    // The classic style's looks: the default style's, with what plt.style.use('classic') and classic mode change.
    private static readonly ThemeLooks ClassicLooks = V2Looks with
    {
        // The classic style's 2-D ticks: in, 4 pt long, 0.5 pt wide, 4 pt pad, marks on the top and right edges (labels off there);
        // xtick.minor.width 0.5 pt and xtick.minor.pad 4.0 pt. AutoLocator in classic mode: nbins = 9, steps = [1, 2, 5, 10].
        Ticks = V2Looks.Ticks with
        {
            Ladder = TickLadder.Classic,
            Direction = TickDirection.In,
            Length = ClassicTickSize,
            Width = ClassicTickWidth,
            Pad = ClassicTickPad,
            MinorWidth = ClassicMinorTickWidth,
            MinorPad = ClassicMinorTickPad,
            OnAllSides = true,
        },
        // LogFormatter.__init__: "if mpl.rcParams['_internal.classic_mode']: minor_thresholds = (0, 0)", so a log scale with a power of
        // ten in view names the powers alone; axes.formatter.limits [-7, 7]; axes.formatter.offset_threshold = 2.
        TickLabels = V2Looks.TickLabels with { LogThresholds = LogLabelThresholds.Classic, FormatterLimits = new PowerLimits(-7, 7), OffsetThreshold = 2 },
        // axes.autolimit_mode = 'round_numbers': a 2-D and a 3-D axis round out to their outermost ticks. Axes.scatter in classic
        // mode: if self._xmargin < 0.05 and x.size > 0: self.set_xmargin(0.05), and the same for Y.
        Range = V2Looks.Range with { Autolimit = AutolimitMode.RoundNumbers, ScatterMargin = 0.05 },
        // axes.axisbelow False: the axis (its grid) at zorder 2.5, over the lines. set_axis_off() and Axes.pie's set_frame_on(False)
        // draw no axes patch: the figure's grey stands behind an axis-less axes.
        Frame = V2Looks.Frame with { SpineWidth = ClassicSpineWidth, AxisZOrder = ClassicAxisZOrder, NoBackgroundWithoutAxes = true },
        Cube = new CubeLook { WallGrid = ClassicWallGrid, SurfaceStride = ClassicSurfaceStride, AxisLineWidth = ClassicCubeAxisLine },
        // mathtext.fontset 'cm' in the classic style (printed: rcParams['mathtext.fontset'] after plt.style.use('classic')).
        Text = V2Looks.Text with
        {
            SupTitleScale = ClassicFontStack.SupTitleScale,
            TitlePad = ClassicTitlePad,
            LabelPad = ClassicLabelPad,
            MathFonts = Rendering.MathText.MathFontSet.ComputerModern,
            // axes.titley 1.0 in the classic style: the title stays titlepad above the top of the box (a polar title over the 90 degree
            // label), its descent dipping below it.
            TitleOverWhatIsBelow = false,
        },
        // patch.force_edgecolor on, patch.edgecolor 'k'; hatch.color 'k': black whatever the edge.
        Patch = V2Looks.Patch with { ForceEdge = true, EdgeColor = ClassicPatchEdge, Hatch = new HatchColorRule(FollowsEdge: false, Colors.Black) },
        // image.cmap = 'jet'.
        ColorMap = V2Looks.ColorMap with { Default = ColorMaps.ColorMaps.Jet },
        // Axes.scatter takes 'b', fill_between patch.facecolor and violinplot 'y' in classic mode, asking neither cycle.
        Cycle = V2Looks.Cycle with { Rule = CycleRule.MatplotlibClassic },
        // errorbar.capsize: 3 pt in the classic style.
        Lines = new SeriesLineLook { Width = ClassicLineWidth, ErrorBarCapSize = ClassicErrorBarCap },
        Strokes = new StrokeLook { Dashes = ClassicDashes, Join = JoinStyle.Round },
        // lines.markeredgewidth 0.5 pt, and 'auto' gives a filled marker the colour 'k' in classic mode (Line2D.get_markeredgecolor).
        Markers = V2Looks.Markers with { LineEdge = new LineMarkerLook(ClassicMarkerEdgeWidth, Colors.Black) },
        // Axes.scatter in classic mode: s = 20, face "b" (no colour of the cycle taken), edge patch.edgecolor under
        // patch.force_edgecolor; axes3d.depthshade is False in the classic style, so a 3-D scatter fades nothing.
        Scatter = new ScatterLook(Area: ClassicScatterArea, Area3D: ScatterArea3D, Face: Colors.Blue, EdgeWidth: PatchLineWidth, EdgeColor: ClassicPatchEdge, DepthShade: null),
        // boxplot.*props under the classic style: a blue box and dashed whiskers, black caps, a red median, '+' fliers, a red square mean.
        Box = BoxLook.Matplotlib(box: Colors.Blue, median: Colors.Red, whisker: Colors.Blue, whiskerStyle: LineStyle.Dashed, cap: Colors.Black,
            capShare: BoxCapShare, lineWidth: BoxLineWidth, flierMarker: MarkerStyle.Plus, flierFace: null, flierEdge: Colors.Black,
            meanMarker: MarkerStyle.Square, meanFace: Colors.Red, meanEdge: Colors.Black, markerSize: BoxMarkerSize),
        // legend.fontsize 'large' (1.2 of font.size), fancybox False, framealpha None (opaque), edgecolor 'inherit' (the axes' edge,
        // black), numpoints 2, scatterpoints 3.
        Legend = LegendLook.Matplotlib(fontScale: 1.2, fancyBox: false, frameAlpha: 1.0, edgeColor: Colors.Black, numPoints: 2, scatterPoints: 3),
        // image.interpolation 'bilinear' in the classic style.
        Image = V2Looks.Image with { Interpolation = "bilinear" },
    };

    /// <summary>The font of a matplotlib theme: its family, its size in pixels (<c>font.size</c>) and how large a figure title is
    /// drawn against it (<c>figure.titlesize</c>, the one size of the two styles that differs relative to the font).</summary>
    internal readonly record struct MatplotlibFontStack(
        string PrimaryFamily,
        double BaseSize,
        double SupTitleScale);
}
