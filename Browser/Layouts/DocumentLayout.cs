#region

using Browser.DrawCommands;
using Browser.Html;

#endregion

namespace Browser.Layouts;

public sealed class DocumentLayout(HtmlNode node) : Layout(node) {
    public override void CalculateLayout() {
        var child = new BlockLayout(Node, this);
        Children.Add(child);
        Width = MainWindow.CanvasWidth - 2 * Tab.Hstep;
        X = Tab.Hstep;
        Y = Tab.Hstep;
        child.CalculateLayout();
        Height = child.Height;
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