using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using System.Threading;
using System.Windows;

namespace TaskbarGruppen
{
    public static class Shortcuts
    {
        const ushort VT_LPWSTR = 31;
        const int ErrorCancelled = 1223;
        const string UnpinVerb = "taskbarunpin";
        const int UnpinWaitMs = 2000;
        const int UnpinPollMs = 100;

        static readonly string[] UnpinNames = { "Unpin from taskbar", "Von Taskleiste lösen", "Détacher de la barre des tâches", "Desanclar de la barra de tareas", "Rimuovi dalla barra delle applicazioni" };

        static readonly string ExePath = Assembly.GetEntryAssembly().Location;

        public static string GroupAppId(string groupId) => "TaskbarGruppen.Group." + groupId;

        public static string ShortcutPath(Group group) => Path.Combine(Store.ShortcutDir, group.ShortcutFile ?? "");

        // Bringt Icon und Startmenue-Verknuepfung einer Gruppe auf den aktuellen Stand.
        public static void Sync(Group group, Library library)
        {
            var iconPath = WriteIcon(group);

            var file = UniqueFileName(group, library);
            if (group.ShortcutFile != null && group.ShortcutFile != file)
                DeleteIfExists(ShortcutPath(group));
            group.ShortcutFile = file;

            Directory.CreateDirectory(Store.ShortcutDir);
            var shortcutPath = ShortcutPath(group);
            var arguments = "--group " + group.Id;
            Write(shortcutPath, arguments, iconPath, group.Name, GroupAppId(group.Id));

            // Angepinnt ist eine Kopie der Verknuepfung, die sonst auf einem alten Stand bliebe.
            foreach (var pinned in PinnedCopies(arguments))
            {
                Write(pinned, arguments, iconPath, group.Name, GroupAppId(group.Id));
                NotifyUpdated(pinned);
            }

            // Der Explorer soll das geaenderte Icon neu einlesen.
            NotifyUpdated(iconPath);
            NotifyUpdated(shortcutPath);
            Native.SHChangeNotify(Native.SHCNE_ASSOCCHANGED, Native.SHCNF_IDLIST | Native.SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);
        }

        // Startmenue-Eintrag, ueber den die Verwaltung spaeter wieder auffindbar ist.
        public static void WriteManagerShortcut()
        {
            Directory.CreateDirectory(Store.ShortcutDir);
            Write(Path.Combine(Store.ShortcutDir, Loc.AppName + ".lnk"), "", ExePath,
                Loc.AppName, "TaskbarGruppen.Manager");
        }

        public static bool IsPinned(Group group) => PinnedCopies("--group " + group.Id).Any();

        // Startet die Rueckfrage von Windows in einem eigenen Prozess mit der Kennung der Gruppe.
        public static Process StartPin(Group group) =>
            Process.Start(new ProcessStartInfo(ExePath, "--pin " + group.Id) { UseShellExecute = true });

        // Loest die Gruppe ueber denselben Befehl wie der Rechtsklick in der Taskleiste.
        // Das muss vor dem Loeschen der Verknuepfungen passieren, sonst bietet Windows ihn nicht mehr an.
        static bool Unpin(Group group)
        {
            var pinned = PinnedCopies("--group " + group.Id);
            foreach (var path in pinned)
            {
                try
                {
                    dynamic shell = Activator.CreateInstance(Type.GetTypeFromProgID("Shell.Application"));
                    dynamic item = shell.NameSpace(Path.GetDirectoryName(path)).ParseName(Path.GetFileName(path));
                    item.InvokeVerb(UnpinVerb);
                    if (!WaitUntilGone(path))
                    {
                        // Manche Windows-Versionen kennen nur den angezeigten Namen des Befehls.
                        foreach (dynamic verb in item.Verbs())
                        {
                            string name = verb.Name;
                            if (IsUnpinName(name))
                            {
                                verb.DoIt();
                                break;
                            }
                        }
                        WaitUntilGone(path);
                    }
                }
                catch (COMException)
                {
                    // Dann bleibt nur der Hinweis an den Nutzer.
                }
            }
            return pinned.All(path => !File.Exists(path));
        }

