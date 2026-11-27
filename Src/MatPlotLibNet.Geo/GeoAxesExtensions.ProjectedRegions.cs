// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.GeoJson;
using MatPlotLibNet.Geo.Projections;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using System.Numerics;

namespace MatPlotLibNet.Geo;

public static partial class GeoAxesExtensions
{
    // A region (a feature) in the plane of a projection: where its polygons stand, how large they are and where their centre
    // is, and the same region resized about its centre. The ground of the maps that draw a region by a value.

    /// <summary>The polygons of the feature on <paramref name="projection"/>. A position the projection cannot place is
    /// left out of its ring; a polygon whose exterior is left with fewer than three positions, and a hole with fewer than
    /// three, are left out.</summary>
    internal static IReadOnlyList<ProjectedPolygon> ProjectedBy(GeoFeature feature, IGeoProjection projection)
    {
        IReadOnlyList<ProjectedPoint> Ring(IReadOnlyList<GeoPosition> ring) =>
            [.. ring.Select(p => projection.Forward(p.Lat, p.Lon)).Where(p => !p.IsOffDomain)];

        var polygons = new List<ProjectedPolygon>();
        foreach (var polygon in feature.Geometry.Polygons)
        {
            var exterior = Ring(polygon.Exterior);
            if (exterior.Count < 3) continue;
            polygons.Add(new ProjectedPolygon(exterior, [.. polygon.Holes.Select(Ring).Where(hole => hole.Count >= 3)]));
        }

        return polygons;
    }

    /// <summary>The features that have an area on <paramref name="projection"/>, each resized by a value so that together they
    /// still meet at their borders (<see cref="GeoAxesExtensions.FlowCartogram"/>), their properties copied.</summary>
    /// <param name="projection">The map projection.</param>
    /// <param name="valueOf">The value of a feature; see <see cref="GeoAxesExtensions.FlowCartogram"/> for a value of zero or of none.</param>
    /// <returns>The features, their coordinates already projected (a series that draws them is raw-projected).</returns>
    /// <param name="features">The value it works on.</param>
    internal static List<GeoFeature> FlowedOn(IReadOnlyList<GeoFeature> features, IGeoProjection projection, Func<GeoFeature, double> valueOf)
    {
        var kept = new List<GeoFeature>();
        var regions = new List<IReadOnlyList<ProjectedPolygon>>();
        foreach (var feature in features)
        {
            var polygons = ProjectedBy(feature, projection);
            if (Measure(polygons) is null) continue;
            kept.Add(feature);
            regions.Add(polygons);
        }

        var resized = new FlowCartogram(regions, [.. kept.Select(valueOf)]).Equalized();
        return [.. resized.Select((geometry, i) => new GeoFeature(geometry, new Dictionary<string, string>(kept[i].Properties)))];
    }

    /// <summary>The area and the centre of the region.</summary>
    /// <returns>The measure; <see langword="null"/> for a region with no area (no polygon, or a degenerate one).</returns>
    /// <param name="region">The value it works on.</param>
    internal static RegionMeasure? Measure(IReadOnlyList<ProjectedPolygon> region)
    {
        double area = 0, cx = 0, cy = 0;
        foreach (var polygon in region)
        {
            Add(polygon.Exterior, 1);
            foreach (var hole in polygon.Holes) Add(hole, -1);
        }

        return area > 0 ? new RegionMeasure(area, new ProjectedPoint(cx / area, cy / area)) : null;

        void Add(IReadOnlyList<ProjectedPoint> ring, double sign)
        {
            double twiceArea = 0, rx = 0, ry = 0;
            for (int i = 0; i < ring.Count; i++)
            {
                var p = ring[i];
                var q = ring[(i + 1) % ring.Count];
                double cross = p.X * q.Y - q.X * p.Y;
                twiceArea += cross;
                rx += (p.X + q.X) * cross;
                ry += (p.Y + q.Y) * cross;
            }

            if (twiceArea == 0) return;
            double ringArea = Math.Abs(twiceArea) / 2;
            area += sign * ringArea;
            cx += sign * ringArea * rx / (3 * twiceArea);
            cy += sign * ringArea * ry / (3 * twiceArea);
        }
    }

    /// <summary>The region resized about <paramref name="centre"/>: every position moved to <paramref name="factor"/> times
    /// its distance from the centre, so the centre stays where it is and the area becomes <c>factor²</c> times as large.</summary>
    /// <returns>The geometry, its coordinates already projected (a series that draws it is raw-projected).</returns>
    internal static GeoGeometry ScaledAbout(IReadOnlyList<ProjectedPolygon> region, ProjectedPoint centre, double factor)
    {
        GeoPosition Scale(ProjectedPoint p) => new(centre.X + factor * (p.X - centre.X), centre.Y + factor * (p.Y - centre.Y));

        return GeoGeometry.OfPolygons([.. region.Select(polygon => GeoPolygon.FromRings(
            [.. polygon.Exterior.Select(Scale)], polygon.Holes.Select(hole => (IReadOnlyList<GeoPosition>)[.. hole.Select(Scale)])))]);
    }

