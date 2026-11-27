// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests;

/// <summary>The generator the indicator tests draw their random series from: the same seed gives the same numbers on every platform,
/// because it uses no library maths.</summary>
public sealed class SeededNormalsTests
{
    [Fact]
    public void TheSameSeed_GivesTheSameNumbers_AndAnotherSeedGivesOthers()
    {
        var a = new SeededNormals(7);
        var b = new SeededNormals(7);
        var c = new SeededNormals(8);

        double first = a.Uniform();

        Assert.Equal(first, b.Uniform());
        Assert.NotEqual(first, c.Uniform());
    }

    [Fact]
    public void AUniform_LiesInZeroToOne_AndANormalCentresOnZeroWithUnitSpread()
    {
        var rng = new SeededNormals(11);
        var uniforms = Enumerable.Range(0, 2000).Select(_ => rng.Uniform()).ToArray();
        var normals = Enumerable.Range(0, 4000).Select(_ => rng.Normal()).ToArray();

        Assert.All(uniforms, u => Assert.InRange(u, 0.0, 1.0));
        Assert.True(Math.Abs(normals.Average()) < 0.1);
        Assert.InRange(normals.Select(x => x * x).Average(), 0.9, 1.1);
    }
}
