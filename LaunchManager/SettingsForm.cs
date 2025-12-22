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
        private CustomComboBox cmbTheme;
        private Button btnBrowse;
        private Button btnOK;
        private Button btnCancel;
        private Button btnOpenConfig;
        private Button btnOpenAppData;

        public string ExeXmlPath { get; private set; }
        public string SelectedTheme { get; private set; }

        public SettingsForm(string currentSim, string currentExePath, string currentTheme)
        {
            Text = "Settings";
            Size = new Size(500, 250);
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
                Location = new Point(20, 140),
                AutoSize = true
            };
            Controls.Add(lblTheme);

            // --- COMBOBOX THEME ---
            cmbTheme = new CustomComboBox
            {
                Location = new Point(80, 138),
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


            // --- PULSANTE: OPEN CONFIG.XML ---
            btnOpenConfig = new Button
            {
                Text = "Open file Config.xml",
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(20, 100),
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
                        MessageBox.Show("The config.xml file was not found.",
                            "Launch Manager 2024", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error opening config.xml:\n" + ex.Message,
                        "Launch Manager 2024", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            Controls.Add(btnOpenConfig);

            // --- PULSANTE: OPEN APPDATA FOLDER ---
            btnOpenAppData = new Button
            {
                Text = "Open Launch Manager 2024 Folder",
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(180, 100),
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
                        MessageBox.Show("The folder does not exist:\n" + appDataPath,
                            "Launch Manager 2024", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error opening folder:\n" + ex.Message,
                        "Launch Manager 2024", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            Controls.Add(btnOpenAppData);

            // --- OK ---
            btnOK = new Button
            {
                Text = "Confirm",
                Location = new Point(220, 170),
                Width = 90
            };

            btnOK.Click += (s, e) =>
            {
                ExeXmlPath = txtExePath.Text.Trim();

                if (string.IsNullOrWhiteSpace(ExeXmlPath))
                {
                    MessageBox.Show("Please enter a valid path to exe.xml.",
                        "Launch Manager 2024", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
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

                    // Aggiorna solo il path exe.xml salvato in config.xml
                    var oldNode = rootNode.SelectSingleNode("ExeXmlPath");
                    if (oldNode != null) rootNode.RemoveChild(oldNode);

                    var exeNode = xml.CreateElement("ExeXmlPath");
                    exeNode.InnerText = ExeXmlPath;
                    rootNode.AppendChild(exeNode);

                    xml.Save(configPath);

                    // Aggiorna il percorso in memoria subito dopo il salvataggio
                    Paths.ExeXmlPath = ExeXmlPath;

                    MessageBox.Show(
                        $"Saved new exe.xml path to config:\n{configPath}",
                        "Launch Manager 2024",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );

                    ThemeManager.CurrentTheme =
                    cmbTheme.SelectedItem.ToString() == "Dark"
                    ? ThemeManager.ThemeMode.Dark
                    : ThemeManager.ThemeMode.Light;

                    ThemeManager.SaveTheme();
                    DialogResult = DialogResult.OK;
                    Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error saving configuration:\n" + ex.Message,
                        "Launch Manager 2024", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            Controls.Add(btnOK);

            // --- CANCEL ---
            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(320, 170),
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