#region

using Browser.Html;

#endregion

namespace Browser.Css;

public abstract class Selector {
    public int Priority = 1;
    public abstract bool Matches(HtmlNode node);
}