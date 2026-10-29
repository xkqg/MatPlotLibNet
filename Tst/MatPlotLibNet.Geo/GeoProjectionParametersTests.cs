// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.Projections;

namespace MatPlotLibNet.Geo.Tests;

/// <summary>What the package knows of a projection beyond <see cref="IGeoProjection"/>: the numbers it was made with (by their
/// PROJ names), whether another projection is the same one, how it reads in a message, and the projection a name and its numbers
/// make again.</summary>
public class GeoProjectionParametersTests
{
    /// <summary>A projection from outside the package: the package knows nothing of its numbers.</summary>
    private sealed class ForeignProjection : IGeoProjection
    {
        public string Name => "Foreign";

        public ProjectedPoint Forward(double latitude, double longitude) => new(longitude, latitude);

        public GeoCoordinate? Inverse(double x, double y) => new(y, x);

        public GeoBounds Bounds => new(-180, 180, -90, 90);
    }

    public static readonly string[] Names =
    [
        "PlateCarree", "Mercator", "Robinson", "Mollweide", "Sinusoidal", "NaturalEarth", "EqualEarth", "Orthographic",
        "LambertConformal", "AlbersEqualArea", "AzimuthalEquidistant", "Stereographic", "TransverseMercator",
    ];

    public static TheoryData<string> ProjectionNames => [.. Names];

    // ---- the numbers a projection was made with --------------------------------------------------------------------------

    [Fact]
    public void AProjectionThatTakesNumbers_NamesThemByTheirProjNames()
    {
        var parameters = new Orthographic(10, 20).ParametersOf();

        Assert.Equal(10, parameters["lat_0"]);
        Assert.Equal(20, parameters["lon_0"]);
        Assert.Equal(2, parameters.Count);
    }

    [Theory]
    [InlineData("PlateCarree")]
    [InlineData("Robinson")]
    public void AProjectionThatTakesNone_HasNone(string name) =>
        Assert.Empty(GeoProjectionExtensions.Make(name, new Dictionary<string, double>())!.ParametersOf());

    [Fact]
    public void AProjectionThePackageDoesNotKnow_HasNone() =>
        Assert.Empty(new ForeignProjection().ParametersOf());

    // ---- is it the same projection ---------------------------------------------------------------------------------------

    [Fact]
    public void AProjection_IsTheSameAsItself()
    {
        var projection = new ForeignProjection();

        Assert.True(projection.IsSameProjectionAs(projection));
    }

    [Fact]
    public void TheSameKindWithTheSameNumbers_IsTheSameProjection() =>
        Assert.True(new Orthographic(10, 20).IsSameProjectionAs(new Orthographic(10, 20)));

    [Fact]
    public void TheSameKindWithOtherNumbers_IsAnotherProjection()
    {
        Assert.False(new Orthographic(10, 20).IsSameProjectionAs(new Orthographic(10, 21)));
        Assert.False(new Orthographic(10, 20).IsSameProjectionAs(new Orthographic(11, 20)));
    }

    [Fact]
    public void AnotherKind_IsAnotherProjection() =>
        Assert.False(new Orthographic().IsSameProjectionAs(new Stereographic()));

    [Fact]
    public void TheSameKindThatTakesNoNumbers_IsTheSameProjection() =>
        Assert.True(new Robinson().IsSameProjectionAs(new Robinson()));

    [Fact]
    public void AProjectionFromOutsideThePackage_IsTheSameAsItselfAndAsNothingElse() =>
        Assert.False(new ForeignProjection().IsSameProjectionAs(new ForeignProjection()));

    // ---- how it reads in a message ---------------------------------------------------------------------------------------

    [Fact]
    public void AProjectionThatTakesNone_ReadsAsItsName() =>
        Assert.Equal("Robinson", new Robinson().Described());

    [Fact]
    public void AProjectionThatTakesNumbers_ReadsWithThem() =>
        Assert.Equal("Orthographic(lat_0=10, lon_0=20)", new Orthographic(10, 20).Described());

    // ---- the projection a name and its numbers make again ----------------------------------------------------------------

    [Theory]
    [MemberData(nameof(ProjectionNames))]
    public void EveryNameOfThePackage_MakesTheProjectionOfThatName_EachTimeAsTheSame(string name)
    {
        var first = GeoProjectionExtensions.Make(name, new Dictionary<string, double>());
        var second = GeoProjectionExtensions.Make(name, new Dictionary<string, double>());

        Assert.NotNull(first);
        Assert.Equal(name, first.Name);
        Assert.True(first.IsSameProjectionAs(second!));
    }

    [Fact]
    public void ANumberThatIsLeftOut_TakesTheProjectionsOwnDefault()
    {
        var made = GeoProjectionExtensions.Make("Orthographic", new Dictionary<string, double> { ["lat_0"] = 10 })!;

        Assert.True(made.IsSameProjectionAs(new Orthographic(10, 0)));
    }

    [Fact]
    public void TheNumbersGiven_AreTheNumbersTheProjectionIsMadeWith()
    {
        var made = GeoProjectionExtensions.Make("LambertConformal",
            new Dictionary<string, double> { ["lat_1"] = 35, ["lat_2"] = 65, ["lon_0"] = 10, ["lat_0"] = 52 })!;

        Assert.True(made.IsSameProjectionAs(new LambertConformal(35, 65, 10, 52)));
    }

    [Fact]
    public void AnUnknownName_MakesNothing() =>
        Assert.Null(GeoProjectionExtensions.Make("Nowhere", new Dictionary<string, double>()));
}
