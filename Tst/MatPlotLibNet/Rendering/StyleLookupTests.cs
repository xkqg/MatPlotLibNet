// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>What the four lookups that turn a style into the object that draws it (an arrow head, a callout outline, a connection path, a
/// marker shape) promise alike: a style is resolved to one object, the same every time it is asked for.</summary>
public class StyleLookupTests
{
    private static void TheSameEveryTime<TStyle, TShape>(Func<TStyle, TShape> of) where TStyle : struct, Enum where TShape : class
    {
        foreach (TStyle style in Enum.GetValues<TStyle>())
            Assert.Same(of(style), of(style));
    }

    public static TheoryData<Action> Lookups => new()
    {
        () => TheSameEveryTime<ArrowStyle, ArrowHead>(ArrowHead.Of),
        () => TheSameEveryTime<BoxStyle, CalloutShape>(CalloutShape.Of),
        () => TheSameEveryTime<ConnectionStyle, ConnectionPath>(ConnectionPath.Of),
        () => TheSameEveryTime<MarkerStyle, MarkerShape>(MarkerShape.Of),
    };

    [Theory]
    [MemberData(nameof(Lookups))]
    public void AStyleIsResolvedToOneObject_TheSameEveryTimeItIsAskedFor(Action check) => check();
}
