// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies <see cref="PolarTransform"/> coordinate conversion.</summary>
public class PolarTransformTests
{
    private static PolarTransform CreateTransform() =>
        new(new Rect(0, 0, 400, 400), 10.0);

    /// <summary>Verifies that r=0 maps to the center of the plot area.</summary>
    [Fact]
    public void PolarToPixel_AtOrigin_ReturnsCenter()
    {
        var t = CreateTransform();
        var pt = t.PolarToPixel(0, 0);
        Assert.Equal(t.CenterX, pt.X, 1);
        Assert.Equal(t.CenterY, pt.Y, 1);
    }

    /// <summary>Verifies that r=rMax, theta=0 maps to the right edge.</summary>
    [Fact]
    public void PolarToPixel_MaxR_Theta0_MapsToRight()
    {
        var t = CreateTransform();
        var pt = t.PolarToPixel(10, 0);
        Assert.True(pt.X > t.CenterX);
        Assert.Equal(t.CenterY, pt.Y, 1);
    }

    /// <summary>Verifies that theta=PI/2 maps upward (Y decreases in pixel coords).</summary>
    [Fact]
    public void PolarToPixel_Theta90_MapsUp()
    {
        var t = CreateTransform();
        var pt = t.PolarToPixel(10, Math.PI / 2);
        Assert.Equal(t.CenterX, pt.X, 1);
        Assert.True(pt.Y < t.CenterY);
    }

    /// <summary>Verifies that theta=PI maps to the left.</summary>
    [Fact]
    public void PolarToPixel_Theta180_MapsLeft()
    {
        var t = CreateTransform();
        var pt = t.PolarToPixel(10, Math.PI);
        Assert.True(pt.X < t.CenterX);
        Assert.Equal(t.CenterY, pt.Y, 1);
    }

    /// <summary>A radius past rMax stands past the edge, on the same scale, as matplotlib's PolarTransform places it: the axes
    /// clips it at the circle (the axes patch, matplotlib's <c>clip_path</c>), never flattens it onto the rim.</summary>
    [Fact]
    public void PolarToPixel_BeyondMax_IsPlacedPastTheEdge()
    {
        var t = CreateTransform();
        var beyond = t.PolarToPixel(20, 0);
        Assert.Equal(200 + 400, beyond.X, 9);
        Assert.Equal(200, beyond.Y, 9);
    }

    /// <summary>A radius or an angle that cannot be placed maps to a pixel no renderer draws, as a cartesian coordinate
    /// that cannot be placed does: an infinite radius is not a radius (it was drawn on the edge as if it were rMax).</summary>
    [Theory]
    [InlineData(double.NaN, 0)]
    [InlineData(double.PositiveInfinity, 0)]
    [InlineData(double.NegativeInfinity, 0)]
    [InlineData(5, double.NaN)]
    [InlineData(5, double.PositiveInfinity)]
    public void PolarToPixel_ACoordinateThatCannotBePlaced_IsNotADrawablePixel(double r, double theta)
    {
        var pt = CreateTransform().PolarToPixel(r, theta);

        Assert.False(double.IsFinite(pt.X) && double.IsFinite(pt.Y));
    }

    // ---- a low end and an origin (matplotlib's rmin and rorigin) ------------------------------------------------------------

    /// <summary>The scale matplotlib's default style gives r from -20 to -3: rlim (-20.85, -2.15), the origin at the low end.</summary>
    private static PolarTransform BelowZero() => new(new Rect(0, 0, 400, 400), -20.85, -2.15, -20.85);

    [Fact]
    public void ARadiusBelowZero_IsPlacedOnAScaleThatReachesIt()
    {
        // matplotlib's PolarTransform: (r - rorigin) * rsign, scaled so that rmax stands on the edge: -10 stands 10.85 / 18.7 out.
        var pt = BelowZero().PolarToPixel(-10, 0);

        Assert.Equal(200 + 200 * 10.85 / 18.7, pt.X, 9);
        Assert.Equal(200, pt.Y, 9);
    }

    [Fact]
    public void TheLowEnd_StandsAtTheCentre_WhenTheOriginIsThere()
    {
        var pt = BelowZero().PolarToPixel(-20.85, 1);

        Assert.Equal(200, pt.X, 9);
        Assert.Equal(200, pt.Y, 9);
    }

    [Fact]
    public void ARadiusBelowTheOrigin_IsNotADrawablePixel()
    {
        var pt = BelowZero().PolarToPixel(-25, 0);

        Assert.False(double.IsFinite(pt.X));
    }

    [Fact]
    public void ARadiusPastTheHighEnd_IsPlacedPastTheEdge() =>
        Assert.Equal(200 + 200 * 25.85 / 18.7, BelowZero().PolarToPixel(5, 0).X, 9);

    /// <summary>An origin below the low end leaves a hole: the low end stands (rmin - rorigin) / (rmax - rorigin) of the way out, and a
    /// radius between the origin and the low end stands inside the hole on the same scale, where the clip to the ring cuts it off
    /// (matplotlib's axes patch is a wedge with that width).</summary>
    [Fact]
    public void AnOriginBelowTheLowEnd_LeavesAHole()
    {
        var transform = new PolarTransform(new Rect(0, 0, 400, 400), 0, 9.4, -3);

        Assert.Equal(200 + 200 * 3 / 12.4, transform.PolarToPixel(0, 0).X, 9);
        Assert.Equal(200 + 200 * 2 / 12.4, transform.PolarToPixel(-1, 0).X, 9);
        Assert.Equal(200 * 3 / 12.4, transform.InnerRadius, 9);
    }

