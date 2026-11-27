// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;

namespace MatPlotLibNet.Tests;

/// <summary>The server a native control is wired to in a test: it takes the name of every gesture sent to it, and says whether each
/// control it was wired to sent its gesture through it. The controls of the toolkits share no base type a suite could read, so a suite
/// hands over what it reads of each (<see cref="Wired"/>); every suite of a control links this one file.</summary>
internal sealed class ServerInteractionProbe
{
    private readonly List<string> _sent = [];

    /// <summary>The send delegate the controls are wired with: records the name of the gesture.</summary>
    /// <param name="method">The name of the hub method.</param>
    /// <param name="payload">The gesture, which is not read.</param>
    /// <returns>A task that is already done.</returns>
    internal Task Send(string method, object payload)
    {
        _sent.Add(method);
        return Task.CompletedTask;
    }

    /// <summary>What a suite reads of a control it wired to the probe.</summary>
    /// <param name="IsInteractive">Whether the control says it is interactive now.</param>
    /// <param name="Host">The host the control's gestures go through.</param>
    internal readonly record struct Wired(bool IsInteractive, InteractionHost Host);

    /// <summary>Checks that every control became interactive and that a gesture raised on each one's host was sent, once, as a zoom.</summary>
    /// <param name="controls">The controls the suite wired to this probe.</param>
    internal void AssertEveryGestureWasSent(params Wired[] controls)
    {
        Assert.All(controls, control => Assert.True(control.IsInteractive));
        foreach (var control in controls)
        {
            control.Host.ServerEventSink!(new ZoomEvent("c", 0, 0, 1, 0, 1));
        }

        Assert.Equal(Enumerable.Repeat("OnZoom", controls.Length), _sent);
    }
}