    /// <summary>A contiguous cartogram made by the fast flow-based method of Gastner, Seguy and More (PNAS 115, E2156, 2018): the
    /// regions are laid on a lattice, each cell holding the density of the regions over it weighted by how much of it each covers,
    /// and the mean density where none does; the density is then let flow from <c>rho(t) = (1 - t) rho0 + t mean</c> over <c>t</c>
    /// from 0 to 1, along the one flux whose divergence is <c>rho0 - mean</c>, so every vertex moves with the velocity
    /// <c>flux / rho(t)</c> and every region ends with the area its value asks for. The flux is a cosine series on the library's FFT,
    /// solved once per flow; the flow is laid again on the moved regions until every region is within 1 % of its area, at most
    /// twenty times. The numbers are those of the method's own program (go_cart): a lattice 512 cells on its longer side with the
    /// map in its middle two thirds, a blur 5 cells wide before the first flow, the step of the integration starting at 0.01, grown
    /// by 1.1 after a step that holds and cut by 0.75 after one that does not, and the velocity read between the cell centres.</summary>
    /// <remarks>A vertex is moved by the same field wherever it stands, so a border two regions share stays one line; an edge is cut
    /// first into pieces no longer than a cell, so that it bends with the flow, and the pieces of an edge two regions share are the
    /// same points in both. A region a few cells across cannot be resolved to the cell: on a world map of population, 147 of the 149
    /// countries no outline rule raises end within 1 % after twenty flows, and Mongolia and Kosovo within 7 %.</remarks>
    internal sealed class FlowCartogram
    {
        /// <summary>The cells along the longer side of the lattice (go_cart's <c>L</c>).</summary>
        private const int LongSide = 512;

        /// <summary>How much larger than the map the lattice is (go_cart's <c>PADDING</c>).</summary>
        private const double Padding = 1.5;

        /// <summary>The width, in cells, of the Gaussian blur laid over the density before the first flow (go_cart's <c>BLUR_WIDTH</c>).</summary>
        private const double BlurWidth = 5;

        /// <summary>The area error the flows stop at (go_cart's <c>MAX_PERMITTED_AREA_ERROR</c>).</summary>
        private const double MostAreaError = 0.01;

        /// <summary>A region's share of the values is at least this times its share of the perimeter (go_cart's
        /// <c>MIN_PERIMETER_FAC</c>)...</summary>
        private const double MinPerimeterShare = 0.025;

        /// <summary>...and at least this (go_cart's floor of the perimeter rule).</summary>
        private const double SmallestShare = 0.00025;

        /// <summary>The most flows laid on one map. A region a few cells across converges by about a tenth a flow, and a flow of a
        /// world map costs about 20 ms of processor time; go_cart sets no limit.</summary>
        private const int MostFlows = 20;

        /// <summary>The rays read across each row of cells for the cover of a cell.</summary>
        private const int RaysPerRow = 4;

        private readonly IReadOnlyList<IReadOnlyList<ProjectedPolygon>> _regions;
        private readonly double[] _values;

        /// <summary>The velocity of a vertex, in cells per unit of time.</summary>
        private readonly record struct Velocity(double X, double Y);

        // the lattice
        private int _nx, _ny;
        private double _scale, _centreX, _centreY;

        // every vertex of every ring, in cells, ring after ring; region -> polygon -> exterior, holes
        private double[] _x = [], _y = [];
        private int[] _ringStart = [];
        private int[] _ringRegion = [];
        private bool[] _ringIsHole = [];
        private int[] _polygonStart = [];
        private int[] _regionPolygonStart = [];

        // the field of the flow under way, and what laying it out and following it needs, made once
        private double[] _density = [], _fluxX = [], _fluxY = [], _cover = [];
        private double _mean;
        private List<double>[] _cuts = [];
        private double[] _vx = [], _vy = [], _midX = [], _midY = [];

        /// <summary>A cartogram that has the regions it is given take the areas their values ask for.</summary>
        /// <param name="regions">The regions, each the polygons of its area on the map.</param>
        /// <param name="values">The value of each region: zero or above, the region ends with an area in proportion to it, but a region
        /// whose share of the values is below <c>max(0.025 * its share of the perimeter, 0.00025)</c> takes that share; anything else
        /// (below zero, no number), the region keeps the mean density of the map and with it its area.</param>
        internal FlowCartogram(IReadOnlyList<IReadOnlyList<ProjectedPolygon>> regions, double[] values)
        {
            _regions = regions;
            _values = values;
        }

