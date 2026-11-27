# @matplotlibnet/client

The chart client the [MatPlotLibNet](https://github.com/xkqg/MatPlotLibNet) packages for React (`@matplotlibnet/react`),
Vue (`@matplotlibnet/vue`) and Angular (`@matplotlibnet/angular`) share. It holds no framework code:

- `ChartSubscriptionClient` connects to a MatPlotLibNet.AspNetCore `ChartHub` over SignalR, subscribes to charts and hands
  each update over as the whole SVG, putting the parts the server sends in place in the last whole document.
- `mountSvg` puts a chart's SVG in an element and wires the script inside it to the hub.

Each framework package depends on this one and exports the same names, so an application installs its framework package
only:

```
npm install @matplotlibnet/react @microsoft/signalr
```

## Developing the framework packages

The React, Vue and Angular packages read this package from its folder while they are developed: their tests through its
source (`vitest.config.ts`), their type check through its declarations (`tsconfig.json` `paths`). Build it first:
`npm run build` here, then `npm test` or `npm run build` in a framework package.
