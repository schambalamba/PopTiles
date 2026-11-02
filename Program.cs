using System;
using System.IO;
using System.Threading;
using System.Windows;

namespace TaskbarGruppen
{
    static class Program
    {
        // Innerhalb dieser Zeit nach einem Schliessen durch Klick daneben gilt ein neuer Start als
        // zweiter Klick auf das Taskleisten-Icon: Die Kachel bleibt dann zu (Umschalt-Verhalten).
        const int ToggleWindowMs = 600;

        [STAThread]
        static int Main(string[] args)
        {
            try
            {
                var pinGroupId = ArgValue(args, "--pin");
                if (pinGroupId != null)
                    return RunPin(pinGroupId);

                var groupId = ArgValue(args, "--group");
                return groupId != null ? RunTile(groupId) : RunManager(ArgValue(args, "--edit"));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Loc.AppName, MessageBoxButton.OK, MessageBoxImage.Error);
                return 1;
            }
        }

        static int RunTile(string groupId)
        {
            if (Store.ClosedRecently(groupId, ToggleWindowMs))
                return 0;

            var name = "Local\\TaskbarGruppen.Tile." + groupId;
            using (var mutex = new Mutex(true, name + ".lock", out var isFirst))
            using (var closeSignal = new EventWaitHandle(false, EventResetMode.AutoReset, name + ".close"))
            {
                if (!isFirst)
                {
                    closeSignal.Set();
                    return 0;
                }

                Native.SetCurrentProcessExplicitAppUserModelID(Shortcuts.GroupAppId(groupId));

                var group = Store.Load().Find(groupId);
                if (group == null)
                {
                    MessageBox.Show(
                        Loc.T("GroupGone"),
                        Loc.AppName, MessageBoxButton.OK, MessageBoxImage.Information);
                    return 0;
                }

                var app = new Application();
                Theme.Apply(app);
                var window = new TileWindow(group);
                WatchSignal(closeSignal, () => window.CloseQuietly());
                return app.Run(window);
            }
        }

        static int RunPin(string groupId)
        {
            Native.SetCurrentProcessExplicitAppUserModelID(Shortcuts.GroupAppId(groupId));

            var group = Store.Load().Find(groupId);
            if (group == null)
                return (int)PinResult.Unavailable;

            var app = new Application();
            Theme.Apply(app);
            return app.Run(new PinWindow(group));
        }

        static int RunManager(string selectGroupId)
        {
            const string name = "Local\\TaskbarGruppen.Manager";
            using (var mutex = new Mutex(true, name + ".lock", out var isFirst))
            using (var showSignal = new EventWaitHandle(false, EventResetMode.AutoReset, name + ".show"))
            {
                if (!isFirst)
                {
                    showSignal.Set();
                    return 0;
                }

                Native.SetCurrentProcessExplicitAppUserModelID("TaskbarGruppen.Manager");

                var app = new Application();
                Theme.Apply(app);
                var window = new ManagerWindow(Store.Load(), selectGroupId);
                WatchSignal(showSignal, () =>
                {
                    if (window.WindowState == WindowState.Minimized)
                        window.WindowState = WindowState.Normal;
                    window.Activate();
                });
                return app.Run(window);
            }
        }

        static void WatchSignal(EventWaitHandle signal, Action action)
        {
            var dispatcher = Application.Current.Dispatcher;
            var thread = new Thread(() =>
            {
                while (true)
                {
                    signal.WaitOne();
                    dispatcher.BeginInvoke(action);
                }
            });
            thread.IsBackground = true;
            thread.Start();
        }

        static string ArgValue(string[] args, string key)
        {
            var index = Array.IndexOf(args, key);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }
    }
}
