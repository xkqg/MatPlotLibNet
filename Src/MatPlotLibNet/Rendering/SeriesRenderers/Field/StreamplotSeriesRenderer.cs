// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders the streamlines of a field as matplotlib's <c>streamplot</c> lays them out (<see cref="StreamlinesOf"/>): every
/// streamline, then the arrow halfway along each, inside one clip of the plot area. The arrow is matplotlib's filled head under a theme that
/// draws it (<see cref="VectorFieldLook.StreamArrowsLikeMatplotlib"/>), an open head of two strokes otherwise.</summary>
internal sealed class StreamplotSeriesRenderer : SeriesRenderer<StreamplotSeries>
{
    /// <inheritdoc />
    public StreamplotSeriesRenderer(SeriesRenderContext context) : base(context) { }

    // FancyArrowPatch(arrowstyle='-|>', mutation_scale=10 * arrowsize): ArrowStyle.CurveFilledB's head_length 0.4 and head_width 0.2 of
    // the scale, and shrinkA = shrinkB = 2 points.
    private const double ScalePoints = 10, HeadLengthShare = 0.4, HeadWidthShare = 0.2, ShrinkPoints = 2;

    /// <inheritdoc />
    public override void Render(StreamplotSeries series)
    {
        var lines = StreamlinesOf(series);
        if (lines.Count == 0) return;

        var color = ColorOf(series);
        var theme = Context.Theme;
        double width = Slot.LineWidthOf(series);
        var stroke = StrokeStyle.Solid(color, width);

        foreach (var line in lines) DrawPolyline(Transform.TransformPath(line.X, line.Y), stroke);
        foreach (var line in lines)
        {
            var tail = Transform.DataToPixel(line.ArrowTail.X, line.ArrowTail.Y);
            var head = Transform.DataToPixel(line.ArrowHead.X, line.ArrowHead.Y);
            if (!tail.CanDrawLineTo(head)) continue;
            var heading = tail.HeadingTo(head);
            double length = heading.Length;
            if (length == 0) continue;
            double ux = heading.Ux, uy = heading.Uy;
            if (theme.Looks.VectorField.StreamArrowsLikeMatplotlib) DrawFilledHead(tail, length, ux, uy, series.ArrowSize, ShapeStyle.Edged(color, color, width));
            else ArrowHead.OpenHead.Draw(Ctx, tail, head, ArrowHead.OpenHeadLength * series.ArrowSize, stroke);
        }
    }

    /// <summary>matplotlib's <c>-|></c> head on the arrow from <paramref name="tail"/>, <paramref name="length"/> pixels along (<paramref name="ux"/>, <paramref name="uy"/>): the path is pulled in 2 pt at
    /// each end where it is long enough to be cut there (<c>ConnectionStyle._clip</c>), and the tip pulled back by half the edge's width over
    /// the sine of the head's half angle (<c>_get_arrow_wedge</c>'s <c>pad_projected</c>), so the mitred edge ends where the path does.</summary>
    private void DrawFilledHead(Point tail, double length, double ux, double uy, double arrowSize, ShapeStyle style)
    {
        double scale = (ScalePoints * arrowSize).PointsInPixels;
        double headLength = HeadLengthShare * scale, headWidth = HeadWidthShare * scale;
        double sine = headWidth / new Point(headLength, headWidth).Length;
        double shrink = ShrinkPoints.PointsInPixels;

        // The start is cut where the path leaves the circle round the tail; the end where it enters the circle round the head, which it
        // can only do when what is left of it starts outside that circle.
        double fromTail = length >= shrink ? shrink : 0;
        double end = length - fromTail >= shrink ? length - shrink : length;
        double back = end - (style.StrokeThickness.Reach / sine);

        var tip = new Point(tail.X + (ux * back), tail.Y + (uy * back));
        var baseCentre = new Point(tip.X - (ux * headLength), tip.Y - (uy * headLength));
        Ctx.DrawPolygon(
            [new(baseCentre.X - (uy * headWidth), baseCentre.Y + (ux * headWidth)), tip, new(baseCentre.X + (uy * headWidth), baseCentre.Y - (ux * headWidth))],
            style);
    }

