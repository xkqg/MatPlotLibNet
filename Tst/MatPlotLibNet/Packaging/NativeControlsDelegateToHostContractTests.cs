// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace MatPlotLibNet.Tests.Packaging;

/// <summary>What the four native packages (WPF, Avalonia, Uno, MAUI) share, they share through the library's own types and not by
/// writing it again in each control: the host decides whether a gesture may act (<c>InteractionHost.Listening</c>), takes the layout
/// of a paint (<c>Painted</c>) and routes the gestures to a server (<c>RouteToServer</c>); one follower subscribes a control to an
/// animation source; every animation timer derives from the one base, <c>AnimationTimer</c>. Inside a package the chart control and
/// the streaming control derive from one surface (<c>MplChartSurface</c>; on MAUI the streaming view derives from the chart view),
/// which forwards the platform's input and hands the host its own facts; a leaf only says where its figure comes from. The rows of
/// <c>OwnedRules.InteractionRules</c> refuse a second spelling in the four packages (and in the whole of <c>Src</c>, the Razor
/// components of the Blazor package included, for the server route, the timer and the follower); the facts below read what each
/// surface writes in place of them.</summary>
public class NativeControlsDelegateToHostContractTests
{
    private static SourceFile FileAt(string path) =>
        RepositoryTree.SourceFiles().Concat(RepositoryTree.ComponentFiles()).Single(file => file.Path == path);

    private static string TextOf(string path) => FileAt(path).Text;

    private static readonly Regex HostInput = new(@"^\s*_host\.Handle(?:PointerPressed|PointerMoved|PointerReleased|Scroll|KeyDown)\(", RegexOptions.CultureInvariant);

    /// <summary>The surface of each package whose input events arrive one by one (WPF, Avalonia, Uno).</summary>
    public static TheoryData<string> Surfaces { get; } =
    [
        "Src/MatPlotLibNet.Wpf/MplChartSurface.cs", "Src/MatPlotLibNet.Avalonia/MplChartSurface.cs", "Src/MatPlotLibNet.Uno/MplChartSurface.cs",
    ];

    private static readonly Regex HandlerSignature = new(@"^\s*(?:private|protected|public|internal)\b", RegexOptions.CultureInvariant);

    /// <summary>Every input the host is given is given behind the one guard, the first thing a handler does after the base call: five
    /// inputs in each surface, each the host's question.</summary>
    [Theory]
    [MemberData(nameof(Surfaces))]
    public void EveryInputGivenToTheHost_StandsBehindTheHostsGuard(string path)
    {
        var lines = FileAt(path).Lines;
        var inputs = Enumerable.Range(0, lines.Count).Where(i => HostInput.IsMatch(lines[i])).ToArray();

        Assert.Equal(5, inputs.Length);
        Assert.All(inputs, i =>
        {
            int back = i - 1;
            while (back >= 0 && lines[back].Trim() != "if (!_host.Listening) return;" && !HandlerSignature.IsMatch(lines[back]))
            {
                back--;
            }

            Assert.True(back >= 0 && lines[back].Trim() == "if (!_host.Listening) return;",
                $"{path}:{i + 1}: the host is given an input with no guard in the handler before it");
        });
    }

    /// <summary>What a surface makes its host with: its own interactivity and its repaint, which a streaming leaf overrides with its
    /// follower's redraw (the limits follow the data first).</summary>
    private const string HostMadeWithTheControlsFacts = @"new InteractionHost\(\s*\(\)\s*=>\s*IsInteractive\s*,\s*Repaint\b";

    public static TheoryData<string> HostMakers { get; } =
    [
        "Src/MatPlotLibNet.Wpf/MplChartSurface.cs", "Src/MatPlotLibNet.Avalonia/MplChartSurface.cs", "Src/MatPlotLibNet.Uno/MplChartSurface.cs",
        "Src/MatPlotLibNet.Maui/MplChartView.cs",
    ];

    [Theory]
    [MemberData(nameof(HostMakers))]
    public void ASurface_GivesItsHostItsOwnInteractivityAndRepaint_AtConstruction(string path) =>
        Assert.Matches(HostMadeWithTheControlsFacts, TextOf(path));

    [Fact]
    public void TheMauiView_AsksItsHostWhetherItIsListening_InEveryHandler()
    {
        var view = FileAt("Src/MatPlotLibNet.Maui/MplChartView.cs");

        Assert.Equal(5, Regex.Matches(view.Text, @"_host\.Listening\b").Count);
        Assert.DoesNotMatch(@"(?<![.\w])Listening\b", view.Code);
    }