        /// <summary>The regions with the areas their values ask for, in the order given, their coordinates on the map.</summary>
        /// <returns>One geometry per region; the regions as they are when no region has a value above zero.</returns>
        internal IReadOnlyList<GeoGeometry> Equalized()
        {
            if (!_values.Any(v => v > 0 && double.IsFinite(v))) return [.. _regions.Select(region => Geometry(region.Select(RingsOf)))];

            LayOnTheLattice();
            Cut();
            var lattice = new CosineLattice(_nx, _ny);
            (_density, _fluxX, _fluxY, _cover) = (new double[_nx * _ny], new double[_nx * _ny], new double[_nx * _ny], new double[_nx * _ny]);
            _cuts = [.. Enumerable.Range(0, _ny * RaysPerRow).Select(_ => new List<double>())];
            (_vx, _vy, _midX, _midY) = (new double[_x.Length], new double[_x.Length], new double[_x.Length], new double[_x.Length]);
            double[] targets = Targets(Areas());
            double startArea = targets.Sum();
            for (int flow = 0; ; flow++)
            {
                double[] areas = Areas();
                if (flow == MostFlows || AreaError(areas, targets) <= MostAreaError) break;
                SolveTheFlux(lattice, areas, targets, blur: flow == 0);
                Flow();
            }

            ScaleTo(startArea);
            return [.. Enumerable.Range(0, _regions.Count).Select(OnTheMap)];
        }

        // ---- the regions on the lattice -------------------------------------------------------------------------------------------

        /// <summary>Sizes the lattice to the map, the map in the middle of it, and puts every vertex on it in cells.</summary>
        private void LayOnTheLattice()
        {
            var points = _regions.SelectMany(r => r).SelectMany(p => p.Holes.Prepend(p.Exterior)).SelectMany(ring => ring).ToList();
            double minX = points.Min(p => p.X), maxX = points.Max(p => p.X), minY = points.Min(p => p.Y), maxY = points.Max(p => p.Y);
            double width = maxX - minX, height = maxY - minY;
            _scale = LongSide / (Padding * Math.Max(width, height));
            _nx = width >= height ? LongSide : Fft.NextPowerOf2((int)Math.Ceiling(Padding * width * _scale));
            _ny = width >= height ? Fft.NextPowerOf2((int)Math.Ceiling(Padding * height * _scale)) : LongSide;
            _centreX = (minX + maxX) / 2;
            _centreY = (minY + maxY) / 2;

            var x = new List<double>();
            var y = new List<double>();
            var ringStart = new List<int>();
            var ringRegion = new List<int>();
            var ringIsHole = new List<bool>();
            var polygonStart = new List<int>();
            var regionPolygonStart = new List<int>();
            for (int region = 0; region < _regions.Count; region++)
            {
                regionPolygonStart.Add(polygonStart.Count);
                foreach (var polygon in _regions[region])
                {
                    polygonStart.Add(ringStart.Count);
                    for (int ring = 0; ring <= polygon.Holes.Count; ring++)
                    {
                        ringStart.Add(x.Count);
                        ringRegion.Add(region);
                        ringIsHole.Add(ring > 0);
                        foreach (var point in ring == 0 ? polygon.Exterior : polygon.Holes[ring - 1])
                        {
                            x.Add((point.X - _centreX) * _scale + _nx / 2.0);
                            y.Add((point.Y - _centreY) * _scale + _ny / 2.0);
                        }
                    }
                }
            }

            ringStart.Add(x.Count);
            polygonStart.Add(ringStart.Count - 1);
            regionPolygonStart.Add(polygonStart.Count - 1);
            (_x, _y, _ringStart, _ringRegion, _ringIsHole) = ([.. x], [.. y], [.. ringStart], [.. ringRegion], [.. ringIsHole]);
            (_polygonStart, _regionPolygonStart) = ([.. polygonStart], [.. regionPolygonStart]);
        }

        /// <summary>Cuts every edge longer than a cell into equal pieces, so that the edges bend with the next flow. The pieces are
        /// measured from the lesser end of the edge, so an edge two rings share, walked one way in one and the other way in the
        /// other, gives the same points in both.</summary>
        private void Cut()
        {
            var x = new List<double>(_x.Length);
            var y = new List<double>(_y.Length);
            var ringStart = new int[_ringStart.Length];
            for (int ring = 0; ring + 1 < _ringStart.Length; ring++)
            {
                ringStart[ring] = x.Count;
                int first = _ringStart[ring], end = _ringStart[ring + 1];
                for (int i = first; i < end; i++)
                {
                    double ax = _x[i], ay = _y[i];
                    x.Add(ax);
                    y.Add(ay);
                    if (i == end - 1) break;
                    double bx = _x[i + 1], by = _y[i + 1];
                    bool forward = ax < bx || (ax == bx && ay < by);
                    double lowX = forward ? ax : bx, lowY = forward ? ay : by, highX = forward ? bx : ax, highY = forward ? by : ay;
                    int pieces = (int)Math.Ceiling(new Point(highX - lowX, highY - lowY).Length);
                    for (int k = 1; k < pieces; k++)
                    {
                        double f = (double)(forward ? k : pieces - k) / pieces;
                        x.Add(lowX + (highX - lowX) * f);
                        y.Add(lowY + (highY - lowY) * f);
                    }
                }
            }

            ringStart[^1] = x.Count;
            (_x, _y, _ringStart) = ([.. x], [.. y], ringStart);
        }

