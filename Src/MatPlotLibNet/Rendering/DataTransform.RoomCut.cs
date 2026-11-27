// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.SeriesRenderers;

namespace MatPlotLibNet.Rendering;

public sealed partial class DataTransform
{
    /// <summary>A path cut where it crosses the room between two parts of a broken axis (<see cref="Rendering.DataTransform.RoomCut.Cut"/>), over the rooms of each axis in
    /// screen order (<see cref="PixelDirection.PixelRooms"/>), with the lists a segment that crosses one is worked out in, made once a path.</summary>
    private sealed class RoomCut(AxisPair<PixelRoom[]> rooms)
    {
        private readonly AxisPair<List<PixelRoom>> _met = new([], []);
        private readonly List<RoomCrossing> _crossings = [];

        // The segment being cut and the place on it a point is moved back from: what the walks along each axis read of this cut.
        private PixelSegment _segment;
        private double _passedAt;

        /// <summary><paramref name="path"/> cut where it crosses the room between two parts of a broken axis, as matplotlib's broken-axis
        /// recipe draws each part as an axes of its own that clips the line to itself: a straight piece of the data runs in each part at the
        /// slope the data gives it there, from where it comes into the part to where it leaves it. Seen from a part, a vertex past a room stands
        /// further away by what the break removes less the room (<see cref="AxisBreakMap.Jumps"/>), so each piece is read off the line
        /// through the vertex on its side and the other vertex so moved. An open path (a line) leaves a gap (a NaN point) between the part it
        /// leaves and the part it comes back into; a closed one (the outline of a fill) runs straight through the room, which the figure's
        /// ground covers. A vertex the axes cannot place keeps its gap.</summary>
        /// <param name="path">The vertices in pixels.</param>
        /// <param name="closed">Whether the path closes back to its first vertex.</param>
        /// <param name="directions">The directions of the transform the path was placed by, whose rooms it is cut at.</param>
        /// <returns>The cut path; <paramref name="path"/> itself when no segment of it crosses a room.</returns>
        internal static IReadOnlyList<Point> Cut(IReadOnlyList<Point> path, bool closed, in AxisPair<PixelDirection> directions)
        {
            int n = path.Count;
            if (n < 2) return path;
            var rooms = new AxisPair<PixelRoom[]>(directions.X.PixelRooms(), directions.Y.PixelRooms());
            int segments = closed ? n : n - 1;

            // The segments that cross a room are counted first, so the cut path is made once at its size: each adds at most three points
            // for a room it crosses (where it enters, a gap, where it leaves), and most segments cross none.
            int crossing = 0;
            for (int i = 0; i < segments; i++)
                if (CrossesARoom(rooms, path[i], path[(i + 1) % n])) crossing++;
            if (crossing == 0) return path;

            var cut = new List<Point>(n + 3 * crossing * (rooms.X.Length + rooms.Y.Length));
            var scratch = new RoomCut(rooms);
            cut.Add(path[0]);
            for (int i = 0; i < segments; i++)
            {
                var a = path[i];
                var b = path[(i + 1) % n];
                if (CrossesARoom(rooms, a, b)) scratch.AddCrossings(cut, new PixelSegment(a, b), closed);
                if (i + 1 < n) cut.Add(b);
            }

            return cut;
        }

        /// <summary>Whether the segment from <paramref name="a"/> to <paramref name="b"/>, both placed, crosses a room along either axis.</summary>
        private static bool CrossesARoom(AxisPair<PixelRoom[]> rooms, Point a, Point b) =>
            a.IsDrawable() && b.IsDrawable()
            && rooms.Select(new PixelSegment(a, b), static (axisRooms, alongX, s) =>
                CrossesAny(axisRooms, s.From.CoordinateAlong(alongX), s.To.CoordinateAlong(alongX))).Any;

        /// <summary>Whether a segment from <paramref name="from"/> to <paramref name="to"/> along one axis crosses one of its rooms.</summary>
        private static bool CrossesAny(PixelRoom[] rooms, double from, double to)
        {
            foreach (var room in rooms)
                if (room.IsCrossedBy(from, to)) return true;
            return false;
        }

