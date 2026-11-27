// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Rendering.Lighting;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>Verifies the one owner of the text the figure JSON carries a light source as (<c>lightSourceType</c>): a directional
/// light is written as <c>directional:</c> and its direction, ambient and diffuse, every digit, in every culture, and read back
/// as the same light; any other text reads as no light, so the axes keep their own.</summary>
public class LightSourceWireTests
{
    [Fact]
    public void ADirectionalLight_IsWrittenAsItsKindAndItsFiveNumbers_InEveryCulture()
    {
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("nl-NL");
            Assert.Equal("directional:0.5,-1,0.25,0.3,0.7", AxesDto.OnTheWire(new DirectionalLight(0.5, -1, 0.25)));
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Fact]
    public void NoLight_IsWrittenAsNoText() => Assert.Null(AxesDto.OnTheWire(((ILightSource?)null)));

    [Fact]
    public void ALightOfAnotherKind_IsWrittenAsNoText() => Assert.Null(AxesDto.OnTheWire(new TestLightSource(1)));

    [Theory]
    [InlineData(0.1, 0.2, 0.3, 0.4, 0.6)]
    [InlineData(-0.57735026918962573, 1e-300, 12345.678, 0, 1)]
    public void ADirectionalLight_ReadsBackAsTheSameLight(double dx, double dy, double dz, double ambient, double diffuse)
    {
        var light = new DirectionalLight(dx, dy, dz, ambient, diffuse);

        Assert.Equal(light, AxesDto.LightFromTheWire(AxesDto.OnTheWire(light)));
    }

    [Fact]
    public void FiveNumbersAndMore_ReadAsTheFirstFive() =>
        Assert.Equal(new DirectionalLight(1, 2, 3, 0.4, 0.5), AxesDto.LightFromTheWire("directional:1,2,3,0.4,0.5,9"));

    [Theory]
    [InlineData(null)]
    [InlineData("ambient:0.5")]
    [InlineData("Directional:1,2,3,0.4,0.5")]
    [InlineData("directional:0.5,0.3")]
    [InlineData("directional:0.5,0.3,NaN_bad,0.2,0.8")]
    public void AnyOtherText_ReadsAsNoLight(string? text) => Assert.Null(AxesDto.LightFromTheWire(text));
}
