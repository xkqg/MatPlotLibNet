// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Animation;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Interaction;

/// <summary>The WPF, Avalonia and MAUI controls each wrote the same animation source property: let go of the source before, keep the
/// one now, listen to it, and move each frame to the control's thread to show it. The follower is that once; a control hands it the
/// two things that are its own, how it reaches its thread and what showing a figure is.</summary>
public class AnimationSourceFollowerTests
{
    [Fact]
    public void ASourceFollowed_IsHeldAndListenedTo()
    {
        var source = new FakeAnimationSource();
        var follower = new AnimationSourceFollower(new PostedWork().Post, _ => { });

        follower.Follow(source);

        Assert.Same(source, follower.Source);
        Assert.Equal(1, source.SubscriberCount);
    }

    [Fact]
    public void ANewSource_LetsGoOfTheOldOne()
    {
        var first = new FakeAnimationSource();
        var second = new FakeAnimationSource();
        var follower = new AnimationSourceFollower(new PostedWork().Post, _ => { });
        follower.Follow(first);

        follower.Follow(second);

        Assert.Equal(0, first.SubscriberCount);
        Assert.Equal(1, second.SubscriberCount);
        Assert.Same(second, follower.Source);
    }

    [Fact]
    public void NoSource_LetsGoOfTheOneFollowed_AndHoldsNone()
    {
        var source = new FakeAnimationSource();
        var follower = new AnimationSourceFollower(new PostedWork().Post, _ => { });
        follower.Follow(source);

        follower.Follow(null);

        Assert.Equal(0, source.SubscriberCount);
        Assert.Null(follower.Source);
    }

    [Fact]
    public void FollowingNothing_BeforeAnySource_IsHarmless()
    {
        var follower = new AnimationSourceFollower(new PostedWork().Post, _ => { });

        Assert.Null(Record.Exception(() => follower.Follow(null)));
        Assert.Null(follower.Source);
    }

    [Fact]
    public void AFrame_IsShownOnTheControlsThread_NotOnTheThreadItArrivedOn()
    {
        var source = new FakeAnimationSource();
        var thread = new PostedWork();
        var shown = new List<Figure>();
        var follower = new AnimationSourceFollower(thread.Post, shown.Add);
        follower.Follow(source);
        var frame = new Figure();

        source.Raise(frame);
        Assert.Empty(shown);   // posted, not yet run
        thread.RunAll();

        Assert.Same(frame, Assert.Single(shown));
    }

    [Fact]
    public void ASourceLetGoOf_ShowsNoMoreFrames()
    {
        var source = new FakeAnimationSource();
        var thread = new PostedWork();
        var shown = new List<Figure>();
        var follower = new AnimationSourceFollower(thread.Post, shown.Add);
        follower.Follow(source);
        follower.Follow(null);

        source.Raise(new Figure());
        thread.RunAll();

        Assert.Empty(shown);
    }
}
