// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.RegularExpressions;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Tools;
using MatPlotLibNet.Rendering.Svg;

namespace MatPlotLibNet.AspNetCore.Tests;

/// <summary>An interaction event travels by name: the sink of a native control, the script of a page and the hub of the
/// server each spelled the name of the hub method that carries it, so an event added to one of them and not the others was
/// silently dropped on the way (a native control's rectangle zoom, span selection and rotation never reached the server,
/// and nothing said so). The names are written once, in <see cref="HubMethodNames"/>, and this holds the three readers to
/// it: every concrete event is routed or named as not routed with its reason, the hub has a method for every route, the
/// sink sends every routed event by that name, and the page's script invokes only names that are routed.</summary>
public class HubRoutingContractTests
{
    /// <summary>One instance of every concrete interaction event. A new event type is not on this list until somebody adds
    /// it, and the first test turns red for it.</summary>
    private static readonly FigureInteractionEvent[] Samples =
    [
        new ZoomEvent("c", 0, 0, 1, 0, 1),
        new PanEvent("c", 0, 1, 1),
        new ResetEvent("c", 0, 0, 1, 0, 1),
        new RectangleZoomEvent("c", 0, 0, 1, 0, 1),
        new LegendToggleEvent("c", 0, 0),
        new Rotate3DEvent("c", 0, 5, 5),
        new BrushSelectEvent("c", 0, 0, 0, 1, 1),
        new SpanSelectEvent("c", 0, 0, 1),
        new HoverEvent("c", 0, 0.5, 0.5),
        new DataCursorEvent("c", 0, new PinnedAnnotation("s", 0, 0, 0, 0, 0)),
        new AddTrendlineEvent("c", 0, new Trendline(0, 0, 1, 1)),
        new AddHorizontalLevelEvent("c", 0, new HorizontalLevel(1.5)),
        new AddFibonacciRetracementEvent("c", 0, new FibonacciRetracement(2, 1)),
        new RemoveDrawingToolEvent("c", 0, new Trendline(0, 0, 1, 1)),
        new RowSelectionEvent("c", 0, ["a"]),
    ];

    private static string? NameOf(FigureInteractionEvent evt) => HubMethodNames.For(evt);

    [Fact]
    public void TheSamples_CoverEveryConcreteEventOfTheLibrary()
    {
        var concrete = typeof(FigureInteractionEvent).Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsPublic: true } && typeof(FigureInteractionEvent).IsAssignableFrom(t))
            .ToHashSet();

        Assert.Equal(concrete.Order(TypeNameComparer.Instance), Samples.Select(s => s.GetType()).Order(TypeNameComparer.Instance));
    }

    [Fact]
    public void EveryEvent_IsRouted_OrNamedAsNotRoutedWithItsReason()
    {
        foreach (var sample in Samples)
        {
            bool routed = NameOf(sample) is not null;
            bool excused = HubMethodNames.NotRouted.TryGetValue(sample.GetType(), out var reason) && !string.IsNullOrWhiteSpace(reason);

            Assert.True(routed ^ excused, $"{sample.GetType().Name} must be routed, or listed in HubMethodNames.NotRouted with a reason, and not both");
        }
    }

    [Fact]
    public void TheHub_HasAMethodForEveryRoutedEvent_TakingThatEvent()
    {
        foreach (var sample in Samples.Where(s => NameOf(s) is not null))
        {
            var method = typeof(ChartHub).GetMethod(NameOf(sample)!, BindingFlags.Public | BindingFlags.Instance);
            Assert.True(method is not null, $"ChartHub has no public method {NameOf(sample)} for {sample.GetType().Name}");

            // A hover travels as the payload the client can supply; the hub stamps the connection id itself.
            var expected = sample is HoverEvent ? typeof(HoverEventPayload) : sample.GetType();
            Assert.Equal(expected, Assert.Single(method!.GetParameters()).ParameterType);
        }
    }

    [Fact]
    public void TheHub_HasTheMethodThatCarriesAViewersPointer()
    {
        var method = typeof(ChartHub).GetMethod(HubMethodNames.Cursor, BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(method);
        Assert.Equal(typeof(CursorEventPayload), Assert.Single(method!.GetParameters()).ParameterType);
    }

    [Fact]
    public void TheSink_SendsEveryRoutedEventUnderItsName_AndSendsNothingForTheOthers()
    {
        foreach (var sample in Samples)
        {
            var sent = new List<string>();
            SignalREventSink.Create((method, _) => { sent.Add(method); return Task.CompletedTask; })(sample);

            if (NameOf(sample) is { } name) Assert.Equal([name], sent);
            else Assert.Empty(sent);
        }
    }

    [Fact]
    public void ThePagesScript_InvokesOnlyRoutedNames()
    {
        // The names of the events, and the one method that is not an event: where a viewer's pointer is.
        var routed = Samples.Select(NameOf).Where(n => n is not null).Append(HubMethodNames.Cursor).ToHashSet();
        var script = SvgSignalRInteractionScript.GetScript(
            enableBrushSelect: true, enableHover: true, enableDataCursor: true, enableViewerCursors: true);

        var invoked = Regex.Matches(script, @"invoke\('(On[A-Za-z0-9]+)'").Select(m => m.Groups[1].Value).Distinct().ToArray();

        Assert.NotEmpty(invoked);
        Assert.All(invoked, name => Assert.Contains(name, routed));
    }

    private sealed class TypeNameComparer : IComparer<Type>
    {
        internal static readonly TypeNameComparer Instance = new();
        public int Compare(Type? x, Type? y) => string.CompareOrdinal(x?.FullName, y?.FullName);
    }
}
