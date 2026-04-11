// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="ContourSeries"/> and <see cref="ContourfSeries"/> explicit level-value support.</summary>
public class ContourLevelTests
{
    private static double[,] Grid2x2 => new double[2, 2];
    private static double[] X2 => [0.0, 1.0];
    private static double[] Y2 => [0.0, 1.0];

    // ── ContourSeries ─────────────────────────────────────────────────────────

    [Fact]
    public void ContourSeries_LevelValues_DefaultsToNull()
    {
        var s = new ContourSeries(X2, Y2, Grid2x2);
        Assert.Null(s.LevelValues);
    }

    [Fact]
    public void ContourSeries_LevelValues_CanBeSet()
    {
        var s = new ContourSeries(X2, Y2, Grid2x2) { LevelValues = [0.2, 0.5, 0.8] };
        Assert.NotNull(s.LevelValues);
        Assert.Equal(3, s.LevelValues.Length);
        Assert.Equal(0.5, s.LevelValues[1]);
    }

    [Fact]
    public void ContourSeries_LevelValues_OverridesLevelsCount()
    {
        // When LevelValues is set the explicit array takes precedence over Levels count
        var s = new ContourSeries(X2, Y2, Grid2x2)
        {
            Levels = 20,
            LevelValues = [0.1, 0.5, 0.9]
        };
        Assert.Equal(3, s.LevelValues!.Length);
    }

    // ── ContourfSeries ────────────────────────────��───────────────────────────

    [Fact]
    public void ContourfSeries_LevelValues_DefaultsToNull()
    {
        var s = new ContourfSeries(X2, Y2, Grid2x2);
        Assert.Null(s.LevelValues);
    }

    [Fact]
    public void ContourfSeries_LevelValues_CanBeSet()
    {
        var s = new ContourfSeries(X2, Y2, Grid2x2) { LevelValues = [0.0, 0.25, 0.75, 1.0] };
        Assert.NotNull(s.LevelValues);
        Assert.Equal(4, s.LevelValues.Length);
    }

    [Fact]
    public void ContourfSeries_LevelValues_OverridesLevelsCount()
    {
        var s = new ContourfSeries(X2, Y2, Grid2x2)
        {
            Levels = 15,
            LevelValues = [0.1, 0.9]
        };
        Assert.Equal(2, s.LevelValues!.Length);
    }
}