        /// <summary>The area each region has now, in cells: its exteriors less its holes.</summary>
        private double[] Areas()
        {
            var areas = new double[_regions.Count];
            for (int ring = 0; ring + 1 < _ringStart.Length; ring++)
            {
                int first = _ringStart[ring], end = _ringStart[ring + 1];
                double twice = 0;
                for (int i = first; i < end; i++)
                {
                    int next = i + 1 < end ? i + 1 : first;
                    twice += _x[i] * _y[next] - _x[next] * _y[i];
                }

                areas[_ringRegion[ring]] += (_ringIsHole[ring] ? -0.5 : 0.5) * Math.Abs(twice);
            }

            return areas;
        }

        /// <summary>The area each region is to end with: the areas of the map shared out in proportion to the values, a region of no
        /// value taking the mean density of the regions that have one. A region whose share of the values is below
        /// <c>max(0.025 * its share of the perimeter, 0.00025)</c> takes that share, so that its outline stays one that can be drawn
        /// and the flow is not asked for a region of no area (go_cart's <c>fill_with_density1</c>).</summary>
        private double[] Targets(double[] areas)
        {
            double[] values = [.. _values.Select(v => v >= 0 && double.IsFinite(v) ? v : double.NaN)];
            double[] perimeters = Perimeters();
            double valued = values.Where(v => !double.IsNaN(v)).Sum(), perimeter = perimeters.Sum();
            for (int r = 0; r < values.Length; r++)
            {
                double least = Math.Max(MinPerimeterShare * perimeters[r] / perimeter, SmallestShare);
                if (values[r] / valued < least) values[r] = least * valued;
            }

            valued = 0;
            double valuedArea = 0;
            for (int r = 0; r < values.Length; r++)
            {
                if (double.IsNaN(values[r])) continue;
                valued += values[r];
                valuedArea += areas[r];
            }

            for (int r = 0; r < values.Length; r++)
                if (double.IsNaN(values[r])) values[r] = areas[r] * valued / valuedArea;

            double total = areas.Sum(), sum = values.Sum();
            return [.. values.Select(v => total * v / sum)];
        }

        /// <summary>The length of the outline of each region: its exteriors and its holes.</summary>
        private double[] Perimeters()
        {
            var perimeters = new double[_regions.Count];
            for (int ring = 0; ring + 1 < _ringStart.Length; ring++)
            {
                int first = _ringStart[ring], end = _ringStart[ring + 1];
                for (int i = first; i < end; i++)
                {
                    int next = i + 1 < end ? i + 1 : first;
                    perimeters[_ringRegion[ring]] += new Point(_x[i], _y[i]).DistanceTo(new Point(_x[next], _y[next]));
                }
            }

            return perimeters;
        }

        /// <summary>The largest error of a region's area against its target, the areas taken at the scale of the targets.</summary>
        private static double AreaError(double[] areas, double[] targets)
        {
            double scale = targets.Sum() / areas.Sum(), largest = 0;
            for (int r = 0; r < areas.Length; r++) largest = Math.Max(largest, Math.Abs(areas[r] * scale / targets[r] - 1));
            return largest;
        }

        // ---- the flux -------------------------------------------------------------------------------------------------------------

        /// <summary>Lays the density on the lattice (<see cref="GeoAxesExtensions.FlowCartogram.LayTheDensity"/>) and solves the flux whose divergence is the density less its mean: for the density's coefficients <c>c</c>, the
        /// flux has <c>c (kx / Nx) / (pi |k|²)</c> as sine-cosine series along X and <c>c (ky / Ny) / (pi |k|²)</c> as cosine-sine
        /// series along Y, <c>|k|² = (kx / Nx)² + (ky / Ny)²</c>.</summary>
        private void SolveTheFlux(CosineLattice lattice, double[] areas, double[] targets, bool blur)
        {
            LayTheDensity([.. targets.Select((target, r) => target / areas[r])], targets.Sum() / areas.Sum());

            double[] coefficients = _fluxX;
            Array.Copy(_density, coefficients, _density.Length);
            lattice.ToCoefficients(coefficients);
            if (blur)
            {
                // exp(-1/2 w² pi² |k|²): a Gaussian of width w laid over the density (go_cart's gaussian_blur).
                for (int ky = 0; ky < _ny; ky++)
                for (int kx = 0; kx < _nx; kx++)
                    coefficients[ky * _nx + kx] *= Math.Exp(-0.5 * BlurWidth * BlurWidth * Math.PI * Math.PI * Wave(kx, ky));
                Array.Copy(coefficients, _density, _density.Length);
                lattice.ToSums(_density, sineAlongX: false, sineAlongY: false);
            }

            _mean = coefficients[0];
            for (int ky = 0; ky < _ny; ky++)
            for (int kx = 0; kx < _nx; kx++)
            {
                int k = ky * _nx + kx;
                double wave = Math.PI * Wave(kx, ky), c = coefficients[k];
                _fluxX[k] = k == 0 ? 0 : c * kx / _nx / wave;
                _fluxY[k] = k == 0 ? 0 : c * ky / _ny / wave;
            }

            lattice.ToSums(_fluxX, sineAlongX: true, sineAlongY: false);
            lattice.ToSums(_fluxY, sineAlongX: false, sineAlongY: true);
        }

