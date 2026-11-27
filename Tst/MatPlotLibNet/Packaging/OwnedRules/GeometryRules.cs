// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Packaging.OwnedRules;

/// <summary>The rules of geometry in the plane and in space: which member of a pair a direction picks, the length of a step and of
/// a vector, the centre, the shorter side and a span of a rectangle, the circle of a round chart, a sector, an arrow head, a step
/// corner.</summary>
internal static class GeometryRules
{
/// <summary>One square of a sum of squares as a line of C# spells it: a name, an element or a bracketed difference, times itself,
    /// the operator spaced (what a script writes in JavaScript, <c>dx*dx</c>, is text of another language).</summary>
    private static string Square(int n) => $@"\(?\s*(?<t{n}>[\w.]+(?:\[[^\]]*\])?|\([^()]*\))\s+\*\s+\k<t{n}>(?![\w\[])\s*\)?";

/// <summary>The open head of the library's own arrows, two strokes from the tip back along the line, is <c>ArrowHead.OpenHead</c>,
    /// <c>OpenHeadMirrored</c> or <c>QuiverHead</c> drawn by <c>ArrowHead.Draw</c> in the arrow's own stroke: no renderer builds its
    /// barbs from an angle and a length it names itself. A filled head follows a matplotlib formula of its own (the streamplot's
    /// <c>-|&gt;</c>, the quiver's polygon) and is another rule.</summary>
    private static readonly OwnedRule OpenBarbs = OwnedRule.Nowhere(
        "the open head of the library's own arrows (two barbs from the tip) is ArrowHead.OpenHead, OpenHeadMirrored or QuiverHead drawn by ArrowHead.Draw: no renderer works its barbs out from an angle and a length it names itself",
        @"\bAtAngle\(\s*-?\s*\w*[hH]ead\w*\s*,|\bconst double\s+(?:Open\w*Head\w*|ArrowHead(?:Size|Angle))\b|Math\.(?:Cos|Sin)\(\s*\w*Head\w*\s*\)",
        under: "Src/MatPlotLibNet/Rendering/SeriesRenderers/") with { Finds = [
        "    private const double OpenHeadLength = 6, OpenHeadAngle = 0.4;",
        "        Ctx.DrawLine(head, head.AtAngle(-headLength, angle - OpenHeadAngle), stroke);",
        "    private const double ArrowHeadAngle = 0.4; // radians (~23 degrees)",
        "        double cosA = Math.Cos(ArrowHeadAngle);",
        "        Ctx.DrawLine(end, end.AtAngle(headLen, angle + 2.5), stroke);"] };

/// <summary>A slice of a ring drawn as a polyline (a pie's slice, a dial's band, a polar heat map's cell) is <c>RingWedge.Polygon()</c>,
    /// cut into the steps <c>RingWedge.StepsFor</c> names for its sweep: no chart names a number of arc steps of its own.</summary>
    private static readonly OwnedRule ArcResolution = new(
        "a sector drawn as a polyline is RingWedge.Polygon() at the one resolution RingWedge.StepsFor names (120 steps to the turn): no chart names a number of arc steps of its own",
        @"\b(?:ArcSteps|ArcSegments|StepsPerTurn)\b|\bint\s+steps\s*=",
        "Src/MatPlotLibNet/Rendering/SeriesRenderers/Circular/RingWedge.cs") { Under = "Src/MatPlotLibNet/Rendering/SeriesRenderers/", Finds = [
        "    private const int ArcSteps = 120;",
        "    private const int ArcSegments = 12;",
        "            int steps = 20;",
        "        int steps = Math.Max(3, (int)Math.Ceiling(Math.Abs(sweep) / (2 * Math.PI) * ArcSteps));"] };

/// <summary>The arcs of a slice of a ring are <c>RingWedge</c>'s: its <c>Path()</c> for the whole outline and its <c>OuterArc()</c>
    /// for the one arc along a rim (the chord diagram's ring is a ring wedge per group and a ribbon's end is the arc of a wedge's rim).
    /// No renderer makes the <c>ArcSegment</c> of a sector itself.</summary>
    private static readonly OwnedRule SectorArcs = new(
        "the arcs of a slice of a ring are RingWedge's (Path, OuterArc): no renderer builds the ArcSegments of a sector itself, the chord diagram's ring and its ribbon ends included",
        @"\bnew ArcSegment\(",
        "Src/MatPlotLibNet/Rendering/SeriesRenderers/Circular/RingWedge.cs") { Under = "Src/MatPlotLibNet/Rendering/SeriesRenderers/", Finds = [
        "            new ArcSegment(centre, radius, radius, startDegrees, endDegrees),",
        "        new ArcSegment(centre, outer, outer, Degrees(arc.Start), Degrees(arc.End)),",
        "            segments.Add(new ArcSegment(centre, radius, radius, Degrees(span.Start), Degrees(span.End)));"] };

/// <summary>The circle a round chart is drawn in is <c>RoundFrame.In(bounds, share, inset)</c>: the centre of the plot bounds and a share of
    /// half the shorter side, less an inset. No chart works out half the shorter side of its bounds itself, whatever it calls the
    /// result and however it spells the share.</summary>
    private static readonly OwnedRule RoundFrameRule = new(
        "the circle a round chart is drawn in is RoundFrame.In(bounds, share, inset): no chart works out a share of half the shorter side of its plot bounds itself",
        @"\bShorterSide\s*(?:/\s*2\s*\*|\*\s*0\.\d)",
        "Src/MatPlotLibNet/Rendering/ValueObjects/RoundFrame.cs") { Under = "Src/", Finds = [
        "        double radius = bounds.ShorterSide / 2 * 0.8;"] };

/// <summary>The length of a step in the plane (a segment, a quiver's vector, a force, the hypotenuse of a head), the distance between
    /// two pixels and its square are the length of a <c>Point</c> read as a step (<c>step.Length</c>, <c>step.SquaredLength</c>,
    /// <c>from.StepTo(to)</c>, <c>from.DistanceTo(to)</c>, <c>from.HeadingTo(to)</c>): no site adds two squares (under a root or not: the
    /// in-circle test of the triangulation and the circle test of the monotone spline read the square of a step too), calls a hypotenuse
    /// of its own or squares a step <c>dx</c> by hand. The Skia package reads SkiaSharp's own <c>SKPoint.Length</c>. Two sums of two
    /// squares are not the length of a step and have owners of their own: the power of a complex number (<c>Welch.Squared</c>, the one
    /// place a spectrum's bin is squared) and the Corwin-Schultz estimator's beta (the sum of two squared log ranges of high over low,
    /// written once in <c>CorwinSchultz</c>). The map package measures the steps of its plane (the horizon's cut, the cartogram's
    /// flow) by the same owner, a step between two projected points being a <c>Point</c> (<c>ProjectedPoint.StepTo</c>). A script's own
    /// JavaScript (<c>var d = dx * dx ...</c>) is text of another language.</summary>
    private static readonly OwnedRule StepLength = new(
        "the length of a step in the plane, the distance between two pixels and its square are Point.Length / SquaredLength (StepTo, DistanceTo, HeadingTo): no site adds two squares, takes a hypotenuse or squares a step itself",
        $@"(?<!var \w+ =\s*|\+\s*){Square(1)}\s*\+\s*{Square(2)}(?!\s*\+\s*{Square(3)})|\bdouble\.Hypot\(|(?<!var \w+ = )\bdx \* dx\b",
        "Src/MatPlotLibNet/Rendering/ValueObjects/PointExtensions.cs",
        "Src/MatPlotLibNet/Numerics/Welch.cs", "Src/MatPlotLibNet/Indicators/CorwinSchultz.cs") { Under = "Src/MatPlotLibNet", Finds = [
        "            total += Math.Sqrt(u * u + v * v);",
        "                double mag = Math.Sqrt(dx[i] * dx[i] + dy[i] * dy[i]);",
        "                _speed[k] = Math.Sqrt((uAxes * uAxes) + (vAxes * vAxes));",
        "            sum += double.Hypot(tx[i + 1] - tx[i], ty[i + 1] - ty[i]);",
        "        float across = MathF.Sqrt(m.ScaleX * m.ScaleX + m.SkewY * m.SkewY);",
        "            if (dx * dx + dy * dy > tolerance) return false;",
        "        int pieces = (int)Math.Ceiling(Math.Sqrt((highX - lowX) * (highX - lowX) + (highY - lowY) * (highY - lowY)));",
        "            double sq = alpha * alpha + beta * beta;",
        "                   + (ax_ * ax_ + ay_ * ay_) * (bx_ * cy_ - by_ * cx_);"] };

/// <summary>The length of a vector in space (a light's direction, a face's normal, the diagonal of a scene's box) is
    /// <c>Vec3.Length</c>: no site adds three squares itself, under a root or not. The map package's great circle measures its cross product so too.</summary>
    private static readonly OwnedRule SpaceLength = new(
        "the length of a vector in space is Vec3.Length: no site adds three squares itself",
        $@"{Square(1)}\s*\+\s*{Square(2)}\s*\+\s*{Square(3)}|\bMath\.Pow\([^;]*,\s*2\)\s*\+",
        "Src/MatPlotLibNet/Rendering/Lighting/Vec3.cs") { Under = "Src/MatPlotLibNet", Finds = [
        "        double lLen = Math.Sqrt(Dx * Dx + Dy * Dy + Dz * Dz);",
        "        return new DepthShade(minZ, Math.Sqrt(rangeX * rangeX + rangeY * rangeY + rangeZ * rangeZ));"] };

/// <summary>The shorter side of a rectangle (what a round chart's circle, a 3-D box's square and the largest radius of a rounded
    /// corner are measured from) is <c>Rect.ShorterSide</c>: no site picks the lesser of a width and a height, or of their halves.</summary>
    private static readonly OwnedRule ShorterSideRule = new(
        "the shorter side of a rectangle is Rect.ShorterSide: no site picks the lesser of a width and a height (or of their halves) itself",
        @"\bMath\.Min\(\s*(?:[\w.]+\.)?(?:Width|Height)\s*(?:/\s*2(?:\.0)?)?\s*,\s*(?:[\w.]+\.)?(?:Width|Height)\s*(?:/\s*2(?:\.0)?)?\s*\)",
        "Src/MatPlotLibNet/Rendering/ValueObjects/Rect.cs") { Under = "Src/", Finds = [
        "        double outerR = Math.Min(bounds.Width, bounds.Height) / 2 * 0.8;",
        "        double radius = Math.Min(bounds.Width, bounds.Height) * 0.4;",
        "            ? series.Radius.Value * Math.Min(Area.PlotBounds.Width, Area.PlotBounds.Height) / 2",
        "        double outer = Math.Min(bounds.Width, bounds.Height) / 2 - room;",
        "    internal static double RadiusOf(Rect plotBounds) => Math.Min(plotBounds.Width, plotBounds.Height) / 2;",
        "        double side = Math.Min(plotBounds.Width, plotBounds.Height);",
        "        cr = Math.Min(cr, Math.Min(r.Width / 2, r.Height / 2));"] };

/// <summary>A span of a length centred on a point (a bar across its axis, a label along its contour, a legend on the middle of its
    /// plot, an arrow on its pivot) is <c>Range1D.Around</c>; one centred in a room (a progress track in its plot, a funnel stage in its
    /// row, a colour bar shrunk along its plot, the square of a 3-D axes) is <c>Range1D.CentredIn</c>, matplotlib's
    /// <c>Bbox.anchored(c='C')</c>, kept apart because <c>x0 + (W - w) / 2</c> and the room's middle less half the span can differ in the
    /// last bit. No site takes half a length off a point or keeps half the room left over, whatever the point and the lengths are
    /// called: the pattern keys on the shape, not on a name (a binary minus before half of anything, <c>/ 2</c>, <c>* 0.5</c>,
    /// <c>0.5 *</c>, <c>-=</c>, and half the difference of two lengths, calls included), so a quarter turn (<c>- Math.PI / 2</c>), a
    /// negation (<c>-w / 2</c>) and the middle of a span from its start (<c>x + w / 2</c>, Rect.Center's and Interval1D's rule) are
    /// not read. The files below spell that shape for another rule, each named with its evidence: <c>BarArrangementExtensions</c>
    /// (<c>(index - (count - 1) / 2.0) * width</c>, how far the i-th of n equal bars of a group stands from the group's middle, a
    /// place in a row of slots, not a span), <c>WaterSample</c> (<c>1 + Gap / 2 + (B - A) / 2</c>, the x of a water on the Piper
    /// diamond, a linear map of two shares) and <c>TernaryPoint</c> (its inverse, <c>x - top / 2</c>), <c>Svg3DRotationScript</c> (the
    /// browser's mirror of <c>Projection3D</c>'s fit, in the JavaScript a 3-D figure carries: a script cannot call the library, and its
    /// text is written into every interactive 3-D SVG, so a change to it changes what the library writes); formulas that halve a
    /// coefficient or a count: <c>EasingFunction</c> (the cubic ease's second half), <c>SqueezeMomentum</c> (the sum 0 + 1 + ... +
    /// N-1, N(N-1)/2), Ehlers' filter coefficients <c>1 - alpha / 2</c> (<c>CyberCycleStep</c>, <c>HighPassStep</c>,
    /// <c>MamaFamaStep</c>), <c>Gamma</c> (Stirling's series), <c>BicubicInterpolation</c> (the cubic kernel),
    /// <c>SpectralDetrend</c> (the mean index (n-1)/2 of a linear fit), <c>SpectralSidesExtensions</c> (the fold of an FFT's index
    /// at (m-1)/2); half of one quantity that is no span placed on a point: <c>PointplotSeries</c> (half the width of a percentile
    /// interval, an error bar's reach), <c>GraphLayoutAlgorithm</c> (an arc's radius, half the distance it spans),
    /// <c>ProjectionOutlineExtensions</c> (an ellipse's two radii), <c>RadarSeriesRenderer</c> (how far a ring's straight side
    /// reaches at an angle, <c>cos(sector / 2) / cos(a - sector / 2)</c>), <c>FractionBox</c> (TeX's rule 15d: a fraction whose
    /// clearance falls short shares the shortfall above and below), <c>BarbsSeriesRenderer</c> (the barb and flag polygon as
    /// matplotlib's <c>quiver._make_barbs</c> writes its vertices, <c>offset - full_width / 2</c>), <c>FlowCartogram</c> (a point
    /// scaled about the middle of the grid), <c>INormalizer</c> (the middle of two levels, its own rule and row) and
    /// <c>AggPixelGrid</c> (where a band's edge stands on the pixel grid, half a pixel before its start, the pixel grid's rule).</summary>
    private static readonly OwnedRule CentredSpan = new(
        "a span of a length centred on a point (a bar across its axis, a label along its line, a legend on its plot) is Range1D.Around, and one centred in a room (a track in its plot, a colour bar along its plot) Range1D.CentredIn: no site takes half a length off a point or keeps half the room left over",
        @"(?:\b[cC]entre\w*|\b[mM]iddle\b|CentreAlong\([^)]*\)|\.Center\.[XY]|\bCenter[XY]\([^)]*\))\s*[-+]\s*\(?[\w.]+\s*/\s*2(?:\.0)?\b"
        + @"|\(\s*[\w.]+(?:\([^()]*\))?\s*-\s*[\w.]+(?:\([^()]*\))?\s*\)\s*(?:/\s*2(?:\.0|d)?\b(?!\s*[*/.\d(])|\*\s*0\.5\b)"
        + @"|(?<=[\w)\]])\s*-\s*(?:\([^()]*\)|(?!Math\.PI\b)[\w.]+(?:\([^()]*\))?(?:\[[^\]]*\])?)\s*(?:/\s*2(?:\.0|d)?\b(?!\s*[*/.\d(])|\*\s*0\.5\b)"
        + @"|(?<=[\w)\]])\s*-\s*0\.5\s*\*\s*[\w.]+|(?<=[\w)\]])\s*-\s*\(\s*[\w.]+(?:\([^()]*\))?\s*/\s*2(?:\.0)?\s*\)"
        + @"|-=\s*(?:\([^()]*\)|[\w.]+(?:\([^()]*\))?(?:\[[^\]]*\])?)\s*(?:/\s*2(?:\.0|d)?\b(?!\s*[*/.\d(])|\*\s*0\.5\b)",
        "Src/MatPlotLibNet/Rendering/ValueObjects/Range1D.cs",
        "Src/MatPlotLibNet/Models/Series/Categorical/BarArrangementExtensions.cs", "Src/MatPlotLibNet/Models/Series/Ternary/WaterSample.cs",
        "Src/MatPlotLibNet/Rendering/TernaryPoint.cs", "Src/MatPlotLibNet/Transforms/SvgTransform.Svg3DRotationScript.cs",
        "Src/MatPlotLibNet/Animation/EasingFunction.cs", "Src/MatPlotLibNet/Indicators/SqueezeMomentum.cs",
        "Src/MatPlotLibNet/Indicators/Steps/CyberCycleStep.cs", "Src/MatPlotLibNet/Indicators/Steps/HighPassStep.cs",
        "Src/MatPlotLibNet/Indicators/Steps/MamaFamaStep.cs", "Src/MatPlotLibNet/Numerics/Gamma.cs",
        "Src/MatPlotLibNet/Rendering/Interpolation/BicubicInterpolation.cs", "Src/MatPlotLibNet/Numerics/SpectralDetrend.cs",
        "Src/MatPlotLibNet/Numerics/SpectralSidesExtensions.cs", "Src/MatPlotLibNet/Models/Series/Categorical/PointplotSeries.cs",
        "Src/MatPlotLibNet/Numerics/GraphLayoutAlgorithm.cs", "Src/MatPlotLibNet.Geo/Projections/ProjectionOutlineExtensions.cs",
        "Src/MatPlotLibNet/Rendering/SeriesRenderers/Circular/RadarSeriesRenderer.cs", "Src/MatPlotLibNet/Rendering/MathText/FractionBox.cs",
        "Src/MatPlotLibNet/Rendering/SeriesRenderers/Field/BarbsSeriesRenderer.cs", "Src/MatPlotLibNet.Geo/GeoAxesExtensions.ProjectedRegions.cs",
        "Src/MatPlotLibNet/Styling/ColorMaps/INormalizer.cs", "Src/MatPlotLibNet.Skia/SkiaRenderContext.AggPixelGrid.cs") { Under = "Src/", Finds = [
        "var from = PointAt(line, at, middle - labelWidth / 2);",
        "double y = bounds.Y + (bounds.Height - barH) / 2;",
        "=> new(plotArea.X + inset, CenterY(plotArea) - box.Height / 2);",
        "var tail = new Point(pt.X - s / 2, pt.Y);",
        "double from = offset - (width / 2);",
        "Ctx.DrawLine(At(perpPos - halfSize * 0.5, crossStart), At(perpPos + halfSize * 0.5, crossStart), stroke);",
        "firstLine[j] = slopeTerm - 0.5 * total[j];",
        "baselines[layer][j] -= total[j] / 2.0;",
        "return Rect.Along(alongX, plotArea.StartAlong(alongX) + (plotArea.LengthAlong(alongX) - box.Length) / 2, box.Length, y, h);",
        "double gap = (bins.BinWidth - barW) / 2;",
        "ContentOffset = (innerWidth - totalContentWidth) / 2,"], Ignores = [
        "double a = 2 * Math.PI * i / n - Math.PI / 2;",
        "var mid = new Point(x + colW / 2, y + rowH / 2);",
        "double v = -width / 2;"] };

/// <summary>The corners of a step line (matplotlib's steps-pre, steps-mid and steps-post) are <c>DrawStyleInterpolation.SteppedBy</c>'s,
    /// for a step series and an ECDF as for a line and an area: no renderer pairs a point's x with its neighbour's y itself (R51). Found by
    /// the shape, a point built from one index and its neighbour's, whatever the arrays are named; and the previous value kept aside.</summary>
    private static readonly OwnedRule StepCorner = new(
        "the corners of a step line (pre, mid or post) are DrawStyleInterpolation.SteppedBy's, a step series' and an ECDF's as a line's and an area's: no renderer pairs a point's x with its neighbour's y itself",
        @"\bnew\s+Point\(\s*\w+\[\s*i\s*[+-]\s*1\s*\]\s*,\s*\w+\[\s*i\s*\]\s*\)|\bnew\s+Point\(\s*\w+\[\s*i\s*\]\s*,\s*\w+\[\s*i\s*[+-]\s*1\s*\]\s*\)|\bnew\s+Point\(\s*\w+\[\s*i\s*\]\s*,\s*prev\w*\s*\)|=\s*\w+\[\s*i\s*[+-]\s*1\s*\]\s*;\s*\w+\[\s*\w+\+\+\s*\]\s*=\s*\w+\[\s*i\s*\]\s*;",
        "Src/MatPlotLibNet/Rendering/SeriesRenderers/DrawStyleInterpolation.cs") { Under = "Src/", Finds = [
        "pts.Add(new Point(pxArr[i + 1], pyArr[i]));",
        "pts.Add(new Point(px[i - 1], py[i]));",
        "pts.Add(new Point(pxMid[i], pyArr[i + 1]));",
        "pts.Add(new Point(pxArr[i], prevPy));",
        "if (i > 0) { xs[n] = x[i - 1]; ys[n++] = y[i]; }"], Ignores = [
        "pts.Add(new Point(px[i], py[i]));",
        "Ctx.DrawLine(new Point(px[i], py[i]), new Point(px[i + 1], py[i + 1]), stroke);"] };

/// <summary>An axis direction picks the X or the Y member of a pair through an Along member of AlongExtensions (the axis, its
    /// breaks, a rectangle's length, start and end, a label's extent across an edge, a range's or a contribution's ends, an axes'
    /// limits, a point at a distance along and across, the member of an AxisPair). A type that keeps the same thing for X and for Y (a
    /// transform, a linked placement, an axes seen with one axis past the primary, the label span of MaxNLocator, the device grid of a
    /// raster export) holds one AxisPair and picks through it. EdgeTerms is the table of what differs per edge. The flag may be named for either direction (isX,
    /// alongX, vertical, horizontal, a series' Vert, a layout's Vertical...): a direction picks a member whatever it is called, in
    /// every package. Which way an orientation runs (a line's, bars', a colour bar's, a Sankey diagram's) is a member of the
    /// orientation in AlongExtensions, so no site compares an orientation to one of its members; and a pick of the X member or the
    /// Y member of one value (its X or its Y, its width or its height, its right or its bottom) is found whatever the condition
    /// before it is.</summary>
    private static readonly OwnedRule AlongRule = new(
        "an axis direction picks the X or the Y member of a pair through an Along member of AlongExtensions (a type that keeps an X and a Y holds one AxisPair), and an orientation says which way it runs through its Along member: no site picks it with a ternary of its own, whatever its flag is called, and no site compares an orientation to one of its members",
        @"\b(?:isX|alongX|AlongX|IsX|alongY|isY|isXAxis|isYAxis|vertical|horizontal|isVertical|isHorizontal|Vertical|Horizontal|IsVertical|IsHorizontal|vert|horiz|Vert|byRow|byColumn|isRow|isColumn|IsRow|IsColumn)\s*(?:\?(?![?.])|$)"
        + @"|\b\w+Along[XY]\s*\?(?![?.])"
        + @"|(?:==|!=|\bis(?:\s+not)?)\s*(?:Bar|Sankey|ColorBar)?Orientation\.(?:Vertical|Horizontal)\b"
        + @"|\?\s*(?<o>(?:[A-Za-z_][\w\[\]]*\.)*)(?<p>\w*?)X(?<s>\w*)\b[^:;?]*:\s*\k<o>\k<p>Y\k<s>\b"
        + @"|\?\s*(?<o>(?:[A-Za-z_][\w\[\]]*\.)*)(?<p>\w*?)Y(?<s>\w*)\b[^:;?]*:\s*\k<o>\k<p>X\k<s>\b"
        + @"|\?\s*(?<o>[A-Za-z_][\w.\[\]]*)\.(?:Width|Right)\s*:\s*\k<o>\.(?:Height|Bottom)\b"
        + @"|\?\s*(?<o>[A-Za-z_][\w.\[\]]*)\.(?:Height|Bottom)\s*:\s*\k<o>\.(?:Width|Right)\b",
        "Src/MatPlotLibNet/Rendering/AlongExtensions.cs", "Src/MatPlotLibNet/Rendering/EdgeTerms.cs") { Under = "Src/", Finds = [
        "var axis = isX ? axes.XAxis : axes.YAxis;",
        "double? limitMin = alongY ? context.YAxisMin : context.XAxisMin;",
        "    var line = alongX",
        "double edge = vertical ? gradient.X + gradient.Width : gradient.Y + gradient.Height;",
        "double ext = WedgeLength(Vertical ? Bar.Height : Bar.Width);",
        "double? limitMin = Vert ? context.XAxisMin : context.YAxisMin;",
        "double crossLen   = vert ? bounds.Width  : bounds.Height;",
        "    var s = horizontal",
        "var at = isHorizontal ? new Point(mid, peak) : new Point(peak, mid);",
        "double width = layout.Vertical ? r.Width : r.Height;",
        "int n = byRow ? rows : cols;",
        "double diff = byColumn ? data[k, i] - data[k, j] : data[i, k] - data[j, k];",
        "var xs = isColumn ? leaves : heights;",
        "int count = isRow ? data.GetLength(0) : data.GetLength(1);",
        "orientation.ValuesAlongX ? upright.Transposed() : upright;",
        "double side = line.Orientation.RunsAlongX ? plot.Width : plot.Height;",
        "double pixel = 1 / (horizontal ? grid.ScaleX : grid.ScaleY);",
        "Orientation = Orientation == BarOrientation.Horizontal ? \"horizontal\" : null,",
        "var orientation = vert == SankeyOrientation.Vertical",
        "if (refLine.Orientation == Orientation.Horizontal)",
        "internal bool Beside => bar.Orientation != ColorBarOrientation.Horizontal;",
        "if (series.Orientation is BarOrientation.Vertical) upright++;",
        "edge = Math.Max(edge, bar.Beside ? plots[i].Right : plots[i].Bottom);",
        "double side = bar.Beside ? plotArea.Width : plotArea.Height;",
        "private double AlongOf(Point point) => Upright ? point.X : point.Y;",
        "internal double[] Across(double[] values) => Upright ? Transform.TransformYUnclipped(values) : Transform.TransformXUnclipped(values);",
        "(axis == DataAxis.X ? axes.XAxis : axes.YAxis).Label"], Ignores = [
        "var look = horizontal?.Look ?? fallback;",
        "bool horizontal = line.From.Y == line.To.Y;",
        "Orientation = BarOrientation.Horizontal,",
        "return new(anchor, onFarEdge ? TextAlignment.Right : TextAlignment.Left);",
        "double highX = xRunsRight ? b.Right : b.X, lowX = xRunsRight ? b.X : b.Right;"] };

