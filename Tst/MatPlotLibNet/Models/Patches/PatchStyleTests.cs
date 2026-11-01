// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Serialization;

namespace MatPlotLibNet.Tests.Models.Patches;

/// <summary>What a patch looks like when nothing is set (matplotlib's patch defaults, measured on 3.11.2: a face of C0 that
/// never moves the colour cycle, no edge on a filled shape, a black edge on an arrow, an edge one unit wide, opaque, no hatch,
/// a hatch in the edge colour and in black when there is no edge), how its opacity reaches the face and the edge alike, and
/// which values it refuses: the style values only, each naming what was refused. The geometry refuses nothing, as
/// matplotlib's does not; a geometry that cannot be drawn has no outline.</summary>
public class PatchStyleTests
{
    private static readonly DataPoint[] Triangle = [new(0, 0), new(1, 0), new(1, 1)];

    /// <summary>One patch of each leaf, as a caller makes it.</summary>
    private static Patch[] OneOfEach() =>
    [
        new Rectangle(new(0, 0), 1, 1), new Polygon(Triangle), new Arrow(new(0, 0), 1, 0),
        new Circle(new(0, 0), 1), new Ellipse(new(0, 0), 2, 1), new Wedge(new(0, 0), 1, 0, 90), new Arc(new(0, 0), 2, 1),
    ];

    public static TheoryData<string> LeafNames { get; } = new(OneOfEach().Select(patch => patch.GetType().Name));

    public static TheoryData<string> FilledLeafNames { get; } = new(OneOfEach().OfType<FilledPatch>().Select(patch => patch.GetType().Name));

    private static Patch Leaf(string name) => OneOfEach().Single(patch => patch.GetType().Name == name);

    // ---- defaults ---------------------------------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(FilledLeafNames))]
    public void EveryFilledLeaf_IsFilledWithTheFirstColourOfTheCycle_ByDefault(string leaf)
    {
        var patch = Assert.IsAssignableFrom<FilledPatch>(Leaf(leaf));

        Assert.Equal(Colors.Tab10Blue, patch.FaceColor);
        Assert.Equal(Colors.Tab10Blue, patch.DrawnFaceColor);
    }

    /// <summary>Every leaf but the arc encloses an area and fills it; the arc is the one matplotlib refuses to fill.</summary>
    [Fact]
    public void EveryLeafButTheArc_IsAFilledPatch()
    {
        Assert.Equal([nameof(Arc)], OneOfEach().Where(patch => patch is not FilledPatch).Select(patch => patch.GetType().Name));
    }

    [Theory]
    [InlineData(nameof(Rectangle))]
    [InlineData(nameof(Polygon))]
    [InlineData(nameof(Circle))]
    [InlineData(nameof(Ellipse))]
    [InlineData(nameof(Wedge))]
    public void AFilledShape_HasNoEdge_ByDefault(string leaf)
    {
        Assert.Equal(Colors.Transparent, Leaf(leaf).EdgeColor);
    }

    [Fact]
    public void AnArrow_HasABlackEdge_ByDefault()
    {
        Assert.Equal(Colors.Black, new Arrow(new(0, 0), 1, 0).EdgeColor);
    }

    /// <summary>matplotlib 3.11.2: <c>Arc((0, 0), 2, 1)</c> has a face of alpha 0 and a black edge; <c>fill=True</c> raises
    /// "Arc objects cannot be filled". An arc here has no face to set: it draws none, and its edge is black.</summary>
    [Fact]
    public void AnArc_HasABlackEdge_AndNoFace()
    {
        var arc = new Arc(new(0, 0), 2, 1);

        Assert.Equal(Colors.Black, arc.EdgeColor);
        Assert.Equal(0, arc.DrawnFaceColor.A);
        Assert.Null(arc.DrawnStyle.Fill);
        Assert.Equal(Colors.Black, arc.DrawnStyle.Stroke);
        Assert.Null(typeof(Arc).GetProperty(nameof(FilledPatch.FaceColor)));
    }

    /// <summary>A hatch on an arc is drawn as matplotlib draws it: over the region the arc and its chord enclose, though the arc has no
    /// face (measured on matplotlib 3.11.2: an upper half arc hatched '//' darkens 401 pixels inside its chord, none without the
    /// hatch). The arc keeps a clear fill for the hatch to lie on, the rule of every patch with a face of none.</summary>
    [Fact]
    public void AHatchedArc_KeepsAClearFillForItsHatch_AsMatplotlibHatchesTheRegionItsChordCloses()
    {
        var arc = new Arc(new(0, 0), 2, 2) { Theta2 = 180, Hatch = HatchPattern.ForwardDiagonal, EdgeColor = Colors.Red };

        var style = arc.DrawnStyle;

        Assert.Equal(0, style.Fill!.Value.A);
        Assert.True(style.HasVisibleHatch);
        Assert.Equal(Colors.Red, style.HatchColor);
    }

