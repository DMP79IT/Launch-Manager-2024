using System;
using System.Drawing;
using System.Windows.Forms;

namespace LaunchManager
{
    public static class CustomDialogs
    {
        public static DialogResult ConfirmCleanup(string message, string title)
        {
            Form prompt = new Form()
            {
                Width = 400,
                Height = 160,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = title,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };

            Label lblMessage = new Label()
            {
                Text = message,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 60
            };

            Button btnYes = new Button()
            {
                Text = "Yes",
                DialogResult = DialogResult.Yes,
                Width = 100,
                Height = 25,
                Location = new Point(80, 80),
                FlatStyle = FlatStyle.Standard,
                TextAlign = ContentAlignment.MiddleCenter
            };

            Button btnNo = new Button()
            {
                Text = "No",
                DialogResult = DialogResult.No,
                Width = 100,
                Height = 25,
                Location = new Point(210, 80),
                FlatStyle = FlatStyle.Standard,
                TextAlign = ContentAlignment.MiddleCenter
            };

            prompt.Controls.Add(lblMessage);
            prompt.Controls.Add(btnYes);
            prompt.Controls.Add(btnNo);

            prompt.AcceptButton = btnYes;
            prompt.CancelButton = btnNo;

            ThemeManager.ApplyTheme(prompt);

            return prompt.ShowDialog();
        }
    
        public static DialogResult RemoveProfileConfirm(string message, string title)
        {
            Form prompt = new Form()
            {
                Width = 400,
                Height = 160,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = title,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };
            
            Label lblMessage = new Label()
            {
                Text = message,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 60
            };

            Button btnYes = new Button()
            {
                Text = "Yes",
                DialogResult = DialogResult.Yes,
                Width = 100,
                Height = 25,
                Location = new Point(80, 80),
                FlatStyle = FlatStyle.Standard,
                TextAlign = ContentAlignment.MiddleCenter
            };

            Button btnNo = new Button()
            {
                Text = "No",
                DialogResult = DialogResult.No,
                Width = 100,
                Height = 25,
                Location = new Point(210, 80),
                FlatStyle = FlatStyle.Standard,
                TextAlign = ContentAlignment.MiddleCenter
            };

            prompt.Controls.Add(lblMessage);
            prompt.Controls.Add(btnYes);
            prompt.Controls.Add(btnNo);

            prompt.AcceptButton = btnYes;
            prompt.CancelButton = btnNo;

            ThemeManager.ApplyTheme(prompt);

            return prompt.ShowDialog();
        }

        public static DialogResult RemoveAppConfirm(string message, string title)
        {
            Form prompt = new Form()
            {
                Width = 400,
                Height = 160,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = title,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };
            
            Label lblMessage = new Label()
            {
                Text = message,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 60
            };

            Button btnYes = new Button()
            {
                Text = "Yes",
                DialogResult = DialogResult.Yes,
                Width = 100,
                Height = 25,
                Location = new Point(80, 80),
                FlatStyle = FlatStyle.Standard,
                TextAlign = ContentAlignment.MiddleCenter
            };

            Button btnNo = new Button()
            {
                Text = "No",
                DialogResult = DialogResult.No,
                Width = 100,
                Height = 25,
                Location = new Point(210, 80),
                FlatStyle = FlatStyle.Standard,
                TextAlign = ContentAlignment.MiddleCenter
            };

            prompt.Controls.Add(lblMessage);
            prompt.Controls.Add(btnYes);
            prompt.Controls.Add(btnNo);

            prompt.AcceptButton = btnYes;
            prompt.CancelButton = btnNo;

            ThemeManager.ApplyTheme(prompt);

            return prompt.ShowDialog();
        }

        public static string RenameProfileDialog(string oldName)
        {
            Form prompt = new Form()
            {
                Width = 400,
                Height = 190,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Rename Profile",
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };
           
            Label lblMessage = new Label()
            {
                Text = $"Enter the new name for the profile: \n\"{oldName}\"",
                Font = new Font("Segoe UI", 10F),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 55
            };

            TextBox txtNewName = new TextBox()
            {
                Font = new Font("Segoe UI", 10F),
                Width = 320,
                Location = new Point(40, 70)
            };

            Button btnOK = new Button()
            {
                Text = "Confirm",
                DialogResult = DialogResult.OK,
                Width = 120,
                Height = 28,
                Location = new Point(60, 110),
                FlatStyle = FlatStyle.Standard
            };

            Button btnCancel = new Button()
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Width = 120,
                Height = 28,
                Location = new Point(200, 110),
                FlatStyle = FlatStyle.Standard
            };

