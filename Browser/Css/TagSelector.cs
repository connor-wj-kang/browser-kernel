#region

using Browser.Html;

#endregion

namespace Browser.Css;

public sealed class TagSelector(string tagName) : Selector {
    public override bool Matches(HtmlNode node) {
        return node is HtmlElement htmlElement && htmlElement.TagName == tagName;
    }
}