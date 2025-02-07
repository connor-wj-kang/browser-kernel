﻿#region

using Browser.DrawCommands;
using Browser.Html;

#endregion

namespace Browser.Layouts;

public sealed class LineLayout(HtmlNode node, Layout? parent = null, Layout? previous = null)
    : Layout(node, parent, previous) {
    public override void CalculateLayout() {
        Width = Parent!.Width;
        X = Parent.X;
        if (Previous != null)
            Y = Previous.Y + Previous.Height;
        else
            Y = Parent.Y;
        Children.ForEach(word => word.CalculateLayout());
        if (Children.Count == 0) {
            Height = 0;
            return;
        }
        var maxAscent = Children.Select(word => -word.Font!.Metrics.Ascent).Max();
        var maxDescent = Children.Select(word => word.Font!.Metrics.Descent).Max();
        var baseline = Y + 1.25f * maxAscent;
        Children.ForEach(word => word.Y = baseline + word.Font!.Metrics.Ascent);
        Height = 1.25f * (maxAscent + maxDescent);
    }

    protected override List<DrawCommand> Paint() {
        return [];
    }

    public override bool ShouldPaint() {
        return true;
    }

    public override List<DrawCommand> PaintEffects(List<DrawCommand> drawCommands) {
        return drawCommands;
    }
}