    /// <summary>One streamline of a streamplot, in data units: its points in order, and the two points its arrow is drawn between (the
    /// arrow points from <paramref name="ArrowTail"/> to <paramref name="ArrowHead"/>).</summary>
    /// <param name="X">The X of each point.</param>
    /// <param name="Y">The Y of each point.</param>
    /// <param name="ArrowTail">The point of the streamline halfway along it by its length.</param>
    /// <param name="ArrowHead">The middle of the step after <paramref name="ArrowTail"/>.</param>
    internal readonly record struct Streamline(double[] X, double[] Y, Point ArrowTail, Point ArrowHead);

    private const int CellsPerDensity = 30;
    private const double MinLength = 0.1;
    // maxlength = 4.0, halved for integration_direction='both'.
    private const double MaxLengthEachWay = 2.0;
    private const double MaxError = 0.003;

    // The layout of one Render: the grid and the mask, the field in grid coordinates, and the trajectory being traced.
    private int _nx, _ny, _mx, _my;
    private double[] _u = [], _v = [], _speed = [];
    private bool[] _mask = [];
    private double _gridToMaskX, _gridToMaskY;
    private double _maxDs;
    private readonly List<int> _claimed = [];
    private int _currentX = -1, _currentY = -1;

    /// <summary>Starts the layout of <paramref name="series"/> on a mask of <paramref name="mx"/> by <paramref name="my"/> cells, its
    /// grid stepped by <paramref name="dx"/> and <paramref name="dy"/>: the mask empty, the field in grid coordinates and its speed.</summary>
    private void StartLayout(StreamplotSeries series, int mx, int my, double dx, double dy)
    {
        _claimed.Clear();
        _currentX = _currentY = -1;
        _nx = series.X.Length;
        _ny = series.Y.Length;
        _mx = mx;
        _my = my;
        _mask = new bool[mx * my];
        _gridToMaskX = (double)(mx - 1) / (_nx - 1);
        _gridToMaskY = (double)(my - 1) / (_ny - 1);
        _maxDs = Math.Min(Math.Min(1.0 / mx, 1.0 / my), 0.1);

        // The field in grid coordinates (data2grid), and its speed in axes coordinates; a value that is not a number is masked (NaN).
        double xDataToGrid = 1.0 / dx, yDataToGrid = 1.0 / dy;
        _u = new double[_nx * _ny];
        _v = new double[_nx * _ny];
        _speed = new double[_nx * _ny];
        for (int row = 0; row < _ny; row++)
        {
            for (int column = 0; column < _nx; column++)
            {
                int k = (row * _nx) + column;
                double u = series.U[row, column], v = series.V[row, column];
                if (!double.IsFinite(u) || !double.IsFinite(v))
                {
                    _u[k] = _v[k] = _speed[k] = double.NaN;
                    continue;
                }
                _u[k] = u * xDataToGrid;
                _v[k] = v * yDataToGrid;
                double uAxes = _u[k] / (_nx - 1), vAxes = _v[k] / (_ny - 1);
                _speed[k] = new Point(uAxes, vAxes).Length;
            }
        }
    }