    /// <summary>The clip of a polar axes is the disc of its outer ring: two half turns (an SVG arc cannot turn a whole circle), and its
    /// box the square round it.</summary>
    [Fact]
    public void TheClip_IsTheDiscOfTheOuterRing()
    {
        var clip = CreateTransform().Clip;

        Assert.Equal(new Rect(0, 0, 400, 400), clip.Bounds);
        var outline = clip.Outline!;
        Assert.Equal(new MoveToSegment(new Point(400, 200)), outline[0]);
        Assert.Equal(new ArcSegment(new Point(200, 200), 200, 200, 0, 180), outline[1]);
        Assert.Equal(new ArcSegment(new Point(200, 200), 200, 200, 180, 360), outline[2]);
        Assert.IsType<CloseSegment>(outline[3]);
        Assert.Equal(4, outline.Count);
    }

    /// <summary>With a hole the clip is the ring between the hole and the outer ring: the outer circle one way round and the inner
    /// one the other, so the nonzero rule leaves the hole out.</summary>
    [Fact]
    public void TheClip_OfAScaleWithAHole_IsTheRing()
    {
        var transform = new PolarTransform(new Rect(0, 0, 400, 400), 0, 9.4, -3);
        double hole = transform.InnerRadius;

        var outline = transform.Clip.Outline!;

        Assert.Equal(8, outline.Count);
        Assert.Equal(new MoveToSegment(new Point(200 + hole, 200)), outline[4]);
        Assert.Equal(new ArcSegment(new Point(200, 200), hole, hole, 360, 180), outline[5]);
        Assert.Equal(new ArcSegment(new Point(200, 200), hole, hole, 180, 0), outline[6]);
    }

    [Fact]
    public void AScaleFromHighToLow_PutsItsLowEndAtTheCentre()
    {
        // matplotlib: ax.set_ylim(10, 0) gives rorigin 10, rsign -1: 10 at the centre, 0 on the edge.
        var transform = new PolarTransform(new Rect(0, 0, 400, 400), 10, 0, 10);

        Assert.Equal(200, transform.PolarToPixel(10, 0).X, 9);
        Assert.Equal(300, transform.PolarToPixel(5, 0).X, 9);
        Assert.Equal(400, transform.PolarToPixel(0, 0).X, 9);
        Assert.False(double.IsFinite(transform.PolarToPixel(11, 0).X));
    }

    [Fact]
    public void ARingStands_WhereItsRadiusIsPlaced() =>
        Assert.Equal(200 * 10.85 / 18.7, BelowZero().PixelRadiusAt(-10), 9);

    /// <summary>A radius drawn on a scale that runs from its origin to its outer end.</summary>
    private readonly record struct RingAt(double R, double RMax, double ROrigin);

    /// <summary>Radii over the scale of each transform below, then past its ends.</summary>
    private static IEnumerable<RingAt> RingsAndData()
    {
        var random = new Random(20261122);
        foreach (var (rMin, rMax, rOrigin) in new[] { (0.0, 10.0, 0.0), (-3.0, 7.3, -5.15), (0.0, 1.0, 0.0), (2.0, 37.0, 0.0) })
        {
            for (int i = 0; i < 60; i++) yield return new(rMin + (rMax - rMin) * random.NextDouble() * 1.2, rMax, rOrigin);
        }
    }

    /// <summary>A ring drawn at a radius and the data at that radius stand at the same distance from the centre, to the last bit: the
    /// transform works the distance out one way, its share of the way from the origin to the edge times the outer radius. One sweep,
    /// each radius that differs named.</summary>
    [Fact]
    public void ARingAndTheDataAtItsRadius_StandAtOneDistance() =>
        Assert.All(RingsAndData(), ring =>
        {
            var transform = new PolarTransform(new Rect(0, 0, 400, 400), ring.ROrigin, ring.RMax, ring.ROrigin);

            Assert.Equal(BitConverter.DoubleToInt64Bits(transform.WedgeOf(ring.R, ring.R, 0, 1).OuterRadius),
                BitConverter.DoubleToInt64Bits(transform.PixelRadiusAt(ring.R)));
        });

    [Fact]
    public void AnOriginAtTheHighEnd_IsMovedAUnitInside_SoTheScaleHasALength() =>
        Assert.Equal(200, new PolarTransform(new Rect(0, 0, 400, 400), 4, 4, 4).PolarToPixel(4, 0).X, 9);

    [Fact]
    public void TheTransformOfAnExtentAlone_IsTheOneWithItsLowEndAndOriginAtZero()
    {
        var alone = new PolarTransform(new Rect(0, 0, 400, 400), 10);
        var full = new PolarTransform(new Rect(0, 0, 400, 400), 0, 10, 0);

        foreach (double r in new[] { 0.0, 2.5, 7.3, 10, 12 })
            Assert.Equal(alone.PolarToPixel(r, 0.7), full.PolarToPixel(r, 0.7));
        Assert.Equal(0, alone.InnerRadius);
    }

    /// <summary>Verifies that CenterX and CenterY are at the midpoint.</summary>
    [Fact]
    public void Center_IsAtMidpoint()
    {
        var t = CreateTransform();
        Assert.Equal(200, t.CenterX, 1);
        Assert.Equal(200, t.CenterY, 1);
    }

    /// <summary>A radius at the edge of 0 or below is read as 1: the plot maps as it does with a radius of 1.</summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(-2.0)]
    public void ARadiusOfNothingAtTheEdge_IsReadAsOne(double rMax)
    {
        var bounds = new Rect(0, 0, 100, 100);

        Assert.Equal(new PolarTransform(bounds, 1).PolarToPixel(0.5, 1.0), new PolarTransform(bounds, rMax).PolarToPixel(0.5, 1.0));
    }
}
