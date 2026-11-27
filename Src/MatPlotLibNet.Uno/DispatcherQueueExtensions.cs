// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.UI.Dispatching;

namespace MatPlotLibNet.Uno;

/// <summary>How an Uno element reaches its UI thread: the one hop its host, its follower of a streaming figure and its follower of an
/// animation source take, at the queue's normal priority.</summary>
internal static class DispatcherQueueExtensions
{
    /// <param name="queue">The element's dispatcher queue.</param>
    extension(DispatcherQueue queue)
    {
        /// <summary>Runs <paramref name="work"/> on the queue's thread, later, in the order it was posted.</summary>
        /// <param name="work">What to run on the UI thread.</param>
        internal void Post(Action work) => queue.TryEnqueue(DispatcherQueuePriority.Normal, work.Invoke);
    }
}
