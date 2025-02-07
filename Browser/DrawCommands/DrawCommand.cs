﻿#region

using SkiaSharp;

#endregion

namespace Browser.DrawCommands;

public abstract class DrawCommand {
    public SKRect Rectangle;

    public abstract void Draw(SKCanvas canvas);
}