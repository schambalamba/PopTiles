using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace TaskbarGruppen
{
    public sealed class GalleryIcon
    {
        readonly Func<int, BitmapSource> _load;
        BitmapSource _icon;

        public GalleryIcon(string title, Func<int, BitmapSource> load)
        {
            Title = title;
            _load = load;
        }

        public string Title { get; }

        public BitmapSource Icon => _icon ?? (_icon = _load(64));

        public BitmapSource Load(int pixels) => _load(pixels);
    }

    public partial class IconGalleryWindow : Window
    {
        const int Columns = 8;

        // Die Windows-eigenen Sammlungen mit den meisten brauchbaren Icons.
        static readonly string[] SystemLibraries = { "shell32.dll", "imageres.dll", "ddores.dll", "mmres.dll" };

        readonly List<GalleryIcon> _apps;
        readonly List<GalleryIcon> _system;
        List<GalleryIcon> _current;

        public BitmapSource Picked { get; private set; }

        public IconGalleryWindow()
        {
            InitializeComponent();
            SourceInitialized += (s, e) => Theme.ApplyTitleBar(this);

            _apps = AppPickerWindow.LoadInstalledApps()
                .Select(app => new GalleryIcon(app.Title, pixels => Icons.Get(app, pixels) as BitmapSource))
                .ToList();
            _system = LoadSystemIcons();

            AppsButton.Content = $"Apps ({_apps.Count})";
            WindowsButton.Content = $"Windows ({_system.Count})";
            _current = _apps;
            RefreshRows();
            Loaded += (s, e) => SearchBox.Focus();
        }

        static List<GalleryIcon> LoadSystemIcons()
        {
            var icons = new List<GalleryIcon>();
            var folder = Environment.GetFolderPath(Environment.SpecialFolder.System);
            foreach (var library in SystemLibraries)
            {
                var path = Path.Combine(folder, library);
                var name = Path.GetFileNameWithoutExtension(library);
                var count = Icons.CountInFile(path);
                for (var i = 0; i < count; i++)
                {
                    var index = i;
                    icons.Add(new GalleryIcon($"{name}, Icon {index + 1}", pixels => Icons.FromFile(path, index, pixels)));
                }
            }
            return icons;
        }

        void RefreshRows()
        {
            var query = SearchBox.Text.Trim();
            var matches = _current
                .Where(icon => query.Length == 0 || icon.Title.IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0)
                .ToList();

            var rows = new List<List<GalleryIcon>>();
            for (var i = 0; i < matches.Count; i += Columns)
                rows.Add(matches.GetRange(i, Math.Min(Columns, matches.Count - i)));
            RowList.ItemsSource = rows;
        }

        void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_current != null)
                RefreshRows();
        }

        void Apps_Click(object sender, RoutedEventArgs e)
        {
            _current = _apps;
            RefreshRows();
        }

        void Windows_Click(object sender, RoutedEventArgs e)
        {
            _current = _system;
            RefreshRows();
        }

        void Icon_Click(object sender, RoutedEventArgs e)
        {
            Picked = ((GalleryIcon)((Button)sender).DataContext).Load(256);
            if (Picked != null)
                DialogResult = true;
        }
    }
}
