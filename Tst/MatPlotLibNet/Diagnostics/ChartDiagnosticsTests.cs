// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Diagnostics;

namespace MatPlotLibNet.Tests.Diagnostics;

/// <summary>Verifies the process-wide diagnostic event bus used to surface otherwise-silent
/// degraded-mode paths (a swallowed regression-fit failure, a dropped unknown series
/// discriminator on deserialize, ...) without changing the library's default lenient/graceful
/// behavior.</summary>
[Collection("ChartDiagnosticsGlobalState")]
public class ChartDiagnosticsTests
{
    [Fact]
    public void Emit_WithSubscriber_InvokesHandlerWithDiagnostic()
    {
        ChartDiagnostic? received = null;
        void Handler(ChartDiagnostic d) => received = d;
        ChartDiagnostics.Emitted += Handler;
        try
        {
            ChartDiagnostics.Emit(new ChartDiagnostic("Test", "message", null));

            Assert.NotNull(received);
            Assert.Equal("Test", received!.Value.Source);
            Assert.Equal("message", received.Value.Message);
            Assert.Null(received.Value.Exception);
        }
        finally
        {
            ChartDiagnostics.Emitted -= Handler;
        }
    }

    [Fact]
    public void Emit_WithNoSubscribers_DoesNotThrow()
    {
        var ex = Record.Exception(() => ChartDiagnostics.Emit(new ChartDiagnostic("Test", "no subscribers", null)));
        Assert.Null(ex);
    }

    [Fact]
    public void Emit_PropagatesExceptionReference()
    {
        var thrown = new InvalidOperationException("boom");
        ChartDiagnostic? received = null;
        void Handler(ChartDiagnostic d) => received = d;
        ChartDiagnostics.Emitted += Handler;
        try
        {
            ChartDiagnostics.Emit(new ChartDiagnostic("Test", "boom happened", thrown));

            Assert.NotNull(received);
            Assert.Same(thrown, received!.Value.Exception);
        }
        finally
        {
            ChartDiagnostics.Emitted -= Handler;
        }
    }

    [Fact]
    public void Unsubscribe_StopsReceivingDiagnostics()
    {
        int callCount = 0;
        void Handler(ChartDiagnostic d) => callCount++;
        ChartDiagnostics.Emitted += Handler;
        ChartDiagnostics.Emitted -= Handler;

        ChartDiagnostics.Emit(new ChartDiagnostic("Test", "should not be observed", null));

        Assert.Equal(0, callCount);
    }
}
