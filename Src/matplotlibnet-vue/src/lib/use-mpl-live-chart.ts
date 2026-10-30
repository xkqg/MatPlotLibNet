// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

import { ref, readonly, onMounted, onUnmounted, type Ref } from 'vue';
import { ChartSubscriptionClient } from './chart-subscription.client';
import type { MplHub } from './mount-svg';

/** Return type for the useMplLiveChart composable. */
export interface UseMplLiveChartResult {
  svgContent: Readonly<Ref<string>>;
  isConnected: Readonly<Ref<boolean>>;
  /** The connection the chart's script sends its gestures through, or `null` until connected. */
  hub: Readonly<Ref<MplHub | null>>;
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
  const hub = ref<MplHub | null>(null);
  const client = new ChartSubscriptionClient();

  onMounted(async () => {
    client.onSvgUpdated((id, svg) => {
      if (id === chartId) {
        svgContent.value = svg;
      }
    });

    await client.connect(hubUrl);
    isConnected.value = client.isConnected;
    hub.value = (client.hub as MplHub | null) ?? null;
    await client.subscribe(chartId);
  });

  onUnmounted(async () => {
    await client.unsubscribe(chartId);
    await client.dispose();
    isConnected.value = false;
    hub.value = null;
  });

  return {
    svgContent: readonly(svgContent),
    isConnected: readonly(isConnected),
    hub: readonly(hub) as Readonly<Ref<MplHub | null>>,
  };
}
