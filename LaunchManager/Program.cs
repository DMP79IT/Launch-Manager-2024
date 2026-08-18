using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace LaunchManager
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            bool createdNew;

            using (var mutex = new Mutex(
                true,
                "LaunchManager2024_SingleInstance",
                out createdNew))
            {
                if (!createdNew)
                {
                    ThemeManager.LoadTheme();

                    CustomDialogs.ShowInfo(
                        "Launch Manager 2024 is already running.",
                        "Launch Manager 2024"
                    );

                    return;
                }

                AvviaApplicazione();
            }
        }

        private static void AvviaApplicazione()
        {
            // === 1️⃣ Estrae LM.exe al primo avvio ===
            try
            {
                string baseDir = Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.ApplicationData),
                    "Launch Manager 2024"
                );

                string lmExePath = Path.Combine(baseDir, "LM.exe");

                if (!File.Exists(lmExePath))
                {
                    Directory.CreateDirectory(baseDir);

                    using (var stream =
                        typeof(Program).Assembly.GetManifestResourceStream(
                            "LaunchManager.LM.exe"))
                    {
                        if (stream == null)
                        {
                            throw new Exception(
                                "Risorsa incorporata LM.exe non trovata " +
                                "nell'assembly principale."
                            );
                        }

                        using (var file = new FileStream(
                            lmExePath,
                            FileMode.Create,
                            FileAccess.Write))
                        {
                            stream.CopyTo(file);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CustomDialogs.ShowError(
                    $"Error extracting LM.exe:\n{ex.Message}",
                    "Launch Manager 2024"
                );
            }

            // === 2️⃣ Avvia l'applicazione normale ===
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}