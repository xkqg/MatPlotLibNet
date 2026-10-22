// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>A JSON document leaves a value out when it is at rest, so a figure that never touches a property writes the
/// bytes it wrote before the property reached the JSON, and a document written before then reads as the default.</summary>
public class RestingValueExtensionsTests
{
    [Fact]
    public void AValueAtRest_IsLeftOut() => Assert.Null(6.0.OrNullAtRest(6.0));

    [Fact]
    public void AValueAwayFromRest_IsWritten() => Assert.Equal(4.5, 4.5.OrNullAtRest(6.0));

    [Fact]
    public void AnEnumAtRest_IsLeftOut_AndOneAwayFromRestIsWritten()
    {
        Assert.Null(MarkerStyle.Circle.OrNullAtRest(MarkerStyle.Circle));
        Assert.Equal(MarkerStyle.Diamond, MarkerStyle.Diamond.OrNullAtRest(MarkerStyle.Circle));
    }

    [Fact]
    public void AFlagThatRestsTrue_IsWrittenOnlyWhenFalse()
    {
        Assert.Null(true.OrNullAtRest(true));
        Assert.Equal(false, false.OrNullAtRest(true));
    }

    /// <summary>A value that may itself be absent rests at whatever the model is created with, absent included: a
    /// margin that rests unset is left out while unset and written once set; a colour that rests set is left out while
    /// it is that colour.</summary>
    [Fact]
    public void AnOptionalValue_IsLeftOutAtItsRest_WhetherThatRestIsSetOrNot()
    {
        double? unset = null;
        Color? grey = Color.FromHex("#808080");

        Assert.Null(unset.OrNullAtRest(null));
        Assert.Equal(0.1, ((double?)0.1).OrNullAtRest(null));
        Assert.Null(grey.OrNullAtRest(Color.FromHex("#808080")));
        Assert.Equal(Color.FromHex("#FFFFFF"), ((Color?)Color.FromHex("#FFFFFF")).OrNullAtRest(grey));
    }

    [Fact]
    public void AText_IsLeftOutAtItsRest_AndWrittenAwayFromIt()
    {
        Assert.Null("sans-serif".OrNullAtRest("sans-serif"));
        Assert.Null(((string?)null).OrNullAtRest(null));
        Assert.Equal("serif", "serif".OrNullAtRest("sans-serif"));
    }
}
