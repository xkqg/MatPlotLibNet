// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Packaging.OwnedRules;

/// <summary>The rules of drawing an axes into its figure: the title of an axis, the figure's ground, the place of a legend, a
/// table's cells, the layout's margins, gaps and wrapped panels, what an edge means, the passes a series is drawn in.</summary>
internal static class RenderingRules
{
/// <summary>A theme that may be null, and the sites that decide what null means: a nullable theme parameter or field, a fallback on
    /// it, a member read through it that may be null, a test of it against null or of its looks through a pattern that is false for
    /// null.</summary>
    internal const string NoThemePattern = @"\bTheme\?\s+\w+|\b[tT]heme(?:d)?\s*\?\?|\b[tT]heme\?\.|\b[tT]heme is (?:not )?null\b|\b[tT]heme is \{";

/// <summary>The title an axis draws is the label it was given, else the name a seaborn verb gives the axis its values run along under
    /// a theme whose verbs name it: one rule, <c>AxesTitleExtensions.TitleOf</c>, which every renderer and the layout read, so what is
    /// drawn is what room is kept for, on every edge, in 2-D, 3-D and ternary axes alike.</summary>
    private static readonly OwnedRule TitleOfAnAxis = new(
        "the title an axis draws is AxesTitleExtensions.TitleOf (the given label, else the name a seaborn verb gives its value axis): no renderer or layout reads an axis' Label itself",
        @"\b\w*[Aa]xis\.Label\b",
        "Src/MatPlotLibNet/Rendering/AxesTitleExtensions.cs") { Under = "Src/MatPlotLibNet/Rendering/", Finds = [
        "        if (Axes.XAxis.Label is not null)"] };

/// <summary>The figure's ground: the background the figure was given, else its theme's; the figure's own background and the room between
    /// the parts of a broken axis (which shows the ground, as the gap between matplotlib's subplots does) read the one owner.</summary>
    private static readonly OwnedRule FigureGround = new(
        "the figure's ground is Figure.Ground (the given background, else the theme's): no site resolves the figure's background itself",
        @"\bBackgroundColor\s*\?\?",
        "Src/MatPlotLibNet/Models/Figure.cs") { Under = "Src/", Finds = [
        "        var bgColor = figure.BackgroundColor ?? theme.Background;"] };

/// <summary>The place a legend stands under its theme: the place it names, or, for one left at Best, the theme's own
    /// (matplotlib's <c>legend.loc</c>). <c>Legend.PositionUnder</c> says it; the layout of the box, the renderer's collection of the
    /// marks, the figure legend's place and the room the constrained layout keeps for a legend outside the plot all ask it, so none
    /// reads a legend's raw position or the theme's default place itself, and none hands a strategy lookup a raw position.</summary>
    /// <remarks>The wire form of a legend writes and reads the position it holds (LegendDto), a theme holds, copies, writes and
    /// reads its own default place (Theme, ThemeDto, the matplotlib theme factory), and the figure's legend compares the place it
    /// resolved with the one the legend holds, to keep the legend itself when they are the same (FigureLegendExtensions).</remarks>
    private static readonly OwnedRule LegendPlaceUnderTheme = new(
        "the place a legend stands under its theme is Legend.PositionUnder (the place it names, else the theme's own): no reader of a legend's place reads Legend.Position or Theme.DefaultLegendPosition itself, and no strategy lookup is handed a raw position",
        @"\b[Ll]egend\.Position\b|\bDefaultLegendPosition\b|\bLegendPositionStrategyFactory\s*\.Create\((?![^;]*PositionUnder\()",
        "Src/MatPlotLibNet/Rendering/LegendRendering/LegendLookExtensions.cs", "Src/MatPlotLibNet/Serialization/LegendDto.cs",
        "Src/MatPlotLibNet/Styling/Theme.cs", "Src/MatPlotLibNet/Serialization/ThemeDto.cs",
        "Src/MatPlotLibNet/Styling/Themes/MatplotlibThemeFactory.cs", "Src/MatPlotLibNet/Rendering/LegendRendering/FigureLegendExtensions.cs") { Under = "Src/", Finds = [
        "var strategy = LegendPositionStrategyFactory.Create(legend.Legend.Position);",
        "if (!axes.Legend.Visible || axes.Legend.Position != edge) continue;",
        "=> legend.Position == LegendPosition.Best ? _theme.DefaultLegendPosition : legend.Position;",
        "var strategy = LegendPositionStrategyFactory.Create(place);"], Ignores = [
        "var strategy = LegendPositionStrategyFactory.Create(legend.PositionUnder(theme));",
        "var place = legend.PositionUnder(theme);"] };

/// <summary>The size of a marker in a legend handle is one rule, <c>LegendLook.MarkerSizeIn</c>: the size the series or its
    /// slot gives, at least a point, no more than the handle is high. Each kind of handle chooses only which size it hands to it, so
    /// the legend's drawing never floors a point size by hand.</summary>
    private static readonly OwnedRule LegendMarkerSize = new(
        "the size of a marker in a legend handle (at least one point, no more than the handle is high) is LegendLook.MarkerSizeIn: no handle floors a point size and converts it to pixels itself",
        @"\bMath\.Max\([^;]*,\s*1\)\.PointsInPixels\b",
        "Src/MatPlotLibNet/Styling/LegendLook.cs") { Under = "Src/", Finds = [
        "double size = Math.Max(swatch.MarkerSize ?? 0, 1).PointsInPixels;",
        "Math.Min(h, Math.Max(slot.ScatterSizeOf(swatch.MarkerSize), 1).PointsInPixels), ShapeStyle.Filled(color));"] };

/// <summary>Where the cells of a table stand on a plot (in the plot from its top, or below the axes as matplotlib's <c>ax.table</c>
    /// stands them, the row headers left of the plot), how wide each column is, how high a row is and the size its text is fitted to:
    /// TableGrid works it out and the drawing and the layout both read it, so the room the layout keeps is where the cells are drawn. A site
    /// that reads where a table stands, a row's height or a fitted width off the table's look itself decides it a second time.</summary>
    private static readonly OwnedRule TableGridRule = new(
        "where a table's cells stand, how high a row is, whether it stretches with its axes and how wide a fitted column is are TableGrid's (TableGrid.Of, TextSize, WithTablesOf, AnyStretchIn), read off the table's look (TableLook.LocationOver, CellHeightOver, Stretches, FittedWidth): no renderer or layout reads them off the look itself",
        @"\.\s*(?:LocationOver|CellHeightOver|Stretches|FittedWidth)\s*\(",
        "Src/MatPlotLibNet/Rendering/SeriesRenderers/Special/TableGrid.cs", "Src/MatPlotLibNet/Models/Series/Special/TableSeries.cs") { Under = "Src/", Finds = [
        "        double rowH = look.CellHeightOver(series.GivenCellHeight);",
        "        bool below = theme.Looks.Table.LocationOver(table.Location) == TableLocation.BelowAxes;",
        "            double labels = look . FittedWidth(widest, null);",
        "        double hung = rows * theme.Looks.Table.CellHeightOver (null);",
        "            if (look.Stretches(table.GivenCellHeight)) twice = true;"], Ignores = [
        "        double rowH = grid.RowHeight;",
        "        double pad = look.PadOver(series.GivenCellPadding, colW);",
        "        var cell = look.CellHeight;",
        "        var legend = axes.Legend.LocationOverride;",
        "        double width = TableGrid.Of(table, plot, ctx, theme) is { } grid ? grid.Bottom : 0;"] };

/// <summary>Where the subplot parameters put each subplot, which what keeps a share of its axes' height (a table's rows) was made on, is
    /// kept by the draw (<c>FigureRanges.MadeOn</c>, set by <c>ChartRenderer.FrameOf</c>) and read only by <c>FigureRanges.StretchOf</c>, the
    /// one rule for how far a layout stretched an axes: no renderer or layout divides by those heights itself.</summary>
    private static readonly OwnedRule MadeOnRule = new(
        "where the subplot parameters put each subplot is the draw's (FigureRanges.MadeOn, set by ChartRenderer.FrameOf) and how far a layout stretched an axes from it is FigureRanges.StretchOf: no renderer or layout reads those plots itself",
        @"\.\s*MadeOn\b",
        "Src/MatPlotLibNet/Rendering/FigureRanges.cs", "Src/MatPlotLibNet/Rendering/ChartRenderer.cs") { Under = "Src/", Finds = [
        "        double stretch = Area.PlotBounds.Height / ranges.MadeOn![index].Height;",
        "        bool twice = figure.Spacing.ConstrainedLayout && ranges?.MadeOn is not null;",
        "        var made = ranges . MadeOn;"], Ignores = [
        "        renderer.Stretch = drawn is { } laidOut && ranges is { } held ? held.StretchOf(axes, laidOut[index].Height) : 1;",
        "        var made = SubplotParameters(figure, grid);",
        "        double madeOn = plot.Height;",
        "            twice: figure.Spacing.ConstrainedLayout && ranges is { Stretches: true });"] };

/// <summary>What an edge an axis hangs its ticks from means: whether it runs along X, which way is out, where it stands beside the
    /// plot, which spine it meets, how a label is anchored upright and turned, where a label and the offset text stand, how far a
    /// turned label reaches away from it, which ink of a title faces its labels. EdgeTerms holds it per edge; a site that compares or
    /// switches on a TickSide, or turns an AxisSide into an edge, a spine or an ink, decides it a second time.</summary>
    private static readonly OwnedRule EdgeMeaning = new(
        "what an edge an axis hangs its ticks from means (along X, which way is out, its spine, how a label hangs and where it stands, the ink of a title beside it) is EdgeTerms: no site compares or switches on a TickSide, or turns an AxisSide into an edge, a spine or an ink",
        @"(?:\bis|==|!=)\s+(?:not\s+)?TickSide\.\w+|\bTickSide\.\w+\s*=>|\bcase\s+TickSide\.|\bTickSide\.\w+\s+or\b|==\s*AxisSide\.\w+\s*\?\s*(?:TickSide|SpineEdge|EdgeTerms)\.",
        "Src/MatPlotLibNet/Rendering/EdgeTerms.cs") { Under = "Src/MatPlotLibNet/", Finds = [
        "bool alongX = draw.Side is TickSide.Bottom or TickSide.Top;",
        "double labelY = draw.Side == TickSide.Bottom",
        "    case TickSide.Left:",
        "    TickSide.Top => new FacingInk(Descent, Ascent),",
        ": _slot.Side == AxisSide.Left ? SpineEdge.Left : SpineEdge.Right;"] };

/// <summary>A tick drawing is hung from its edge in one place: the side, the edge, half the spine on it and how a label is
    /// anchored at the rotation it is drawn at.</summary>
    private static readonly OwnedRule EdgeHanging = new(
        "a tick drawing is turned to face its edge (EdgeTerms.Facing: the side and how a label is anchored at its rotation) and hung from it (EdgeTerms.Hang: the edge and half the spine on it) in one place: no site sets them by hand",
        @"\bSpineHalf\s*=\s*\w|\bAlignment\s*=\s*[^;]*(?:[Rr]otation|LabelAlignment)|[Rr]otation\s*==\s*0\s*\?\s*TextAlignment\.",
        "Src/MatPlotLibNet/Rendering/EdgeTerms.cs") { Under = "Src/MatPlotLibNet/Rendering/", Finds = [
        "SpineHalf     = _axes.Spines.Bottom.HalfWidthIn(_theme),",
        "Alignment     = rotation == 0 ? TextAlignment.Center : TextAlignment.Left,",
        "Alignment     = Terms.LabelAlignment,"] };

/// <summary>Where the need of an axes goes: the margin of an edge of the grid when its cell touches that edge, the gap beside it
    /// otherwise, and the needs that meet in a gap stacked. AxisMargins decides it for one direction of the grid, on both layout paths;
    /// a loop that reads which edges a cell touches and widens a margin or a gap itself decides it a second time.</summary>
    private static readonly OwnedRule EdgeElseGap = new(
        "an axes' need widens the margin of the edge its cell touches, or the gap beside it, in AxisMargins: no layout loop reads which edges a cell touches to widen a margin or a gap itself",
        @"\bif\s*\(\s*!?\s*(?:cell|at)\.|\b(?:cell|at)\.(?:Left|Right|Top|Bottom)\b",
        "Src/MatPlotLibNet/Rendering/Layout/ConstrainedLayoutEngine.AxisMargins.cs") { Under = "Src/MatPlotLibNet/Rendering/Layout/", Finds = [
        "            if (cell.Left   && m.Left  > left)  left  = m.Left;",
        "            if (!cell.Right && m.Right > hGap)  hGap  = m.Right;",
        "                if (at.Right) right = Math.Max(right, reach + _pads.Right + room.Right);",
        "            if (cell.Right) lastColumnPast = Math.Max(lastColumnPast, rightBand.Past);",
        "                if (cell.Top) maxTopBand = Math.Max(maxTopBand, supReserved + m.TopBand);",
        "        double right = cell.Right ? Math.Max(right, band) : right;"] };

/// <summary>Whether a subplot stands on an end edge of the grid: AxisMargins reads it for the margins and the gaps, and
    /// FigureGrid.StandsAtBarEdge for the edge the figure's colour bar stands at, which the bar stands clear of and which an outside
    /// legend there stands past it on. A site that reads a cell's edge along a direction itself decides it a second time, whatever the
    /// edges are called.</summary>
    private static readonly OwnedRule CellEdgeAlong = new(
        "whether a subplot's cell is on an end edge of the grid along a direction is read by AxisMargins (margins and gaps) and FigureGrid.StandsAtBarEdge (the figure's colour bar): no other site reads a cell's edge along a direction",
        @"\bEdgeOf\([^()]*\)\s*\.\s*(?:Start|End)Along\(|\b(?:at|cell|edges?)\s*\.\s*(?:Start|End)Along\(",
        "Src/MatPlotLibNet/Rendering/Layout/ConstrainedLayoutEngine.AxisMargins.cs", "Src/MatPlotLibNet/Rendering/Layout/FigureGrid.cs") { Under = "Src/", Finds = [
        "                if (!at.EndAlong(bar.Beside)) continue;",
        "        if (index < 0 || !_grid.EdgeOf(index).EndAlong(bar.Beside)) return 0;",
        "            if (cells.EdgeOf(i).EndAlong(figureBar.Beside)) room = Math.Max(room, past);",
        "        bool first = grid.EdgeOf( i ) . StartAlong(true);",
        "        bool last = edges.EndAlong(alongX);",
        "        if (cell.StartAlong(false)) top = band;"], Ignores = [
        "            edge = Math.Max(edge, plots[i].EndAlong(bar.Beside));",
        "        past.WithEndAlong(bar.Beside, Math.Max(past.EndAlong(bar.Beside), reach));",
        "        return ReserveOutsideLegend(axes, ctx, theme, edge, edge).EndAlong(bar.Beside);",
        "            if (cells.StandsAtBarEdge(i, bar)) room = Math.Max(room, RoomPastBar(figure.SubPlots[i], ctx, figure.Theme, bar, past));",
        "        return Rect.Along(alongX, along.Lo, box.Length, whole.StartAlong(!alongX) + whole.LengthAlong(!alongX), box.Thickness);"] };

/// <summary>Whether a draw is laid out by a layout that keeps room for what is drawn (the tight or the constrained one) is said once,
    /// by the draw's ranges: the layout path, the stretch of a table's rows and the outside legend beside a colour bar read it. A site that
    /// joins the two settings itself decides it a second time.</summary>
    private static readonly OwnedRule LayoutKeepsRoom = new(
        "whether the draw's layout keeps room (tight or constrained) is FigureRanges.KeepsRoom: no other site joins TightLayout and ConstrainedLayout",
        @"\bTightLayout\s*(?:\|\||&&)\s*!?\s*[\w.?]*\bConstrainedLayout\b|\bConstrainedLayout\s*(?:\|\||&&)\s*!?\s*[\w.?]*\bTightLayout\b",
        "Src/MatPlotLibNet/Rendering/FigureRanges.cs") { Under = "Src/", Finds = [
        "        if ((figure.Spacing.TightLayout || figure.Spacing.ConstrainedLayout) && SeriesRenderers.TableGrid.AnyStretchIn(figure))",
        "        if (figure.Spacing.TightLayout || figure.Spacing.ConstrainedLayout)",
        "        bool keeps = spacing.ConstrainedLayout || spacing.TightLayout;",
        "        if (!s.TightLayout && !s.ConstrainedLayout) return past;",
        "        var measured = figure?.Spacing.TightLayout||figure?.Spacing.ConstrainedLayout;"], Ignores = [
        "        Resolved(figure.Spacing with { TightLayout = false, ConstrainedLayout = false, LayoutRect = null }, figure, grid);",
        "        figure.Spacing.TightLayout && figure.Theme.Looks.Figure.TightPad is { } pad",
        "            if (!figure.Spacing.ConstrainedLayout) return room;",
        "        if (ranges.KeepsRoom) return engine.Compute(figure, measureCtx, legends);"] };

/// <summary>How far a margin or a gap may shrink and grow: a floor and a cap (which what an axes is measured to draw there lifts),
    /// the library's twelve numbers or none under matplotlib's tight layout. MarginBounds clamps; a clamp or a cap typed in the layout
    /// decides it a second time.</summary>
    private static readonly OwnedRule MarginClamp = new(
        "a margin or a gap is held between its floor and its cap by MarginBounds.Clamp, with the numbers of LayoutBounds: no layout clamps a margin or types a cap itself",
        @"\bMath\.Clamp\(|\bMath\.Max\(\s*[0-9]",
        "Src/MatPlotLibNet/Rendering/Layout/MarginBounds.cs") { Under = "Src/MatPlotLibNet/Rendering/Layout/", Finds = [
        "        double maxLeft = Math.Max(Math.Max(120, maxLeftBand), OutsideLegendCap(figure, ctx, LegendPosition.OutsideLeft));",
        "            MarginLeft    = Math.Clamp(left,  30, maxLeft) + room.Left,",
        "            VerticalGap  = Math.Clamp(vGap,   0, maxVGap),",
        "        double maxHGap = maxNonRightBand > 0 || maxNonLeftBand > 0 ? Math.Max(150, stackedH) : 150;"] };

/// <summary>How many rows and columns a number of panels takes in a grid that wraps them (filled row by row, or balanced so no
    /// row is much shorter than the others), and the cell the panel at an index takes, row by row: PanelWrap works them out for a
    /// facet grid, small multiples, the tiles of an ops dashboard and the cells a subplot index names.</summary>
    private static readonly OwnedRule PanelWrapOfABuilder = OwnedRule.Nowhere(
        "the rows and columns a wrapped grid of panels takes and the cell of each panel are PanelWrap's (Filled, Balanced, At): no figure builder divides a panel count or an index by its columns itself",
        @"\b\w+\s*[/%]\s*_?(?:num)?(?:[cC]ols|[cC]olumns|maxCols|MaxCols|MaxTilesPerRow)\b|-\s*1\s*\)\s*/\s*_?(?:rows|maxCols|MaxTilesPerRow|numCols)\b",
        under: "Src/MatPlotLibNet/Builders/") with { Finds = [
        "int numRows  = (numCats + numCols - 1) / numCols;",
        "figure.AddSubPlot(GridPosition.Single(i / columns, i % columns), tile);",
        "int rows = ((_panels.Count - 1) / _maxCols) + 1;",
        "int tileRows = ((_tiles.Count - 1) / MaxTilesPerRow) + 1;"] };

/// <summary>The cell a subplot index names in the figure's grid is PanelWrap's too.</summary>
    private static readonly OwnedRule PanelWrapOfTheLayout = OwnedRule.Nowhere(
        "the cell a subplot index names in a grid of rows and columns is PanelWrap.At: the layout divides no index by its columns itself",
        @"\b\w+\s*[/%]\s*Cols\b",
        under: "Src/MatPlotLibNet/Rendering/Layout/") with { Finds = [
        "return GridPosition.Single(at / Cols, at % Cols);"] };

