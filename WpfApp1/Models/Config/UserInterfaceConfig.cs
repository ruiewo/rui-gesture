namespace RuiGesture.Models.Config;

using System;
using System.Drawing;

public class UserInterfaceConfig
{
    public Func<Point, Point> TooltipPositionBinding { get; set; } = (point) =>
    {
        var rect = Screen.FromPoint(point).WorkingArea;
        return new Point(rect.X + rect.Width - 10, rect.Y + rect.Height - 10);
    };

    public int TooltipTimeout { get; set; } = 3000;

    public int BalloonTimeout { get; set; } = 10000;
}