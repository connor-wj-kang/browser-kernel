namespace Browser;

public static class Resources {
    public const string DefaultCss =
        """
        pre {
            background-color: gray;
        }

        a {
            color: blue;
        }

        i {
            font-style: italic;
        }

        b {
            font-weight: bold;
        }

        small {
            font-size: 90%;
        }

        big {
            font-size: 110%;
        }

        input {
            font-size: 16px;
            font-weight: normal;
            font-style: normal;
            background-color: lightblue;
        }

        button {
            font-size: 16px;
            font-weight: normal;
            font-style: normal;
            background-color: orange;
        }

        input:focus {
            outline: 1px solid black;
        }

        button:focus {
            outline: 1px solid black;
        }

        div:focus {
            outline: 1px solid black;
        }

        a:focus {
            outline: 1px solid black;
        }

        iframe {
            outline: 1px solid black;
        }

        @media (prefers-color-scheme: dark) {
            a {
                color: lightblue;
            }
        
            input {
                background-color: blue;
            }
        
            button {
                background-color: orangered;
            }
        
            input:focus {
                outline: 1px solid white;
            }
        
            button:focus {
                outline: 1px solid white;
            }
        
            div:focus {
                outline: 1px solid white;
            }
        
            a:focus {
                outline: 1px solid white;
            }
        }
        """;

    public const string RuntimeJs =
        """
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
        
            set innerHTML(s) {
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
        """;

    public const string EventDispatchJs =
        "function dispatchEvent(type, handle) { return new Node(handle).dispatchEvent(new Event(type)); }";
}