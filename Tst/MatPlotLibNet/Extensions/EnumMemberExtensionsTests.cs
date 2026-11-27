// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Extensions;

/// <summary>A value that is no member of its enum is refused by the parameter and the value, in the sentence the site gives it; the
/// exception of a switch's last arm and of a set of flags with a bit outside the members is made by the same owner.</summary>
public sealed class EnumMemberExtensionsTests
{
    [Fact]
    public void AMember_PassesAsItIs()
    {
        Assert.Equal(DayOfWeek.Monday, DayOfWeek.Monday.ThrowIfNotDefined("day", "A week starts on one of its seven days."));
    }

    [Fact]
    public void ANonMember_IsRefusedByTheParameterTheValueAndTheSentence()
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => ((DayOfWeek)9).ThrowIfNotDefined("day", "A week starts on one of its seven days."));

        Assert.Equal("day", error.ParamName);
        Assert.Equal((DayOfWeek)9, error.ActualValue);
        Assert.StartsWith("A week starts on one of its seven days.", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TheRefusalOfAValue_ForAPlaceThatDecidesItByAnotherTest_NamesTheSame()
    {
        var arm = ((DayOfWeek)9).NotAMember("day", "Not a day.");

        Assert.Equal("day", arm.ParamName);
        Assert.Equal((DayOfWeek)9, arm.ActualValue);
        Assert.StartsWith("Not a day.", arm.Message, StringComparison.Ordinal);
    }
}
