// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

import { defineComponent, h, ref, watchEffect } from 'vue';
import { useMplLiveChart } from './use-mpl-live-chart';
import { mountSvg } from './mount-svg';

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
    const { svgContent, hub } = useMplLiveChart(props.chartId, props.hubUrl);
    const host = ref<HTMLElement | null>(null);

    // Mounted by hand, not through innerHTML: the interaction script of a server-interactive chart travels inside the
    // SVG, and a script that arrives through innerHTML never runs. The host also carries the connection.
    watchEffect(() => {
      if (host.value) mountSvg(host.value, svgContent.value || props.initialSvg, hub.value ?? undefined);
    }, { flush: 'post' });

    return () =>
      h('div', { class: `mpl-chart mpl-live ${props.cssClass}`.trim() }, [
        h('div', { ref: host }),
      ]);
  },
});
