using LaunchManager.Controls;
using LaunchManager.Models;
using LaunchManager.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;





namespace LaunchManager
{

    public partial class MainForm : Form
    {
        class ExeAppEntry
        {
            public string Name { get; set; }
            public string Path { get; set; }
            public string CommandLine { get; set; }
            public bool Disabled { get; set; }
        }
        private DataGridView grid;
        private Dictionary<int, Image> appIcons = new Dictionary<int, Image>();
        private ToolStrip toolbar;
        private StatusStrip status;
        private ToolStripLabel lblTitle, lblTotal, lblActive, lblCrtr, lblAppInfo;
        private List<AppEntry> _apps = new List<AppEntry>();
        private bool _suppressProfileChange = false;
        private CustomComboBox cmbProfiles;
        private int rowIndexFromMouseDown;
        private int rowIndexOfItemUnderMouseToDrop;
        private Rectangle dragBoxFromMouseDown;



        //=== LETTURA DELL'ELENCO DEI PROFILI NELLA COMBOBOX ===
        private void LoadProfiles()
        {
            try
            {
                string sim = Paths.CurrentSim;
                string profilesDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Launch Manager 2024",
                    sim,
                    "Profiles"
                );

                cmbProfiles.Items.Clear();

                if (!Directory.Exists(profilesDir))
                    Directory.CreateDirectory(profilesDir);

                // Carica tutti i profili .xml
                var profileFiles = Directory.GetFiles(profilesDir, "*.xml");
                foreach (var file in profileFiles)
                {
                    string name = Path.GetFileNameWithoutExtension(file);
                    cmbProfiles.Items.Add(name);
                }

                try
                {
                    string activeProfile = ConfigService.GetActiveProfile();      // es. "FBW A32XN.xml"
                    string activeName = Path.GetFileNameWithoutExtension(activeProfile);

                    if (!string.IsNullOrWhiteSpace(activeName) &&
                        cmbProfiles.Items.Contains(activeName))
                    {
                        cmbProfiles.SelectedItem = activeName;
                    }
                    else if (cmbProfiles.Items.Count > 0)
                    {
                        // Fallback: se ActiveProfile non è valido, seleziona il primo profilo
                        cmbProfiles.SelectedIndex = 0;
                    }
                }
                catch
                {
                    // In caso di errore, seleziona comunque il primo se esiste
                    if (cmbProfiles.Items.Count > 0)
                        cmbProfiles.SelectedIndex = 0;
                }

                // === CONFRONTO DETTAGLIATO PROFILO vs EXE.XML ===
                if (cmbProfiles.SelectedItem != null)
                {
                    string profileName = cmbProfiles.SelectedItem.ToString();
                    string profilePath = Path.Combine(Paths.GetProfilesPath(), profileName + ".xml");

                    var profileApps = XmlStore.LoadPrograms(profilePath);
                    var exeApps = ParseExeXmlApps(Paths.ExeXmlPath);

                    string Normalize(string name) => (name ?? "").Trim();

                    // 1. Applicazioni presenti in EXE ma non nel PROFILO
                    var extraInExe = exeApps
                        .Where(e => !profileApps.Any(p =>
                            Normalize(p.Name).Equals(Normalize(e.Name), StringComparison.OrdinalIgnoreCase)))
                        .ToList();

                    foreach (var app in extraInExe)
                    {
                        var result = CustomDialogs.ShowQuestion(
                            $"Found in exe.xml only: {app.Name}. Do you want to add it to the profile?",
                            "Extra app in exe.xml");

                        if (result == DialogResult.Yes)
                        {
                            var newApp = new AppEntry
                            {
                                Name = app.Name,
                                Path = app.Path,
                                Mode = "MSFS",
                                Active = true
                            };
                            profileApps.Add(newApp);
                            XmlStore.SavePrograms(profileApps, profilePath);
                        }
                    }

                    // 1b. Apps present in PROFILE but NOT in EXE
                    var extraInProfile = profileApps
                        .Where(p => !exeApps.Any(e =>
                            Normalize(e.Name).Equals(Normalize(p.Name), StringComparison.OrdinalIgnoreCase)))
                        .ToList();

                    foreach (var app in extraInProfile)
                    {
                        var result = CustomDialogs.ShowQuestion(
                            $"Present in profile only: {app.Name}. Do you want to remove it from the profile?",
                            "App not found in exe.xml");

                        if (result == DialogResult.Yes)
                        {
                            profileApps.Remove(app);
                            XmlStore.SavePrograms(profileApps, profilePath);
                        }
                    }

                    // 2. APP CON DIFFERENZE (Active o Mode)
                    foreach (var pApp in profileApps)
                    {
                        var eApp = exeApps.FirstOrDefault(e => e.Name == pApp.Name);
                        if (eApp != null)
                        {
                            bool shouldBeDisabled = !pApp.Active;      // Active true → Disabled False
                            bool activeDiff = eApp.Disabled != shouldBeDisabled;

                            // calcola Mode lato exe.xml guardando il PATH, non la CommandLine
                            string lmExePath = Path.Combine(
                                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                "Launch Manager 2024",
                                "LM.exe"
                            );

                            string exeMode = string.Equals(eApp.Path, lmExePath, StringComparison.OrdinalIgnoreCase)
                                ? "LM"
                                : "MSFS";

                            bool modeDiff = pApp.Mode != exeMode;

                            if (activeDiff || modeDiff)
                            {
                                string msg = $"⚠ {pApp.Name} has differences:\n";
                                if (activeDiff)
                                    msg += $"• Profile: Active={pApp.Active} | exe.xml: Disabled={eApp.Disabled}\n";
                                if (modeDiff)
                                    msg += $"• Profile: Mode={pApp.Mode} | exe.xml: {exeMode}\n\n";

                                msg += "Do you want to align exe.xml to the profile?";

                                var result = CustomDialogs.ShowQuestion(
                                    msg,
                                    "Differences detected");

                                if (result == DialogResult.Yes)
                                {
                                    ApplyProfileToExeXml(profilePath);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CustomDialogs.ShowError(
                    $"Error loading profiles:\n{ex.Message}",
                    "Load Profiles Error");
            }
        }

        private void ApplyProfileToExeXml(string profilePath)
        {
            string sim = Paths.CurrentSim;
            string exeXmlPath = Paths.ExeXmlPath;

            var profileApps = XmlStore.LoadPrograms(profilePath);

            // 1) Backup con rotazione massima
            string backupFile = BackupExeXml();

            // 2) Rebuild exe.xml
            var xml = new XmlDocument();
            xml.AppendChild(xml.CreateXmlDeclaration("1.0", "utf-8", null));

            var root = xml.CreateElement("SimBase.Document");
            root.SetAttribute("Type", "SimConnect");
            root.SetAttribute("version", "1,0");
            xml.AppendChild(root);

            var descr = xml.CreateElement("Descr");
            descr.InnerText = "SimConnect";
            root.AppendChild(descr);

            var filename = xml.CreateElement("Filename");
            filename.InnerText = "SimConnect.xml";
            root.AppendChild(filename);

            var disabled = xml.CreateElement("Disabled");
            disabled.InnerText = "False";
            root.AppendChild(disabled);

            foreach (var app in profileApps)
            {
                var add = xml.CreateElement("Launch.Addon");

                void AddNode(string name, string value)
                {
                    var node = xml.CreateElement(name);
                    node.InnerText = value;
                    add.AppendChild(node);
                }

                AddNode("Name", app.Name);
                AddNode("Disabled", app.Active ? "False" : "True");
                AddNode("ManualLoad", "False");

                if (app.Mode == "LM")
                {
                    string lmPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "Launch Manager 2024",
                        "LM.exe"
                    );

                    AddNode("Path", lmPath);

                    if (!string.IsNullOrEmpty(app.ID))
                        AddNode("CommandLine", app.ID);
                }
                else
                {
                    AddNode("Path", app.Path);

                    if (!string.IsNullOrWhiteSpace(app.Arguments))
                        AddNode("CommandLine", app.Arguments);

                    AddNode("NewConsole", "False");
                }

                root.AppendChild(add);
            }

            xml.Save(exeXmlPath);

            CustomDialogs.ShowInfo(
                $"exe.xml has been aligned to profile:\n{Path.GetFileName(profilePath)}",
                "Launch Manager 2024");
        }


        public MainForm()
        {
            ThemeManager.LoadTheme();

            CheckForUpdates();

            InitializeWindowSettings();
            this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

            BuildUI();

            ThemeManager.ApplyTheme(this);

            LoadData();

            // Inizializzazione drag & drop
            grid.AllowDrop = true;
            grid.MouseDown += dataGridView1_MouseDown;
            grid.MouseDown += Grid_MouseDownForContextMenu;
            grid.MouseMove += dataGridView1_MouseMove;
            grid.DragOver += dataGridView1_DragOver;
            grid.DragDrop += dataGridView1_DragDrop;
        }


        private async void CheckForUpdates()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(8);
                    client.DefaultRequestHeaders.Add("User-Agent", "LaunchManager-2024");
                    client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

                    // LISTA TUTTI I TAG (funziona SEMPRE)
                    string tagsUrl = "https://api.github.com/repos/DMP79IT/Launch-Manager-2024/tags";
                    string json = await client.GetStringAsync(tagsUrl);

                    var tags = JArray.Parse(json);
                    if (tags.Count == 0) return;

                    // Prendi il PRIMO (più recente)
                    string tagName = tags[0]["name"]?.Value<string>();
                    if (string.IsNullOrEmpty(tagName) || !tagName.StartsWith("v"))
                        return;

                    string onlineVersionStr = tagName.Replace("v", "");

                    if (Version.TryParse(onlineVersionStr, out Version onlineVersion))
                    {
                        Version current = Assembly.GetExecutingAssembly().GetName().Version;
                        if (onlineVersion > current)
                        {
                            string onlineUrl = "https://it.flightsim.to/file/100036/launch-manager-2024";
                            var result = CustomDialogs.ShowUpdateDialog(current, onlineVersion);
                            if (result == DialogResult.Yes)
                            {
                                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                                {
                                    FileName = onlineUrl,
                                    UseShellExecute = true
                                });
                            }
                        }
                    }
                }
            }
            catch { }
        }


