// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

import { ref, watch, readonly, type Ref } from 'vue';

/** Return type for the useMplChart composable. */
export interface UseMplChartResult {
  svgContent: Readonly<Ref<string>>;
  loading: Readonly<Ref<boolean>>;
  error: Readonly<Ref<Error | null>>;
}

/**
 * Vue composable that fetches chart SVG from a MatPlotLibNet endpoint.
 * Re-fetches whenever the chartUrl changes.
 *
 * @param chartUrl - The URL of the chart SVG endpoint (reactive ref or plain string).
 * @returns The SVG content, loading state, and any error as readonly refs.
 */
export function useMplChart(chartUrl: Ref<string> | string): UseMplChartResult {
  const svgContent = ref('');
  const loading = ref(false);
  const error = ref<Error | null>(null);
  let controller: AbortController | null = null;

  watch(
    () => (typeof chartUrl === 'string' ? chartUrl : chartUrl.value),
    async (url) => {
      if (!url) return;

      controller?.abort();
      controller = new AbortController();
      loading.value = true;
      error.value = null;

      try {
        const response = await fetch(url, { signal: controller.signal });
        svgContent.value = await response.text();
      } catch (err) {
        if ((err as Error).name !== 'AbortError') {
          error.value = err as Error;
        }
      } finally {
        loading.value = false;
      }
    },
    { immediate: true },
  );

  return {
    svgContent: readonly(svgContent),
    loading: readonly(loading),
    error: readonly(error),
  };
}