    /// <summary>The streamlines of <paramref name="series"/>, in the order matplotlib draws them; none when its grid has fewer than two points a
    /// side, its field does not match its grid, its first step is not a number or is zero, or its mask has fewer than two cells a side
    /// (where matplotlib's spacing divides by zero).</summary>
    /// <param name="series">The streamplot.</param>
    /// <returns>The streamlines.</returns>
    /// <remarks>The streamlines of a field as matplotlib's <c>streamplot</c> lays them out (<c>matplotlib/streamplot.py</c>, 3.11.2, with its
    /// defaults: <c>minlength=0.1</c>, <c>maxlength=4.0</c>, <c>integration_direction='both'</c>, <c>broken_streamlines=True</c>, one arrow).
    /// A mask of 30 x 30 cells times the density (<c>StreamMask</c>) lets a streamline enter each cell once; seeds are tried in a spiral from
    /// the mask's corner inward (<c>_gen_starting_points</c>), and each is integrated backward and forward in grid coordinates with an
    /// adaptive second-order Runge-Kutta (<c>_integrate_rk12</c>) whose speed is measured in axes coordinates, up to 2 each way, ended at the
    /// edge of the grid by one Euler step, where it would enter a cell another streamline holds, or where the field stops or is missing; one
    /// shorter than 0.1 is dropped and its cells are freed. The grid is <c>X[0]</c> plus a whole number of <c>X[1] - X[0]</c> steps (and the
    /// same for Y), as matplotlib's evenly spaced grid is.</remarks>
    internal IReadOnlyList<Streamline> StreamlinesOf(StreamplotSeries series)
    {
        double[] x = series.X, y = series.Y;
        if (x.Length < 2 || y.Length < 2) return [];
        if (series.U.GetLength(0) != y.Length || series.U.GetLength(1) != x.Length
            || series.V.GetLength(0) != y.Length || series.V.GetLength(1) != x.Length) return [];
        double dx = x[1] - x[0], dy = y[1] - y[0];
        if (!double.IsFinite(x[0]) || !double.IsFinite(y[0]) || !double.IsFinite(dx) || !double.IsFinite(dy) || dx == 0 || dy == 0) return [];
        // (30 * density).astype(int): truncated toward zero.
        double cells = CellsPerDensity * series.Density;
        if (!(cells >= 2)) return [];
        int m = (int)Math.Min(cells, int.MaxValue);

        StartLayout(series, m, m, dx, dy);
        return Trace(x[0], y[0], dx, dy);
    }

    private List<Streamline> Trace(double xOrigin, double yOrigin, double dx, double dy)
    {
        var lines = new List<Streamline>();
        double maskToGridX = 1.0 / _gridToMaskX, maskToGridY = 1.0 / _gridToMaskY;
        double xDataToGrid = 1.0 / dx, yDataToGrid = 1.0 / dy;
        var xs = new List<double>();
        var ys = new List<double>();
        foreach (var (xm, ym) in SpiralFromTheCorner())
        {
            if (_mask[(ym * _mx) + xm]) continue;
            xs.Clear();
            ys.Clear();
            if (!Integrate(xm * maskToGridX, ym * maskToGridY, xs, ys)) continue;

            // grid2data, then shifted to the grid's origin.
            var tx = new double[xs.Count];
            var ty = new double[ys.Count];
            for (int i = 0; i < tx.Length; i++)
            {
                tx[i] = (xs[i] / xDataToGrid) + xOrigin;
                ty[i] = (ys[i] / yDataToGrid) + yOrigin;
            }
            lines.Add(WithArrow(tx, ty));
        }
        return lines;
    }

    /// <summary>The streamline with its arrow halfway along it: <c>np.searchsorted(s, s[-1] / 2)</c> over the running length <c>s</c> (in data
    /// units) is the arrow's tail, and the middle of the next step its head.</summary>
    private static Streamline WithArrow(double[] tx, double[] ty)
    {
        int n = tx.Length;
        var running = new double[n - 1];
        double sum = 0;
        for (int i = 0; i < n - 1; i++)
        {
            sum += new Point(tx[i], ty[i]).DistanceTo(new Point(tx[i + 1], ty[i + 1]));
            running[i] = sum;
        }
        double half = running[^1] * 0.5;
        int idx = 0;
        while (idx < running.Length - 1 && running[idx] < half) idx++;
        return new Streamline(tx, ty, new Point(tx[idx], ty[idx]), new Point((tx[idx] + tx[idx + 1]) / 2, (ty[idx] + ty[idx + 1]) / 2));
    }

