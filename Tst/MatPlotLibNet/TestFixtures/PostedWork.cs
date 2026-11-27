// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests;

/// <summary>A stand-in for a control's thread: the work posted waits here until the test runs it.</summary>
internal sealed class PostedWork
{
    /// <summary>The work posted and not yet run, in the order it was posted.</summary>
    public readonly Queue<Action> Waiting = new();

    /// <summary>Keeps a piece of work until <see cref="RunAll"/>.</summary>
    /// <param name="work">The work.</param>
    public void Post(Action work) => Waiting.Enqueue(work);

    /// <summary>Runs everything that waits, work posted by the work itself included.</summary>
    public void RunAll()
    {
        while (Waiting.Count > 0)
        {
            Waiting.Dequeue()();
        }
    }
}
