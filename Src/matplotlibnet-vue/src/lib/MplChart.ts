// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

import { defineComponent, h, ref, toRef, watchEffect } from 'vue';
import { useMplChart } from './use-mpl-chart';
import { mountSvg } from './mount-svg';

/**
 * Static chart component that fetches SVG from a MatPlotLibNet endpoint and renders it inline.
 * Mirrors MplChart.razor from MatPlotLibNet.Blazor.
 *
 * Usage:
 * ```vue
 * <MplChart chartUrl="/api/chart.svg" cssClass="my-chart" />
 * ```
 */
export const MplChart = defineComponent({
  name: 'MplChart',
  props: {
    chartUrl: { type: String, required: true },
    cssClass: { type: String, default: '' },
  },
  setup(props) {
    const { svgContent } = useMplChart(toRef(props, 'chartUrl'));
    const host = ref<HTMLElement | null>(null);

    // Mounted so that the scripts of an interactive chart run (a script that arrives through innerHTML never does).
    watchEffect(() => {
      if (host.value) mountSvg(host.value, svgContent.value);
    }, { flush: 'post' });

    return () =>
      h('div', { class: `mpl-chart ${props.cssClass}`.trim() }, [
        h('div', { ref: host }),
      ]);
  },
});