    /// <summary>The rules of the area, in the order the scan reads them.</summary>
    internal static OwnedRule[] All { get; } =
    [
        OpenBarbs,
        ArcResolution,
        SectorArcs,
        RoundFrameRule,
        StepLength,
        SpaceLength,
        ShorterSideRule,
        new("the centre of a rectangle is Rect.Center, which reads AlongExtensions.CentreAlong: no site adds half a width to an X or half a height to a Y itself",
            @"\b([\w.]+)\.X \+ \1\.Width (?:/ 2(?:\.0)?|\* 0\.5)\b|\b([\w.]+)\.Y \+ \2\.Height (?:/ 2(?:\.0)?|\* 0\.5)\b|\bX \+ Width / 2|\bY \+ Height / 2",
            "Src/MatPlotLibNet/Rendering/AlongExtensions.cs") { Under = "Src/", Finds = [
            "            _                   => PlotArea.X + PlotArea.Width / 2"] },
        CentredSpan,
        StepCorner,
        new("a point on the rim round a centre at an angle (the screen's, a polar axes', a bearing, a turned frame's down) is PointExtensions' OnRim, AtAngle, AtAngleCounterClockwise, AtBearing or BelowTurnedBy: no site adds a radius times a cosine and a sine to a coordinate",
            @"(?:\b[cC][xy]\b|\b_c[xy]\b|\b\w+\.[XY]\b)\s*[+-]\s*\(?\s*[\w.]+(?:\s*[*+]\s*[\w.()]+)*\)?\s*\*\s*Math\.(?:Cos|Sin)\(",
            "Src/MatPlotLibNet/Rendering/ValueObjects/PointExtensions.cs") { Under = "Src/", Finds = [
            "        new(arc.Center.X + arc.RadiusX * Math.Cos(radians), arc.Center.Y + arc.RadiusY * Math.Sin(radians));"] },
        new("the i-th value of an optional array, else a fallback, is ElementOr: the bounds test is not typed at every part",
            @"\b\w+\s*<\s*[\w.!?]+\.(?:Length|Count)\s*\?\s*[\w.!?]+\[[\w.]+\]|\b\w+\s*<\s*[\w.!?]+\.(?:Length|Count)\s*$",
            "Src/MatPlotLibNet/Extensions/ArrayElementExtensions.cs") { Finds = [
            "                    .. Enumerable.Range(0, leaves).Select(i => i < row.Leaves.Count ? row.Leaves[i].ToString(CultureInfo.InvariantCulture) : string.Empty),"] },
        new("a value held between 0 and 1 is value.Saturate(): the clamp to the unit interval is not typed at every site",
            @"Math\.Clamp\([^;]*,\s*0(?:\.0)?\s*,\s*1(?:\.0)?\)",
            "Src/MatPlotLibNet/Styling/UnitIntervalExtensions.cs") { Finds = [
            "    public void SetOpacity(double opacity) => _opacity = Math.Clamp(opacity, 0.0, 1.0);"] },
        AlongRule,
        OwnedRule.Nowhere("which way a straight segment runs, and whether it runs neither way, is the segment's (StraightAlongX, Slanted in AlongExtensions): no site compares the ends' coordinates itself",
            @"\b(\w+)\.([XY])\s*==\s*(\w+)\.\2\s*\|\|\s*\1\.(?!\2)[XY]\s*==\s*\3\.(?!\2)[XY]\b", under: "Src/") with { Finds = [
            "        bool axisAligned = from.X == to.X || from.Y == to.Y;"] },
        OwnedRule.Nowhere("the X axis and the primary Y axis of an axes as one pair are Axes.PrimaryAxes (AlongExtensions): no site builds that pair itself",
            @"new\s+AxisPair<Axis>\s*\(", under: "Src/") with { Finds = [
            "        var axes = new AxisPair<Axis>(Axes.XAxis, Axes.YAxis);"] },
        OwnedRule.Nowhere("a flag per direction (which axes a mark lies beyond, which axes the patches range) is one AxisPair<bool>, combined through AlongExtensions (Any, |, &): no type of its own holds a bool X and a bool Y",
            @"\b(?:struct|class|record)\s+\w+\s*\(\s*bool\s+X\s*,\s*bool\s+Y\s*\)", under: "Src/") with { Finds = [
            "internal readonly record struct BeyondAxes(bool X, bool Y);"] },
    ];
}
