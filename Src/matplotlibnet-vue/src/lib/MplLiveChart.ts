// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

import { defineComponent, h } from 'vue';
import { useMplLiveChart } from './use-mpl-live-chart';

/**
 * Live chart component that receives real-time updates via SignalR.
 * Mirrors MplLiveChart.razor from MatPlotLibNet.Blazor.
 *
 * Usage:
 * ```vue
 * <MplLiveChart chartId="sensor-1" hubUrl="/charts-hub" cssClass="live" />
 * ```
 */
export const MplLiveChart = defineComponent({
  name: 'MplLiveChart',
  props: {
    chartId: { type: String, required: true },
    hubUrl: { type: String, default: '/charts-hub' },
    cssClass: { type: String, default: '' },
    initialSvg: { type: String, default: '' },
  },
  setup(props) {
    const { svgContent } = useMplLiveChart(props.chartId, props.hubUrl);

    return () => {
      const displaySvg = svgContent.value || props.initialSvg;
      return h('div', { class: `mpl-chart mpl-live ${props.cssClass}`.trim() }, [
        h('div', { innerHTML: displaySvg }),
      ]);
    };
  },
});
