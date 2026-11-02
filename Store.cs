using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows.Media;

namespace TaskbarGruppen
{
    [DataContract]
    public class Item : INotifyPropertyChanged
    {
        string _title;
        ImageSource _icon;

        [DataMember(Order = 1)]
        public string Title
        {
            get => _title;
            set { _title = value; Notify(); }
        }

        // Programm, Datei, Ordner, URL oder shell:AppsFolder\<AppID> fuer Store-Apps.
        [DataMember(Order = 2)]
        public string Target { get; set; }

        // Fuer Ziele ohne eigenes Icon, zum Beispiel steam://rungameid/...
        [DataMember(Order = 3, EmitDefaultValue = false)]
        public string IconFile { get; set; }

        // Startparameter und Arbeitsordner, die manche Spiele zwingend brauchen.
        [DataMember(Order = 4, EmitDefaultValue = false)]
        public string Arguments { get; set; }

        [DataMember(Order = 5, EmitDefaultValue = false)]
        public string WorkingDir { get; set; }

        public ImageSource Icon => _icon ?? (_icon = Icons.Get(this, 96));

        public bool IsUrl => Target != null && Target.Contains("://");

        public event PropertyChangedEventHandler PropertyChanged;

        void Notify([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    [DataContract]
    public class Group : INotifyPropertyChanged
    {
        string _name;

        [DataMember(Order = 1)]
        public string Id { get; set; }

        [DataMember(Order = 2)]
        public string Name
        {
            get => _name;
            set { _name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name))); }
        }

        [DataMember(Order = 3)]
        public ObservableCollection<Item> Items { get; set; } = new ObservableCollection<Item>();

        // Dateiname der aktuellen Startmenue-Verknuepfung, damit sie beim Umbenennen ersetzt werden kann.
        [DataMember(Order = 4)]
        public string ShortcutFile { get; set; }

        // Gruppen-Icon: Bild vor Symbol. Ist beides leer, entsteht es automatisch aus den Eintraegen.
        [DataMember(Order = 5, EmitDefaultValue = false)]
        public string IconGlyph { get; set; }

        [DataMember(Order = 6, EmitDefaultValue = false)]
        public string IconColor { get; set; }

        // Dateiname im Icon-Ordner. Das Bild wird kopiert, damit das Original wegfallen darf.
        [DataMember(Order = 7, EmitDefaultValue = false)]
        public string IconImage { get; set; }

        // Faerbt zusaetzlich die Kachel in IconColor ein.
        [DataMember(Order = 8, EmitDefaultValue = false)]
        public bool TileColor { get; set; }

        // Anordnung in der Kachel: row, 2, 3, 4 oder list. Leer bedeutet automatisch.
        [DataMember(Order = 9, EmitDefaultValue = false)]
        public string Layout { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        [OnDeserialized]
        void OnDeserialized(StreamingContext context)
        {
            if (Items == null)
                Items = new ObservableCollection<Item>();
        }
    }

    [DataContract]
    public class Library
    {
        [DataMember]
        public ObservableCollection<Group> Groups { get; set; } = new ObservableCollection<Group>();

        public Group Find(string id) => Groups.FirstOrDefault(g => g.Id == id);
    }

    public static class Store
    {
        // Die Umgebungsvariable trennt Testlaeufe von den echten Gruppen und dem echten Startmenue.
        static readonly string Override = Environment.GetEnvironmentVariable("TASKBARGRUPPEN_DATA");

        // Bewusst neben der EXE statt in %APPDATA%: Wird das Tool aus einer als Paket installierten App
        // gestartet, leitet Windows %APPDATA% in einen privaten Bereich um, den die Taskleiste nicht sieht.
        public static readonly string Dir = Override ?? Path.Combine(
            Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "data");

        public static readonly string ShortcutDir = Override != null
            ? Path.Combine(Override, "shortcuts")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), Loc.AppName);

        public static bool IsTestRun => Override != null;

        public static string IconDir => Path.Combine(Dir, "icons");

        static string FilePath => Path.Combine(Dir, "groups.json");
        static string StateDir => Path.Combine(Dir, "state");

        public static Library Load()
        {
            if (!File.Exists(FilePath))
                return new Library();

            try
            {
                using (var stream = File.OpenRead(FilePath))
                {
                    var library = (Library)new DataContractJsonSerializer(typeof(Library)).ReadObject(stream);
                    if (library.Groups == null)
                        library.Groups = new ObservableCollection<Group>();
                    return library;
                }
            }
            catch (Exception ex) when (ex is SerializationException || ex is InvalidCastException)
            {
                throw new InvalidDataException(Loc.T("GroupFileDamaged") + "\n" + FilePath, ex);
            }
        }

        public static void Save(Library library)
        {
            Directory.CreateDirectory(Dir);
            var temp = FilePath + ".tmp";
            using (var stream = File.Create(temp))
            using (var writer = JsonReaderWriterFactory.CreateJsonWriter(stream, new UTF8Encoding(false), false, true))
                new DataContractJsonSerializer(typeof(Library)).WriteObject(writer, library);

            if (File.Exists(FilePath))
                File.Replace(temp, FilePath, null);
            else
                File.Move(temp, FilePath);
        }

        public static void MarkClosed(string groupId)
        {
            Directory.CreateDirectory(StateDir);
            var path = Path.Combine(StateDir, groupId);
            File.WriteAllBytes(path, new byte[0]);
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow);
        }

        public static bool ClosedRecently(string groupId, int milliseconds)
        {
            var path = Path.Combine(StateDir, groupId);
            return File.Exists(path)
                && (DateTime.UtcNow - File.GetLastWriteTimeUtc(path)).TotalMilliseconds < milliseconds;
        }
    }
}
