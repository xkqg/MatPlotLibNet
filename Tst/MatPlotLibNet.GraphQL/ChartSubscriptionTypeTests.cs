// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.GraphQL;

namespace MatPlotLibNet.GraphQL.Tests;

public class ChartSubscriptionTypeTests
{
    [Fact]
    public void OnChartSvgUpdated_ReturnsMessagePayload()
    {
        var message = new ChartEventMessage("test-chart", "<svg>updated</svg>");
        var subscription = new ChartSubscriptionType();

        var result = subscription.OnChartSvgUpdated(message);

        Assert.Equal("<svg>updated</svg>", result);
    }

    [Fact]
    public void OnChartUpdated_ReturnsMessagePayload()
    {
        var message = new ChartEventMessage("test-chart", "{\"title\":\"Live\"}");
        var subscription = new ChartSubscriptionType();

        var result = subscription.OnChartUpdated(message);

        Assert.Equal("{\"title\":\"Live\"}", result);
    }

    [Fact]
    public void ChartEventMessage_StoresChartIdAndPayload()
    {
        var message = new ChartEventMessage("my-chart", "<svg/>");

        Assert.Equal("my-chart", message.ChartId);
        Assert.Equal("<svg/>", message.Payload);
    }
}
