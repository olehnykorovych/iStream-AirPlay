using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace IStream
{
    // App icon: an open circle that runs, in one line, into the stem of a lowercase "i", with the dot above.
    // Default: graphite iOS-style tile with a white mark. inverted: light tile with a dark mark
    // (used on the dark sidebar so the icon doesn't disappear into it).
    static class IconArt
    {
        public static Bitmap Draw(int size) { return Draw(size, false); }

        public static Bitmap Draw(int size, bool inverted)
        {
            var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.Clear(Color.Transparent);
                float s = size, u = s / 100f;   // drawn on a 100 x 100 grid

                Color top = inverted ? Color.FromArgb(0xFF, 0xFF, 0xFF) : Color.FromArgb(0x2B, 0x2B, 0x2E);
                Color bottom = inverted ? Color.FromArgb(0xE5, 0xE5, 0xEA) : Color.FromArgb(0x0A, 0x0A, 0x0B);
                Color glyph = inverted ? Color.FromArgb(0x11, 0x11, 0x11) : Color.White;

                var tile = new RectangleF(0, 0, s - 1, s - 1);
                using (var bg = RoundRect(tile, s * 0.225f))
                {
                    using (var br = new LinearGradientBrush(new PointF(0, 0), new PointF(0, s), top, bottom)) g.FillPath(br, bg);
                    // hairline edge: keeps the dark tile visible on a dark taskbar, the light one on a light background
                    Color edge = inverted ? Color.FromArgb(70, 0, 0, 0) : Color.FromArgb(60, 255, 255, 255);
                    using (var pen = new Pen(edge, Math.Max(1f, s / 64f))) g.DrawPath(pen, bg);
                }

                // thicker line at tray sizes so it stays readable
                float sw = (s <= 24 ? 11f : s <= 40 ? 9.5f : 8f) * u;
                // at tray sizes the dot sits higher and the stem is shorter, so the two don't merge into one blob
                float dotY = s <= 24 ? 35f : 42f, stemTop = s <= 24 ? 60f : 56f;
                const float r = 30f, a0 = 135f, a1 = 70f, stemX = 48f;
                float ex = 50 + r * (float)Math.Cos(a1 * Math.PI / 180), ey = 50 + r * (float)Math.Sin(a1 * Math.PI / 180);

                using (var path = new GraphicsPath())
                using (var pen = new Pen(glyph, sw))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    pen.LineJoin = LineJoin.Round;
                    // open ring from the bottom-left, round over the top, down to the bottom...
                    path.AddArc((50 - r) * u, (50 - r) * u, 2 * r * u, 2 * r * u, a0, 360f - (a0 - a1));
                    // ...then curling up into the stem of the "i"
                    path.AddBezier(ex * u, ey * u, (ex - 8) * u, (ey + 4) * u, stemX * u, (50 + r - 2) * u, stemX * u, (50 + r - 10) * u);
                    path.AddLine(stemX * u, (50 + r - 10) * u, stemX * u, stemTop * u);
                    g.DrawPath(pen, path);
                }
                float dot = sw * 0.72f;
                using (var br = new SolidBrush(glyph)) g.FillEllipse(br, stemX * u - dot, dotY * u - dot, dot * 2, dot * 2);
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
