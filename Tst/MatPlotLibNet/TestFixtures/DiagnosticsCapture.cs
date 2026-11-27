// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Diagnostics;

namespace MatPlotLibNet.Tests;

/// <summary>What a piece of work returned and what the library announced while it ran.</summary>
/// <typeparam name="T">The type of the result.</typeparam>
/// <param name="Result">What the work returned.</param>
/// <param name="Diagnostics">The diagnostics announced during the work, in order.</param>
public readonly record struct Captured<T>(T Result, IReadOnlyList<ChartDiagnostic> Diagnostics);

/// <summary>The one way a test listens to <see cref="ChartDiagnostics.Emitted"/>: it subscribes when the capture is made and lets go
/// when it is disposed, so a forgotten subscription cannot outlive the test. The event is process-wide, so a test class that listens
/// belongs to the <c>ChartDiagnosticsGlobalState</c> collection.</summary>
public sealed class DiagnosticsCapture : IDisposable
{
    private readonly object _gate = new();

    private readonly List<ChartDiagnostic> _seen = [];
    private readonly List<(string Source, TaskCompletionSource<ChartDiagnostic> Waiting)> _waiting = [];

    /// <summary>Starts listening.</summary>
    public DiagnosticsCapture() => ChartDiagnostics.Emitted += Collect;

    /// <summary>What has been announced since the capture was made, in order.</summary>
    public IReadOnlyList<ChartDiagnostic> Seen
    {
        get
        {
            lock (_gate)
            {
                return [.. _seen];
            }
        }
    }

    /// <summary>The last diagnostic announced, or <see langword="null"/> when none has been.</summary>
    public ChartDiagnostic? Last
    {
        get
        {
            lock (_gate)
            {
                return _seen.Count == 0 ? null : _seen[^1];
            }
        }
    }

    /// <summary>The first diagnostic announced by <paramref name="source"/>, already seen or still to come.</summary>
    /// <param name="source">The announcing component's name.</param>
    /// <returns>A task that completes with that diagnostic.</returns>
    public Task<ChartDiagnostic> WhenAnnounced(string source)
    {
        var waiting = new TaskCompletionSource<ChartDiagnostic>(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_gate)
        {
            foreach (var seen in _seen)
            {
                if (seen.Source == source)
                {
                    waiting.TrySetResult(seen);
                    return waiting.Task;
                }
            }

            _waiting.Add((source, waiting));
        }

        return waiting.Task;
    }

    /// <summary>Lets go of the event.</summary>
    public void Dispose() => ChartDiagnostics.Emitted -= Collect;

    /// <summary>Runs <paramref name="work"/> and returns what the library announced while it ran.</summary>
    /// <param name="work">The work.</param>
    /// <returns>The diagnostics, in order.</returns>
    public static IReadOnlyList<ChartDiagnostic> During(Action work)
    {
        using var capture = new DiagnosticsCapture();
        work();
        return capture.Seen;
    }

    /// <summary>Runs <paramref name="work"/> and returns what it returned and what the library announced while it ran.</summary>
    /// <typeparam name="T">The type the work returns.</typeparam>
    /// <param name="work">The work.</param>
    /// <returns>The result and the diagnostics.</returns>
    public static Captured<T> Of<T>(Func<T> work)
    {
        using var capture = new DiagnosticsCapture();
        var result = work();
        return new Captured<T>(result, capture.Seen);
    }

    /// <summary>Runs <paramref name="work"/> to its end and returns what the library announced meanwhile.</summary>
    /// <param name="work">The work.</param>
    /// <returns>The diagnostics, in order.</returns>
    public static async Task<IReadOnlyList<ChartDiagnostic>> DuringAsync(Func<Task> work)
    {
        using var capture = new DiagnosticsCapture();
        await work();
        return capture.Seen;
    }

    private void Collect(ChartDiagnostic diagnostic)
    {
        lock (_gate)
        {
            _seen.Add(diagnostic);
            foreach (var (source, waiting) in _waiting.ToArray())
            {
                if (source == diagnostic.Source && waiting.TrySetResult(diagnostic))
                {
                    _waiting.RemoveAll(w => w.Waiting == waiting);
                }
            }
        }
    }
}
