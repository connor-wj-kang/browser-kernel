const LISTENERS = {};
const SET_TIMEOUT_REQUESTS = {};
const XHR_REQUESTS = {};
let RAF_LISTENERS = [];

function setTimeout(callback, time_delta) {
    const handle = Object.keys(SET_TIMEOUT_REQUESTS).length;
    SET_TIMEOUT_REQUESTS[handle] = callback;
    setTimeoutCS(handle, time_delta);
}

function __runSetTimeout(handle) {
    const callback = SET_TIMEOUT_REQUESTS[handle];
    callback();
}

class XMLHttpRequest {
    constructor() {
        this.handle = Object.keys(XHR_REQUESTS).length;
        XHR_REQUESTS[this.handle] = this;
    }

    open(method, url) {
        this.method = method;
        this.url = url;
    }

    send(body) {
        this.responseText = XMLHttpRequestCS(this.method, this.url, body, this.handle);
    }
}

function __runXHROnload(body, handle) {
    const obj = XHR_REQUESTS[handle];
    const evt = new Event('load');
    obj.responseText = body;
    if (obj.onload)
        obj.onload(evt);
}

function requestAnimationFrame(fn) {
    RAF_LISTENERS.push(fn);
    requestAnimationFrame();
}

function __runRAFHandlers() {
    const handlers_copy = RAF_LISTENERS;
    RAF_LISTENERS = [];
    for (let i = 0; i < handlers_copy.length; i++) {
        handlers_copy[i]();
    }
}

const console = {
    log: x => logCS(x),
};

const document = {
    querySelectorAll: s => {
        const handles = querySelectorAllCS(s);
        return handles.map(function (h) {
            return new Node(h);
        });
    },
};

class Node {
    #innerHTML = "";

    constructor(handle) {
        this.handle = handle;
    }

    get innerHTML() {
        return this.#innerHTML;
    }

    set innerHtml(s) {
        this.#innerHTML = innerHtmlSetterCS(this.handle, s.toString());
    }

    getAttribute(attr) {
        return getAttributeCS(this.handle, attr);
    }

    addEventListener(type, listener) {
        if (!LISTENERS[this.handle]) LISTENERS[this.handle] = {};
        const dict = LISTENERS[this.handle];
        if (!dict[type]) dict[type] = [];
        const list = dict[type];
        list.push(listener);
    }

    dispatchEvent(evt) {
        const type = evt.type;
        const handle = this.handle;
        const list = LISTENERS[handle] && LISTENERS[handle][type] || [];
        for (let i = 0; i < list.length; i++) {
            list[i].call(this, evt);
        }
        return evt.do_default;
    }
}

class Event {
    constructor(type) {
        this.type = type;
        this.do_default = true;
    }

    preventDefault() {
        this.do_default = false;
    }
}