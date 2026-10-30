// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>The Kraskov-Stögbauer-Grassberger estimator of conditional mutual information in the Frenzel-Pompe form,
/// <c>ψ(k) - ⟨ψ(n_xz + 1) + ψ(n_yz + 1) - ψ(n_z + 1)⟩</c>, with the maximum norm and counts taken strictly inside the distance to the
/// k-th neighbour in the joint space.</summary>
public sealed class KraskovEstimatorTests
{
    private static double[][] One(params double[] column) => [column];

    private static readonly double[] Ramp = [0, 1, 2, 3, 4];

    /// <summary>x = y = 0..4, k = 1, nothing conditioned on (a constant z): every neighbour is exactly 1 away in x, y and the joint
    /// space, so with counts strictly below that distance every n_x and n_y is 0 and n_z is 4, and the value is ψ(5) - ψ(1) = 25/12.
    /// A count that included the neighbour on the edge (≤) would give another value.</summary>
    [Fact]
    public void TheHandCase_IsTwentyFiveOverTwelve_WithAConstantCondition()
    {
        double value = KraskovEstimator.ConditionalMutualInformation(One(Ramp), One(Ramp), One(0, 0, 0, 0, 0), neighbours: 1);

        Assert.Equal(25.0 / 12.0, value, 1e-12);
    }

    /// <summary>With no conditioning columns at all the estimate is the same as with a constant one.</summary>
    [Fact]
    public void WithNoConditionColumns_ItIsTheSameAsWithAConstantOne()
    {
        double none = KraskovEstimator.ConditionalMutualInformation(One(Ramp), One(Ramp), [], neighbours: 1);

        Assert.Equal(25.0 / 12.0, none, 1e-12);
    }

    /// <summary>Shifting any variable, and scaling all of them by the same factor, changes no distance order and so no count. (Scaling
    /// the variables by different factors does change them: the maximum norm weighs the columns as they come, which is why the
    /// transfer entropy standardises every variable first.)</summary>
    [Fact]
    public void ShiftedVariables_AndAllScaledAlike_GiveTheSameValue()
    {
        // Values that are whole multiples of 1/64, so that scaling by 4 and shifting by a whole number is exact and every tie stays a tie.
        double[] x = [19, 109, 13, 326, 141, 282, 250, 51, 166, 70, 307, 211];
        double[] y = [70, 26, 141, 237, 58, 320, 179, 122, 262, 13, 211, 154];
        double[] z = [32, 6, 58, 19, 45, 13, 51, 26, 38, 0, 64, 29];
        x = [.. x.Select(v => v / 64)];
        y = [.. y.Select(v => v / 64)];
        z = [.. z.Select(v => v / 64)];

        double plain = KraskovEstimator.ConditionalMutualInformation(One(x), One(y), One(z), neighbours: 3);
        double changed = KraskovEstimator.ConditionalMutualInformation(
            One([.. x.Select(v => 4 * v + 100)]), One([.. y.Select(v => 4 * v - 7)]), One([.. z.Select(v => 4 * v)]), neighbours: 3);

        Assert.Equal(plain, changed, 12);
    }

    /// <summary>A variable of two columns is measured in the maximum norm over both of them.</summary>
    [Fact]
    public void AVariableOfTwoColumns_IsMeasuredInTheMaximumNorm()
    {
        // Two columns whose larger difference is always the second: only it matters, so the first can be anything.
        double[] big = [0, 10, 20, 30, 40, 50];
        double[] small = [0, 0.1, 0.2, 0.1, 0.0, 0.1];
        double[] y = [3, 1, 4, 1, 5, 9];

        double both = KraskovEstimator.ConditionalMutualInformation([small, big], One(y), [], neighbours: 2);
        double onlyBig = KraskovEstimator.ConditionalMutualInformation(One(big), One(y), [], neighbours: 2);

        Assert.Equal(onlyBig, both, 12);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void FewerThanOneNeighbour_IsRefused(int neighbours) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => KraskovEstimator.ConditionalMutualInformation(One(Ramp), One(Ramp), [], neighbours));

    [Fact]
    public void AsManyNeighboursAsThereAreOtherSamples_IsRefused() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => KraskovEstimator.ConditionalMutualInformation(One(Ramp), One(Ramp), [], neighbours: 5));

    [Fact]
    public void ColumnsOfDifferentLengths_AreRefused() =>
        Assert.Throws<ArgumentException>(() => KraskovEstimator.ConditionalMutualInformation(One(Ramp), One(0, 1, 2), [], neighbours: 1));

    [Fact]
    public void NoColumnsForAVariable_IsRefused() =>
        Assert.Throws<ArgumentException>(() => KraskovEstimator.ConditionalMutualInformation([], One(Ramp), [], neighbours: 1));
}
