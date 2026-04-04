// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

import * as signalR from '@microsoft/signalr';

/**
 * TypeScript implementation of the IChartSubscriptionClient pattern.
 * Mirrors the C# ChartSubscriptionClient in MatPlotLibNet.Blazor.
 * Connects to a MatPlotLibNet ChartHub via SignalR for real-time chart updates.
 */
export class ChartSubscriptionClient {
  private connection: signalR.HubConnection | null = null;
  private onSvgUpdatedHandler: ((chartId: string, svg: string) => void) | null = null;
  private onChartUpdatedHandler: ((chartId: string, json: string) => void) | null = null;

  /** Whether the client is currently connected to the hub. */
  get isConnected(): boolean {
    return this.connection?.state === signalR.HubConnectionState.Connected;
  }

  /** Connects to the SignalR hub at the specified URL. */
  async connect(hubUrl: string): Promise<void> {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect()
      .build();

    this.connection.on('UpdateChartSvg', (chartId: string, svg: string) => {
      this.onSvgUpdatedHandler?.(chartId, svg);
    });

    this.connection.on('UpdateChart', (chartId: string, json: string) => {
      this.onChartUpdatedHandler?.(chartId, json);
    });

    this.connection.onreconnected(async () => {
      // Re-subscribe logic is handled by the component
    });

    await this.connection.start();
  }

  /** Subscribes to updates for the specified chart. */
  async subscribe(chartId: string): Promise<void> {
    await this.connection?.invoke('Subscribe', chartId);
  }

  /** Unsubscribes from chart updates. */
  async unsubscribe(chartId: string): Promise<void> {
    await this.connection?.invoke('Unsubscribe', chartId);
  }

  /** Registers a callback invoked when an SVG update is received. */
  onSvgUpdated(handler: (chartId: string, svg: string) => void): void {
    this.onSvgUpdatedHandler = handler;
  }

  /** Registers a callback invoked when a JSON chart update is received. */
  onChartUpdated(handler: (chartId: string, json: string) => void): void {
    this.onChartUpdatedHandler = handler;
  }

  /** Disconnects and cleans up the SignalR connection. */
  async dispose(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
    }
  }
}
