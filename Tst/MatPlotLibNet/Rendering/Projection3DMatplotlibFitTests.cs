// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Pins where <see cref="Projection3D"/> puts the box in the plot to the rule matplotlib 3.11 follows, read from its own
/// code (<c>mpl_toolkits/mplot3d/axes3d.py</c>):
/// <code>
/// def set_top_view(self):
///     xdwl = 0.95 / self._dist
///     xdw = 0.9 / self._dist
///     ydwl = 0.95 / self._dist
///     ydw = 0.9 / self._dist
///     self.viewLim.intervalx = (-xdwl, xdw)
///     self.viewLim.intervaly = (-ydwl, ydw)
///
/// def apply_aspect(self, position=None):
///     ...
///     box_aspect = 1
///     pb = position.frozen()
///     pb1 = pb.shrunk_to_aspect(box_aspect, pb, fig_aspect)
///     self._set_position(pb1.anchored(self.get_anchor(), pb), 'active')
/// </code>
/// The projected point is a data point of a square axes (the plot shrunk to a square, centred) whose limits are fixed at
/// (-0.95/dist, 0.9/dist) in both directions, dist = 10: the box is NOT fitted to its projected corners. The pins are
/// matplotlib's own pixels for an 800 x 600 figure with one 3-D subplot (the axes square is x 179..641, y 72..534 top
/// down), limits (0, 1) on all three axes, elev 30, azim -60, printed by
/// <c>fig.canvas.draw(); ax.transData.transform(proj3d.proj_transform(x, y, z, ax.get_proj())[:2])</c> (y turned top down).
/// A camera distance d draws as matplotlib's perspective with focal length d / 10: the eye stands at the same place
/// (<c>eye_focal = R + dist * ps * focal_length</c>) and the picture has the same size.</summary>
public sealed class Projection3DMatplotlibFitTests
{
    private static readonly Rect AxesSquare = new(179, 72, 462, 462);

    // (x, y, z) -> matplotlib's pixel, default perspective camera (focal_length 1).
    public static TheoryData<double, double, double, double, double> DefaultCamera => new()
    {
        { 0, 0, 0, 213.8842, 420.0856 }, { 1, 0, 0, 473.4015, 504.8440 }, { 0, 1, 0, 366.4517, 292.2005 },
        { 1, 1, 0, 611.2681, 363.3590 }, { 0, 0, 1, 204.4622, 224.4322 }, { 1, 0, 1, 476.2140, 302.2445 },
        { 0, 1, 1, 364.3308, 107.7673 }, { 1, 1, 1, 620.0047, 172.5732 }, { 0.5, 0.5, 0.5, 416.2432, 296.7568 },
    };

    // (distance, x, y, z) -> matplotlib's pixel with set_proj_type('persp', focal_length=distance / 10).
    public static TheoryData<double, double, double, double, double, double> CloserCamera => new()
    {
        { 5, 0, 0, 0, 214.5782, 419.6627 }, { 5, 1, 0, 0, 476.4432, 515.9173 }, { 5, 0, 0, 1, 194.9559, 221.1857 },
        { 5, 1, 1, 1, 620.7132, 172.1414 }, { 5, 0.5, 0.5, 0.5, 416.2432, 296.7568 },
        { 2, 0, 0, 0, 216.6318, 418.4110 }, { 2, 1, 0, 0, 487.8795, 557.5518 }, { 2, 0, 0, 1, 160.5195, 209.4255 },
        { 2, 1, 1, 1, 622.8686, 170.8278 },
    };

    [Theory]
    [MemberData(nameof(DefaultCamera))]
    public void TheDefaultCamera_PutsEveryCornerWhereMatplotlibDrawsIt(double x, double y, double z, double px, double py)
    {
        var projection = new Projection3D(30, -60, AxesSquare, 0, 1, 0, 1, 0, 1);

        AssertAt(px, py, projection.Project(x, y, z));
    }

    [Theory]
    [MemberData(nameof(CloserCamera))]
    public void ACameraDistance_DrawsAsMatplotlibsFocalLengthOfATenth(double distance, double x, double y, double z,
        double px, double py)
    {
        var projection = new Projection3D(30, -60, AxesSquare, 0, 1, 0, 1, 0, 1, distance);

        AssertAt(px, py, projection.Project(x, y, z));
    }

    /// <summary>A plot that is not square draws the box in the square centred inside it, matplotlib's <c>apply_aspect</c>
    /// with the default anchor <c>'C'</c>: the subplot box of the same figure (x 100..720) puts every point where the
    /// square does.</summary>
    [Fact]
    public void APlotThatIsNotSquare_DrawsInTheSquareCentredInIt()
    {
        var wide = new Projection3D(30, -60, new Rect(100, 72, 620, 462), 0, 1, 0, 1, 0, 1);
        var tall = new Projection3D(30, -60, new Rect(179, 0, 462, 606), 0, 1, 0, 1, 0, 1);
        var square = new Projection3D(30, -60, AxesSquare, 0, 1, 0, 1, 0, 1);

        foreach (var (x, y, z) in (ReadOnlySpan<(double, double, double)>)[(0, 0, 0), (1, 0, 1), (0.25, 0.75, 0.5)])
        {
            var expected = square.Project(x, y, z);
            AssertAt(expected.X, expected.Y, wide.Project(x, y, z));
            AssertAt(expected.X, expected.Y, tall.Project(x, y, z));
        }
    }

    /// <summary>The size of the box depends on the camera angle only through the projection, as in matplotlib: the
    /// centre of the box stays at the same pixel whatever the camera looks from.</summary>
    [Theory]
    [InlineData(0, 0)]
    [InlineData(60, 210)]
    [InlineData(-30, 45)]
    public void TheCentreOfTheBox_StaysAtTheSamePixel(double elevation, double azimuth)
    {
        var projection = new Projection3D(elevation, azimuth, AxesSquare, -2, 8, 0, 1, 100, 300);

        AssertAt(416.2432, 296.7568, projection.Project(3, 0.5, 200));
    }

    private static void AssertAt(double px, double py, Point actual)
    {
        Assert.InRange(actual.X, px - 2e-4, px + 2e-4);
        Assert.InRange(actual.Y, py - 2e-4, py + 2e-4);
    }
}
