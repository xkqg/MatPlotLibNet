// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

import * as signalR from '@microsoft/signalr';
import { SvgPartsDocument, type SvgParts } from './svg-parts-document';

/**
 * TypeScript implementation of the IChartSubscriptionClient pattern.
 * Mirrors the C# ChartSubscriptionClient in MatPlotLibNet.Blazor.
 * Connects to a MatPlotLibNet ChartHub via SignalR for real-time chart updates.
 *
 * It subscribes as a viewer that patches: the server sends a chart's whole document once and afterwards only the parts that
 * changed, which are put in place in the last whole document; the handler is always given the whole SVG, as the figure's
 * `ToSvg` writes it. Against a server from before parts existed it subscribes the old way.
 */
export class ChartSubscriptionClient {
  private connection: signalR.HubConnection | null = null;
  private onSvgUpdatedHandler: ((chartId: string, svg: string) => void) | null = null;
  private onChartUpdatedHandler: ((chartId: string, json: string) => void) | null = null;
  private readonly subscribed = new Set<string>();
  private readonly documents = new Map<string, SvgPartsDocument>();

  /** The SignalR connection, which a chart's script reaches the server through, or `null` while not connected. */
  get hub(): signalR.HubConnection | null {
    return this.connection;
  }

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
      if (SvgPartsDocument.isWrapped(svg)) {
        const document = new SvgPartsDocument(svg);
        this.documents.set(chartId, document);
        this.onSvgUpdatedHandler?.(chartId, document.plain);
        return;
      }
      this.documents.delete(chartId);
      this.onSvgUpdatedHandler?.(chartId, svg);
    });

    // Only the parts that changed: put in place in the last whole document, which is handed on whole. A part for a chart
    // held no document of, or one that cannot be placed, is left: the server sends the whole document again when needed.
    this.connection.on('UpdateChartParts', (chartId: string, parts: SvgParts) => {
      const document = this.documents.get(chartId);
      if (document && document.apply(parts)) this.onSvgUpdatedHandler?.(chartId, document.plain);
    });

    this.connection.on('UpdateChart', (chartId: string, json: string) => {
      this.onChartUpdatedHandler?.(chartId, json);
    });

    // A reconnect is a new connection to the server, with none of the groups the old one joined: every chart this
    // client had subscribed to is subscribed to again, so a chart does not go silent for good after a network blip.
    this.connection.onreconnected(async () => {
      for (const chartId of this.subscribed) {
        await this.subscribeOn(chartId);
      }
    });

    await this.connection.start();
  }

  /** Subscribes to updates for the specified chart. */
  async subscribe(chartId: string): Promise<void> {
    await this.subscribeOn(chartId);
    this.subscribed.add(chartId);
  }

  /** Subscribes as a viewer that patches; a server that has no such method is subscribed to the old way. */
  private async subscribeOn(chartId: string): Promise<void> {
    if (!this.connection) return;
    try {
      await this.connection.invoke('SubscribeToParts', chartId);
    } catch {
      await this.connection.invoke('Subscribe', chartId);
    }
  }

  /** Unsubscribes from chart updates. */
  async unsubscribe(chartId: string): Promise<void> {
    this.subscribed.delete(chartId);
    this.documents.delete(chartId);
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
    this.documents.clear();
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
    }
  }
}