    [Fact]
    public void ACircle_KeepsWhatItWasMadeWith()
    {
        var circle = new Circle(new(1, 2), 3);

        Assert.Equal(new DataPoint(1, 2), circle.Center);
        Assert.Equal(3, circle.Radius);
    }

    [Fact]
    public void AnEllipse_KeepsWhatItWasMadeWith_AndIsNotTurned()
    {
        var ellipse = new Ellipse(new(1, 2), 4, 3);

        Assert.Equal(new DataPoint(1, 2), ellipse.Center);
        Assert.Equal(4, ellipse.Width);
        Assert.Equal(3, ellipse.Height);
        Assert.Equal(0, ellipse.Angle);
    }

    [Fact]
    public void AWedge_KeepsWhatItWasMadeWith_AndReachesItsCentre()
    {
        var wedge = new Wedge(new(1, 2), 3, 30, 120);

        Assert.Equal(new DataPoint(1, 2), wedge.Center);
        Assert.Equal(3, wedge.Radius);
        Assert.Equal(30, wedge.Theta1);
        Assert.Equal(120, wedge.Theta2);
        Assert.Null(wedge.Width);
    }

    /// <summary>matplotlib's <c>Arc</c> defaults: not turned, from 0 to 360 degrees, the whole ellipse.</summary>
    [Fact]
    public void AnArc_KeepsWhatItWasMadeWith_AndIsTheWholeEllipse()
    {
        var arc = new Arc(new(1, 2), 4, 3);

        Assert.Equal(new DataPoint(1, 2), arc.Center);
        Assert.Equal(4, arc.Width);
        Assert.Equal(3, arc.Height);
        Assert.Equal(0, arc.Angle);
        Assert.Equal(0, arc.Theta1);
        Assert.Equal(360, arc.Theta2);
    }

    [Theory]
    [MemberData(nameof(LeafNames))]
    public void EveryLeaf_RestsAtMatplotlibsPatchDefaults(string leaf)
    {
        var patch = Leaf(leaf);

        Assert.Equal(1.0, patch.EdgeWidth);
        Assert.Equal(LineStyle.Solid, patch.EdgeLineStyle);
        Assert.Equal(1.0, patch.Alpha);
        Assert.Null(patch.Label);
        Assert.Equal(HatchPattern.None, patch.Hatch);
        Assert.Null(patch.HatchColor);
    }

    [Fact]
    public void AnArrow_RestsAtMatplotlibsFancyArrowDefaults()
    {
        var arrow = new Arrow(new(1, 2), 3, 4);

        Assert.Equal(new DataPoint(1, 2), arrow.Start);
        Assert.Equal(3, arrow.Dx);
        Assert.Equal(4, arrow.Dy);
        Assert.Equal(0.001, arrow.Width);
        Assert.Null(arrow.HeadWidth);
        Assert.Null(arrow.HeadLength);
        Assert.False(arrow.LengthIncludesHead);
        Assert.Equal(ArrowHeadShape.Full, arrow.Shape);
        Assert.Equal(0, arrow.Overhang);
    }

    [Fact]
    public void ARectangle_KeepsWhatItWasMadeWith_AndIsNotTurned()
    {
        var rectangle = new Rectangle(new(1, 2), -3, 4);

        Assert.Equal(new DataPoint(1, 2), rectangle.Anchor);
        Assert.Equal(-3, rectangle.Width);
        Assert.Equal(4, rectangle.Height);
        Assert.Equal(0, rectangle.Angle);
    }

    // ---- the colours drawn ---------------------------------------------------------------------------------------------

    [Fact]
    public void Alpha_ReachesTheFaceAndTheEdgeAlike()
    {
        var patch = new Rectangle(new(0, 0), 1, 1) { FaceColor = Colors.Red, EdgeColor = Colors.Green, Alpha = 0.5 };

        Assert.Equal(Colors.Red.WithAlpha(128), patch.DrawnFaceColor);
        Assert.Equal(Colors.Green.WithAlpha(128), patch.DrawnEdgeColor);
    }

    [Fact]
    public void Alpha_ScalesAColourThatIsAlreadyTranslucent_AndLeavesNoneAsNone()
    {
        var patch = new Polygon(Triangle) { FaceColor = Colors.Red.WithAlpha(100), Alpha = 0.5 };

        Assert.Equal(Colors.Red.WithAlpha(50), patch.DrawnFaceColor);
        Assert.Equal(0, patch.DrawnEdgeColor.A);
    }

