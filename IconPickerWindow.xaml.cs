using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace TaskbarGruppen
{
    public partial class IconPickerWindow : Window
    {
        const int PreviewPixels = 128;

        readonly Group _group;
        string _glyph;
        string _color;
        BitmapSource _image;

        public IconPickerWindow(Group group)
        {
            _group = group;
            _glyph = group.IconGlyph;
            _color = group.IconColor;
            if (group.IconImage != null)
            {
                var path = Path.Combine(Store.IconDir, group.IconImage);
                _image = File.Exists(path) ? Icons.LoadPicture(path) : null;
            }

            InitializeComponent();
            SourceInitialized += (s, e) => Theme.ApplyTitleBar(this);

            NameText.Text = group.Name;
            SymbolList.ItemsSource = Icons.Symbols;
            ColorList.ItemsSource = Icons.Colors;
            EntryList.ItemsSource = group.Items;
            if (group.Items.Count == 0)
                EntrySection.Visibility = Visibility.Collapsed;
            TileColorBox.IsChecked = group.TileColor;
            RefreshPreview();
        }

        void RefreshPreview()
        {
            Preview.Source = Icons.RenderChoice(_group.Items, _glyph, _color, _image, PreviewPixels);
            CustomSwatch.Background = _color != null
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString(_color))
                : Brushes.Transparent;
        }

        void Auto_Click(object sender, RoutedEventArgs e)
        {
            _glyph = null;
            _image = null;
            _color = null;
            RefreshPreview();
        }

        void Symbol_Click(object sender, RoutedEventArgs e)
        {
            _glyph = ((Icons.SymbolChoice)((Button)sender).DataContext).Code;
            _image = null;
            RefreshPreview();
        }

        void Color_Click(object sender, RoutedEventArgs e)
        {
            _color = ((Icons.ColorChoice)((Button)sender).DataContext).Hex;
            RefreshPreview();
        }

        void CustomColor_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.ColorDialog { FullOpen = true, AnyColor = true })
            {
                if (_color != null)
                    dialog.Color = System.Drawing.ColorTranslator.FromHtml(_color);

                var owner = new System.Windows.Forms.NativeWindow();
                owner.AssignHandle(new WindowInteropHelper(this).Handle);
                try
                {
                    if (dialog.ShowDialog(owner) != System.Windows.Forms.DialogResult.OK)
                        return;
                }
                finally
                {
                    owner.ReleaseHandle();
                }

                _color = $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}";
                RefreshPreview();
            }
        }

        void Entry_Click(object sender, RoutedEventArgs e)
        {
            var item = (Item)((Button)sender).DataContext;
            if (Icons.Get(item, 256) is BitmapSource image)
            {
                _image = image;
                _glyph = null;
                RefreshPreview();
            }
        }

        void Gallery_Click(object sender, RoutedEventArgs e)
        {
            var gallery = new IconGalleryWindow { Owner = this };
            if (gallery.ShowDialog() == true)
            {
                _image = gallery.Picked;
                _glyph = null;
                RefreshPreview();
            }
        }

        void Browse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = Loc.T("SelectImageProgram"),
                Filter = $"{Loc.T("ImagesAndPrograms")}|*.png;*.jpg;*.jpeg;*.bmp;*.ico;*.exe;*.dll|{Loc.T("AllFiles")}|*.*",
            };
            if (dialog.ShowDialog(this) != true)
                return;

            var image = Icons.LoadPicture(dialog.FileName);
            if (image == null)
            {
                MessageBox.Show(this, Loc.T("NoImage"), Loc.T("ChooseIcon"),
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            _image = image;
            _glyph = null;
            RefreshPreview();
        }

        void Apply_Click(object sender, RoutedEventArgs e)
        {
            var imageFile = _group.Id + "-custom.png";
            var imagePath = Path.Combine(Store.IconDir, imageFile);

            if (_image != null)
            {
                Directory.CreateDirectory(Store.IconDir);
                File.WriteAllBytes(imagePath, Icons.EncodePicture(_image));
                _group.IconImage = imageFile;
            }
            else
            {
                if (File.Exists(imagePath))
                    File.Delete(imagePath);
                _group.IconImage = null;
            }

            _group.IconGlyph = _glyph;
            _group.IconColor = _glyph != null ? _color ?? Icons.DefaultColor : _color;
            _group.TileColor = TileColorBox.IsChecked == true;
            DialogResult = true;
        }
    }
}