        /// <summary>Adds the cut of <paramref name="segment"/> at every room it crosses: for each, the point where it enters the room and
        /// the point where it leaves it, with a gap between them on an open path.</summary>
        private void AddCrossings(List<Point> cut, PixelSegment segment, bool closed)
        {
            _segment = segment;
            var crossed = rooms.Select(this, static (axisRooms, alongX, cut) =>
                Crossed(axisRooms, cut._segment.From.CoordinateAlong(alongX), cut._segment.To.CoordinateAlong(alongX), cut._met.Along(alongX)));

            // Seen from the first end's part, the second end stands past every room between them by their jumps, along each axis.
            var seenAlong = crossed.Select(segment, static (c, alongX, s) => s.To.CoordinateAlong(alongX) + c.Sign * c.Jumps);
            var seen = new PixelSegment(segment.From, new Point(seenAlong.X, seenAlong.Y));
            _crossings.Clear();
            crossed.Select(new CrossingsInto(seen, _crossings), static (c, alongX, into) => AddCrossingsOf(c, into, alongX));
            _crossings.Sort(static (p, q) => p.Enter.CompareTo(q.Enter));

            // Rooms of the two axes the segment is in at once (it crosses where they meet) hide one stretch: from the first entry to the
            // last exit.
            int k = 0;
            while (k < _crossings.Count)
            {
                var enter = _crossings[k];
                var leave = enter;
                for (k++; k < _crossings.Count && _crossings[k].Enter <= leave.Leave; k++)
                    if (_crossings[k].Leave > leave.Leave) leave = _crossings[k];

                cut.Add(PointAt(crossed, seen, enter.Enter, enter.AlongX, enter.EnterAt));
                if (!closed) cut.Add(new Point(double.NaN, double.NaN));
                cut.Add(PointAt(crossed, seen, leave.Leave, leave.AlongX, leave.LeaveAt));
            }

        }

        /// <summary>The point at <paramref name="t"/> on the line from the first end to the moved second, moved back past every room it has
        /// gone past, its coordinate along the axis of the room it stands on set exactly to that room's edge.</summary>
        private Point PointAt(AxisPair<CrossedRooms> crossed, PixelSegment seen, double t, bool alongX, double edge)
        {
            _passedAt = t;
            var back = crossed.Select(this, static (c, along, cut) => c.Sign * c.JumpsLeftBefore(cut._passedAt, cut._crossings, along));
            var onLine = new Point(seen.From.X + t * (seen.To.X - seen.From.X) - back.X, seen.From.Y + t * (seen.To.Y - seen.From.Y) - back.Y);
            return onLine.WithCoordinateAlong(alongX, edge);
        }

        /// <summary>The rooms of one axis a segment crosses from <paramref name="from"/> to <paramref name="to"/> along it, gathered in
        /// <paramref name="met"/> in the order it meets them, with the way it runs and their jumps summed.</summary>
        private static CrossedRooms Crossed(PixelRoom[] rooms, double from, double to, List<PixelRoom> met)
        {
            met.Clear();
            foreach (var room in rooms)
                if (room.IsCrossedBy(from, to)) met.Add(room);
            if (to < from) met.Reverse();
            double jumps = 0;
            foreach (var room in met) jumps += room.Jump;
            return new CrossedRooms(met, Math.Sign(to - from), jumps);
        }

        /// <summary>Adds each room of <paramref name="crossed"/> as the stretch of the segment's parameter it hides: the room's near edge and far
        /// edge seen from the first end (each past the jumps of the rooms met before it, the far one past its own too), over the way from the
        /// first end to the moved second.</summary>
        /// <returns>How many it added.</returns>
        private static int AddCrossingsOf(CrossedRooms crossed, CrossingsInto into, bool alongX)
        {
            double from = into.Seen.From.CoordinateAlong(alongX), before = 0, span = into.Seen.To.CoordinateAlong(alongX) - from;
            foreach (var room in crossed.Rooms)
            {
                double near = crossed.Sign > 0 ? room.Lo : room.Hi, far = crossed.Sign > 0 ? room.Hi : room.Lo;
                into.Crossings.Add(new RoomCrossing(
                    (near + crossed.Sign * before - from) / span,
                    (far + crossed.Sign * (before + room.Jump) - from) / span,
                    alongX, near, far));
                before += room.Jump;
            }

            return crossed.Rooms.Count;
        }

        /// <summary>A stretch of a segment, in its own parameter from 0 at its first end to 1 at its second, that lies in a room, with the
        /// place along the room's axis it enters and leaves it at and how far each end has gone past the rooms of that axis it crossed.</summary>
        private readonly record struct RoomCrossing(double Enter, double Leave, bool AlongX, double EnterAt, double LeaveAt);

        /// <summary>A segment seen from its first end's part and the list its crossings are gathered in.</summary>
        private readonly record struct CrossingsInto(PixelSegment Seen, List<RoomCrossing> Crossings);

        /// <summary>The rooms of one axis a segment crosses, in the order it meets them, the way it runs along that axis (1 or -1) and their
        /// jumps summed.</summary>
        private readonly record struct CrossedRooms(List<PixelRoom> Rooms, int Sign, double Jumps)
        {
            /// <summary>The jumps of the rooms of this axis the segment has gone past at <paramref name="t"/>: those it leaves at or before it.</summary>
            internal double JumpsLeftBefore(double t, List<RoomCrossing> crossings, bool alongX)
            {
                double jumps = 0;
                int k = 0;
                foreach (var crossing in crossings)
                {
                    if (crossing.AlongX != alongX) continue;
                    if (crossing.Leave <= t) jumps += Rooms[k].Jump;
                    k++;
                }

                return jumps;
            }
        }
    }
}
