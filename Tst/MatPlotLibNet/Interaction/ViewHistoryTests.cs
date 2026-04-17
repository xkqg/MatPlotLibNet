// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;

namespace MatPlotLibNet.Tests.Interaction;

public sealed class ViewHistoryTests
{
    [Fact]
    public void InitialState_CannotGoBackOrForward()
    {
        var mgr = new ViewHistoryManager();
        Assert.False(mgr.CanGoBack);
        Assert.False(mgr.CanGoForward);
    }

    [Fact]
    public void Push_IncrementsCount()
    {
        var mgr = new ViewHistoryManager();
        mgr.Push(0, 10, 0, 100);
        Assert.Equal(1, mgr.Count);
    }

    [Fact]
    public void AfterTwoPushes_CanGoBack()
    {
        var mgr = new ViewHistoryManager();
        mgr.Push(0, 10, 0, 100);
        mgr.Push(2, 8, 20, 80);
        Assert.True(mgr.CanGoBack);
    }

    [Fact]
    public void Back_ReturnsPreviousView()
    {
        var mgr = new ViewHistoryManager();
        mgr.Push(0, 10, 0, 100);
        mgr.Push(2, 8, 20, 80);
        var prev = mgr.Back();
        Assert.NotNull(prev);
        Assert.Equal(0, prev.Value.XMin);
        Assert.Equal(10, prev.Value.XMax);
    }

    [Fact]
    public void Forward_AfterBack_ReturnsNextView()
    {
        var mgr = new ViewHistoryManager();
        mgr.Push(0, 10, 0, 100);
        mgr.Push(2, 8, 20, 80);
        mgr.Back();
        var fwd = mgr.Forward();
        Assert.NotNull(fwd);
        Assert.Equal(2, fwd.Value.XMin);
    }

    [Fact]
    public void PushAfterBack_ClearsForwardHistory()
    {
        var mgr = new ViewHistoryManager();
        mgr.Push(0, 10, 0, 100);
        mgr.Push(2, 8, 20, 80);
        mgr.Back();
        mgr.Push(1, 9, 10, 90); // should clear forward
        Assert.False(mgr.CanGoForward);
    }

    [Fact]
    public void OverflowCap_At50()
    {
        var mgr = new ViewHistoryManager();
        for (int i = 0; i < 60; i++)
            mgr.Push(i, i + 10, 0, 100);
        Assert.Equal(50, mgr.Count);
    }

    [Fact]
    public void Back_AtBeginning_ReturnsNull()
    {
        var mgr = new ViewHistoryManager();
        mgr.Push(0, 10, 0, 100);
        Assert.Null(mgr.Back());
    }

    [Fact]
    public void Forward_AtEnd_ReturnsNull()
    {
        var mgr = new ViewHistoryManager();
        mgr.Push(0, 10, 0, 100);
        Assert.Null(mgr.Forward());
    }
}
