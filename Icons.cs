using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TaskbarGruppen
{
    public static class Icons
    {
        const int BiggerSizeOk = 0x1, IconOnly = 0x4;

        public const string DefaultColor = "#2563EB";
        public const string DefaultGlyph = "E8B7";

        public sealed class SymbolChoice
        {
            public string Code { get; set; }
            public string Name { get; set; }
            public string Text => GlyphText(Code);
        }

        public sealed class ColorChoice
        {
            public string Hex { get; set; }
            public string Name { get; set; }
            public Brush Brush => new SolidColorBrush((Color)ColorConverter.ConvertFromString(Hex));
        }

        static SymbolChoice S(string code, string name) => new SymbolChoice { Code = code, Name = Loc.T("Sym." + name) };

        // Nur Zeichen, die in Segoe Fluent Icons und Segoe MDL2 Assets gleich belegt sind.
        public static readonly SymbolChoice[] Symbols =
        {
            S("E8B7", "Folder"), S("E80F", "Home"), S("E821", "Work"), S("E774", "Internet"),
            S("E715", "Mail"), S("E8BD", "Chat"), S("E716", "People"), S("E787", "Calendar"),
            S("E8A5", "Document"), S("E70F", "Edit"), S("E790", "Paint"), S("EB9F", "Photo"),
            S("E722", "Camera"), S("E714", "Video"), S("EC4F", "Music"), S("E7FC", "Games"),
            S("E943", "Code"), S("E756", "Console"), S("E713", "Settings"), S("E90F", "Tools"),
            S("E8EF", "Calculator"), S("E9D2", "Chart"), S("E719", "Shopping"), S("E7BE", "Learning"),
            S("E753", "Cloud"), S("E896", "Download"), S("E749", "Print"), S("E72E", "Security"),
            S("E734", "Star"), S("EB51", "Heart"), S("EA80", "Idea"), S("E7C1", "Flag"),
            S("E8F1", "Library"), S("E709", "Travel"), S("E804", "Car"), S("ECA5", "Apps"),
        };

        static ColorChoice C(string hex, string name) => new ColorChoice { Hex = hex, Name = Loc.T("Col." + name) };

        public static readonly ColorChoice[] Colors =
        {
            C("#2563EB", "Blue"), C("#0D9488", "Teal"), C("#16A34A", "Green"), C("#D97706", "Orange"),
            C("#DC2626", "Red"), C("#DB2777", "Pink"), C("#7C3AED", "Purple"), C("#475569", "Gray"),
        };

        static readonly Color[] LetterColors =
        {
            Color.FromRgb(0x25, 0x63, 0xEB), Color.FromRgb(0x0D, 0x94, 0x88), Color.FromRgb(0x7C, 0x3A, 0xED),
            Color.FromRgb(0xDB, 0x27, 0x77), Color.FromRgb(0xD9, 0x77, 0x06), Color.FromRgb(0x05, 0x96, 0x69),
        };

        public static ImageSource Get(Item item, int pixels)
        {
            var source = item.IconFile ?? (item.IsUrl ? null : item.Target);
            var icon = source != null ? FromShell(source, pixels) : null;
            return icon ?? LetterTile(item.Title, pixels);
        }

        static BitmapSource FromShell(string target, int pixels)
        {
            var iid = typeof(Native.IShellItemImageFactory).GUID;
            if (Native.SHCreateItemFromParsingName(target, IntPtr.Zero, ref iid, out var factory) != 0 || factory == null)
                return null;

            try
            {
                var size = new Native.SIZE { Width = pixels, Height = pixels };
                if (factory.GetImage(size, BiggerSizeOk | IconOnly, out var bitmap) != 0)
                    return null;

                try { return CopyPremultiplied(bitmap); }
                finally { Native.DeleteObject(bitmap); }
            }
            finally
            {
                Marshal.ReleaseComObject(factory);
            }
        }

        // Imaging.CreateBitmapSourceFromHBitmap verwirft den Alphakanal, deshalb werden die Pixel direkt kopiert.
        static BitmapSource CopyPremultiplied(IntPtr bitmap)
        {
            var size = Marshal.SizeOf(typeof(Native.DIBSECTION));
            if (Native.GetObject(bitmap, size, out var dib) != size || dib.bmBits == IntPtr.Zero || dib.bmBitsPixel != 32)
                return null;

            var width = dib.bmWidth;
            var height = Math.Abs(dib.bmHeight);
            var stride = dib.bmWidthBytes;
            var pixels = new byte[stride * height];
            Marshal.Copy(dib.bmBits, pixels, 0, pixels.Length);

            if (dib.biHeight > 0)
            {
                var flipped = new byte[pixels.Length];
                for (var y = 0; y < height; y++)
                    Buffer.BlockCopy(pixels, y * stride, flipped, (height - 1 - y) * stride, stride);
                pixels = flipped;
            }

            var source = BitmapSource.Create(width, height, 96, 96, PixelFormats.Pbgra32, null, pixels, stride);
            source.Freeze();
            return source;
        }

        static ImageSource LetterTile(string title, int pixels)
        {
            var text = string.IsNullOrWhiteSpace(title) ? "?" : title.Trim().Substring(0, 1).ToUpperInvariant();
            var hash = (title ?? "").Aggregate(17, (sum, c) => unchecked(sum * 31 + c));
            var color = LetterColors[Math.Abs(hash % LetterColors.Length)];

            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                var radius = pixels * 0.2;
                dc.DrawRoundedRectangle(new SolidColorBrush(color), null, new Rect(0, 0, pixels, pixels), radius, radius);
                var formatted = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                    new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal),
                    pixels * 0.55, Brushes.White, 1.0);
                dc.DrawText(formatted, new Point((pixels - formatted.Width) / 2, (pixels - formatted.Height) / 2));
            }
            return Render(visual, pixels);
        }

        static BitmapSource Render(Visual visual, int pixels)
        {
            var bitmap = new RenderTargetBitmap(pixels, pixels, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);
            bitmap.Freeze();
            return bitmap;
        }

        // Taskleisten-Icon der Gruppe: 2x2-Vorschau der ersten vier Eintraege.
        static BitmapSource Mosaic(IList<ImageSource> icons, string color, int pixels)
        {
            var visual = new DrawingVisual();
            RenderOptions.SetBitmapScalingMode(visual, BitmapScalingMode.Fant);
            using (var dc = visual.RenderOpen())
            {
                var radius = pixels * 0.2;
                var background = new SolidColorBrush(color != null ? (Color)ColorConverter.ConvertFromString(color) : Color.FromRgb(0x2B, 0x31, 0x3D));
                var outline = new Pen(new SolidColorBrush(Color.FromArgb(0x70, 0xFF, 0xFF, 0xFF)), Math.Max(1, pixels / 32.0));
                var inset = outline.Thickness / 2;
                dc.DrawRoundedRectangle(background, outline,
                    new Rect(inset, inset, pixels - 2 * inset, pixels - 2 * inset), radius, radius);

                var padding = pixels * 0.13;
                var gap = pixels * 0.06;
                var cell = (pixels - 2 * padding - gap) / 2;
                for (var i = 0; i < Math.Min(4, icons.Count); i++)
                {
                    var x = padding + (i % 2) * (cell + gap);
                    var y = padding + (i / 2) * (cell + gap);
                    dc.DrawImage(icons[i], new Rect(x, y, cell, cell));
                }

                if (icons.Count == 0)
                {
                    for (var i = 0; i < 4; i++)
                    {
                        var x = padding + (i % 2) * (cell + gap);
                        var y = padding + (i / 2) * (cell + gap);
                        var placeholder = new SolidColorBrush(Color.FromArgb(0x50, 0xFF, 0xFF, 0xFF));
                        dc.DrawRoundedRectangle(placeholder, null, new Rect(x, y, cell, cell), cell * 0.2, cell * 0.2);
                    }
                }
            }
            return Render(visual, pixels);
        }

        static BitmapSource Symbol(string glyph, string color, int pixels)
        {
            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                var radius = pixels * 0.2;
                var background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color ?? DefaultColor));
                dc.DrawRoundedRectangle(background, null, new Rect(0, 0, pixels, pixels), radius, radius);
                var formatted = new FormattedText(GlyphText(glyph), CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                    new Typeface("Segoe Fluent Icons, Segoe MDL2 Assets"), pixels * 0.6, Brushes.White, 1.0);
                dc.DrawText(formatted, new Point((pixels - formatted.Width) / 2, (pixels - formatted.Height) / 2));
            }
            return Render(visual, pixels);
        }

        static BitmapSource Picture(BitmapSource image, int pixels)
        {
            var scale = (double)pixels / Math.Max(image.PixelWidth, image.PixelHeight);
            var width = image.PixelWidth * scale;
            var height = image.PixelHeight * scale;

            var visual = new DrawingVisual();
            RenderOptions.SetBitmapScalingMode(visual, BitmapScalingMode.Fant);
            using (var dc = visual.RenderOpen())
                dc.DrawImage(image, new Rect((pixels - width) / 2, (pixels - height) / 2, width, height));
            return Render(visual, pixels);
        }

        public static string GlyphText(string glyph) => ((char)Convert.ToInt32(glyph, 16)).ToString();

        public static int CountInFile(string path) =>
            (int)Native.PrivateExtractIcons(path, 0, 0, 0, null, null, 0, 0);

        public static BitmapSource FromFile(string path, int index, int pixels)
        {
            var handles = new IntPtr[1];
            var ids = new uint[1];
            if (Native.PrivateExtractIcons(path, index, pixels, pixels, handles, ids, 1, 0) != 1 || handles[0] == IntPtr.Zero)
                return null;

            try
            {
                var source = Imaging.CreateBitmapSourceFromHIcon(handles[0], Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                source.Freeze();
                return source;
            }
            finally
            {
                Native.DestroyIcon(handles[0]);
            }
        }

        // Laedt ein Bild oder das Icon einer Datei. Null, wenn sich daraus kein Bild gewinnen laesst.
        public static BitmapSource LoadPicture(string path)
        {
            var extension = Path.GetExtension(path).ToLowerInvariant();
            if (extension != ".png" && extension != ".jpg" && extension != ".jpeg" && extension != ".bmp")
                return FromShell(path, 256);

            try
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = new Uri(path);
                image.EndInit();
                image.Freeze();
                return image;
            }
            catch (Exception ex) when (ex is IOException || ex is NotSupportedException || ex is UriFormatException)
            {
                return null;
            }
        }

        // Quadratische Fassung fuer die Ablage im Icon-Ordner.
        public static byte[] EncodePicture(BitmapSource image) => EncodePng(Picture(image, 256));

        static BitmapSource LoadGroupImage(Group group)
        {
            if (group.IconImage == null)
                return null;
            var path = Path.Combine(Store.IconDir, group.IconImage);
            return File.Exists(path) ? LoadPicture(path) : null;
        }

        public static BitmapSource RenderChoice(IEnumerable<Item> items, string glyph, string color, BitmapSource image, int pixels)
        {
            if (image != null)
                return Picture(image, pixels);
            if (glyph != null)
                return Symbol(glyph, color, pixels);
            return Mosaic(items.Take(4).Select(item => item.Icon).ToList(), color, pixels);
        }

        public static BitmapSource RenderGroup(Group group, int pixels) =>
            RenderChoice(group.Items, group.IconGlyph, group.IconColor, LoadGroupImage(group), pixels);

        public static byte[] BuildGroupIcon(Group group)
        {
            var image = LoadGroupImage(group);
            int[] sizes = { 16, 20, 24, 32, 40, 48, 64, 256 };
            var images = sizes.Select(size => EncodePng(RenderChoice(group.Items, group.IconGlyph, group.IconColor, image, size))).ToList();

            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write((short)0);
                writer.Write((short)1);
                writer.Write((short)sizes.Length);

                var offset = 6 + 16 * sizes.Length;
                for (var i = 0; i < sizes.Length; i++)
                {
                    var dimension = (byte)(sizes[i] >= 256 ? 0 : sizes[i]);
                    writer.Write(dimension);
                    writer.Write(dimension);
                    writer.Write((byte)0);
                    writer.Write((byte)0);
                    writer.Write((short)1);
                    writer.Write((short)32);
                    writer.Write(images[i].Length);
                    writer.Write(offset);
                    offset += images[i].Length;
                }
                foreach (var png in images)
                    writer.Write(png);

                writer.Flush();
                return stream.ToArray();
            }
        }

        static byte[] EncodePng(BitmapSource bitmap)
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (var stream = new MemoryStream())
            {
                encoder.Save(stream);
                return stream.ToArray();
            }
        }
    }
}
