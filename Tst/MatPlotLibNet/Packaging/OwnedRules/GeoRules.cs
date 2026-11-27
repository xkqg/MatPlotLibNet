// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Packaging.OwnedRules;

/// <summary>The rules of the map package: a point off a projection's domain, the numbers a projection is made with, its centre, its
/// bounds and its outline, the width of a map's lines, and the theme its series draw under.</summary>
internal static class GeoRules
{
    /// <summary>The rules of the area, in the order the scan reads them.</summary>
    internal static OwnedRule[] All { get; } =
    [
        new("a point off a projection's domain is ProjectedPoint's own value (ProjectedPoint.OffDomain, point.IsOffDomain): no projection builds the pair of missing coordinates and no reader tests it itself",
            @"\bnew\s*\(\s*double\.NaN\s*,\s*double\.NaN\s*\)|\bIsNaN\(\w+(?:\.\w+)*\.X\)|\bIsNaN\(x\)\s*&&\s*!double\.IsNaN\(y\)",
            "Src/MatPlotLibNet.Geo/Projections/ProjectedPointExtensions.cs") { Under = "Src/MatPlotLibNet.Geo/", Finds = [
            "        if (cosC < 0) return new(double.NaN, double.NaN);",
            "        if (double.IsNaN(projected.X) || double.IsNaN(projected.Y)) return;",
            "        if (!double.IsNaN(far.Point.X) && !double.IsNaN(far.Point.Y)) return far.Point;",
            "            if (!double.IsNaN(x) && !double.IsNaN(y))"] },
        new("the numbers a projection is made with are a ProjectionParameterSet: the PROJ names are spelled once, and no projection keeps a dictionary of its own or makes itself again key by key",
            @"""(?:lat_[012]|lon_0)""|\bnew Dictionary<string, double>\s*\{\s*\[|\bGetValueOrDefault\(""",
            "Src/MatPlotLibNet.Geo/Projections/ProjectionParameterSet.cs") { Under = "Src/MatPlotLibNet.Geo/", Finds = [
            "        _parameters = new() { [\"lat_0\"] = centerLat, [\"lon_0\"] = centerLon };",
            "        new Dictionary<string, double> { [key] = CenterLat };",
            "        new TransverseMercator(parameters.GetValueOrDefault(\"lon_0\", 0));"] },
        new("the centre of an azimuthal projection is a CentredFrame (a place seen from it a CentredPlace): no projection works out the sine and cosine of its centre itself",
            @"\b_?[sScC](?:in|os)(?:Phi|Lat)0\b",
            "Src/MatPlotLibNet.Geo/Projections/CentredFrame.cs", "Src/MatPlotLibNet.Geo/Projections/CentredPlace.cs") { Under = "Src/MatPlotLibNet.Geo/", Finds = [
            "        _cosPhi0 = Math.Cos(_phi0);",
            "        _sinLat0 = Math.Sin(lat0);"] },
        new("a projection's bounds are worked out by GeoBoundsExtensions (WorldEdgeBounds, GeoBounds.Symmetric) from the projection's own geometry or the numbers cartopy states: no projection types a box or the world-edge formula itself",
            @"\bGeoBounds Bounds\s*(?:$|\{(?!\s*get;)|=>\s*(?:new\b|$))|\bForward\(0,\s*180\)",
            "Src/MatPlotLibNet.Geo/Projections/GeoBoundsExtensions.cs") { Under = "Src/MatPlotLibNet.Geo/", Finds = [
            "    public GeoBounds Bounds => new(-180, 180, -90, 90);",
            "    public GeoBounds Bounds =>",
            "    public GeoBounds Bounds",
            "            var (xMax, _) = Forward(0, 180);"], Ignores = [
            "    GeoBounds Bounds { get; }",
            "    public GeoBounds Bounds => this.WorldEdgeBounds();",
            "    public GeoBounds Bounds => GeoBounds.Symmetric(Radius, Radius);"] },
        OwnedRule.Nowhere("the outline of a map is traced by its projection (IProjectionOutline.TraceOutline) and kept once by ProjectionOutlineExtensions.Outline: no projection caches an outline of its own",
            @"\b_outline\b|\?\?=\s*(?:this\.MeridianOutline|Bounds\.InscribedCircle)", under: "Src/MatPlotLibNet.Geo/") with { Finds = [
            "    private IReadOnlyList<GeoJson.GeoPosition>? _outline;",
            "    IReadOnlyList<GeoPosition> IProjectionOutline.Outline => cache ??= Bounds.InscribedCircle(61);"] },
        new("the width of a map's coastlines, borders and region outlines is GeoAxesExtensions' (CoastlineWidth, BorderWidth, RegionEdgeWidth): no verb types a width (the series' resting width is its own, RestingStrokeWidth)",
            @"\b\w*(?:[lL]ine|[sS]troke|Edge)Width\s*=\s*\d*\.\d+\b",
            "Src/MatPlotLibNet.Geo/GeoAxesExtensions.cs", "Src/MatPlotLibNet.Geo/Series/GeoPolygonSeries.cs") { Under = "Src/MatPlotLibNet.Geo/", Finds = [
            "        public AxesBuilder Coastlines(Color? color = null, double lineWidth = 0.5) =>",
            "                StrokeWidth = 0.3,"] },
        OwnedRule.Nowhere("a map's series draws under a theme, never null: its renderer reads the theme of its pass (SeriesRenderContext.Theme)",
            RenderingRules.NoThemePattern, under: "Src/MatPlotLibNet.Geo/") with { Finds = [
            "    internal Color OutlineUnder(Theme? theme) => EdgeColor ?? theme?.Neutral ?? Colors.Black;"] },
    ];
}
