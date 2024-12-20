#region

using System.Collections.Generic;

#endregion

namespace Browser.Html;

public class HtmlElement : HtmlNode {
    public readonly Dictionary<string, string> Attributes;
    public readonly string TagName;

    public HtmlElement(string tagName, Dictionary<string, string> attributes, HtmlNode? parent = null) {
        Attributes = attributes;
        TagName = tagName;
        Parent = parent;
    }
}