            prompt.Controls.Add(lblMessage);
            prompt.Controls.Add(txtNewName);
            prompt.Controls.Add(btnOK);
            prompt.Controls.Add(btnCancel);

            prompt.AcceptButton = btnOK;
            prompt.CancelButton = btnCancel;

            ThemeManager.ApplyTheme(prompt);

            var result = prompt.ShowDialog();

            if (result == DialogResult.OK)
                return txtNewName.Text.Trim();
            else
                return null; // Utente ha annullato
        }

        public static DialogResult ShowUpdateDialog(Version current, Version online)
        {
            Form prompt = new Form()
            {
                Width = 420,
                Height = 190,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Update available",
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };

            Label lblMessage = new Label()
            {
                Text = $"A new version of Launch Manager 2024 is available.\n\n" +
                       $"Current version: {current}\nOnline version: {online}\n\n" +
                       $"Do you want to open the download page on Flightsim.to?",
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 90
            };

            Button btnYes = new Button()
            {
                Text = "Open page",
                DialogResult = DialogResult.Yes,
                Width = 120,
                Height = 28,
                Location = new Point(60, 110),
                FlatStyle = FlatStyle.Standard,
                TextAlign = ContentAlignment.MiddleCenter
            };

            Button btnNo = new Button()
            {
                Text = "Cancel",
                DialogResult = DialogResult.No,
                Width = 120,
                Height = 28,
                Location = new Point(220, 110),
                FlatStyle = FlatStyle.Standard,
                TextAlign = ContentAlignment.MiddleCenter
            };

            prompt.Controls.Add(lblMessage);
            prompt.Controls.Add(btnYes);
            prompt.Controls.Add(btnNo);

            prompt.AcceptButton = btnYes;
            prompt.CancelButton = btnNo;

            ThemeManager.ApplyTheme(prompt);

            return prompt.ShowDialog();
        }

        public static DialogResult ShowQuestion(string message, string title)
        {
            Form prompt = new Form()
            {
                Width = 420,
                Height = 180,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = title,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };

            Label lblMessage = new Label()
            {
                Text = message,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 80
            };

            Button btnYes = new Button()
            {
                Text = "Yes",
                DialogResult = DialogResult.Yes,
                Width = 120,
                Height = 28,
                Location = new Point(60, 100),
                FlatStyle = FlatStyle.Standard
            };

            Button btnNo = new Button()
            {
                Text = "No",
                DialogResult = DialogResult.No,
                Width = 120,
                Height = 28,
                Location = new Point(220, 100),
                FlatStyle = FlatStyle.Standard
            };

            prompt.Controls.Add(lblMessage);
            prompt.Controls.Add(btnYes);
            prompt.Controls.Add(btnNo);

            prompt.AcceptButton = btnYes;
            prompt.CancelButton = btnNo;

            ThemeManager.ApplyTheme(prompt);

            return prompt.ShowDialog();
        }

        public static void ShowError(string message, string title)
        {
            Form prompt = new Form()
            {
                Width = 420,
                Height = 180,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = title,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };

            Label lblMessage = new Label()
            {
                Text = message,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 90
            };

            Button btnOk = new Button()
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Width = 120,
                Height = 28,
                Location = new Point(150, 110),
                FlatStyle = FlatStyle.Standard,
                TextAlign = ContentAlignment.MiddleCenter
            };

            prompt.Controls.Add(lblMessage);
            prompt.Controls.Add(btnOk);

            prompt.AcceptButton = btnOk;
            prompt.CancelButton = btnOk;

            ThemeManager.ApplyTheme(prompt);

            prompt.ShowDialog();
        }

        public static void ShowInfo(string message, string title)
        {
            Form prompt = new Form()
            {
                Width = 420,
                Height = 180,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = title,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };

            Label lblMessage = new Label()
            {
                Text = message,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 90
            };

            Button btnOk = new Button()
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Width = 120,
                Height = 28,
                Location = new Point(150, 110),
                FlatStyle = FlatStyle.Standard,
                TextAlign = ContentAlignment.MiddleCenter
            };

            prompt.Controls.Add(lblMessage);
            prompt.Controls.Add(btnOk);

            prompt.AcceptButton = btnOk;
            prompt.CancelButton = btnOk;

            ThemeManager.ApplyTheme(prompt);

            prompt.ShowDialog();
        }
    }
}

    