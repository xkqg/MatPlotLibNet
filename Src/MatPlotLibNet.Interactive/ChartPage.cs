// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using System.Net;

namespace MatPlotLibNet.Interactive;

/// <summary>Generates a self-contained HTML page for displaying a chart with live SignalR updates.</summary>
internal static class ChartPage
{
    /// <summary>Generates the HTML page containing the chart SVG and SignalR client script.</summary>
    /// <param name="chartId">The chart identifier for SignalR subscription.</param>
    /// <param name="initialSvg">The initial SVG content to embed.</param>
    /// <param name="port">The local server port for the SignalR hub URL.</param>
    /// <returns>A complete HTML document string.</returns>
    public static string Generate(string chartId, string initialSvg, int port)
    {
        var encodedChartId = WebUtility.HtmlEncode(chartId);

        return $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8" />
                <meta name="viewport" content="width=device-width, initial-scale=1" />
                <title>MatPlotLibNet - {{encodedChartId}}</title>
                <style>
                    * { margin: 0; padding: 0; box-sizing: border-box; }
                    body {
                        display: flex;
                        justify-content: center;
                        align-items: center;
                        min-height: 100vh;
                        background: #fafafa;
                        font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
                    }
                    #chart-container {
                        background: white;
                        border-radius: 8px;
                        box-shadow: 0 2px 8px rgba(0,0,0,0.1);
                        padding: 16px;
                    }
                    #chart-container svg { display: block; }
                    #status {
                        position: fixed;
                        bottom: 8px;
                        right: 8px;
                        font-size: 12px;
                        color: #999;
                    }
                </style>
            </head>
            <body>
                <div id="chart-container">{{initialSvg}}</div>
                <div id="status">Connected</div>

                <script src="/js/signalr.min.js"></script>
                <script>
                    const chartId = "{{encodedChartId}}";
                    const container = document.getElementById("chart-container");
                    const status = document.getElementById("status");

                    const connection = new signalR.HubConnectionBuilder()
                        .withUrl("http://localhost:{{port}}/charts-hub")
                        .withAutomaticReconnect()
                        .build();

                    connection.on("UpdateChartSvg", (id, svg) => {
                        if (id === chartId) {
                            container.innerHTML = svg;
                        }
                    });

                    connection.onreconnecting(() => { status.textContent = "Reconnecting..."; });
                    connection.onreconnected(() => {
                        status.textContent = "Connected";
                        connection.invoke("Subscribe", chartId);
                    });
                    connection.onclose(() => { status.textContent = "Disconnected"; });

                    async function start() {
                        try {
                            await connection.start();
                            await connection.invoke("Subscribe", chartId);
                            status.textContent = "Connected";
                        } catch (err) {
                            status.textContent = "Connection failed";
                            setTimeout(start, 3000);
                        }
                    }

                    start();
                </script>
            </body>
            </html>
            """;
    }
}
