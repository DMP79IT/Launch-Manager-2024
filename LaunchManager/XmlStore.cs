using LaunchManager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace LaunchManager.Services
{
    internal static class XmlStore
    {
        /// <summary>
        /// Carica la lista dei programmi da un file XML.
        /// </summary>
        public static List<AppEntry> LoadPrograms(string filePath = null)
        {
            try
            {
                string path = filePath ?? Path.Combine(Paths.GetProfilesPath(), "Programs.xml");
                if (!File.Exists(path))
                    return new List<AppEntry>();

                string content = File.ReadAllText(path).Trim();
                if (string.IsNullOrWhiteSpace(content))
                    return new List<AppEntry>();

                // Usa lo stesso nome della radice usata in salvataggio
                var serializer = new XmlSerializer(
                    typeof(List<AppEntry>),
                    new XmlRootAttribute("ArrayOfPrograms_CL")
                );

                using (var stream = new FileStream(path, FileMode.Open))
                    return (List<AppEntry>)serializer.Deserialize(stream);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("Errore nel caricamento XML: " + ex.Message);
                return new List<AppEntry>();
            }
        }

        /// <summary>
        /// Salva la lista dei programmi nel file XML specificato o predefinito.
        /// </summary>
        public static void SavePrograms(List<AppEntry> apps, string filePath = null)
        {
            try
            {
                string path = filePath ?? Path.Combine(Paths.GetProfilesPath(), "Programs.xml");
                string dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var serializer = new XmlSerializer(
                    typeof(List<AppEntry>),
                    new XmlRootAttribute("ArrayOfPrograms_CL")
                );

                using (var writer = new StreamWriter(path))
                    serializer.Serialize(writer, apps);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("Errore nel salvataggio XML: " + ex.Message);
            }
        }

        /// <summary>
        /// Esegue il backup del file Programs.xml del simulatore corrente.
        /// </summary>
        public static void BackupPrograms()
        {
            try
            {
                string source = Path.Combine(Paths.GetProfilesPath(), "Programs.xml");
                if (!File.Exists(source)) return;

                string dir = Path.Combine(Paths.GetProfilesPath(), "Backup");
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string backup = Path.Combine(dir, $"Programs_backup_{DateTime.Now:yyyyMMdd_HHmmss}.xml");
                File.Copy(source, backup, true);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("Errore nel backup: " + ex.Message);
            }
        }
    }
}
