using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace TaskbarGruppen
{
    public partial class TileWindow : Window
    {
        const int MaxColumns = 6;
        const int MaxRowColumns = 12;
        const double LightColorThreshold = 150;
        const byte TileColorAlpha = 0xD0;
        const uint MonitorDefaultToNearest = 2;

        readonly Group _group;
        readonly DispatcherTimer _outsideClickTimer;
        IntPtr _hwnd;
        bool _closing;

        public TileWindow(Group group)
        {
            _group = group;
            InitializeComponent();

            NameText.Text = group.Name;
            ItemsList.ItemsSource = group.Items;
            ItemsList.Tag = ColumnsFor(group);
            if (group.Layout == "list")
                ItemsList.ItemTemplate = (DataTemplate)FindResource("ListRowTemplate");
            if (group.Items.Count == 0)
                EmptyText.Visibility = Visibility.Visible;
            if (group.TileColor && group.IconColor != null)
                ApplyTileColor((Color)ColorConverter.ConvertFromString(group.IconColor));

            // Falls Windows dem Prozess den Fokus verweigert, feuert Deactivated nie.
            // Der Timer schliesst die Kachel dann trotzdem beim Klick daneben.
            _outsideClickTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
            _outsideClickTimer.Tick += (s, e) => CheckOutsideClick();

            SourceInitialized += OnSourceInitialized;
            Loaded += OnLoaded;
            Deactivated += (s, e) => CloseFromOutside();
            PreviewKeyDown += OnPreviewKeyDown;
        }

        // Schliesst ohne Umschalt-Sperre: fuer einen Start per Tastatur, waehrend die Kachel offen ist.
        public void CloseQuietly()
        {
            _closing = true;
            Close();
        }

        static int ColumnsFor(Group group)
        {
            var count = Math.Max(group.Items.Count, 1);
            switch (group.Layout)
            {
                case "list":
                    return 1;
                case "row":
                    return Math.Min(count, MaxRowColumns);
                case "2":
                case "3":
                case "4":
                    return Math.Min(count, int.Parse(group.Layout));
            }
            return count <= 4 ? count : Math.Min((int)Math.Ceiling(Math.Sqrt(count)), MaxColumns);
        }

        // Ueberschreibt die Theme-Farben nur fuer dieses Fenster. Die Schrift richtet sich nach der Helligkeit,
        // damit sie auch auf hellen Farben lesbar bleibt.
        void ApplyTileColor(Color color)
        {
            var isLight = 0.299 * color.R + 0.587 * color.G + 0.114 * color.B > LightColorThreshold;
            var alpha = Theme.HasAcrylic ? TileColorAlpha : (byte)0xFF;
            var ink = isLight ? Colors.Black : Colors.White;

            Resources["TileTint"] = new SolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B));
            Resources["Fg"] = new SolidColorBrush(ink);
            Resources["FgDim"] = new SolidColorBrush(Color.FromArgb(0xC0, ink.R, ink.G, ink.B));
            Resources["Hover"] = new SolidColorBrush(Color.FromArgb(0x26, ink.R, ink.G, ink.B));
            Resources["Pressed"] = new SolidColorBrush(Color.FromArgb(0x18, ink.R, ink.G, ink.B));
            Resources["Accent"] = new SolidColorBrush(ink);
        }

        void OnSourceInitialized(object sender, EventArgs e)
        {
            _hwnd = new WindowInteropHelper(this).Handle;

            // Macht den Fensterhintergrund durchlaessig, damit der Acryl-Effekt von Windows sichtbar wird.
            HwndSource.FromHwnd(_hwnd).CompositionTarget.BackgroundColor = Colors.Transparent;

            Theme.ApplyTitleBar(this);
            var corners = Native.DWMWCP_ROUND;
            Native.DwmSetWindowAttribute(_hwnd, Native.DWMWA_WINDOW_CORNER_PREFERENCE, ref corners, sizeof(int));
            var backdrop = Native.DWMSBT_TRANSIENTWINDOW;
            Native.DwmSetWindowAttribute(_hwnd, Native.DWMWA_SYSTEMBACKDROP_TYPE, ref backdrop, sizeof(int));
        }

        void OnLoaded(object sender, RoutedEventArgs e)
        {
            MoveToTaskbarClick();
            Activate();
            Native.SetForegroundWindow(_hwnd);
            _outsideClickTimer.Start();
        }

        void MoveToTaskbarClick()
        {
            Native.GetCursorPos(out var cursor);
            var monitor = Native.MonitorFromPoint(cursor, MonitorDefaultToNearest);
            var info = new Native.MONITORINFO { cbSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Native.MONITORINFO)) };
            Native.GetMonitorInfo(monitor, ref info);
            Native.GetDpiForMonitor(monitor, 0, out var dpi, out _);

            var scale = dpi / 96.0;
            var width = (int)Math.Ceiling(ActualWidth * scale);
            var height = (int)Math.Ceiling(ActualHeight * scale);
            var gap = (int)(12 * scale);

            var bar = new Native.APPBARDATA { cbSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Native.APPBARDATA)) };
            Native.SHAppBarMessage(Native.ABM_GETTASKBARPOS, ref bar);

            int x, y;
            var screen = info.rcMonitor;
            switch (bar.uEdge)
            {
                case Native.ABE_TOP:
                    x = cursor.X - width / 2;
                    y = screen.Top + (bar.rc.Bottom - bar.rc.Top) + gap;
                    break;
                case Native.ABE_LEFT:
                    x = screen.Left + (bar.rc.Right - bar.rc.Left) + gap;
                    y = cursor.Y - height / 2;
                    break;
                case Native.ABE_RIGHT:
                    x = screen.Right - (bar.rc.Right - bar.rc.Left) - gap - width;
                    y = cursor.Y - height / 2;
                    break;
                default:
                    x = cursor.X - width / 2;
                    y = screen.Bottom - (bar.rc.Bottom - bar.rc.Top) - gap - height;
                    break;
            }

            x = Math.Max(screen.Left + gap, Math.Min(x, screen.Right - gap - width));
            y = Math.Max(screen.Top + gap, Math.Min(y, screen.Bottom - gap - height));

            // WPF setzt sonst seine eigene Left/Top-Position wieder durch. Left/Top rechnen in der DPI des
            // aktuellen Monitors, SetWindowPos korrigiert danach einen Wechsel auf einen Monitor mit anderer DPI.
            var currentScale = VisualTreeHelper.GetDpi(this).DpiScaleX;
            Left = x / currentScale;
            Top = y / currentScale;
            Native.SetWindowPos(_hwnd, IntPtr.Zero, x, y, 0, 0, Native.SWP_NOSIZE | Native.SWP_NOZORDER | Native.SWP_NOACTIVATE);
        }

        void CheckOutsideClick()
        {
            var buttonDown = (Native.GetAsyncKeyState(Native.VK_LBUTTON) & 0x8000) != 0
                || (Native.GetAsyncKeyState(Native.VK_RBUTTON) & 0x8000) != 0
                || (Native.GetAsyncKeyState(Native.VK_MBUTTON) & 0x8000) != 0;
            if (!buttonDown)
                return;

            Native.GetCursorPos(out var cursor);
            Native.GetWindowRect(_hwnd, out var rect);
            var inside = cursor.X >= rect.Left && cursor.X < rect.Right && cursor.Y >= rect.Top && cursor.Y < rect.Bottom;
            if (!inside)
                CloseFromOutside();
        }

        void CloseFromOutside()
        {
            if (_closing)
                return;
            _closing = true;
            Store.MarkClosed(_group.Id);
            Close();
        }

        void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                CloseQuietly();
        }

        void Item_Click(object sender, RoutedEventArgs e)
        {
            var item = (Item)((Button)sender).DataContext;
            _closing = true;
            _outsideClickTimer.Stop();
            Hide();
            Shortcuts.Launch(item);
            Close();
        }

        void Edit_Click(object sender, RoutedEventArgs e)
        {
            _closing = true;
            Shortcuts.OpenManager(_group.Id);
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            _outsideClickTimer.Stop();
            base.OnClosed(e);
        }
    }
}
