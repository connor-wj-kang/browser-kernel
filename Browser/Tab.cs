#region

using System.Diagnostics;
using System.Net.Http;
using Browser.Css;
using Browser.DrawCommands;
using Browser.Html;
using Browser.Js;
using Browser.Layouts;
using Browser.Utils;
using SkiaSharp;

#endregion

namespace Browser;

public class Tab {
    public const float Vstep = 18;
    public const float Hstep = 16;
    public readonly Stack<Uri> History = [];
    public List<string> AllowedOrigins = [];
    public BrowserData BrowserData;
    public List<(Selector, Dictionary<string, string>)> Css = [];
    public HtmlNode DomTree;
    public List<DrawCommand> DrawCommands = [];
    public HtmlNode? FocusDom;
    public JsContext? Js;
    public Layout LayoutTree;
    public bool Loaded;
    public bool NeedsAnimationFrame = false;
    public bool NeedsRafCallBacks = false;
    public bool NeedsRender;
    public bool ScrollChangeInTab;
    public float ScrollY;
    public TaskRunner TaskRunner;
    public Uri Uri;

    public Tab(BrowserData browserData, Uri uri) {
        Uri = uri;
        BrowserData = browserData;
        TaskRunner = new TaskRunner(this);
        TaskRunner.StartThread();
    }

    public void RunAnimationFrame(float scroll) {
        if (!ScrollChangeInTab) ScrollY = scroll;
        BrowserData.Measure.Time("script-runRAFHandlers");
        Js?.Interpreter.Execute("__runRAFHandlers()");
        BrowserData.Measure.Stop("script-runRAFHandlers");
        Render();
        ScrollY = 0;
        if (ScrollChangeInTab) ScrollY = scroll;
        var documentHeight = (float)Math.Ceiling(LayoutTree?.Height ?? 600 + 2 * Vstep);
        var commitData = new CommitData(Uri, scroll, documentHeight, DrawCommands);
        DrawCommands = [];
        BrowserData.Commit(this, commitData);
        ScrollChangeInTab = false;
    }

    public bool AllowedRequest(Uri uri) {
        return AllowedOrigins.Count == 0 || AllowedOrigins.Contains(UriUtils.GetOrigin(uri));
    }

    public void Load(Uri uri, FormUrlEncodedContent payload = null) {
        Loaded = false;
        ScrollY = 0;
        ScrollChangeInTab = true;
        TaskRunner.ClearPendingTasks();
        var stopWatch = Stopwatch.StartNew();
        var (headers, html) = UriUtils.Request(uri, Uri, payload);
        stopWatch.Stop();
        Debug.WriteLine($"Load time: {stopWatch.ElapsedMilliseconds}ms");
        Uri = uri;
        History.Push(uri);
        AllowedOrigins.Clear();
        if (headers.TryGetValue("content-security-policy", out var value)) {
            var csp = value.Split(' ');
            if (csp.Length > 0 && csp[0] == "default-src") AllowedOrigins.AddRange(csp.Skip(1));
        }
        DomTree = new HtmlParser(html).Parse();
        if (Js != null) Js.Discarded = true;
        Js = new JsContext(this);
        var scripts = HtmlNode
            .TreeToList(DomTree, [])
            .Where(node => node is HtmlElement { TagName: "script" } htmlElement &&
                           htmlElement.Attributes.ContainsKey("src"))
            .Select(node => node is HtmlElement htmlElement ? htmlElement.Attributes["src"] : "")
            .ToList();
        foreach (var script in scripts) {
            var scriptUri = new Uri(uri, script);
            var js = "";
            try {
                var (header, body) = UriUtils.Request(scriptUri);
                js = body;
            }
            catch (Exception) {
            }
            var task = new TaskUnit(Js.Run, script, js);
            TaskRunner.ScheduleTask(task);
        }
        // await Task.WhenAll(scripts.Select(async script => {
        //     var scriptUri = new Uri(uri, script);
        //     if (!AllowedRequest(scriptUri)) {
        //         Debug.WriteLine($"Blocked script {script} due to SCP");
        //         return;
        //     }
        //     var js = "";
        //     try {
        //         var (header, body) = await UriUtils.Request(scriptUri);
        //         js = body;
        //     }
        //     catch (Exception) {
        //         return;
        //     }
        //     var task = new Task(() => Js.Run(script, js));
        //     TaskRunner.ScheduleTask(task);
        // }));
        Css = CssParser.DefaultCss.ToList();
        var links = HtmlNode
            .TreeToList(DomTree, [])
            .Where(node => node is HtmlElement { TagName: "link" } htmlElement &&
                           htmlElement.Attributes.GetValueOrDefault("rel") == "stylesheet" &&
                           htmlElement.Attributes.ContainsKey("href"))
            .Select(node => node is HtmlElement htmlElement ? htmlElement.Attributes["href"] : "")
            .ToList();
        foreach (var link in links) {
            var styleUri = new Uri(uri, link);
            var css = "";
            try {
                var (header, body) = UriUtils.Request(styleUri);
                css = body;
            }
            catch (Exception) {
                return;
            }
            Css.AddRange(new CssParser(css).Parse());
        }
        // await Task.WhenAll(links.Select(async link => {
        //     var styleUri = new Uri(uri, link);
        //     if (!AllowedRequest(styleUri)) {
        //         Debug.WriteLine($"Blocked style {link} due to CSP");
        //         return;
        //     }
        //     var css = "";
        //     try {
        //         var (header, body) = await UriUtils.Request(styleUri);
        //         css = body;
        //     }
        //     catch (Exception) {
        //         return;
        //     }
        //     Css.AddRange(new CssParser(css).Parse());
        // }));
        Debug.WriteLine("Loaded");
        SetNeedsRender();
        Loaded = true;
    }

