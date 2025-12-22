using System;
using System.Drawing;
using System.Windows.Forms;

namespace LaunchManager
{
    public class ProfileForm : Form
    {
        public string ProfileName { get; private set; }

        private TextBox txtProfileName;
        private Button btnOk;
        private Button btnCancel;

        public ProfileForm()
        {
            Text = "New profile";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(350, 140);
            MaximizeBox = false;
            MinimizeBox = false;

            // Etichetta
            Label lbl = new Label
            {
                Text = "Profile Name:",
                AutoSize = true,
                Location = new Point(20, 20)
            };
            Controls.Add(lbl);

            // TEXTBOX (tema)
            txtProfileName = new TextBox
            {
                Location = new Point(20, 45),
                Width = 300,
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(txtProfileName);

            // Conferma
            btnOk = new Button
            {
                Text = "Confirm",
                Location = new Point(140, 90),
                DialogResult = DialogResult.OK
            };
            Controls.Add(btnOk);

            // Annulla
            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(240, 90),
                DialogResult = DialogResult.Cancel
            };
            Controls.Add(btnCancel);

            // Applica tema
            ThemeManager.ApplyTheme(this);

            AcceptButton = btnOk;
            CancelButton = btnCancel;

            // Validazione
            btnOk.Click += (s, e) =>
            {
                ProfileName = txtProfileName.Text.Trim();
                if (string.IsNullOrEmpty(ProfileName))
                {
                    MessageBox.Show("Please enter a valid profile name.", "Warning",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.None;
                }
            };
        }
    }
}
