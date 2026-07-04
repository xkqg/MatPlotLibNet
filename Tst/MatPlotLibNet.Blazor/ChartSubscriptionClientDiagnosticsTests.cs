// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Net;
using MatPlotLibNet.Diagnostics;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging.Abstractions;

namespace MatPlotLibNet.Blazor.Tests;

/// <summary>Serializes every test in this assembly that subscribes to the process-global
/// <see cref="ChartDiagnostics.Emitted"/> event. xunit test collections are scoped per assembly, so
/// this mirrors (does not share state with) the sibling <c>ChartDiagnosticsGlobalState</c> collection
/// defined in the MatPlotLibNet.Tests assembly.</summary>
[CollectionDefinition("ChartDiagnosticsGlobalState", DisableParallelization = true)]
public sealed class ChartDiagnosticsGlobalStateCollection
{
}

/// <summary>Council fix — <see cref="ChartSubscriptionClient"/>'s private DisposeCurrentHubAsync used
/// a bare <c>catch { }</c> around <c>hub.DisposeAsync()</c> to swallow dispose-time races (an
/// already-closing/closed hub during teardown, e.g. a second ConnectAsync tearing down the prior hub
/// concurrently with the hub's own shutdown). That swallow is legitimate behavior — dispose must
/// never throw — but it was completely invisible. The catch now reports the race via
/// <see cref="ChartDiagnostics"/> (Source <c>"ChartSubscriptionClient"</c>) before continuing to
/// suppress it, so operators can see teardown races without changing the graceful-degrade contract.
/// </summary>
[Collection("ChartDiagnosticsGlobalState")]
public class ChartSubscriptionClientDiagnosticsTests
{
    /// <summary>Drives a real dispose-time exception through <see cref="ChartSubscriptionClient"/>'s
    /// internal hub-factory test seam (the same seam <c>ChartSubscriptionClientHubTests</c> uses):
    /// the injected <see cref="HubConnection"/>'s <c>DisposeAsync</c> throws, simulating the
    /// already-closing/closed-hub race. <see cref="ChartSubscriptionClient.DisposeAsync"/> must still
    /// not throw, and must emit a diagnostic naming the failure.</summary>
    [Fact]
    public async Task DisposeCurrentHub_OnDisposeRace_EmitsDiagnostic()
    {
        ChartDiagnostic? received = null;
        void Handler(ChartDiagnostic d) => received = d;
        ChartDiagnostics.Emitted += Handler;
        try
        {
            var client = new ChartSubscriptionClient(
                hubUrl => new ThrowingDisposeHubConnection(),
                ChartSubscriptionClient.RegisterHandler);

            await client.ConnectAsync("http://localhost/stub", TestContext.Current.CancellationToken);

            var ex = await Record.ExceptionAsync(() => client.DisposeAsync().AsTask());

            Assert.Null(ex);
            Assert.NotNull(received);
            Assert.Equal("ChartSubscriptionClient", received!.Value.Source);
            Assert.NotNull(received.Value.Exception);
        }
        finally
        {
            ChartDiagnostics.Emitted -= Handler;
        }
    }

    /// <summary>A test-only <see cref="HubConnection"/> whose <see cref="StartAsync"/> is overridden
    /// as a no-op (so no real transport/protocol negotiation is ever attempted — <c>DisposeAsync</c>
    /// is virtual, this is the production-code seam being exercised, not a mock of it) and whose
    /// <see cref="DisposeAsync"/> always throws, reproducing a dispose-time race without needing a
    /// live SignalR connection. The base constructor's dependencies (connection factory, protocol,
    /// endpoint, service provider) are never actually invoked because <c>StartAsync</c> short-circuits
    /// before any of them would be used — they exist only to satisfy the base constructor's non-null
    /// argument checks.</summary>
    private sealed class ThrowingDisposeHubConnection : HubConnection
    {
        public ThrowingDisposeHubConnection()
            : base(new StubConnectionFactory(), new StubHubProtocol(), new UriEndPoint(new Uri("http://localhost/stub")), new StubServiceProvider(), NullLoggerFactory.Instance)
        {
        }

        public override Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public override ValueTask DisposeAsync() =>
            throw new ObjectDisposedException(nameof(ThrowingDisposeHubConnection), "Simulated dispose-time race.");
    }

    private sealed class StubServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    private sealed class StubConnectionFactory : IConnectionFactory
    {
        public ValueTask<ConnectionContext> ConnectAsync(EndPoint endpoint, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Never invoked: StartAsync is overridden as a no-op.");
    }

    private sealed class StubHubProtocol : IHubProtocol
    {
        public string Name => "stub";
        public int Version => 1;
        public TransferFormat TransferFormat => TransferFormat.Text;
        public bool IsVersionSupported(int version) => true;
        public bool TryParseMessage(ref ReadOnlySequence<byte> input, IInvocationBinder binder, out HubMessage message) =>
            throw new NotSupportedException("Never invoked: StartAsync is overridden as a no-op.");
        public void WriteMessage(HubMessage message, IBufferWriter<byte> output) =>
            throw new NotSupportedException("Never invoked: StartAsync is overridden as a no-op.");
        public ReadOnlyMemory<byte> GetMessageBytes(HubMessage message) =>
            throw new NotSupportedException("Never invoked: StartAsync is overridden as a no-op.");
    }
}
