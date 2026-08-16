using LaunchManager.Controls;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace LaunchManager
{
    public class AddAppForm : Form
    {
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

        public bool IncludeNewConsole => chkIncludeNewConsole.Checked;
        public bool NewConsole => cmbNewConsole.SelectedItem?.ToString() == "True";

        public string Mode => rdoLM.Checked ? "LM" : "MSFS";

        public TextBox txtName, txtPath, txtArgs;
        public NumericUpDown numDelaySeconds, numStartMinimizedDelaySeconds, numCloseWindowDelaySeconds;
        public CheckBox chkStartMinimized, chkCloseWindow, chkCloseMSFS;
        public CheckBox chkIncludeNewConsole;
        public ComboBox cmbNewConsole;
        public RadioButton rdoBef, rdoAft;
        public RadioButton rdoLM, rdoMSFS;

        private Button btnBrowse, btnAccept, btnCancel;
        private Label lblStartMinimizedDelaySeconds, lblCloseWindowDelaySeconds, lblDelaySeconds;

        public AddAppForm()
        {
            InitializeComponent();

            rdoLM.CheckedChanged += (s, e) =>
            {
                AggiornaUI();
                ThemeManager.ApplyTheme(this);
            };

            rdoMSFS.CheckedChanged += (s, e) =>
            {
                AggiornaUI();
                ThemeManager.ApplyTheme(this);
            };

            chkStartMinimized.CheckedChanged += (s, e) =>
            {
                numStartMinimizedDelaySeconds.Enabled =
                    chkStartMinimized.Checked && rdoLM.Checked;

                ThemeManager.ApplyTheme(this);
            };

            chkCloseWindow.CheckedChanged += (s, e) =>
            {
                numCloseWindowDelaySeconds.Enabled =
                    chkCloseWindow.Checked && rdoLM.Checked;

                ThemeManager.ApplyTheme(this);
            };

            chkIncludeNewConsole.CheckedChanged += (s, e) =>
            {
                cmbNewConsole.Enabled = chkIncludeNewConsole.Checked;
                ThemeManager.ApplyTheme(this);
            };

            AggiornaUI();
            ThemeManager.ApplyTheme(this);
        }

        private void InitializeComponent()
        {
            Text = "Add app";
            Size = new Size(400, 525);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            Label lblName = new Label
            {
                Text = "Name:",
                Location = new Point(20, 20),
                AutoSize = true
            };

            txtName = new TextBox
            {
                Location = new Point(100, 18),
                Width = 250
            };

            Label lblPath = new Label
            {
                Text = "Path:",
                Location = new Point(20, 60),
                AutoSize = true
            };

            txtPath = new TextBox
            {
                Location = new Point(100, 58),
                Width = 210
            };

            btnBrowse = new Button
            {
                Text = "...",
                Location = new Point(320, 56),
                Width = 30
            };

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

            Label lblArgs = new Label
            {
                Text = "Arguments:",
                Location = new Point(20, 100),
                AutoSize = true
            };

            txtArgs = new TextBox
            {
                Location = new Point(100, 98),
                Width = 250
            };

            GroupBox grpTiming = new GroupBox
            {
                Text = "Starts compared to MSFS",
                Location = new Point(20, 130),
                Size = new Size(340, 70)
            };

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

            grpTiming.Controls.AddRange(new Control[]
            {
                rdoAft,
                lblDelaySeconds,
                numDelaySeconds
            });

            Controls.Add(grpTiming);

            GroupBox grp = new GroupBox
            {
                Text = "Additional options",
                Location = new Point(20, 210),
                Size = new Size(340, 160)
            };

            chkStartMinimized = new CheckBox
            {
                Text = "Start minimized",
                Location = new Point(10, 25),
                AutoSize = true
            };

            numStartMinimizedDelaySeconds = new NumericUpDown
            {
                Location = new Point(150, 23),
                Width = 50,
                Minimum = 0,
                Maximum = 600
            };

            lblStartMinimizedDelaySeconds = new Label
            {
                Text = "Delay (s)",
                Location = new Point(220, 25),
                AutoSize = true
            };

            chkCloseWindow = new CheckBox
            {
                Text = "Close window",
                Location = new Point(10, 55),
                AutoSize = true
            };

            numCloseWindowDelaySeconds = new NumericUpDown
            {
                Location = new Point(150, 53),
                Width = 50,
                Minimum = 0,
                Maximum = 600
            };

            lblCloseWindowDelaySeconds = new Label
            {
                Text = "Delay (s)",
                Location = new Point(220, 55),
                AutoSize = true
            };

            chkCloseMSFS = new CheckBox
            {
                Text = "Close when MSFS closes",
                Location = new Point(10, 85),
                AutoSize = true
            };

            chkIncludeNewConsole = new CheckBox
            {
                Text = "Include NewConsole",
                Location = new Point(10, 115),
                AutoSize = true
            };

            cmbNewConsole = new ComboBox
            {
                Location = new Point(205, 112),
                Width = 85,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = false
            };

            cmbNewConsole.Items.AddRange(new object[]
            {
                "False",
                "True"
            });

            cmbNewConsole.SelectedIndex = 0;

            grp.Controls.AddRange(new Control[]
            {
                chkStartMinimized,
                numStartMinimizedDelaySeconds,
                lblStartMinimizedDelaySeconds,

                chkCloseWindow,
                numCloseWindowDelaySeconds,
                lblCloseWindowDelaySeconds,

                chkCloseMSFS,

                chkIncludeNewConsole,
                cmbNewConsole
            });

            Controls.Add(grp);

            GroupBox grpMode = new GroupBox
            {
                Text = "Start with",
                Location = new Point(20, 380),
                Size = new Size(340, 60)
            };

            rdoLM = new RadioButton
            {
                Text = "LM",
                Location = new Point(20, 25),
                AutoSize = true
            };

            rdoMSFS = new RadioButton
            {
                Text = "MSFS",
                Location = new Point(200, 25),
                AutoSize = true,
                Checked = true
            };

            grpMode.Controls.AddRange(new Control[]
            {
                rdoLM,
                rdoMSFS
            });

            Controls.Add(grpMode);

            btnAccept = new Button
            {
                Text = "Confirm",
                Location = new Point(80, 440),
                Width = 100
            };

            btnAccept.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(AppPath) || !File.Exists(AppPath))
                {
                    CustomDialogs.ShowError("Select a valid executable file.", "Error");
                    return;
                }

                DialogResult = DialogResult.OK;
                Close();
            };

            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(200, 440),
                Width = 100
            };

            btnCancel.Click += (s, e) => Close();

            Controls.AddRange(new Control[]
            {
                lblName,
                txtName,

                lblPath,
                txtPath,
                btnBrowse,

                lblArgs,
                txtArgs,

                grpTiming,
                grp,
                grpMode,

                btnAccept,
                btnCancel
            });
        }

        private void AggiornaUI()
        {
            bool isLM = rdoLM.Checked;

            rdoAft.Enabled = isLM;
            numDelaySeconds.Enabled = isLM;

            chkStartMinimized.Enabled = isLM;
            numStartMinimizedDelaySeconds.Enabled =
                isLM && chkStartMinimized.Checked;

            chkCloseWindow.Enabled = isLM;
            numCloseWindowDelaySeconds.Enabled =
                isLM && chkCloseWindow.Checked;

            chkCloseMSFS.Enabled = isLM;

            chkIncludeNewConsole.Enabled = true;
            cmbNewConsole.Enabled = chkIncludeNewConsole.Checked;
        }
    }
}