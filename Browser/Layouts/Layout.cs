#region

using System.Collections.Generic;
using Browser.DrawCommands;
using Browser.Html;
using SkiaSharp;

#endregion

namespace Browser.Layouts;

public abstract class Layout(HtmlNode node, Layout? parent = null, Layout? previous = null) {
    public readonly List<Layout> Children = [];
    public readonly HtmlNode Node = node;
    protected readonly Layout? Parent = parent;
    protected readonly Layout? Previous = previous;
    public SKFont? Font = null;
    public float Height = 0;
    public float Width = 0;
    public float X = 0;
    public float Y = 0;

    public static void PaintTree(Layout layout, List<DrawCommand> drawCommands) {
        drawCommands.AddRange(layout.Paint());
        foreach (var child in layout.Children) PaintTree(child, drawCommands);
    }

    public static List<Layout> TreeToList(Layout layoutTree, List<Layout> list) {
        list.Add(layoutTree);
        layoutTree.Children.ForEach(child => TreeToList(child, list));
        return list;
    }

    public abstract void CalculateLayout();

    protected abstract List<DrawCommand> Paint();

    public abstract bool ShouldPaint();

    public abstract List<DrawCommand> PaintEffects(List<DrawCommand> drawCommands);
}