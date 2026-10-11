using Avalonia;
using Avalonia.Controls;
using System;
using System.Linq;

namespace ActiveRest.Views
{
    internal static class WindowPlacement
    {
        /// <summary>Moves a freshly opened window so that it lies fully inside the working area of the screen it is on.</summary>
        public static void ClampToWorkArea(Window window)
        {
            var screen = window.Screens.ScreenFromWindow(window) ?? window.Screens.Primary ?? window.Screens.All.FirstOrDefault();
            if (screen == null) return;

            var work = screen.WorkingArea;
            double scale = window.RenderScaling;

            // Small laptop screens: shrink the window instead of letting it run off the bottom.
            double maxHeight = work.Height / scale - 8;
            if (window.Bounds.Height > maxHeight) window.Height = maxHeight;

            int width = (int)Math.Ceiling(window.Bounds.Width * scale);
            int height = (int)Math.Ceiling(window.Bounds.Height * scale);

            int x = Math.Clamp(window.Position.X, work.X, Math.Max(work.X, work.Right - width));
            int y = Math.Clamp(window.Position.Y, work.Y, Math.Max(work.Y, work.Bottom - height));
            if (x != window.Position.X || y != window.Position.Y)
                window.Position = new PixelPoint(x, y);
        }
    }
}
