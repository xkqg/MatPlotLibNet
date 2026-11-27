// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;

namespace MatPlotLibNet.AspNetCore.Tests;

/// <summary>A handler of <see cref="ChartSessionOptions"/> (a brush selection, a hover, a data cursor, the one for every event) is registered fluently, and a null
/// one is refused rather than stored, so the failure names the argument instead of arriving later as a gesture that does nothing.</summary>
public class ChartSessionOptionsTests
{
    public static TheoryData<string, Func<ChartSessionOptions, ChartSessionOptions>, Func<ChartSessionOptions, ChartSessionOptions>> Handlers => new()
    {
        { "brush select", opts => opts.OnBrushSelect(_ => default), opts => opts.OnBrushSelect(null!) },
        { "hover", opts => opts.OnHover(_ => new ValueTask<string?>("tooltip")), opts => opts.OnHover(null!) },
        { "data cursor", opts => opts.OnDataCursor(_ => default), opts => opts.OnDataCursor(null!) },
        { "every event", opts => opts.Handle(new RecordingChartEventHandler()), opts => opts.Handle(null!) },
    };

    [Theory]
    [MemberData(nameof(Handlers))]
    public void AHandler_ReturnsTheSameOptions_AndANullOneIsRefused(string handler,
        Func<ChartSessionOptions, ChartSessionOptions> register, Func<ChartSessionOptions, ChartSessionOptions> registerNull)
    {
        var opts = new ChartSessionOptions();

        Assert.Same(opts, register(opts));
        Assert.Equal("handler", Assert.Throws<ArgumentNullException>(() => registerNull(opts)).ParamName);
        _ = handler;
    }
}
