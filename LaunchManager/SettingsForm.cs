using LaunchManager.Controls;
using LaunchManager.Services;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace LaunchManager
{
    public partial class SettingsForm : Form
    {
        private TextBox txtExePath;
        private TextBox txtBackupPath;
        private CustomComboBox cmbTheme;
        private Button btnBrowse;
        private Button btnBrowseBackup;
        private Button btnOK;
        private Button btnCancel;
        private Button btnOpenConfig;
        private Button btnOpenAppData;

        public string ExeXmlPath { get; private set; }
        public string SelectedTheme { get; private set; }

        public SettingsForm(string currentSim, string currentExePath, string currentTheme)
        {
            Text = "Settings";
            Size = new Size(500, 320);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;

            // --- LABEL PATH ---
            var lblPath = new Label
            {
                Text = $"Path 'EXE.XML' ({currentSim})",
                Location = new Point(20, 20),
                AutoSize = true
            };
            Controls.Add(lblPath);

            // --- LABEL TEMA ---
            var lblTheme = new Label
            {
                Text = "Theme:",
                Location = new Point(20, 200),
                AutoSize = true
            };
            Controls.Add(lblTheme);

            // --- COMBOBOX THEME ---
            cmbTheme = new CustomComboBox
            {
                Location = new Point(80, 198),
                Width = 120,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbTheme.Items.Add("Light");
            cmbTheme.Items.Add("Dark");

            // Imposta valore attuale
            cmbTheme.SelectedItem = currentTheme == "Dark" ? "Dark" : "Light";

            Controls.Add(cmbTheme);

            // --- TEXTBOX PATH ---
            txtExePath = new TextBox
            {
                Text = currentExePath ?? "",
                Location = new Point(20, 45),
                Width = 360
            };
            Controls.Add(txtExePath);

            // --- PULSANTE SFOGLIA ---
            btnBrowse = new Button
            {
                Text = "Browse...",
                Location = new Point(390, 44),
                Width = 70
            };
            btnBrowse.Click += (s, e) =>
            {
                using (var ofd = new OpenFileDialog())
                {
                    ofd.Filter = "XML file (*.xml)|*.xml|All files (*.*)|*.*";
                    ofd.Title = "Select the simulator EXE.XML file";
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        txtExePath.Text = ofd.FileName;
                    }
                }
            };
            Controls.Add(btnBrowse);

            // --- LABEL BACKUP PATH ---
            var lblBackupPath = new Label
            {
                Text = "Backup folder",
                Location = new Point(20, 85),
                AutoSize = true
            };
            Controls.Add(lblBackupPath);

            // --- TEXTBOX BACKUP PATH ---
            txtBackupPath = new TextBox
            {
                Text = ConfigService.GetEffectiveBackupPath(),
                Location = new Point(20, 110),
                Width = 360
            };
            Controls.Add(txtBackupPath);

            // --- PULSANTE SFOGLIA BACKUP ---
            btnBrowseBackup = new Button
            {
                Text = "Browse...",
                Location = new Point(390, 109),
                Width = 70
            };
            btnBrowseBackup.Click += (s, e) =>
            {
                using (var fbd = new FolderBrowserDialog())
                {
                    fbd.Description = "Select the folder where exe.xml backups will be saved";

                    if (Directory.Exists(txtBackupPath.Text))
                        fbd.SelectedPath = txtBackupPath.Text;

                    if (fbd.ShowDialog() == DialogResult.OK)
                    {
                        txtBackupPath.Text = fbd.SelectedPath;
                    }
                }
            };
            Controls.Add(btnBrowseBackup);

            // --- PULSANTE: OPEN CONFIG.XML ---
            btnOpenConfig = new Button
            {
                Text = "Open file Config.xml",
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(20, 150),
                Width = 120,
                FlatStyle = FlatStyle.System
            };
            btnOpenConfig.Click += (s, e) =>
            {
                try
                {
                    string configPath = Paths.GetConfigPath();
                    if (File.Exists(configPath))
                    {
                        System.Diagnostics.Process.Start("explorer.exe", configPath);
                    }
                    else
                    {
                        CustomDialogs.ShowError("The config.xml file was not found.", "Launch Manager 2024");
                    }
                }
                catch (Exception ex)
                {
                    CustomDialogs.ShowError("Error opening config.xml:\n" + ex.Message, "Launch Manager 2024");
                }
            };
            Controls.Add(btnOpenConfig);

            // --- PULSANTE: OPEN APPDATA FOLDER ---
            btnOpenAppData = new Button
            {
                Text = "Open Launch Manager 2024 Folder",
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(180, 150),
                Width = 200,
                FlatStyle = FlatStyle.System
            };
            btnOpenAppData.Click += (s, e) =>
            {
                try
                {
                    string appDataPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "Launch Manager 2024"
                    );

                    if (Directory.Exists(appDataPath))
                    {
                        System.Diagnostics.Process.Start("explorer.exe", appDataPath);
                    }
                    else
                    {
                        CustomDialogs.ShowError("The folder does not exist:\n" + appDataPath, "Launch Manager 2024");
                    }
                }
                catch (Exception ex)
                {
                    CustomDialogs.ShowError("Error opening folder:\n" + ex.Message, "Launch Manager 2024");
                }
            };
            Controls.Add(btnOpenAppData);

            // --- OK ---
            btnOK = new Button
            {
                Text = "Confirm",
                Location = new Point(270, 230),
                Width = 90
            };

            btnOK.Click += (s, e) =>
            {
                ExeXmlPath = txtExePath.Text.Trim();
                string backupPath = txtBackupPath.Text.Trim();

                if (string.IsNullOrWhiteSpace(ExeXmlPath))
                {
                    CustomDialogs.ShowError("Please enter a valid path to exe.xml.", "Launch Manager 2024");
                    return;
                }

                if (string.IsNullOrWhiteSpace(backupPath))
                {
                    CustomDialogs.ShowError("Please enter a valid backup folder.", "Launch Manager 2024");
                    return;
                }

                try
                {
                    Directory.CreateDirectory(backupPath);

                    string configPath = Paths.GetConfigPath();

                    // Assicura che il file esista
                    if (!File.Exists(configPath))
                    {
                        var newXml = new System.Xml.XmlDocument();
                        var root = newXml.CreateElement("Config");
                        newXml.AppendChild(root);
                        newXml.Save(configPath);
                    }

                    // Carica o crea il nodo principale
                    var xml = new System.Xml.XmlDocument();
                    xml.Load(configPath);
                    var rootNode = xml.SelectSingleNode("/Config") ?? xml.AppendChild(xml.CreateElement("Config"));

                    // ✅ LEGGI PATH ORIGINALE
                    string originalPath = null;
                    var originalNode = rootNode.SelectSingleNode("ExeXmlPath");
                    if (originalNode != null)
                        originalPath = originalNode.InnerText;

                    // ✅ LEGGI TEMA ORIGINALE
                    string originalTheme = null;
                    var originalThemeNode = rootNode.SelectSingleNode("Theme");
                    if (originalThemeNode != null)
                        originalTheme = originalThemeNode.InnerText;

                    // ✅ LEGGI BACKUP PATH ORIGINALE
                    string originalBackupPath = null;
                    var originalBackupNode = rootNode.SelectSingleNode("BackupPath");
                    if (originalBackupNode != null)
                        originalBackupPath = originalBackupNode.InnerText;

                    // SALVA PATH
                    if (originalNode != null) rootNode.RemoveChild(originalNode);
                    var exeNode = xml.CreateElement("ExeXmlPath");
                    exeNode.InnerText = ExeXmlPath;
                    rootNode.AppendChild(exeNode);

                    // SALVA TEMA
                    string selectedTheme = cmbTheme.SelectedItem.ToString();
                    if (originalThemeNode != null) rootNode.RemoveChild(originalThemeNode);
                    var themeNode = xml.CreateElement("Theme");
                    themeNode.InnerText = selectedTheme;
                    rootNode.AppendChild(themeNode);

                    // SALVA BACKUP PATH
                    if (originalBackupNode != null) rootNode.RemoveChild(originalBackupNode);
                    var backupNode = xml.CreateElement("BackupPath");
                    backupNode.InnerText = backupPath;
                    rootNode.AppendChild(backupNode);

                    xml.Save(configPath);

                    // Aggiorna in memoria
                    Paths.ExeXmlPath = ExeXmlPath;
                    ThemeManager.CurrentTheme = selectedTheme == "Dark" ? ThemeManager.ThemeMode.Dark : ThemeManager.ThemeMode.Light;
                    ThemeManager.SaveTheme();

                    // Aggiorna anche il backup path nel config service
                    ConfigService.SetBackupPath(backupPath);

                    // ✅ MESSAGGI SOLO SE CAMBIATI
                    if (!string.Equals(ExeXmlPath, originalPath, StringComparison.OrdinalIgnoreCase))
                    {
                        CustomDialogs.ShowInfo($"Saved new exe.xml path to config:\n{configPath}", "Launch Manager 2024");
                    }

                    if (!string.Equals(selectedTheme, originalTheme, StringComparison.OrdinalIgnoreCase))
                    {
                        CustomDialogs.ShowInfo($"Theme changed to {selectedTheme}.", "Launch Manager 2024");
                    }

                    if (!string.Equals(backupPath, originalBackupPath, StringComparison.OrdinalIgnoreCase))
                    {
                        CustomDialogs.ShowInfo($"Backup folder changed to:\n{backupPath}", "Launch Manager 2024");
                    }

                    DialogResult = DialogResult.OK;
                    Close();
                }
                catch (Exception ex)
                {
                    CustomDialogs.ShowError("Error saving configuration:\n" + ex.Message, "Launch Manager 2024");
                }
            };

            Controls.Add(btnOK);

            // --- CANCEL ---
            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(370, 230),
                Width = 90
            };
            btnCancel.Click += (s, e) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };
            Controls.Add(btnCancel);

            // --- LABEL VERSIONE ---
            var lblVersion = new Label
            {
                AutoSize = true,
                Text = $"Launch Manager 2024 — v{Application.ProductVersion}",
                Font = new Font("Segoe UI", 8F, FontStyle.Italic),
                ForeColor = Color.Gray,
                Location = new Point(20, btnOK.Top + (btnOK.Height / 2) - 6),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            Controls.Add(lblVersion);

            ThemeManager.ApplyTheme(this);
        }
    }
}