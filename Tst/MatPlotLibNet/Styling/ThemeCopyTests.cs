// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>A theme copied under a new name (what <see cref="ThemeBuilder"/>, a style sheet laid over a theme and the figure
/// JSON all do) keeps every setting of the theme it copies. The test reads the settings off the type, so a setting added to
/// <see cref="Theme"/> that the copy forgets turns it red by name, without anyone extending a list here.</summary>
public class ThemeCopyTests
{
    private static readonly PropertyInfo[] Settings = typeof(Theme)
        .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        .Where(p => p.CanWrite)
        .ToArray();

    [Fact]
    public void ACopy_KeepsEverySettingOfTheThemeItCopies()
    {
        var probe = Probe();

        var copy = new Theme("copy", probe);

        Assert.Equal("copy", copy.Name);
        Assert.All(Settings, p => Assert.True(Equals(p.GetValue(copy), p.GetValue(probe)), $"the copy lost {p.Name}"));
    }

    [Fact]
    public void TheProbe_DiffersFromThePlainThemeInEverySetting()
    {
        var probe = Probe();

        Assert.All(Settings, p => Assert.False(Equals(p.GetValue(probe), p.GetValue(Theme.Default)),
            $"no value for {p.Name} that differs from the plain theme's: give its type a sample in {nameof(Sample)}"));
    }

    /// <summary>The plain theme with every setting changed: to the classic theme's value, else the v2 theme's, else a sample
    /// of its type, whichever first differs from the plain theme's.</summary>
    private static Theme Probe()
    {
        var probe = new Theme("probe", Theme.Default);
        foreach (var p in Settings)
        {
            object? plain = p.GetValue(Theme.Default);
            object? value = new[] { p.GetValue(Theme.MatplotlibClassic), p.GetValue(Theme.MatplotlibV2), Sample(p.PropertyType, plain) }
                .FirstOrDefault(v => !Equals(v, plain));
            p.SetValue(probe, value ?? plain);
        }

        return probe;
    }

    /// <summary>A value of <paramref name="type"/> that differs from <paramref name="plain"/>, for the settings no built-in
    /// theme changes.</summary>
    private static object? Sample(Type type, object? plain)
    {
        var bare = Nullable.GetUnderlyingType(type) ?? type;
        if (bare == typeof(bool)) return !(plain is true);
        if (bare == typeof(double)) return (plain is double d ? d : 0) + 0.125;
        if (bare == typeof(int)) return (plain is int i ? i : 0) + 7;
        if (bare == typeof(Color)) return Color.FromHex("#123456");
        if (bare == typeof(Color[])) return new[] { Color.FromHex("#123456") };
        if (bare == typeof(PropCycler)) return new PropCycler([Colors.Red], [LineStyle.Dashed], [MarkerStyle.Circle], [2.0]);
        if (bare == typeof(SubPlotSpacing)) return SubPlotSpacing.FromFractions(left: 0.2, right: 0.2, top: 0.2, bottom: 0.2);
        if (bare == typeof(AlarmPalette)) return new AlarmPalette(Colors.Red, Colors.Green, Colors.Blue, Colors.Black);
        if (bare.IsEnum) return Enum.GetValues(bare).Cast<object>().First(v => !Equals(v, plain));
        return null;
    }
}
