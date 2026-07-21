using System.Windows;
using System.Windows.Forms;

namespace BlockMachine.Helpers;

public static class ScreenHelper
{
    public static IReadOnlyList<Rect> GetScreenBounds()
    {
        return Screen.AllScreens
            .Select(screen => new Rect(
                screen.Bounds.X,
                screen.Bounds.Y,
                screen.Bounds.Width,
                screen.Bounds.Height))
            .ToList();
    }
}
