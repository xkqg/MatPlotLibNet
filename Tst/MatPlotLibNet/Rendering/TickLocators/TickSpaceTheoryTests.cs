// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.TickLocators;

/// <summary>The theory every locator that reads the room along an axis is held to (matplotlib's <c>get_tick_space</c>): a room that is
/// no number is no axis to ask, and a label of no size takes no room, so both lay the ticks the most room lays, as matplotlib's locators
/// ask for nine with no axis and its <c>get_tick_space</c> answers a label of no size with <c>2**31 - 1</c>. A new locator that reads the
/// room joins <see cref="Locators"/>.</summary>
public class TickSpaceTheoryTests
{
    [Fact]
    public void ALabelOfNoSize_TakesNoRoom()
    {
        // matplotlib's get_tick_space: "if size > 0: return int(np.floor(length / size)) else: return 2**31 - 1".
        Assert.Equal(int.MaxValue, MaxNLocator.LabelsAlong(100, 0, alongX: true));
        Assert.Equal(int.MaxValue, MaxNLocator.LabelsAlong(0, 0, alongX: false));
        Assert.Equal(16, MaxNLocator.LabelsAlong(100, 2, alongX: true));
    }

    /// <summary>Every locator that reads the room, laying its ticks for a given room.</summary>
    public static TheoryData<string> Locators { get; } =
    [
        "the library's ruler", "a log axis", "a log axis' minor ticks", "a logit axis", "a logit axis too narrow for its ideal ticks",
        "the default style's ladder",
    ];

    private static double[] TicksOf(string locator, double tickSpace) => locator switch
    {
        "the library's ruler" => MaxNLocator.ForTickSpace(tickSpace).Locate(0, 10),
        "a log axis" => ((ITickSpaceLocator)new LogLocator()).LocateWithSpace(1, 1e30, tickSpace, Theme.MatplotlibV2),
        "a log axis' minor ticks" => LogLocator.MinorsOver(new Range1D(1, 1e5), [1, 10, 100, 1000, 10000, 100000], tickSpace, Theme.MatplotlibV2),
        "a logit axis" => new LogitLocator().LocateWithSpace(1e-12, 1 - 1e-12, tickSpace, Theme.MatplotlibV2),
        "a logit axis too narrow for its ideal ticks" => new LogitLocator().LocateWithSpace(0.3, 0.7, tickSpace, Theme.MatplotlibV2),
        _ => TickLadder.DefaultStyle.ForTickSpace(tickSpace).TicksIn(new Range1D(0, 10), roundNumbers: false),
    };

    /// <summary>The theory every locator that reads the room is held to: a room that is no number, and a room without end, lay the
    /// ticks the most room lays.</summary>
    [Theory]
    [MemberData(nameof(Locators))]
    public void ARoomThatIsNoNumber_OrWithoutEnd_LaysTheTicksOfTheMostRoom(string locator)
    {
        double[] most = TicksOf(locator, 1e6);

        Assert.NotEmpty(TicksOf(locator, 9));
        Assert.Equal(most, TicksOf(locator, double.NaN));
        Assert.Equal(most, TicksOf(locator, double.PositiveInfinity));
    }
}
