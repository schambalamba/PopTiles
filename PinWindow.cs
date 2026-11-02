using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace TaskbarGruppen
{
    public enum PinResult
    {
        Pinned = 0,
        Declined = 1,
        NotAllowed = 2,
        Unavailable = 3,
    }

    // Offizielle Windows-Funktion zum Anheften mit Rueckfrage an den Nutzer. Sie gilt fuer die Kennung des
    // laufenden Prozesses, deshalb laeuft sie je Gruppe in einem eigenen Prozess (--pin) mit deren Kennung.
    // Die Typen werden erst zur Laufzeit geladen, damit das Programm auf aelteren Windows-Versionen startet.
    static class TaskbarPin
    {
        const string ManagerType = "Windows.UI.Shell.TaskbarManager, Windows.UI.Shell, ContentType=WindowsRuntime";
        const string ExtensionsType = "System.WindowsRuntimeSystemExtensions, System.Runtime.WindowsRuntime, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";

        // Windows braucht nach dem Anlegen einer Verknuepfung einige Sekunden, bis es sie als App kennt.
        const int MaxWaitMs = 30000;
        const int PollMs = 500;

        public static async Task<PinResult> RequestAsync(Action ensureForeground, Action<string> log)
        {
            var type = Type.GetType(ManagerType, false);
            var extensions = Type.GetType(ExtensionsType, false);
            if (type == null || extensions == null)
            {
                log("TaskbarManager not available");
                return PinResult.Unavailable;
            }

            try
            {
                var manager = type.GetMethod("GetDefault").Invoke(null, null);
                var allowedProperty = type.GetProperty("IsPinningAllowed");
                var waited = 0;
                while (!(bool)allowedProperty.GetValue(manager))
                {
                    if (waited >= MaxWaitMs)
                    {
                        log($"IsPinningAllowed=False after {waited} ms");
                        return PinResult.NotAllowed;
                    }
                    ensureForeground();
                    await Task.Delay(PollMs);
                    waited += PollMs;
                }
                log($"IsPinningAllowed=True after {waited} ms");

                var operation = type.GetMethod("RequestPinCurrentAppAsync").Invoke(manager, null);
                var asTask = extensions.GetMethods()
                    .First(m => m.Name == "AsTask" && m.IsGenericMethod && m.GetParameters().Length == 1
                        && m.GetParameters()[0].ParameterType.Name == "IAsyncOperation`1")
                    .MakeGenericMethod(typeof(bool));
                var pinned = await (Task<bool>)asTask.Invoke(null, new[] { operation });
                log("RequestPinCurrentApp=" + pinned);
                return pinned ? PinResult.Pinned : PinResult.Declined;
            }
            catch (Exception ex) when (ex is TargetInvocationException || ex is InvalidOperationException || ex is TypeLoadException || ex is System.Runtime.InteropServices.COMException)
            {
                log("Error: " + (ex.InnerException ?? ex).Message);
                return PinResult.Unavailable;
            }
        }
    }

    // Windows zeigt die Rueckfrage nur fuer ein Programm im Vordergrund, deshalb braucht der Prozess ein Fenster.
    sealed class PinWindow : Window
    {
        const int ForegroundDelayMs = 150;

        readonly string _logPath = Path.Combine(Store.Dir, "pin.log");

        public PinWindow(Group group)
        {
            Title = Loc.AppName;
            Width = 340;
            SizeToContent = SizeToContent.Height;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            SetResourceReference(BackgroundProperty, "Bg");

            var text = new TextBlock
            {
                Text = string.Format(Loc.T("Pinning"), group.Name),
                Margin = new Thickness(18),
                TextWrapping = TextWrapping.Wrap,
                FontSize = 13,
            };
            text.SetResourceReference(TextBlock.ForegroundProperty, "Fg");
            Content = text;

            SourceInitialized += (s, e) => Theme.ApplyTitleBar(this);
            ContentRendered += async (s, e) => await RunAsync(group);
        }

        async Task RunAsync(Group group)
        {
            Log($"--- {DateTime.Now:s} Group {group.Id}");
            var hwnd = new WindowInteropHelper(this).Handle;
            Activate();
            Native.SetForegroundWindow(hwnd);
            await Task.Delay(ForegroundDelayMs);
            Log("Foreground=" + (Native.GetForegroundWindow() == hwnd));

            var result = await TaskbarPin.RequestAsync(() =>
            {
                if (Native.GetForegroundWindow() != hwnd)
                {
                    Activate();
                    Native.SetForegroundWindow(hwnd);
                }
            }, Log);
            Log("Result=" + result);
            Application.Current.Shutdown((int)result);
        }

        void Log(string line)
        {
            try
            {
                Directory.CreateDirectory(Store.Dir);
                File.AppendAllText(_logPath, line + Environment.NewLine);
            }
            catch (IOException)
            {
                // Das Protokoll dient nur der Fehlersuche und darf das Anheften nicht aufhalten.
            }
        }
    }
}
