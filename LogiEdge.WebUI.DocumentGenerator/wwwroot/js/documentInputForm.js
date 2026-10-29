// Parent-side bridge for the document input form iframe.
// The iframe is sandboxed (allow-scripts only, opaque origin), so all communication
// goes through postMessage with versioned JSON messages (see .notes/document-input-forms.md §4).

let messageListener = null;

export function initBridge(dotNetRef, iframeId) {
    // Replace any previous registration: a recreated component instance must not be
    // shadowed by a stale listener holding an old iframe id / DotNet reference.
    if (messageListener !== null) window.removeEventListener('message', messageListener);

    messageListener = (event) => {
        // Look up the element at event time: HxModal may recreate the iframe between open/close cycles.
        const el = document.getElementById(iframeId);
        if (!el || event.source !== el.contentWindow) return;

        let parsed;
        try {
            parsed = JSON.parse(event.data);
        } catch (err) {
            return; // not a bridge message
        }
        if (!parsed || typeof parsed !== 'object' || parsed.v !== 1 || typeof parsed.type !== 'string') return;

        // Pass the raw string to C# — parsing/validation happens there.
        dotNetRef.invokeMethodAsync('OnFormMessage', event.data).catch(err => console.error('LogiEdge input form bridge error:', err));
    };
    window.addEventListener('message', messageListener);
}

export function postToIframe(iframeId, messageJsonString) {
    // Post the raw JSON string: the bridge protocol is versioned JSON strings in both directions.
    const el = document.getElementById(iframeId);
    if (!el || !el.contentWindow) {
        console.warn('LogiEdge input form bridge: iframe not found for id', iframeId);
        return;
    }
    el.contentWindow.postMessage(messageJsonString, '*');
}

export function disposeBridge() {
    if (messageListener !== null) {
        window.removeEventListener('message', messageListener);
        messageListener = null;
    }
}