    public static TheoryData<string> PaintingFiles { get; } =
    [
        "Src/MatPlotLibNet.Wpf/MplChartSurface.cs", "Src/MatPlotLibNet.Avalonia/MplChartSurface.cs",
        "Src/MatPlotLibNet.Uno/MplChartSurface.cs", "Src/MatPlotLibNet.Maui/MplChartView.cs",
    ];

    [Theory]
    [MemberData(nameof(PaintingFiles))]
    public void APaint_HandsItsLayoutToTheHost(string path) =>
        Assert.Matches(@"\.Painted\(", TextOf(path));

    public static TheoryData<string> AnimatedControls { get; } =
    [
        "Src/MatPlotLibNet.Wpf/MplChartControl.cs", "Src/MatPlotLibNet.Avalonia/MplChartControl.cs",
        "Src/MatPlotLibNet.Uno/MplChartElement.cs", "Src/MatPlotLibNet.Maui/MplChartView.cs",
    ];

    [Theory]
    [MemberData(nameof(AnimatedControls))]
    public void AChartControl_HasAnAnimationSource_ThroughTheFollower(string path)
    {
        string text = TextOf(path);

        Assert.Matches(@"public IAnimationSource\? AnimationSource", text);
        Assert.Matches(@"new AnimationSourceFollower\(", text);
    }

    /// <summary>The Blazor component is a control too, and it is read as source: it follows its animation source through the same
    /// follower as the four native packages, and keeps no subscription of its own to the source's frames.</summary>
    [Fact]
    public void TheBlazorChart_FollowsItsAnimationSource_ThroughTheFollower()
    {
        string text = TextOf("Src/MatPlotLibNet.Blazor/MplChart.razor");

        Assert.Matches(@"new AnimationSourceFollower\(", text);
        Assert.DoesNotMatch(@"\bFrameReady\b", text);
    }

    /// <summary>The streaming control of each package: the Uno one cannot be built in a test (its base type comes out of Uno's build
    /// pipeline), so what each one writes is read here, for all four alike.</summary>
    public static TheoryData<string> StreamingLeaves { get; } =
    [
        "Src/MatPlotLibNet.Wpf/MplStreamingChartControl.cs", "Src/MatPlotLibNet.Avalonia/MplStreamingChartControl.cs",
        "Src/MatPlotLibNet.Uno/MplStreamingChartElement.cs", "Src/MatPlotLibNet.Maui/MplStreamingChartView.cs",
    ];

    /// <summary>A streaming leaf follows the figure it is bound to through the library's streaming follower, is painted again through
    /// the follower's redraw (the limits follow the data, then it draws), and lets go of the figure when it leaves the screen: the
    /// figure outlives the control and would otherwise keep asking a dead one to draw.</summary>
    [Theory]
    [MemberData(nameof(StreamingLeaves))]
    public void AStreamingLeaf_FollowsItsFigure_RepaintsThroughTheFollower_AndLetsGoWhenItLeaves(string path)
    {
        string text = TextOf(path);

        Assert.Matches(@"new StreamingFigureFollower\(", text);
        Assert.Matches(@"override void Repaint\(\) => _follower\.Redraw\(\);", text);
        Assert.Matches(@"_follower\.Follow\((?!null\))", text);
        Assert.Matches(@"_follower\.Follow\(null\)", text);
    }

    public static TheoryData<string> ServerRouteFiles { get; } =
    [
        "Src/MatPlotLibNet.Wpf/ServerInteractionExtensions.cs", "Src/MatPlotLibNet.Avalonia/ServerInteractionExtensions.cs",
        "Src/MatPlotLibNet.Uno/ServerInteractionExtensions.cs", "Src/MatPlotLibNet.Maui/ServerInteractionExtensions.cs",
    ];

    /// <summary>Each package says <c>WithServerInteraction</c> once, over its surface, for the chart control and the streaming control
    /// alike (it hands back the control it was called on), and says it as one call of the library's route.</summary>
    [Theory]
    [MemberData(nameof(ServerRouteFiles))]
    public void AServerInteraction_IsOneCallOfTheRoute_OverTheSurface(string path)
    {
        string text = TextOf(path);

        Assert.Single(Regex.Matches(text, @"extension<TControl>\(TControl \w+\) where TControl : Mpl\w+"));
        Assert.Single(Regex.Matches(text, @"public TControl WithServerInteraction\("));
        Assert.Single(Regex.Matches(text, @"\.RouteToServer\(sendAsync\)"));
    }
}
