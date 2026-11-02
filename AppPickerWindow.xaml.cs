using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace TaskbarGruppen
{
    public partial class AppPickerWindow : Window
    {
        readonly ICollectionView _view;

        public List<Item> Selected { get; private set; } = new List<Item>();

        public AppPickerWindow()
        {
            InitializeComponent();
            SourceInitialized += (s, e) => Theme.ApplyTitleBar(this);

            _view = CollectionViewSource.GetDefaultView(LoadInstalledApps());
            _view.Filter = Matches;
            AppList.ItemsSource = _view;
            Loaded += (s, e) => SearchBox.Focus();
        }

        // shell:AppsFolder enthaelt dieselben Eintraege wie "Alle Apps" im Startmenue, inklusive Store-Apps.
        public static List<Item> LoadInstalledApps()
        {
            dynamic shell = Activator.CreateInstance(Type.GetTypeFromProgID("Shell.Application"));
            dynamic folder = shell.NameSpace("shell:AppsFolder");
            var apps = new List<Item>();
            foreach (dynamic entry in folder.Items())
            {
                string name = entry.Name;
                string path = entry.Path;
                if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(path))
                    apps.Add(new Item { Title = name, Target = @"shell:AppsFolder\" + path });
            }
            apps.AddRange(InstalledPrograms.Load(apps.Select(a => a.Title), apps.Select(a => a.Target)));
            return apps.OrderBy(a => a.Title, StringComparer.CurrentCultureIgnoreCase).ToList();
        }

        bool Matches(object entry)
        {
            var query = SearchBox.Text.Trim();
            return query.Length == 0 || ((Item)entry).Title.IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0;
        }

        void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => _view?.Refresh();

        void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Down)
                return;
            if (AppList.SelectedIndex < 0 && AppList.Items.Count > 0)
                AppList.SelectedIndex = 0;
            (AppList.ItemContainerGenerator.ContainerFromIndex(Math.Max(AppList.SelectedIndex, 0)) as ListBoxItem)?.Focus();
            e.Handled = true;
        }

        void Add_Click(object sender, RoutedEventArgs e) => Finish();

        void AppList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (AppList.SelectedItem != null)
                Finish();
        }

        void Finish()
        {
            Selected = AppList.SelectedItems.Cast<Item>().ToList();
            if (Selected.Count == 0 && AppList.Items.Count == 1)
                Selected.Add((Item)AppList.Items[0]);
            DialogResult = Selected.Count > 0;
        }
    }
}
