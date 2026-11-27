// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="AreaSeries"/> behaviour; what it starts from and the settings it stores are held by <see cref="Serialization.RestingValueCorpusTests"/> and the drawn-property round trips.</summary>
public class AreaSeriesTests
{
    /// <summary>A second curve the fill draws, set after the fill was made, holds one value for each position, as the verb that
    /// makes the fill refuses one that does not, in its words: the setter refuses it and the fill keeps the curve it had,
    /// whichever way it lies.</summary>
    [Theory]
    [InlineData(BarOrientation.Vertical, nameof(AreaSeries.YData2), 2)]
    [InlineData(BarOrientation.Vertical, nameof(AreaSeries.YData2), 0)]
    [InlineData(BarOrientation.Horizontal, nameof(AreaSeries.XData2), 2)]
    [InlineData(BarOrientation.Horizontal, nameof(AreaSeries.XData2), 0)]
    public void ASecondCurveTheFillDrawsThatIsNotOneValuePerPosition_IsRefused(BarOrientation orientation, string property, int length)
    {
        var series = new AreaSeries([1.0, 2, 3], [2.0, 4, 3]) { Orientation = orientation };
        var setter = typeof(AreaSeries).GetProperty(property)!;

        var refused = Assert.Throws<System.Reflection.TargetInvocationException>(() => setter.SetValue(series, new double[length]));

        Assert.EndsWith(string.Invariant($"Got 3 and {length}."), Assert.IsType<ArgumentException>(refused.InnerException).Message, StringComparison.Ordinal);
        Assert.Null(setter.GetValue(series));
    }

    /// <summary>A fill that lies down draws the second curve it carries on X: one of another length than the positions keeps it
    /// standing, as a fill made lying down with that curve is refused.</summary>
    [Fact]
    public void AFillWhoseXCurveIsNotOneValuePerPosition_IsRefusedToLieDown()
    {
        var series = new AreaSeries([1.0, 2, 3], [2.0, 4, 3]) { XData2 = [9.0] };

        Assert.Throws<ArgumentException>(() => series.Orientation = BarOrientation.Horizontal);

        Assert.Equal(BarOrientation.Vertical, series.Orientation);
    }

    /// <summary>Verifies that Where predicate defaults to null.</summary>
    [Fact]
    public void AnArea_HasNoWherePredicate_UntilOneIsGiven()
    {
        var series = new AreaSeries([1.0], [2.0]);
        Assert.Null(series.Where);
    }
}
