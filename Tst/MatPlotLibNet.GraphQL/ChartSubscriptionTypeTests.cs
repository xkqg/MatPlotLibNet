// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.GraphQL;

namespace MatPlotLibNet.GraphQL.Tests;

/// <summary>Verifies <see cref="ChartSubscriptionType"/> behavior.</summary>
public class ChartSubscriptionTypeTests
{
    /// <summary>Verifies that OnChartSvgUpdated returns the SVG payload from the event message.</summary>
    [Fact]
    public void OnChartSvgUpdated_ReturnsMessagePayload()
    {
        var message = new ChartEventMessage("test-chart", "<svg>updated</svg>");
        var subscription = new ChartSubscriptionType();

        var result = subscription.OnChartSvgUpdated(message);

        Assert.Equal("<svg>updated</svg>", result);
    }

    /// <summary>Verifies that OnChartUpdated returns the JSON payload from the event message.</summary>
    [Fact]
    public void OnChartUpdated_ReturnsMessagePayload()
    {
        var message = new ChartEventMessage("test-chart", "{\"title\":\"Live\"}");
        var subscription = new ChartSubscriptionType();

        var result = subscription.OnChartUpdated(message);

        Assert.Equal("{\"title\":\"Live\"}", result);
    }

    /// <summary>Verifies that ChartEventMessage correctly stores the chart ID and payload.</summary>
    [Fact]
    public void ChartEventMessage_StoresChartIdAndPayload()
    {
        var message = new ChartEventMessage("my-chart", "<svg/>");

        Assert.Equal("my-chart", message.ChartId);
        Assert.Equal("<svg/>", message.Payload);
    }
}
