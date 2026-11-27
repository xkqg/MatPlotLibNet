// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.GeoJson;
using MatPlotLibNet.Geo.Projections;
using MatPlotLibNet.Rendering;
using System.Buffers;

namespace MatPlotLibNet.Geo.Series;

/// <summary>Where the rings of a map land in the plot: each position goes through the projection (or is taken as already
/// projected) and then through the transform of the axes, the rule that places the marks of every other series, so a mark placed on a
/// map stands on the place it marks and a zoom or a pan moves the two together: the projection's bounds fill the plot from edge to
/// edge, north up, until the limits are set. A position the projection cannot place
/// (NaN) is left out of the ring; on a projection that shows one side of the globe, a ring that crosses its horizon is cut there
/// (<see cref="Geo.Series.GeoPixelMapping.HorizonClip"/>).</summary>
/// <param name="Projection">The map projection.</param>
/// <param name="RawProjected">Whether the positions are already projected (X, Y) values, as for a background that spans the
/// bounds.</param>
/// <param name="Transform">The transform of the axes: the projection's bounds onto the plot, or the limits the axes was given.</param>
/// <param name="Horizon">The cut at the horizon of a projection that has one, for positions that are not yet projected; otherwise
/// <see langword="null"/>.</param>
internal readonly record struct GeoPixelMapping(IGeoProjection Projection, bool RawProjected, DataTransform Transform, GeoPixelMapping.HorizonClip? Horizon)
{
    /// <summary>The mapping of positions onto the plot.</summary>
    /// <param name="projection">The map projection.</param>
    /// <param name="rawProjected">Whether the positions are already projected.</param>
    /// <param name="transform">The transform of the axes.</param>
    /// <returns>The mapping.</returns>
    internal static GeoPixelMapping For(IGeoProjection projection, bool rawProjected, DataTransform transform)
    {
        var horizon = rawProjected || projection is not IProjectionHorizon ? null : HorizonClip.Of(projection, point => transform.DataToPixel(point.X, point.Y));
        return new(projection, rawProjected, transform, horizon);
    }

    /// <summary>A ring of positions as pixels, a position the projection cannot place left out.</summary>
    /// <param name="ring">The positions of the ring.</param>
    /// <returns>The pixels of the positions the projection places.</returns>
    internal List<Point> Ring(IReadOnlyList<GeoPosition> ring)
    {
        var points = new List<Point>(ring.Count);
        foreach (var position in ring) Add(points, position.Lon, position.Lat);
        return points;
    }

    /// <summary>The parts of a ring that the map shows, as pixels: the ring as <see cref="Ring"/> has it, unless the projection has a
    /// horizon, when what is on its far side is left out and each part on the near side is a piece of its own.</summary>
    /// <param name="ring">The positions of the ring.</param>
    /// <param name="closed">Whether the ring is a polygon's, which is closed along the horizon where it is cut; a line is not.</param>
    /// <returns>The pieces of the ring that the map shows.</returns>
    internal RingPieces Pieces(IReadOnlyList<GeoPosition> ring, bool closed) =>
        Horizon is null ? new RingPieces(Ring(ring)) : closed ? Horizon.Ring(ring) : Horizon.Line(ring);

    private void Add(List<Point> points, double lon, double lat)
    {
        var projected = RawProjected ? new ProjectedPoint(lon, lat) : Projection.Forward(lat, lon);
        if (projected.IsOffDomain) return;

        points.Add(Transform.DataToPixel(projected.X, projected.Y));
    }

    /// <summary>Cuts the rings and the lines of a map at the horizon of a projection that shows one side of the globe
    /// (<see cref="IProjectionHorizon"/>), as cartopy cuts them: the part on the far side is left out, a line ends on the edge of the map
    /// where it leaves, and a polygon is closed along that edge from where it leaves to where it comes back, so a country that stands
    /// across the horizon is drawn up to it and not bridged by a straight line. An edge is followed in longitude and latitude, the shorter
    /// way round the globe, and is cut where its two ends stand on different sides; the point of the cut lies on the outline of the map
    /// (the polygon cartopy cuts against), so the polygon cut meets the ocean it lies on.</summary>
    /// <remarks>The places are given in the order the ring runs, and a ring's winding is the one <see cref="GeoPolygon.FromRings"/> gives
    /// (an exterior counter-clockwise, a hole clockwise, the area on the left of the edge): where the ring leaves, the next part of the
    /// polygon is the one that comes back first going counter-clockwise along the horizon. A place with no number is left out, as the map
    /// leaves it out where there is no horizon. One instance draws one frame and is not shared between threads.</remarks>
    internal sealed class HorizonClip
    {
        /// <summary>The halvings that find where an edge reaches the horizon: the place is then within a millionth of the edge's length of
        /// it, which is a millionth of a pixel for an edge of a degree on a globe a thousand pixels across.</summary>
        private const int Halvings = 20;

        /// <summary>How close two keys must stand to be the same point on the outline: a billionth of an edge.</summary>
        private const double SameKey = 1e-9;

        private readonly IGeoProjection _projection;
        private readonly Func<ProjectedPoint, Point> _place;
        private readonly ProjectedPoint[] _limb;
        private readonly ProjectedPoint _centre;
        private readonly double _innerSquared, _outerSquared;
        private Vertex[] _vertices = [];
        private int _count;

        /// <summary>A place of the ring being cut and where the projection puts it.</summary>
        /// <param name="Position">The place.</param>
        /// <param name="Point">Where the projection puts it; (NaN, NaN) on the far side.</param>
        /// <param name="Shown">Whether the place is on the near side and inside the outline of the map.</param>
        private readonly record struct Vertex(GeoPosition Position, ProjectedPoint Point, bool Shown);

        /// <summary>A point on the outline, and how far round it stands: the index of the outline's edge it is on and how far along that
        /// edge it is, so that the points of the outline are in order by their keys.</summary>
        private readonly record struct LimbPoint(ProjectedPoint At, double Key);

        /// <summary>The two ends of an edge of the outline, as vectors from the centre of the map.</summary>
        private readonly record struct Wedge(double StartX, double StartY, double EndX, double EndY);

        /// <summary>A stretch of a ring that is shown: from where it enters to where it leaves.</summary>
        private sealed class Run
        {
            internal List<Point> Points { get; } = [];

            internal double EntryKey { get; set; }

            internal double ExitKey { get; set; }
        }

        private HorizonClip(IGeoProjection projection, Func<ProjectedPoint, Point> place)
        {
            _projection = projection;
            _place = place;
            var outline = projection.Outline();
            int count = outline.Count > 1 && outline[0] == outline[^1] ? outline.Count - 1 : outline.Count;
            var limb = new ProjectedPoint[count];
            double twiceArea = 0;
            for (int i = 0; i < count; i++)
            {
                limb[i] = new ProjectedPoint(outline[i].Lon, outline[i].Lat);
                var next = outline[(i + 1) % count];
                twiceArea += outline[i].Lon * next.Lat - next.Lon * outline[i].Lat;
            }

            // The walk along the horizon runs counter-clockwise.
            if (twiceArea < 0) Array.Reverse(limb);
            _limb = limb;
            var bounds = projection.Bounds;
            _centre = new ProjectedPoint((bounds.XMin + bounds.XMax) / 2, (bounds.YMin + bounds.YMax) / 2);

            // A place nearer the centre than every edge of the outline is inside it, one further than every corner is outside it; only
            // those between need the edge they stand against.
            _innerSquared = double.MaxValue;
            for (int j = 0; j < count; j++)
            {
                var wedge = WedgeOf(j);
                double edgeX = wedge.EndX - wedge.StartX, edgeY = wedge.EndY - wedge.StartY;
                double across = wedge.StartX * edgeY - wedge.StartY * edgeX;
                _innerSquared = Math.Min(_innerSquared, across * across / new Point(edgeX, edgeY).SquaredLength);
                _outerSquared = Math.Max(_outerSquared, new Point(wedge.StartX, wedge.StartY).SquaredLength);
            }
        }

        /// <summary>The clip of <paramref name="projection"/>.</summary>
        /// <param name="projection">The projection.</param>
        /// <param name="place">Where a point of the plane of the projection lands in what the pieces are given in (the pixels of the
        /// plot): applied to every point of every piece, the cuts and the corners of the outline too.</param>
        /// <returns>The clip; <see langword="null"/> for a projection that shows the whole globe or has no horizon.</returns>
        internal static HorizonClip? Of(IGeoProjection projection, Func<ProjectedPoint, Point> place) =>
            projection is IProjectionHorizon ? new HorizonClip(projection, place) : null;

        /// <summary>The parts of a closed ring that the projection shows.</summary>
        /// <param name="ring">The positions of the ring in order, wound as a polygon's ring is; its first position may be repeated at
        /// the end.</param>
        /// <returns>A ring on the near side as it is; one on the far side none; one across the horizon a polygon for each part that is
        /// shown, closed along the horizon, counter-clockwise for an exterior.</returns>
        internal RingPieces Ring(IReadOnlyList<GeoPosition> ring) => Cut(ring, closed: true);

        /// <summary>The parts of a line that the projection shows.</summary>
        /// <param name="line">The positions of the line in order.</param>
        /// <returns>A line on the near side as it is; one on the far side none; one that crosses the horizon a line for each part that is
        /// shown, each ending on the outline where it leaves the near side or starting on it where it comes back.</returns>
        internal RingPieces Line(IReadOnlyList<GeoPosition> line) => Cut(line, closed: false);

        /// <summary>The parts of <paramref name="places"/> that the projection shows. The places are held in a buffer of the pool for the
        /// length of the call, so that a ring of any length costs no array of its own.</summary>
        private RingPieces Cut(IReadOnlyList<GeoPosition> places, bool closed)
        {
            _vertices = ArrayPool<Vertex>.Shared.Rent(places.Count);
            try
            {
                int shown = Load(places);
                if (shown == 0) return default;
                if (shown == _count) return new RingPieces(AllPoints());

                if (!closed)
                {
                    var runs = Runs(_count, closed: false);
                    var pieces = new List<List<Point>>(runs.Count);
                    foreach (var run in runs) pieces.Add(run.Points);
                    return new RingPieces(pieces);
                }

                // A ring repeats its first position at the end; the walk goes round the places once.
                int count = _vertices[0].Position == _vertices[_count - 1].Position ? _count - 1 : _count;
                return Linked(Runs(count, closed: true));
            }
            finally
            {
                ArrayPool<Vertex>.Shared.Return(_vertices);
                _vertices = [];
            }
        }

        /// <summary>Places <paramref name="positions"/> in the plane, leaving out a place with no number.</summary>
        /// <returns>How many of them are on the near side.</returns>
        private int Load(IReadOnlyList<GeoPosition> positions)
        {
            _count = 0;
            int shown = 0;
            for (int i = 0; i < positions.Count; i++)
            {
                var position = positions[i];
                if (double.IsNaN(position.Lon) || double.IsNaN(position.Lat)) continue;
                var point = _projection.Forward(position.Lat, position.Lon);
                bool isShown = !point.IsOffDomain && Inside(point);
                if (isShown) shown++;
                _vertices[_count++] = new Vertex(position, point, isShown);
            }

            return shown;
        }

        /// <summary>Whether a place the projection placed is inside the outline of the map: a place between the horizon's circle and the
        /// polygon the map draws as its outline is not, as cartopy leaves a place outside its boundary out.</summary>
        private bool Inside(ProjectedPoint point)
        {
            double square = _centre.StepTo(point).SquaredLength;
            if (square <= _innerSquared) return true;
            if (square > _outerSquared) return false;

            return square <= _centre.StepTo(OnTheOutline(point).At).SquaredLength;
        }

        private List<Point> AllPoints()
        {
            var points = new List<Point>(_count);
            for (int i = 0; i < _count; i++) points.Add(_place(_vertices[i].Point));
            return points;
        }

        /// <summary>The stretches of the loaded places that are shown, each from where it enters the near side to where it leaves.</summary>
        /// <param name="count">How many of the loaded places are walked.</param>
        /// <param name="closed">Whether the places make a ring, the last joined to the first: the walk then starts on the far side, so
        /// every stretch has an entry and an exit; otherwise a line, whose first and last places may be shown with no cut.</param>
        private List<Run> Runs(int count, bool closed)
        {
            var runs = new List<Run>();
            Run? run = null;
            int start = 0, steps = count - 1;
            if (closed)
            {
                while (_vertices[start].Shown) start++;
                steps = count;
            }
            else if (_vertices[0].Shown)
            {
                run = new Run();
                run.Points.Add(_place(_vertices[0].Point));
            }

            for (int step = 1; step <= steps; step++)
            {
                var from = _vertices[(start + step - 1) % count];
                var to = _vertices[(start + step) % count];
                if (to.Shown)
                {
                    if (!from.Shown)
                    {
                        var entry = OnTheOutline(Crossing(near: to, far: from));
                        run = new Run { EntryKey = entry.Key };
                        run.Points.Add(_place(entry.At));
                    }

                    run!.Points.Add(_place(to.Point));
                }
                else if (from.Shown)
                {
                    var exit = OnTheOutline(Crossing(near: from, far: to));
                    run!.ExitKey = exit.Key;
                    run.Points.Add(_place(exit.At));
                    runs.Add(run);
                    run = null;
                }
            }

            // A line that ends on a shown place has a stretch still open; a ring never does.
            if (run is not null) runs.Add(run);
            return runs;
        }

        /// <summary>Where the edge from <paramref name="near"/> to <paramref name="far"/> reaches the horizon, in the plane: the last
        /// place along it, the shorter way round in longitude, that the projection still places. When the projection places the far end
        /// too (it stands between the horizon's circle and the outline) the edge never reaches the horizon and the far end is where
        /// it is cut.</summary>
        private ProjectedPoint Crossing(Vertex near, Vertex far)
        {
            if (!far.Point.IsOffDomain) return far.Point;

            double lon = ((far.Position.Lon - near.Position.Lon + 540) % 360) - 180, lat = far.Position.Lat - near.Position.Lat;
            double low = 0, high = 1;
            var found = near.Point;
            for (int i = 0; i < Halvings; i++)
            {
                double middle = (low + high) / 2;
                var at = _projection.Forward(near.Position.Lat + middle * lat, near.Position.Lon + middle * lon);
                if (at.IsOffDomain)
                {
                    high = middle;
                }
                else
                {
                    low = middle;
                    found = at;
                }
            }

            return found;
        }

        /// <summary>The point of the outline that a point on the horizon is on: where the line from the centre of the map through it
        /// meets the outline, which is a polygon a little inside the horizon's circle.</summary>
        private LimbPoint OnTheOutline(ProjectedPoint point)
        {
            double dx = point.X - _centre.X, dy = point.Y - _centre.Y;

            // The edge whose wedge from the centre holds the direction: the one where the direction is furthest from both its sides.
            int edge = 0;
            double best = double.NegativeInfinity;
            for (int j = 0; j < _limb.Length; j++)
            {
                var wedge = WedgeOf(j);
                double beyondStart = wedge.StartX * dy - wedge.StartY * dx, beforeEnd = dx * wedge.EndY - dy * wedge.EndX;
                double inside = Math.Min(beyondStart, beforeEnd);
                if (inside > best)
                {
                    best = inside;
                    edge = j;
                }
            }

            var chosen = WedgeOf(edge);
            double edgeX = chosen.EndX - chosen.StartX, edgeY = chosen.EndY - chosen.StartY;
            double across = dx * edgeY - dy * edgeX;
            double reach = (chosen.StartX * edgeY - chosen.StartY * edgeX) / across;
            double along = (chosen.StartX * dy - chosen.StartY * dx) / across;
            return new LimbPoint(new ProjectedPoint(_centre.X + reach * dx, _centre.Y + reach * dy), edge + along);
        }

        /// <summary>The two ends of edge <paramref name="j"/> of the outline, as vectors from the centre.</summary>
        private Wedge WedgeOf(int j)
        {
            var start = _limb[j];
            var end = _limb[(j + 1) % _limb.Length];
            return new Wedge(start.X - _centre.X, start.Y - _centre.Y, end.X - _centre.X, end.Y - _centre.Y);
        }

        /// <summary>The stretches closed into polygons: after each one leaves, the outline is followed counter-clockwise to the first
        /// stretch that enters, and so on round until the polygon is back where it began.</summary>
        private RingPieces Linked(List<Run> runs)
        {
            var polygons = new List<List<Point>>();
            var used = new bool[runs.Count];
            for (int first = 0; first < runs.Count; first++)
            {
                if (used[first]) continue;

                // The first stretch of a polygon is the polygon: the walk along the outline and the stretches that follow are added to it.
                used[first] = true;
                var polygon = runs[first].Points;
                int current = first;
                while (true)
                {
                    int next = FirstEntryAfter(runs, runs[current].ExitKey);
                    FollowTheOutline(polygon, runs[current].ExitKey, runs[next].EntryKey);
                    if (next == first || used[next]) break;

                    used[next] = true;
                    polygon.AddRange(runs[next].Points);
                    current = next;
                }

                polygons.Add(polygon);
            }

            return new RingPieces(polygons);
        }

        /// <summary>The stretch whose entry is the first met going counter-clockwise from <paramref name="exitKey"/>.</summary>
        private int FirstEntryAfter(List<Run> runs, double exitKey)
        {
            int first = 0;
            double nearest = double.MaxValue;
            for (int i = 0; i < runs.Count; i++)
            {
                double distance = Around(runs[i].EntryKey - exitKey);
                if (distance < nearest)
                {
                    nearest = distance;
                    first = i;
                }
            }

            return first;
        }

        /// <summary>Adds the corners of the outline that lie counter-clockwise between two points of it.</summary>
        private void FollowTheOutline(List<Point> polygon, double fromKey, double toKey)
        {
            double distance = Around(toKey - fromKey);
            for (double corner = Math.Floor(fromKey) + 1; corner < fromKey + distance; corner++)
                polygon.Add(_place(_limb[(int)corner % _limb.Length]));
        }

        /// <summary>A distance in keys, counter-clockwise: from 0 up to the number of edges. Two keys that are the same point, to the
        /// rounding of the arithmetic that placed them (a ring that dips between the horizon's circle and the outline leaves and
        /// returns at one point), are a distance of nothing, not a lap of the globe.</summary>
        private double Around(double distance)
        {
            double wrapped = distance % _limb.Length;
            if (wrapped < 0) wrapped += _limb.Length;
            return wrapped > _limb.Length - SameKey ? 0 : wrapped;
        }
    }
}
