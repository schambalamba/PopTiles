using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace TaskbarGruppen
{
    // Ergaenzt die Startmenue-Liste um Programme, die Windows als installiert fuehrt,
    // aber ohne Startmenue-Eintrag. Das sind vor allem Spiele aus Steam, Ubisoft Connect, Epic und GOG.
    public static class InstalledPrograms
    {
        const string UninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";

        // Treiber, Laufzeitumgebungen und Hilfsdienste will niemand aus einer Gruppe starten.
        static readonly Regex Technical = new Regex(
            @"redistributable|runtime|\bsdk\b|driver|treiber|toolkit|\bupdate\b|\bupdater\b|service|physx|compatibility|installer|\baddon\b|\badd-on\b|^vs_|gameinput|coapp|plugin|agent|sapi|launcher",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Bei manchen Herstellern zeigt das Icon auf Installer oder Hilfsprogramme statt auf das Programm.
        static readonly Regex NotTheProgram = new Regex(
            @"unins|setup|install|update|helper|crash|clicktorun|package cache|-amd64\.exe|-x64\.exe|\\windows\\", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Spiele dieser Anbieter tragen sich bei Windows ein und duerfen nicht am Namensfilter scheitern.
        static readonly Regex GameStorePublisher = new Regex(@"electronic arts|blizzard", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        static readonly Regex Version = new Regex(@"\s+v?\d+(\.\d+)+.*$", RegexOptions.Compiled);

        static readonly Regex Steam = new Regex(@"steam://uninstall/(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static readonly Regex Ubisoft = new Regex(@"uplay://uninstall/(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // knownTargets sind die Ziele der Startmenue-Eintraege: Dieselbe Programmdatei unter anderem Namen zaehlt als bekannt.
        public static List<Item> Load(IEnumerable<string> knownNames, IEnumerable<string> knownTargets)
        {
            var known = knownNames.Select(Normalize).Where(n => n.Length > 0).ToList();
            var knownFiles = new HashSet<string>(knownTargets
                .Where(t => t.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                .Select(t => t.Substring(t.LastIndexOf('\\') + 1)), StringComparer.OrdinalIgnoreCase);
            var found = new Dictionary<string, Item>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in ReadEntries().Select(ToItem).Concat(ReadEpicGames()).Concat(ReadGogGames()))
            {
                if (item == null || IsKnown(Normalize(item.Title), known) || found.ContainsKey(item.Title))
                    continue;
                if (!item.IsUrl && item.Arguments == null && knownFiles.Contains(Path.GetFileName(item.Target)))
                    continue;
                found[item.Title] = item;
            }
            return found.Values.ToList();
        }

        sealed class Entry
        {
            public string Name, Icon, Uninstall, Publisher;
        }

        [DataContract]
        sealed class EpicManifest
        {
            [DataMember] public string DisplayName;
            [DataMember] public string AppName;
            [DataMember] public string CatalogNamespace;
            [DataMember] public string CatalogItemId;
            [DataMember] public string InstallLocation;
            [DataMember] public string LaunchExecutable;
            [DataMember] public string[] AppCategories;
            [DataMember] public bool bIsIncompleteInstall;
        }

        // Epic traegt seine Spiele nicht bei Windows ein, sondern fuehrt je Spiel eine eigene Datei.
        static IEnumerable<Item> ReadEpicGames()
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                @"Epic\EpicGamesLauncher\Data\Manifests");
            if (!Directory.Exists(folder))
                yield break;

            var serializer = new DataContractJsonSerializer(typeof(EpicManifest));
            foreach (var file in Directory.GetFiles(folder, "*.item"))
            {
                EpicManifest game;
                try
                {
                    using (var stream = File.OpenRead(file))
                        game = (EpicManifest)serializer.ReadObject(stream);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is SerializationException)
                {
                    continue;
                }

                // Der Launcher fuehrt dort auch Engines und Plugins, die keine startbaren Spiele sind.
                if (game.bIsIncompleteInstall || game.AppCategories == null || !game.AppCategories.Contains("games")
                    || string.IsNullOrEmpty(game.DisplayName) || string.IsNullOrEmpty(game.LaunchExecutable))
                    continue;

                yield return new Item
                {
                    Title = game.DisplayName,
                    Target = $"com.epicgames.launcher://apps/{game.CatalogNamespace}%3A{game.CatalogItemId}%3A{game.AppName}?action=launch&silent=true",
                    IconFile = IconPath(Path.Combine(game.InstallLocation ?? "", game.LaunchExecutable)),
                };
            }
        }

        // GOG-Spiele starten teils nur mit Parametern, zum Beispiel ueber ScummVM.
        static IEnumerable<Item> ReadGogGames()
        {
            using (var hive = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
            using (var games = hive.OpenSubKey(@"SOFTWARE\GOG.com\Games"))
            {
                if (games == null)
                    yield break;

                foreach (var id in games.GetSubKeyNames())
                {
                    using (var key = games.OpenSubKey(id))
                    {
                        var name = key?.GetValue("gameName") as string;
                        var exe = IconPath(key?.GetValue("exe") as string);
                        if (string.IsNullOrWhiteSpace(name) || exe == null)
                            continue;

                        var parameters = key.GetValue("launchParam") as string;
                        var ownIcon = IconPath(Path.Combine(key.GetValue("path") as string ?? "", $"goggame-{id}.ico"));
                        yield return new Item
                        {
                            Title = name,
                            Target = exe,
                            Arguments = string.IsNullOrWhiteSpace(parameters) ? null : parameters,
                            WorkingDir = key.GetValue("workingDir") as string,
                            IconFile = ownIcon ?? exe,
                        };
                    }
                }
            }
        }

        static IEnumerable<Entry> ReadEntries()
        {
            var roots = new[]
            {
                new { Hive = RegistryHive.LocalMachine, View = RegistryView.Registry64 },
                new { Hive = RegistryHive.LocalMachine, View = RegistryView.Registry32 },
                new { Hive = RegistryHive.CurrentUser, View = RegistryView.Default },
            };

            foreach (var root in roots)
            {
                using (var hive = RegistryKey.OpenBaseKey(root.Hive, root.View))
                using (var uninstall = hive.OpenSubKey(UninstallKey))
                {
                    if (uninstall == null)
                        continue;

                    foreach (var name in uninstall.GetSubKeyNames())
                    {
                        using (var key = uninstall.OpenSubKey(name))
                        {
                            if (key == null || key.GetValue("ParentKeyName") != null || (key.GetValue("SystemComponent") as int? ?? 0) != 0)
                                continue;
                            if (key.GetValue("DisplayName") is string displayName && displayName.Trim().Length > 0)
                                yield return new Entry { Name = displayName.Trim(), Icon = key.GetValue("DisplayIcon") as string, Uninstall = key.GetValue("UninstallString") as string, Publisher = key.GetValue("Publisher") as string };
                        }
                    }
                }
            }
        }

        static Item ToItem(Entry entry)
        {
            var icon = IconPath(entry.Icon);
            var uninstall = entry.Uninstall ?? "";

            var steam = Steam.Match(uninstall);
            if (steam.Success)
                return new Item { Title = entry.Name, Target = "steam://rungameid/" + steam.Groups[1].Value, IconFile = icon };

            var ubisoft = Ubisoft.Match(uninstall);
            if (ubisoft.Success)
                return new Item { Title = entry.Name, Target = $"uplay://launch/{ubisoft.Groups[1].Value}/0", IconFile = icon };

            // Ohne Launcher ist das Icon der einzige Hinweis auf die Programmdatei. Sonst bleibt der Weg ueber "Datei".
            var fromGameStore = GameStorePublisher.IsMatch(entry.Publisher ?? "");
            if ((!fromGameStore && Technical.IsMatch(entry.Name)) || icon == null
                || !icon.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                || NotTheProgram.IsMatch(icon))
                return null;

            return new Item { Title = Version.Replace(entry.Name, ""), Target = icon, IconFile = icon };
        }

        // DisplayIcon sieht je nach Hersteller so aus: "C:\Pfad\app.exe",0 oder C:/Pfad/app.ico
        static string IconPath(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            var path = Regex.Replace(raw.Trim(), @",\s*-?\d+\s*$", "").Trim().Trim('"').Replace('/', '\\');
            try
            {
                return File.Exists(path) ? path : null;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        static string Normalize(string name) =>
            Regex.Replace(Regex.Replace(name ?? "", @"[^\p{L}\p{N}+]+", " "), @"\s+", " ").Trim().ToLowerInvariant();

        // "Notepad++ (64-bit x64)" und "Mozilla Firefox" gehoeren zu den Startmenue-Eintraegen "Notepad++" und "Firefox".
        static bool IsKnown(string title, List<string> known) =>
            known.Any(name => (" " + title + " ").Contains(" " + name + " "));
    }
}