        /// <summary>|k|² of a wave of the lattice, in cycles per cell.</summary>
        private double Wave(int kx, int ky) => (double)kx * kx / ((double)_nx * _nx) + (double)ky * ky / ((double)_ny * _ny);

        /// <summary>Lays on each cell the density over it: the densities of the regions weighted by how much of the cell each covers, and the
        /// mean over what no region covers. The cover is read along <see cref="GeoAxesExtensions.FlowCartogram.RaysPerRow"/> rays across each row of cells: a ray is cut
        /// by the edges of every ring of a region, the stretches between the first and second cut, the third and fourth and so on are
        /// the region's (so a hole is left out), and each stretch covers the cells it crosses by its length in each.</summary>
        private void LayTheDensity(double[] densities, double mean)
        {
            int rays = _ny * RaysPerRow;
            double[] mass = _density, cover = _cover;
            List<double>[] cuts = _cuts;
            Array.Clear(mass);
            Array.Clear(cover);

            int ring = 0;
            for (int region = 0; region < _regions.Count; region++)
            {
                for (; ring + 1 < _ringStart.Length && _ringRegion[ring] == region; ring++)
                {
                    int first = _ringStart[ring], end = _ringStart[ring + 1];
                    for (int i = first; i < end; i++)
                    {
                        int next = i + 1 < end ? i + 1 : first;
                        double y0 = _y[i], y1 = _y[next];
                        if (y0 == y1) continue;
                        // ray g runs at y = (g + 1/2) / RaysPerRow; an edge cuts the rays in [min(y0, y1), max(y0, y1)).
                        int from = Math.Max(0, (int)Math.Ceiling(Math.Min(y0, y1) * RaysPerRow - 0.5));
                        int to = Math.Min(rays - 1, (int)Math.Ceiling(Math.Max(y0, y1) * RaysPerRow - 0.5) - 1);
                        for (int g = from; g <= to; g++)
                            cuts[g].Add(_x[i] + ((g + 0.5) / RaysPerRow - y0) * (_x[next] - _x[i]) / (y1 - y0));
                    }
                }

                for (int g = 0; g < rays; g++)
                {
                    var ray = cuts[g];
                    if (ray.Count == 0) continue;
                    ray.Sort();
                    int row = g / RaysPerRow * _nx;
                    for (int c = 0; c + 1 < ray.Count; c += 2)
                    {
                        double left = Math.Max(0, ray[c]), right = Math.Min(_nx, ray[c + 1]);
                        for (int i = (int)left; i < right && i < _nx; i++)
                        {
                            double length = (Math.Min(right, i + 1) - Math.Max(left, i)) / RaysPerRow;
                            mass[row + i] += length * densities[region];
                            cover[row + i] += length;
                        }
                    }

                    ray.Clear();
                }
            }

            // Two regions meet along a line, so a cell covered beyond the whole by rounding is their weighted mean.
            for (int cell = 0; cell < mass.Length; cell++)
                mass[cell] = cover[cell] >= 1 ? mass[cell] / cover[cell] : mass[cell] + (1 - cover[cell]) * mean;
        }

        // ---- the flow -------------------------------------------------------------------------------------------------------------

        /// <summary>Moves every vertex from t = 0 to 1 by the velocity of the flux, a step at a time: a step is taken by Euler and
        /// by the midpoint rule, and holds when the two land within <c>min(Nx, Ny) 1e-6</c> (squared) of each other and inside the
        /// lattice.</summary>
        private void Flow()
        {
            int n = _x.Length;
            double[] vx = _vx, vy = _vy;
            double tolerance = Math.Min(_nx, _ny) * 1e-6;
            double t = 0, dt = 1e-2;
            bool known = false;
            while (t < 1)
            {
                // The last step lands on 1, where the density is the mean everywhere; past it, it could fall below zero.
                dt = Math.Min(dt, 1 - t);
                if (!known)
                {
                    for (int k = 0; k < n; k++) (vx[k], vy[k]) = VelocityAt(_x[k], _y[k], t);
                    known = true;
                }

                if (Step(t, dt, vx, vy, _midX, _midY, tolerance))
                {
                    (_x, _midX) = (_midX, _x);
                    (_y, _midY) = (_midY, _y);
                    t += dt;
                    dt *= 1.1;
                    known = false;
                }
                else
                {
                    dt *= 0.75;
                }
            }
        }

