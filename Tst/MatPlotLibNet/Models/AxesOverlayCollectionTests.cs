// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Models;

/// <summary>An overlay added to an axes through its own verb appears in that overlay's collection.</summary>
public class AxesOverlayCollectionTests
{
    public static TheoryData<Action<Axes>> TheOverlayVerbs => new()
    {
        axes =>
        {
            axes.Annotate("label", 3.0, 4.0);
            Assert.Equal("label", Assert.Single(axes.Annotations).Text);
        },
        axes =>
        {
            axes.AddSignal(5.0, 100.0, SignalDirection.Buy);
            Assert.Equal(SignalDirection.Buy, Assert.Single(axes.Signals).Direction);
        },
    };

    [Theory]
    [MemberData(nameof(TheOverlayVerbs))]
    public void Axes_AnOverlayVerb_AppearsInItsCollection(Action<Axes> addOneAndCheck) => addOneAndCheck(new Axes());
}
