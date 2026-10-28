// Iframe-side bridge for document input forms. Inlined into the srcdoc by C# — the sandboxed
// iframe has an opaque origin, so external script fetches are impossible (see §4 of the design).
// Runs before the template HTML and exposes window.logiedge = { putOutputValue, rpc }.
(function () {
    function post(message) {
        try {
            window.parent.postMessage(JSON.stringify(message), '*');
        } catch (err) {
            // Nothing else we can do from inside a sandboxed iframe.
        }
    }

    try {
        const programmaticValues = {};
        let rpcIdCounter = 0;
        const pendingRpcCalls = new Map();

        window.logiedge = {
            // Stores JSON-serializable values produced by template logic. Last write wins.
            putOutputValue(key, value) {
                programmaticValues[key] = value;
            },
            rpc(method) {
                const available = (typeof context !== 'undefined' && Array.isArray(context.rpcAvailable)) ? context.rpcAvailable : [];
                if (!available.includes(method)) {
                    return Promise.reject(new Error('RPC method not allowed: ' + method));
                }
                const id = ++rpcIdCounter;
                return new Promise((resolve, reject) => {
                    const timeout = setTimeout(() => {
                        pendingRpcCalls.delete(id);
                        reject(new Error('RPC call timed out: ' + method));
                    }, 30000);
                    pendingRpcCalls.set(id, { resolve: resolve, reject: reject, timeout: timeout });
                    post({ v: 1, type: 'rpc', id: id, method: method });
                });
            }
        };

        // Zero-JS case: serialize every non-disabled named input by its name attribute.
        // checkbox -> bool, radio group -> value of the checked member (omitted if none),
        // everything else -> string. The `hidden` attribute does NOT exclude an input.
        function collectDomValues() {
            const values = {};
            for (const el of document.querySelectorAll('input[name], select[name], textarea[name]')) {
                if (el.disabled || !el.name) continue;
                if (el instanceof HTMLInputElement && el.type === 'checkbox') {
                    values[el.name] = el.checked;
                } else if (el instanceof HTMLInputElement && el.type === 'radio') {
                    if (el.checked && !(el.name in values)) values[el.name] = el.value;
                } else {
                    values[el.name] = el.value;
                }
            }
            return values;
        }

        // Merge order: DOM values, then programmatic values on top, then computeUserInput (final).
        function collectValues() {
            const merged = Object.assign({}, collectDomValues(), programmaticValues);
            if (typeof window.computeUserInput === 'function') {
                return window.computeUserInput(merged);
            }
            return merged;
        }

        window.addEventListener('message', event => {
            let msg;
            try {
                msg = JSON.parse(event.data);
            } catch (err) {
                return; // not a bridge message
            }
            if (!msg || typeof msg !== 'object' || msg.v !== 1 || typeof msg.type !== 'string') return;

            switch (msg.type) {
                case 'collect': {
                    try {
                        if (typeof window.validateInputs === 'function') {
                            const validationError = window.validateInputs();
                            if (validationError != null) {
                                post({ v: 1, type: 'error', message: String(validationError) });
                                return; // send nothing else
                            }
                        }
                        post({ v: 1, type: 'values', values: collectValues() });
                    } catch (err) {
                        post({ v: 1, type: 'error', message: err && err.message ? err.message : String(err) });
                    }
                    break;
                }
                case 'rpcResult': {
                    const pending = pendingRpcCalls.get(msg.id);
                    if (!pending) return;
                    clearTimeout(pending.timeout);
                    pendingRpcCalls.delete(msg.id);
                    if (msg.error != null) pending.reject(new Error(String(msg.error)));
                    else pending.resolve(msg.result);
                    break;
                }
            }
        });

        // Surface template runtime errors in the modal.
        window.addEventListener('error', event => {
            post({ v: 1, type: 'error', message: event.message || 'Script error.' });
        });

        // The bridge is loaded — NOT after the template's async work (RPC calls etc.).
        document.addEventListener('DOMContentLoaded', () => {
            post({ v: 1, type: 'ready' });
        });
    } catch (err) {
        post({ v: 1, type: 'error', message: 'Bridge initialization failed: ' + (err && err.message ? err.message : String(err)) });
    }
})();
