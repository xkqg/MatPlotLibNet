// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR.Client;

namespace MatPlotLibNet.Tests;

/// <summary>One reader of a registered chart: a connection to the chart hub that is started and subscribed to the chart, and the two
/// texts the hub sends back to it, the republished SVG and the tooltip a hover asks for, each in an <see cref="Inbox"/>.</summary>
internal sealed class ChartViewer
{
    /// <summary>How long a test waits for something the server sends before it fails.</summary>
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(5);

    /// <summary>How long a test lets the server send something it must not, before it says nothing came.</summary>
    private static readonly TimeSpan Silence = TimeSpan.FromMilliseconds(250);

    private ChartViewer(HubConnection connection)
    {
        Connection = connection;
        connection.On<string, string>("UpdateChartSvg", Svgs.Arrive);
        connection.On<string, string>("ReceiveTooltipContent", Tooltips.Arrive);
    }

    /// <summary>The connection to the hub; the test invokes the hub's methods on it.</summary>
    internal HubConnection Connection { get; }

    /// <summary>The SVG the server republished for this reader, in the order it arrived.</summary>
    internal Inbox Svgs { get; } = new();

    /// <summary>The tooltip the server sent to this reader for a hover, in the order it arrived.</summary>
    internal Inbox Tooltips { get; } = new();

    /// <summary>Connects a reader to the hub of <paramref name="host"/>, starts the connection and subscribes it to the chart.</summary>
    /// <param name="connection">The connection, not yet started.</param>
    /// <param name="chartId">The chart to subscribe to.</param>
    /// <param name="ct">Cancels the connecting.</param>
    /// <returns>The reader.</returns>
    internal static async Task<ChartViewer> JoinAsync(HubConnection connection, string chartId, CancellationToken ct)
    {
        var viewer = new ChartViewer(connection);
        await connection.StartAsync(ct);
        await connection.InvokeAsync("Subscribe", chartId, cancellationToken: ct);
        return viewer;
    }

    /// <summary>Invokes a hub method on this reader's connection, as the page does for a gesture, and waits for the SVG the server
    /// republishes for it.</summary>
    /// <param name="method">The name of the hub method.</param>
    /// <param name="payload">The gesture.</param>
    /// <param name="ct">Cancels the wait.</param>
    /// <returns>The republished SVG.</returns>
    internal async Task<string> GestureAsync(string method, object payload, CancellationToken ct)
    {
        await Connection.InvokeAsync(method, payload, cancellationToken: ct);
        return (await Svgs.NextAsync(ct)).Text;
    }

    /// <summary>What the server sent to a reader under one callback, in order: <see cref="NextAsync"/> hands out the next one and fails when none
    /// comes in time; <see cref="NothingArrivesAsync"/> says none came while the server had time to send one.</summary>
    internal sealed class Inbox
    {
        private readonly Channel<(string ChartId, string Text)> _arrived = Channel.CreateUnbounded<(string, string)>();

        /// <summary>Takes in what the server sent.</summary>
        /// <param name="chartId">The chart the text is about.</param>
        /// <param name="text">The text.</param>
        internal void Arrive(string chartId, string text) => _arrived.Writer.TryWrite((chartId, text));

        /// <summary>The next thing the server sent that no call has taken yet.</summary>
        /// <param name="ct">Cancels the wait.</param>
        /// <returns>The chart and the text.</returns>
        /// <exception cref="TimeoutException">Nothing arrived in time.</exception>
        internal async Task<(string ChartId, string Text)> NextAsync(CancellationToken ct) =>
            await _arrived.Reader.ReadAsync(ct).AsTask().WaitAsync(Patience, ct);

        /// <summary>Gives the server time to send something it must not, and fails when it did.</summary>
        /// <param name="because">What went wrong when something came.</param>
        /// <param name="ct">Cancels the wait.</param>
        internal async Task NothingArrivesAsync(string because, CancellationToken ct)
        {
            await Task.Delay(Silence, ct);
            Assert.False(_arrived.Reader.TryPeek(out _), because);
        }
    }
}