    /// <summary>The mask's cells from its corner, spiralling inward (<c>_gen_starting_points</c>): along the bottom, up the right, back along
    /// the top, down the left, each turn one cell further in.</summary>
    private IEnumerable<(int X, int Y)> SpiralFromTheCorner()
    {
        int xFirst = 0, yFirst = 1, xLast = _mx - 1, yLast = _my - 1;
        int x = 0, y = 0, direction = 0;
        for (int i = 0; i < _mx * _my; i++)
        {
            yield return (x, y);
            switch (direction)
            {
                case 0:
                    x++;
                    if (x >= xLast) { xLast--; direction = 1; }
                    break;
                case 1:
                    y++;
                    if (y >= yLast) { yLast--; direction = 2; }
                    break;
                case 2:
                    x--;
                    if (x <= xFirst) { xFirst++; direction = 3; }
                    break;
                default:
                    y--;
                    if (y <= yFirst) { yFirst++; direction = 0; }
                    break;
            }
        }
    }

    /// <summary>One streamline from the seed (<c>integrate</c>): backward, then forward from the seed again, joined; <see langword="false"/>
    /// when it is shorter than <see cref="MinLength"/>, whose cells are then freed. The seed's cell is free: <see cref="Trace"/> skips a
    /// taken one, as matplotlib's loop does before it calls <c>integrate</c>.</summary>
    private bool Integrate(double x0, double y0, List<double> xs, List<double> ys)
    {
        _claimed.Clear();
        Enter(GridToMask(x0, _gridToMaskX), GridToMask(y0, _gridToMaskY));

        var backX = new List<double>();
        var backY = new List<double>();
        double total = Rk12(x0, y0, -1, backX, backY);
        for (int i = backX.Count - 1; i >= 0; i--)
        {
            xs.Add(backX[i]);
            ys.Add(backY[i]);
        }

        // reset_start_point: the seed's cell is the current one again.
        _currentX = GridToMask(x0, _gridToMaskX);
        _currentY = GridToMask(y0, _gridToMaskY);
        int forwardStart = xs.Count;
        total += Rk12(x0, y0, +1, xs, ys);
        // The forward part's first point is the seed, which the backward part already holds.
        if (xs.Count > forwardStart)
        {
            xs.RemoveAt(forwardStart);
            ys.RemoveAt(forwardStart);
        }

        if (total > MinLength) return true;
        foreach (int cell in _claimed) _mask[cell] = false;
        return false;
    }

    private static int GridToMask(double g, double gridToMask) => (int)Math.Round(g * gridToMask, MidpointRounding.ToEven);

    /// <summary>The streamline enters a mask cell (<c>StreamMask._update_trajectory</c>): it holds the cell it is in already, or takes a free
    /// one; <see langword="false"/> when the cell is another streamline's (or one it left).</summary>
    private bool Enter(int xm, int ym)
    {
        if (xm == _currentX && ym == _currentY) return true;
        int cell = (ym * _mx) + xm;
        if (_mask[cell]) return false;
        _claimed.Add(cell);
        _mask[cell] = true;
        _currentX = xm;
        _currentY = ym;
        return true;
    }

    private bool WithinGrid(double xi, double yi) => xi >= 0 && xi <= _nx - 1 && yi >= 0 && yi <= _ny - 1;

    /// <summary>What the field says at a point of the grid (<c>forward_time</c>, and <c>backward_time</c> going the other way).</summary>
    private enum Flow
    {
        /// <summary>The step along the field, per unit of length in axes coordinates.</summary>
        Along,

        /// <summary>The point is outside the grid.</summary>
        OutOfBounds,

        /// <summary>The field stops there or is missing (<c>TerminateTrajectory</c>).</summary>
        Stop,
    }

