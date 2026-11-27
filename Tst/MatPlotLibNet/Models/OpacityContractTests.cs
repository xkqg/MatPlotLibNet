// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Serialization;

namespace MatPlotLibNet.Tests.Models;

/// <summary>An opacity is a number from 0 (clear) to 1 (opaque), and every setting of one refuses what is not, as matplotlib's
/// <c>Artist.set_alpha</c> does (python, matplotlib 3.11.2: <c>set_alpha(1.5)</c> raises <c>ValueError: alpha (1.5) is outside
/// 0-1 range</c>, and so does a NaN or a negative number). The rule has one owner (<c>ThrowIfNotOpacity</c>) that every setter reads; this holds
/// the census: every public setting of the library named after an opacity is found by reflection and tried, so an opacity added
/// without the rule turns this red, naming it, and a kind that cannot be built here is a row to add, never a setting that goes
/// unchecked.</summary>
public class OpacityContractTests
{
    private static readonly double[] NotOpacities = [1.5, -0.01, double.NaN, double.PositiveInfinity, double.NegativeInfinity];

    private static bool IsOpacity(PropertyInfo p) =>
        p.PropertyType == typeof(double) && p.GetIndexParameters().Length == 0
        && (p.Name.Contains("Alpha", StringComparison.Ordinal) || p.Name.Contains("Opacity", StringComparison.Ordinal))
        && p.SetMethod is { IsPublic: true };

    /// <summary>Every public setting (a setter or an init accessor) of a double named after an opacity, on a class or record of the
    /// library that is not a wire type or an interface: <c>Type.Property</c>.</summary>
    private static readonly IReadOnlyList<PropertyInfo> Settings =
    [
        .. typeof(Plt).Assembly.GetExportedTypes()
            .Where(t => !t.IsInterface && t.Namespace is not null && !t.Namespace.EndsWith(".Serialization", StringComparison.Ordinal))
            .SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(IsOpacity)
            .OrderBy(p => p.DeclaringType!.FullName, StringComparer.Ordinal).ThenBy(p => p.Name, StringComparer.Ordinal),
    ];

    public static TheoryData<string> Rows { get; } = new(Settings.Select(Name));

    private static string Name(PropertyInfo p) => $"{p.DeclaringType!.Name}.{p.Name}";

    /// <summary>One instance of the type that declares each setting. A series is the one a figure of its kind holds; the rest are made
    /// the way their own constructor wants.</summary>
    private static object Instance(Type type)
    {
        if (typeof(ISeries).IsAssignableFrom(type) && DrawnSeriesFixtures.DrawnTypes.Contains(type))
        {
            return DrawnSeriesFixtures.Instance(type.Name);
        }

        double[] a = [1, 2, 3];
        return type.Name switch
        {
            nameof(Rectangle) => new Rectangle(new(0, 0), 1, 1),
            nameof(Patch) => new Rectangle(new(0, 0), 1, 1),
            nameof(FigureImage) => new FigureImage(new double[,] { { 1, 2 }, { 3, 4 } }, 0, 0),
            nameof(SpanRegion) => new SpanRegion(0, 1, Orientation.Vertical),
            nameof(BollingerBands) => new BollingerBands(a),
            nameof(DrawDown) => new DrawDown(a),
            nameof(KeltnerChannels) => new KeltnerChannels(OhlcvBars.Hlc(a, a, a)),
            nameof(InteractionTheme) => new InteractionTheme(),
            nameof(Legend) => new Legend(),
            nameof(Pane3DConfig) => new Pane3DConfig(),
            nameof(GridStyle) => new GridStyle(),
            _ => Activator.CreateInstance(type) ?? throw new InvalidOperationException($"{type.Name} cannot be built here: add it to the table."),
        };
    }

    [Theory]
    [MemberData(nameof(Rows))]
    public void ASetting_RefusesWhatIsNotAnOpacity(string row)
    {
        var property = Settings.Single(p => Name(p) == row);
        var instance = Instance(property.DeclaringType!);

        foreach (double bad in NotOpacities)
        {
            var thrown = Assert.ThrowsAny<Exception>(() => property.SetValue(instance, bad));
            Assert.IsType<ArgumentOutOfRangeException>(thrown.InnerException);
        }
    }

    [Theory]
    [MemberData(nameof(Rows))]
    public void ASetting_TakesEveryOpacity_Clear_Opaque_AndBetween(string row)
    {
        var property = Settings.Single(p => Name(p) == row);
        var instance = Instance(property.DeclaringType!);

        foreach (double good in new[] { 0.0, 0.25, 1.0 })
        {
            property.SetValue(instance, good);

            Assert.Equal(good, (double)property.GetValue(instance)!);
        }
    }

    [Fact]
    public void ARefusedOpacity_LeavesTheSettingWhereItWas()
    {
        var series = new Axes().Bar(["a"], [1.0]);
        series.Alpha = 0.4;

        Assert.Throws<ArgumentOutOfRangeException>(() => series.Alpha = 2.0);

        Assert.Equal(0.4, series.Alpha);
    }

    [Fact]
    public void TheCensus_FindsTheSettingsThisRoundCovers()
    {
        // 36 settings: 23 series (a chord's ribbons and a Sankey diagram's links among them), 3 indicators, a figure image, a span, a zoom
        // indicator, a patch, a legend frame, a 3-D pane, three interaction opacities and a grid line. A new opacity moves this number and
        // must be tried above.
        Assert.Equal(36, Settings.Count);
    }
}
