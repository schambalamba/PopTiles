using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;

namespace TaskbarGruppen
{
    public partial class ManagerWindow : Window
    {
        const string AutoLayout = "auto";

        readonly Library _library;
        Point _dragStart;
        Item _dragItem;

        Group CurrentGroup => GroupList.SelectedItem as Group;
        Item CurrentItem => ItemList.SelectedItem as Item;

        public ManagerWindow(Library library, string selectGroupId)
        {
            _library = library;
            InitializeComponent();
            SourceInitialized += (s, e) => Theme.ApplyTitleBar(this);
            Activated += (s, e) => RefreshPinButton();
            Shortcuts.WriteManagerShortcut();

            // Haelt Verknuepfungen aktuell, auch wenn sich der Pfad der EXE oder der Daten geaendert hat.
            foreach (var group in library.Groups)
                Shortcuts.Sync(group, library);
            if (library.Groups.Count > 0)
                Store.Save(library);

            GroupList.ItemsSource = library.Groups;
            GroupList.SelectedItem = library.Find(selectGroupId) ?? library.Groups.FirstOrDefault();
            RefreshPanels();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // TextBox-Aenderungen ohne Fokuswechsel sollen nicht verloren gehen.
            if (CurrentGroup != null)
                SaveGroup(CurrentGroup);
            base.OnClosing(e);
        }

        void SaveGroup(Group group)
        {
            Shortcuts.Sync(group, _library);
            Store.Save(_library);
            if (group == CurrentGroup)
                RefreshIconPreview();
        }

        void RefreshIconPreview() =>
            GroupIconPreview.Source = CurrentGroup != null ? Icons.RenderGroup(CurrentGroup, 64) : null;

