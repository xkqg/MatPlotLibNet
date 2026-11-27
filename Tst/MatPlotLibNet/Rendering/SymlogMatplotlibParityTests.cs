// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The symmetric-log transform is matplotlib's <c>SymmetricalLogTransform</c>, one rule for a symlog axis, its browser view and
/// the symmetric-log normaliser: within the threshold a value is stretched by <c>linscale_adj = linscale / (1 - 1/base)</c>, beyond
/// it <c>sign * linthresh * (linscale_adj + log_base(|v| / linthresh))</c>; an axis takes matplotlib's defaults (base 10, linscale 1).
/// The numbers are matplotlib 3.11.2's, read with python:
/// <code>
/// SymmetricalLogTransform(10, 1.0, 1.0).transform([0.5, 1, 10, 500, -5])
///   -> [0.5555555555555556, 1.1111111111111112, 2.111111111111111, 3.8100811154471295, -1.81008111544713]
/// SymmetricalLogTransform(10, 2.0, 1.0).transform([0.5, 2, 10, 500, -5])
///   -> [0.5555555555555556, 2.2222222222222223, 3.62016223089426, 7.018102239566296, -3.0181022395662973]
/// SymLogNorm(linthresh=1.0, linscale=1.0, vmin=-100, vmax=1000, base=10)([-10, 0, 0.5, 5, 500])
///   -> [0.13846153846153847, 0.43076923076923085, 0.5076923076923078, 0.681395846754218, 0.9583189236772949]
/// SymLogNorm(linthresh=0.5, linscale=2.0, vmin=-10, vmax=100, base=2)([-3, 0, 0.25, 4, 50])
///   -> [0.08699711313121619, 0.41680947646421035, 0.5169808482212627, 0.7674092776138937, 0.9499143141214738]
/// </code>
/// The linear stretch was taken as 1, so a symlog axis drew its linear part a ninth narrower than matplotlib's, and the normaliser
/// coloured by an unstretched linear part too.</summary>
public class SymlogMatplotlibParityTests
{
    [Theory]
    [InlineData(1.0, 0.5, 0.5555555555555556)]
    [InlineData(1.0, 1.0, 1.1111111111111112)]
    [InlineData(1.0, 10.0, 2.111111111111111)]
    [InlineData(1.0, 500.0, 3.8100811154471295)]
    [InlineData(1.0, -5.0, -1.81008111544713)]
    [InlineData(2.0, 0.5, 0.5555555555555556)]
    [InlineData(2.0, 2.0, 2.2222222222222223)]
    [InlineData(2.0, 10.0, 3.62016223089426)]
    [InlineData(2.0, 500.0, 7.018102239566296)]
    [InlineData(2.0, -5.0, -3.0181022395662973)]
    public void ASymlogAxis_MapsAValueAsMatplotlibDoes(double linthresh, double value, double expected)
    {
        Assert.Equal(expected, SymlogTransform.Forward(value, linthresh), Math.Abs(expected) * 1e-14);
        Assert.Equal(expected, ScaleRule.SymLogAt(linthresh).Forward(value), Math.Abs(expected) * 1e-14);
    }

    [Theory]
    [InlineData(1.0, 0.5)]
    [InlineData(1.0, -1.0)]
    [InlineData(1.0, 500.0)]
    [InlineData(2.0, 2.0)]
    [InlineData(2.0, -10.0)]
    [InlineData(0.25, 1e6)]
    public void TheInverse_UndoesTheMapping_OnBothSidesOfTheThreshold(double linthresh, double value) =>
        Assert.Equal(value, SymlogTransform.Inverse(SymlogTransform.Forward(value, linthresh), linthresh), Math.Abs(value) * 1e-12);

    [Theory]
    [InlineData(1.0, 1.0, 10.0, -100.0, 1000.0, new[] { -10.0, 0, 0.5, 5, 500 },
        new[] { 0.13846153846153847, 0.43076923076923085, 0.5076923076923078, 0.681395846754218, 0.9583189236772949 })]
    [InlineData(0.5, 2.0, 2.0, -10.0, 100.0, new[] { -3.0, 0, 0.25, 4, 50 },
        new[] { 0.08699711313121619, 0.41680947646421035, 0.5169808482212627, 0.7674092776138937, 0.9499143141214738 })]
    public void TheSymlogNormaliser_ColoursAsMatplotlibsSymLogNorm(double linthresh, double linscale, double @base, double min, double max,
        double[] values, double[] expected)
    {
        var norm = new SymLogNormalizer(linthresh, @base, linscale);

        for (int i = 0; i < values.Length; i++)
            Assert.Equal(expected[i], norm.Normalize(values[i], min, max), 1e-12);
    }
}
