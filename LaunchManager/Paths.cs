using System;
using System.IO;

namespace LaunchManager
{
    /// <summary>
    /// Gestisce i percorsi principali utilizzati dal Launch Manager 2024.
    /// Tutti i path sono statici e centralizzati qui.
    /// </summary>
    public static class Paths
    {
        /// <summary>
        /// Nome del simulatore attualmente supportato (solo MSFS 2024).
        /// </summary>
        public static string CurrentSim => "FS2024";

        /// <summary>
        /// Percorso predefinito dell’exe.xml per MSFS 2024.
        /// </summary>
        private static string _exeXmlPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            @"Packages\Microsoft.Limitless_8wekyb3d8bbwe\LocalCache\exe.xml"
        );

        /// <summary>
        /// Percorso corrente dell’exe.xml (può essere personalizzato).
        /// </summary>
        public static string ExeXmlPath
        {
            get => _exeXmlPath;
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                    _exeXmlPath = value;
            }
        }

        /// <summary>
        /// Imposta manualmente un nuovo percorso per il file exe.xml.
        /// </summary>
        public static void SetExeXmlPath(string newPath)
        {
            if (!string.IsNullOrWhiteSpace(newPath))
                _exeXmlPath = newPath;
        }

        /// <summary>
        /// Restituisce la cartella dei profili dell’utente per FS2024.
        /// </summary>
        public static string GetProfilesPath()
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Launch Manager 2024",
                CurrentSim,
                "Profiles"
            );
            Directory.CreateDirectory(path);
            return path;
        }

        /// <summary>
        /// Restituisce la cartella dei backup per FS2024.
        /// </summary>
        public static string GetBackupPath()
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Launch Manager 2024",
                CurrentSim,
                "Backup"
            );
            Directory.CreateDirectory(path);
            return path;
        }

        /// <summary>
        /// Restituisce il percorso completo del file di configurazione globale.
        /// </summary>
        public static string GetConfigPath()
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Launch Manager 2024"
            );
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "config.xml");
        }
    }
}