        /// <summary>One step of every vertex, written to <paramref name="midX"/> and <paramref name="midY"/>. The velocity at the
        /// half step is read wherever it lands (beyond a wall by the rule of <see cref="VelocityAt"/>); whether the step holds is
        /// decided by where it ends (<see cref="Holds"/>).</summary>
        /// <returns>Whether the step holds.</returns>
        private bool Step(double t, double dt, double[] vx, double[] vy, double[] midX, double[] midY, double tolerance)
        {
            for (int k = 0; k < _x.Length; k++)
            {
                double halfX = _x[k] + 0.5 * dt * vx[k], halfY = _y[k] + 0.5 * dt * vy[k];
                var half = VelocityAt(halfX, halfY, t + 0.5 * dt);
                midX[k] = _x[k] + dt * half.X;
                midY[k] = _y[k] + dt * half.Y;
                if (!Holds(new Point(_x[k] + dt * vx[k], _y[k] + dt * vy[k]), new Point(midX[k], midY[k]), tolerance, _nx, _ny)) return false;
            }

            return true;
        }

        /// <summary>Whether a step of a vertex holds: its Euler landing and its midpoint landing lie within
        /// <paramref name="tolerance"/> (squared) of each other, and the midpoint lies inside the lattice of <paramref name="nx"/> by
        /// <paramref name="ny"/> cells, its walls included. A landing that is no number does not hold.</summary>
        /// <param name="euler">Where the step by Euler lands, in cells.</param>
        /// <param name="mid">Where the step by the midpoint rule lands, in cells.</param>
        /// <param name="tolerance">The most the two may lie apart, squared.</param>
        /// <param name="nx">The cells across the lattice.</param>
        /// <param name="ny">The cells up the lattice.</param>
        internal static bool Holds(Point euler, Point mid, double tolerance, int nx, int ny) =>
            new Point(mid.X - euler.X, mid.Y - euler.Y).SquaredLength <= tolerance
            && mid.X >= 0 && mid.X <= nx && mid.Y >= 0 && mid.Y <= ny;

        /// <summary>The velocity at a point at time <paramref name="t"/>: the flux over the density <c>(1 - t) rho0 + t mean</c>,
        /// each read between the four cell centres round the point. Beyond the outer centres the flux across a wall is 0 at the
        /// wall, and everything else keeps the value of the outer centre (go_cart's <c>interpol</c>).</summary>
        private Velocity VelocityAt(double x, double y, double t)
        {
            int i0 = (int)Math.Floor(x + 0.5) - 1, j0 = (int)Math.Floor(y + 0.5) - 1;
            double x0 = i0 < 0 ? 0 : i0 + 0.5, x1 = i0 + 1 >= _nx ? _nx : i0 + 1.5;
            double y0 = j0 < 0 ? 0 : j0 + 0.5, y1 = j0 + 1 >= _ny ? _ny : j0 + 1.5;
            double fx = (x - x0) / (x1 - x0), fy = (y - y0) / (y1 - y0);

            var v00 = CellVelocity(i0, j0, t);
            var v10 = CellVelocity(i0 + 1, j0, t);
            var v01 = CellVelocity(i0, j0 + 1, t);
            var v11 = CellVelocity(i0 + 1, j0 + 1, t);
            double w00 = (1 - fx) * (1 - fy), w10 = fx * (1 - fy), w01 = (1 - fx) * fy, w11 = fx * fy;
            return new Velocity(
                w00 * v00.X + w10 * v10.X + w01 * v01.X + w11 * v11.X,
                w00 * v00.Y + w10 * v10.Y + w01 * v01.Y + w11 * v11.Y);
        }

        /// <summary>The velocity at a cell centre, or at a wall for -1 and Nx (Ny): the flux there over the density there.</summary>
        private Velocity CellVelocity(int i, int j, double t)
        {
            double density = (1 - t) * At(_density, i, j, zeroAtX: false, zeroAtY: false) + t * _mean;
            return new Velocity(At(_fluxX, i, j, zeroAtX: true, zeroAtY: false) / density, At(_fluxY, i, j, zeroAtX: false, zeroAtY: true) / density);
        }

        /// <summary>The value at cell centre (i, j), or at the wall for -1 and Nx (Ny).</summary>
        private double At(double[] field, int i, int j, bool zeroAtX, bool zeroAtY)
        {
            if (i < 0 || i >= _nx)
            {
                if (zeroAtX) return 0;
                i = Math.Clamp(i, 0, _nx - 1);
            }

            if (j < 0 || j >= _ny)
            {
                if (zeroAtY) return 0;
                j = Math.Clamp(j, 0, _ny - 1);
            }

            return field[j * _nx + i];
        }

        // ---- back on the map ------------------------------------------------------------------------------------------------------

        /// <summary>Scales the regions about the centre of the lattice to <paramref name="area"/> together, the area they started with.</summary>
        private void ScaleTo(double area)
        {
            double factor = Math.Sqrt(area / Areas().Sum());
            for (int k = 0; k < _x.Length; k++)
            {
                _x[k] = _nx / 2.0 + factor * (_x[k] - _nx / 2.0);
                _y[k] = _ny / 2.0 + factor * (_y[k] - _ny / 2.0);
            }
        }

