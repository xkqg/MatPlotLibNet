// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>What a colour map and a colour scale put on the wire, and what they read back: the one place the choice between the name
/// field and the description field is made, and the one place it is undone. A series, a colour bar, a theme, a pair grid and an image on
/// the figure all write and read their map through these two values, so these rows are the whole rule.</summary>
public class ColorScaleWireTests
{
    private static readonly Color Red = Color.FromHex("#FF0000"), Blue = Color.FromHex("#0000FF");

    // ---- a map goes on the wire ---------------------------------------------------------------------------------------

    [Fact]
    public void NoMap_IsNothingOnTheWire()
    {
        var wire = ((IColorMap?)null).ColorMapOnTheWire();

        Assert.Equal(default, wire);
        Assert.False(wire.IsWritten);
    }

    [Fact]
    public void AMapTheLibraryShips_TravelsByItsNameAlone()
    {
        var wire = ColorMaps.Viridis.ColorMapOnTheWire();

        Assert.Equal("viridis", wire.Name);
        Assert.Null(wire.Description);
        Assert.True(wire.IsWritten);
    }

    [Fact]
    public void AMapBuiltInCode_TravelsAsItsDescriptionAlone()
    {
        var wire = new LinearColorMap("wire-ramp", [Red, Blue]).ColorMapOnTheWire();

        Assert.Null(wire.Name);
        Assert.IsType<LinearColorMapDto>(wire.Description);
        Assert.True(wire.IsWritten);
    }

    [Fact]
    public void AMapOfYourOwnTypeNobodyRegistered_IsCodeAndStaysOffTheWire()
    {
        var wire = new TestColorMap("unregistered-wire-test-map", _ => Red).ColorMapOnTheWire();

        Assert.False(wire.IsWritten);
    }

    // ---- a map is read back -------------------------------------------------------------------------------------------

    [Fact]
    public void ANameAlone_ReadsAsTheMapRegisteredUnderIt()
    {
        Assert.Same(ColorMaps.Viridis, new ColorMapWire("viridis", null).Read());
    }

    [Fact]
    public void ADescriptionAlone_ReadsAsTheMapItDescribes()
    {
        var map = new ColorMapWire(null, new ListedColorMapDto("listed-wire", [Red, Blue], null, null, null)).Read();

        Assert.Equal("listed-wire", Assert.IsType<ListedColorMap>(map).Name);
    }

    [Fact]
    public void ADescriptionWritten_BeatsAName()
    {
        var map = new ColorMapWire("viridis", new ListedColorMapDto("listed-wire", [Red, Blue], null, null, null)).Read();

        Assert.Equal("listed-wire", Assert.IsType<ListedColorMap>(map).Name);
    }

    [Fact]
    public void NothingWritten_ReadsAsNoMap()
    {
        Assert.Null(default(ColorMapWire).Read());
    }

    [Fact]
    public void ADescriptionThatCannotMakeAMap_IsRefusedByName()
    {
        var wire = new ColorMapWire(null, new LinearColorMapDto("empty", [], null, null, null, null));

        Assert.Equal("colors", Assert.Throws<ArgumentException>(() => wire.Read()).ParamName);
    }

    // ---- a colour scale: the map and the normaliser as one ------------------------------------------------------------

    [Fact]
    public void AScaleWithAMapAndANormaliser_WritesBothAndReadsBothBack()
    {
        var wire = ColorScaleWire.Of(ColorMaps.Viridis, new LogNormalizer());

        Assert.Equal("viridis", wire.Map.Name);
        Assert.IsType<LogNormalizerDto>(wire.Normalizer);
        Assert.Same(ColorMaps.Viridis, wire.ReadMap());
        Assert.IsType<LogNormalizer>(wire.ReadNormalizer());
    }

    [Fact]
    public void AScaleWithNeitherMapNorNormaliser_IsAllAbsent()
    {
        var wire = ColorScaleWire.Of(null, null);

        Assert.Equal(default, wire);
        Assert.Null(wire.ReadMap());
        Assert.Null(wire.ReadNormalizer());
    }

    [Fact]
    public void ANormaliserWrittenOutsideTheLibrary_IsCodeAndStaysOffTheWire()
    {
        Assert.Null(ColorScaleWire.Of(null, new TestNormalizer((_, _, _) => 0.5)).Normalizer);
    }
}
