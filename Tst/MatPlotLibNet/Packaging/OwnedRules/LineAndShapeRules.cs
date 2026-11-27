// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Packaging.OwnedRules;

/// <summary>The rules of a line and a shape: how a stroke and a shape are made, a dash, a width, a join, the edge and the hatch of
/// a patch, the runs of a line, a grid line and a spine.</summary>
internal static class LineAndShapeRules
{
/// <summary>The widths the library draws its own outlines, hairlines and marks at: 1, 0.5 and 2.5 pixels.</summary>
    private const string OwnWidth = @"(?:0\.5|1(?:\.0)?|2\.5)(?![\w.])";

/// <summary>A cell of a colour-mapped mesh (a heatmap, a 2-D histogram, a mesh, a spectrogram's library mesh) stands on the pixel grid
    /// of a raster export under a theme that asks, as Agg snaps a quad of matplotlib's QuadMesh: no seam between two cells. The grid
    /// renderers that draw another rectangle (a filled contour's backdrop in one style, a pair grid's histogram bars) own theirs.</summary>
    private static readonly OwnedRule MeshCellOnThePixelGrid = new(
        "a cell of a colour-mapped mesh is drawn through DrawSnappedRectangle: no grid renderer draws a coloured cell with DrawRectangle",
        @"\bCtx\.DrawRectangle\(",
        "Src/MatPlotLibNet/Rendering/SeriesRenderers/Grid/ContourBandFillExtensions.cs",
        "Src/MatPlotLibNet/Rendering/SeriesRenderers/Grid/PairGridSeriesRenderer.cs") { Under = "Src/MatPlotLibNet/Rendering/SeriesRenderers/Grid/", Finds = [
        "            Ctx.DrawRectangle(cell, new ShapeStyle(color, null, 0));"] };

/// <summary>A rectangle a renderer draws with its corners on the pixel grid of a surface that has one (a bar, a box, a cell of a
    /// matrix) goes through <c>ctx.DrawSnappedRectangle</c>: the renderer never asks the grid where a rectangle stands
    /// (<c>OnPixelGrid(rect, style)</c>) and then draws what it was told. The legend asks it for a rounded frame, which is another
    /// shape drawn by its own callout.</summary>
    private static readonly OwnedRule SnappedRectangle = new(
        "a rectangle with its corners on the pixel grid is drawn by ctx.DrawSnappedRectangle: no renderer asks the grid where a rectangle stands (OnPixelGrid(rect, style)) and draws what it was told",
        @"\bOnPixelGrid\(\s*[^,;()]+(?:\([^;()]*\))?[^,;()]*,\s*\w*[sS]tyle\w*\s*\)",
        "Src/MatPlotLibNet/Rendering/RenderContextPixelGridExtensions.cs", "Src/MatPlotLibNet/Rendering/LegendRendering/LegendRenderer.cs") { Under = "Src/", Finds = [
        "            Ctx.DrawRectangle(Ctx.OnPixelGrid(pixel.Bar, barStyle), barStyle);",
        "        if (box.IsDrawable()) Ctx.DrawRectangle(Ctx.OnPixelGrid(box, style), style);",
        "var snapped = Ctx.OnPixelGrid(cell.Bounds, cellStyle);"] };

/// <summary>A shape is made by its named factories in one file: <c>Filled</c>, <c>Outlined</c> and <c>Edged</c>, which every source of a
    /// fill and an edge (a resolved patch edge, a marker kind, a hatch layer, a state segment, a renderer's own mark) hands its values to.
    /// Read across lines, as <see cref="StrokeMade"/> is.</summary>
    private static readonly OwnedRule ShapeMade = new(
        "a ShapeStyle is made in ShapeStyle.cs (Filled, Outlined, Edged): no other file constructs one, by new ShapeStyle( or by a target-typed new( of a member typed ShapeStyle",
        @"\bnew\s+ShapeStyle\s*\(|\bShapeStyle\??\s+\w+\s*(?:\([^()]*(?:\([^()]*\)[^()]*)*\))?\s*(?:=>|=)\s*new\s*\(",
        "Src/MatPlotLibNet/Styling/ValueObjects/ShapeStyle.cs") { Under = "Src/", AcrossLines = true, Finds = [
        "Ctx.DrawPolygon(points, new ShapeStyle(fill, edge, width));",
        "internal ShapeStyle Fill(Theme theme) =>\n            new(segment.Color, null, 0)",
        "internal ShapeStyle HatchLayer => new(new Color(0, 0, 0, 0), null, 0)"] };

/// <summary>A stroke is assembled from its colour, width, dash and join in one place, <c>StrokeStyle.Of</c>: every source of a line (a
    /// tool, a spine, a grid, a shape's outline, a series' slot, a box's look) hands its values to it. Read across lines, so a member typed
    /// <c>StrokeStyle</c> and made by a target-typed <c>new(</c> on the next line is found too.</summary>
    private static readonly OwnedRule StrokeMade = new(
        "a StrokeStyle is made in StrokeStyle.cs (StrokeStyle.Of, Solid): no other file constructs one, by new StrokeStyle( or by a target-typed new( of a member typed StrokeStyle",
        @"\bnew\s+StrokeStyle\s*\(|\bStrokeStyle\??\s+\w+\s*(?:\([^()]*(?:\([^()]*\)[^()]*)*\))?\s*(?:=>|=)\s*new\s*\(",
        "Src/MatPlotLibNet/Styling/ValueObjects/StrokeStyle.cs") { Under = "Src/", AcrossLines = true, Finds = [
        "Ctx.DrawLine(a, b, new StrokeStyle(colour, width, LineStyle.Dashed));",
        "internal StrokeStyle StrokeOf(Color color, GivenStroke given, LineWidthRule rule) =>\n        new(color, LineWidthOf(given.Width, rule), LineStyleOf(given.Style));",
        "private StrokeStyle StatsStroke(ViolinStatsLine line, Color statsColor) =>\n        new(line.Color ?? statsColor, Slot.LineWidthOf(line.Width), line.Style);"], Ignores = [
        "/// internal StrokeStyle StrokeOf() =>\n///     new(colour, 1, LineStyle.Solid);"] };

/// <summary>'No dash' is said by the two values a line is: a drawn stroke (<c>StrokeStyle.Solid</c>, <c>IsSolid</c>) and a given one
    /// (<c>GivenStroke.Solid</c>, <c>RestingStyle</c>, which the cycler's empty list also hands out); a surface asks the stroke whether it is
    /// solid and the dash writer asks the theme's DashLook for a pattern, never the enum.</summary>
    /// <remarks>The other owners hold a declaration or data, not the rule: the resting dash a property is declared with beside it (a spine,
    /// a trendline, a patch's edge, a zoom indicator, a grid, a box's line; each kind's own value, others are none or dashed), a look's table
    /// (a violin's stats lines, matplotlib's whisker under the default style) and the guide line three verbs choose to draw solid where a
    /// reference line rests dashed (a Pareto chart's column line, a spectrum's zero line, a control chart's centre line).</remarks>
    private static readonly OwnedRule NoDash = new(
        "'no dash' is named by StrokeStyle (Solid, IsSolid) and GivenStroke (Solid, RestingStyle): no site builds or asks a solid line itself; the rest declare a resting dash beside its property or hold a look's data",
        @"\bLineStyle\.Solid\b",
        "Src/MatPlotLibNet/Styling/ValueObjects/StrokeStyle.cs", "Src/MatPlotLibNet/Styling/GivenStroke.cs",
        "Src/MatPlotLibNet/Models/SpinesConfig.cs", "Src/MatPlotLibNet/Models/Tools/Trendline.cs", "Src/MatPlotLibNet/Models/Patches/Patch.cs",
        "Src/MatPlotLibNet/Models/ZoomIndicator.cs", "Src/MatPlotLibNet/Styling/Theme.cs", "Src/MatPlotLibNet/Styling/BoxStroke.cs",
        "Src/MatPlotLibNet/Styling/Looks/ViolinLook.cs", "Src/MatPlotLibNet/Styling/Themes/MatplotlibThemeFactory.cs",
        "Src/MatPlotLibNet/Builders/CategoricalAxesExtensions.cs", "Src/MatPlotLibNet/Builders/SignalAxesExtensions.cs",
        "Src/MatPlotLibNet/Builders/XYAxesExtensions.cs") { Under = "Src/", Finds = [
        "    public LineStyle LineStyle { get; set; } = LineStyle.Solid;"] };

/// <summary>A line a renderer draws in a look of its own (a guide, a crosshair, a needle, a stem) is its colour, the rule it is drawn
    /// wide by and its dash, which the slot makes a stroke of: no renderer joins a stroke factory to a width it asked the slot for, or
    /// rewrites a stroke's width and dash with <c>with</c>.</summary>
    private static readonly OwnedRule OwnLineOfARenderer = new(
        "a renderer's own line is Slot.StrokeOf(colour, LineWidthRule[, dash]): no renderer puts a StrokeStyle together from Slot.LineWidthOf, or rewrites a stroke's width or dash with `with`",
        @"StrokeStyle\.\w+\([^;]*LineWidthOf\(|\bStroke\s+with\s*\{[^}]*\b(?:Thickness|Style)\s*=",
        "Src/MatPlotLibNet/Rendering/SeriesSlot.cs") { Under = "Src/MatPlotLibNet/Rendering/", Finds = [
        "            StrokeStyle.Of(color, slot.LineWidthOf(given.Width, rule), slot.LineStyleOf(given.Style));"] };

/// <summary>The glue of a patch's fill to its resolved edge, whatever the locals are called: <c>edge.Color, edge.Width</c>,
    /// <c>edgeColor, edgeWidth</c> or <c>edge, edgeWidth</c>. The scatter renderer owns the one line that says it for a marker (a
    /// marker's edge is the scatter rule's, not a patch's).</summary>
    private static readonly OwnedRule PatchEdgeGlue = new(
        "a patch-like shape's fill and its resolved edge become a style in ResolvedPatchEdge.StyleOf (or PatchEdge.StyleOf): no renderer glues the edge's colour and width to a fill by hand, under any name",
        @"(?:new ShapeStyle|ShapeStyle\.Edged)\([^;]*\bedge[A-Za-z]*(?:\.Color)?\s*,\s*edge[A-Za-z]*(?:\.Width)?\b",
        "Src/MatPlotLibNet/Rendering/SeriesRenderers/XY/ScatterSeriesRenderer.cs") { Under = "Src/MatPlotLibNet/Rendering/", Finds = [
        "var style = ShapeStyle.Edged(fill, edge.Color, edge.Width);",
        "var style = new ShapeStyle(fillColor, edgeColor, edgeWidth) { Hatch = series.Hatch, HatchColor = hatchColor };",
        "return ShapeStyle.Edged(fill, edge, edgeWidth);"], Ignores = [
        "new ShapeStyle(FaceColor, EdgeColor, EdgeWidth)",
        "? new ShapeStyle(faded, look.EdgeColor is { } edge ? Fade(edge) : faded, width)"] };

/// <summary>The pattern or the colour of a hatch copied from one holder to another by hand (<c>Hatch = node.Hatch</c>,
    /// <c>HatchColor = HatchColor</c>, <c>patch.HatchColor = HatchColor</c>): a series, a patch and a tree node pass the pair on as
    /// one <c>Hatching</c>, so a wire form and a model never restate its two halves.</summary>
    /// <remarks>A shape's own layers (its pattern split into the fill under it and the strokes over it) and a state segment's fill pass
    /// one HatchFill to ShapeStyle.WithHatch, so no file copies the halves.</remarks>
    private static readonly OwnedRule PatternCopy = OwnedRule.Nowhere(
        "a hatch's pattern and colour pass from a model to its wire form and back as one Hatching: no site copies the halves from one holder to the other",
        @"\bHatch(?:Color)?\s*=\s*(?:[A-Za-z_]\w*\.)*Hatch(?:Color)?\b", under: "Src/MatPlotLibNet/") with { Finds = [
        "HatchColor = HatchColor,",
        "HatchColor = node.HatchColor,",
        "Hatch = dto.Hatch,",
        "patch.HatchColor = HatchColor;",
        "patch.Hatch = Hatch ?? patch.Hatch;"], Ignores = [
        "Hatch = dto.Hatching.Pattern,\nHatchColor = dto.Hatching.Color,\nHatch = value.PatternOnTheWire;"] };

/// <summary>The width rule of a kind of line is a named value of LineWidthRule, never a copy made per class.</summary>
    private static readonly OwnedRule WidthRuleNamed = new(
        "the rule a kind of line is drawn wide by is a named LineWidthRule (OnePixelCollection, OwnHairline...): no class builds one with `with` or keeps a static copy",
        @"\bLineWidthRule\.\w+\s+with\b|\bstatic\s+LineWidthRule\s+\w+\s*\{",
        "Src/MatPlotLibNet/Styling/LineWidthRule.cs") { Under = "Src/", Finds = [
        "    internal static LineWidthRule Line { get; } = new(SeriesLineLook.LibraryWidth, Cycled: true);"] };

/// <summary>A kind's given width and stroke are read through the kind (IWidthGiven, IGivenStroked), never picked apart beside it.</summary>
    private static readonly OwnedRule WidthReadThroughTheKind = new(
        "a series' line is stroked from the series itself (Slot.StrokeOf(color, series), Slot.LineWidthOf(series), DrawSeriesLine): only the slot and the theme's line look take the kind's given stroke or width and its rule apart",
        @"StrokeOf\([^;]*\bseries\.Stroke\b|LineWidthOf\([^;]*\b(?:series\.GivenLineWidth|\w+Series\.\w*WidthRule)\b|WidthOf\([^;]*\bWidthRule\)",
        "Src/MatPlotLibNet/Rendering/SeriesSlot.cs", "Src/MatPlotLibNet/Styling/Looks/SeriesLineLook.cs") { Under = "Src/", Finds = [
        "    internal double LineWidthOf(IWidthGiven series) => LineWidthOf(series.GivenLineWidth, series.WidthRule);"] };

/// <summary>A stroke width the library draws at its own number is a named LineWidthRule, never a number typed in a renderer.</summary>
    private static readonly OwnedRule NoLiteralStrokeWidth = OwnedRule.Nowhere(
        "a width a renderer draws an outline, a hairline or a mark at is a named LineWidthRule (OwnPixel, OwnHairline, OwnMark) read through the slot: no renderer types 1, 0.5 or 2.5 as a stroke width",
        @"(?:new ShapeStyle|\.Edged)\([^;]*,\s*" + OwnWidth + @"\)|\.Outlined\([^;]*,\s*" + OwnWidth + @"\)|StrokeStyle(?:\.Solid)?\([^;]*,\s*" + OwnWidth + @"\s*(?:,\s*(?:Styling\.)?LineStyle\.\w+\s*)?\)"
        + @"|\bThickness\s*=\s*" + OwnWidth + @"|\bconst double \w*(?:Thickness|Edge)\s*=\s*" + OwnWidth,
        under: "Src/MatPlotLibNet/Rendering/") with { Finds = [
        "            ctx.DrawRectangle(new Rect(barX, barY, totalW, barH), new ShapeStyle(null, theme.ForegroundText, 0.5));"] };

/// <summary>The plain grid matplotlib draws is written once: its width in <c>GridStyle.Plain</c>, its opacity (matplotlib's
    /// <c>grid.alpha</c>, the same under both of its styles) as one constant of the matplotlib theme factory. The style sheet's grid
    /// defaults and the two matplotlib grids read them.</summary>
    private static readonly OwnedRule PlainGridWidth = new(
        "the plain grid's width is GridStyle.Plain's: no theme grid types 0.8 again",
        @"\bLineWidth\s*=\s*0\.8\b",
        "Src/MatPlotLibNet/Styling/Theme.cs") { Under = "Src/MatPlotLibNet/Styling/", Finds = [
        "        LineWidth = 0.8.PointsInPixels,"] };

/// <summary>The style sheet's grid defaults read the plain grid and the matplotlib grid opacity, never a number.</summary>
    private static readonly OwnedRule StyleSheetGridDefault = OwnedRule.Nowhere(
        "the style sheet's grid width and opacity read GridStyle.Plain and MatplotlibThemeFactory.GridAlpha: no number is typed for them",
        @"Grid(?:LineWidth|Alpha)\]\s*=\s*[0-9]",
        under: "Src/MatPlotLibNet/Styling/") with { Finds = [
        "        [RcParamKeys.GridLineWidth]       = 0.8,",
        "        [RcParamKeys.GridAlpha]           = 1.0,"] };

/// <summary>The matplotlib grids (classic and default style) take matplotlib's grid opacity from one constant.</summary>
    private static readonly OwnedRule MatplotlibGridAlpha = OwnedRule.Nowhere(
        "the matplotlib grids take matplotlib's grid.alpha from MatplotlibThemeFactory.GridAlpha: no grid of the factory types 1.0",
        @"^\s*Alpha\s*=\s*1\.0\s*,?\s*$",
        under: "Src/MatPlotLibNet/Styling/Themes/") with { Finds = [
        "        Alpha = 1.0,"] };