        void ChangeIcon_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentGroup != null && new IconPickerWindow(CurrentGroup) { Owner = this }.ShowDialog() == true)
                SaveGroup(CurrentGroup);
        }

        void Layout_Changed(object sender, RoutedEventArgs e)
        {
            var layout = (string)((RadioButton)sender).Tag;
            if (CurrentGroup == null || (CurrentGroup.Layout ?? AutoLayout) == layout)
                return;
            CurrentGroup.Layout = layout == AutoLayout ? null : layout;
            SaveGroup(CurrentGroup);
        }

        void RefreshPanels()
        {
            var group = CurrentGroup;
            DetailPanel.Visibility = group != null ? Visibility.Visible : Visibility.Collapsed;
            NoGroupHint.Visibility = group != null ? Visibility.Collapsed : Visibility.Visible;
            DeleteGroupButton.IsEnabled = group != null;
            GroupNameBox.DataContext = group;
            foreach (var option in LayoutPanel.Children.OfType<RadioButton>())
                option.IsChecked = (string)option.Tag == (group?.Layout ?? AutoLayout);
            RefreshPinButton();
            ItemList.ItemsSource = group?.Items;
            RefreshIconPreview();
            RefreshItemState();
        }

        void RefreshItemState()
        {
            ItemsHint.Visibility = CurrentGroup?.Items.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
            ItemEditRow.IsEnabled = CurrentItem != null;
            ItemTitleBox.DataContext = CurrentItem;
        }

        void GroupList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) => RefreshPanels();

        void ItemList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) => RefreshItemState();

        void NewGroup_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new NameDialog(NextGroupName()) { Owner = this };
            if (dialog.ShowDialog() != true)
                return;

            var group = new Group { Id = Guid.NewGuid().ToString("N").Substring(0, 12), Name = dialog.GroupName };
            _library.Groups.Add(group);
            SaveGroup(group);
            GroupList.SelectedItem = group;

            if (!Store.IsTestRun)
                StartPin(group);
        }

        string NextGroupName()
        {
            var name = Loc.T("NewGroup");
            for (var n = 2; _library.Groups.Any(g => g.Name == name); n++)
                name = Loc.T("NewGroup") + " " + n;
            return name;
        }

        void DeleteGroup_Click(object sender, RoutedEventArgs e)
        {
            var group = CurrentGroup;
            if (group == null)
                return;

            var answer = MessageBox.Show(this,
                string.Format(Loc.T("DeleteGroupConfirm"), group.Name),
                Loc.T("DeleteGroupTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
            if (answer != MessageBoxResult.Yes)
                return;

            if (!Shortcuts.Remove(group))
                MessageBox.Show(this, Loc.T("UnpinFailed"),
                    Loc.T("DeleteGroupTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
            _library.Groups.Remove(group);
            Store.Save(_library);
            GroupList.SelectedItem = _library.Groups.FirstOrDefault();
            RefreshPanels();
        }

        void GroupName_LostFocus(object sender, RoutedEventArgs e)
        {
            if (CurrentGroup == null)
                return;
            if (string.IsNullOrWhiteSpace(CurrentGroup.Name))
                CurrentGroup.Name = Loc.T("DefaultGroup");
            SaveGroup(CurrentGroup);
        }

        void ItemTitle_LostFocus(object sender, RoutedEventArgs e)
        {
            if (CurrentGroup != null)
                SaveGroup(CurrentGroup);
        }

        void AddItems(IEnumerable<Item> items)
        {
            var group = CurrentGroup;
            if (group == null)
                return;

            Item last = null;
            foreach (var item in items.Where(i => !group.Items.Any(o => string.Equals(o.Target, i.Target, StringComparison.OrdinalIgnoreCase))))
            {
                group.Items.Add(item);
                last = item;
            }

            if (last != null)
                ItemList.SelectedItem = last;
            SaveGroup(group);
            RefreshItemState();
        }

        void AddApp_Click(object sender, RoutedEventArgs e)
        {
            var picker = new AppPickerWindow { Owner = this };
            if (picker.ShowDialog() == true)
                AddItems(picker.Selected);
        }

        void AddFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Multiselect = true, Title = Loc.T("SelectFileProgram"), DereferenceLinks = false };
            if (dialog.ShowDialog(this) == true)
                AddItems(dialog.FileNames.Select(ItemFromPath));
        }

        void AddFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog { Description = Loc.T("SelectFolder") })
            {
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    AddItems(new[] { ItemFromPath(dialog.SelectedPath) });
            }
        }

        void AddUrl_Click(object sender, RoutedEventArgs e) => AddUrlFromBox();

        void UrlBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                AddUrlFromBox();
        }

        void AddUrlFromBox()
        {
            var item = ItemFromUrl(UrlBox.Text);
            if (item == null)
            {
                MessageBox.Show(this, Loc.T("InvalidUrl"), Loc.T("WebAddress"),
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            AddItems(new[] { item });
            UrlBox.Clear();
        }

        static Item ItemFromPath(string path)
        {
            var name = Directory.Exists(path) ? new DirectoryInfo(path).Name : Path.GetFileNameWithoutExtension(path);
            return new Item { Title = string.IsNullOrEmpty(name) ? path : name, Target = path };
        }

        static Item ItemFromUrl(string text)
        {
            text = (text ?? "").Trim();
            if (text.Length == 0)
                return null;
            if (!text.Contains("://"))
                text = "https://" + text;
            if (!Uri.TryCreate(text, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) || !uri.Host.Contains('.'))
                return null;

            var host = uri.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? uri.Host.Substring(4) : uri.Host;
            return new Item { Title = host, Target = uri.AbsoluteUri };
        }

        void ItemList_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(typeof(Item)) is Item dragged)
            {
                MoveDraggedItem(dragged, e.GetPosition(ItemList));
                e.Effects = DragDropEffects.Move;
                e.Handled = true;
                return;
            }

            e.Effects =e.Data.GetDataPresent(DataFormats.FileDrop) || e.Data.GetDataPresent(DataFormats.Text)
                ? DragDropEffects.Link
                : DragDropEffects.None;
            e.Handled = true;
        }

        void ItemList_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(Item)))
            {
                e.Effects = DragDropEffects.Move;
                e.Handled = true;
                return;
            }

            if (e.Data.GetData(DataFormats.FileDrop) is string[] paths)
                AddItems(paths.Select(ItemFromPath));
            else if (e.Data.GetData(DataFormats.Text) is string text && ItemFromUrl(text) is Item url)
                AddItems(new[] { url });
        }

        void ItemList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
                RemoveCurrentItem();
        }

        void RemoveItem_Click(object sender, RoutedEventArgs e) => RemoveCurrentItem();

        void RemoveCurrentItem()
        {
            if (CurrentGroup == null || CurrentItem == null)
                return;
            CurrentGroup.Items.Remove(CurrentItem);
            SaveGroup(CurrentGroup);
            RefreshItemState();
        }

        void ItemList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStart = e.GetPosition(ItemList);
            var container = ItemsControl.ContainerFromElement(ItemList, e.OriginalSource as DependencyObject) as ListBoxItem;
            _dragItem = container?.DataContext as Item;
        }

        void ItemList_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_dragItem == null || e.LeftButton != MouseButtonState.Pressed || CurrentGroup == null)
                return;

            var distance = e.GetPosition(ItemList) - _dragStart;
            if (Math.Abs(distance.Y) < SystemParameters.MinimumVerticalDragDistance
                && Math.Abs(distance.X) < SystemParameters.MinimumHorizontalDragDistance)
                return;

            var item = _dragItem;
            var group = CurrentGroup;
            var startIndex = group.Items.IndexOf(item);
            _dragItem = null;

            var result = DragDrop.DoDragDrop(ItemList, new DataObject(typeof(Item), item), DragDropEffects.Move);

            // Esc oder Loslassen ausserhalb der Liste stellt die alte Reihenfolge wieder her.
            if (result == DragDropEffects.None)
                group.Items.Move(group.Items.IndexOf(item), startIndex);
            else if (group.Items.IndexOf(item) != startIndex)
                SaveGroup(group);
        }

        // Verschiebt den gezogenen Eintrag schon waehrend des Ziehens, das zeigt die neue Stelle ohne Extra-Markierung.
        void MoveDraggedItem(Item item, Point position)
        {
            var items = CurrentGroup.Items;
            var from = items.IndexOf(item);
            for (var i = 0; i < items.Count; i++)
            {
                if (i == from || !(ItemList.ItemContainerGenerator.ContainerFromIndex(i) is ListBoxItem row))
                    continue;

                var top = row.TranslatePoint(new Point(0, 0), ItemList).Y;
                var middle = top + row.ActualHeight / 2;
                var pastMiddle = from < i ? position.Y > middle : position.Y < middle;
                if (position.Y >= top && position.Y < top + row.ActualHeight && pastMiddle)
                {
                    items.Move(from, i);
                    return;
                }
            }
        }

        void Help_Click(object sender, RoutedEventArgs e) => Shortcuts.OpenHelp();

        void Pin_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentGroup != null)
                StartPin(CurrentGroup);
        }

        void StartPin(Group group)
        {
            SaveGroup(group);
            PinButton.IsEnabled = false;
            var process = Shortcuts.StartPin(group);
            process.EnableRaisingEvents = true;
            process.Exited += (s, e) => Dispatcher.BeginInvoke(new Action(() => OnPinFinished(group, (PinResult)process.ExitCode)));
        }

        void OnPinFinished(Group group, PinResult result)
        {
            RefreshPinButton();
            if (result != PinResult.NotAllowed && result != PinResult.Unavailable)
                return;

            var answer = MessageBox.Show(this,
                Loc.T("PinDenied"),
                Loc.T("PinToTaskbar"), MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (answer == MessageBoxResult.Yes)
                Process.Start("explorer.exe", $"/select,\"{Shortcuts.ShortcutPath(group)}\"");
        }

        void RefreshPinButton()
        {
            var pinned = CurrentGroup != null && Shortcuts.IsPinned(CurrentGroup);
            PinButton.IsEnabled = CurrentGroup != null && !pinned;
            PinButton.Content = Loc.T(pinned ? "PinnedState" : "PinToTaskbar");
        }
    }
}
