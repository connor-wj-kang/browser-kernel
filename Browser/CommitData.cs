#region

using Browser.DrawCommands;

#endregion

namespace Browser;

public class CommitData(Uri uri, float scroll, float height, List<DrawCommand> displayList) {
    public List<DrawCommand> DisplayList = displayList;
    public float Height = height;
    public float Scroll = scroll;
    public Uri Uri = uri;
}