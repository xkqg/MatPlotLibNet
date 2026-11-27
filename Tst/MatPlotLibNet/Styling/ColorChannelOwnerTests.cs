// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>A value between 0 and 1 and the byte of a colour channel it becomes are worked out in one place each: the unit-interval
/// hold (<c>Saturate</c>), a byte scaled by a share and rounded to the nearest step (<c>Scaled</c>), the same scaled by cutting
/// the fraction off (<c>ScaledTruncating</c>, matplotlib's <c>(x * 255).astype(uint8)</c>), a colour at an opacity
/// (<c>WithOpacity</c>) and a blend of two colours (<c>Color.Lerp</c>). Each site that spelled one of these by hand kept its rounding,
/// so both roundings are named, and a site says which one it has.</summary>
public class ColorChannelOwnerTests
{
    /// <summary>A colour desaturated as seaborn's <c>utils.desaturate(color, prop)</c> does it: to HLS, the saturation times the share,
    /// back to RGB, each channel to the nearest step as Agg takes a float colour. Printed with python (seaborn 0.13.2):
    /// <c>desaturate('b', .75)</c> = (31.875, 31.875, 223.125), <c>desaturate('#1f77b4', .75)</c> = (49.625, 115.625, 161.375),
    /// <c>desaturate('#ff7f0e', .75)</c> = (224.875, 128.875, 44.125), a grey stays grey. A share of 1 is the colour itself.</summary>
    [Theory]
    [InlineData("#0000FF", 0.75, "#2020DF")]
    [InlineData("#1F77B4", 0.75, "#3274A1")]
    [InlineData("#FF7F0E", 0.75, "#E1812C")]
    [InlineData("#808080", 0.75, "#808080")]
    [InlineData("#1F77B4", 1.0, "#1F77B4")]
    [InlineData("#1F77B4", 0.0, "#6A6A6A")]
    [InlineData("#00FF00", 0.5, "#40BF40")]
    public void Desaturated_IsSeabornsDesaturate(string colour, double share, string expected)
    {
        Assert.Equal(expected, Color.FromHex(colour).Desaturated(share).ToHex());
    }

    [Fact]
    public void Desaturated_KeepsTheOpacity()
    {
        Assert.Equal(128, Color.FromHex("#1F77B480").Desaturated(0.75).A);
    }

    [Theory]
    [InlineData(-0.5, 0.0)]
    [InlineData(0.0, 0.0)]
    [InlineData(0.25, 0.25)]
    [InlineData(1.0, 1.0)]
    [InlineData(1.5, 1.0)]
    [InlineData(double.NegativeInfinity, 0.0)]
    [InlineData(double.PositiveInfinity, 1.0)]
    public void Saturate_HoldsAValueBetweenZeroAndOne(double value, double expected)
    {
        Assert.Equal(expected, value.Saturate());
    }

    /// <summary>The hold is <c>Math.Clamp</c>, which passes a missing value on: a cell that is not a number stays one.</summary>
    [Fact]
    public void Saturate_LeavesAMissingValueMissing()
    {
        Assert.True(double.IsNaN(double.NaN.Saturate()));
    }

    [Theory]
    [InlineData(200, 0.5, 100)]
    [InlineData(255, 0.5, 128)]   // 127.5 rounds to the even step above
    [InlineData(101, 0.5, 50)]    // 50.5 rounds to the even step below
    [InlineData(1, 0.5, 0)]
    [InlineData(255, 0.85, 217)]  // 216.75
    [InlineData(255, 1.5, 255)]   // a share above 1 is held at 1
    [InlineData(255, -0.5, 0)]    // a share below 0 is held at 0
    [InlineData(0, 0.7, 0)]
    public void Scaled_RoundsToTheNearestStep(int channel, double share, int expected)
    {
        Assert.Equal((byte)expected, ((byte)channel).Scaled(share));
    }

    [Theory]
    [InlineData(255, 0.5, 127)]
    [InlineData(101, 0.5, 50)]
    [InlineData(1, 0.5, 0)]
    [InlineData(255, 0.85, 216)]  // 216.75: the fraction is cut off where Scaled rounds up
    [InlineData(255, 1.5, 255)]
    [InlineData(255, -0.5, 0)]
    public void ScaledTruncating_CutsTheFractionOff(int channel, double share, int expected)
    {
        Assert.Equal((byte)expected, ((byte)channel).ScaledTruncating(share));
    }

