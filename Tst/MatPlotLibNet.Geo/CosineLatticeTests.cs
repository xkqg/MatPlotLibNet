// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.GeoJson;

namespace MatPlotLibNet.Geo.Tests;

/// <summary>The cosine series over a lattice of cells that the contiguous cartogram solves its flow on: the coefficients of the
/// values at the cell centres (the forward cosine transform with Neumann walls) and a series of cosines or sines along each
/// axis summed back at the cell centres, each held to the sum written out.</summary>
public class CosineLatticeTests
{
    private const int Nx = 8, Ny = 4;

    private static double[] Cells()
    {
        var cells = new double[Nx * Ny];
        for (int j = 0; j < Ny; j++)
        for (int i = 0; i < Nx; i++)
            cells[j * Nx + i] = 1 + (i * 7 + j * 3) % 5 + 0.125 * i * j;
        return cells;
    }

    [Theory]
    [InlineData(4, 1)]
    [InlineData(1, 2)]
    public void ALatticeOfOneRowOrColumn_GivesItsCellsBack(int nx, int ny)
    {
        // Two lines go through one transform; a lattice of one row (or column) has a line with no partner.
        var lattice = new GeoAxesExtensions.FlowCartogram.CosineLattice(nx, ny);
        double[] cells = [.. Enumerable.Range(0, nx * ny).Select(k => 1.0 + k * k % 3)];

        double[] back = [.. cells];
        lattice.ToCoefficients(back);
        lattice.ToSums(back, sineAlongX: false, sineAlongY: false);

        for (int k = 0; k < cells.Length; k++) Assert.Equal(cells[k], back[k], 1e-12);
    }

    [Fact]
    public void AConstantLattice_HasOnlyTheMeanAsCoefficient()
    {
        double[] coefficients = [.. Enumerable.Repeat(2.5, Nx * Ny)];
        new GeoAxesExtensions.FlowCartogram.CosineLattice(Nx, Ny).ToCoefficients(coefficients);

        Assert.Equal(2.5, coefficients[0], 1e-12);
        Assert.All(coefficients.Skip(1), c => Assert.Equal(0.0, c, 1e-12));
    }

    [Fact]
    public void TheCoefficients_SummedAsCosines_GiveTheCellsBack()
    {
        var lattice = new GeoAxesExtensions.FlowCartogram.CosineLattice(Nx, Ny);
        double[] cells = Cells();

        double[] back = [.. cells];
        lattice.ToCoefficients(back);
        lattice.ToSums(back, sineAlongX: false, sineAlongY: false);

        for (int k = 0; k < cells.Length; k++) Assert.Equal(cells[k], back[k], 1e-12);
    }

    [Fact]
    public void TheCoefficients_AreTheCosineSeriesOfTheCells()
    {
        // c(kx, ky) = w(kx) w(ky) / (Nx Ny) * sum of the cells times cos(pi kx (i + 1/2) / Nx) cos(pi ky (j + 1/2) / Ny),
        // w(0) = 1 and w(k > 0) = 2: the series that gives the cells back at their centres.
        double[] cells = Cells();

        double[] coefficients = [.. cells];
        new GeoAxesExtensions.FlowCartogram.CosineLattice(Nx, Ny).ToCoefficients(coefficients);

        for (int ky = 0; ky < Ny; ky++)
        for (int kx = 0; kx < Nx; kx++)
        {
            double sum = 0;
            for (int j = 0; j < Ny; j++)
            for (int i = 0; i < Nx; i++)
                sum += cells[j * Nx + i] * Math.Cos(Math.PI * kx * (i + 0.5) / Nx) * Math.Cos(Math.PI * ky * (j + 0.5) / Ny);
            double expected = sum * (kx == 0 ? 1 : 2) * (ky == 0 ? 1 : 2) / (Nx * Ny);
            Assert.Equal(expected, coefficients[ky * Nx + kx], 1e-12);
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void ASeries_IsSummedWithACosineOrASineAlongEachAxis(bool sineAlongX, bool sineAlongY)
    {
        double[] coefficients = Cells();

        double[] sum = [.. coefficients];
        new GeoAxesExtensions.FlowCartogram.CosineLattice(Nx, Ny).ToSums(sum, sineAlongX, sineAlongY);

        for (int j = 0; j < Ny; j++)
        for (int i = 0; i < Nx; i++)
        {
            double expected = 0;
            for (int ky = 0; ky < Ny; ky++)
            for (int kx = 0; kx < Nx; kx++)
            {
                double alongX = sineAlongX ? Math.Sin(Math.PI * kx * (i + 0.5) / Nx) : Math.Cos(Math.PI * kx * (i + 0.5) / Nx);
                double alongY = sineAlongY ? Math.Sin(Math.PI * ky * (j + 0.5) / Ny) : Math.Cos(Math.PI * ky * (j + 0.5) / Ny);
                expected += coefficients[ky * Nx + kx] * alongX * alongY;
            }

            Assert.Equal(expected, sum[j * Nx + i], 1e-10);
        }
    }
}
