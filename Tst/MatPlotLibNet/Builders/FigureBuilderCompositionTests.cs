// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Tests.Packaging;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The figure-level verbs that draw on the default axes (<c>Plt.Create().Plot(x, y)</c>) are the subplot's verbs: the
/// figure builder configures its default axes through an <see cref="AxesBuilder"/> and each of its verbs forwards to the
/// <see cref="AxesBuilder"/> verb of the same name, so what a series verb does is written once.</summary>
public class FigureBuilderCompositionTests
{
    private const BindingFlags Everything = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    [Fact]
    public void TheDefaultAxes_IsConfiguredThroughAnAxesBuilder()
    {
        var steps = typeof(FigureBuilder).GetFields(Everything)
            .Where(f => f.FieldType.IsGenericType && f.FieldType.GetGenericTypeDefinition() == typeof(List<>))
            .Select(f => f.FieldType.GetGenericArguments()[0])
            .Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Action<>))
            .Select(t => t.GetGenericArguments()[0])
            .ToList();

        Assert.Contains(typeof(AxesBuilder), steps);
        Assert.DoesNotContain(typeof(Axes), steps);
    }

    /// <summary>Read over the builder's own methods and every call a caller writes on it, the extension members of the classes its
    /// verbs stand in included.</summary>
    [Fact]
    public void NoMemberOfTheFigureBuilder_BuildsASeriesOnTheAxesItself()
    {
        var factories = typeof(FigureBuilder).GetMethods(Everything).Concat(typeof(FigureBuilder).Calls().Select(call => call.Method))
            .Where(m => m.GetParameters().Any(p => p.ParameterType.IsGenericType
                && p.ParameterType.GetGenericTypeDefinition() == typeof(Func<,>)
                && p.ParameterType.GetGenericArguments()[0] == typeof(Axes)))
            .Select(m => m.Name)
            .ToList();

        Assert.True(factories.Count == 0, $"FigureBuilder still builds on the axes itself in: {string.Join(", ", factories)}.");
    }

    [Fact]
    public void ADateVerbTwice_LeavesTheDefaultAxesWithAutomaticDateTicks()
    {
        DateTime[] days = [new(2024, 1, 1), new(2024, 1, 2), new(2024, 1, 3)];

        var axes = Plt.Create().Plot(days, [1.0, 2.0, 3.0]).Scatter(days, [3.0, 2.0, 1.0]).Build().SubPlots[0];

        Assert.Equal(AxisScale.Date, axes.XAxis.Scale);
        Assert.IsType<AutoDateLocator>(axes.XAxis.TickLocator);
        Assert.IsType<AutoDateFormatter>(axes.XAxis.TickFormatter);
        Assert.Equal(2, axes.Series.Count);
    }
}
