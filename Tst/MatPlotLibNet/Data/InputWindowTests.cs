// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;

namespace MatPlotLibNet.Tests.Data;

/// <summary>The one ring the library keeps the last inputs of a series in: <see cref="RingBuffer{T}"/> is it behind a lock, the
/// windowed streaming indicators, the running mean and the Hilbert filter keep their inputs in it. These hold its arithmetic: where
/// the oldest is, what a place back reads, and how a window is copied out in the order the inputs came.</summary>
public sealed class InputWindowTests
{
    [Fact]
    public void ANewWindow_HoldsNothing_AndIsAsWideAsItWasMade()
    {
        var window = new InputWindow<double>(3);

        Assert.Equal(3, window.Width);
        Assert.Equal(0, window.Count);
        Assert.False(window.IsFull);
    }

    [Fact]
    public void AWidthBelowOne_IsRefused() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new InputWindow<double>(0));

    [Fact]
    public void WhileFilling_TheOldestIsTheFirstInput_AndPlacesCountFromIt()
    {
        var window = new InputWindow<int>(4);
        window.Push(10);
        window.Push(11);

        Assert.Equal(2, window.Count);
        Assert.Equal(10, window.Oldest);
        Assert.Equal(10, window[0]);
        Assert.Equal(11, window[1]);
    }

    [Fact]
    public void OnceFull_APushEvictsTheOldest_AndTheOrderFollowsTheInputs()
    {
        var window = new InputWindow<int>(3);
        foreach (int value in new[] { 1, 2, 3, 4, 5 }) window.Push(value);

        Assert.True(window.IsFull);
        Assert.Equal(3, window.Count);
        Assert.Equal(3, window.Oldest);
        Assert.Equal([3, 4, 5], [window[0], window[1], window[2]]);
    }

    [Fact]
    public void Back_ReadsAPlaceBeforeTheNewest_AndTheRestingValueWhereNoInputHasCome()
    {
        var window = new InputWindow<double>(4);
        window.Push(1);
        window.Push(2);

        Assert.Equal(2, window.Back(0));
        Assert.Equal(1, window.Back(1));
        Assert.Equal(0, window.Back(2));
        Assert.Equal(0, window.Back(3));

        foreach (double value in new double[] { 3, 4, 5 }) window.Push(value);
        Assert.Equal([5, 4, 3, 2], [window.Back(0), window.Back(1), window.Back(2), window.Back(3)]);
    }

    [Fact]
    public void CopyTo_WritesTheHeldInputsOldestFirst_InOneRunAndInTwo()
    {
        var window = new InputWindow<int>(4);
        var copy = new int[4];
        foreach (int value in new[] { 1, 2, 3 }) window.Push(value);
        window.CopyTo(copy);
        Assert.Equal([1, 2, 3, 0], copy);

        foreach (int value in new[] { 4, 5, 6 }) window.Push(value);
        window.CopyTo(copy);
        Assert.Equal([3, 4, 5, 6], copy);
    }

    [Fact]
    public void Slots_AreTheInputsInTheOrderOfTheirPlaces_ForASumWhoseOrderMustStay()
    {
        var window = new InputWindow<int>(3);
        foreach (int value in new[] { 1, 2, 3, 4 }) window.Push(value);

        Assert.Equal([4, 2, 3], window.Slots.ToArray());
    }

    [Fact]
    public void PushRange_KeepsTheTail_WhenItIsLongerThanTheWindow_AndAppendsInOrderWhenItIsNot()
    {
        var window = new InputWindow<int>(3);
        window.PushRange([1, 2, 3, 4, 5]);
        Assert.Equal(3, window.Count);
        Assert.Equal([3, 4, 5], [window[0], window[1], window[2]]);

        window.PushRange([6]);
        Assert.Equal([4, 5, 6], [window[0], window[1], window[2]]);
    }

    [Fact]
    public void Fold_WalksTheHeldInputsOldestFirst()
    {
        var window = new InputWindow<int>(3);
        foreach (int value in new[] { 1, 2, 3, 4 }) window.Push(value);

        Assert.Equal("234", window.Fold("", static (text, value) => text + value));
    }

    [Fact]
    public void Clear_ForgetsEveryInput_AndReleasesTheirSlots()
    {
        var window = new InputWindow<string>(2);
        window.Push("a");
        window.Push("b");
        window.Clear();

        Assert.Equal(0, window.Count);
        Assert.All(window.Slots.ToArray(), Assert.Null);
        window.Push("c");
        Assert.Equal("c", window.Oldest);
    }
}
