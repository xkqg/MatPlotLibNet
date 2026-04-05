// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

import { describe, it, expect, vi, beforeEach } from 'vitest';
import { ChartSubscriptionClient } from '../lib/chart-subscription.client';

const mockStart = vi.fn().mockResolvedValue(undefined);
const mockStop = vi.fn().mockResolvedValue(undefined);
const mockInvoke = vi.fn().mockResolvedValue(undefined);
const mockOn = vi.fn();
const mockOnReconnected = vi.fn();

vi.mock('@microsoft/signalr', () => {
  return {
    HubConnectionBuilder: vi.fn().mockImplementation(() => ({
      withUrl: vi.fn().mockReturnThis(),
      withAutomaticReconnect: vi.fn().mockReturnThis(),
      build: vi.fn().mockReturnValue({
        start: mockStart,
        stop: mockStop,
        invoke: mockInvoke,
        on: mockOn,
        onreconnected: mockOnReconnected,
        state: 'Connected',
      }),
    })),
    HubConnectionState: { Connected: 'Connected' },
  };
});

describe('ChartSubscriptionClient', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('connects to the hub and starts the connection', async () => {
    const client = new ChartSubscriptionClient();
    await client.connect('/charts-hub');

    expect(mockStart).toHaveBeenCalledOnce();
  });

  it('reports isConnected after connect', async () => {
    const client = new ChartSubscriptionClient();
    expect(client.isConnected).toBe(false);

    await client.connect('/charts-hub');

    expect(client.isConnected).toBe(true);
  });

  it('subscribes by invoking Subscribe on the hub', async () => {
    const client = new ChartSubscriptionClient();
    await client.connect('/charts-hub');
    await client.subscribe('sensor-1');

    expect(mockInvoke).toHaveBeenCalledWith('Subscribe', 'sensor-1');
  });

  it('unsubscribes by invoking Unsubscribe on the hub', async () => {
    const client = new ChartSubscriptionClient();
    await client.connect('/charts-hub');
    await client.unsubscribe('sensor-1');

    expect(mockInvoke).toHaveBeenCalledWith('Unsubscribe', 'sensor-1');
  });

  it('registers UpdateChartSvg and UpdateChart handlers on connect', async () => {
    const client = new ChartSubscriptionClient();
    await client.connect('/charts-hub');

    expect(mockOn).toHaveBeenCalledWith('UpdateChartSvg', expect.any(Function));
    expect(mockOn).toHaveBeenCalledWith('UpdateChart', expect.any(Function));
  });

  it('invokes onSvgUpdated callback when SVG update is received', async () => {
    const handler = vi.fn();
    const client = new ChartSubscriptionClient();
    client.onSvgUpdated(handler);
    await client.connect('/charts-hub');

    const svgCallback = mockOn.mock.calls.find((c: string[]) => c[0] === 'UpdateChartSvg')![1];
    svgCallback('chart-1', '<svg>test</svg>');

    expect(handler).toHaveBeenCalledWith('chart-1', '<svg>test</svg>');
  });

  it('stops the connection on dispose', async () => {
    const client = new ChartSubscriptionClient();
    await client.connect('/charts-hub');
    await client.dispose();

    expect(mockStop).toHaveBeenCalledOnce();
  });
});
