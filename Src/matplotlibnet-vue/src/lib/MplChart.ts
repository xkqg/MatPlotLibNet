// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

import { defineComponent, h, toRef } from 'vue';
import { useMplChart } from './use-mpl-chart';

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

    return () =>
      h('div', { class: `mpl-chart ${props.cssClass}`.trim() }, [
        h('div', { innerHTML: svgContent.value }),
      ]);
  },
});