    /// <summary>A unit opacity is the scale of the full channel: <c>byte.MaxValue.Scaled(opacity)</c> is the byte of an opacity.</summary>
    [Theory]
    [InlineData(0.0, 0)]
    [InlineData(0.8, 204)]
    [InlineData(0.5, 128)]
    [InlineData(1.0, 255)]
    [InlineData(1.5, 255)]
    [InlineData(-0.5, 0)]
    public void WithOpacity_ReplacesTheOpacityOfTheColour_AndKeepsItsChannels(double opacity, int expected)
    {
        var drawn = new Color(10, 20, 30, 77).WithOpacity(opacity);

        Assert.Equal(new Color(10, 20, 30, (byte)expected), drawn);
    }

    [Theory]
    [InlineData(0.0, 0, 0, 0, 0)]
    [InlineData(1.0, 200, 100, 50, 25)]
    [InlineData(0.5, 100, 50, 25, 12)]   // 12.5 rounds to the even step below
    public void Lerp_BlendsEveryChannelIncludingTheOpacity_RoundingToTheNearestStep(double t, int r, int g, int b, int a)
    {
        var blended = Color.Lerp(new Color(0, 0, 0, 0), new Color(200, 100, 50, 25), t);

        Assert.Equal(new Color((byte)r, (byte)g, (byte)b, (byte)a), blended);
    }

    /// <summary>A channel a share of the way to another, rounded to the nearest step: the one blend of two bytes, which a blend of two
    /// colours (<c>Color.Lerp</c>) and a colour laid over its backdrop (<c>CompositedOver</c>) both read.</summary>
    [Theory]
    [InlineData(0, 255, 0.5, 128)]    // 127.5 rounds to the even step above
    [InlineData(10, 20, 0.25, 12)]    // 12.5 rounds to the even step below
    [InlineData(200, 100, 0.3, 170)]
    [InlineData(40, 90, 0.0, 40)]
    [InlineData(40, 90, 1.0, 90)]
    public void Toward_IsTheStepAShareOfTheWayToAnotherChannel(int from, int to, double share, int expected)
    {
        Assert.Equal((byte)expected, ((byte)from).Toward((byte)to, share));
    }

    /// <summary>A colour laid over its backdrop is each backdrop channel its opacity's share of the way to the colour's, opaque.</summary>
    [Fact]
    public void CompositedOver_IsEachBackdropChannelTowardTheColour()
    {
        var over = new Color(201, 97, 33, 140);
        var backdrop = new Color(12, 250, 128);
        double share = over.A.Share;

        Assert.Equal(new Color(backdrop.R.Toward(over.R, share), backdrop.G.Toward(over.G, share), backdrop.B.Toward(over.B, share)),
            over.CompositedOver(backdrop));
    }

    /// <summary>The colour map's own blend, read through the owner: halfway between two stops is the rounded mean of each channel.</summary>
    [Fact]
    public void ALinearMap_BlendsItsStopsByTheOwner()
    {
        var map = new MatPlotLibNet.Styling.ColorMaps.LinearColorMap("two", [new Color(0, 0, 0), new Color(255, 255, 255)]);

        Assert.Equal(Color.Lerp(new Color(0, 0, 0), new Color(255, 255, 255), 0.25), map.GetColor(0.25));
    }

    /// <summary>A byte read as a share of the full channel: the other way of <c>Scaled</c>, the one division by 255.</summary>
    [Theory]
    [InlineData(0, 0.0)]
    [InlineData(51, 0.2)]
    [InlineData(255, 1.0)]
    public void Share_IsTheByteAsAShareOfTheFullChannel(int channel, double expected)
    {
        Assert.Equal(expected, ((byte)channel).Share);
        Assert.Equal((byte)channel, byte.MaxValue.Scaled(((byte)channel).Share));
    }

    /// <summary>A colour of none is one of no opacity, whatever its channels: matplotlib's <c>'none'</c>.</summary>
    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(255, false)]
    public void IsNone_IsAColourOfNoOpacity(int alpha, bool expected)
    {
        Assert.Equal(expected, new Color(10, 20, 30, (byte)alpha).IsNone);
    }

    /// <summary>A series' opacity below full replaces the colour's own (a colour of none stays none); at the resting opacity 1 the colour
    /// keeps an alpha of its own.</summary>
    [Fact]
    public void WithOpacityBelowFull_ReplacesBelowFull_AndKeepsTheColourAtFull()
    {
        var translucent = new Color(10, 20, 30, 77);

        Assert.Equal(translucent.WithAlpha(128), translucent.WithOpacityBelowFull(0.5));
        Assert.Equal(translucent, translucent.WithOpacityBelowFull(1.0));
        Assert.Equal(Colors.Transparent, Colors.Transparent.WithOpacityBelowFull(0.5));
    }
}
