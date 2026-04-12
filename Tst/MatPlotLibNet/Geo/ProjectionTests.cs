// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.Projections;

namespace MatPlotLibNet.Tests.Geo;

/// <summary>Verifies the IMapProjection implementations.</summary>
public class ProjectionTests
{
    // ── Equirectangular ───────────────────────────────────────────────

    [Fact]
    public void Equirectangular_Project_OriginMapsToCenter()
    {
        var proj = MapProjections.Equirectangular();
        var (nx, ny) = proj.Project(0, 0);
        Assert.Equal(0.5, nx, 3);
        Assert.Equal(0.5, ny, 3);
    }

    [Fact]
    public void Equirectangular_Project_NorthPoleMapsToTop()
    {
        var proj = MapProjections.Equirectangular();
        var (_, ny) = proj.Project(0, 90);
        Assert.Equal(0.0, ny, 3);
    }

    [Fact]
    public void Equirectangular_Project_SouthPoleMapsToBottom()
    {
        var proj = MapProjections.Equirectangular();
        var (_, ny) = proj.Project(0, -90);
        Assert.Equal(1.0, ny, 3);
    }

    [Fact]
    public void Equirectangular_Project_DateLineMapsToEdge()
    {
        var proj = MapProjections.Equirectangular();
        var (nx, _) = proj.Project(180, 0);
        Assert.Equal(1.0, nx, 3);
    }

    [Fact]
    public void Equirectangular_Bounds_DefaultIsWholeWorld()
    {
        var proj = MapProjections.Equirectangular();
        var (lonMin, lonMax, latMin, latMax) = proj.Bounds;
        Assert.Equal(-180, lonMin);
        Assert.Equal(180, lonMax);
        Assert.Equal(-90, latMin);
        Assert.Equal(90, latMax);
    }

    // ── Mercator ─────────────────────────────────────────────────────

    [Fact]
    public void Mercator_Project_EquatorMapsToMiddle()
    {
        var proj = MapProjections.Mercator();
        var (_, ny) = proj.Project(0, 0);
        Assert.Equal(0.5, ny, 3);
    }

    [Fact]
    public void Mercator_Project_ClampsLatitudeAt85()
    {
        var proj = MapProjections.Mercator();
        var (_, nyHigh) = proj.Project(0, 90);   // clamped to 85.05
        var (_, nyClamp) = proj.Project(0, 85.0511);
        // Both should map to near the top (0.0)
        Assert.True(nyHigh < 0.05);
        Assert.True(nyClamp < 0.05);
    }

    [Fact]
    public void Mercator_Project_PrimeMeridianMapsToCenter()
    {
        var proj = MapProjections.Mercator();
        var (nx, _) = proj.Project(0, 0);
        Assert.Equal(0.5, nx, 3);
    }

    [Fact]
    public void Mercator_Bounds_DefaultIsWholeWorld()
    {
        var proj = MapProjections.Mercator();
        var (lonMin, lonMax, _, _) = proj.Bounds;
        Assert.Equal(-180, lonMin);
        Assert.Equal(180, lonMax);
    }

    // ── Factory ──────────────────────────────────────────────────────

    [Fact]
    public void MapProjections_Factory_ReturnsEquirectangular()
    {
        var proj = MapProjections.Equirectangular();
        Assert.IsType<EquirectangularProjection>(proj);
    }
}