    private Flow Step(double xi, double yi, double sign, out double kx, out double ky)
    {
        kx = ky = 0;
        if (!WithinGrid(xi, yi)) return Flow.OutOfBounds;
        double dsdt = Interpolate(_speed, xi, yi);
        if (double.IsNaN(dsdt) || dsdt == 0) return Flow.Stop;
        double dtds = 1.0 / dsdt;
        kx = sign * (Interpolate(_u, xi, yi) * dtds);
        ky = sign * (Interpolate(_v, xi, yi) * dtds);
        return Flow.Along;
    }

    /// <summary>Linear interpolation on the integer grid (<c>interpgrid</c>); NaN where a corner is missing.</summary>
    private double Interpolate(double[] a, double xi, double yi)
    {
        int x = (int)xi, y = (int)yi;
        int xn = x == _nx - 1 ? x : x + 1;
        int yn = y == _ny - 1 ? y : y + 1;
        double xt = xi - x, yt = yi - y;
        double a0 = (a[(y * _nx) + x] * (1 - xt)) + (a[(y * _nx) + xn] * xt);
        double a1 = (a[(yn * _nx) + x] * (1 - xt)) + (a[(yn * _nx) + xn] * xt);
        return (a0 * (1 - yt)) + (a1 * yt);
    }

    /// <summary>One way of a streamline (<c>_integrate_rk12</c>): the points it passes, appended to <paramref name="xs"/> and
    /// <paramref name="ys"/> (a point is appended again where a step is refused for its error, as matplotlib's is), and how far it went.</summary>
    private double Rk12(double x0, double y0, double sign, List<double> xs, List<double> ys)
    {
        int start = xs.Count;
        double ds = _maxDs, total = 0, xi = x0, yi = y0;
        while (true)
        {
            Flow flow;
            double k1x = 0, k1y = 0, k2x = 0, k2y = 0;
            if (WithinGrid(xi, yi))
            {
                xs.Add(xi);
                ys.Add(yi);
                flow = Step(xi, yi, sign, out k1x, out k1y);
                if (flow == Flow.Along) flow = Step(xi + (ds * k1x), yi + (ds * k1y), sign, out k2x, out k2y);
            }
            else
            {
                flow = Flow.OutOfBounds;
            }

            if (flow == Flow.OutOfBounds)
            {
                // Out of the grid during this step: an Euler step to its edge, unless the way is still empty.
                if (xs.Count > start) total += EulerStepToTheEdge(sign, xs, ys);
                break;
            }
            if (flow == Flow.Stop) break;

            double dx1 = ds * k1x, dy1 = ds * k1y;
            double dx2 = ds * 0.5 * (k1x + k2x), dy2 = ds * 0.5 * (k1y + k2y);
            // The error in axes coordinates.
            double error = new Point((dx2 - dx1) / (_nx - 1), (dy2 - dy1) / (_ny - 1)).Length;
            if (error < MaxError)
            {
                xi += dx2;
                yi += dy2;
                if (!WithinGrid(xi, yi) || !Enter(GridToMask(xi, _gridToMaskX), GridToMask(yi, _gridToMaskY))) break;
                if (total + ds > MaxLengthEachWay) break;
                total += ds;
            }

            ds = error == 0 ? _maxDs : Math.Min(_maxDs, 0.85 * ds * Math.Pow(MaxError / error, 0.5));
        }
        return total;
    }

    /// <summary>The last point carried along the field to the edge of the grid it would cross first (<c>_euler_step</c>); how far it went.</summary>
    private double EulerStepToTheEdge(double sign, List<double> xs, List<double> ys)
    {
        double xi = xs[^1], yi = ys[^1];
        // The field was read at this point a moment ago, so it is along.
        Step(xi, yi, sign, out double cx, out double cy);
        double dsx = cx == 0 ? double.PositiveInfinity : cx < 0 ? xi / -cx : (_nx - 1 - xi) / cx;
        double dsy = cy == 0 ? double.PositiveInfinity : cy < 0 ? yi / -cy : (_ny - 1 - yi) / cy;
        double ds = Math.Min(dsx, dsy);
        xs.Add(xi + (cx * ds));
        ys.Add(yi + (cy * ds));
        return ds;
    }
}
