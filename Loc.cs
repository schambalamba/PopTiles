using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;

namespace TaskbarGruppen
{
    // Alle sichtbaren Texte. Die Spalten folgen der Reihenfolge in Languages, Englisch ist der Rueckfall.
    // XAML greift ueber {DynamicResource L.Schluessel} darauf zu, der Code ueber Loc.T("Schluessel").
    public static class Loc
    {
        public const string AppName = "PopTiles";

        static readonly string[] Languages = { "en", "de", "fr", "es", "it" };

        // Die Umgebungsvariable erlaubt Tests einer Sprache, ohne Windows umzustellen.
        static readonly int Column = Math.Max(0, Array.IndexOf(Languages,
            Environment.GetEnvironmentVariable("TASKBARGRUPPEN_LANG") ?? CultureInfo.CurrentUICulture.TwoLetterISOLanguageName));

        public static string Language => Languages[Column];

        public static string T(string key) => Texts.TryGetValue(key, out var row) ? row[Column] : key;

        public static void Apply(ResourceDictionary resources)
        {
            resources["L.AppName"] = AppName;
            foreach (var entry in Texts)
                resources["L." + entry.Key] = entry.Value[Column];
        }

        static readonly Dictionary<string, string[]> Texts = new Dictionary<string, string[]>
        {
            ["GroupGone"] = new[] { "This group no longer exists. You can unpin its icon from the taskbar.", "Diese Gruppe gibt es nicht mehr. Du kannst ihr Icon von der Taskleiste lösen.", "Ce groupe n'existe plus. Tu peux détacher son icône de la barre des tâches.", "Este grupo ya no existe. Puedes desanclar su icono de la barra de tareas.", "Questo gruppo non esiste più. Puoi rimuovere la sua icona dalla barra delle applicazioni." },
            ["GroupFileDamaged"] = new[] { "The group file is damaged:", "Die Gruppen-Datei ist beschädigt:", "Le fichier des groupes est endommagé :", "El archivo de grupos está dañado:", "Il file dei gruppi è danneggiato:" },
            ["CouldNotStart"] = new[] { "Could not start {0}:\n{1}", "{0} konnte nicht gestartet werden:\n{1}", "Impossible de lancer {0} :\n{1}", "No se pudo iniciar {0}:\n{1}", "Impossibile avviare {0}:\n{1}" },
            ["DefaultGroup"] = new[] { "Group", "Gruppe", "Groupe", "Grupo", "Gruppo" },
            ["Pinning"] = new[] { "Pinning \"{0}\" to the taskbar …", "„{0}“ wird an die Taskleiste angeheftet …", "Épinglage de « {0} » à la barre des tâches …", "Anclando «{0}» a la barra de tareas …", "Aggiunta di «{0}» alla barra delle applicazioni …" },
            ["EditGroup"] = new[] { "Edit group", "Gruppe bearbeiten", "Modifier le groupe", "Editar grupo", "Modifica gruppo" },
            ["TileEmpty"] = new[] { "No items yet. The gear opens the editor.", "Noch keine Einträge. Das Zahnrad öffnet die Bearbeitung.", "Aucun élément pour l'instant. La roue dentée ouvre l'édition.", "Aún no hay elementos. El engranaje abre la edición.", "Ancora nessun elemento. L'ingranaggio apre la modifica." },

            ["Help"] = new[] { "Help", "Hilfe", "Aide", "Ayuda", "Guida" },
            ["Layout"] = new[] { "Layout", "Anordnung", "Disposition", "Disposición", "Disposizione" },
            ["LayoutAuto"] = new[] { "Automatic", "Automatisch", "Automatique", "Automática", "Automatica" },
            ["LayoutRow"] = new[] { "One row", "Eine Reihe", "Une ligne", "Una fila", "Una riga" },
            ["Columns2"] = new[] { "2 columns", "2 Spalten", "2 colonnes", "2 columnas", "2 colonne" },
            ["Columns3"] = new[] { "3 columns", "3 Spalten", "3 colonnes", "3 columnas", "3 colonne" },
            ["Columns4"] = new[] { "4 columns", "4 Spalten", "4 colonnes", "4 columnas", "4 colonne" },
            ["LayoutList"] = new[] { "List", "Liste", "Liste", "Lista", "Elenco" },
            ["CustomColor"] = new[] { "Custom color …", "Eigene Farbe …", "Couleur personnalisée …", "Color personalizado …", "Colore personalizzato …" },
            ["TileColor"] = new[] { "Also use this color for the tile", "Farbe auch für die Kachel verwenden", "Utiliser aussi cette couleur pour la tuile", "Usar este color también para el mosaico", "Usa questo colore anche per il riquadro" },
            ["Groups"] = new[] { "Groups", "Gruppen", "Groupes", "Grupos", "Gruppi" },
            ["NewGroup"] = new[] { "New group", "Neue Gruppe", "Nouveau groupe", "Nuevo grupo", "Nuovo gruppo" },
            ["Delete"] = new[] { "Delete", "Löschen", "Supprimer", "Eliminar", "Elimina" },
            ["Name"] = new[] { "Name", "Name", "Nom", "Nombre", "Nome" },
            ["ChangeIcon"] = new[] { "Change icon", "Icon ändern", "Changer l'icône", "Cambiar icono", "Cambia icona" },
            ["AddApp"] = new[] { "Add app", "App hinzufügen", "Ajouter une app", "Añadir app", "Aggiungi app" },
            ["File"] = new[] { "File", "Datei", "Fichier", "Archivo", "File" },
            ["Folder"] = new[] { "Folder", "Ordner", "Dossier", "Carpeta", "Cartella" },
            ["ItemsHint"] = new[] { "Add apps, or drag files and shortcuts here.", "Füge Apps hinzu oder ziehe Dateien und Verknüpfungen hierher.", "Ajoute des apps ou fais glisser ici des fichiers et des raccourcis.", "Añade apps o arrastra aquí archivos y accesos directos.", "Aggiungi app o trascina qui file e collegamenti." },
            ["DisplayName"] = new[] { "Display name", "Anzeigename", "Nom affiché", "Nombre visible", "Nome visualizzato" },
            ["Remove"] = new[] { "Remove", "Entfernen", "Retirer", "Quitar", "Rimuovi" },
            ["WebAddress"] = new[] { "Web address", "Web-Adresse", "Adresse web", "Dirección web", "Indirizzo web" },
            ["Add"] = new[] { "Add", "Hinzufügen", "Ajouter", "Añadir", "Aggiungi" },
            ["PinToTaskbar"] = new[] { "Pin to taskbar", "An Taskleiste anheften", "Épingler à la barre des tâches", "Anclar a la barra de tareas", "Aggiungi alla barra delle applicazioni" },
            ["PinnedState"] = new[] { "Pinned", "Ist angeheftet", "Épinglé", "Anclado", "Aggiunto" },
            ["PinNote"] = new[] { "New groups are pinned right away, and Windows asks you to confirm once. Changes are saved automatically.", "Neue Gruppen werden direkt angeheftet, Windows fragt dabei einmal nach. Änderungen werden automatisch gespeichert.", "Les nouveaux groupes sont épinglés aussitôt, Windows demande une seule confirmation. Les modifications sont enregistrées automatiquement.", "Los grupos nuevos se anclan de inmediato y Windows pide confirmación una vez. Los cambios se guardan automáticamente.", "I nuovi gruppi vengono aggiunti subito e Windows chiede una conferma. Le modifiche vengono salvate automaticamente." },
            ["NoGroupHint"] = new[] { "Create your first group on the left.", "Lege links deine erste Gruppe an.", "Crée ton premier groupe à gauche.", "Crea tu primer grupo a la izquierda.", "Crea il tuo primo gruppo a sinistra." },
            ["DeleteGroupTitle"] = new[] { "Delete group", "Gruppe löschen", "Supprimer le groupe", "Eliminar grupo", "Elimina gruppo" },
            ["DeleteGroupConfirm"] = new[] { "Delete group \"{0}\"? Its icon will also be unpinned from the taskbar.", "Gruppe „{0}“ löschen? Ihr Icon wird dabei auch von der Taskleiste gelöst.", "Supprimer le groupe « {0} » ? Son icône sera aussi détachée de la barre des tâches.", "¿Eliminar el grupo «{0}»? Su icono también se desanclará de la barra de tareas.", "Eliminare il gruppo «{0}»? Anche la sua icona verrà rimossa dalla barra delle applicazioni." },
            ["UnpinFailed"] = new[] { "The group was deleted, but its icon could not be unpinned automatically. Right-click it in the taskbar to unpin it.", "Die Gruppe ist gelöscht, ihr Icon konnte aber nicht automatisch gelöst werden. Löse es per Rechtsklick in der Taskleiste.", "Le groupe est supprimé, mais son icône n'a pas pu être détachée automatiquement. Fais un clic droit dessus dans la barre des tâches pour la détacher.", "El grupo se ha eliminado, pero su icono no se pudo desanclar automáticamente. Haz clic derecho en él en la barra de tareas para desanclarlo.", "Il gruppo è stato eliminato, ma non è stato possibile rimuovere automaticamente la sua icona. Fai clic destro su di essa nella barra delle applicazioni per rimuoverla." },
            ["SelectFileProgram"] = new[] { "Select a file or program", "Datei oder Programm auswählen", "Choisir un fichier ou un programme", "Seleccionar un archivo o programa", "Seleziona un file o un programma" },
            ["SelectFolder"] = new[] { "Select a folder", "Ordner auswählen", "Choisir un dossier", "Seleccionar una carpeta", "Seleziona una cartella" },
            ["InvalidUrl"] = new[] { "That doesn't look like a valid web address.", "Das sieht nicht nach einer gültigen Web-Adresse aus.", "Cela ne ressemble pas à une adresse web valide.", "Eso no parece una dirección web válida.", "Non sembra un indirizzo web valido." },
            ["PinDenied"] = new[] { "Windows did not allow automatic pinning. Show the shortcut in Explorer? You can right-click it there and pin it to the taskbar yourself.", "Windows hat das automatische Anheften nicht zugelassen. Soll der Explorer die Verknüpfung zeigen? Dort kannst du sie per Rechtsklick selbst an die Taskleiste anheften.", "Windows n'a pas autorisé l'épinglage automatique. Afficher le raccourci dans l'Explorateur ? Tu pourras l'épingler toi-même à la barre des tâches par un clic droit.", "Windows no ha permitido el anclaje automático. ¿Mostrar el acceso directo en el Explorador? Allí puedes anclarlo a la barra de tareas con el botón derecho.", "Windows non ha consentito l'aggiunta automatica. Mostrare il collegamento in Esplora file? Da lì puoi aggiungerlo tu alla barra delle applicazioni con il tasto destro." },

            ["SelectSeveral"] = new[] { "Hold Ctrl or Shift to select several", "Mehrere: Strg oder Umschalt halten", "Plusieurs : maintiens Ctrl ou Maj", "Varios: mantén Ctrl o Mayús", "Più voci: tieni premuto Ctrl o Maiusc" },
            ["Cancel"] = new[] { "Cancel", "Abbrechen", "Annuler", "Cancelar", "Annulla" },
            ["ChooseIcon"] = new[] { "Choose icon", "Icon wählen", "Choisir l'icône", "Elegir icono", "Scegli icona" },
            ["AutomaticIcon"] = new[] { "Automatic from the first four items", "Automatisch aus den ersten vier Einträgen", "Automatique à partir des quatre premiers éléments", "Automático a partir de los cuatro primeros elementos", "Automatica dai primi quattro elementi" },
            ["Symbol"] = new[] { "Symbol", "Symbol", "Symbole", "Símbolo", "Simbolo" },
            ["Color"] = new[] { "Color", "Farbe", "Couleur", "Color", "Colore" },
            ["ItemIcon"] = new[] { "Icon of an item", "Icon eines Eintrags", "Icône d'un élément", "Icono de un elemento", "Icona di un elemento" },
            ["InstalledIcons"] = new[] { "Installed icons …", "Installierte Icons …", "Icônes installées …", "Iconos instalados …", "Icone installate …" },
            ["ChooseImage"] = new[] { "Choose image or program …", "Bild oder Programm wählen …", "Choisir une image ou un programme …", "Elegir imagen o programa …", "Scegli immagine o programma …" },
            ["Apply"] = new[] { "Apply", "Übernehmen", "Appliquer", "Aplicar", "Applica" },
            ["SelectImageProgram"] = new[] { "Select an image or program", "Bild oder Programm auswählen", "Choisir une image ou un programme", "Seleccionar una imagen o programa", "Seleziona un'immagine o un programma" },
            ["ImagesAndPrograms"] = new[] { "Images and programs", "Bilder und Programme", "Images et programmes", "Imágenes y programas", "Immagini e programmi" },
            ["AllFiles"] = new[] { "All files", "Alle Dateien", "Tous les fichiers", "Todos los archivos", "Tutti i file" },
            ["NoImage"] = new[] { "Could not load an image from this file.", "Aus dieser Datei lässt sich kein Bild laden.", "Impossible de charger une image à partir de ce fichier.", "No se pudo cargar una imagen de este archivo.", "Impossibile caricare un'immagine da questo file." },
            ["InstalledIconsTitle"] = new[] { "Installed icons", "Installierte Icons", "Icônes installées", "Iconos instalados", "Icone installate" },
            ["Search"] = new[] { "Search", "Suche", "Rechercher", "Buscar", "Cerca" },
            ["GalleryHint"] = new[] { "Click an icon to use it. Search finds apps by name.", "Klick auf ein Icon übernimmt es. Die Suche findet Apps nach Namen.", "Clique sur une icône pour l'utiliser. La recherche trouve les apps par nom.", "Haz clic en un icono para usarlo. La búsqueda encuentra apps por nombre.", "Fai clic su un'icona per usarla. La ricerca trova le app per nome." },
            ["GroupName"] = new[] { "Group name", "Name der Gruppe", "Nom du groupe", "Nombre del grupo", "Nome del gruppo" },
            ["Create"] = new[] { "Create", "Erstellen", "Créer", "Crear", "Crea" },

            ["Sym.Folder"] = new[] { "Folder", "Ordner", "Dossier", "Carpeta", "Cartella" },
            ["Sym.Home"] = new[] { "Home", "Haus", "Maison", "Casa", "Casa" },
            ["Sym.Work"] = new[] { "Work", "Arbeit", "Travail", "Trabajo", "Lavoro" },
            ["Sym.Internet"] = new[] { "Internet", "Internet", "Internet", "Internet", "Internet" },
            ["Sym.Mail"] = new[] { "Mail", "Mail", "Courrier", "Correo", "Posta" },
            ["Sym.Chat"] = new[] { "Chat", "Chat", "Discussion", "Chat", "Chat" },
            ["Sym.People"] = new[] { "People", "Personen", "Personnes", "Personas", "Persone" },
            ["Sym.Calendar"] = new[] { "Calendar", "Kalender", "Calendrier", "Calendario", "Calendario" },
            ["Sym.Document"] = new[] { "Document", "Dokument", "Document", "Documento", "Documento" },
            ["Sym.Edit"] = new[] { "Edit", "Bearbeiten", "Modifier", "Editar", "Modifica" },
            ["Sym.Paint"] = new[] { "Paint", "Farbe", "Peinture", "Pintura", "Pittura" },
            ["Sym.Photo"] = new[] { "Photo", "Foto", "Photo", "Foto", "Foto" },
            ["Sym.Camera"] = new[] { "Camera", "Kamera", "Appareil photo", "Cámara", "Fotocamera" },
            ["Sym.Video"] = new[] { "Video", "Video", "Vidéo", "Vídeo", "Video" },
            ["Sym.Music"] = new[] { "Music", "Musik", "Musique", "Música", "Musica" },
            ["Sym.Games"] = new[] { "Games", "Spiele", "Jeux", "Juegos", "Giochi" },
            ["Sym.Code"] = new[] { "Code", "Code", "Code", "Código", "Codice" },
            ["Sym.Console"] = new[] { "Console", "Konsole", "Console", "Consola", "Console" },
            ["Sym.Settings"] = new[] { "Settings", "Einstellungen", "Paramètres", "Ajustes", "Impostazioni" },
            ["Sym.Tools"] = new[] { "Tools", "Werkzeug", "Outils", "Herramientas", "Strumenti" },
            ["Sym.Calculator"] = new[] { "Calculator", "Rechner", "Calculatrice", "Calculadora", "Calcolatrice" },
            ["Sym.Chart"] = new[] { "Chart", "Diagramm", "Graphique", "Gráfico", "Grafico" },
            ["Sym.Shopping"] = new[] { "Shopping", "Einkaufen", "Achats", "Compras", "Acquisti" },
            ["Sym.Learning"] = new[] { "Learning", "Lernen", "Apprentissage", "Aprendizaje", "Studio" },
            ["Sym.Cloud"] = new[] { "Cloud", "Cloud", "Cloud", "Nube", "Cloud" },
            ["Sym.Download"] = new[] { "Download", "Download", "Téléchargement", "Descarga", "Download" },
            ["Sym.Print"] = new[] { "Print", "Drucken", "Impression", "Imprimir", "Stampa" },
            ["Sym.Security"] = new[] { "Security", "Sicherheit", "Sécurité", "Seguridad", "Sicurezza" },
            ["Sym.Star"] = new[] { "Star", "Stern", "Étoile", "Estrella", "Stella" },
            ["Sym.Heart"] = new[] { "Heart", "Herz", "Cœur", "Corazón", "Cuore" },
            ["Sym.Idea"] = new[] { "Idea", "Idee", "Idée", "Idea", "Idea" },
            ["Sym.Flag"] = new[] { "Flag", "Flagge", "Drapeau", "Bandera", "Bandiera" },
            ["Sym.Library"] = new[] { "Library", "Bibliothek", "Bibliothèque", "Biblioteca", "Libreria" },
            ["Sym.Travel"] = new[] { "Travel", "Reisen", "Voyage", "Viajes", "Viaggi" },
            ["Sym.Car"] = new[] { "Car", "Auto", "Voiture", "Coche", "Auto" },
            ["Sym.Apps"] = new[] { "Apps", "Apps", "Apps", "Apps", "App" },

            ["Col.Blue"] = new[] { "Blue", "Blau", "Bleu", "Azul", "Blu" },
            ["Col.Teal"] = new[] { "Teal", "Türkis", "Turquoise", "Turquesa", "Turchese" },
            ["Col.Green"] = new[] { "Green", "Grün", "Vert", "Verde", "Verde" },
            ["Col.Orange"] = new[] { "Orange", "Orange", "Orange", "Naranja", "Arancione" },
            ["Col.Red"] = new[] { "Red", "Rot", "Rouge", "Rojo", "Rosso" },
            ["Col.Pink"] = new[] { "Pink", "Pink", "Rose", "Rosa", "Rosa" },
            ["Col.Purple"] = new[] { "Purple", "Violett", "Violet", "Morado", "Viola" },
            ["Col.Gray"] = new[] { "Gray", "Grau", "Gris", "Gris", "Grigio" },
        };
    }
}