    public void SetNeedsRender() {
        NeedsRender = true;
        BrowserData.SetNeedsAnimationFrame(this);
    }

    public float ClampScroll(float scroll) {
        return 0;
    }

    public void Render() {
        if (!NeedsRender) return;
        Css.Sort((a, b) => CssParser.CascadePriority(a) - CssParser.CascadePriority(b));
        CssParser.ApplyCss(DomTree, Css);
        LayoutTree = new DocumentLayout(DomTree);
        LayoutTree.CalculateLayout();
        DrawCommands = [];
        Layout.PaintTree(LayoutTree, DrawCommands);
        NeedsRender = false;
        var clampedScroll = ClampScroll(ScrollY);
        if (clampedScroll != ScrollY) ScrollChangeInTab = true;
        BrowserData.Measure.Stop("render");
    }

    public void ReLayout() {
        DrawCommands.Clear();
        LayoutTree.CalculateLayout();
        Layout.PaintTree(LayoutTree, DrawCommands);
    }

    public void Click(float x, float y) {
        Render();
        FocusDom = null;
        Debug.WriteLine($"x {x}, y {y} y {y + BrowserData.ActiveTabScroll} {BrowserData.ActiveTabScroll}");
        y -= BrowserData.ActiveTabScroll;
        var domList = Layout
            .TreeToList(LayoutTree, [])
            .Where(obj => obj.X <= x && x < obj.X + obj.Width && obj.Y <= y && y < obj.Y + obj.Height)
            .ToList();
        if (domList.Count == 0) return;
        var elt = domList.Last().Node;
        if (elt != null && Js!.DispatchEvent("click", elt)) return;
        while (elt != null) {
            switch (elt) {
                case HtmlText:
                    break;
                case HtmlElement { TagName: "a" } htmlElement when htmlElement.Attributes.ContainsKey("href"): {
                    var uri = new Uri(Uri, htmlElement.Attributes["href"]);
                    Load(uri);
                    return;
                }
                case HtmlElement { TagName: "input" } htmlElement: {
                    htmlElement.Attributes["value"] = "";
                    if (FocusDom != null) FocusDom.IsFocused = false;
                    FocusDom = elt;
                    elt.IsFocused = true;
                    SetNeedsRender();
                    return;
                }
                case HtmlElement { TagName: "button" }: {
                    while (elt.Parent != null) {
                        if (elt is HtmlElement { TagName: "form" } htmlElement &&
                            htmlElement.Attributes.ContainsKey("action")) {
                            SubmitForm(htmlElement);
                            return;
                        }
                        elt = elt.Parent;
                    }
                    break;
                }
            }
            elt = elt?.Parent;
        }
    }

    public void SubmitForm(HtmlElement elt) {
        if (Js.DispatchEvent("submit", elt)) return;
        var inputs = HtmlNode
            .TreeToList(elt, [])
            .Where(node => node is HtmlElement { TagName: "input" } htmlElement &&
                           htmlElement.Attributes.ContainsKey("name"))
            .Select(node => node is HtmlElement htmlElement ? htmlElement : null).ToList();
        var values = new Dictionary<string, string>();
        inputs.ForEach(input => {
            var name = input.Attributes["name"];
            var value = input.Attributes.GetValueOrDefault("value", "");
            values[name] = value;
        });
        var postContent = new FormUrlEncodedContent(values);
        var uri = new Uri(Uri, elt.Attributes["action"]);
        Load(uri, postContent);
    }

    public void KeyPress(char key) {
        if (FocusDom is not HtmlElement htmlElement) return;
        if (Js.DispatchEvent("keydown", FocusDom)) return;
        htmlElement.Attributes["value"] += key;
        SetNeedsRender();
    }

    public void Scroll(int deltaY) {
        var maxY = Math.Max(LayoutTree.Height + 2 * 18, 0);
        ScrollY = Math.Min(ScrollY - deltaY, maxY);
    }

    public void Draw(SKCanvas canvas) {
        DrawCommands.ForEach(drawCommand => {
            // if (drawCommand.Rectangle.Top > ScrollY) return;
            // if (drawCommand.Rectangle.Bottom < ScrollY) return;
            drawCommand.Draw(canvas);
        });
    }

    public void GoBack() {
        if (History.Count <= 1) return;
        History.Pop();
        var back = History.Pop();
        Load(back);
    }
}