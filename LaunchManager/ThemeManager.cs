using System;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;
using System.IO;

namespace LaunchManager
{
    public static class ThemeManager
    {
        public enum ThemeMode { Light, Dark }

        public static ThemeMode CurrentTheme = ThemeMode.Light;

        // -------------------
        // PALETTE
        // -------------------
        public static Color LightBack = Color.FromArgb(232, 232, 232);       // #E8E8E8
        public static Color LightControl = Color.FromArgb(242, 242, 242);    // #F2F2F2
        public static Color LightText = Color.FromArgb(32, 32, 32);          // #202020
        public static Color LightToolbar = Color.FromArgb(220, 220, 220);    // #DCDCDC

        // Dark più chiaro (grafite)
        public static Color DarkBack = Color.FromArgb(55, 57, 60);
        public static Color DarkControl = Color.FromArgb(68, 71, 74);
        public static Color DarkText = Color.FromArgb(235, 235, 235);
        public static Color DarkToolbar = Color.FromArgb(78, 81, 84);

        // =======================================================
        //  CARICA TEMA DA CONFIG.XML
        // =======================================================
        public static void LoadTheme()
        {
            try
            {
                string configPath = Paths.GetConfigPath();
                if (!File.Exists(configPath)) return;

                XmlDocument xml = new XmlDocument();
                xml.Load(configPath);

                var node = xml.SelectSingleNode("/Config/Theme");
                if (node != null)
                {
                    CurrentTheme = node.InnerText == "Dark" ?
                        ThemeMode.Dark : ThemeMode.Light;
                }
            }
            catch { }
        }

        // =======================================================
        //  SALVA TEMA IN CONFIG.XML
        // =======================================================
        public static void SaveTheme()
        {
            try
            {
                string configPath = Paths.GetConfigPath();

                if (!File.Exists(configPath))
                {
                    XmlDocument newDoc = new XmlDocument();
                    var root = newDoc.CreateElement("Config");
                    newDoc.AppendChild(root);
                    newDoc.Save(configPath);
                }

                XmlDocument xml = new XmlDocument();
                xml.Load(configPath);

                var rootNode = xml.SelectSingleNode("/Config")
                    ?? xml.AppendChild(xml.CreateElement("Config"));

                var oldNode = rootNode.SelectSingleNode("Theme");
                if (oldNode != null) rootNode.RemoveChild(oldNode);

                var themeNode = xml.CreateElement("Theme");
                themeNode.InnerText = CurrentTheme == ThemeMode.Light ? "Light" : "Dark";
                rootNode.AppendChild(themeNode);

                xml.Save(configPath);
            }
            catch { }
        }

        // =======================================================
        //  APPLICA IL TEMA AD UN FORM o CONTROL
        // =======================================================
        public static void ApplyTheme(Control root)
        {
            if (root is Form f)
            {
                f.BackColor = CurrentTheme == ThemeMode.Light ? LightBack : DarkBack;
                f.ForeColor = CurrentTheme == ThemeMode.Light ? LightText : DarkText;
            }

            foreach (Control c in root.Controls)
            {
                ApplyControlTheme(c);
                ApplyTheme(c);
            }
        }

