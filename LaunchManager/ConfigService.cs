using System;
using System.IO;
using System.Xml.Linq;

namespace LaunchManager.Services
{
    public static class ConfigService
    {
        // Percorso del file config.xml globale salvato in AppData\Roaming
        private static string ConfigPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Launch Manager 2024",
            "config.xml");

        // Simulatore attualmente supportato
        public static string LastSimulator => "FS2024";

        // Coordinate e dimensioni finestra principali
        public static int WindowX { get; set; } = 100;
        public static int WindowY { get; set; } = 100;
        public static int WindowWidth { get; set; } = 1100;
        public static int WindowHeight { get; set; } = 700;

        // Profilo attivo salvato nel config.xml
        public static string ActiveProfile { get; private set; } = "Default.xml";

        /// <summary>
        /// Carica dal config.xml le impostazioni principali già presenti.
        /// Se il file non esiste o è corrotto, non manda in crash il programma.
        /// </summary>
        public static void Load()
        {
            try
            {
                // Se il file config.xml non esiste, esce senza fare nulla
                if (!File.Exists(ConfigPath))
                    return;

                // Carica il documento XML
                var doc = XDocument.Load(ConfigPath);

                // Recupera il nodo radice <Config>
                var root = doc.Element("Config");
                if (root == null)
                    return;

                // Legge posizione e dimensioni finestra, mantenendo i default se mancano
                WindowX = (int?)root.Element("WindowX") ?? WindowX;
                WindowY = (int?)root.Element("WindowY") ?? WindowY;
                WindowWidth = (int?)root.Element("WindowWidth") ?? WindowWidth;
                WindowHeight = (int?)root.Element("WindowHeight") ?? WindowHeight;

                // Legge il profilo attivo se presente
                ActiveProfile = root.Element("ActiveProfile")?.Value ?? ActiveProfile;
            }
            catch
            {
                // Nessun crash se il file è corrotto o mancante
            }
        }

        /// <summary>
        /// Salva nel config.xml le impostazioni finestra e il profilo attivo.
        /// Non tocca le altre impostazioni già presenti nel file.
        /// </summary>
        public static void Save()
        {
            try
            {
                // Assicura l'esistenza della cartella AppData\Launch Manager 2024
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));

                XDocument doc;

                // Se il file esiste lo carica, così mantiene tutti i nodi esistenti
                if (File.Exists(ConfigPath))
                    doc = XDocument.Load(ConfigPath);
                else
                    doc = new XDocument(new XElement("Config"));

                // Recupera o crea il nodo radice <Config>
                XElement root = doc.Element("Config");
                if (root == null)
                {
                    root = new XElement("Config");
                    doc.Add(root);
                }

                // Aggiorna o crea i nodi relativi alla finestra
                UpdateOrAdd(root, "LastSimulator", LastSimulator);
                UpdateOrAdd(root, "WindowX", WindowX.ToString());
                UpdateOrAdd(root, "WindowY", WindowY.ToString());
                UpdateOrAdd(root, "WindowWidth", WindowWidth.ToString());
                UpdateOrAdd(root, "WindowHeight", WindowHeight.ToString());

                // Salva anche il profilo attivo, se valorizzato
                if (!string.IsNullOrWhiteSpace(ActiveProfile))
                    UpdateOrAdd(root, "ActiveProfile", ActiveProfile);

                // Scrive fisicamente il file config.xml
                doc.Save(ConfigPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ConfigService.Save] Errore: {ex.Message}");
            }
        }

        /// <summary>
        /// Aggiorna un nodo XML se esiste già, altrimenti lo crea.
        /// </summary>
        private static void UpdateOrAdd(XElement root, string name, string value)
        {
            var elem = root.Element(name);

            if (elem == null)
                root.Add(new XElement(name, value));
            else
                elem.Value = value;
        }

        /// <summary>
        /// Carica il config.xml se esiste, altrimenti crea un documento XML nuovo con nodo <Config>.
        /// </summary>
        private static XDocument LoadOrCreateConfig()
        {
            if (File.Exists(ConfigPath))
                return XDocument.Load(ConfigPath);

            return new XDocument(new XElement("Config"));
        }

        /// <summary>
        /// Restituisce il nodo radice <Config>.
        /// Se non esiste, lo crea.
        /// </summary>
        private static XElement GetOrCreateRoot(XDocument doc)
        {
            var root = doc.Element("Config");

            if (root == null)
            {
                root = new XElement("Config");
                doc.Add(root);
            }

            return root;
        }

        /// <summary>
        /// Salva nel config.xml il nome del profilo attivo.
        /// </summary>
        public static void SetActiveProfile(string profileName)
        {
            try
            {
                // Carica o crea il file XML
                var doc = LoadOrCreateConfig();

                // Recupera o crea la root <Config>
                var root = GetOrCreateRoot(doc);

                // Aggiorna il nodo ActiveProfile
                UpdateOrAdd(root, "ActiveProfile", profileName);

                // Aggiorna anche il valore in memoria
                ActiveProfile = profileName;

                // Assicura l'esistenza della cartella e salva
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
                doc.Save(ConfigPath);

                System.Diagnostics.Debug.WriteLine($"[INFO] ActiveProfile saved: {profileName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WARNING] Unable to save active profile: {ex.Message}");
            }
        }

        /// <summary>
        /// Legge dal config.xml il profilo attivo.
        /// Se manca, restituisce Default.xml
        /// </summary>
        public static string GetActiveProfile()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                    return "Default.xml";

                var doc = XDocument.Load(ConfigPath);
                return doc.Element("Config")?.Element("ActiveProfile")?.Value ?? "Default.xml";
            }
            catch
            {
                return "Default.xml";
            }
        }

        /// <summary>
        /// Salva nel config.xml il percorso personalizzato della cartella backup.
        /// Questo valore viene usato solo se l'utente sceglie una cartella diversa dal default.
        /// </summary>
        public static void SetBackupPath(string backupPath)
        {
            try
            {
                // Carica o crea il file XML
                var doc = LoadOrCreateConfig();

                // Recupera o crea la root <Config>
                var root = GetOrCreateRoot(doc);

                // Salva il nodo BackupPath
                UpdateOrAdd(root, "BackupPath", backupPath ?? string.Empty);

                // Assicura l'esistenza della cartella di configurazione e salva
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
                doc.Save(ConfigPath);

                System.Diagnostics.Debug.WriteLine($"[INFO] BackupPath saved: {backupPath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WARNING] Unable to save backup path: {ex.Message}");
            }
        }

        /// <summary>
        /// Legge dal config.xml il percorso backup personalizzato.
        /// Se non esiste, restituisce stringa vuota.
        /// </summary>
        public static string GetBackupPath()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                    return string.Empty;

                var doc = XDocument.Load(ConfigPath);
                return doc.Element("Config")?.Element("BackupPath")?.Value ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Restituisce il percorso backup effettivo da usare nel programma.
        /// - Se l'utente ha configurato BackupPath, usa quello
        /// - Altrimenti usa il path standard definito in Paths.GetBackupPath()
        /// </summary>
        public static string GetEffectiveBackupPath()
        {
            try
            {
                string configuredPath = GetBackupPath();

                // Se esiste un path personalizzato valido, lo usa
                if (!string.IsNullOrWhiteSpace(configuredPath))
                {
                    Directory.CreateDirectory(configuredPath);
                    return configuredPath;
                }
            }
            catch
            {
                // Se qualcosa va storto, si passa al fallback di default
            }

            // Fallback: cartella backup standard del programma
            return Paths.GetBackupPath();
        }
    }
}