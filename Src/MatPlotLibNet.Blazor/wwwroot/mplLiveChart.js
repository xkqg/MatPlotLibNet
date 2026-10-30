// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

// The page side of MplLiveChart. The server-interactive scripts inside a chart's SVG reach the server through
// a hub connection they find on the chart's host element (host.__mplHub); here that connection is a proxy whose
// invoke() goes through the component to the one SignalR connection the .NET client holds, and whose on()
// collects the handlers the script registers for the messages the server sends about the other viewers.

// Makes the host element the home of the proxy and hands the component the moments a chart goes idle.
export function attach(host, bridge) {
    const handlers = {};
    host.__mplHandlers = handlers;
    host.__mplHub = {
        // A rejected send (a connection that is down, a circuit that is closing) must not surface in the page.
        invoke: (method, payload) => bridge.invokeMethodAsync('Send', method, payload).catch(() => { }),
        on: (name, fn) => { (handlers[name] || (handlers[name] = [])).push(fn); }
    };
    host.addEventListener('mpl:idle', () => { bridge.invokeMethodAsync('OnIdle').catch(() => { }); });
}

// Passes a message the server sent to the handlers the scripts of this chart registered for it.
export function deliver(host, method, args) {
    const list = host.__mplHandlers && host.__mplHandlers[method];
    if (!list) return;
    for (const fn of list.slice()) fn.apply(null, args);
}

// Whether the chart is in the middle of a drag or a band, which a redrawn picture must not cut off.
export function isBusy(host) {
    const svg = host.querySelector('svg');
    return !!svg && svg.getAttribute('data-mpl-busy') === 'true';
}
