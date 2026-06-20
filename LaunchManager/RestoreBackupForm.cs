using LaunchManager.Controls;
using LaunchManager.Services;
using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace LaunchManager
{
    public class RestoreBackupForm : Form
    {
        private CustomComboBox cmbBackups;
        private Button btnConfirm;
        private Button btnCancel;
        public string SelectedBackupPath { get; private set; }

        public RestoreBackupForm()
        {
            Text = "Restore Backup exe.xml";
            Width = 400;
            Height = 180;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            Label lbl = new Label()
            {
                Text = "Select a backup to restore:",
                Left = 20,
                Top = 20,
                Width = 340
            };
            Controls.Add(lbl);

            cmbBackups = new CustomComboBox()
            {
                Left = 20,
                Top = 50,
                Width = 340,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            Controls.Add(cmbBackups);

            btnConfirm = new Button()
            {
                Text = "Confirm",
                Left = 160,
                Top = 90,
                Width = 100
            };
            btnConfirm.Click += BtnConfirm_Click;
            Controls.Add(btnConfirm);

            btnCancel = new Button()
            {
                Text = "Cancel",
                Left = 270,
                Top = 90,
                Width = 90
            };
            btnCancel.Click += (s, e) =>
            {
                SelectedBackupPath = null;
                DialogResult = DialogResult.Cancel;
                Close();
            };
            Controls.Add(btnCancel);

            LoadBackups();

            ThemeManager.ApplyTheme(this);
        }

        private void LoadBackups()
        {
            try
            {
                string backupDir = ConfigService.GetEffectiveBackupPath();

                if (!Directory.Exists(backupDir))
                {
                    CustomDialogs.ShowError($"The backup folder does not exist.\n{backupDir}", "Launch Manager 2024");
                    return;
                }

                cmbBackups.Items.Clear();

                var files = Directory.GetFiles(backupDir, "exebackup_*.xml")
                                     .OrderByDescending(f => File.GetLastWriteTime(f))
                                     .ToList();

                foreach (var file in files)
                {
                    string nome = Path.GetFileName(file);
                    cmbBackups.Items.Add(nome);
                }

                if (cmbBackups.Items.Count > 0)
                    cmbBackups.SelectedIndex = 0;
                else
                    CustomDialogs.ShowInfo($"No backup available in the folder:\n{backupDir}", "Launch Manager 2024");
            }
            catch (Exception ex)
            {
                CustomDialogs.ShowError("Error loading backups: " + ex.Message, "Launch Manager 2024");
            }
        }

        private void BtnConfirm_Click(object sender, EventArgs e)
        {
            if (cmbBackups.SelectedItem == null)
            {
                CustomDialogs.ShowError("Please select a backup before proceeding.", "Launch Manager 2024");
                return;
            }

            string backupDir = ConfigService.GetEffectiveBackupPath();
            string selectedFile = Path.Combine(backupDir, cmbBackups.SelectedItem.ToString());

            if (!File.Exists(selectedFile))
            {
                CustomDialogs.ShowError("The selected file no longer exists.", "Launch Manager 2024");
                return;
            }

            SelectedBackupPath = selectedFile;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}