#region

using Browser.Html;

#endregion

namespace Browser.Css;

public sealed class DescendantSelector : Selector {
    private readonly Selector _ancestor;
    private readonly Selector _descendant;

    public DescendantSelector(Selector ancestor, Selector descendant) {
        _ancestor = ancestor;
        _descendant = descendant;
        Priority = ancestor.Priority + descendant.Priority;
    }

    public override bool Matches(HtmlNode node) {
        if (!_descendant.Matches(node)) return false;
        while (node.Parent != null) {
            if (_ancestor.Matches(node.Parent)) return true;
            node = node.Parent;
        }
        return false;
    }
}