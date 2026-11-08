using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace IStream
{
    // Ikona aplikacji: telefon + fale transmisji. Domyślnie czarny kwadrat z białym znakiem;
    // inverted = biały kwadrat z czarnym znakiem (na czarnym pasku bocznym).
    static class IconArt
    {
        public static Bitmap Draw(int size) { return Draw(size, false); }

        public static Bitmap Draw(int size, bool inverted)
        {
            var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                float s = size;
                Color back = inverted ? Color.FromArgb(0xFA, 0xFA, 0xFA) : Color.FromArgb(0x0A, 0x0A, 0x0A);
                Color glyph = inverted ? Color.FromArgb(0x0A, 0x0A, 0x0A) : Color.White;

                using (var bg = RoundRect(new RectangleF(0, 0, s - 1, s - 1), s * 0.22f))
                {
                    using (var br = new SolidBrush(back)) g.FillPath(br, bg);
                    if (!inverted)   // delikatna obwódka, żeby czarna ikona była widoczna na ciemnym pasku zadań
                        using (var pen = new Pen(Color.FromArgb(90, 255, 255, 255), Math.Max(1f, s / 48f))) g.DrawPath(pen, bg);
                }

                float stroke = Math.Max(1.2f, s * 0.065f);
                using (var pen = new Pen(glyph, stroke))
                {
                    pen.LineJoin = LineJoin.Round;
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;

                    var phone = new RectangleF(s * 0.20f, s * 0.17f, s * 0.31f, s * 0.66f);
                    using (var p = RoundRect(phone, s * 0.07f)) g.DrawPath(pen, p);
                    g.DrawLine(pen, s * 0.31f, s * 0.27f, s * 0.40f, s * 0.27f);

                    float cx = s * 0.52f, cy = s * 0.50f;
                    foreach (float r in new[] { s * 0.15f, s * 0.27f })
                        g.DrawArc(pen, cx - r, cy - r, r * 2, r * 2, -42, 84);
                }
            }
            return bmp;
        }

        static GraphicsPath RoundRect(RectangleF r, float radius)
        {
            float d = radius * 2;
            var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static void SaveIco(string path, int[] sizes)
        {
            var pngs = new List<byte[]>();
            foreach (int sz in sizes)
                using (var b = Draw(sz))
                using (var ms = new MemoryStream())
                {
                    b.Save(ms, ImageFormat.Png);
                    pngs.Add(ms.ToArray());
                }

            using (var fs = File.Create(path))
            using (var w = new BinaryWriter(fs))
            {
                w.Write((short)0);
                w.Write((short)1);
                w.Write((short)sizes.Length);
                int offset = 6 + 16 * sizes.Length;
                for (int i = 0; i < sizes.Length; i++)
                {
                    byte dim = (byte)(sizes[i] >= 256 ? 0 : sizes[i]);
                    w.Write(dim);
                    w.Write(dim);
                    w.Write((byte)0);
                    w.Write((byte)0);
                    w.Write((short)1);
                    w.Write((short)32);
                    w.Write(pngs[i].Length);
                    w.Write(offset);
                    offset += pngs[i].Length;
                }
                foreach (var png in pngs) w.Write(png);
            }
        }
    }
}
