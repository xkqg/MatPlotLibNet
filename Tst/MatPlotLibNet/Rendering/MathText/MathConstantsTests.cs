// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.MathText;

/// <summary>Each math font set's constants are matplotlib 3.11.2's for it (<c>matplotlib._mathtext</c>, printed with python):
/// <c>ComputerModernFontConstants</c> for 'cm', <c>DejaVuSansFontConstants</c> for 'dejavusans' (with the quad matplotlib measures,
/// <c>DejaVuSansFonts.get_quad('normal', 12, 72) / 12</c> = 0.97412109375), the library's own on DejaVu Sans' with its large operators
/// 1.4 times the text; and the module's <c>SHRINK_FACTOR</c> = 0.7, <c>NUM_SIZE_LEVELS</c> = 6, and the rule of
/// <c>get_underline_thickness</c> = 0.75 / 12 of the size.</summary>
public sealed class MathConstantsTests
{
    /// <summary>The constants of a set, in the order the arrays of the facts name them: script space, delta, the two slanted and integral
    /// deltas, the six drops, the three numerator and two denominator shifts, the axis height, the quad, the x height, the scale of a
    /// large operator.</summary>
    private static void AssertConstants(MathConstants c, params double[] expected)
    {
        double[] read =
        [
            c.ScriptSpace, c.Delta, c.DeltaSlanted, c.DeltaIntegral, c.SupDrop, c.SubDrop, c.Sup1, c.Sub1, c.Sub2,
            c.Num1, c.Num2, c.Num3, c.Denom1, c.Denom2, c.AxisHeight, c.Quad, c.XHeight, c.LargeOperatorScale,
        ];
        for (int i = 0; i < read.Length; i++) Assert.Equal(expected[i], read[i], 15);
    }

    [Fact]
    public void ComputerModern_IsMatplotlibsComputerModernFontConstants() =>
        // ComputerModernFontConstants: script_space = delta = 0.132861328125, delta_slanted = delta_integral = 0.3, _x_height = 451470
        AssertConstants(MathConstants.ComputerModern,
            0.132861328125, 0.132861328125, 0.3, 0.3,
            404864 / 451470.0, 52429 / 451470.0, 432949 / 451470.0, 157286 / 451470.0, 259226 / 451470.0,
            709370 / 451470.0, 412858 / 451470.0, 465286 / 451470.0, 719272 / 451470.0, 361592 / 451470.0,
            0.25,                    // 262144 / 2**20
            1.0000028610229492,      // 1048579 / 2**20
            0.43055534362792969,     // 451470 / 2**20
            1);

    [Fact]
    public void DejaVuSans_IsMatplotlibsDejaVuSansFontConstants() =>
        // DejaVuSansFontConstants: _x_height = 1120, x_height = 1120 / 2048, axis_height = 512 / 2048; the rest FontConstantsBase's
        AssertConstants(MathConstants.DejaVuSans,
            0.05, 0.025, 0.2, 0.1,
            790.527 / 1120, 102.4 / 1120, 845.824 / 1120, 307.199 / 1120, 632.832 / 1120,
            1529.86 / 1120, 868.352 / 1120, 970.752 / 1120, 1548.29 / 1120, 768.0 / 1120,
            0.25, 0.97412109375, 0.546875, 1);

    [Fact]
    public void TheLibrarysOwn_IsDejaVuSansWithLargerOperators()
    {
        Assert.Equal(MathConstants.DejaVuSans with { LargeOperatorScale = 1.4 }, MathConstants.Library);
    }

    [Fact]
    public void TheModulesConstants_AreSharedByEverySet()
    {
        foreach (var c in new[] { MathConstants.ComputerModern, MathConstants.DejaVuSans, MathConstants.Library })
        {
            Assert.Equal(0.7, c.ShrinkFactor);
            Assert.Equal(6, c.SizeLevels);
            Assert.Equal(0.0625, c.RuleThickness);
            Assert.Equal(0.84, c.MatrixStrutHeight);
            Assert.Equal(0.36, c.MatrixStrutDepth);
            Assert.Equal(1, c.MatrixColumnGap);
            Assert.Equal(0.2, c.DelimiterShortfall);
        }
    }

    [Theory]
    [InlineData(null, "Library")]
    [InlineData("cm", "ComputerModern")]
    [InlineData("dejavusans", "DejaVuSans")]
    public void EachSet_ReadsItsOwnConstants(string? name, string constants)
    {
        var expected = constants switch
        {
            "ComputerModern" => MathConstants.ComputerModern,
            "DejaVuSans" => MathConstants.DejaVuSans,
            _ => MathConstants.Library,
        };

        Assert.Same(expected, MathFontSet.Named(name).Constants);
    }

    /// <summary>A face leans by design (Computer Modern's math italic) or when its slant leans (the slants: FontStyleExtensionsTests); the
    /// label's own face and an upright font do not.</summary>
    [Fact]
    public void AFace_LeansWhenItsFontIsItalicOrItLeansByDesign()
    {
        Assert.False(MathFace.Label.Slanted);
        Assert.False(new MathFace(new Font()).Slanted);
        Assert.True((new MathFace(new Font()) with { Leans = true }).Slanted);
    }

    /// <summary>A run is drawn in the label's colour: a face of its own names none.</summary>
    [Fact]
    public void ARunsFont_TakesTheLabelsColour()
    {
        var face = new Font { Family = "cmmi10", Size = 10 };

        Assert.Same(face, new SpanFont(face, 10).Colored(null));
        Assert.Equal(Colors.Red, new SpanFont(face, 7).Colored(Colors.Red).Color);
        Assert.Equal(7, new SpanFont(face, 7).Colored(Colors.Red).Size);
    }

    [Fact]
    public void APointInATurnedFrame_IsTurnedCounterClockwise()
    {
        var at = new Point(10, 20).InTurnedFrame(5, 1, Math.Sin(Math.PI / 2), Math.Cos(Math.PI / 2));

        Assert.Equal(11, at.X, 9);
        Assert.Equal(15, at.Y, 9);
    }
}
