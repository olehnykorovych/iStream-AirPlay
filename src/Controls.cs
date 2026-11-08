using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace IStream
{
    // ------------------------------------------------------------------ motyw: czarno-biały (ciemny) i biało-czarny (jasny)

    static class Theme
    {
        public static bool IsDark = true;
        public static Color Side, Bg, Card, CardHover, Field, FieldHover, Border, BorderSoft,
                            Text, Muted, Faint, Accent, AccentHover, AccentDown, OnAccent, AccentTint,
                            NavHover, NavSelected, Good, Warn, WarnTint, WarnBorder, Bad, MenuBg,
                            ToggleOff, KnobOff;

        public const int Light = 0, Dark = 1, Gray = 2;
        public static int Variant = Dark;

        static Theme() { Apply(true); }

        public static void Apply(bool dark) { Apply(dark ? Dark : Light); }

        public static void Apply(int variant)
        {
            Variant = variant;
            IsDark = variant != Light;   // gray uses dark title bar, scrollbars and menus
            if (variant == Gray)
            {
                // graphite: between white-on-black and black-on-white
                Side = C(0x262626); Bg = C(0x2E2E2E); Card = C(0x383838); CardHover = C(0x424242);
                Field = C(0x292929); FieldHover = C(0x333333); Border = C(0x555555); BorderSoft = C(0x474747);
                Text = C(0xF5F5F5); Muted = C(0xC2C2C2); Faint = C(0x929292);
                Accent = C(0xF5F5F5); AccentHover = C(0xDEDEDE); AccentDown = C(0xC4C4C4); OnAccent = C(0x1F1F1F);
                AccentTint = C(0x474747); NavHover = C(0x303030); NavSelected = C(0x3A3A3A);
                Good = C(0x4ADE80); Warn = C(0xFBBF24); Bad = C(0xF87171);
                WarnTint = C(0x383838); WarnBorder = C(0x5A5A5A);
                MenuBg = C(0x383838); ToggleOff = C(0x5C5C5C); KnobOff = C(0xE0E0E0);
            }
            else if (variant == Dark)
            {
                Side = C(0x000000); Bg = C(0x0A0A0A); Card = C(0x141414); CardHover = C(0x1F1F1F);
                Field = C(0x0A0A0A); FieldHover = C(0x171717); Border = C(0x333333); BorderSoft = C(0x242424);
                Text = C(0xFAFAFA); Muted = C(0xA3A3A3); Faint = C(0x6B6B6B);
                Accent = C(0xFFFFFF); AccentHover = C(0xE0E0E0); AccentDown = C(0xC7C7C7); OnAccent = C(0x000000);
                AccentTint = C(0x262626); NavHover = C(0x141414); NavSelected = C(0x1F1F1F);
                Good = C(0x4ADE80); Warn = C(0xFBBF24); Bad = C(0xF87171);
                WarnTint = C(0x141414); WarnBorder = C(0x3D3D3D);
                MenuBg = C(0x171717); ToggleOff = C(0x3A3A3A); KnobOff = C(0xD4D4D4);
            }
            else
            {
                Side = C(0xF4F4F4); Bg = C(0xFFFFFF); Card = C(0xFAFAFA); CardHover = C(0xF0F0F0);
                Field = C(0xFFFFFF); FieldHover = C(0xF5F5F5); Border = C(0xD6D6D6); BorderSoft = C(0xE8E8E8);
                Text = C(0x0A0A0A); Muted = C(0x525252); Faint = C(0x8A8A8A);
                Accent = C(0x0A0A0A); AccentHover = C(0x2B2B2B); AccentDown = C(0x404040); OnAccent = C(0xFFFFFF);
                AccentTint = C(0xEBEBEB); NavHover = C(0xEBEBEB); NavSelected = C(0xE2E2E2);
                Good = C(0x16A34A); Warn = C(0xB45309); Bad = C(0xDC2626);
                WarnTint = C(0xFAFAFA); WarnBorder = C(0xD6D6D6);
                MenuBg = C(0xFFFFFF); ToggleOff = C(0xD4D4D4); KnobOff = C(0xFFFFFF);
            }
        }

        public static readonly string SerifFamily = Pick(new[] { "Georgia", "Cambria", "Times New Roman" });
        public static readonly Font H1 = new Font(SerifFamily, 22f);
        public static readonly Font Brand = new Font(SerifFamily, 15f);
        public static readonly Font Section = new Font("Segoe UI Semibold", 10.5f);
        public static readonly Font Body = new Font("Segoe UI", 10f);
        public static readonly Font BodySemi = new Font("Segoe UI Semibold", 10f);
        public static readonly Font Small = new Font("Segoe UI", 9f);
        public static readonly Font Tiny = new Font("Segoe UI", 8.25f);

        static Color C(int rgb) { return Color.FromArgb((rgb >> 16) & 255, (rgb >> 8) & 255, rgb & 255); }

        static string Pick(string[] names)
        {
            foreach (string n in names)
            {
                try { using (var f = new FontFamily(n)) return n; }
                catch { }
            }
            return "Segoe UI";
        }

        public static int ColorRef(Color c) { return c.R | (c.G << 8) | (c.B << 16); }
    }

    static class Gfx
    {
        public static float Scale = 1f;

        public static int D(float v) { return (int)Math.Round(v * Scale); }

        public static GraphicsPath Round(RectangleF r, float rad)
        {
            var p = new GraphicsPath();
            rad = Math.Min(rad, Math.Min(r.Width, r.Height) / 2f);
            if (rad < 0.5f) { p.AddRectangle(r); return p; }
            float d = rad * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static void Fill(Graphics g, RectangleF r, float rad, Color c)
        {
            using (var p = Round(r, rad))
            using (var b = new SolidBrush(c)) g.FillPath(b, p);
        }

        public static void Stroke(Graphics g, RectangleF r, float rad, Color c, float w)
        {
            var rr = RectangleF.Inflate(r, -w / 2f, -w / 2f);
            using (var p = Round(rr, rad))
            using (var pen = new Pen(c, w)) g.DrawPath(pen, p);
        }

        public static void Prepare(Graphics g, Control c)
        {
            g.Clear(c.Parent != null ? c.Parent.BackColor : Theme.Bg);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        }
    }

    // ------------------------------------------------------------------ ikony (rysowane wektorowo)

    static class Icons
    {
        static Pen P(Color c, float w)
        {
            var p = new Pen(c, w);
            p.StartCap = LineCap.Round;
            p.EndCap = LineCap.Round;
            p.LineJoin = LineJoin.Round;
            return p;
        }

        static float W(RectangleF r) { return Math.Max(1.3f, r.Width * 0.085f); }

        public static void Display(Graphics g, RectangleF r, Color c)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var pen = P(c, W(r)))
            {
                var m = new RectangleF(r.X + r.Width * 0.06f, r.Y + r.Height * 0.12f, r.Width * 0.88f, r.Height * 0.6f);
                using (var p = Gfx.Round(m, r.Width * 0.1f)) g.DrawPath(pen, p);
                float cx = r.X + r.Width / 2f, by = r.Bottom - r.Height * 0.08f;
                g.DrawLine(pen, cx, m.Bottom, cx, by);
                g.DrawLine(pen, r.X + r.Width * 0.3f, by, r.X + r.Width * 0.7f, by);
            }
        }

        public static void Sliders(Graphics g, RectangleF r, Color c)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var pen = P(c, W(r)))
            using (var b = new SolidBrush(c))
            {
                float[] ys = { 0.22f, 0.5f, 0.78f };
                float[] ks = { 0.32f, 0.68f, 0.45f };
                for (int i = 0; i < 3; i++)
                {
                    float y = r.Y + r.Height * ys[i];
                    g.DrawLine(pen, r.X + r.Width * 0.08f, y, r.Right - r.Width * 0.08f, y);
                    float kx = r.X + r.Width * ks[i], kr = r.Width * 0.12f;
                    g.FillEllipse(b, kx - kr, y - kr, kr * 2, kr * 2);
                }
            }
        }

        public static void Help(Graphics g, RectangleF r, Color c)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float u = r.Width, cx = r.X + r.Width / 2f, cy = r.Y + r.Height / 2f;
            using (var pen = P(c, W(r)))
            {
                g.DrawEllipse(pen, RectangleF.Inflate(r, -u * 0.06f, -u * 0.06f));
                g.DrawArc(pen, cx - u * 0.14f, cy - u * 0.28f, u * 0.28f, u * 0.26f, 190, 240);
                g.DrawLine(pen, cx + u * 0.03f, cy - u * 0.03f, cx, cy + u * 0.07f);
            }
            using (var b = new SolidBrush(c))
            {
                float d = u * 0.11f;
                g.FillEllipse(b, cx - d / 2, r.Y + r.Height * 0.72f - d / 2, d, d);
            }
        }

        public static void Power(Graphics g, RectangleF r, Color c)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float u = r.Width;
            using (var pen = P(c, Math.Max(1.5f, u * 0.1f)))
            {
                var rr = RectangleF.Inflate(r, -u * 0.12f, -u * 0.12f);
                g.DrawArc(pen, rr, -55, 290);
                g.DrawLine(pen, r.X + u / 2f, r.Y + u * 0.04f, r.X + u / 2f, r.Y + u * 0.46f);
            }
        }

        public static void Expand(Graphics g, RectangleF r, Color c)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float u = r.Width, a = u * 0.1f, L = u * 0.3f;
            float l = r.X + a, t = r.Y + a, rt = r.Right - a, b = r.Bottom - a;
            using (var pen = P(c, Math.Max(1.5f, u * 0.1f)))
            {
                g.DrawLines(pen, new[] { new PointF(l, t + L), new PointF(l, t), new PointF(l + L, t) });
                g.DrawLines(pen, new[] { new PointF(rt - L, t), new PointF(rt, t), new PointF(rt, t + L) });
                g.DrawLines(pen, new[] { new PointF(rt, b - L), new PointF(rt, b), new PointF(rt - L, b) });
                g.DrawLines(pen, new[] { new PointF(l + L, b), new PointF(l, b), new PointF(l, b - L) });
            }
        }

        public static void Warning(Graphics g, RectangleF r, Color c)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float u = r.Width, cx = r.X + u / 2f;
            using (var pen = P(c, W(r)))
            {
                g.DrawPolygon(pen, new[]
                {
                    new PointF(cx, r.Y + u * 0.08f),
                    new PointF(r.Right - u * 0.05f, r.Bottom - u * 0.1f),
                    new PointF(r.X + u * 0.05f, r.Bottom - u * 0.1f)
                });
                g.DrawLine(pen, cx, r.Y + u * 0.38f, cx, r.Y + u * 0.6f);
            }
            using (var b = new SolidBrush(c))
            {
                float d = u * 0.11f;
                g.FillEllipse(b, cx - d / 2, r.Y + u * 0.72f - d / 2, d, d);
            }
        }

        public static void Info(Graphics g, RectangleF r, Color c)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float u = r.Width, cx = r.X + u / 2f;
            using (var pen = P(c, W(r)))
            {
                g.DrawEllipse(pen, RectangleF.Inflate(r, -u * 0.06f, -u * 0.06f));
                g.DrawLine(pen, cx, r.Y + u * 0.45f, cx, r.Y + u * 0.72f);
            }
            using (var b = new SolidBrush(c))
            {
                float d = u * 0.11f;
                g.FillEllipse(b, cx - d / 2, r.Y + u * 0.3f - d / 2, d, d);
            }
        }

        // Telefon obrócony o rot*90° (wyspa u góry pokazuje, gdzie jest góra) + strzałka kierunku obrotu.
        public static void Rotation(Graphics g, RectangleF r, int rot, Color fg, Color accent)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float s = Math.Min(r.Width, r.Height);
            float cx = r.X + r.Width / 2f, cy = r.Y + r.Height / 2f;
            if (rot == 1 || rot == 3) cy += s * 0.07f;   // strzałka tylko u góry — wyważ kompozycję
            float pw = s * 0.26f, ph = s * 0.44f;

            GraphicsState st = g.Save();
            g.TranslateTransform(cx, cy);
            g.RotateTransform(rot * 90f);
            using (var pen = P(fg, Math.Max(1.4f, s * 0.035f)))
            using (var path = Gfx.Round(new RectangleF(-pw / 2, -ph / 2, pw, ph), pw * 0.24f))
                g.DrawPath(pen, path);
            using (var b = new SolidBrush(fg))
            using (var isl = Gfx.Round(new RectangleF(-pw * 0.2f, -ph / 2 + ph * 0.09f, pw * 0.4f, ph * 0.075f), ph * 0.04f))
                g.FillPath(b, isl);
            g.Restore(st);

            if (rot == 0) return;
            float R = s * 0.36f;
            using (var pen = P(accent, Math.Max(1.5f, s * 0.04f)))
            {
                float start, sweep;
                if (rot == 1) { start = -150; sweep = 110; }        // zgodnie z ruchem wskazówek
                else if (rot == 3) { start = -30; sweep = -110; }   // przeciwnie
                else { start = 160; sweep = 220; }                  // pół obrotu górą
                g.DrawArc(pen, cx - R, cy - R, 2 * R, 2 * R, start, sweep);

                double end = (start + sweep) * Math.PI / 180.0;
                float ex = cx + R * (float)Math.Cos(end), ey = cy + R * (float)Math.Sin(end);
                float dir = sweep > 0 ? 1f : -1f;
                double tx = -Math.Sin(end) * dir, ty = Math.Cos(end) * dir;   // kierunek ruchu
                double back = Math.Atan2(-ty, -tx);
                float len = s * 0.105f;
                for (int k = -1; k <= 1; k += 2)
                {
                    double a = back + k * 0.62;
                    g.DrawLine(pen, ex, ey, ex + len * (float)Math.Cos(a), ey + len * (float)Math.Sin(a));
                }
            }
        }
    }

    // ------------------------------------------------------------------ kontrolki

    // A scrolling page that can be told not to move. WinForms scrolls a control into view whenever it gets focus,
    // which threw the settings page back to the top after a theme change rebuilt it. While Hold is set, focus
    // changes leave the scroll position alone; otherwise (Tab key) the normal scroll-into-view applies.
    sealed class ScrollPanel : Panel
    {
        public bool Hold;
        protected override Point ScrollToControl(Control activeControl)
        {
            return Hold ? DisplayRectangle.Location : base.ScrollToControl(activeControl);
        }
    }

    class CardPanel : Panel
    {
        Color fill = Theme.Card, stroke = Theme.BorderSoft;
        public float Radius = 12f;

        public CardPanel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = fill;
        }

        public void SetColors(Color f, Color s) { fill = f; stroke = s; BackColor = f; Invalidate(); }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Gfx.Prepare(e.Graphics, this);
            var r = new RectangleF(0, 0, Width - 1, Height - 1);
            Gfx.Fill(e.Graphics, r, Radius * Gfx.Scale, fill);
            if (stroke != Color.Empty) Gfx.Stroke(e.Graphics, r, Radius * Gfx.Scale, stroke, 1f);
        }
    }

    class IconBox : Control
    {
        public Action<Graphics, RectangleF, Color> Painter;
        public Color IconColor = Theme.Text;

        public IconBox()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Gfx.Prepare(e.Graphics, this);
            if (Painter != null) Painter(e.Graphics, new RectangleF(1, 1, Width - 2, Height - 2), IconColor);
        }
    }

    sealed class StatusDot : Control
    {
        Color color = Color.Gray;

        public StatusDot()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        public Color DotColor
        {
            get { return color; }
            set { if (color != value) { color = value; Invalidate(); } }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Gfx.Prepare(e.Graphics, this);
            using (var b = new SolidBrush(color)) e.Graphics.FillEllipse(b, 0.5f, 0.5f, Width - 2, Height - 2);
        }
    }

    enum BtnKind { Primary, Secondary, Nav, Tile, Chip, Warn }

    sealed class FlatButton : Control
    {
        public readonly BtnKind Kind;
        public Action<Graphics, RectangleF, Color> Glyph;
        bool selected, hover, down;

        public FlatButton(BtnKind kind)
        {
            Kind = kind;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.Selectable | ControlStyles.StandardClick, true);
            Cursor = Cursors.Hand;
            Font = kind == BtnKind.Chip ? Theme.Small : kind == BtnKind.Nav ? Theme.Body : Theme.BodySemi;
        }

        public bool Selected
        {
            get { return selected; }
            set { if (selected != value) { selected = value; Invalidate(); } }
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; down = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) { down = true; Invalidate(); } base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { down = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }
        protected override void OnTextChanged(EventArgs e) { Invalidate(); base.OnTextChanged(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
        protected override bool IsInputKey(Keys k) { return k == Keys.Enter || k == Keys.Space || base.IsInputKey(k); }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter) { e.Handled = true; OnClick(EventArgs.Empty); }
            base.OnKeyDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Gfx.Prepare(g, this);
            float s = Gfx.Scale;
            Color fill = Color.Empty, stroke = Color.Empty, fore = Theme.Text;
            float strokeW = 1f;

            switch (Kind)
            {
                case BtnKind.Primary:
                    fill = !Enabled ? Theme.Card : down ? Theme.AccentDown : hover ? Theme.AccentHover : Theme.Accent;
                    fore = Enabled ? Theme.OnAccent : Theme.Faint;
                    break;
                case BtnKind.Warn:
                    fill = down ? ControlPaint.Dark(Theme.Warn, 0.05f) : hover ? ControlPaint.Light(Theme.Warn, 0.25f) : Theme.Warn;
                    fore = Theme.Side;
                    break;
                case BtnKind.Secondary:
                    fill = !Enabled ? Theme.Bg : down ? Theme.Card : hover ? Theme.CardHover : Theme.Card;
                    stroke = Enabled ? Theme.Border : Theme.BorderSoft;
                    fore = Enabled ? Theme.Text : Theme.Faint;
                    break;
                case BtnKind.Nav:
                    fill = selected ? Theme.NavSelected : hover ? Theme.NavHover : Color.Empty;
                    fore = selected || hover ? Theme.Text : Theme.Muted;
                    break;
                case BtnKind.Tile:
                    fill = selected ? Theme.AccentTint : hover ? Theme.FieldHover : Theme.Field;
                    stroke = selected ? Theme.Accent : Theme.BorderSoft;
                    strokeW = selected ? Math.Max(1.5f, 1.6f * s) : 1f;
                    break;
                case BtnKind.Chip:
                    fill = hover ? Theme.CardHover : Theme.Card;
                    stroke = Theme.BorderSoft;
                    fore = hover ? Theme.Text : Theme.Muted;
                    break;
            }

            var r = new RectangleF(0, 0, Width - 1, Height - 1);
            float rad = Kind == BtnKind.Chip ? r.Height / 2f : (Kind == BtnKind.Tile ? 12f : 9f) * s;
            if (fill != Color.Empty) Gfx.Fill(g, r, rad, fill);
            if (stroke != Color.Empty) Gfx.Stroke(g, r, rad, stroke, strokeW);
            if (Focused && ShowFocusCues) Gfx.Stroke(g, RectangleF.Inflate(r, -2 * s, -2 * s), rad, Theme.Accent, 1.5f);

            if (Kind == BtnKind.Tile)
            {
                if (Glyph != null) Glyph(g, new RectangleF(0, 0, Width, Height), fore);
                return;
            }

            var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;
            Rectangle tr;
            if (Kind == BtnKind.Nav)
            {
                int pad = (int)(12 * s);
                if (Glyph != null)
                {
                    float isz = 17 * s;
                    Glyph(g, new RectangleF(pad, (Height - isz) / 2f, isz, isz), fore);
                    pad += (int)(isz + 11 * s);
                }
                tr = new Rectangle(pad, 0, Width - pad - (int)(8 * s), Height);
                flags |= TextFormatFlags.Left;
            }
            else if (Glyph != null && !string.IsNullOrEmpty(Text))
            {
                float isz = 16 * s, gap = 9 * s;
                Size ts = TextRenderer.MeasureText(g, Text, Font, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
                float x0 = (Width - (isz + gap + ts.Width)) / 2f;
                Glyph(g, new RectangleF(x0, (Height - isz) / 2f, isz, isz), fore);
                tr = new Rectangle((int)(x0 + isz + gap), 0, ts.Width + 6, Height);
                flags |= TextFormatFlags.Left | TextFormatFlags.NoPadding;
            }
            else
            {
                tr = ClientRectangle;
                flags |= TextFormatFlags.HorizontalCenter;
            }
            TextRenderer.DrawText(g, Text, Font, tr, fore, flags);
        }
    }

    sealed class ToggleSwitch : Control
    {
        bool on, hover;
        public event EventHandler CheckedChanged;

        public ToggleSwitch()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.Selectable | ControlStyles.StandardClick, true);
            Cursor = Cursors.Hand;
        }

        public bool Checked
        {
            get { return on; }
            set
            {
                if (on == value) return;
                on = value;
                Invalidate();
                EventHandler h = CheckedChanged;
                if (h != null) h(this, EventArgs.Empty);
            }
        }

        public void SetSilently(bool v) { on = v; Invalidate(); }

        protected override void OnClick(EventArgs e) { Focus(); Checked = !Checked; base.OnClick(e); }
        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
        protected override bool IsInputKey(Keys k) { return k == Keys.Space || base.IsInputKey(k); }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space) { e.Handled = true; Checked = !Checked; }
            base.OnKeyDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Gfx.Prepare(g, this);
            float s = Gfx.Scale;
            var r = new RectangleF(0, 0, Width - 1, Height - 1);
            Color track = on ? (hover ? Theme.AccentHover : Theme.Accent) : (hover ? ControlPaint.Light(Theme.ToggleOff, 0.15f) : Theme.ToggleOff);
            Gfx.Fill(g, r, r.Height / 2f, track);
            if (Focused && ShowFocusCues) Gfx.Stroke(g, RectangleF.Inflate(r, 1, 1), r.Height / 2f, Theme.Text, 1f);
            float inset = 3 * s, d = r.Height - inset * 2;
            float x = on ? r.Right - inset - d : inset;
            using (var b = new SolidBrush(on ? Theme.OnAccent : Theme.KnobOff)) g.FillEllipse(b, x, inset, d, d);
        }
    }

    sealed class SelectBox : Control
    {
        readonly List<string> items = new List<string>();
        int index = -1;
        bool hover;
        public event EventHandler SelectedIndexChanged;

        public SelectBox()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.Selectable | ControlStyles.StandardClick, true);
            Cursor = Cursors.Hand;
            Font = Theme.Body;
        }

        public int SelectedIndex { get { return index; } }

        public void SetItems(IList<string> list, int selected)
        {
            items.Clear();
            items.AddRange(list);
            index = selected >= 0 && selected < items.Count ? selected : 0;
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
        protected override void OnClick(EventArgs e) { base.OnClick(e); Focus(); ShowMenu(); }
        protected override bool IsInputKey(Keys k) { return k == Keys.Space || k == Keys.Enter || k == Keys.Up || k == Keys.Down || base.IsInputKey(k); }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter) { e.Handled = true; ShowMenu(); }
            else if (e.KeyCode == Keys.Down && index < items.Count - 1) { e.Handled = true; Pick(index + 1); }
            else if (e.KeyCode == Keys.Up && index > 0) { e.Handled = true; Pick(index - 1); }
            base.OnKeyDown(e);
        }

        void ShowMenu()
        {
            if (items.Count == 0) return;
            var menu = new ContextMenuStrip();
            menu.Renderer = new DarkMenuRenderer();
            menu.ShowImageMargin = false;
            menu.ShowCheckMargin = true;
            menu.Font = Font;
            menu.BackColor = Theme.MenuBg;
            menu.ForeColor = Theme.Text;
            menu.Padding = new Padding(3, 4, 3, 4);
            for (int i = 0; i < items.Count; i++)
            {
                int idx = i;
                var it = new ToolStripMenuItem(items[i]);
                it.Checked = i == index;
                it.ForeColor = Theme.Text;
                it.Padding = new Padding(0, Gfx.D(4), 0, Gfx.D(4));
                it.Click += delegate { Pick(idx); };
                menu.Items.Add(it);
            }
            menu.MinimumSize = new Size(Width, 0);
            menu.Opened += delegate { Native.RoundCorners(menu.Handle); };
            menu.Closed += delegate
            {
                try
                {
                    if (!IsDisposed && IsHandleCreated) BeginInvoke(new Action(menu.Dispose));
                    else menu.Dispose();
                }
                catch { }
            };
            menu.Show(this, new Point(0, Height + Gfx.D(4)));
        }

        void Pick(int i)
        {
            if (i == index) return;
            index = i;
            Invalidate();
            EventHandler h = SelectedIndexChanged;
            if (h != null) h(this, EventArgs.Empty);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Gfx.Prepare(g, this);
            float s = Gfx.Scale;
            var r = new RectangleF(0, 0, Width - 1, Height - 1);
            Gfx.Fill(g, r, 8 * s, hover ? Theme.FieldHover : Theme.Field);
            Gfx.Stroke(g, r, 8 * s, Focused ? Theme.Accent : Theme.Border, 1f);

            string text = index >= 0 && index < items.Count ? items[index] : "";
            var tr = new Rectangle((int)(12 * s), 0, Width - (int)(40 * s), Height);
            TextRenderer.DrawText(g, text, Font, tr, Theme.Text,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);

            float cx = Width - 18 * s, cy = Height / 2f, a = 4 * s;
            using (var pen = new Pen(Theme.Muted, Math.Max(1.3f, 1.5f * s)))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                g.DrawLines(pen, new[] { new PointF(cx - a, cy - a / 2), new PointF(cx, cy + a / 2), new PointF(cx + a, cy - a / 2) });
            }
        }
    }

    sealed class TextField : Control
    {
        public readonly TextBox Box = new TextBox();

        public TextField()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Box.BorderStyle = BorderStyle.None;
            Box.BackColor = Theme.Field;
            Box.ForeColor = Theme.Text;
            Box.Font = Theme.Body;
            Controls.Add(Box);
            Box.GotFocus += delegate { Invalidate(); };
            Box.LostFocus += delegate { Invalidate(); };
            Cursor = Cursors.IBeam;
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            int pad = Gfx.D(12);
            Box.SetBounds(pad, (Height - Box.Height) / 2, Width - 2 * pad, Box.Height);
        }

        protected override void OnMouseDown(MouseEventArgs e) { Box.Focus(); base.OnMouseDown(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Gfx.Prepare(e.Graphics, this);
            var r = new RectangleF(0, 0, Width - 1, Height - 1);
            Gfx.Fill(e.Graphics, r, 8 * Gfx.Scale, Theme.Field);
            Gfx.Stroke(e.Graphics, r, 8 * Gfx.Scale, Box.Focused ? Theme.Accent : Theme.Border, 1f);
        }
    }

    sealed class DarkMenuRenderer : ToolStripProfessionalRenderer
    {
        public DarkMenuRenderer() : base(new DarkColors()) { RoundedEdges = false; }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Enabled ? Theme.Text : Theme.Faint;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (!e.Item.Selected) return;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Gfx.Fill(e.Graphics, new RectangleF(2, 0, e.Item.Width - 4, e.Item.Height - 1), 6 * Gfx.Scale, Theme.CardHover);
        }

        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = e.ImageRectangle;
            using (var pen = new Pen(Theme.Accent, Math.Max(1.6f, 2f * Gfx.Scale)))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;
                g.DrawLines(pen, new[]
                {
                    new PointF(r.Left + r.Width * 0.2f, r.Top + r.Height * 0.52f),
                    new PointF(r.Left + r.Width * 0.42f, r.Top + r.Height * 0.74f),
                    new PointF(r.Left + r.Width * 0.8f, r.Top + r.Height * 0.3f)
                });
            }
        }
    }

    sealed class DarkColors : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground { get { return Theme.MenuBg; } }
        public override Color ImageMarginGradientBegin { get { return Theme.MenuBg; } }
        public override Color ImageMarginGradientMiddle { get { return Theme.MenuBg; } }
        public override Color ImageMarginGradientEnd { get { return Theme.MenuBg; } }
        public override Color MenuBorder { get { return Theme.Border; } }
        public override Color MenuItemBorder { get { return Theme.CardHover; } }
        public override Color MenuItemSelected { get { return Theme.CardHover; } }
        public override Color CheckBackground { get { return Theme.MenuBg; } }
        public override Color CheckSelectedBackground { get { return Theme.CardHover; } }
        public override Color CheckPressedBackground { get { return Theme.CardHover; } }
        public override Color SeparatorDark { get { return Theme.Border; } }
        public override Color SeparatorLight { get { return Theme.MenuBg; } }
    }
}
