// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Packaging.OwnedRules;

/// <summary>What the interaction modifiers share about a gesture is written once: how a pointer is read against the layout
/// (<c>ChartLayoutViewExtensions</c>), the rubber band a held key opens (<c>DragTrack</c>, <c>DragFloor</c>), the box two corners
/// span (<c>DataRange.Between</c>) and the notch of the wheel (<c>ZoomNotch</c>). What the four native packages (WPF, Avalonia,
/// Uno, MAUI) share they share through the library's own types: the host decides whether a gesture may act
/// (<c>InteractionHost.Listening</c>), takes the layout of a paint and routes the gestures to a server; one follower subscribes a
/// control to an animation source; every animation timer derives from the one base, <c>AnimationTimer</c>; and inside each package
/// the chart and the streaming control derive from one surface that says what a control is.</summary>
internal static class InteractionRules
{
/// <summary>One notch of the wheel zooms by matplotlib's factor (<c>scale_factor = 0.85 ** event.step</c>, backend_bases.py).</summary>
    private static readonly OwnedRule ZoomNotchFactor = new(
        "one notch of the wheel, and a pinch of the same size, zooms a range by matplotlib's 0.85 in and 1/0.85 out, written once in ZoomNotch: no modifier or translator types a factor",
        @"\b0\.85\b|\b1\.15\b|\b0\.15\b",
        "Src/MatPlotLibNet/Interaction/ZoomNotch.cs") { Under = "Src/MatPlotLibNet/Interaction/", Finds = [
        "    private const double ZoomFactor = 0.15; // 15 % zoom per scroll notch"] };

    private const string Interaction = "Src/MatPlotLibNet/Interaction/";

    private const string LayoutReading = "Src/MatPlotLibNet/Interaction/ChartLayoutViewExtensions.cs";

    private static readonly string[] Packages = ["Wpf", "Avalonia", "Uno", "Maui"];

    private static string FolderOf(string package) => $"Src/MatPlotLibNet.{package}/";

    private static IEnumerable<OwnedRule> PerPackage(string rule, string pattern, string[] finds) =>
        Packages.Select(package => OwnedRule.Nowhere($"{rule} ({package})", pattern, FolderOf(package)) with { Finds = finds });

    /// <summary>The one file of each package that says what a native control is apart from its figure: the surface the chart and the
    /// streaming control of WPF, Avalonia and Uno derive from, and the MAUI view the streaming view derives from.</summary>
    private static string SurfaceOf(string package) => FolderOf(package) + (package == "Maui" ? "MplChartView.cs" : "MplChartSurface.cs");

    /// <summary>What a native control is apart from where its figure comes from, written once per package by its surface.</summary>
    private static IEnumerable<OwnedRule> OnePerPackageSurface() =>
        Packages.Select(package => new OwnedRule(
            $"what a native control is apart from its figure (its host made with its own facts, the five gestures behind the host's guard, its handler, the failure and the clicked point it passes on, its painter, the host it routes to a server) is written once, by the package's surface: the chart and the streaming control only say where their figure comes from ({package})",
            @"\b_host\.(?:Handle\w+\(|Listening\b)|\bnew (?:[\w.]+\.)?InteractionHost\(|(?<![.\w])(?:[\w.]+\.)?(?:InteractionHost|SkiaChartPainter)\s+_\w+|\bIChartEventHandler\? ChartEventHandler\b|\bevent Action<(?:Exception|PinnedAnnotation)>\? (?:ChartEventHandlerFaulted|DataPointClicked)\b|\bIServerInteractive\.Host\b",
            SurfaceOf(package)) { Under = FolderOf(package), Finds = [
            "        _host.HandleScroll(ToScrollArgs(e, this));",
            "        if (!_host.Listening) return;",
            "        _host = new InteractionHost(() => IsInteractive, _follower.Redraw);",
            "    public event Action<Exception>? ChartEventHandlerFaulted;",
            "    InteractionHost IServerInteractive.Host => _host;",
            "    private readonly SkiaChartPainter _painter = new();",
            "    private readonly MatPlotLibNet.Skia.SkiaChartPainter _painterCopy = new();",
            "    private readonly MatPlotLibNet.Interaction.InteractionHost _gestures;",
            "        _gestures = new MatPlotLibNet.Interaction.InteractionHost(() => true, Repaint);"], Ignores = [
            "        _view.Host.Painted(figure, target.Paint(figure, _view.Host.ActiveBrushSelect));",
            "    private protected override Figure? Shown => StreamingFigure?.Figure;",
            "    private protected override void Repaint() => _follower.Redraw();"] });