        // =======================================================
        //  TEMA PER CONTROLLO SINGOLO
        // =======================================================
        private static void ApplyControlTheme(Control c)
        {
            bool dark = CurrentTheme == ThemeMode.Dark;

            Color back = dark ? DarkControl : LightControl;
            Color txt = dark ? DarkText : LightText;
            Color txtDisabled = dark ? Color.FromArgb(150, 150, 150) : Color.FromArgb(120, 120, 120);
            Color backDisabled = dark ? Color.FromArgb(70, 70, 70) : Color.FromArgb(220, 220, 220);

            switch (c)
            {
                case Label lbl:
                    lbl.ForeColor = c.Enabled ? txt : txtDisabled;
                    break;

                case TextBox tb:
                    tb.BackColor = c.Enabled ? back : backDisabled;
                    tb.ForeColor = c.Enabled ? txt : txtDisabled;
                    tb.BorderStyle = BorderStyle.FixedSingle;
                    break;

                case StatusStrip ss:
                    ss.BackColor = dark ? DarkBack : LightBack;
                    ss.ForeColor = dark ? DarkText : LightText;

                    foreach (ToolStripItem item in ss.Items)
                    {
                        item.ForeColor = dark ? DarkText : LightText;
                    }
                    break;

                case Button b:
                    b.BackColor = c.Enabled ? back : backDisabled;
                    b.ForeColor = c.Enabled ? txt : txtDisabled;
                    b.FlatStyle = FlatStyle.Flat;
                    break;

                case GroupBox g:
                    g.ForeColor = txt;
                    g.BackColor = Color.Transparent;
                    break;

                case CheckBox chk:
                    chk.ForeColor = c.Enabled ? txt : txtDisabled;
                    chk.BackColor = Color.Transparent;
                    break;

                case RadioButton rb:
                    rb.ForeColor = c.Enabled ? txt : txtDisabled;
                    rb.BackColor = Color.Transparent;
                    break;

                case NumericUpDown nud:
                    nud.BackColor = c.Enabled ? back : backDisabled;
                    nud.ForeColor = c.Enabled ? txt : txtDisabled;
                    nud.BorderStyle = BorderStyle.FixedSingle;

                    if (nud.Controls.Count > 0)
                    {
                        nud.Controls[0].BackColor = c.Enabled ? back : backDisabled;
                        nud.Controls[0].ForeColor = c.Enabled ? txt : txtDisabled;
                    }
                    break;

                case DataGridView dg:
                    ApplyGridTheme(dg, dark);
                    break;

                case ToolStrip ts:
                    ts.BackColor = dark ? DarkToolbar : LightToolbar;
                    ts.ForeColor = txt;
                    break;

                case LaunchManager.Controls.CustomComboBox cc:
                    cc.BackColor = back;
                    cc.ForeColor = txt;
                    break;
            }
        }


        // =======================================================
        //  DATA GRID VIEW
        // =======================================================
        private static void ApplyGridTheme(DataGridView g, bool dark)
        {
            Color back = dark ? DarkBack : LightBack;
            Color cell = dark ? DarkControl : Color.FromArgb(248, 248, 248);   
            Color txt = dark ? DarkText : LightText;

            Color selBack = dark ? Color.FromArgb(90, 90, 90) : Color.FromArgb(200, 200, 200);
            Color grid = dark ? Color.FromArgb(200, 215, 232) : Color.Silver;

            g.EnableHeadersVisualStyles = false;

            g.BackgroundColor = back;
            g.GridColor = grid;
            g.BorderStyle = BorderStyle.None;

            // Header colonne
            g.ColumnHeadersDefaultCellStyle.BackColor = cell;
            g.ColumnHeadersDefaultCellStyle.ForeColor = txt;
            g.ColumnHeadersDefaultCellStyle.SelectionBackColor = cell;
            g.ColumnHeadersDefaultCellStyle.SelectionForeColor = txt;

            // Celle normali
            g.DefaultCellStyle.BackColor = cell;
            g.DefaultCellStyle.ForeColor = txt;
            g.DefaultCellStyle.SelectionBackColor = selBack;
            g.DefaultCellStyle.SelectionForeColor = txt;

            // Righe normali
            g.RowsDefaultCellStyle.BackColor = cell;
            g.RowsDefaultCellStyle.ForeColor = txt;

            // Righe alternate
            g.AlternatingRowsDefaultCellStyle.BackColor = dark
                ? Color.FromArgb(78, 81, 84)
                : Color.WhiteSmoke;

            g.AlternatingRowsDefaultCellStyle.ForeColor = txt;

            // Header riga
            g.RowHeadersDefaultCellStyle.BackColor = cell;
            g.RowHeadersDefaultCellStyle.ForeColor = txt;
        }
    }
}
