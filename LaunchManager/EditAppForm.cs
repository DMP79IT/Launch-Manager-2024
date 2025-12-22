using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace LaunchManager
{
    public class EditAppForm : Form
    {

        public bool _loadingData = false;
        public string AppName => txtName.Text.Trim();
        public string AppPath => txtPath.Text.Trim();
        public string Arguments => txtArgs.Text.Trim();
        public string Timing => "After";
        public int DelaySeconds => (int)numDelaySeconds.Value;
        public bool StartMinimized => chkStartMinimized.Checked;
        public int StartMinimizedDelaySeconds => (int)numStartMinimizedDelaySeconds.Value;
        public bool CloseWindow => chkCloseWindow.Checked;
        public int CloseWindowDelaySeconds => (int)numCloseWindowDelaySeconds.Value;
        public bool CloseMSFS => chkCloseMSFS.Checked;
        public string Mode => rdoLM.Checked ? "LM" : "MSFS";

        // Rese public per accesso dal MainForm
        public TextBox txtName, txtPath, txtArgs;
        public NumericUpDown numDelaySeconds, numStartMinimizedDelaySeconds, numCloseWindowDelaySeconds;
        public CheckBox chkStartMinimized, chkCloseWindow, chkCloseMSFS;
        public RadioButton rdoBef, rdoAft;
        public RadioButton rdoLM, rdoMSFS;
        private Button btnBrowse, btnAccept, btnCancel;
        private Label lblStartMinimizedDelaySeconds, lblCloseWindowDelaySeconds, lblDelaySeconds;



        public EditAppForm()
        {
            InitializeComponent();

            ThemeManager.ApplyTheme(this);


            // === EVENTI DINAMICI ===
            rdoLM.CheckedChanged += (s, e) => AggiornaUI();
            rdoMSFS.CheckedChanged += (s, e) => AggiornaUI();

            chkStartMinimized.CheckedChanged += (s, e) => numStartMinimizedDelaySeconds.Enabled = chkStartMinimized.Checked;
            chkCloseWindow.CheckedChanged += (s, e) => numCloseWindowDelaySeconds.Enabled = chkCloseWindow.Checked;

            AggiornaUI(); // Imposta stato iniziale
        }

        private void InitializeComponent()
        {
            Text = "Edit app";
            Size = new Size(400, 480);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.White;

            // --- NOME ---
            Label lblName = new Label { Text = "Name:", Location = new Point(20, 20), AutoSize = true };
            txtName = new TextBox { Location = new Point(100, 18), Width = 250 };

            // --- PERCORSO ---
            Label lblPath = new Label { Text = "Path:", Location = new Point(20, 60), AutoSize = true };
            txtPath = new TextBox { Location = new Point(100, 58), Width = 210 };
            btnBrowse = new Button { Text = "...", Location = new Point(320, 56), Width = 30 };
            btnBrowse.Click += (s, e) =>
            {
                using (var dlg = new OpenFileDialog())
                {
                    dlg.Filter = "Programs (*.exe;*.lnk)|*.exe;*.lnk|All files (*.*)|*.*";
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        txtPath.Text = dlg.FileName;
                        if (string.IsNullOrWhiteSpace(txtName.Text))
                            txtName.Text = Path.GetFileNameWithoutExtension(dlg.FileName);
                    }
                }
            };

            // --- ARGOMENTI ---
            Label lblArgs = new Label { Text = "Arguments:", Location = new Point(20, 100), AutoSize = true };
            txtArgs = new TextBox { Location = new Point(100, 98), Width = 250 };

            // --- GRUPPO: AVVIO RISPETTO A MSFS ---
            GroupBox grpTiming = new GroupBox
            {
                Text = "Starts compared to MSFS",
                Location = new Point(20, 130),
                Size = new Size(340, 70)
            };

            // Solo AFTER rimane
            rdoAft = new RadioButton
            {
                Text = "After",
                Location = new Point(20, 30),
                AutoSize = true,
                Checked = true
            };

            lblDelaySeconds = new Label
            {
                Text = "Startup time (s):",
                Location = new Point(100, 30),
                AutoSize = true
            };

            numDelaySeconds = new NumericUpDown
            {
                Location = new Point(220, 28),
                Width = 60,
                Minimum = 0,
                Maximum = 600
            };

            grpTiming.Controls.AddRange(new Control[] { rdoAft, lblDelaySeconds, numDelaySeconds });
            Controls.Add(grpTiming);


            // --- OPZIONI AGGIUNTIVE ---
            GroupBox grp = new GroupBox
            {
                Text = "Additional options",
                Location = new Point(20, 210),
                Size = new Size(340, 130)
            };
            chkStartMinimized = new CheckBox { Text = "Start minimized", Location = new Point(10, 25), AutoSize = true };
            numStartMinimizedDelaySeconds = new NumericUpDown { Location = new Point(150, 23), Width = 50, Minimum = 0, Maximum = 600 };
            lblStartMinimizedDelaySeconds = new Label { Text = "Delay (s)", Location = new Point(220, 25), AutoSize = true };
            chkCloseWindow = new CheckBox { Text = "Close window", Location = new Point(10, 55), AutoSize = true };
            numCloseWindowDelaySeconds = new NumericUpDown { Location = new Point(150, 53), Width = 50, Minimum = 0, Maximum = 600 };
            lblCloseWindowDelaySeconds = new Label { Text = "Delay (s)", Location = new Point(220, 55), AutoSize = true };
            chkCloseMSFS = new CheckBox { Text = "Close when MSFS closes", Location = new Point(10, 85), AutoSize = true };
            grp.Controls.AddRange(new Control[] { chkStartMinimized, numStartMinimizedDelaySeconds, lblStartMinimizedDelaySeconds, chkCloseWindow, numCloseWindowDelaySeconds, lblCloseWindowDelaySeconds, chkCloseMSFS });

            // --- GRUPPO: MODALITÀ ---
            GroupBox grpMode = new GroupBox
            {
                Text = "Start with",
                Location = new Point(20, 350),
                Size = new Size(340, 60)
            };
            rdoLM = new RadioButton { Text = "LM", Location = new Point(20, 25), AutoSize = true, Checked = true };
            rdoMSFS = new RadioButton { Text = "MSFS", Location = new Point(200, 25), AutoSize = true };
            grpMode.Controls.AddRange(new Control[] { rdoLM, rdoMSFS });

            // --- BOTTONI ---
            btnAccept = new Button { Text = "Confirm", Location = new Point(80, 410), Width = 100 };
            btnAccept.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(AppPath) || !File.Exists(AppPath))
                {
                    MessageBox.Show("Select a valid executable file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                DialogResult = DialogResult.OK;
                Close();
            };

            btnCancel = new Button { Text = "Cancel", Location = new Point(200, 410), Width = 100 };
            btnCancel.Click += (s, e) => Close();

            Controls.AddRange(new Control[]
            {
        lblName, txtName,
        lblPath, txtPath, btnBrowse,
        lblArgs, txtArgs,
        grpTiming,
        grp,
        grpMode,
        btnAccept, btnCancel
            });
        }


        // === LOGICA DI ATTIVAZIONE CONTROLLI ===
        public void AggiornaUI()
        {
            if (_loadingData) return; // blocca durante il caricamento iniziale

            bool isLM = rdoLM.Checked;

            rdoAft.Enabled = isLM;
            numDelaySeconds.Enabled = isLM;

            chkStartMinimized.Enabled = isLM;
            numStartMinimizedDelaySeconds.Enabled = isLM && chkStartMinimized.Checked;

            chkCloseWindow.Enabled = isLM;
            numCloseWindowDelaySeconds.Enabled = isLM && chkCloseWindow.Checked;

            chkCloseMSFS.Enabled = isLM;
        }
    }
}