    [Fact]
    public void AtRest_TheFaceAndTheEdgeAreDrawnAsSet()
    {
        var arrow = new Arrow(new(0, 0), 1, 0);

        Assert.Equal(Colors.Tab10Blue, arrow.DrawnFaceColor);
        Assert.Equal(Colors.Black, arrow.DrawnEdgeColor);
    }

    [Fact]
    public void TheHatch_IsDrawnInTheEdgeColour_UnlessItHasAColourOfItsOwn()
    {
        var patch = new Rectangle(new(0, 0), 1, 1) { EdgeColor = Colors.Green, Hatch = HatchPattern.Cross, Alpha = 0.5 };

        Assert.Equal(Colors.Green.WithAlpha(128), patch.DrawnHatchColor);

        patch.HatchColor = Colors.Red;

        Assert.Equal(Colors.Red.WithAlpha(128), patch.DrawnHatchColor);
    }

    [Fact]
    public void TheHatchOfAShapeWithNoEdge_IsBlack()
    {
        // matplotlib: Rectangle((0, 0), 1, 1, hatch='/').get_hatchcolor() is (0, 0, 0, 1) while the edge is 'none'.
        var patch = new Rectangle(new(0, 0), 1, 1) { Hatch = HatchPattern.ForwardDiagonal };

        Assert.Equal(Colors.Black, patch.DrawnHatchColor);
    }

    // ---- the style the patch is drawn with -------------------------------------------------------------------------------

    /// <summary>The one style the drawing hands every backend: the face, the edge, its width and dashes, the hatch and its colour,
    /// each colour with the opacity applied.</summary>
    [Fact]
    public void TheDrawnStyle_CarriesTheFaceTheEdgeAndTheHatch_WithTheOpacityApplied()
    {
        var patch = new Rectangle(new(0, 0), 1, 1)
        {
            FaceColor = Colors.Red, EdgeColor = Colors.Green, EdgeWidth = 2.5, EdgeLineStyle = LineStyle.Dotted,
            Hatch = HatchPattern.Dots, HatchColor = Colors.Blue, Alpha = 0.5,
        };

        var style = patch.DrawnStyle;

        Assert.Equal(Colors.Red.WithAlpha(128), style.Fill);
        Assert.Equal(Colors.Green.WithAlpha(128), style.Stroke);
        Assert.Equal(2.5, style.StrokeThickness);
        Assert.Equal(LineStyle.Dotted, style.StrokeLineStyle);
        Assert.Equal(HatchPattern.Dots, style.Hatch);
        Assert.Equal(Colors.Blue.WithAlpha(128), style.HatchColor);
    }

    /// <summary>A face of none fills nothing and an edge of none strokes nothing: no backend is handed a colour of none to paint.</summary>
    [Fact]
    public void AFaceOfNone_FillsNothing_AndAnEdgeOfNone_StrokesNothing()
    {
        var patch = new Polygon(Triangle) { FaceColor = Colors.Transparent };

        var style = patch.DrawnStyle;

        Assert.Null(style.Fill);
        Assert.Null(style.Stroke);
        Assert.False(style.HasVisibleStroke);
    }

    /// <summary>A hatch needs something to lie on: a hatched patch with a face of none keeps a clear fill, so the hatch is drawn
    /// and the face is not, as matplotlib draws the hatch of an unfilled patch.</summary>
    [Fact]
    public void AHatchedPatchWithAFaceOfNone_KeepsAClearFill_ForItsHatch()
    {
        var patch = new Rectangle(new(0, 0), 1, 1) { FaceColor = Colors.Transparent, Hatch = HatchPattern.Cross };

        var style = patch.DrawnStyle;

        Assert.Equal(0, style.Fill!.Value.A);
        Assert.True(style.HasVisibleHatch);
        Assert.Equal(Colors.Black, style.HatchColor);
    }

    /// <summary>A patch that is entirely clear (alpha 0) draws no face and no edge.</summary>
    [Fact]
    public void AClearPatch_DrawsNeitherFaceNorEdge()
    {
        var arrow = new Arrow(new(0, 0), 1, 0) { Alpha = 0 };

        Assert.Null(arrow.DrawnStyle.Fill);
        Assert.Null(arrow.DrawnStyle.Stroke);
    }