    /// <summary>The rules of the area, in the order the scan reads them.</summary>
    internal static OwnedRule[] All { get; } =
    [
        MeshCellOnThePixelGrid,
        SnappedRectangle,
        ShapeMade,
        new("a shape that is filled and not outlined is built by ShapeStyle.Filled: no outline is typed once, not at every mark",
            @"\bEdged\([^;]*, *null, *0(?:\.0)?\)|\bEdged\(([^,;]+), *\1, *0(?:\.0)?\)",
            "Src/MatPlotLibNet/Styling/ValueObjects/ShapeStyle.cs") { Under = "Src/", Finds = [
            "    public static ShapeStyle Filled(Color fill) => Edged(fill, null, 0);"] },
        new("a shape that is outlined and not filled is built by ShapeStyle.Outlined",
            @"\bEdged\(null,",
            "Src/MatPlotLibNet/Styling/ValueObjects/ShapeStyle.cs") { Under = "Src/", Finds = [
            "    public static ShapeStyle Outlined(Color stroke, double thickness) => Edged(null, stroke, thickness);"] },
        OwnedRule.Nowhere("whether a marker kind takes an edge (one made of strokes does not: it inks with its edge and takes the line as its automatic edge) is MarkerShape.TakesAnEdge, read by StyleOf and AutoEdge: no site branches on the kind of a marker",
            @"\bis\s+(?:not\s+)?StrokedMarker\b", under: "Src/") with { Finds = [
            "        if (shape is StrokedMarker) return ShapeStyle.Outlined(color, width);"] },
        StrokeMade,
        new("the line a shape's outline is drawn with is ShapeStyle.OutlineStroke: colour, width, dash and join read once",
            @"StrokeStyle\.Of\([^;]*StrokeThickness",
            "Src/MatPlotLibNet/Styling/ValueObjects/ShapeStyle.cs") { Finds = [
            "    public StrokeStyle OutlineStroke() => StrokeStyle.Of(Stroke!.Value, StrokeThickness, StrokeLineStyle, StrokeJoin);"] },
        NoDash,
        new("a minor grid line is the major one faded to half its opacity and width (StrokeStyle.Faded), asked once by AxisTickLook.MinorGridStroke: no site halves a grid line's alpha or width by hand",
            @"\.Faded\(|\.A\s*/\s*2(?![\d.])|\bThickness\s*\*\s*0\.5\b",
            "Src/MatPlotLibNet/Styling/Looks/AxisTickLook.cs") { Under = "Src/", Finds = [
            "var minor = grid.Stroke.Faded(0.5);",
            "var minor = grid with { Color = grid.Color.WithAlpha((byte)(grid.Color.A / 2)) };",
            "var minor = stroke with { Thickness = stroke.Thickness * 0.5 };"] },
        OwnLineOfARenderer,
        new("the lines points are drawn as are their LineRuns (MissingValueExtensions): each unbroken run of two or more drawable points, one walk; no renderer or surface breaks a line at its gaps or drops a lone point itself",
            @"\bRuns\([^;]*IsDrawable\(\)|\.DrawableRuns\(\)|\bi\s*-\s*start\s*>=\s*2\b",
            "Src/MatPlotLibNet/Extensions/MissingValueExtensions.cs") { Under = "Src/", Finds = [
            "foreach (var run in points.DrawableRuns())",
            "if (start >= 0 && i - start >= 2) ctx.DrawLines(outline[start..i], stroke);"] },
        new("whether points are a line is PathSinkExtensions.FormsALine (two points or more): no renderer or surface counts the points of a polyline itself",
            @"\b(?:points|run|pieces\[\w+\]|means|pixels|pixelPoints|outline|flat\.Points)\.(?:Count|Length)\s*(?:>=\s*2|<\s*2)(?![\d.])",
            "Src/MatPlotLibNet/Rendering/PathSinkExtensions.cs") { Under = "Src/MatPlotLibNet/", Finds = [
            "if (run.Count >= 2) marks.AddPath(run);",
            "if (means.Count >= 2) DrawPolyline(means, stroke);",
            "if (flat.Points.Length < 2) return;"] },
        new("the line of a series that is one stroked line is drawn by SeriesRenderer.DrawSeriesLine (in its colour, stroked as its kind is): no renderer glues DrawPolyline to the slot's stroke of the series",
            @"DrawPolyline\([^;]*Slot\.StrokeOf\(",
            "Src/MatPlotLibNet/Rendering/SeriesRenderers/SeriesRenderer.cs") { Under = "Src/", Finds = [
            "DrawPolyline(path, Slot.StrokeOf(color, series));"] },
        new("a grid becomes a line through GridStyle.Stroke; only the records that carry a grid field by field read its colour, width and style",
            @"(?:\b[gG]rid|DefaultGrid)\w*\.(?:Color|LineWidth|LineStyle)\b",
            "Src/MatPlotLibNet/Serialization/GridDto.cs", "Src/MatPlotLibNet/Styling/GridChange.cs", "Src/MatPlotLibNet/Styling/ThemeRcKey.cs") { Finds = [
            "        var gridColor = grid.Color;"] },
        new("matplotlib's plain grid colour is written once, in GridStyle.Plain",
            @"(?i)#b0b0b0",
            "Src/MatPlotLibNet/Styling/Theme.cs") { Finds = [
            "        [RcParamKeys.GridColor]           = Color.FromHex(\"#b0b0b0\"),"] },
        OwnedRule.Nowhere("one spine of an axes is changed by SpinesConfig.WithEdge, and spines are hidden by SpinesConfig.Hiding: no site copies the spines and the one edge with a `with` of its own",
            @"[sS]pines\.(?:Top|Bottom|Left|Right)\s+with\s*\{", under: "Src/") with { Finds = [
            "    public AxesBuilder HideTopSpine() { _axes.Spines = _axes.Spines with { Top = _axes.Spines.Top with { Visible = false } }; return this; }",
            "            Right = _axes.Spines.Right with { Visible = false },",
            "            SpineEdge.Left => spines with { Left = spines.Left with { Color = color } },"], Ignores = [
            "    public AxesBuilder HideTopSpine() { _axes.Spines = _axes.Spines.Hiding(SpineEdge.Top); return this; }",
            "        internal SpinesConfig WithEdgeColor(SpineEdge edge, Color color) => spines.WithEdge(edge, spine => spine with { Color = color });",
            "        sides = sides with { Right = value };"] },
        new("a spine's colour under a theme is read by SpineConfig.StrokeIn and nowhere else",
            @"\bColorIn\(",
            "Src/MatPlotLibNet/Models/SpinesConfig.cs") { Finds = [
            "    internal Color ColorIn(Theme theme) => Color ?? theme.ForegroundText;"] },
        new("how far a stroke reaches past the line it is drawn along, half its width, is width.Reach (StrokeWidthExtensions): a spine's and a colour bar frame's room for a tick, a raster line's band and projecting end, an arrow head's tip; no site halves a stroke's width itself",
            @"LineWidthIn\([^)]*\)\s*/\s*2(?:\.0)?\b|\b(?:[tT]hickness|StrokeThickness|[sS]trokeWidth|LineWidth\w*|[fF]rameWidth|[sS]pineWidth|EdgeWidth)\s*/\s*2(?:\.0)?\b|\b0\.5\s*\*\s*[\w.]*(?:Thickness|LineWidth|EdgeWidth)\b",
            "Src/MatPlotLibNet/Styling/StrokeWidthExtensions.cs") { Under = "Src/", Finds = [
            "internal double HalfWidthIn(Theme theme) => LineWidthIn(theme) / 2.0;",
            "var majorMark = TickMark.Of(major, ink, frameWidth / 2);",
            "double half = thickness / 2;",
            "double back = end - (0.5 * style.StrokeThickness / sine);"] },
        PatchEdgeGlue,
        new("the theme's scatter edge becomes a style in SeriesSlot (ScatterStyleOf, BubbleStyleOf) or in the scatter renderer's per-point rule: no other renderer glues a marker edge to a fill by hand",
            @"\bScatterEdgeOf\(",
            "Src/MatPlotLibNet/Rendering/SeriesSlot.cs", "Src/MatPlotLibNet/Rendering/SeriesRenderers/XY/ScatterSeriesRenderer.cs") { Finds = [
            "    internal MarkerEdge? ScatterEdgeOf(Color face) =>"] },
        new("a shape is joined as its series' artist is to matplotlib (Theme.JoinedAs(series.Artist, shape) over StrokeLook.JoinedAs; the scatter look as MatplotlibArtist.Scatter): no producer decides itself that it draws a collection or names another kind's artist",
            @"\bCollectionStyleOf\(|\.OfCollection\(|\.Collection\s*\?|StrokeJoin\s*=\s*[\w.]*Strokes\.Join\b|\bJoinedAs\(\s*MatplotlibArtist\.|\bEdged\([^;]*,\s*JoinStyle\.\w+\s*\)",
            "Src/MatPlotLibNet/Styling/Looks/StrokeLook.cs", "Src/MatPlotLibNet/Rendering/SeriesSlot.cs") { Under = "Src/", Finds = [
            "Ctx.DrawPolygon(face.Vertices, theme.CollectionStyleOf(ShapeStyle.Edged(face.Fill, stroke, width)));",
            "return Collection ? Looks.Strokes.OfCollection(style) : style;",
            "var style = shape with { StrokeJoin = theme.Looks.Strokes.Join };",
            "Ctx.DrawPolygon(face.Vertices, theme.JoinedAs(MatplotlibArtist.Mesh, style));",
            "Ctx.DrawPolygon(face.Vertices, ShapeStyle.Edged(face.Fill, stroke, width, JoinStyle.Round));"] },
        new("the colour of a hatch is decided by PatchLook.HatchOver, asked through ResolvedPatchEdge: nothing else names the rule",
            @"HatchColorOver|PatchHatchColorOf|\.HatchOver\(",
            "Src/MatPlotLibNet/Styling/Looks/PatchLook.cs", "Src/MatPlotLibNet/Styling/ResolvedPatchEdge.cs") { Finds = [
            "    private Color? HatchColorOver(Color? given, HatchGround ground) =>"] },
        OwnedRule.Nowhere("a renderer lays a pattern and the colour of its strokes over a shape with ShapeStyle.WithHatch(HatchFill) or ResolvedPatchEdge.StyleOf: it never assigns a shape's Hatch or HatchColor",
            @"\bHatch(?:Color)?\s*=(?![=>])", under: "Src/MatPlotLibNet/Rendering/") with { Finds = [
            "            var style = new ShapeStyle(fillColor, edgeColor, edgeWidth) { Hatch = series.Hatch, HatchColor = series.HatchColor };"] },
        OwnedRule.Nowhere("a series that hatches writes its pattern as Hatching.PatternOnTheWire and reads both settings back as one Hatching: the pair is not restated at each series",
            @"\bHatch != HatchPattern\.None \?|\bHatch\.OrNullAtRest\(HatchPattern\.None\)|\bdto\.Hatch\b|\.Hatch = dto\.Hatch", under: "Src/MatPlotLibNet/Models/Series/") with { Finds = [
            "        Hatch = Hatch != HatchPattern.None ? Hatch : null,"] },
        new("a pattern and the colour of its strokes that a series, a patch or a tree node names are read as one HatchFill, owner.Hatching, and set as owner.LayHatching(hatching): no site builds the pair from the two settings",
            @"\bnew(?: HatchFill)?\(\s*[\w.]*\bHatch\s*,\s*[\w.]*\bHatchColor\s*\)",
            "Src/MatPlotLibNet/Styling/IHatched.cs") { Finds = [
            "        internal HatchFill Hatching => new(owner.Hatch, owner.HatchColor);"] },
        new("what a document carries of a hatch reads back as one HatchFill through HatchFill.FromTheWire, the read half of PatternOnTheWire: no wire form decides by itself that an absent pattern is none",
            @"\?\?\s*HatchPattern\.None\b",
            "Src/MatPlotLibNet/Styling/HatchFill.cs") { Under = "Src/MatPlotLibNet/", Finds = [
            "        s.Hatch = dto.Hatch ?? HatchPattern.None;"] },
        PatternCopy,
        new("a drawn annotation line (a reference line, a span's border, a trendline, a level, a Fibonacci retracement, a zoom indicator) is stroked by IStrokedTool's StrokeUnder(theme): its colour or the one its kind takes where it names none, its width as its kind draws it and its dash; no part builds the stroke of a tool or falls back on the neutral itself",
            @"new StrokeStyle\([^;]*\b\w+\.(?:LineWidth|LineStyle)\)|\b(?:refLine|span|t|l|fib|zoom|tool)\.Color\s*\?\?|\?\?\s*_?[tT]heme\.Neutral\b",
            "Src/MatPlotLibNet/Models/IStrokedTool.cs") { Under = "Src/MatPlotLibNet/", Finds = [
            "        Ctx.DrawLine(line.From, line.To, new StrokeStyle(spine.Color ?? Theme.ForegroundText, spine.LineWidth, spine.LineStyle));"] },
        new("whether a stroke is solid is asked of StrokeStyle.IsSolid (a closed outline drawn as its shape, a frame drawn as four bars): no site compares a dash with LineStyle.Solid",
            @"[!=]=\s*(?:Styling\.)?LineStyle\.Solid\b|\bis\s+(?:not\s+)?(?:Styling\.)?LineStyle\.Solid\b",
            "Src/MatPlotLibNet/Styling/ValueObjects/StrokeStyle.cs") { Under = "Src/", Finds = [
            "        if (grid.LineStyle == LineStyle.Solid)"] },
        WidthRuleNamed,
        WidthReadThroughTheKind,
        NoLiteralStrokeWidth,
        PlainGridWidth,
        StyleSheetGridDefault,
        MatplotlibGridAlpha,
    ];
}
