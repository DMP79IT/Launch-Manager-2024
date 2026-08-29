using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Serialization;
using System.Runtime.InteropServices;

namespace LM
{
    internal static class Program
    {
        static void Log(string message)
        {
            try
            {
                string logDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Launch Manager 2024"
                );

                Directory.CreateDirectory(logDir);
                string logFile = Path.Combine(logDir, "LM_runner.log");

                File.AppendAllText(
                    logFile,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n"
                );
            }
            catch
            {
            }
        }

        [STAThread]

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_MINIMIZE = 6;
        private const int SW_RESTORE = 9;

        [DllImport("user32.dll")]
        private static extern bool PostMessage(
            IntPtr hWnd,
            uint Msg,
            IntPtr wParam,
            IntPtr lParam
        );

        private const uint WM_CLOSE = 0x0010;

        static void Main(string[] args)
        {
            try
            {
                Log("[STARTUP] LM started in single-runner mode");

                if (args.Length > 0 && Guid.TryParse(args[0], out _))
                {
                    // Parametro usato solo dall'avvio manuale da MainForm.
                    // Non viene salvato nel profilo XML.
                    bool manualLaunch = args.Any(arg =>
    arg.Equals(
        "--manual-launch",
        StringComparison.OrdinalIgnoreCase
    )
);

                    bool closeWithMsfsTemporary = manualLaunch;

                    RunSingleApp(
                        args[0],
                        closeWithMsfsTemporary,
                        manualLaunch
                    );
                }
                else
                {
                    Log("[WARN] No parameters passed — LM exits immediately.");
                }
            }
            catch (Exception ex)
            {
                Log($"[FATAL] {ex}");
            }
        }