    /// <summary>The rules of the area, in the order the scan reads them.</summary>
    internal static OwnedRule[] All { get; } =
    [
        ZoomNotchFactor,
        new("an event of the library is applied to the subplot it names, or dropped when the figure has none, through FigureInteractionEvent's OnTargetAxes (OnMovableAxes for a view that moves): no event tests the target and answers false itself",
            @"\bif\s*\(\s*!\s*TryTarget(?:Movable)?Axes\(",
            "Src/MatPlotLibNet/Interaction/FigureInteractionEvent.cs") { Under = Interaction, Finds = [
            "        if (!TryTargetAxes(figure, out var axes)) return false;",
            "        if (!TryTargetMovableAxes(figure, out var axes)) return false;"], Ignores = [
            "    internal override bool TryApplyTo(Figure figure) => OnTargetAxes(figure, axes => axes.Overlays.Add(Tool));"] },
        new("a drawing tool (a trendline, a horizontal level, a Fibonacci retracement) enters and leaves an axes through its own list, its IDrawingTool.AddTo and RemoveFrom: no axes member or event names a tool's list to add or remove it, or switches over the kinds of tool",
            @"\b(?:Trendlines|HorizontalLevels|FibonacciRetracements)\.(?:Add|Remove)\(|\bcase\s+(?:Trendline|HorizontalLevel|FibonacciRetracement)\s+\w+\s*:",
            "Src/MatPlotLibNet/Models/Tools/Trendline.cs", "Src/MatPlotLibNet/Models/Tools/HorizontalLevel.cs",
            "Src/MatPlotLibNet/Models/Tools/FibonacciRetracement.cs") { Under = "Src/", Finds = [
            "    internal void RemoveLevel(HorizontalLevel level) => Overlays.HorizontalLevels.Remove(level);",
            "        Overlays.Trendlines.Add(line);",
            "            case Trendline t:          axes.RemoveTrendline(t); break;"], Ignores = [
            "    public IReadOnlyList<Trendline> Trendlines => Overlays.Trendlines;",
            "        if (Tool is IDrawingTool tool) tool.RemoveFrom(axes.Overlays);"] },
        new("whether a pointer falls on an axes, and the axes it fell in (the first when it fell in none), are the layout's IsOnAxes and AxesOrFirstAt: no modifier tests or defaults a hit itself",
            @"HitTestAxes\([^)]*\)\)?\s*(?:is\s+not\s+null|is\s+null|\?\?|\.GetValueOrDefault\()",
            LayoutReading) { Under = Interaction, Finds = [
            "        _layout.HitTestAxes(args.X, args.Y) is not null;"] },
        new("a press of the left button on an axes is the layout's LeftPressAxes (and IsLeftPressOnAxes): no modifier compares the button itself",
            @"\bButton\s*[!=]=\s*PointerButton\.Left\b",
            LayoutReading) { Under = Interaction, Finds = [
            "        args.Button == PointerButton.Left &&"] },
        new("the range every secondary Y axis shows between two pixels is the layout's SecondaryRangesBetween, in the shape the caller names (DataRange.Between or DataRange.FromCorners): no modifier walks the secondary views itself",
            @"\bSecondaryViewsOf\(",
            LayoutReading) { Under = Interaction, Finds = [
            "        var views = _layout.SecondaryViewsOf(_activeAxes);"] },
        new("a drag a held key opens is followed by a DragTrack (its press, its pointer, its data under the press), held by RubberBandModifier, which answers the pointer with it: no modifier keeps the start and the current pixel of a drag, or a track of its own",
            @"\b_(?:start|current)(?:Pixel|Data)[XY]?\b|\bDragTrack\s+_\w+",
            "Src/MatPlotLibNet/Interaction/Modifiers/DragTrack.cs", "Src/MatPlotLibNet/Interaction/Modifiers/RubberBandModifier.cs") { Under = Interaction, Finds = [
            "    private double _startDataX;"] },
        OwnedRule.Nowhere("a drag too short to count is judged by its DragFloor: no modifier types a pixel threshold against the start of a drag",
            @"\)\s*<\s*2(?:\.0)?\b",
            under: Interaction) with { Finds = [
            "        if (Math.Abs(args.X - _startPixelX) < 2 && Math.Abs(args.Y - _startPixelY) < 2) return;"] },
        OwnedRule.Nowhere("the box two data corners span is DataRange.Between: no modifier takes the smaller and the larger of a drag's coordinates itself",
            @"Math\.(?:Min|Max)\(\s*(?:_start\w*|a\.[XY]|start\w*)\s*,",
            under: Interaction) with { Finds = [
            "        double x1 = Math.Min(_startDataX, endDataX);"] },
        .. PerPackage(
            "whether a native control may act on a gesture is the host's (InteractionHost.Listening: a paint has given it a figure and the control is interactive): no control reads the host's IsAttached or tests its own IsInteractive in a condition",
            @"\bIsAttached\b|\b(?:if|while)\s*\([^)]*\bIsInteractive\b|(?:&&|\|\|)\s*!?\s*\w*\.?IsInteractive\b|\bIsInteractive\s*(?:&&|\|\|)",
            ["        if (IsInteractive)"]),
        .. OnePerPackageSurface(),
        new("the state a rubber band shows while its drag is in progress (the zoom rectangle, the span, the brush) is made through DragTrack.Showing, which reads where the drag started, where the pointer is and its axes only while it is in progress: no modifier reads the band's IsActive, Start, Current or AxesIndex",
            @"\bIsActive\s*\?|\b(?:Band|_drag)\s*\.\s*(?:IsActive|Start|Current|AxesIndex)\b",
            "Src/MatPlotLibNet/Interaction/Modifiers/DragTrack.cs") { Under = Interaction, Finds = [
            "        Band.IsActive ? new BrushSelectState(Band.Start.X, Band.Start.Y, Band.Current.X, Band.Current.Y, Band.AxesIndex) : null;",
            "            ? new SpanSelectState(Band.Start.X, Band.Current.X, Band.AxesIndex, Layout.GetPlotArea(Band.AxesIndex))",
            "        if (!_drag.IsActive) return null;"], Ignores = [
            "        Band.Showing(static (x0, y0, x1, y1, axes) => new BrushSelectState(x0, y0, x1, y1, axes));",
            "    public sealed override void OnPointerPressed(PointerInputArgs args) => _drag.Begin(Layout, args);",
            "        var box = drag.DataBox;"] },
        new("the layout of a paint is handed to the host as the paint's LayoutResult and becomes the ChartLayout the host aims its controller with in InteractionHost.Painted: no control builds a ChartLayout",
            @"\bChartLayout\.Create\(",
            "Src/MatPlotLibNet/Interaction/InteractionHost.cs") { Under = "Src/", Finds = [
            "        Dispatcher.UIThread.Post(() => Attach(figure, ChartLayout.Create(figure, layoutResult)));"] },
        new("a native control is wired to a server sink by RouteToServer (IServerInteractive), which makes the sink and sets it on the host: no control makes the sink or keeps a forward of the host's ServerEventSink",
            @"\bSignalREventSink\.Create\b|\bServerEventSink\b",
            "Src/MatPlotLibNet/Interaction/InteractionHost.cs", "Src/MatPlotLibNet/Interaction/ServerRouteExtensions.cs") { Under = "Src/", Finds = [
            "    internal Action<FigureInteractionEvent>? ServerEventSink"] },
        new("an animation timer's rule (16 ms to begin with, whether it runs, a start that is idempotent, a tick said with the timer as its sender, the timer's members) is the base AnimationTimer's: every timer derives from it and none implements IAnimationTimer or declares a Tick itself",
            @"FromMilliseconds\(\s*16\s*\)|\bTick\?\.Invoke\(|\b_running\b|[:,]\s*(?:[\w.]+\.)?IAnimationTimer\b|\bevent EventHandler\?\s+Tick\b",
            "Src/MatPlotLibNet/Animation/AnimationTimer.cs") { Under = "Src/", Finds = [
            "    private bool _running;",
            "public sealed class WpfAnimationTimer : IAnimationTimer",
            "internal sealed class Ticker : IDisposable, MatPlotLibNet.Animation.IAnimationTimer",
            "    public event EventHandler? Tick"], Ignores = [
            "public interface IAnimationTimer : IDisposable",
            "public sealed class WpfAnimationTimer : AnimationTimer"] },
        new("a control is painted again after a gesture through the repaint it gave its host at construction, as it gave it its interactivity: no control (and no follower) subscribes to the host to hear that a gesture changed the figure",
            @"\bInvalidateRequested\s*[+-]=",
            "Src/MatPlotLibNet/Interaction/InteractionHost.cs") { Under = "Src/", Finds = [
            "        _host.InvalidateRequested += InvalidateVisual;"] },
        new("a control follows an animation source through AnimationSourceFollower (let go of the one before, subscribe to the one now, hand the frame to the control's thread): no control subscribes to FrameReady itself",
            @"\bFrameReady\s*[+-]=",
            "Src/MatPlotLibNet/Interaction/AnimationSourceFollower.cs") { Under = "Src/", Finds = [
            "                _animationSource.FrameReady -= OnAnimationFrameReady;"] },
    ];
}