        /// <summary>The geometry of a region, its coordinates back on the map.</summary>
        private GeoGeometry OnTheMap(int region) =>
            Geometry(Enumerable.Range(_regionPolygonStart[region], _regionPolygonStart[region + 1] - _regionPolygonStart[region])
                .Select(polygon => Enumerable.Range(_polygonStart[polygon], _polygonStart[polygon + 1] - _polygonStart[polygon])
                    .Select(ring => (IReadOnlyList<GeoPosition>)[.. Enumerable.Range(_ringStart[ring], _ringStart[ring + 1] - _ringStart[ring])
                        .Select(k => new GeoPosition((_x[k] - _nx / 2.0) / _scale + _centreX, (_y[k] - _ny / 2.0) / _scale + _centreY))])
                    .ToList()));

        /// <summary>The rings of a polygon as they stand, the exterior first.</summary>
        private static IReadOnlyList<IReadOnlyList<GeoPosition>> RingsOf(ProjectedPolygon polygon) =>
            [.. polygon.Holes.Prepend(polygon.Exterior).Select(ring => (IReadOnlyList<GeoPosition>)[.. ring.Select(p => new GeoPosition(p.X, p.Y))])];

        /// <summary>A Polygon or MultiPolygon of polygons given as their rings, the exterior first.</summary>
        private static GeoGeometry Geometry(IEnumerable<IReadOnlyList<IReadOnlyList<GeoPosition>>> polygonRings) =>
            GeoGeometry.OfPolygons([.. polygonRings.Select(rings => GeoPolygon.FromRings(rings[0], rings.Skip(1)))]);

        /// <summary>Cosine series over a lattice of <c>Nx</c> by <c>Ny</c> unit cells, whose walls nothing crosses: the coefficients of
        /// the values at the cell centres, and a series of cosines or sines along each axis summed back at the cell centres. Each line is
        /// a transform of twice its length on the library's FFT, two real lines sharing one complex transform; the lengths are powers of
        /// two.</summary>
        /// <remarks>The values are held row by row: the cell in column <c>i</c> and row <c>j</c> at <c>j * Nx + i</c>, and the
        /// coefficient of <c>cos(pi kx x / Nx) cos(pi ky y / Ny)</c> at <c>ky * Nx + kx</c>.</remarks>
        internal sealed class CosineLattice
        {
            private readonly int _nx, _ny;
            private readonly Axis _alongX, _alongY;
            private readonly double[] _rowA, _rowB, _columnA, _columnB;

            /// <summary>What one axis needs for its lines: the half-cell shift and the buffer of a transform of twice its length.</summary>
            /// <param name="Length">The cells along the axis.</param>
            /// <param name="Twiddles">e^(-i pi k / 2N) for k below N.</param>
            /// <param name="Buffer">The buffer of length 2N.</param>
            private readonly record struct Axis(int Length, Complex[] Twiddles, Complex[] Buffer)
            {
                internal static Axis Of(int n)
                {
                    var twiddles = new Complex[n];
                    for (int k = 0; k < n; k++) twiddles[k] = Complex.FromPolarCoordinates(1, -Math.PI * k / (2 * n));
                    return new Axis(n, twiddles, new Complex[2 * n]);
                }
            }

            /// <summary>A lattice of <paramref name="nx"/> by <paramref name="ny"/> cells, with the tables its transforms read worked
            /// out once.</summary>
            /// <param name="nx">The number of columns, a power of two.</param>
            /// <param name="ny">The number of rows, a power of two.</param>
            internal CosineLattice(int nx, int ny)
            {
                _nx = nx;
                _ny = ny;
                _alongX = Axis.Of(nx);
                _alongY = Axis.Of(ny);
                (_rowA, _rowB, _columnA, _columnB) = (new double[nx], new double[nx], new double[ny], new double[ny]);
            }

            /// <summary>Writes over the values of <paramref name="cells"/> at the cell centres the coefficients of the cosine series that
            /// takes them: <c>c(kx, ky) = w(kx) w(ky) / (Nx Ny) * sum cells(i, j) cos(pi kx (i + 1/2) / Nx) cos(pi ky (j + 1/2) / Ny)</c>,
            /// with <c>w(0) = 1</c> and <c>w(k) = 2</c> above it. The first is the mean of the cells.</summary>
            /// <param name="cells">The values at the cell centres, row after row; replaced by the coefficients.</param>
            internal void ToCoefficients(double[] cells)
            {
                Rows(cells, (a, b) => Forward(a, b, _alongX));
                Columns(cells, (a, b) => Forward(a, b, _alongY));
                for (int ky = 0; ky < _ny; ky++)
                for (int kx = 0; kx < _nx; kx++)
                    cells[ky * _nx + kx] *= (kx == 0 ? 1.0 : 2.0) * (ky == 0 ? 1.0 : 2.0) / ((double)_nx * _ny);
            }

