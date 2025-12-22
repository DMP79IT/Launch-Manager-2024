using System;
using System.Drawing;
using System.Windows.Forms;

namespace LaunchManager.Controls
{
    public class CustomComboBox : ComboBox
    {
        public CustomComboBox()
        {
            DrawMode = DrawMode.OwnerDrawFixed;
            DropDownStyle = ComboBoxStyle.DropDownList;

            // 🔥 Aggiungere QUI per eliminare il flicker
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint, true);

            UpdateStyles();
        }
        protected override void WndProc(ref Message m)
        {
            const int WM_PAINT = 0xF;

            if (m.Msg == WM_PAINT)
            {
                base.WndProc(ref m);
                DrawCustom();
                return;
            }

            base.WndProc(ref m);
        }
        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0)
                return;

            bool dark = ThemeManager.CurrentTheme == ThemeManager.ThemeMode.Dark;

            Color back = dark ? ThemeManager.DarkControl : ThemeManager.LightControl;
            Color text = dark ? ThemeManager.DarkText : ThemeManager.LightText;
            Color backSel = dark ? Color.FromArgb(90, 90, 90) : Color.FromArgb(200, 200, 200);

            // Sfondo dell'elemento
            using (Brush b = ((e.State & DrawItemState.Selected) != 0)
                ? new SolidBrush(backSel)
                : new SolidBrush(back))
            {
                e.Graphics.FillRectangle(b, e.Bounds);
            }

            // Testo dell'elemento (centrato verticalmente)
            StringFormat sf = new StringFormat()
            {
                LineAlignment = StringAlignment.Center, // << centratura verticale
                Alignment = StringAlignment.Near        // << sinistra
            };

            using (Brush tb = new SolidBrush(text))
            {
                Rectangle textRect = new Rectangle(
                    e.Bounds.X + 5,          // padding sinistro
                    e.Bounds.Y,              // niente offset verticale
                    e.Bounds.Width - 5,
                    e.Bounds.Height
                );

                e.Graphics.DrawString(
                    Items[e.Index].ToString(),
                    Font,
                    tb,
                    textRect,
                    sf
                );
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
        }

        private void DrawCustom()
        {
            using (Graphics g = CreateGraphics())
            {
                bool dark = ThemeManager.CurrentTheme == ThemeManager.ThemeMode.Dark;

                Color back = dark ? ThemeManager.DarkControl : ThemeManager.LightControl;
                Color border = dark ? Color.FromArgb(100, 100, 100) : Color.Gray;
                Color text = dark ? ThemeManager.DarkText : ThemeManager.LightText;
                Color arrow = dark ? Color.FromArgb(200, 200, 200) : Color.FromArgb(80, 80, 80);

                // Sfondo
                g.FillRectangle(new SolidBrush(back), ClientRectangle);

                // Testo selezionato
                if (SelectedIndex >= 0)
                {
                    var sf = new StringFormat
                    {
                        LineAlignment = StringAlignment.Center,
                        Alignment = StringAlignment.Center
                    };

                    g.DrawString(
                        Items[SelectedIndex].ToString(),
                        Font,
                        new SolidBrush(text),
                        new RectangleF(0, 0, Width, Height),
                        sf
                    );
                }

                // Freccia ▼
                int x = Width - 18;
                int y = Height / 2 - 2;

                Point[] triangle = new Point[]
                {
            new Point(x, y),
            new Point(x + 10, y),
            new Point(x + 5, y + 5)
                };

                g.FillPolygon(new SolidBrush(arrow), triangle);

                // Bordo
                g.DrawRectangle(new Pen(border, 1), 0, 0, Width - 1, Height - 1);
            }
        }

    }
}