        static bool IsUnpinName(string name)
        {
            var plain = (name ?? "").Replace("&", "");
            return UnpinNames.Any(n => plain.IndexOf(n, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        static bool WaitUntilGone(string path)
        {
            for (var waited = 0; waited < UnpinWaitMs && File.Exists(path); waited += UnpinPollMs)
                Thread.Sleep(UnpinPollMs);
            return !File.Exists(path);
        }

        // Liefert false, wenn das Taskleisten-Icon nicht automatisch geloest werden konnte.
        public static bool Remove(Group group)
        {
            var unpinned = Unpin(group);
            if (group.ShortcutFile != null)
                DeleteIfExists(ShortcutPath(group));
            if (Directory.Exists(Store.IconDir))
                foreach (var file in Directory.GetFiles(Store.IconDir, group.Id + "*"))
                    DeleteIfExists(file);
            return unpinned;
        }

        // Der Dateiname enthaelt einen Hash des Inhalts: Windows merkt sich Icons je Pfad,
        // ein neuer Name zwingt die Taskleiste, ein geaendertes Icon wirklich neu zu lesen.
        static string WriteIcon(Group group)
        {
            var bytes = Icons.BuildGroupIcon(group);
            string hash;
            using (var md5 = MD5.Create())
                hash = BitConverter.ToString(md5.ComputeHash(bytes), 0, 4).Replace("-", "").ToLowerInvariant();

            Directory.CreateDirectory(Store.IconDir);
            var path = Path.Combine(Store.IconDir, $"{group.Id}-{hash}.ico");
            if (!File.Exists(path))
                File.WriteAllBytes(path, bytes);

            foreach (var old in Directory.GetFiles(Store.IconDir, group.Id + "*.ico").Where(f => f != path))
            {
                try { File.Delete(old); }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    // Ein vom Explorer noch benutztes altes Icon stoert nicht und faellt beim naechsten Mal weg.
                }
            }
            return path;
        }

        public static void Launch(Item item)
        {
            try
            {
                var info = new ProcessStartInfo(item.Target) { UseShellExecute = true };
                if (item.Arguments != null)
                    info.Arguments = item.Arguments;
                if (item.WorkingDir != null && Directory.Exists(item.WorkingDir))
                    info.WorkingDirectory = item.WorkingDir;
                else if (File.Exists(item.Target))
                    info.WorkingDirectory = Path.GetDirectoryName(item.Target);
                Process.Start(info);
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == ErrorCancelled)
            {
                // Abbruch im UAC-Dialog ist kein Fehler.
            }
            catch (Win32Exception ex)
            {
                MessageBox.Show(string.Format(Loc.T("CouldNotStart"), item.Title, ex.Message),
                    Loc.AppName, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Die Anleitung liegt in der Sprache des Programms neben der EXE, Englisch ist der Rueckfall.
        public static void OpenHelp()
        {
            var folder = Path.Combine(Path.GetDirectoryName(ExePath), "docs");
            var path = Path.Combine(folder, $"README.{Loc.Language}.txt");
            if (!File.Exists(path))
                path = Path.Combine(folder, "README.en.txt");
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }

        public static void OpenManager(string groupId)
        {
            Process.Start(new ProcessStartInfo(ExePath, "--edit " + groupId) { UseShellExecute = true });
        }

        static string UniqueFileName(Group group, Library library)
        {
            var name = string.Concat((group.Name ?? "").Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c)).Trim().TrimEnd('.');
            if (name.Length == 0)
                name = Loc.T("DefaultGroup");

            var candidate = name + ".lnk";
            for (var n = 2; library.Groups.Any(g => g != group && string.Equals(g.ShortcutFile, candidate, StringComparison.OrdinalIgnoreCase)); n++)
                candidate = $"{name} {n}.lnk";
            return candidate;
        }

        static IEnumerable<string> PinnedCopies(string arguments)
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                @"Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar");
            if (Store.IsTestRun || !Directory.Exists(folder))
                return Enumerable.Empty<string>();
            return Directory.GetFiles(folder, "*.lnk").Where(path => ReadArguments(path) == arguments).ToList();
        }

        static string ReadArguments(string path)
        {
            const int Capacity = 1024;
            var link = (Native.IShellLinkW)new Native.ShellLink();
            var buffer = Marshal.AllocHGlobal(Capacity * sizeof(char));
            try
            {
                ((IPersistFile)link).Load(path, 0);
                link.GetArguments(buffer, Capacity);
                return Marshal.PtrToStringUni(buffer);
            }
            catch (COMException)
            {
                return null;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
                Marshal.ReleaseComObject(link);
            }
        }

        static void Write(string path, string arguments, string iconPath, string description, string appId)
        {
            var link = (Native.IShellLinkW)new Native.ShellLink();
            try
            {
                link.SetPath(ExePath);
                link.SetArguments(arguments);
                link.SetWorkingDirectory(Path.GetDirectoryName(ExePath));
                link.SetIconLocation(iconPath, 0);
                link.SetDescription(description);

                // Eine eigene AppID je Gruppe verhindert, dass Windows die angepinnten Icons zusammenlegt.
                var store = (Native.IPropertyStore)link;
                var key = new Native.PROPERTYKEY { fmtid = new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"), pid = 5 };
                var value = new Native.PROPVARIANT { vt = VT_LPWSTR, value = Marshal.StringToCoTaskMemUni(appId) };
                try
                {
                    store.SetValue(ref key, ref value);
                    store.Commit();
                }
                finally
                {
                    Marshal.FreeCoTaskMem(value.value);
                }

                ((IPersistFile)link).Save(path, true);
            }
            finally
            {
                Marshal.ReleaseComObject(link);
            }
        }

        static void NotifyUpdated(string path)
        {
            var pointer = Marshal.StringToHGlobalUni(path);
            try { Native.SHChangeNotify(Native.SHCNE_UPDATEITEM, Native.SHCNF_PATHW | Native.SHCNF_FLUSH, pointer, IntPtr.Zero); }
            finally { Marshal.FreeHGlobal(pointer); }
        }

        static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