    // ---- refusals --------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    [InlineData(double.NaN)]
    public void AnAlphaOutsideZeroToOne_IsRefused_AndTheOldOneKept(double alpha)
    {
        var patch = new Rectangle(new(0, 0), 1, 1) { Alpha = 0.4 };

        var refused = Assert.Throws<ArgumentOutOfRangeException>(() => patch.Alpha = alpha);

        Assert.Equal("value", refused.ParamName);
        Assert.Equal(0.4, patch.Alpha);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void AnAlphaAtEitherEnd_IsTaken(double alpha)
    {
        var patch = new Arrow(new(0, 0), 1, 0) { Alpha = alpha };

        Assert.Equal(alpha, patch.Alpha);
    }

    [Theory]
    [InlineData(-0.5)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void AnEdgeWidthThatIsNegativeOrNotFinite_IsRefused_AndTheOldOneKept(double width)
    {
        var patch = new Polygon(Triangle) { EdgeWidth = 2 };

        var refused = Assert.Throws<ArgumentOutOfRangeException>(() => patch.EdgeWidth = width);

        Assert.Equal("value", refused.ParamName);
        Assert.Equal(2, patch.EdgeWidth);
    }

    [Fact]
    public void AnEdgeOfNoWidth_IsTaken()
    {
        var patch = new Polygon(Triangle) { EdgeWidth = 0 };

        Assert.Equal(0, patch.EdgeWidth);
    }

    [Fact]
    public void APolygonMadeWithoutVertices_IsRefused()
    {
        var refused = Assert.Throws<ArgumentNullException>(() => new Polygon(null!));

        Assert.Equal("vertices", refused.ParamName);
    }

    [Fact]
    public void SettingNoVerticesOnAPolygon_IsRefused_AndTheOldOnesKept()
    {
        var polygon = new Polygon(Triangle);

        var refused = Assert.Throws<ArgumentNullException>(() => polygon.Vertices = null!);

        Assert.Equal("value", refused.ParamName);
        Assert.Equal(Triangle, polygon.Vertices);
    }

    [Fact]
    public void TheGeometry_RefusesNothing_AsMatplotlibsDoesNot()
    {
        var rectangle = new Rectangle(new(0, 0), 1, 1) { Width = -5, Height = double.NaN, Angle = 720 };
        var arrow = new Arrow(new(0, 0), 1, 0) { Width = -0.1, HeadWidth = 0, HeadLength = -1, Overhang = 3 };

        Assert.Equal(-5, rectangle.Width);
        Assert.True(double.IsNaN(rectangle.Height));
        Assert.Equal(-0.1, arrow.Width);
        Assert.Equal(-1, arrow.HeadLength);
    }

    /// <summary>The curved shapes refuse no number either: a size of 0 or below, or an angle that is not a number, is kept and the
    /// shape has no outline (<see cref="CurvedPatchOutlineTests"/>).</summary>
    [Fact]
    public void TheCurvedGeometry_RefusesNothing_AsMatplotlibsDoesNot()
    {
        var circle = new Circle(new(0, 0), 1) { Radius = -2 };
        var ellipse = new Ellipse(new(0, 0), 1, 1) { Width = 0, Angle = double.NaN };
        var wedge = new Wedge(new(0, 0), 1, 0, 90) { Theta1 = double.PositiveInfinity, Width = 5 };
        var arc = new Arc(new(0, 0), 1, 1) { Height = -1, Theta2 = -720 };

        Assert.Equal(-2, circle.Radius);
        Assert.Equal(0, ellipse.Width);
        Assert.True(double.IsNaN(ellipse.Angle));
        Assert.Equal(double.PositiveInfinity, wedge.Theta1);
        Assert.Equal(5, wedge.Width);
        Assert.Equal(-1, arc.Height);
        Assert.Equal(-720, arc.Theta2);
    }

    // ---- the drawn-property contract can set every property on its own ------------------------------------------------

    /// <summary>The JSON contract sets the drawn properties of a part one at a time, each to a value that is not its default
    /// (<see cref="DrawnSeriesFixtures.NotTheDefault"/>), so no setter may depend on another having been set first, and the
    /// value generator has to know every type a patch has. Each setter takes the value on its own and gives it back.</summary>
    [Theory]
    [MemberData(nameof(LeafNames))]
    public void EverySettableProperty_TakesTheContractsValue_OnItsOwn(string leaf)
    {
        var properties = Leaf(leaf).GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.SetMethod is { IsPublic: true } && p.GetIndexParameters().Length == 0)
            .OrderBy(p => p.Name, StringComparer.Ordinal).ToList();

        Assert.NotEmpty(properties);
        for (int k = 0; k < properties.Count; k++)
        {
            var patch = Leaf(leaf);
            var property = properties[k];
            object? before = property.GetValue(patch);

            object value = DrawnSeriesFixtures.NotTheDefault(property, before, k);
            property.SetValue(patch, value);

            Assert.False(DrawnSeriesFixtures.SameValue(before, value), $"{leaf}.{property.Name}: the contract's value is the default.");
            Assert.True(DrawnSeriesFixtures.SameValue(value, property.GetValue(patch)), $"{leaf}.{property.Name}: set to {DrawnSeriesFixtures.Show(value)}, read {DrawnSeriesFixtures.Show(property.GetValue(patch))}.");
        }
    }
}
