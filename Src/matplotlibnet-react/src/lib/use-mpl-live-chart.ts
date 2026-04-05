// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

import { useState, useEffect } from 'react';
import { ChartSubscriptionClient } from './chart-subscription.client';

/** Return type for the useMplLiveChart hook. */
export interface UseMplLiveChartResult {
  svgContent: string;
  isConnected: boolean;
}

/**
 * React hook that subscribes to real-time chart updates via SignalR.
 *
 * @param chartId - The identifier of the chart to subscribe to.
 * @param hubUrl - The SignalR hub URL (defaults to "/charts-hub").
 * @returns The current SVG content and connection state.
 */
export function useMplLiveChart(chartId: string, hubUrl: string = '/charts-hub'): UseMplLiveChartResult {
  const [svgContent, setSvgContent] = useState('');
  const [isConnected, setIsConnected] = useState(false);

  useEffect(() => {
    const client = new ChartSubscriptionClient();

    client.onSvgUpdated((id, svg) => {
      if (id === chartId) {
        setSvgContent(svg);
      }
    });

    client.connect(hubUrl).then(() => {
      setIsConnected(client.isConnected);
      return client.subscribe(chartId);
    });

    return () => {
      client.unsubscribe(chartId)
        .then(() => client.dispose())
        .then(() => setIsConnected(false));
    };
  }, [chartId, hubUrl]);

  return { svgContent, isConnected };
}