            /// <summary>Writes over <paramref name="coefficients"/> their series summed at the cell centres, with a cosine along an axis
            /// or, where asked, a sine: <c>sum c(kx, ky) T(pi kx (i + 1/2) / Nx) T(pi ky (j + 1/2) / Ny)</c>.</summary>
            /// <param name="coefficients">The coefficients, held as <see cref="GeoAxesExtensions.FlowCartogram.CosineLattice.ToCoefficients"/> writes them.</param>
            /// <param name="sineAlongX">A sine along X in place of the cosine.</param>
            /// <param name="sineAlongY">A sine along Y in place of the cosine.</param>
            internal void ToSums(double[] coefficients, bool sineAlongX, bool sineAlongY)
            {
                Rows(coefficients, (a, b) => Backward(a, b, _alongX, sineAlongX));
                Columns(coefficients, (a, b) => Backward(a, b, _alongY, sineAlongY));
            }

            /// <summary>Runs <paramref name="pair"/> over the rows of <paramref name="values"/>, two at a time, in place.</summary>
            private void Rows(double[] values, Action<double[], double[]> pair)
            {
                double[] a = _rowA, b = _rowB;
                for (int j = 0; j < _ny; j += 2)
                {
                    bool two = j + 1 < _ny;
                    Array.Copy(values, j * _nx, a, 0, _nx);
                    if (two) Array.Copy(values, (j + 1) * _nx, b, 0, _nx);
                    else Array.Clear(b);
                    pair(a, b);
                    Array.Copy(a, 0, values, j * _nx, _nx);
                    if (two) Array.Copy(b, 0, values, (j + 1) * _nx, _nx);
                }
            }

            /// <summary>Runs <paramref name="pair"/> over the columns of <paramref name="values"/>, two at a time, in place.</summary>
            private void Columns(double[] values, Action<double[], double[]> pair)
            {
                double[] a = _columnA, b = _columnB;
                for (int i = 0; i < _nx; i += 2)
                {
                    bool two = i + 1 < _nx;
                    for (int j = 0; j < _ny; j++)
                    {
                        a[j] = values[j * _nx + i];
                        b[j] = two ? values[j * _nx + i + 1] : 0;
                    }

                    pair(a, b);
                    for (int j = 0; j < _ny; j++)
                    {
                        values[j * _nx + i] = a[j];
                        if (two) values[j * _nx + i + 1] = b[j];
                    }
                }
            }

            /// <summary>The cosine transforms of two lines, written over them: <c>X(k) = sum x(m) cos(pi k (m + 1/2) / n)</c>. A line
            /// mirrored to length 2n and transformed gives <c>Y(k) = 2 e^(i pi k / 2n) X(k)</c>; the lines go in as the real and the
            /// imaginary part of one transform <c>Z</c>, and come out as <c>(Z(k) + conj Z(2n - k)) / 2</c> and
            /// <c>(Z(k) - conj Z(2n - k)) / 2i</c>.</summary>
            private static void Forward(double[] a, double[] b, Axis axis)
            {
                int n = axis.Length;
                var buffer = axis.Buffer;
                for (int m = 0; m < n; m++) buffer[m] = buffer[2 * n - 1 - m] = new Complex(a[m], b[m]);
                Fft.TransformInPlace(buffer);
                for (int k = 0; k < n; k++)
                {
                    Complex z = buffer[k], mirror = Complex.Conjugate(buffer[(2 * n - k) % (2 * n)]);
                    a[k] = 0.25 * (axis.Twiddles[k] * (z + mirror)).Real;
                    b[k] = 0.25 * (axis.Twiddles[k] * (z - mirror)).Imaginary;
                }
            }

            /// <summary>The series of two lines summed at the centres, written over them: <c>S(m) = sum c(k) e^(i pi k (m + 1/2) / n)</c>,
            /// whose real part is the cosine series and whose imaginary part the sine series. <c>S(m)</c> is the conjugate of
            /// <c>F(m)</c>, the transform of <c>c(k) e^(-i pi k / 2n)</c> padded with zeros to length 2n, and <c>F(2n - 1 - m)</c> is
            /// <c>S(m)</c> itself; the lines go in as the real and the imaginary part of one transform <c>P</c> and come out as
            /// <c>(P(m) + conj P(2n - 1 - m)) / 2</c> and <c>(P(m) - conj P(2n - 1 - m)) / 2i</c>.</summary>
            private static void Backward(double[] a, double[] b, Axis axis, bool sine)
            {
                int n = axis.Length;
                var buffer = axis.Buffer;
                for (int k = 0; k < n; k++) buffer[k] = new Complex(a[k], b[k]) * axis.Twiddles[k];
                Array.Clear(buffer, n, n);
                Fft.TransformInPlace(buffer);
                for (int m = 0; m < n; m++)
                {
                    Complex p = buffer[m], mirror = Complex.Conjugate(buffer[2 * n - 1 - m]);
                    Complex first = 0.5 * (p + mirror), second = new Complex(0, -0.5) * (p - mirror);
                    a[m] = sine ? -first.Imaginary : first.Real;
                    b[m] = sine ? -second.Imaginary : second.Real;
                }
            }
        }
    }
}