    /// <summary>The rules of the area, in the order the scan reads them.</summary>
    internal static OwnedRule[] All { get; } =
    [
        TitleOfAnAxis,
        FigureGround,
        LegendPlaceUnderTheme,
        LegendMarkerSize,
        TableGridRule,
        MadeOnRule,
        new("matplotlib's tight_layout pad, 1.08 times the font size, is one constant of the matplotlib theme factory",
            @"\b1\.08\b",
            "Src/MatPlotLibNet/Styling/Themes/MatplotlibThemeFactory.cs") { Finds = [
            "    private const double TightLayoutPad = 1.08;"] },
        EdgeMeaning,
        EdgeHanging,
        OwnedRule.Nowhere("a member that draws, ranges or orders under a theme takes the theme, never null: a caller with no theme of its own passes Theme.Default, the library's look, so no site decides what no theme means",
            NoThemePattern, under: "Src/MatPlotLibNet/Rendering/") with { Finds = [
            "    internal static ShapeStyle StyleOf(Patch patch, Theme? theme) => patch.Style(theme ?? Theme.Default);"] },
        new("the input a Y axis title's band is measured from (its tick label band, its side, the container's gap and the theme's label font) is made by YAxisContainer.TitleInputUnder, for the drawing and the layout alike",
            @"\bnew YAxisTitleInput\(",
            "Src/MatPlotLibNet/Rendering/YAxisTitleExtent.cs") { Under = "Src/MatPlotLibNet/", Finds = [
            "            new YAxisTitleInput(Axes.YAxis.MajorTicks.TickLabelBand(MeasuredYTickMaxWidth), AxisSide.Left, Axes.YAxes.TitleGap,"] },
        new("where an axis title stands beside the tick labels of its edge, and the room it takes, are TitleClearance's (Beside an edge: the ink that faces the labels and the place without labels; TitleOffset, TitleExtent): no site builds a clearance or names a place without labels itself",
            @"\bnew TitleClearance\(|\bWithoutTickLabels\s*=",
            "Src/MatPlotLibNet/Rendering/TitleClearance.cs") { Under = "Src/", Finds = [
            "            new TitleClearance(config.TickLabelBand(tallestTickLabel), XLabelGap, labelSize, ink)"] },
        new("a series pass is made by a factory of SeriesPass (OverPlot over a Cartesian plot, OnUnitSquare and Layered for a polar, ternary or 3-D axes), which sets what a pass is (the mapping its groups carry, its clip, its patches and grid, whose z-order orders it): no site builds a pass by hand",
            @"\bnew SeriesPass\(",
            "Src/MatPlotLibNet/Rendering/SeriesPass.cs") { Under = "Src/", Finds = [
            "        new SeriesPass(axes, render, transform) { ClickMapping = transform, Clip = new ClipRegion(render.PlotArea, outline) };"] },
        new("the unit square a polar, ternary or 3-D axes draws its series through and collects their marks through, for a legend at Best, is SeriesPass.UnitSquare: no site spells the mapping of 0 to 1 over the plot again",
            @"\bDataTransform\(\s*0(?:\.0)?\s*,\s*1(?:\.0)?\s*,\s*0(?:\.0)?\s*,\s*1(?:\.0)?\s*,",
            "Src/MatPlotLibNet/Rendering/SeriesPass.cs") { Under = "Src/", Finds = [
            "        CollectMarks(Axes.Series, new DataTransform(0, 1, 0, 1, PlotArea), marks, ComputeScene().Projection);"], Ignores = [
            "        var transform = new DataTransform(0, 10, 0, 1, PlotArea);"] },
        new("the plot, the context and the theme an axes is drawn into are one RenderPass the chart renderer forms for each axes and hands down (AxesRenderer.Pass, CartesianPartContext.Pass), and a pass over a Cartesian plot clips to that plot by itself (SeriesPass.OverPlot): no part forms a render pass again or hands a pass a clip of the plot",
            @"\bnew RenderPass\(|\bnew ClipRegion\(\s*[\w.]*PlotArea\s*\)",
            "Src/MatPlotLibNet/Rendering/ChartRenderer.cs") { Under = "Src/", Finds = [
            "        (SeriesPass.OverPlot(Axes, new RenderPass(PlotArea, Ctx, Theme), _placed.Transform, new ClipRegion(PlotArea)) with",
            "        Draw(SeriesPass.OverPlot(Axes, Pass, transform, new ClipRegion(PlotArea)) with { OrderedBy = Theme.Default });"] },
        new("a series is drawn in its group, its slot numbered, what belongs inside the group drawn and the group closed, by the one loop of SeriesPass (DrawSteps); in a 3-D scene the pass hands the group to the depth queue as a SeriesGroup, which opens it around each run of the series' marks: no renderer or part writes that loop again",
            @"\bDrawInItsGroup\(|\.BeginSeriesGroup\(",
            "Src/MatPlotLibNet/Rendering/SeriesPass.cs", "Src/MatPlotLibNet/Rendering/SeriesGroup.cs") { Under = "Src/", Finds = [
            "            bool openedGroup = Ctx.BeginSeriesGroup(Axes, series, i);"] },
        new("the steps of a data layer are held and sorted by LayerOrder, the one orderer: no other site rents an array of steps or sorts one",
            @"ArrayPool<LayerStep>|\.SortByZ\(\)",
            "Src/MatPlotLibNet/Rendering/LayerOrder.cs") { Under = "Src/", Finds = [
            "        _steps = _rented ? ArrayPool<LayerStep>.Shared.Rent(capacity) : [];"] },
        new("what the edge opposite a primary axis draws (the mirrored ticks, the marks a theme puts on every side, nothing where an axis of its own stands) is CartesianEdges: no site reads the mirror or the theme's every-side marks itself",
            @"\.MajorTicks\.Mirror\b|\.OnAllSides\b",
            "Src/MatPlotLibNet/Rendering/CartesianEdges.cs") { Under = "Src/MatPlotLibNet/Rendering/", Finds = [
            "    private bool MirrorsTicks => Axes.XAxis.MajorTicks.Mirror && !drawsTopAxis;"] },
        new("the tick drawing of a placed axis is made and hung from its edge, and from the edge opposite it, by PlacedAxis: no part makes or hangs one itself",
            @"TickDrawContext\.For\(|\.Hang\(",
            "Src/MatPlotLibNet/Rendering/PlacedAxis.cs") { Under = "Src/MatPlotLibNet/Rendering/", Finds = [
            "                right.Hang(yPart.TickDraw, Axes.Spines.Right.VerticalX(PlotArea, right.EdgeX(PlotArea, 0), transform), Axes.Spines)));"] },
        new("a horizontal axis title is placed beside its edge in one place, UprightEdgeTitle; the room it takes below the plot is AxesTitleExtensions' BottomTitleExtent",
            @"\.XTitleBeside\(",
            "Src/MatPlotLibNet/Rendering/UprightEdgeTitle.cs", "Src/MatPlotLibNet/Rendering/AxesTitleExtensions.cs") { Under = "Src/", Finds = [
            "        double offset = axis.XTitleBeside(edge, theme, tallestTickLabel, font.Size).TitleOffset() + axis.TitlePad;"] },
        OwnedRule.Nowhere("every part of a Cartesian axes render is built from the render's CartesianPartContext: no part is handed the axes, the plot, the context and the theme one by one",
            @"\bnew (?:CartesianParts\.)?Cartesian\w*Part\(\s*(?:Axes|axes|_axes)\b", under: "Src/") with { Finds = [
            "        new CartesianParts.CartesianSpansPart(Axes, PlotArea, Ctx, Theme, transform, range).Render();"] },
        new("whether an axis is drawn in parts (a room between them) is AxisBreakMap.IsDrawnInParts: no site asks for a room above zero, or for rooms or parts, itself",
            @"\{\s*Room:\s*>\s*0\s*\}|\bRoom\s*>\s*0|\bRooms\.IsEmpty\b|\bParts\.IsEmpty\b",
            "Src/MatPlotLibNet/Rendering/AxisBreakMap.cs") { Under = "Src/", Finds = [
            "    internal bool HasRooms => _dir.X.Breaks is { Room: > 0 } || _dir.Y.Breaks is { Room: > 0 };",
            "        if (BreaksAlong(alongX) is not { } map || map.Rooms.IsEmpty) return [];",
            "        if (map.Parts.IsEmpty) return OutsideTheBreaks(axis.TickValuesBetween(source, shown, length, theme, isX), map);",
            "            if (breaks.Room > 0)"], Ignores = [
            "        if (dir.Breaks is not { IsDrawnInParts: true } map) return [];"] },
        new("the rooms of an axis drawn in parts stand on the screen by PixelDirection.RoomAt (both ends placed, from the first pixel to the last, the jump in pixels): no site places the two ends of a room itself",
            @"\b(?:PixelAlong|Place)\([^;]*\b[Rr]ooms?\b[^;]*\.Lo\b[^;]*\b(?:PixelAlong|Place)\([^;]*\.Hi\b",
            "Src/MatPlotLibNet/Rendering/PixelDirection.cs") { Under = "Src/", Finds = [
            "            double a = PixelAlong(map.Rooms[i].Lo, alongX), b = PixelAlong(map.Rooms[i].Hi, alongX);",
            "            double a = Transform.PixelAlong(room.Lo, isX), b = Transform.PixelAlong(room.Hi, isX);"], Ignores = [
            "            DrawCorners(shape, isX, Transform.PixelAlong(rooms[i].Lo, isX), LengthOf(parts[i], isX), acrossStart, acrossLength, stroke);",
            "    private double LengthOf(Range1D part, bool isX) => Math.Abs(Transform.PixelAlong(part.Hi, isX) - Transform.PixelAlong(part.Lo, isX));"] },
        EdgeElseGap,
        CellEdgeAlong,
        LayoutKeepsRoom,
        MarginClamp,
        PanelWrapOfABuilder,
        PanelWrapOfTheLayout,
    ];
}
