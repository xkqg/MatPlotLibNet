// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Bunit;
using MatPlotLibNet.Animation;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Blazor.Tests;

/// <summary>Verifies <see cref="MplChart"/> animation source wiring.</summary>
public class MplChartAnimationTests : BunitContext
{
    [Fact]
    public void AnimationSource_DefaultsToNull_WhenNotProvided()
    {
        var figure = Plt.Create().Plot([1.0], [2.0]).Build();
        var cut = Render<MplChart>(p => p.Add(x => x.Figure, figure));
        Assert.Null(cut.Instance.AnimationSource);
    }

    [Fact]
    public void AnimationSource_CanBeSetViaParameter()
    {
        var figure = Plt.Create().Plot([1.0], [2.0]).Build();
        var fake = new FakeAnimationSource();
        var cut = Render<MplChart>(p =>
        {
            p.Add(x => x.Figure, figure);
            p.Add(x => x.AnimationSource, fake);
        });
        Assert.Same(fake, cut.Instance.AnimationSource);
    }

    [Fact]
    public void AnimationSource_Set_SubscribesToFrameReady()
    {
        var figure = Plt.Create().Plot([1.0], [2.0]).Build();
        var fake = new FakeAnimationSource();
        Render<MplChart>(p =>
        {
            p.Add(x => x.Figure, figure);
            p.Add(x => x.AnimationSource, fake);
        });
        Assert.Equal(1, fake.SubscriberCount);
    }

    [Fact]
    public void AnimationSource_Replaced_UnsubscribesOld()
    {
        var figure = Plt.Create().Plot([1.0], [2.0]).Build();
        var fake1 = new FakeAnimationSource();
        var fake2 = new FakeAnimationSource();
        var cut = Render<MplChart>(p =>
        {
            p.Add(x => x.Figure, figure);
            p.Add(x => x.AnimationSource, fake1);
        });
        cut.Render(p =>
        {
            p.Add(x => x.Figure, figure);
            p.Add(x => x.AnimationSource, fake2);
        });
        Assert.Equal(0, fake1.SubscriberCount);
        Assert.Equal(1, fake2.SubscriberCount);
    }

    [Fact]
    public void Dispose_UnsubscribesFromAnimationSource()
    {
        var figure = Plt.Create().Plot([1.0], [2.0]).Build();
        var fake = new FakeAnimationSource();
        var cut = Render<MplChart>(p =>
        {
            p.Add(x => x.Figure, figure);
            p.Add(x => x.AnimationSource, fake);
        });
        cut.Instance.Dispose();
        Assert.Equal(0, fake.SubscriberCount);
    }

    [Fact]
    public void FrameReady_Invokes_OnAnimationFrameReady_UpdatesMarkup()
    {
        // Drives the frame the component shows when its animation source says one is ready.
        var initial = Plt.Create().WithTitle("InitialTitle").Plot([1.0], [1.0]).Build();
        var next = Plt.Create().WithTitle("FrameTwoTitle").Plot([2.0], [2.0]).Build();
        var fake = new FakeAnimationSource();

        var cut = Render<MplChart>(p =>
        {
            p.Add(x => x.Figure, initial);
            p.Add(x => x.AnimationSource, fake);
        });
        Assert.Contains("InitialTitle", cut.Markup);

        fake.Raise(next);

        Assert.Contains("FrameTwoTitle", cut.Markup);
    }

    [Fact]
    public async Task FrameReady_FromAnotherThread_IsShownOnTheComponentsOwnThread()
    {
        var initial = Plt.Create().WithTitle("InitialTitle").Plot([1.0], [1.0]).Build();
        var next = Plt.Create().WithTitle("FrameThreeTitle").Plot([2.0], [2.0]).Build();
        var fake = new FakeAnimationSource();
        var cut = Render<MplChart>(p =>
        {
            p.Add(x => x.Figure, initial);
            p.Add(x => x.AnimationSource, fake);
        });

        await Task.Run(() => fake.Raise(next), Xunit.TestContext.Current.CancellationToken);

        cut.WaitForAssertion(() => Assert.Contains("FrameThreeTitle", cut.Markup));
    }
}
