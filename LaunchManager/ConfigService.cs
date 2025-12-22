using System;
using System.IO;
using System.Xml.Linq;

namespace LaunchManager.Services
{
    public static class ConfigService
    {
        private static string ConfigPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Launch Manager 2024",
            "config.xml");

        public static string LastSimulator => "FS2024";
        public static int WindowX { get; set; } = 100;
        public static int WindowY { get; set; } = 100;
        public static int WindowWidth { get; set; } = 1100;
        public static int WindowHeight { get; set; } = 700;

        public static void Load()
        {
            try
            {
                if (!File.Exists(ConfigPath)) return;

                var doc = XDocument.Load(ConfigPath);
                var root = doc.Element("Config");
                if (root == null) return;

                WindowX = (int?)root.Element("WindowX") ?? WindowX;
                WindowY = (int?)root.Element("WindowY") ?? WindowY;
                WindowWidth = (int?)root.Element("WindowWidth") ?? WindowWidth;
                WindowHeight = (int?)root.Element("WindowHeight") ?? WindowHeight;
            }
            catch
            {
                // Nessun crash se il file è corrotto o mancante
            }
        }

        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));

                XDocument doc;

                // Se il file esiste, lo carichiamo per mantenere i tag esistenti
                if (File.Exists(ConfigPath))
                {
                    doc = XDocument.Load(ConfigPath);
                }
                else
                {
                    doc = new XDocument(new XElement("Config"));
                }

                XElement root = doc.Element("Config");
                if (root == null)
                {
                    root = new XElement("Config");
                    doc.Add(root);
                }

                // Aggiorna o crea i nodi delle impostazioni finestra
                UpdateOrAdd(root, "LastSimulator", LastSimulator);
                UpdateOrAdd(root, "WindowX", WindowX.ToString());
                UpdateOrAdd(root, "WindowY", WindowY.ToString());
                UpdateOrAdd(root, "WindowWidth", WindowWidth.ToString());
                UpdateOrAdd(root, "WindowHeight", WindowHeight.ToString());

                // ⚠️ NON toccare ActiveProfile se già esiste: così rimane salvato
                if (root.Element("ActiveProfile") == null && !string.IsNullOrEmpty(ActiveProfile))
                {
                    root.Add(new XElement("ActiveProfile", ActiveProfile));
                }

                doc.Save(ConfigPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ConfigService.Save] Errore: {ex.Message}");
            }
        }

        private static void UpdateOrAdd(XElement root, string name, string value)
        {
            var elem = root.Element(name);
            if (elem == null)
                root.Add(new XElement(name, value));
            else
                elem.Value = value;
        }


        // Helper locale
        private static void SetOrUpdate(XElement root, string name, string value)
        {
            var elem = root.Element(name);
            if (elem == null)
                root.Add(new XElement(name, value));
            else
                elem.Value = value;
        }


        public static void SetExeXmlPath(string newPath)
        {
            try
            {
                string configPath = ConfigPath;

                if (!Directory.Exists(Path.GetDirectoryName(configPath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(configPath));

                File.WriteAllText(configPath, newPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WARNING] Unable to update config.xml: {ex.Message}");
            }
        }

        // === NUOVO: memorizza il profilo attivo ===
        public static string ActiveProfile { get; private set; } = "Default.xml";

        public static void SetActiveProfile(string profileName)
        {
            try
            {
                // Percorso completo del file
                string configPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Launch Manager 2024",
                    "config.xml"
                );

                // Se il file non esiste, crealo da zero
                XDocument doc;
                if (File.Exists(configPath))
                    doc = XDocument.Load(configPath);
                else
                    doc = new XDocument(new XElement("Config"));

                // Recupera o crea il nodo <Config>
                XElement root = doc.Element("Config");
                if (root == null)
                {
                    root = new XElement("Config");
                    doc.Add(root);
                }

                // Recupera o crea il nodo <ActiveProfile>
                XElement elem = root.Element("ActiveProfile");
                if (elem == null)
                    root.Add(new XElement("ActiveProfile", profileName));
                else
                    elem.Value = profileName;

                // Crea la cartella se manca e salva
                Directory.CreateDirectory(Path.GetDirectoryName(configPath));
                doc.Save(configPath);

                System.Diagnostics.Debug.WriteLine($"[INFO] ActiveProfile saved: {profileName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WARNING] Unable to save active profile: {ex.Message}");
            }
        }
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
    }
}
