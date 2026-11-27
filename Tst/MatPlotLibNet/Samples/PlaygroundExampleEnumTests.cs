// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Playground;

namespace MatPlotLibNet.Tests.Samples;

/// <summary>
/// Phase N.1 — the Playground now uses a <see cref="PlaygroundExample"/> enum
/// instead of free-form strings. These tests pin the enum-vs-dispatcher
/// relationship so the two can never drift — adding a new enum value without
/// a builder fails the build, and adding a builder without an enum value is
/// impossible (type-checked).
/// </summary>
public class PlaygroundExampleEnumTests
{
    [Fact]
    public void PlaygroundExample_EnumIsDefined()
    {
        var values = Enum.GetValues<PlaygroundExample>();
        Assert.True(values.Length >= 16,
            $"expected at least 16 playground examples; got {values.Length}");
    }

    [Fact]
    public void PlaygroundExamples_EveryEnumValue_HasBuilder()
    {
        // If someone adds a new PlaygroundExample member without a corresponding
        // builder, this test fires the canary.
        var defaults = new PlaygroundOptions { Title = "Test" };
        foreach (var example in Enum.GetValues<PlaygroundExample>())
        {
            var (figure, code) = PlaygroundExamples.Build(example, defaults);
            Assert.NotNull(figure);
            Assert.False(string.IsNullOrWhiteSpace(code),
                $"{example}: code snippet must not be empty");
        }
    }

    [Fact]
    public void Build_UnregisteredEnumValue_ThrowsArgumentException()
    {
        var opts = new PlaygroundOptions { Title = "Test" };
        Assert.Throws<ArgumentException>(() =>
            PlaygroundExamples.Build((PlaygroundExample)999, opts));
    }

    [Fact]
    public void DisplayName_RoundTrips()
    {
        // UI shows human-readable labels ("Line Chart"). The mapping must be
        // a single source of truth (attribute-driven), not duplicated.
        foreach (var example in Enum.GetValues<PlaygroundExample>())
        {
            string display = example.DisplayName();
            Assert.False(string.IsNullOrWhiteSpace(display),
                $"{example}: display name must not be blank");
            var parsed = PlaygroundExampleExtensions.FromDisplayName(display);
            Assert.Equal(example, parsed);
        }
    }

    /// <summary>DisplayName fallback — enum value with no [Description] attr → ToString().</summary>
    [Fact]
    public void DisplayName_NoDescriptionAttribute_FallsBackToEnumName()
    {
        var unnamed = (PlaygroundExample)999;
        Assert.Equal("999", unnamed.DisplayName());
    }

    /// <summary>FromDisplayName null arm — unknown name returns null.</summary>
    [Fact]
    public void FromDisplayName_UnknownName_ReturnsNull()
    {
        Assert.Null(PlaygroundExampleExtensions.FromDisplayName("DoesNotExist"));
    }
}
