// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;
using MatPlotLibNet.Models;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet.Tests.Rendering.Svg.Interaction;

/// <summary>A step-through SVG moves between its frames by itself and by hand: it plays on load unless the reader asked for reduced
/// motion, stops at the last frame unless it loops, steps with the arrow keys, and a slider and a button show where it is and let
/// the reader take over. The frame that is shown, the position of the slider, the label of the button and the intervals armed are
/// all asserted, with time the test's own.</summary>
public class StepThroughScriptTests
{
    private static Figure Frame(int k) =>
        Plt.Create().WithSize(300, 200).WithTitle($"frame {k}").Plot([0.0, 1.0], [0.0, k]).Build();

    private static string Steps(int frames, bool loop = true)
    {
        using var stream = new MemoryStream();
        new StepThroughSvgTransform().Transform(Enumerable.Range(0, frames).Select(Frame), TimeSpan.FromMilliseconds(100), loop, stream);
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static int Shown(InteractionScriptHarness h)
    {
        var visible = h.Document.QuerySelectorAllRaw("[data-frame]").Where(f => string.IsNullOrEmpty(f.getAttribute("display"))).ToArray();
        Assert.Single(visible);   // exactly one frame at a time
        return int.Parse(visible[0].getAttribute("data-frame")!);
    }

    private static string? Slider(InteractionScriptHarness h, string attribute) => h.GetAttribute("[role='slider']", attribute);

    private static string? Button(InteractionScriptHarness h, string attribute) => h.GetAttribute("[role='button']", attribute);

    private static void Key(InteractionScriptHarness h, string key) => h.Simulate("svg[data-interval-ms]", "keydown", e => e.key = key);

    // ── on load ─────────────────────────────────────────────────────────────────

    [Fact]
    public void OnLoad_TheControlsAppear_TheFirstFrameIsShown_AndItPlays()
    {
        using var h = InteractionScriptHarness.FromSvg(Steps(3));

        Assert.Equal(0, Shown(h));
        Assert.True(string.IsNullOrEmpty(h.GetAttribute("[data-steps-controls]", "display")));
        Assert.Equal(1, h.ActiveIntervals);
        Assert.Equal("Pause", Button(h, "aria-label"));
        Assert.Equal("true", Button(h, "aria-pressed"));
    }

    [Fact]
    public void ForAReaderWhoAskedForReducedMotion_ItDoesNotPlayOnLoad()
    {
        using var h = InteractionScriptHarness.FromSvg(Steps(3), reducedMotion: true);

        Assert.Equal(0, h.ActiveIntervals);
        Assert.Equal("Play", Button(h, "aria-label"));
        h.Tick();
        Assert.Equal(0, Shown(h));
    }

    [Fact]
    public void AReaderWhoAskedForReducedMotion_CanStillPlayIt_ByChoice()
    {
        using var h = InteractionScriptHarness.FromSvg(Steps(3), reducedMotion: true);

        h.Simulate("[role='button']", "click");

        Assert.Equal(1, h.ActiveIntervals);
        h.Tick();
        Assert.Equal(1, Shown(h));
    }

    // ── playing ─────────────────────────────────────────────────────────────────

    [Fact]
    public void WhileItPlays_EachIntervalShowsTheNextFrame_AndALoopStartsAgain()
    {
        using var h = InteractionScriptHarness.FromSvg(Steps(3, loop: true));

        h.Tick();
        Assert.Equal(1, Shown(h));
        h.Tick();
        Assert.Equal(2, Shown(h));
        h.Tick();
        Assert.Equal(0, Shown(h));   // back to the first
        Assert.Equal(1, h.ActiveIntervals);
    }

    [Fact]
    public void WithoutALoop_ItStopsAtTheLastFrame_AndTheButtonOffersToPlayAgain()
    {
        using var h = InteractionScriptHarness.FromSvg(Steps(3, loop: false));

        h.Tick();
        h.Tick();
        Assert.Equal(2, Shown(h));
        h.Tick();   // the interval that would have gone past the end

        Assert.Equal(2, Shown(h));
        Assert.Equal(0, h.ActiveIntervals);
        Assert.Equal("Play", Button(h, "aria-label"));
        Assert.Equal("false", Button(h, "aria-pressed"));
    }

    [Fact]
    public void PlayingAgainAfterTheEnd_StartsFromTheFirstFrame()
    {
        using var h = InteractionScriptHarness.FromSvg(Steps(3, loop: false));
        h.Tick();
        h.Tick();
        h.Tick();

        h.Simulate("[role='button']", "click");

        Assert.Equal(0, Shown(h));
        Assert.Equal(1, h.ActiveIntervals);
    }

    [Fact]
    public void TheButton_PausesAndResumes()
    {
        using var h = InteractionScriptHarness.FromSvg(Steps(4));
        h.Tick();

        h.Simulate("[role='button']", "click");
        Assert.Equal(0, h.ActiveIntervals);
        Assert.Equal("Play", Button(h, "aria-label"));
        h.Tick();
        Assert.Equal(1, Shown(h));   // paused: time passing changes nothing

        h.Simulate("[role='button']", "click");
        Assert.Equal(1, h.ActiveIntervals);
        h.Tick();
        Assert.Equal(2, Shown(h));
    }

    [Fact]
    public void TheSpaceBar_PlaysAndPauses_LikeTheButton()
    {
        using var h = InteractionScriptHarness.FromSvg(Steps(3));

        Key(h, " ");
        Assert.Equal(0, h.ActiveIntervals);
        Key(h, " ");
        Assert.Equal(1, h.ActiveIntervals);
    }

    // ── by hand ─────────────────────────────────────────────────────────────────

    [Fact]
    public void TheArrowKeys_StepOneFrame_AndTakeOverFromThePlayback()
    {
        using var h = InteractionScriptHarness.FromSvg(Steps(4));

        Key(h, "ArrowRight");

        Assert.Equal(1, Shown(h));
        Assert.Equal(0, h.ActiveIntervals);   // the reader took over: it no longer plays by itself
        Key(h, "ArrowRight");
        Key(h, "ArrowRight");
        Assert.Equal(3, Shown(h));
        Key(h, "ArrowRight");
        Assert.Equal(3, Shown(h));   // no wrapping by hand
        Key(h, "ArrowLeft");
        Assert.Equal(2, Shown(h));
    }

    [Fact]
    public void ArrowLeftAtTheFirstFrame_StaysThere()
    {
        using var h = InteractionScriptHarness.FromSvg(Steps(3));

        Key(h, "ArrowLeft");

        Assert.Equal(0, Shown(h));
    }

    [Fact]
    public void HomeAndEnd_GoToTheFirstAndTheLastFrame()
    {
        using var h = InteractionScriptHarness.FromSvg(Steps(5));

        Key(h, "End");
        Assert.Equal(4, Shown(h));
        Key(h, "Home");
        Assert.Equal(0, Shown(h));
    }

    [Fact]
    public void TheSlider_FollowsTheFrame_InValueAndInWords()
    {
        using var h = InteractionScriptHarness.FromSvg(Steps(4));

        Key(h, "ArrowRight");
        Key(h, "ArrowRight");

        Assert.Equal("2", Slider(h, "aria-valuenow"));
        Assert.Equal("Frame 3 of 4", Slider(h, "aria-valuetext"));
    }

    [Fact]
    public void AClickOnTheSlider_GoesToTheFrameAtThatPlace()
    {
        using var h = InteractionScriptHarness.FromSvg(Steps(5));
        // The harness's rectangle for any element is left 50, width 20: the far end of the track is the last frame, the middle the third.
        h.Simulate("[data-steps-track]", "click", e => e.clientX = 60);

        Assert.Equal(2, Shown(h));
        Assert.Equal(0, h.ActiveIntervals);

        h.Simulate("[data-steps-track]", "click", e => e.clientX = 70);
        Assert.Equal(4, Shown(h));
    }

    [Fact]
    public void TheArrowsOnTheSliderItself_Step_AsOnTheDocument()
    {
        using var h = InteractionScriptHarness.FromSvg(Steps(3));

        h.Simulate("[role='slider']", "keydown", e => e.key = "ArrowRight");

        Assert.Equal(1, Shown(h));
    }
}
