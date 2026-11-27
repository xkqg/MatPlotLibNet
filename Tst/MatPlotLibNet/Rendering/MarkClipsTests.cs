// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The one rule of a clip on every surface (<see cref="MarkClips{TClip}"/>): a mark is cut by every clip pushed, a text by none,
/// and a clip that holds marks alone is applied once, at its push.</summary>
public class MarkClipsTests
{
    [Fact]
    public void AClipThatHoldsMarksAlone_IsAppliedAtItsPush_AndLiftedAtItsPop()
    {
        var surface = new Recording();
        var clips = new MarkClips<string>();

        clips.Push(surface, "a");
        clips.Mark(surface);
        clips.Mark(surface);
        Assert.Equal("a", clips.Pop(surface));

        Assert.Equal(["apply a", "lift"], surface.Calls);
    }

    [Fact]
    public void ATextLiftsEveryAppliedClip_AndTheNextMarkAppliesThemAgain_OutermostFirst()
    {
        var surface = new Recording();
        var clips = new MarkClips<string>();

        clips.Push(surface, "outer");
        clips.Push(surface, "inner");
        clips.Lift(surface);
        clips.Lift(surface);
        clips.Mark(surface);
        clips.Pop(surface);
        clips.Pop(surface);

        Assert.Equal(["apply outer", "apply inner", "lift", "lift", "apply outer", "apply inner", "lift", "lift"], surface.Calls);
    }

    [Fact]
    public void AClipPushedAfterAText_WaitsForAMark_AndAPopOfAClipNotApplied_LiftsNothing()
    {
        var surface = new Recording();
        var clips = new MarkClips<string>();

        clips.Push(surface, "outer");
        clips.Lift(surface);
        clips.Push(surface, "inner");
        Assert.Equal("inner", clips.Pop(surface));
        clips.Mark(surface);
        clips.Pop(surface);

        Assert.Equal(["apply outer", "lift", "apply outer", "lift"], surface.Calls);
    }

    [Fact]
    public void WithNoClipPushed_AMarkATextAndAPop_DoNothing()
    {
        var surface = new Recording();
        var clips = new MarkClips<string>();

        clips.Mark(surface);
        clips.Lift(surface);
        Assert.Null(clips.Pop(surface));
        clips.Push(surface, "a");
        clips.Pop(surface);
        Assert.Null(clips.Pop(surface));

        Assert.Equal(["apply a", "lift"], surface.Calls);
    }

    private sealed class Recording : IClipSurface<string>
    {
        public List<string> Calls { get; } = [];

        public void Apply(string clip) => Calls.Add($"apply {clip}");

        public void Lift() => Calls.Add("lift");
    }
}