        static void RunSingleApp(
    string appId,
    bool closeWithMsfsTemporary = false,
    bool manualLaunch = false
)
        {
            Thread.Sleep(new Random().Next(100, 500));

            try
            {
                string sim = "FS2024";
                string profilesDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Launch Manager 2024",
                    sim,
                    "Profiles"
                );

                string activeProfileName = ConfigService.GetActiveProfile();
                string profilePath = Path.Combine(profilesDir, activeProfileName);

                if (!File.Exists(profilePath))
                {
                    Log($"[WARN] Profile not found: {profilePath}");
                    return;
                }

                var serializer = new XmlSerializer(
                    typeof(List<AppEntry>),
                    new XmlRootAttribute("ArrayOfPrograms_CL")
                );

                List<AppEntry> apps;

                using (var fs = new FileStream(
                    profilePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite
                ))
                {
                    apps = (List<AppEntry>)serializer.Deserialize(fs);
                }

                var app = apps.FirstOrDefault(a => a.ID == appId);

                if (app == null)
                {
                    Log($"[WARN] No app found for ID {appId}");
                    return;
                }

                if (!app.Active && !manualLaunch)
                {
                    Log($"[INFO] {app.Name} is not active, no action taken.");
                    return;
                }

                if (!app.Active && manualLaunch)
                {
                    Log(
                        $"[INFO] {app.Name} is inactive in the profile, " +
                        "but will be launched manually."
                    );
                }

                // Questa variabile esiste soltanto in memoria:
                // - true se l'opzione è salvata nel profilo;
                // - oppure true se MainForm ha usato --close-with-msfs.
                // Non modifica app.CloseMSFS e non modifica il file XML.
                bool closeWithMsfs =
                    app.CloseMSFS || closeWithMsfsTemporary;

                Log(
                    $"[STARTUP] LM launching {app.Name} ({app.Path}) - " +
                    $"Profile CloseMSFS={app.CloseMSFS}, " +
                    $"Temporary CloseMSFS={closeWithMsfsTemporary}, " +
                    $"Effective CloseMSFS={closeWithMsfs}"
                );

                if (app.DelaySeconds > 0)
                {
                    Log($"[DELAY] Waiting {app.DelaySeconds}s before launching {app.Name}");
                    Thread.Sleep(app.DelaySeconds * 1000);
                }

                if (!File.Exists(app.Path))
                {
                    Log($"[ERROR] File not found: {app.Path}");
                    return;
                }

                string appDir = Path.GetDirectoryName(app.Path);
                Directory.SetCurrentDirectory(appDir);

                var psi = new ProcessStartInfo
                {
                    FileName = app.Path,
                    Arguments = app.Arguments,
                    UseShellExecute = true,
                    WorkingDirectory = appDir,
                    WindowStyle = app.StartMinimized
                        ? ProcessWindowStyle.Minimized
                        : ProcessWindowStyle.Normal
                };

                Process proc = Process.Start(psi);
                Log($"[LAUNCH] {app.Name} launched (PID={proc.Id})");

                if (app.StartMinimized)
                {
                    new Thread(() =>
                    {
                        try
                        {
                            int delay = Math.Max(app.StartMinimizedDelaySeconds, 0);

                            if (delay > 0)
                            {
                                Log($"[MINIMIZE] Waiting {delay}s before minimizing {app.Name}");
                                Thread.Sleep(delay * 1000);
                            }

                            proc.Refresh();

                            if (proc.MainWindowHandle != IntPtr.Zero)
                            {
                                ShowWindow(proc.MainWindowHandle, SW_MINIMIZE);

                                Log(
                                    $"[MINIMIZE] {app.Name} minimized " +
                                    $"{(delay > 0 ? $"after {delay}s" : "immediately")}"
                                );
                            }
                            else
                            {
                                Log($"[MINIMIZE] No main window found for {app.Name}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Log($"[MINIMIZE ERROR] {app.Name}: {ex.Message}");
                        }
                    }).Start();
                }

                if (app.CloseWindow)
                {
                    new Thread(() =>
                    {
                        try
                        {
                            int delay = Math.Max(app.CloseWindowDelaySeconds, 0);

                            if (delay > 0)
                            {
                                Log($"[CLOSEWIN] Waiting {delay}s before closing window of {app.Name}");
                                Thread.Sleep(delay * 1000);
                            }

                            proc.Refresh();

                            if (proc.MainWindowHandle != IntPtr.Zero)
                            {
                                PostMessage(
                                    proc.MainWindowHandle,
                                    WM_CLOSE,
                                    IntPtr.Zero,
                                    IntPtr.Zero
                                );

                                Log($"[CLOSEWIN] Simulated click on 'Close' for {app.Name}");
                            }
                            else
                            {
                                Log($"[CLOSEWIN] No main window found for {app.Name}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Log($"[CLOSEWIN ERROR] {app.Name}: {ex.Message}");
                        }
                    }).Start();
                }

                WaitForSimToClose();

                Log("[WAIT] MSFS has exited — final app management...");

                // QUI è l'unica modifica nella logica di chiusura:
                // prima era: if (app.CloseMSFS)
                if (closeWithMsfs)
                {
                    try
                    {
                        string exeName = Path.GetFileNameWithoutExtension(app.Path);

                        var targets = Process.GetProcesses()
                            .Where(p =>
                            {
                                try
                                {
                                    return p.ProcessName.Equals(
                                               exeName,
                                               StringComparison.OrdinalIgnoreCase
                                           )
                                           ||
                                           string.Equals(
                                               p.MainModule.FileName,
                                               app.Path,
                                               StringComparison.OrdinalIgnoreCase
                                           );
                                }
                                catch
                                {
                                    return false;
                                }
                            })
                            .ToList();

                        if (targets.Count == 0)
                        {
                            Log($"[INFO] No running process found for {app.Name}");
                        }
                        else
                        {
                            foreach (var p in targets)
                            {
                                try
                                {
                                    Log(
                                        $"[FORCE CLOSE] Forcing termination of " +
                                        $"{p.ProcessName} (PID={p.Id})..."
                                    );

                                    p.Kill();

                                    if (!p.WaitForExit(3000))
                                    {
                                        Log(
                                            $"[FORCE CLOSE WARN] {app.Name} " +
                                            "may have secondary processes still active."
                                        );
                                    }

                                    Log(
                                        $"[FORCE CLOSE] {app.Name} " +
                                        "successfully terminated."
                                    );
                                }
                                catch (Exception ex)
                                {
                                    Log(
                                        $"[FORCE CLOSE ERROR] {app.Name}: " +
                                        ex.Message
                                    );
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"[WAIT ERROR] {app.Name}: {ex.Message}");
                    }
                }
                else
                {
                    Log($"[INFO] {app.Name} left open");
                }

                Log($"[EXIT] LM finished for {app.Name}");
            }
            catch (Exception ex)
            {
                Log($"[RunSingleApp ERROR] {ex}");
            }
        }

        static void WaitForSimToClose()
        {
            Log("[WAIT] Waiting for MSFS to close...");

            string[] simNames =
            {
                "FlightSimulator2024",
                "FlightSimulator",
                "MSFS",
                "MicrosoftFlightSimulator"
            };

            bool simWasRunning = false;
            DateTime lastSeen = DateTime.Now;

            while (true)
            {
                var found = simNames.SelectMany(name =>
                {
                    try
                    {
                        return Process.GetProcessesByName(name);
                    }
                    catch
                    {
                        return Array.Empty<Process>();
                    }
                }).ToList();

                if (found.Any())
                {
                    if (!simWasRunning)
                    {
                        Log($"[WAIT] MSFS detected (PID={found.First().Id})");
                    }

                    simWasRunning = true;
                    lastSeen = DateTime.Now;
                }
                else if (
                    simWasRunning &&
                    (DateTime.Now - lastSeen).TotalSeconds > 10
                )
                {
                    Log("[WAIT] MSFS closed — proceeding with app shutdown...");
                    break;
                }

                Thread.Sleep(5000);
            }
        }
    }

    public class AppEntry
    {
        public string ID { get; set; }
        public bool Active { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string Arguments { get; set; }
        public string Mode { get; set; }
        public string Timing { get; set; }
        public int DelaySeconds { get; set; }
        public bool StartMinimized { get; set; }
        public int StartMinimizedDelaySeconds { get; set; }
        public bool CloseWindow { get; set; }
        public int CloseWindowDelaySeconds { get; set; }
        public bool CloseMSFS { get; set; }
    }

    public static class ConfigService
    {
        public static string GetActiveProfile()
        {
            try
            {
                string configPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Launch Manager 2024",
                    "config.xml"
                );

                if (!File.Exists(configPath))
                {
                    return "Default.xml";
                }

                var doc = System.Xml.Linq.XDocument.Load(configPath);
                var elem = doc.Root?.Element("ActiveProfile");

                return elem?.Value ?? "Default.xml";
            }
            catch
            {
                return "Default.xml";
            }
        }
    }
}