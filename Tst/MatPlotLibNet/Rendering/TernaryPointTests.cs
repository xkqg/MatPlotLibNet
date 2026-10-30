// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A ternary diagram places a composition of three parts in an equilateral triangle: the part called Top at the
/// apex, Left at the bottom left, Right at the bottom right (the order of mpltern, counter-clockwise). One owner converts
/// between the composition and the position in a frame one unit wide, <c>x = r + t/2</c> and <c>y = (√3/2)·t</c>, so the
/// renderer, the hit test, the table and the Piper diagram all stand on the same rule. The vectors below are worked out by
/// hand from that rule.</summary>
public class TernaryPointTests
{
    [Theory]
    [InlineData(0.2, 0.5, 0.3, 0.4, 0.173205)]                       // T1
    [InlineData(1.0, 0.0, 0.0, 0.5, 0.866025)]                       // T2: the apex
    [InlineData(0.0, 1.0, 0.0, 0.0, 0.0)]                            // the bottom left corner
    [InlineData(0.0, 0.0, 1.0, 1.0, 0.0)]                            // the bottom right corner
    [InlineData(1.0 / 3, 1.0 / 3, 1.0 / 3, 0.5, 0.288675)]           // the centroid
    [InlineData(0.5, 0.5, 0.0, 0.25, 0.433013)]                      // halfway along the left edge
    public void ACompositionStandsWhereTheVectorsSay(double top, double left, double right, double x, double y)
    {
        var at = new TernaryPoint(top, left, right).InFrame();

        Assert.Equal(x, at.X, 6);
        Assert.Equal(y, at.Y, 6);
    }

    [Fact]
    public void TheApexIsExactlyRootThreeOverTwo_NotTheRoundedNumber()
    {
        // 0.866 is 2.5e-5 short: close enough to pass a six-decimal vector, far enough to put a point off its gridline.
        Assert.Equal(Math.Sqrt(3) / 2, new TernaryPoint(1, 0, 0).InFrame().Y, 12);
        Assert.Equal(Math.Sqrt(3) / 2, TernaryFrame.Height, 12);
    }

    [Fact]
    public void ACompositionIsClosedBeforeItIsPlaced_SoTwoAndTwentyAreTheSameAsTheFractions()
    {
        // T3: (2, 5, 3) and (20, 50, 30) are 20 %, 50 % and 30 % of their sums, the composition of T1.
        var fractions = new TernaryPoint(0.2, 0.5, 0.3).Closed().InFrame();

        Assert.Equal(fractions, new TernaryPoint(2, 5, 3).Closed().InFrame());
        Assert.Equal(fractions.X, new TernaryPoint(20, 50, 30).Closed().InFrame().X, 12);
        Assert.Equal(fractions.Y, new TernaryPoint(20, 50, 30).Closed().InFrame().Y, 12);
    }

    [Fact]
    public void AClosedCompositionSumsToOne()
    {
        var closed = new TernaryPoint(2, 5, 3).Closed();

        Assert.Equal(1.0, closed.Top + closed.Left + closed.Right, 12);
        Assert.Equal(0.2, closed.Top, 12);
    }

    [Theory]
    [InlineData(-0.1, 0.5, 0.6, "Top")]
    [InlineData(0.5, -1.0, 0.6, "Left")]
    [InlineData(0.5, 0.5, -1e-9, "Right")]
    [InlineData(double.NaN, 0.5, 0.5, "Top")]
    [InlineData(0.5, double.PositiveInfinity, 0.5, "Left")]
    [InlineData(0.5, 0.5, double.NaN, "Right")]
    public void APartThatIsNegativeOrNotANumber_IsRefusedByItsName(double top, double left, double right, string name)
    {
        var refused = Assert.Throws<ArgumentOutOfRangeException>(() => new TernaryPoint(top, left, right).Closed());

        Assert.Contains(name, refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ACompositionOfNothing_IsRefusedAsSuch()
    {
        // Zero of everything has no proportions to place; a silent 0/0 would put a point at NaN.
        var refused = Assert.Throws<ArgumentException>(() => new TernaryPoint(0, 0, 0).Closed());

        Assert.Contains("all three parts are zero", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void EveryPlaceOnA21By21Grid_ComesBackToTheCompositionItWasMadeFrom()
    {
        // T4: composition → frame → composition agrees to 1e-12 on the whole grid.
        for (int i = 0; i <= 20; i++)
        {
            for (int j = 0; i + j <= 20; j++)
            {
                var made = new TernaryPoint(i / 20.0, 1 - i / 20.0 - j / 20.0, j / 20.0);

                var back = made.InFrame().ToTernary();

                Assert.Equal(made.Top, back.Top, 12);
                Assert.Equal(made.Left, back.Left, 12);
                Assert.Equal(made.Right, back.Right, 12);
            }
        }
    }

    [Fact]
    public void APlaceInsideTheTriangle_IsInside_AndOutsideItIsNot()
    {
        Assert.True(new Point(0.5, 0.3).ToTernary().IsInside());
        Assert.True(new Point(0.0, 0.0).ToTernary().IsInside());   // a corner is on the triangle
        Assert.True(new TernaryPoint(0.5, 0.5, 0).InFrame().ToTernary().IsInside());   // on an edge: a part of -1e-17 is rounding, not outside

        Assert.False(new Point(0.5, 0.9).ToTernary().IsInside());   // above the apex: Left and Right are positive, Top is past 1
        Assert.False(new Point(-0.1, 0.05).ToTernary().IsInside());   // beyond the left edge: a part is below 0
        Assert.False(new Point(0.5, -0.05).ToTernary().IsInside());   // under the base
    }
}
