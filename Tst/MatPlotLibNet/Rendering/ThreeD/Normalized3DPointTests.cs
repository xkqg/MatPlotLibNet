// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Rendering.ThreeD;

/// <summary>Verifies the one owner of a point of the 3-D vertex wire (<c>data-v3d</c>): its three normalised coordinates, in the
/// digits of the frame or of a mark, separated by commas, in every culture, the same characters the general format writes.</summary>
public class Normalized3DPointTests
{
    public static TheoryData<double, double, double> Points => new()
    {
        { 0, 0, 0 },
        { 0.5, -0.25, 1 },
        { 1.0 / 3.0, -2.0 / 3.0, 0.123456789 },
        { 1e-7, -3.3333333e-12, 9.87654321e15 },
        { -1.7976931348623157E+308, double.Epsilon, double.NaN },
    };

    [Theory]
    [MemberData(nameof(Points))]
    public void APoint_IsItsThreeCoordinates_InTheDigitsItIsGiven_InEveryCulture(double nx, double ny, double nz)
    {
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("nl-NL");
            var point = new Normalized3DPoint(nx, ny, nz);
            Span<char> into = stackalloc char[Normalized3DPoint.PointLength];
            foreach (string digits in new[] { Normalized3DPoint.FrameDigits, Normalized3DPoint.MarkDigits })
            {
                string expected = string.Join(',', new[] { nx, ny, nz }.Select(v => v.ToString(digits, CultureInfo.InvariantCulture)));

                Assert.Equal(expected, into[..point.FormatWire(into, digits)].ToString());
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Fact]
    public void TheFrame_IsWrittenInSixDigits_AndAMark_InFour()
    {
        Assert.Equal("G6", Normalized3DPoint.FrameDigits);
        Assert.Equal("G4", Normalized3DPoint.MarkDigits);
    }
}
