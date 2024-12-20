#region

using System.Collections.Generic;

#endregion

namespace Browser.Html;

public abstract class HtmlNode {
    public readonly Dictionary<string, string> Styles = new();
    public List<HtmlNode> Children = [];
    public bool IsFocused = false;
    public HtmlNode? Parent = null;

    public static List<HtmlNode> TreeToList(HtmlNode domTree, List<HtmlNode> list) {
        list.Add(domTree);
        domTree.Children.ForEach(child => TreeToList(child, list));
        return list;
    }
}