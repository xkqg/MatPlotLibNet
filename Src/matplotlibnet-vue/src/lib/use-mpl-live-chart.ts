// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

import { ref, readonly, onMounted, onUnmounted, type Ref } from 'vue';
import { ChartSubscriptionClient } from './chart-subscription.client';

/** Return type for the useMplLiveChart composable. */
export interface UseMplLiveChartResult {
  svgContent: Readonly<Ref<string>>;
  isConnected: Readonly<Ref<boolean>>;
}

/**
 * Vue composable that subscribes to real-time chart updates via SignalR.
 *
 * @param chartId - The identifier of the chart to subscribe to.
 * @param hubUrl - The SignalR hub URL (defaults to "/charts-hub").
 * @returns The current SVG content and connection state as readonly refs.
 */
export function useMplLiveChart(chartId: string, hubUrl: string = '/charts-hub'): UseMplLiveChartResult {
  const svgContent = ref('');
  const isConnected = ref(false);
  const client = new ChartSubscriptionClient();

  onMounted(async () => {
    client.onSvgUpdated((id, svg) => {
      if (id === chartId) {
        svgContent.value = svg;
      }
    });

    await client.connect(hubUrl);
    isConnected.value = client.isConnected;
    await client.subscribe(chartId);
  });

  onUnmounted(async () => {
    await client.unsubscribe(chartId);
    await client.dispose();
    isConnected.value = false;
  });

  return {
    svgContent: readonly(svgContent),
    isConnected: readonly(isConnected),
  };
}
