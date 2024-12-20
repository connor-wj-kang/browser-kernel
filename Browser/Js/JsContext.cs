#region

using System.Diagnostics;
using System.Net.Http;
using Browser.Css;
using Browser.Html;
using Browser.Utils;
using Jint;

#endregion

namespace Browser.Js;

public class JsContext {
    public bool Discarded = false;
    public Dictionary<int, HtmlNode> HandleToNode = new();
    public Engine Interpreter = new();
    public Dictionary<HtmlNode, int> NodeToHandle = new();
    public Tab Tab;

    public JsContext(Tab tab) {
        Tab = tab;
        Tab.BrowserData.Measure.Time("script-runtime");
        Interpreter
            .SetValue("logCS", new Action<object>(Console.WriteLine))
            .SetValue("querySelectorAllCS", QuerySelectorAll)
            .SetValue("getAttributeCS", GetAttributes)
            .SetValue("innerHtmlSetterCS", InnerHtmlSetter)
            .SetValue("setTimeoutCS", SetTimeout)
            .SetValue("requestAnimationFrameCS", RequestAnimationFrame)
            .Execute(Resources.RuntimeJs)
            .Execute(Resources.EventDispatchJs);
        Tab.BrowserData.Measure.Stop("script-runtime");
    }

    public void Run(string script, string code) {
        try {
            Tab.BrowserData.Measure.Time("script-load");
            Interpreter.Execute(code);
            Tab.BrowserData.Measure.Stop("script-load");
        }
        catch (Exception e) {
            Tab.BrowserData.Measure.Stop("script-load");
            Debug.WriteLine($"Script: {script} crashed. {e.Message}");
        }
    }

    public void InnerHtmlSetter(int handle, string innerHtml) {
        var doc = new HtmlParser($"<html><body>{innerHtml}</body></html>").Parse();
        var newNodes = doc.Children[0].Children;
        var elt = HandleToNode[handle];
        elt.Children = newNodes;
        elt.Children.ForEach(child => child.Parent = elt);
        Tab.SetNeedsRender();
    }

    public void DispatchSetTimeout(int handle) {
        if (Discarded) return;
        Tab.BrowserData.Measure.Time("script-settimeout");
        Interpreter.Invoke("__runSetTimeout", handle);
        Tab.BrowserData.Measure.Stop("script-settimeout");
    }

    public void SetTimeout(int handle, int time) {
        var runCallBack = new TimerCallback(_ => {
            var task = new TaskUnit(DispatchSetTimeout, handle);
            Tab.TaskRunner.ScheduleTask(task);
        });
        new Timer(runCallBack, null, time, Timeout.Infinite);
    }

    public void DispatchXHROnLoad(string body, int handle) {
        if (Discarded) return;
        Tab.BrowserData.Measure.Time("script-xhr");
        var doDefault = Interpreter.Invoke("__runXHROnLoad", body, handle);
        Tab.BrowserData.Measure.Stop("script-xhr");
    }

    public void XMLHttpRequestSend(string method, string uri, FormUrlEncodedContent body, int handle) {
        var fullUri = new Uri(Tab.Uri, uri);
        if (!Tab.AllowedRequest(fullUri)) throw new Exception("Cross-origin XHR blocked by CSP");
        if (UriUtils.GetOrigin(fullUri) != UriUtils.GetOrigin(Tab.Uri))
            throw new Exception("Cross-origin XHR request not allowed");
        var (headers, response) = UriUtils.Request(fullUri, Tab.Uri, body);
        var task = new TaskUnit(DispatchXHROnLoad, response, handle);
        Tab.TaskRunner.ScheduleTask(task);
    }

    public void RequestAnimationFrame() {
        Tab.BrowserData.SetNeedsAnimationFrame(Tab);
    }

    public bool DispatchEvent(string type, HtmlNode elt) {
        var handle = NodeToHandle.GetValueOrDefault(elt, -1);
        var doDefault = Interpreter.Invoke("dispatchEvent", type, handle).AsBoolean();
        return !doDefault;
    }

    public int GetHandle(HtmlNode elt) {
        var handle = 0;
        if (!NodeToHandle.ContainsKey(elt)) {
            handle = NodeToHandle.Count;
            NodeToHandle[elt] = handle;
            HandleToNode[handle] = elt;
        }
        else {
            handle = NodeToHandle[elt];
        }
        return handle;
    }

    public List<int> QuerySelectorAll(string selectorText) {
        var selector = new CssParser(selectorText).ParseSelector();
        var nodes = HtmlNode.TreeToList(Tab.DomTree, []).Where(node => selector.Matches(node)).ToList();
        return nodes.Select(GetHandle).ToList();
    }

    public string GetAttributes(int handle, string attr) {
        var elt = HandleToNode[handle];
        if (elt is not HtmlElement htmlElement) return "";
        attr = htmlElement.Attributes.GetValueOrDefault(attr, "");
        return attr;
    }
}