        // Comando per il clic veloce con il tasto destro
        private void Grid_MouseDownForContextMenu(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var hit = grid.HitTest(e.X, e.Y);
                if (hit.RowIndex >= 0)
                {
                    // Seleziona la riga sotto il mouse
                    grid.ClearSelection();
                    grid.Rows[hit.RowIndex].Selected = true;
                    grid.CurrentCell = grid.Rows[hit.RowIndex].Cells[hit.ColumnIndex >= 0 ? hit.ColumnIndex : 0];
                }
            }
        }

        // Comandi per il Darag&Drop in tabella
        private void dataGridView1_MouseDown(object sender, MouseEventArgs e)
        {
            var hitTest = grid.HitTest(e.X, e.Y);
            rowIndexFromMouseDown = hitTest.RowIndex;

            if (rowIndexFromMouseDown != -1)
            {
                Size dragSize = SystemInformation.DragSize;
                dragBoxFromMouseDown = new Rectangle(
                    new Point(e.X - (dragSize.Width / 2), e.Y - (dragSize.Height / 2)),
                    dragSize);
            }
            else
            {
                dragBoxFromMouseDown = Rectangle.Empty;
            }
        }

        private void dataGridView1_MouseMove(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                if (dragBoxFromMouseDown != Rectangle.Empty &&
                    !dragBoxFromMouseDown.Contains(e.X, e.Y))
                {
                    grid.DoDragDrop(grid.Rows[rowIndexFromMouseDown], DragDropEffects.Move);
                }
            }
        }

        private void dataGridView1_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        private void dataGridView1_DragDrop(object sender, DragEventArgs e)
        {
            Point clientPoint = grid.PointToClient(new Point(e.X, e.Y));
            rowIndexOfItemUnderMouseToDrop = grid.HitTest(clientPoint.X, clientPoint.Y).RowIndex;

            if (e.Effect == DragDropEffects.Move)
            {
                if (rowIndexOfItemUnderMouseToDrop < 0 || rowIndexOfItemUnderMouseToDrop == rowIndexFromMouseDown)
                    return;

                DataGridViewRow rowToMove = e.Data.GetData(typeof(DataGridViewRow)) as DataGridViewRow;

                grid.Rows.RemoveAt(rowIndexFromMouseDown);
                grid.Rows.Insert(rowIndexOfItemUnderMouseToDrop, rowToMove);
            }
        }


        // Caricamento icone Pulsanti Toolbar
        private Image LoadIcon(string fileName)
        {
            try
            {
                var asm = System.Reflection.Assembly.GetExecutingAssembly();
                string resourcePath = $"LaunchManager.Resources.{fileName}"; // usa il namespace del tuo progetto

                using (var stream = asm.GetManifestResourceStream(resourcePath))
                {
                    if (stream != null)
                    {
                        using (var original = Image.FromStream(stream))
                        {
                            return new Bitmap(original, new Size(32, 32));
                        }
                    }
                    else
                    {
                        CustomDialogs.ShowError($"Embedded icon not found:\n{resourcePath}", "Launch Manager 2024");
                    }
                }
            }
            catch (Exception ex)
            {
                CustomDialogs.ShowError($"Error loading icon {fileName}:\n{ex.Message}", "Launch Manager 2024");
            }

            return SystemIcons.Application.ToBitmap();
        }




        private void InitializeWindowSettings()
        {
            try
            {
                string configPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Launch Manager 2024",
                    "config.xml"
                );

                if (File.Exists(configPath))
                {
                    var xml = new System.Xml.XmlDocument();
                    xml.Load(configPath);
                    var root = xml.SelectSingleNode("/Config");

                    if (root != null)
                    {
                        var exeNode = root["ExeXmlPath"];
                        if (exeNode != null && !string.IsNullOrWhiteSpace(exeNode.InnerText))
                        {
                            Paths.SetExeXmlPath(exeNode.InnerText);
                        }
                        int.TryParse(root["WindowX"]?.InnerText, out int x);
                        int.TryParse(root["WindowY"]?.InnerText, out int y);
                        int.TryParse(root["WindowWidth"]?.InnerText, out int w);
                        int.TryParse(root["WindowHeight"]?.InnerText, out int h);
                        string state = root["WindowState"]?.InnerText;

                        if (w > 0 && h > 0)
                        {
                            StartPosition = FormStartPosition.Manual;
                            Bounds = new Rectangle(x, y, w, h);

                            // Imposta lo stato (Normal o Maximized)
                            if (state == "Maximized")
                                WindowState = FormWindowState.Maximized;
                            else
                                WindowState = FormWindowState.Normal;
                        }
                    }
                }
                // Imposta dimensione minima/iniziale della finestra
                this.MinimumSize = new Size(1150, 650);   // adatta questi valori a ciò che serve
                this.Size = new Size(1150, 650);         // dimensione iniziale suggerita
                this.StartPosition = FormStartPosition.CenterScreen;
            }
            catch
            {
                // in caso di errore, lascia la finestra in posizione di default
            }
        }



        // =========================================
        // COSTRUZIONE INTERFACCIA
        // =========================================
        private void BuildUI()
        {
            try
            {
                string configPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Launch Manager 2024",
                    "config.xml"
                );

                if (File.Exists(configPath))
                {
                    var xml = new System.Xml.XmlDocument();
                    xml.Load(configPath);
                    var root = xml.SelectSingleNode("/Config");

                    if (root != null)
                    {
                        int.TryParse(root["WindowX"]?.InnerText, out int x);
                        int.TryParse(root["WindowY"]?.InnerText, out int y);
                        int.TryParse(root["WindowWidth"]?.InnerText, out int w);
                        int.TryParse(root["WindowHeight"]?.InnerText, out int h);

                        if (w > 0 && h > 0)
                        {
                            StartPosition = FormStartPosition.Manual;
                            Bounds = new Rectangle(x, y, w, h);
                        }
                    }
                }
            }
            catch
            {
                // In caso di errore di lettura, finestra centrata
                StartPosition = FormStartPosition.CenterScreen;
                Width = 1100;
                Height = 700;
            }

            Text = "Launch Manager 2024";
            BackColor = Color.White;

            // === TOOLBAR ===
            toolbar = new ToolStrip
            {
                ImageScalingSize = new Size(32, 32),
                Height = 40,
                GripStyle = ToolStripGripStyle.Hidden,
                BackColor = Color.FromArgb(240, 240, 240),
                RenderMode = ToolStripRenderMode.System,
                Dock = DockStyle.Top
            };

            // === TOOLTIP ===
            ToolTip tooltip = new ToolTip
            {
                AutoPopDelay = 3000,
                InitialDelay = 300,
                ReshowDelay = 100,
                ShowAlways = true
            };


            // =======================
            // === SEZIONE PROFILI ===
            // =======================


            // === CREA NUOVO PROFILO ===

            var btnNewProfile = new ToolStripButton()
            {
                Image = LoadIcon("AddProfile.png"),
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                ToolTipText = "Create new profile"
            };
            toolbar.Items.Add(btnNewProfile);

            btnNewProfile.Click += (s, e) =>
            {
                using (var form = new ProfileForm())
                {
                    if (form.ShowDialog(this) == DialogResult.OK)
                    {
                        string nuovoProfilo = form.ProfileName;
                        if (!string.IsNullOrWhiteSpace(nuovoProfilo))
                        {
                            string sim = Paths.CurrentSim;

                            string baseDir = Path.Combine(
                                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                "Launch Manager 2024",
                                sim,
                                "Profiles"
                            );
                            Directory.CreateDirectory(baseDir);

                            string profilePath = Path.Combine(baseDir, $"{nuovoProfilo}.xml");
                            if (File.Exists(profilePath))
                            {
                                CustomDialogs.ShowError($"There is already a profile called \"{nuovoProfilo}\" for {sim}.", "Launch Manager 2024");
                                return;
                            }

                            // --- crea il file XML base ---
                            string xmlContent =
                                @"<?xml version=""1.0"" encoding=""utf-8""?>
                            <ArrayOfPrograms_CL xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
                            </ArrayOfPrograms_CL>";
                            File.WriteAllText(profilePath, xmlContent);

                            // --- aggiorna la combo SENZA triggerare il reload ---
                            _suppressProfileChange = true;
                            cmbProfiles.Items.Add(nuovoProfilo);
                            cmbProfiles.SelectedItem = nuovoProfilo;
                            _suppressProfileChange = false;

                            // Aggiorna la combo profili
                            LoadProfiles();

                            CustomDialogs.ShowInfo($"Profile \"{nuovoProfilo}\" created for {sim} in:\n{profilePath}", "Launch Manager 2024");
                        }
                    }
                }
            };


            // === RINOMINA PROFILO ===

            var btnRenameProfile = new ToolStripButton()
            {
                Image = LoadIcon("RenameProfile.png"),
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                ToolTipText = "Rename selected profile"
            };
            toolbar.Items.Add(btnRenameProfile);
            btnRenameProfile.Click += (s, e) =>
            {
                try
                {
                    if (cmbProfiles.SelectedItem == null)
                    {
                        CustomDialogs.ShowError("First select a profile to rename", "Launch Manager 2024");
                        return;
                    }

                    string oldName = cmbProfiles.SelectedItem.ToString();
                    string sim = Paths.CurrentSim;
                    string profilesDir = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "Launch Manager 2024",
                        sim,
                        "Profiles"
                    );

                    string oldPath = Path.Combine(profilesDir, $"{oldName}.xml");
                    if (!File.Exists(oldPath))
                    {
                        CustomDialogs.ShowError("The profile file no longer exists", "Launch Manager 2024");
                        return;
                    }

                    // Mostra finestra di input (modalità semplice)
                    string newName = CustomDialogs.RenameProfileDialog(oldName);

                    if (string.IsNullOrWhiteSpace(newName) || newName == oldName)
                        return;

                    // Se annulla o lascia vuoto, non fare nulla
                    if (string.IsNullOrWhiteSpace(newName) || newName == oldName)
                        return;

                    string newPath = Path.Combine(profilesDir, $"{newName}.xml");

                    if (File.Exists(newPath))
                    {
                        CustomDialogs.ShowError("There is already a profile with this name", "Launch Manager 2024");
                        return;
                    }

                    // Rinomina file
                    File.Move(oldPath, newPath);

                    // Aggiorna combo
                    int index = cmbProfiles.SelectedIndex;
                    cmbProfiles.Items[index] = newName;
                    cmbProfiles.SelectedIndex = index;

                    CustomDialogs.ShowInfo($"Profile renamed \"{newName}\"", "Launch Manager 2024");
                }
                catch (Exception ex)
                {
                    CustomDialogs.ShowError($"Error while renaming profile:\n{ex.Message}", "Launch Manager 2024");
                }
            };


            // === SALVA PROFILO ===

            var btnSaveProfile = new ToolStripButton()
            {
                Image = LoadIcon("SaveProfile.png"),
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                ToolTipText = "Save the apps in the list to the selected profile"
            };
            toolbar.Items.Add(btnSaveProfile);

            btnSaveProfile.Click += (s, e) =>
            {
                if (grid.Rows.Count == 0)
                {
                    CustomDialogs.ShowError("There are no applications to save.", "Launch Manager 2024");
                    return;
                }

                string profileName = cmbProfiles.SelectedItem?.ToString();

                // 🔹 1️⃣ Nessun profilo selezionato → chiedi nome
                if (string.IsNullOrWhiteSpace(profileName))
                {
                    using (var form = new ProfileForm())
                    {
                        ThemeManager.ApplyTheme(form);

                        if (form.ShowDialog(this) != DialogResult.OK)
                            return; // annullato

                        profileName = form.ProfileName?.Trim();
                        if (string.IsNullOrEmpty(profileName))
                        {
                            CustomDialogs.ShowError("Invalid profile name.", "Launch Manager 2024");
                            return;
                        }

                        string sim = Paths.CurrentSim;
                        string baseDir = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                            "Launch Manager 2024", sim, "Profiles"
                        );
                        Directory.CreateDirectory(baseDir);

                        string profilePath = Path.Combine(baseDir, $"{profileName}.xml");

                        if (File.Exists(profilePath))
                        {
                            CustomDialogs.ShowError($"There is already a profile called \"{profileName}\" for {sim}.", "Launch Manager 2024");
                            return;
                        }

                        // --- crea file xml base ---
                        string xmlContent =
                        @"<?xml version=""1.0"" encoding=""utf-8""?>
                        <ArrayOfPrograms_CL xmlns:xsd=""http://www.w3.org/2001/XMLSchema""
                        xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
                        </ArrayOfPrograms_CL>";
                        File.WriteAllText(profilePath, xmlContent);

                        // --- aggiorna combo ---
                        if (!cmbProfiles.Items.Contains(profileName))
                            _suppressProfileChange = true;

                        cmbProfiles.Items.Add(profileName);
                        cmbProfiles.SelectedItem = profileName;

                        _suppressProfileChange = false;

                        CustomDialogs.ShowInfo($"Profile \"{profileName}\" created for {sim}.", "Launch Manager 2024");
                    }
                }

                // 🔹 2️⃣ Salva app attuali in lista
                var appList = new List<AppEntry>();

                foreach (DataGridViewRow row in grid.Rows)
                {
                    if (row.IsNewRow) continue;

                    if (row.Tag is AppEntry app)
                    {
                        appList.Add(app);
                    }
                    else
                    {
                        bool active = (row.Cells[0].Value is bool b && b);
                        string name = row.Cells[1].Value?.ToString() ?? "";
                        string mode = row.Cells[2].Value?.ToString() ?? "";
                        string path = row.Cells[3].Value?.ToString() ?? "";

                        appList.Add(new AppEntry
                        {
                            ID = Guid.NewGuid().ToString(),
                            Active = active,
                            Name = name,
                            Mode = mode,
                            Path = path,
                            CloseMSFS = false
                        });
                    }
                }

                // 🔹 3️⃣ Salva XML
                string simFinal = Paths.CurrentSim;
                string profilesDirFinal = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Launch Manager 2024", simFinal, "Profiles"
                );
                Directory.CreateDirectory(profilesDirFinal);

                string profilePathFinal = Path.Combine(profilesDirFinal, profileName + ".xml");
                XmlStore.SavePrograms(appList, profilePathFinal);

                CustomDialogs.ShowInfo($"Profile \"{profileName}\" successfully saved.", "Launch Manager 2024");

                // 🔹 5️⃣ Ricarica (sincronizzazione)
                if (File.Exists(profilePathFinal))
                {
                    try
                    {
                        var loadedApps = XmlStore.LoadPrograms(profilePathFinal);
                        if (loadedApps != null)
                        {
                            grid.Rows.Clear();
                            foreach (var app in loadedApps)
                            {
                                int rowIndex = grid.Rows.Add(
                                    app.Active, app.Name, app.Mode, app.Path, app.Arguments, app.Timing,
                                    app.DelaySeconds, app.StartMinimized ? "Yes" : "No", app.StartMinimizedDelaySeconds,
                                    app.CloseWindow ? "Yes" : "No", app.CloseWindowDelaySeconds, app.CloseMSFS);

                                grid.Rows[rowIndex].Tag = app;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        CustomDialogs.ShowError($"Error while reloading profile:\n{ex.Message}", "Launch Manager 2024");
                    }
                }
            };



            // === ELIMINA PROFILO ===

            var btnDeleteProfile = new ToolStripButton()
            {
                Image = LoadIcon("DeleteProfile.png"),
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                ToolTipText = "Delete selected profile"
            };
            toolbar.Items.Add(btnDeleteProfile);
            btnDeleteProfile.Click += (s, e) =>
            {
                try
                {
                    if (cmbProfiles.SelectedItem == null)
                    {
                        CustomDialogs.ShowError("Select a profile to delete", "Launch Manager 2024");
                        return;
                    }

                    string profileName = cmbProfiles.SelectedItem.ToString();
                    string sim = Paths.CurrentSim;

                    string profilePath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "Launch Manager 2024",
                        sim,
                        "Profiles",
                        profileName + ".xml"
                    );

                    if (!File.Exists(profilePath))
                    {
                        CustomDialogs.ShowError("The profile file no longer exists or has already been removed", "Launch Manager 2024");
                        LoadProfiles();
                        return;
                    }

                    var confirm = CustomDialogs.RemoveProfileConfirm(
                        $"Are you sure you want to delete the selected profile?",
                        "Delete confirm"
                    );

                    if (confirm == DialogResult.Yes)
                    {
                        File.Delete(profilePath);

                        // 🔹 Svuota completamente la griglia PRIMA di ricaricare la combo
                        grid.Rows.Clear();
                        appIcons.Clear();
                        _apps.Clear();

                        // Aggiorna la combo profili
                        LoadProfiles();

                        CustomDialogs.ShowInfo($"Profile \"{profileName}\" successfully deleted.", "Launch Manager 2024");
                    }
                }
                catch (Exception ex)
                {
                    CustomDialogs.ShowError($"Error deleting profile:\n{ex.Message}", "Launch Manager 2024");

                }
            };


            // === COMBO PROFILI ===

            cmbProfiles = new CustomComboBox()
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 200
            };

            var comboHost = new ToolStripControlHost(cmbProfiles);
            toolbar.Items.Add(comboHost);
            LoadProfiles();

            cmbProfiles.SelectedIndexChanged += (s, e) => // Quando cambia profilo, ricarica la tabella
            {
                if (_suppressProfileChange) return;   // ⛔ blocca ricarichi involontari
                LoadData();                           // il tuo normale caricamento del profilo selezionato
            };


            // === APPLICA PROFILO ===

            var btnApplyProfile = new ToolStripButton()
            {
                Image = LoadIcon("ApplyProfile.png"),
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                ToolTipText = "Apply selected profile"
            };
            toolbar.Items.Add(btnApplyProfile);
            btnApplyProfile.Click += (s, e) =>
            {
                if (cmbProfiles.SelectedItem == null)
                {
                    CustomDialogs.ShowError("Select a profile to apply", "Launch Manager 2024");
                    return;
                }

                string selectedProfile = cmbProfiles.SelectedItem.ToString();

                ConfigService.SetActiveProfile(selectedProfile + ".xml");

                string sim = Paths.CurrentSim; // "FS2024"
                string profilesDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Launch Manager 2024",
                    sim,
                    "Profiles"
                );
                string profilePath = Path.Combine(profilesDir, selectedProfile + ".xml");

                if (!File.Exists(profilePath))
                {
                    CustomDialogs.ShowError("The selected profile does not exist", "Launch Manager 2024");
                    return;
                }

                try
                {
                    // 1️⃣ Legge il profilo
                    var profileApps = XmlStore.LoadPrograms(profilePath);

                    // 2️⃣ Percorso exe.xml del simulatore
                    string exeXmlPath = Paths.ExeXmlPath;


                    // 3️⃣ Backup automatico
                    string backupDir = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "Launch Manager 2024",
                        sim,
                        "Backup"
                    );

                    if (!Directory.Exists(backupDir))
                        Directory.CreateDirectory(backupDir);

                    string backupFile = Path.Combine(
                        backupDir,
                        $"exe_backup_{DateTime.Now:yyyyMMdd_HHmmss}.xml"
                    );

                    if (File.Exists(exeXmlPath))
                        File.Copy(exeXmlPath, backupFile, true);

                    // 4️⃣ Ricostruisci exe.xml
                    var xml = new System.Xml.XmlDocument();
                    xml.AppendChild(xml.CreateXmlDeclaration("1.0", "utf-8", null));

                    var root = xml.CreateElement("SimBase.Document");
                    root.SetAttribute("Type", "SimConnect");
                    root.SetAttribute("version", "1,0");
                    xml.AppendChild(root);

                    // === INTESTAZIONE STANDARD DI MSFS ===
                    var descr = xml.CreateElement("Descr");
                    descr.InnerText = "SimConnect";
                    root.AppendChild(descr);

                    var filename = xml.CreateElement("Filename");
                    filename.InnerText = "SimConnect.xml";
                    root.AppendChild(filename);

                    var disabled = xml.CreateElement("Disabled");
                    disabled.InnerText = "False";
                    root.AppendChild(disabled);


                    // RICOSTRUZIONE EXE.XML
                    // Le app in modalità LM vengono avviate tramite LM.exe (CommandLine = ID)
                    // Le app in modalità MSFS vengono avviate direttamente

                    foreach (var app in profileApps)
                    {
                        var add = xml.CreateElement("Launch.Addon");

                        void AddNode(string name, string value)
                        {
                            var node = xml.CreateElement(name);
                            node.InnerText = value;
                            add.AppendChild(node);
                        }

                        AddNode("Name", app.Name);
                        AddNode("Disabled", app.Active ? "False" : "True");
                        AddNode("ManualLoad", "False");

                        // --- LOGICA PRINCIPALE ---
                        if (app.Mode == "LM")
                        {
                            // Percorso del Launch Manager
                            string lmPath = Path.Combine(
                                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                "Launch Manager 2024",
                                "LM.exe"
                            );

                            AddNode("Path", lmPath);

                            // CommandLine = ID dell'app (già presente nel profilo)
                            if (!string.IsNullOrEmpty(app.ID))
                                AddNode("CommandLine", app.ID);
                        }
                        else // Mode == "MSFS"
                        {
                            // Avvio diretto dell'applicazione
                            AddNode("Path", app.Path);

                            // ✅ Se ci sono argomenti, aggiungili come CommandLine
                            if (!string.IsNullOrWhiteSpace(app.Arguments))
                                AddNode("CommandLine", app.Arguments);

                            // (Facoltativo) per compatibilità con lo schema MSFS
                            AddNode("NewConsole", "False");
                        }

                        root.AppendChild(add);
                    }

                    // 5️⃣ Salva il nuovo exe.xml
                    xml.Save(exeXmlPath);

                    CustomDialogs.ShowInfo($"Profile '{selectedProfile}' successfully applied.\nBackup saved in:\n{backupFile}", "Launch Manager 2024");
                }
                catch (Exception ex)
                {
                    CustomDialogs.ShowError($"Error applying profile:\n{ex.Message}", "Launch Manager 2024");
                }
            };


            // === APRI FILE PROFILO ===

            var btnOpenProfile = new ToolStripButton()
            {
                Image = LoadIcon("OpenXML.png"),
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                Text = "Open Profile File",
                ToolTipText = "Open the XML file of the selected profile"
            };
            toolbar.Items.Add(btnOpenProfile);

            btnOpenProfile.Click += (s, e) =>
            {
                try
                {
                    string profileName = cmbProfiles.SelectedItem?.ToString();
                    if (string.IsNullOrWhiteSpace(profileName))
                    {
                        CustomDialogs.ShowInfo("Select a profile before opening the XML file.", "Launch Manager 2024");
                        return;
                    }

                    string sim = Paths.CurrentSim;
                    string profilePath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "Launch Manager 2024", sim, "Profiles", $"{profileName}.xml"
                    );

                    if (!File.Exists(profilePath))
                    {
                        CustomDialogs.ShowError($"The profile file \"{profileName}\" does not exist:\n{profilePath}", "Launch Manager 2024");
                        return;
                    }

                    // 🔹 Apri con l’editor predefinito di Windows
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                    {
                        FileName = profilePath,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    CustomDialogs.ShowError($"Error opening profile file:\n{ex.Message}", "Launch Manager 2024");
                }
            };


            // === APRI CARTELLA PROFILI ===

            var btnOpenProfileFolder = new ToolStripButton()
            {
                Image = LoadIcon("OpenProfileFolder.png"),
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                ToolTipText = "Open profile folder",
            };
            toolbar.Items.Add(btnOpenProfileFolder);

            btnOpenProfileFolder.Click += (s, e) =>
            {
                string profileDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Launch Manager 2024",      // cartella principale in %appdataa%
                    "FS2024",                   // sottocartella
                    "Profiles"                  // sottocartella
                );

                try
                {
                    if (!Directory.Exists(profileDir))
                    {
                        CustomDialogs.ShowInfo($"Profile folder not found:\n{profileDir}", "Launch Manager 2024");
                        return;
                    }

                    // Apri la cartella in Esplora risorse
                    Process.Start("explorer.exe", profileDir);
                }
                catch (Exception ex)
                {
                    CustomDialogs.ShowError($"Error opening profile folder:\n{ex.Message}", "Launch Manager 2024");
                }
            };


            // === SINCRONIZZA CARTELLA PROFILI ===

            var btnSyncProfileFolder = new ToolStripButton()
            {
                Image = LoadIcon("SyncProfileFolder.png"), // <-- Usa la tua icona
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                ToolTipText = "Refresh Profile Folder"
            };
            toolbar.Items.Add(btnSyncProfileFolder);

            btnSyncProfileFolder.Click += (s, e) =>
            {
                try
                {
                    // Salva il profilo selezionato
                    string current = cmbProfiles.SelectedItem?.ToString();

                    // Aggiorna la lista profili
                    LoadProfiles();

                    // Ripristina la selezione se ancora esiste
                    if (current != null && cmbProfiles.Items.Contains(current))
                        cmbProfiles.SelectedItem = current;
                }
                catch
                {
                    // Silenzio assoluto anche sugli errori
                }
            };


            // === SEPARATORE ELASTICO SINISTRO ===
            var spacerLeft = new ToolStripSpringLabel();
            toolbar.Items.Add(spacerLeft);

            // === TITOLO CENTRALE ===
            lblTitle = new ToolStripLabel("Launch Manager 2024")
            {
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.CadetBlue,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(0, 2, 0, 0)
            };
            toolbar.Items.Add(lblTitle);

            // === SEPARATORE ELASTICO DESTRO ===
            var spacerRight = new ToolStripSpringLabel();
            toolbar.Items.Add(spacerRight);




            // ============================
            // === SEZIONE GESTIONE APP ===
            // ============================


            // === SETTINGS ===

            var btnSettings = new ToolStripButton()
            {
                Image = LoadIcon("Settings.png"),
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                ToolTipText = "Settings",
                Alignment = ToolStripItemAlignment.Right
            };
            toolbar.Items.Add(btnSettings);

            btnSettings.Click += (s, e) =>
            {
                try
                {
                    // 1️⃣ Carica il tema dal config.xml
                    ThemeManager.LoadTheme();

                    string currentSim = "FS2024";
                    string exeXmlPath = Paths.ExeXmlPath;

                    // 2️⃣ Usa il tema che hai appena caricato
                    string theme = ThemeManager.CurrentTheme == ThemeManager.ThemeMode.Dark
                                   ? "Dark"
                                   : "Light";

                    using (var form = new SettingsForm(currentSim, exeXmlPath, theme))
                    {
                        if (form.ShowDialog(this) == DialogResult.OK)
                        {
                            string nuovoPercorso = form.ExeXmlPath;

                            // 3️⃣ Ricarica il tema (il nuovo valore salvato)
                            ThemeManager.LoadTheme();

                            // 4️⃣ Applica alla MainForm
                            ThemeManager.ApplyTheme(this);
                        }
                    }
                }
                catch (Exception ex)
                {
                    CustomDialogs.ShowError($"Error opening settings window:\n{ex.Message}", "Launch Manager 2024");
                }
            };


            // === SALVA IMPOSTAZIONI ===

            var btnSave = new ToolStripButton()
            {
                Image = LoadIcon("Save.png"),
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                ToolTipText = "Save configuration",
                Alignment = ToolStripItemAlignment.Right
            };
            toolbar.Items.Add(btnSave);

            btnSave.Click += (s, e) =>
            {
                SaveCompleteConfiguration();
                CustomDialogs.ShowInfo("Configuration saved manually.", "Launch Manager 2024");
            };


            // === SEPARATORE ===

            var sep = new ToolStripSeparator()
            {
                Alignment = ToolStripItemAlignment.Right // opzionale
            };
            toolbar.Items.Add(sep);


            // === PULSANTE CLEAN BACKUP FOLDER ===
            var btnCleanBackup = new ToolStripButton()
            {
                Image = LoadIcon("CleanBackupFolder.png"), // oppure null se non hai l'icona
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                ToolTipText = "Clean backup folder",
                Alignment = ToolStripItemAlignment.Right
            };
            toolbar.Items.Add(btnCleanBackup);

            btnCleanBackup.Click += (s, e) =>
            {
                string sim = "FS2024"; // puoi renderlo dinamico se supporti più simulatori
                string backupDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Launch Manager 2024",
                    sim,
                    "Backup"
                );

                if (!Directory.Exists(backupDir))
                {
                    CustomDialogs.ShowInfo($"Backup folder not found:\n{backupDir}", "Launch Manager 2024");
                    return;
                }

                var confirm = CustomDialogs.ConfirmCleanup(
                "Do you really want to delete all backup files?",
                "Confirm Cleanup"
                );

                if (confirm == DialogResult.Yes)
                {
                    try
                    {
                        foreach (var file in Directory.GetFiles(backupDir))
                            File.Delete(file);

                        CustomDialogs.ShowInfo("Backup folder successfully cleaned.", "Launch Manager 2024");
                    }
                    catch (Exception ex)
                    {
                        CustomDialogs.ShowError($"Error while cleaning folder:\n{ex.Message}", "Launch Manager 2024");
                    }
                }
            };


            // === APRI CARTELLA BACKUP ===

            var btnOpenBackupFolder = new ToolStripButton()
            {
                Image = LoadIcon("OpenBackupFolder.png"), // opzionale: icona per il pulsante
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                ToolTipText = "Open backup folder",
                Alignment = ToolStripItemAlignment.Right
            };
            toolbar.Items.Add(btnOpenBackupFolder);

            btnOpenBackupFolder.Click += (s, e) =>
            {
                string sim = "FS2024"; // se serve puoi renderlo dinamico
                string backupDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Launch Manager 2024",
                    sim,
                    "Backup"
                );

                try
                {
                    if (!Directory.Exists(backupDir))
                    {
                        CustomDialogs.ShowInfo($"Backup folder not found:\n{backupDir}", "Launch Manager 2024");
                        return;
                    }

                    Process.Start("explorer.exe", backupDir);
                }
                catch (Exception ex)
                {
                    CustomDialogs.ShowError($"Error opening backup folder:\n{ex.Message}", "Launch Manager 2024");
                }
            };


            // === RIPRISTINA BACKUP ===

            var btnRestoreBackup = new ToolStripButton()
            {
                Image = LoadIcon("RestoreBackup.png"),
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                ToolTipText = "Restore Backup exe.xml",
                Alignment = ToolStripItemAlignment.Right
            };
            toolbar.Items.Add(btnRestoreBackup);

            btnRestoreBackup.Click += (s, e) =>
            {
                using (var form = new RestoreBackupForm())
                {
                    if (form.ShowDialog(this) == DialogResult.OK)
                    {
                        try
                        {
                            string backupFile = form.SelectedBackupPath;
                            string targetFile = Paths.ExeXmlPath;

                            if (!File.Exists(backupFile))
                            {
                                CustomDialogs.ShowError("The selected backup file does not exist.", "Launch Manager 2024");
                                return;
                            }

                            // Sostituisce direttamente il file exe.xml del simulatore
                            File.Copy(backupFile, targetFile, true);

                            CustomDialogs.ShowInfo($"The simulator exe.xml file has been successfully restored from:\n\n{backupFile}", "Launch Manager 2024");
                        }
                        catch (Exception ex)
                        {
                            CustomDialogs.ShowError($"Error during restore:\n{ex.Message}", "Launch Manager 2024");
                        }
                    }
                    else
                    {
                        // Operazione annullata: nessuna azione
                    }
                }
            };


            // === CREA BACKUP ===

            var btnManBackup = new ToolStripButton()
            {
                Image = LoadIcon("CreateBackup.png"),
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                ToolTipText = "Create Backup exe.xml",
                Alignment = ToolStripItemAlignment.Right
            };
            toolbar.Items.Add(btnManBackup);

            btnManBackup.Click += (s, e) =>
            {
                try
                {
                    string sim = Paths.CurrentSim; // "FS2024"

                    // Percorso exe.xml del simulatore
                    string exeXmlPath = Paths.ExeXmlPath;

                    if (!File.Exists(exeXmlPath))
                    {
                        CustomDialogs.ShowError($"The {sim} exe.xml file was not found.", "Launch Manager 2024");
                        return;
                    }

                    // Backup manuale con rotazione
                    string backupFile = BackupExeXml();

                    CustomDialogs.ShowInfo($"Backup created successfully!\n\nPath:\n{backupFile}", "Launch Manager 2024");
                }
                catch (Exception ex)
                {
                    CustomDialogs.ShowError($"Error creating backup:\n{ex.Message}", "Launch Manager 2024");
                }
            };


            // === SEPARATORE ===

            var sep0 = new ToolStripSeparator()
            {
                Alignment = ToolStripItemAlignment.Right
            };
            toolbar.Items.Add(sep0);


            // === APRI CARTELLA APP SELEZIONATA ===

            var btnOpenAppFolder = new ToolStripButton()
            {
                Image = LoadIcon("OpenAppFolder.png"),
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                ToolTipText = "Open selected app folder",
                Alignment = ToolStripItemAlignment.Right
            };
            toolbar.Items.Add(btnOpenAppFolder);

            btnOpenAppFolder.Click += (s, e) =>
            {
                if (grid.SelectedRows.Count == 0)
                {
                    CustomDialogs.ShowInfo("Select an application whose folder to open.", "Launch Manager 2024");
                    return;
                }

                try
                {
                    var row = grid.SelectedRows[0];
                    string appPath = row.Cells[3].Value?.ToString(); // ✅ Colonna Percorso

                    if (string.IsNullOrWhiteSpace(appPath))
                    {
                        CustomDialogs.ShowError("Invalid or empty path.", "Launch Manager 2024");
                        return;
                    }

                    // 🔹 Se è un collegamento (.lnk), risolvi la destinazione reale
                    if (Path.GetExtension(appPath).Equals(".lnk", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            dynamic shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));
                            var shortcut = shell.CreateShortcut(appPath);
                            string realPath = shortcut.TargetPath;
                            System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell);

                            if (!string.IsNullOrEmpty(realPath) && File.Exists(realPath))
                            {
                                appPath = realPath;
                            }
                            else
                            {
                                // ✅ Il collegamento esiste ma il target non più — apri la cartella del .lnk
                                string lnkFolder = Path.GetDirectoryName(appPath);
                                if (!string.IsNullOrEmpty(lnkFolder) && Directory.Exists(lnkFolder))
                                {
                                    System.Diagnostics.Process.Start("explorer.exe", lnkFolder);
                                    return;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            CustomDialogs.ShowError($"Unable to resolve connection:\n{ex.Message}", "Launch Manager 2024");
                        }
                    }

                    // 🔹 Se il file reale esiste, apri la sua cartella
                    string folderPath = Path.GetDirectoryName(appPath);
                    if (!string.IsNullOrEmpty(folderPath) && Directory.Exists(folderPath))
                    {
                        System.Diagnostics.Process.Start("explorer.exe", folderPath);
                    }
                    else
                    {
                        CustomDialogs.ShowError("Folder not found or inaccessible.", "Launch Manager 2024");
                    }
                }
                catch (Exception ex)
                {
                    CustomDialogs.ShowError($"Error opening folder:\n{ex.Message}", "Launch Manager 2024");
                }
            };


            // === AVVIA APP SELEZIONATA ===
            var btnRunApp = new ToolStripButton()
            {
                Image = LoadIcon("LaunchApp.png"),
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                ToolTipText = "Launch selected app",
                Alignment = ToolStripItemAlignment.Right
            };
            toolbar.Items.Add(btnRunApp);

            btnRunApp.Click += (s, e) =>
            {
                if (grid.SelectedRows.Count == 0)
                {
                    CustomDialogs.ShowInfo("Select an app to launch.", "Launch Manager 2024");
                    return;
                }

                var row = grid.SelectedRows[0];
                string appPath = row.Cells[3].Value?.ToString();   // path dell'app
                string arguments = row.Cells.Count > 4 ? row.Cells[4].Value?.ToString() : ""; // eventuali argomenti

                if (string.IsNullOrWhiteSpace(appPath))
                {
                    CustomDialogs.ShowError("No valid path specified.", "Launch Manager 2024");
                    return;
                }

                // Risolve i collegamenti .lnk
                string resolvedPath = ResolveShortcut(appPath);

                if (!File.Exists(resolvedPath))
                {
                    CustomDialogs.ShowError($"File not found:\n{resolvedPath}", "Launch Manager 2024");
                    return;
                }

                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = resolvedPath,
                        Arguments = arguments ?? "",
                        UseShellExecute = true,
                        WorkingDirectory = Path.GetDirectoryName(resolvedPath)
                    };

                    Process.Start(startInfo);
                }
                catch (Exception ex)
                {
                    CustomDialogs.ShowError($"Error starting app:\n{ex.Message}", "Launch Manager 2024");
                }
            };

            // Funzione di supporto
            string ResolveShortcut(string shortcutPath)
            {
                try
                {
                    if (!shortcutPath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
                        return shortcutPath;

                    // Usa WScript.Shell senza bisogno di riferimenti COM
                    Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                    dynamic shell = Activator.CreateInstance(shellType);
                    dynamic shortcut = shell.CreateShortcut(shortcutPath);
                    string targetPath = shortcut.TargetPath;

                    // Pulizia oggetti COM
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shortcut);
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell);

                    return targetPath;
                }
                catch
                {
                    return shortcutPath;
                }
            }


            // RIMUOVI APP

            var btnRemoveApp = new ToolStripButton()
            {
                Image = LoadIcon("RemoveApp.png"),
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                ToolTipText = "Remove selected app",
                Alignment = ToolStripItemAlignment.Right
            };
            toolbar.Items.Add(btnRemoveApp);

            btnRemoveApp.Click += (s, e) =>
            {
                if (grid.SelectedRows.Count == 0)
                {
                    CustomDialogs.ShowInfo("Remove selected app.", "Launch Manager 2024");
                    return;
                }

                int count = grid.SelectedRows.Count;
                string message = (count == 1)
                    ? "Do you really want to remove this app?"
                    : $"Do you really want to remove these {count} apps?";

                var confirm = CustomDialogs.RemoveAppConfirm(message, "Delete confirm");

                if (confirm != DialogResult.Yes)
                    return;

                foreach (DataGridViewRow row in grid.SelectedRows)
                {
                    if (!row.IsNewRow)
                        grid.Rows.Remove(row);
                }

                AggiornaContatori();
                AutoSaveCurrentProfile();
            };


            // --- MODIFICA APP ---

            var btnEditApp = new ToolStripButton()
            {
                Image = LoadIcon("EditApp.png"),
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                ToolTipText = "Edit selected app",
                Alignment = ToolStripItemAlignment.Right
            };
            toolbar.Items.Add(btnEditApp);

            btnEditApp.Click += (s, e) =>
            {
                if (grid.SelectedRows.Count == 0)
                {
                    CustomDialogs.ShowInfo("Select an app to edit.", "Launch Manager 2024");
                    return;
                }

                var row = grid.SelectedRows[0];
                var app = row.Tag as AppEntry;
                if (app == null)
                {
                    CustomDialogs.ShowError("Unable to read data from the selected app.", "Launch Manager 2024");
                    return;
                }

                // --- Prepara finestra di modifica ---
                using (var dlg = new EditAppForm())
                {
                    dlg._loadingData = true; // sospende l'aggiornamento automatico

                    dlg.txtName.Text = app.Name;
                    dlg.txtPath.Text = app.Path;
                    dlg.txtArgs.Text = app.Arguments;
                    dlg.numDelaySeconds.Value = app.DelaySeconds;
                    dlg.chkStartMinimized.Checked = app.StartMinimized;
                    dlg.numStartMinimizedDelaySeconds.Value = app.StartMinimizedDelaySeconds;
                    dlg.chkCloseWindow.Checked = app.CloseWindow;
                    dlg.numCloseWindowDelaySeconds.Value = app.CloseWindowDelaySeconds;
                    dlg.chkCloseMSFS.Checked = app.CloseMSFS;

                    if (app.Mode == "LM" || app.Mode == "Launch Manager")
                        dlg.rdoLM.Checked = true;
                    else
                        dlg.rdoMSFS.Checked = true;

                    dlg.rdoAft.Checked = true;

                    dlg._loadingData = false; // riattiva la gestione UI
                    dlg.AggiornaUI(); // aggiorna coerentemente lo stato

                    // Mostra la finestra
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        // --- Aggiorna i valori dell'oggetto AppEntry ---
                        app.Name = dlg.AppName;
                        app.Path = ResolveShortcutTarget(dlg.AppPath);
                        app.Arguments = dlg.Arguments;
                        app.Mode = dlg.Mode;
                        app.Timing = dlg.Timing;
                        app.DelaySeconds = dlg.DelaySeconds;
                        app.StartMinimized = dlg.StartMinimized;
                        app.StartMinimizedDelaySeconds = dlg.StartMinimizedDelaySeconds;
                        app.CloseWindow = dlg.CloseWindow;
                        app.CloseWindowDelaySeconds = dlg.CloseWindowDelaySeconds;
                        app.CloseMSFS = dlg.CloseMSFS;

                        // --- Aggiorna la riga visibile ---
                        row.Cells[1].Value = app.Name;
                        row.Cells[2].Value = dlg.Mode;
                        row.Cells[3].Value = ResolveShortcutTarget(app.Path);

                        // --- Aggiorna il Tag ---
                        row.Tag = app;

                        // --- Aggiorna icona ---
                        try
                        {
                            Icon extractedIcon = Icon.ExtractAssociatedIcon(app.Path);
                            appIcons[row.Index] = extractedIcon?.ToBitmap() ?? SystemIcons.Application.ToBitmap();
                        }
                        catch
                        {
                            appIcons[row.Index] = SystemIcons.Application.ToBitmap();
                        }

                        AggiornaContatori();

                        // 🔹 Ricostruisci la mappa delle icone per riallineare gli indici
                        var newIcons = new Dictionary<int, Image>();
                        for (int i = 0; i < grid.Rows.Count; i++)
                        {
                            if (appIcons.TryGetValue(i, out var icon))
                                newIcons[i] = icon;
                            else
                                newIcons[i] = SystemIcons.Application.ToBitmap(); // fallback se manca
                        }

                        appIcons = newIcons;
                        grid.Invalidate(); // forza il ridisegno

                        lblAppInfo.Text =
                            $"{app.Name}  |  " +
                            $"[{(app.Mode == "LM" ? "LM" : "MSFS")}]  |  " +
                            $"Start after MSFS  ({app.DelaySeconds}s)  |  " +
                            $"Minimized: {(app.StartMinimized ? "Yes" : "No")}  ({app.StartMinimizedDelaySeconds}s)  |  " +
                            $"CloseWin: {(app.CloseWindow ? "Yes" : "No")}  ({app.CloseWindowDelaySeconds}s)  |  " +
                            $"Close with MSFS: {(app.CloseMSFS ? "Yes" : "No")}";

                        // 🔹 Auto‑save current profile
                        AutoSaveCurrentProfile();
                    }
                    ;
                }
            };



            // AGGIUNGI APP

            var btnAddApp = new ToolStripButton()
            {
                Image = LoadIcon("AddApp.png"),
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                ToolTipText = "Add new app",
                Alignment = ToolStripItemAlignment.Right
            };
            toolbar.Items.Add(btnAddApp);

            btnAddApp.Click += (s, e) =>
            {
                using (var dlg = new AddAppForm())
                {
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        Image icon;

                        try
                        {
                            string path = dlg.AppPath;

                            // 🔹 Se è una scorciatoia .lnk, risolvi il percorso reale
                            if (Path.GetExtension(path).Equals(".lnk", StringComparison.OrdinalIgnoreCase))
                            {
                                path = ResolveShortcutTarget(path);
                            }

                            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                            {
                                if (Path.GetExtension(path).Equals(".exe", StringComparison.OrdinalIgnoreCase))
                                {
                                    Icon extracted = Icon.ExtractAssociatedIcon(path);
                                    icon = extracted?.ToBitmap() ?? SystemIcons.Application.ToBitmap();
                                }
                                else
                                {
                                    icon = SystemIcons.Application.ToBitmap();
                                }
                            }
                            else
                            {
                                icon = SystemIcons.Application.ToBitmap();
                            }
                        }
                        catch
                        {
                            icon = SystemIcons.Application.ToBitmap();
                        }

                        int rowIndex = grid.Rows.Add(
                            true,                                   // checkbox
                            dlg.AppName,                            // name
                            dlg.Mode,                               // mode
                            ResolveShortcutTarget(dlg.AppPath)      // percorso
                        );


                        // Memorizza i dettagli estesi nel Tag
                        grid.Rows[rowIndex].Tag = new AppEntry
                        {
                            ID = Guid.NewGuid().ToString(),
                            Active = true,
                            Name = dlg.AppName,
                            Path = ResolveShortcutTarget(dlg.AppPath),
                            Arguments = dlg.Arguments,
                            Mode = dlg.Mode,
                            Timing = dlg.Timing,
                            DelaySeconds = dlg.DelaySeconds,
                            StartMinimized = dlg.StartMinimized,
                            StartMinimizedDelaySeconds = dlg.StartMinimizedDelaySeconds,
                            CloseWindow = dlg.CloseWindow,
                            CloseWindowDelaySeconds = dlg.CloseWindowDelaySeconds,
                            CloseMSFS = dlg.CloseMSFS
                        };

                        appIcons[rowIndex] = icon;

                        grid.ClearSelection();
                        grid.Rows[rowIndex].Selected = true;
                        grid.FirstDisplayedScrollingRowIndex = rowIndex;

                        // 🔹 Auto‑save current profile
                        AutoSaveCurrentProfile();
                    }
                }
            };



            // --- GRID ---
            grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = true,
                BackgroundColor = Color.White,
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(245, 245, 245),
                    ForeColor = Color.Black,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold)
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Font = new Font("Segoe UI", 9),
                    SelectionBackColor = Color.LightSteelBlue,
                    SelectionForeColor = Color.Black
                },
                ReadOnly = false // serve per i checkbox
            };

            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = grid.ColumnHeadersDefaultCellStyle.BackColor;
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = grid.ColumnHeadersDefaultCellStyle.ForeColor;

            // ✅ Colonna 1 — Attivo
            var colActive = new DataGridViewCheckBoxColumn
            {
                HeaderText = "",
                Width = 40,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                ReadOnly = false,
                FlatStyle = FlatStyle.Standard,
            };
            grid.Columns.Add(colActive);
            grid.EditMode = DataGridViewEditMode.EditOnEnter;

            // ✅ Colonna 2 — Nome (con icona)
            var colName = new DataGridViewTextBoxColumn
            {
                HeaderText = "Name",
                Width = 250,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                ReadOnly = true
            };
            grid.Columns.Add(colName);

            // 🎨 Disegno icona accanto al nome
            grid.CellPainting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex != 1) // Colonna “Nome”
                    return;

                e.Handled = true;

                // --- SELEZIONE ---
                Color backColor = e.State.HasFlag(DataGridViewElementStates.Selected)
                    ? e.CellStyle.SelectionBackColor
                    : e.CellStyle.BackColor;

                using (Brush backBrush = new SolidBrush(backColor))
                    e.Graphics.FillRectangle(backBrush, e.CellBounds);


                // --- ICONA ---
                string path = grid.Rows[e.RowIndex].Cells[3]?.Value?.ToString() ?? "";
                Image icon = SystemIcons.Application.ToBitmap();

                try
                {
                    if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                    {
                        string ext = Path.GetExtension(path).ToLowerInvariant();

                        if (ext == ".exe")
                        {
                            Icon extracted = Icon.ExtractAssociatedIcon(path);
                            if (extracted != null) icon = extracted.ToBitmap();
                        }
                        else if (ext == ".lnk")
                        {
                            string real = ResolveShortcutTarget(path);
                            if (File.Exists(real))
                            {
                                Icon extracted = Icon.ExtractAssociatedIcon(real);
                                if (extracted != null) icon = extracted.ToBitmap();
                            }
                        }
                    }
                }
                catch { }

                int iconSize = 16;
                int padding = 4;
                int textOffset = iconSize + padding * 2;
                int iconY = e.CellBounds.Top + (e.CellBounds.Height - iconSize) / 2;

                e.Graphics.DrawImage(icon, e.CellBounds.Left + padding, iconY, iconSize, iconSize);


                // --- TESTO ---
                string text = e.FormattedValue?.ToString() ?? "";

                Color textColor = e.State.HasFlag(DataGridViewElementStates.Selected)
                    ? e.CellStyle.SelectionForeColor
                    : e.CellStyle.ForeColor;

                var textRect = new Rectangle(
                    e.CellBounds.Left + textOffset,
                    e.CellBounds.Top,
                    e.CellBounds.Width - textOffset,
                    e.CellBounds.Height
                );

                TextRenderer.DrawText(e.Graphics, text, e.CellStyle.Font, textRect,
                    textColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);


                // --- BORDI ---
                e.Paint(e.ClipBounds, DataGridViewPaintParts.Border);
            };



            // ✅ Colonna 3 — Modo
            var colMode = new DataGridViewTextBoxColumn
            {
                HeaderText = "Mode",
                Width = 100,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                ReadOnly = true
            };
            grid.Columns.Add(colMode);

            // ✅ Colonna 4 — Percorso
            var colPath = new DataGridViewTextBoxColumn
            {
                HeaderText = "Path",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                ReadOnly = true
            };
            grid.Columns.Add(colPath);

            // === DRAG & DROP DI FILE SULLA GRIGLIA ===
            grid.AllowDrop = true;

            grid.DragEnter += (s, e) =>
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    e.Effect = DragDropEffects.Copy;
                else
                    e.Effect = DragDropEffects.None;
            };

            grid.DragDrop += (s, e) =>
            {
                try
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    if (files == null || files.Length == 0)
                        return;

                    foreach (string file in files)
                    {
                        string path = file;
                        string name = Path.GetFileNameWithoutExtension(path);

                        // Se è un collegamento (.lnk), risolvi il percorso reale
                        if (Path.GetExtension(path).Equals(".lnk", StringComparison.OrdinalIgnoreCase))
                        {
                            path = ResolveShortcutTarget(path);
                        }

                        // Filtra solo file eseguibili o link
                        string ext = Path.GetExtension(path).ToLowerInvariant();
                        if (ext != ".exe" && ext != ".lnk")
                            continue;

                        // Estrai l’icona
                        Image icon;
                        try
                        {
                            Icon extracted = Icon.ExtractAssociatedIcon(path);
                            icon = extracted?.ToBitmap() ?? SystemIcons.Application.ToBitmap();
                        }
                        catch
                        {
                            icon = SystemIcons.Application.ToBitmap();
                        }

                        // Aggiungi la riga
                        int rowIndex = grid.Rows.Add(true, name, "MSFS", path);
                        grid.Rows[rowIndex].Tag = new AppEntry
                        {
                            ID = Guid.NewGuid().ToString(),
                            Active = true,
                            Name = name,
                            Mode = "MSFS",
                            Path = path,
                            Arguments = "",
                            Timing = "After",
                            DelaySeconds = 0,
                            StartMinimized = false,
                            CloseMSFS = false
                        };

                        appIcons[rowIndex] = icon;
                    }

                    AggiornaContatori();
                }
                catch (Exception ex)
                {
                    CustomDialogs.ShowError($"Error while adding apps from drag & drop:\n{ex.Message}", "Launch Manager 2024");
                }

                // 🔹 Salvataggio nascosto
                AutoSaveCurrentProfile();
            };


            //ATTIVAZIONE/DISATTIVAZIONE APP CON AUTOSALVATAGGIO PROFILO

            // ✅ Click immediato sui checkbox
            grid.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (grid.IsCurrentCellDirty)
                    grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };

            // ✅ Aggiorna contatori e salva automaticamente quando cambia lo stato del checkbox
            grid.CellValueChanged += (s, e) =>
            {
                if (e.ColumnIndex == 0 && e.RowIndex >= 0)
                {
                    var row = grid.Rows[e.RowIndex];
                    if (row.Tag is AppEntry app)
                    {
                        app.Active = row.Cells[0].Value is bool val && val;

                        // 🔹 Salvataggio nascosto
                        AutoSaveCurrentProfile();

                        // 🔹 Aggiorna status bar
                        if (!app.Active)
                        {
                            lblAppInfo.Text = $"⚠️ DEACTIVATED ⚠️";
                            lblAppInfo.ForeColor = Color.IndianRed;
                        }
                        else
                        {
                            lblAppInfo.Text = $"{(app.Mode == "LM" ? "Start with LM" : "Start with MSFS")}";
                            lblAppInfo.ForeColor = SystemColors.ControlText;
                        }

                        AggiornaContatori();
                    }
                }
            };



            // ✅ Deseleziona tutto cliccando sullo sfondo
            grid.MouseClick += (s, e) =>
            {
                var hit = grid.HitTest(e.X, e.Y);
                if (hit.Type == DataGridViewHitTestType.None)
                    grid.ClearSelection();
            };


            // --- MENU CONTESTUALE DELLA GRIGLIA (icone + effetto hover moderno) ---
            var contextMenu = new ContextMenuStrip
            {
                ShowImageMargin = true,
                ImageScalingSize = new Size(16, 16),
                Renderer = new MyMenuRenderer() // usa il renderer personalizzato definito sotto
            };

            // Helper per aggiungere voci con icona
            ToolStripMenuItem AddMenuItem(string text, string iconFile, EventHandler onClick)
            {
                Image icon = LoadIcon(iconFile);
                var item = new ToolStripMenuItem(text, icon, onClick);
                return item;
            }

            // --- VOCI PRINCIPALI ---
            var ctxAdd = AddMenuItem("Add App", "AddApp.png", (s, e) => btnAddApp.PerformClick());
            var ctxEdit = AddMenuItem("Edit App", "EditApp.png", (s, e) => btnEditApp.PerformClick());
            var ctxDelete = AddMenuItem("Delete App", "RemoveApp.png", (s, e) => btnRemoveApp.PerformClick());

            var ctxRun = AddMenuItem("Launch App", "LaunchApp.png", (s, e) => btnRunApp.PerformClick());
            var ctxOpen = AddMenuItem("Open App Folder", "OpenAppFolder.png", (s, e) => btnOpenAppFolder.PerformClick());

            var ctxBackup = AddMenuItem("Create Backup", "CreateBackup.png", (s, e) => btnManBackup.PerformClick());
            var ctxRestore = AddMenuItem("Restore Backup", "RestoreBackup.png", (s, e) => btnRestoreBackup.PerformClick());

            // --- NUOVE VOCI: CARTELLE PROFILI E BACKUP ---
            var ctxRefreshProfiles = AddMenuItem("Refresh Profile Folder", "SyncProfileFolder.png", (s, e) =>
            {
                try
                {
                    string current = cmbProfiles.SelectedItem?.ToString();
                    LoadProfiles(); // ricarica profili dalla cartella

                    // se il profilo esiste ancora, lo riseleziona
                    if (current != null && cmbProfiles.Items.Contains(current))
                        cmbProfiles.SelectedItem = current;
                }
                catch { }
            });

            var ctxOpenProfile = AddMenuItem("Open Profile Folder", "OpenProfileFolder.png", (s, e) =>
            {
                string profileDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Launch Manager 2024",
                    "FS2024",
                    "Profiles"
                );

                try
                {
                    if (Directory.Exists(profileDir))
                        Process.Start("explorer.exe", profileDir);
                    else
                        CustomDialogs.ShowInfo($"Profile folder not found:\n{profileDir}", "Launch Manager 2024");
                }
                catch (Exception ex)
                {
                    CustomDialogs.ShowError($"Error opening profile folder:\n{ex.Message}", "Launch Manager 2024");
                }
            });

            var ctxOpenBackup = AddMenuItem("Open Backup Folder", "OpenBackupFolder.png", (s, e) =>
            {
                string backupDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Launch Manager 2024",
                    "FS2024",
                    "Backup"
                );

                try
                {
                    if (Directory.Exists(backupDir))
                        Process.Start("explorer.exe", backupDir);
                    else
                        CustomDialogs.ShowInfo($"Backup folder not found:\n{backupDir}", "Launch Manager 2024");
                }
                catch (Exception ex)
                {
                    CustomDialogs.ShowError($"Error opening backup folder:\n{ex.Message}", "Launch Manager 2024");
                }
            });

            var ctxCleanBackup = AddMenuItem("Clean Backup Folder", "CleanBackupFolder.png", (s, e) =>
            {
                string backupDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Launch Manager 2024",
                    "FS2024",
                    "Backup"
                );

                if (!Directory.Exists(backupDir))
                {
                    CustomDialogs.ShowInfo($"Backup folder not found:\n{backupDir}", "Launch Manager 2024");
                    return;
                }

                if (CustomDialogs.ShowQuestion("Do you want to delete all files in the Backup folder?", "Launch Manager 2024") == DialogResult.Yes)
                {
                    try
                    {
                        foreach (var file in Directory.GetFiles(backupDir))
                            File.Delete(file);

                        CustomDialogs.ShowInfo("Backup folder cleaned successfully!", "Launch Manager 2024");
                    }
                    catch (Exception ex)
                    {
                        CustomDialogs.ShowError($"Error cleaning backup folder:\n{ex.Message}", "Launch Manager 2024");
                    }
                }
            });

            // --- STRUTTURA COMPLETA DEL MENU ---
            contextMenu.Items.AddRange(new ToolStripItem[]
            {
                ctxAdd, ctxEdit, ctxDelete,
                new ToolStripSeparator(),
                ctxRun,
                ctxOpen,
                new ToolStripSeparator(),
                ctxBackup,
                ctxRestore,
                new ToolStripSeparator(),
                ctxRefreshProfiles,
                ctxOpenProfile,
                ctxOpenBackup,
                ctxCleanBackup
            });

            // --- LOGICA DI ABILITAZIONE ---
            grid.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    var hit = grid.HitTest(e.X, e.Y);
                    bool hasSelection = hit.RowIndex >= 0 && hit.RowIndex < grid.Rows.Count;

                    ctxEdit.Enabled = hasSelection;
                    ctxDelete.Enabled = hasSelection;
                    ctxRun.Enabled = hasSelection;
                    ctxOpen.Enabled = hasSelection;

                    // --- Disattiva "Restore Backup" se la cartella è vuota ---
                    string backupDir = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "Launch Manager 2024",
                        "FS2024",
                        "Backup"
                    );

                    bool hasBackups = Directory.Exists(backupDir) && Directory.GetFiles(backupDir).Length > 0;
                    ctxRestore.Enabled = hasBackups;

                    if (!hasSelection)
                        grid.ClearSelection();
                }
            };

            // --- ASSEGNA IL MENU ALLA GRIGLIA ---
            grid.ContextMenuStrip = contextMenu;



            // --- SCORCIATOIE DA TASTIERA ---
            grid.KeyDown += (s, e) =>
            {
                if (e.Control && e.KeyCode == Keys.N) { btnAddApp.PerformClick(); e.SuppressKeyPress = true; }
                else if (e.Control && e.KeyCode == Keys.E) { btnEditApp.PerformClick(); e.SuppressKeyPress = true; }
                else if (e.KeyCode == Keys.Delete) { btnRemoveApp.PerformClick(); e.SuppressKeyPress = true; }
                else if (e.Control && e.KeyCode == Keys.R) { btnRunApp.PerformClick(); e.SuppressKeyPress = true; }
                else if (e.Control && e.KeyCode == Keys.F) { btnOpenAppFolder.PerformClick(); e.SuppressKeyPress = true; }
                else if (e.Control && e.KeyCode == Keys.B && !e.Shift) { btnManBackup.PerformClick(); e.SuppressKeyPress = true; }
                else if (e.Control && e.Shift && e.KeyCode == Keys.B) { btnRestoreBackup.PerformClick(); e.SuppressKeyPress = true; }
            };

            Controls.Add(grid);
            grid.BringToFront();


            // --- MOSTRA INFO APP NELLA STATUS STRIP ---
            grid.SelectionChanged += (s, e) =>
            {
                if (grid.SelectedRows.Count == 0)
                {
                    lblAppInfo.Text = "No application selected";
                    return;
                }

                var row = grid.SelectedRows[0];
                if (row.Tag is AppEntry app)
                {
                    // Se l'app è disattivata
                    if (!app.Active)
                    {
                        lblAppInfo.Text = $"⚠️ DEACTIVATED";
                        return;
                    }

                    // Mostra solo le info comuni
                    string info = $"{(app.Mode == "LM" ? "Start with LM" : "Start with MSFS")}";

                    if (app.Mode == "LM")
                    {
                        // Aggiungi le informazioni extra solo per LM
                        info +=
                            $"  |  {(app.Timing == "Before" ? "Start before MSFS" : "Start after MSFS")} ({app.DelaySeconds}s)" +
                            $"  |  Minimized: {(app.StartMinimized ? "Yes" : "No")} ({app.StartMinimizedDelaySeconds}s)" +
                            $"  |  CloseWin: {(app.CloseWindow ? "Yes" : "No")} ({app.CloseWindowDelaySeconds}s)" +
                            $"  |  Close with MSFS: {(app.CloseMSFS ? "Yes" : "No")}";
                    }

                    lblAppInfo.Text = info;
                }
                else
                {
                    lblAppInfo.Text = "App data not available";
                }
            };



            // --- Doppio clic sulla riga per modificare ---
            grid.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex < 0) return; // ignore header

                // seleziona la riga cliccata
                grid.ClearSelection();
                grid.Rows[e.RowIndex].Selected = true;

                // riusa tutta la logica di Edit + autosave
                btnEditApp.PerformClick();
            };




            // --- STATUS BAR ---
            status = new StatusStrip();
            // Label base
            lblTotal = new ToolStripLabel("Total: 0");
            lblActive = new ToolStripLabel("Active: 0");
            // Label centrale espandibile con info app
            lblAppInfo = new ToolStripStatusLabel("No app selected")
            {
                Spring = true, // si espande e occupa lo spazio centrale
                TextAlign = ContentAlignment.MiddleCenter
            };
            // Label autore finale
            lblCrtr = new ToolStripLabel("Launch Manager 2024 © by DMP79")
            {
                Alignment = ToolStripItemAlignment.Right
            };
            // Aggiunge elementi alla status bar in ordine
            status.Items.Add(lblTotal);
            status.Items.Add(lblActive);
            status.Items.Add(lblAppInfo);
            status.Items.Add(lblCrtr);
            Controls.Add(status);
            Controls.Add(toolbar);

        }



        // =====================================================================
        // 🔹 RISOLVE SCORCIATOIE (.lnk)
        // Questa funzione riceve il percorso di un file .lnk (scorciatoia di Windows)
        // e prova a determinare il percorso dell’eseguibile reale a cui punta.
        // Se qualcosa va storto o il file non è una scorciatoia valida,
        // restituisce semplicemente il percorso originale.
        // =====================================================================
        private string ResolveShortcutTarget(string shortcutPath)
        {
            try
            {
                // Se il file non esiste o non è un .lnk → restituisci così com'è
                if (!File.Exists(shortcutPath) ||
                    !shortcutPath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
                    return shortcutPath;

                // Altrimenti crea il COM solo per i .lnk
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                dynamic shell = Activator.CreateInstance(shellType);
                var shortcut = shell.CreateShortcut(shortcutPath);
                string target = shortcut.TargetPath;

                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell);
                return string.IsNullOrEmpty(target) ? shortcutPath : target;
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                // Scorciatoia non valida o accesso negato → restituisci originale
                return shortcutPath;
            }
            catch
            {
                return shortcutPath;
            }
        }



        // =========================================
        // GESTIONE APPLICAZIONI
        // =========================================
        private void AggiungiApp()
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Filter = "Programs|*.exe|All files|*.*";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    grid.Rows.Add(true, Path.GetFileNameWithoutExtension(dlg.FileName), "LM", dlg.FileName);
                    AggiornaContatori();
                }
            }
        }

        private void RimuoviApp()
        {
            foreach (DataGridViewRow row in grid.SelectedRows)
                grid.Rows.Remove(row);
            AggiornaContatori();
        }

        private void AggiornaContatori()
        {
            try
            {
                int total = 0;
                int active = 0;

                foreach (DataGridViewRow row in grid.Rows)
                {
                    if (row.IsNewRow) continue;
                    total++;

                    // Controlla se la cella 0 (checkbox) è true
                    if (row.Cells[0].Value is bool isActive && isActive)
                        active++;
                }

                lblTotal.Text = $"Total: {total}";
                lblActive.Text = $"Active: {active}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WARN] Error in UpdateCounters(): {ex.Message}");
            }
        }


        // =========================================
        // SALVATAGGIO E CARICAMENTO DATI
        // =========================================
        private void LoadData()
        {
            try
            {
                // 🧹 Pulisce la griglia e il dizionario icone
                grid.Rows.Clear();
                appIcons.Clear();

                // Nessun profilo selezionato → esci
                if (cmbProfiles == null || cmbProfiles.SelectedItem == null)
                    return;

                string sim = Paths.CurrentSim;
                string profileName = cmbProfiles.SelectedItem.ToString();

                string profilePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Launch Manager 2024",
                    sim,
                    "Profiles",
                    profileName + ".xml"
                );

                if (!File.Exists(profilePath))
                    return;

                // 🔹 Carica le app dal file XML
                var loadedApps = XmlStore.LoadPrograms(profilePath);
                _apps = loadedApps;

                int rowIndex = 0;
                foreach (var a in loadedApps)
                {
                    rowIndex = grid.Rows.Add(
                        a.Active,
                        a.Name,
                        a.Mode,
                        ResolveShortcutTarget(a.Path)
                    );

                    // Memorizza i dati completi nel Tag
                    grid.Rows[rowIndex].Tag = a;

                    // Carica l’icona
                    try
                    {
                        Icon extractedIcon = null;
                        if (!string.IsNullOrWhiteSpace(a.Path) && File.Exists(a.Path))
                            extractedIcon = Icon.ExtractAssociatedIcon(a.Path);

                        appIcons[rowIndex] = extractedIcon?.ToBitmap() ?? SystemIcons.Application.ToBitmap();
                    }
                    catch
                    {
                        appIcons[rowIndex] = SystemIcons.Application.ToBitmap();
                    }
                }


                AggiornaContatori();
            }
            catch (Exception ex)
            {
                CustomDialogs.ShowError($"Error loading XML profile:\n{ex.Message}", "Launch Manager 2024");
            }
        }

        public class ToolStripSpringLabel : ToolStripLabel
        {
            public override Size GetPreferredSize(Size constrainingSize)
            {
                if (IsOnOverflow || Owner == null)
                    return DefaultSize;

                // Spazio totale dell'area utile del ToolStrip
                int availableWidth = Owner.DisplayRectangle.Width;

                // Sottrai la larghezza di tutti gli altri item (margini inclusi)
                foreach (ToolStripItem item in Owner.Items)
                {
                    if (item.IsOnOverflow || item == this) continue;
                    availableWidth -= item.Width + item.Margin.Horizontal;
                }

                // Conta quanti spring ci sono (sinistro + destro, ecc.)
                int springCount = 0;
                foreach (ToolStripItem item in Owner.Items)
                {
                    if (item is ToolStripSpringLabel tsl && !item.IsOnOverflow)
                        springCount++;
                }
                if (springCount <= 0) springCount = 1; // fallback paranoico

                // Divide lo spazio tra tutti gli spring
                int myWidth = Math.Max(availableWidth / springCount, DefaultSize.Width);
                return new Size(myWidth, DefaultSize.Height);
            }
        }


        // SALVATAGGIO DATI
        private void SaveData()
        {
            try
            {
                var appList = new List<AppEntry>(); // ✅ crea la lista locale

                foreach (DataGridViewRow row in grid.Rows)
                {
                    if (row.IsNewRow) continue;

                    // 🔹 Se il Tag contiene un AppEntry completo, usalo
                    if (row.Tag is AppEntry app)
                    {
                        appList.Add(app);
                    }
                    else
                    {
                        // 🔹 fallback: crea un AppEntry minimo se necessario
                        appList.Add(new AppEntry
                        {
                            Active = row.Cells[0].Value is bool b && b,
                            Name = row.Cells[1].Value?.ToString(),
                            Mode = row.Cells[2].Value?.ToString(),
                            Path = row.Cells[3].Value?.ToString()
                        });
                    }
                }

                // 🔹 Percorso del profilo attivo (o Default)
                string profileName = cmbProfiles?.SelectedItem?.ToString() ?? "Default";
                string profileDir = Paths.GetProfilesPath();
                string profilePath = Path.Combine(profileDir, $"{profileName}.xml");

                // 🔹 Salva i programmi nel file XML del profilo
                XmlStore.SavePrograms(appList, profilePath); // ✅ qui ora salvi la lista corretta

                CustomDialogs.ShowInfo($"Configuration saved in profile:\n{profilePath}", "Launch Manager 2024");
            }
            catch (Exception ex)
            {
                CustomDialogs.ShowError("Error saving profile: " + ex.Message, "Launch Manager 2024");
            }
        }


        // SALVATAGGIO FILES BACKUP DI exe.xml (MAX 5)
        private string BackupExeXml(int maxBackups = 5)  // ← string!
        {
            try
            {
                string sim = Paths.CurrentSim;
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string backupDir = Path.Combine(appData, "Launch Manager 2024", sim, "Backup");
                Directory.CreateDirectory(backupDir);

                string exeXmlPath = Paths.ExeXmlPath;

                var backups = Directory.GetFiles(backupDir, "exe_backup_*.xml");    // Conta TUTTI i backup exe.xml_*
                while (backups.Length >= maxBackups)  
                {
                    var oldest = backups.OrderBy(f => File.GetCreationTime(f)).First(); // Cancella fino a farli diventare 5
                    File.Delete(oldest);
                    backups = Directory.GetFiles(backupDir, "exe_backup_*.xml");
                }

                string today = DateTime.Now.ToString("yyyyMMdd");
                string timestamp = DateTime.Now.ToString("HHmmss");
                string backupPath = Path.Combine(backupDir, $"exe_backup_{today}_{timestamp}.xml");
                if (File.Exists(exeXmlPath)) File.Copy(exeXmlPath, backupPath, true);

                return backupPath;  // ← CRUCIALE! Per i dialog
            }
            catch
            {
                return "";  // ← Errore sicuro
            }
        }

        
        // =========================================
        // CHIUSURA E SALVATAGGI FINALI
        // =========================================
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            SaveCompleteConfiguration();
            base.OnFormClosing(e);
        }


        // =========================================
        // SALVATAGGIO COMPLETO CONFIGURAZIONE
        // =========================================
        private void SaveCompleteConfiguration()
        {
            try
            {
                string configPath = Paths.GetConfigPath();

                var xml = new System.Xml.XmlDocument();
                System.Xml.XmlElement root;

                if (File.Exists(configPath))
                {
                    xml.Load(configPath);
                    root = xml.SelectSingleNode("/Config") as System.Xml.XmlElement;
                    if (root == null)
                    {
                        xml.RemoveAll();
                        root = xml.CreateElement("Config");
                        xml.AppendChild(root);
                    }
                }
                else
                {
                    xml.AppendChild(xml.CreateXmlDeclaration("1.0", "utf-8", null));
                    root = xml.CreateElement("Config");
                    xml.AppendChild(root);
                }

                void Upsert(string name, string value)
                {
                    var el = root[name] ?? xml.CreateElement(name);
                    el.InnerText = value ?? string.Empty;
                    if (el.ParentNode == null) root.AppendChild(el);
                }

                // === STATO FINESTRA ===

                Upsert("WindowX", Location.X.ToString());
                Upsert("WindowY", Location.Y.ToString());
                Upsert("WindowWidth", Width.ToString());
                Upsert("WindowHeight", Height.ToString());
                Upsert("WindowState", WindowState.ToString());

                // === PERCORSO EXE.XML ===
                Upsert("ExeXmlPath", Paths.ExeXmlPath);

                xml.Save(configPath);

                // === SALVA PROFILO CORRENTE ===
                if (grid.Rows.Count > 0)
                {
                    var appList = new List<AppEntry>();

                    foreach (DataGridViewRow row in grid.Rows)
                    {
                        if (row.IsNewRow) continue;

                        if (row.Tag is AppEntry app)
                        {
                            app.Active = Convert.ToBoolean(row.Cells[0].Value ?? app.Active);
                            app.Name = row.Cells[1].Value?.ToString() ?? app.Name;
                            app.Mode = row.Cells[2].Value?.ToString() ?? app.Mode;
                            app.Path = row.Cells[3].Value?.ToString() ?? app.Path;

                            appList.Add(app);
                        }
                        else
                        {
                            appList.Add(new AppEntry
                            {
                                Active = row.Cells[0].Value is bool b && b,
                                Name = row.Cells[1].Value?.ToString(),
                                Mode = row.Cells[2].Value?.ToString(),
                                Path = row.Cells[3].Value?.ToString()
                            });
                        }
                    }

                    string profileName = cmbProfiles?.SelectedItem?.ToString() ?? "Default";
                    string profilePath = Path.Combine(Paths.GetProfilesPath(), $"{profileName}.xml");
                    XmlStore.SavePrograms(appList, profilePath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Autosave failed: {ex.Message}");
            }
        }

        // Renderer semplice con effetto hover moderno
        // === RENDERER PERSONALIZZATO DEL MENU ===
        private class MyMenuRenderer : ToolStripProfessionalRenderer
        {
            protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
            {
                Rectangle rect = new Rectangle(Point.Empty, e.Item.Size);

                if (e.Item.Selected && e.Item.Enabled)
                {
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(230, 243, 255)))
                        e.Graphics.FillRectangle(b, rect);
                    using (Pen p = new Pen(Color.FromArgb(180, 210, 255)))
                        e.Graphics.DrawRectangle(p, new Rectangle(0, 0, rect.Width - 1, rect.Height - 1));
                }
                else
                {
                    using (SolidBrush b = new SolidBrush(Color.White))
                        e.Graphics.FillRectangle(b, rect);
                }
            }
        }

        // =========================================
        // CONFRONTO INIZIALE exe.xml / file profilo.xml
        // =========================================

        private List<ExeAppEntry> ParseExeXmlApps(string exeXmlPath)
        {
            var exeApps = new List<ExeAppEntry>();
            if (!File.Exists(exeXmlPath)) return exeApps;

            var xml = new XmlDocument();
            xml.Load(exeXmlPath);

            var addons = xml.SelectNodes("//Launch.Addon");
            foreach (XmlNode addon in addons)
            {
                var app = new ExeAppEntry();
                app.Name = addon.SelectSingleNode("Name")?.InnerText ?? "";
                app.Path = addon.SelectSingleNode("Path")?.InnerText ?? "";
                app.CommandLine = addon.SelectSingleNode("CommandLine")?.InnerText ?? "";
                var disabledText = addon.SelectSingleNode("Disabled")?.InnerText;
                app.Disabled = string.Equals(disabledText, "True", StringComparison.OrdinalIgnoreCase);
                exeApps.Add(app);
            }
            return exeApps;
        }


        // Codice x il salvataggio nascosto

        private void AutoSaveCurrentProfile()
        {
            string profileName = cmbProfiles.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(profileName))
                return;

            string sim = Paths.CurrentSim;
            string profilesDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Launch Manager 2024", sim, "Profiles"
            );
            string profilePath = Path.Combine(profilesDir, profileName + ".xml");

            var appList = new List<AppEntry>();
            foreach (DataGridViewRow r in grid.Rows)
                if (!r.IsNewRow && r.Tag is AppEntry a)
                    appList.Add(a);

            XmlStore.SavePrograms(appList, profilePath);
        }
    }
}
