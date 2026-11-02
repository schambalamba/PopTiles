using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Win32;

namespace TaskbarGruppen
{
    public static class Theme
    {
        public static bool IsDark
        {
            get
            {
                var value = Registry.GetValue(
                    @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                    "AppsUseLightTheme", 1);
                return value is int light && light == 0;
            }
        }

        // Der Acryl-Hintergrund existiert erst ab Windows 11 22H2.
        public static bool HasAcrylic => Environment.OSVersion.Version.Build >= 22621;

        public static void Apply(Application app)
        {
            var dark = IsDark;
            var resources = app.Resources;
            Loc.Apply(resources);
            resources["Bg"] = Brush(dark ? "#202020" : "#F3F3F3");
            resources["Panel"] = Brush(dark ? "#2B2B2B" : "#FFFFFF");
            resources["Fg"] = Brush(dark ? "#FFFFFF" : "#1B1B1B");
            resources["FgDim"] = Brush(dark ? "#B8B8B8" : "#5F5F5F");
            resources["Line"] = Brush(dark ? "#454545" : "#D6D6D6");
            resources["Hover"] = Brush(dark ? "#1FFFFFFF" : "#14000000");
            resources["Pressed"] = Brush(dark ? "#14FFFFFF" : "#0C000000");
            resources["Accent"] = Brush(dark ? "#60CDFF" : "#005FB8");
            resources["AccentFg"] = Brush(dark ? "#000000" : "#FFFFFF");
            // Ohne Acryl braucht die Kachel eine deckende Farbe.
            resources["TileTint"] = HasAcrylic
                ? Brush(dark ? "#99202020" : "#B3F3F3F3")
                : Brush(dark ? "#FF202020" : "#FFF3F3F3");

            resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/PopTiles;component/ThemeStyles.xaml")
            });
        }

        public static void ApplyTitleBar(Window window)
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            var dark = IsDark ? 1 : 0;
            Native.DwmSetWindowAttribute(hwnd, Native.DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));
        }

        static SolidColorBrush Brush(string color)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
            brush.Freeze();
            return brush;
        }
    }
}
