#region

using System.Diagnostics;
using System.Net.Http;
using Browser.DrawCommands;
using Browser.Js;
using SkiaSharp;

#endregion

namespace Browser;

public class BrowserData {
    public const long RefreshRateSec = 3;
    private readonly Lock _lock = new();
    public Tab? ActiveTab;
    public List<DrawCommand> ActiveTabDisplayList = [];
    public float ActiveTabScroll;
    public Uri? ActiveTabUrl;
    public Timer? AnimationTimer;
    public MeasureTime Measure = new();
    public bool NeedsAnimationFrame;
    public bool NeedsRasterAndDraw;
    public List<Tab> Tabs = [];

    public BrowserData() {
        Thread.CurrentThread.Name = "Browser Thread";
    }

    public void Render() {
        ActiveTab?.TaskRunner.Run();
        if (ActiveTab!.Loaded) ActiveTab.RunAnimationFrame(ActiveTabScroll);
    }

    public void Commit(Tab tab, CommitData data) {
        lock (_lock) {
            if (tab != ActiveTab) return;
            ActiveTabUrl = data.Uri;
            if (data.Scroll != 0) ActiveTabScroll = data.Scroll;
            if (data.DisplayList.Count != 0) ActiveTabDisplayList = data.DisplayList;
            AnimationTimer = null;
            SetNeedsRasterAndDraw();
        }
    }

    public void SetNeedsAnimationFrame(Tab tab) {
        lock (_lock) {
            if (tab == ActiveTab) NeedsAnimationFrame = true;
        }
    }

    public void SetNeedsRasterAndDraw() {
        NeedsRasterAndDraw = true;
    }

    public void RasterAndDraw(SKCanvas canvas) {
        lock (_lock) {
            if (!NeedsRasterAndDraw) return;
            Measure.Time("raster/draw");
            RasterTab(canvas);
            Measure.Stop("raster/draw");
            NeedsRasterAndDraw = false;
        }
    }

    public void HandleClick(float x, float y) {
        lock (_lock) {
            SetNeedsRasterAndDraw();
            var task = new TaskUnit(ActiveTab!.Click, x, y);
            ActiveTab?.TaskRunner.ScheduleTask(task);
        }
    }

    public void HandleDown(float step) {
        lock (_lock) {
            ActiveTabScroll += step;
            SetNeedsRasterAndDraw();
            NeedsAnimationFrame = true;
        }
    }

    public void ScheduleAnimationFrame() {
        lock (_lock) {
            if (NeedsAnimationFrame && AnimationTimer == null) {
                var callback = new TimerCallback(_ => {
                    float scroll;
                    Tab? activeTab;
                    scroll = ActiveTabScroll;
                    activeTab = ActiveTab;
                    NeedsAnimationFrame = false;
                    Debug.WriteLine("raster/draw");
                    var task = new TaskUnit(ActiveTab!.RunAnimationFrame, scroll);
                    activeTab?.TaskRunner.ScheduleTask(task);
                });
                AnimationTimer = new Timer(callback, null, 1, Timeout.Infinite);
            }
        }
    }

    public void SetActiveTab(Tab tab) {
        ActiveTab = tab;
        ActiveTabScroll = 0;
        ActiveTabUrl = null;
        NeedsAnimationFrame = true;
        AnimationTimer = null;
    }

    public void ScheduleLoad(Uri uri, FormUrlEncodedContent body = null) {
        ActiveTab.TaskRunner.ClearPendingTasks();
        var task = new TaskUnit(ActiveTab.Load, uri, body);
        ActiveTab.TaskRunner.ScheduleTask(task);
    }

    public void NewTab(Uri uri) {
        lock (_lock) {
            NewTabInternal(uri);
        }
    }

    public void ClampScroll(float scroll) {
    }


    public void NewTabInternal(Uri uri) {
        var newTab = new Tab(this, uri);
        Tabs.Add(newTab);
        SetActiveTab(newTab);
        ScheduleLoad(uri);
    }

    public void RasterTab(SKCanvas canvas) {
        if (ActiveTab == null) return;
        canvas.Clear(SKColors.White);
        canvas.Translate(0, ActiveTabScroll);
        ActiveTabDisplayList.ForEach(cmd => cmd.Draw(canvas));
    }

    public void HandleQuit() {
        Measure.Finish();
        Tabs.ForEach(tab => tab.TaskRunner.SetNeedsQuit());
    }
}