using System.Drawing;
using System.Drawing.Drawing2D;

namespace PcToolkit
{
    internal static class FlagIcons
    {
        public static void DrawUs(Graphics g, Rectangle r)
        {
            SmoothingMode old = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (Brush red = new SolidBrush(Color.FromArgb(178, 34, 52)))
                g.FillRectangle(red, r);

            using (Brush white = new SolidBrush(Color.White))
            {
                int stripeH = r.Height / 7;
                for (int i = 1; i < 7; i += 2)
                    g.FillRectangle(white, r.Left, r.Top + i * stripeH, r.Width, stripeH);
            }

            Rectangle canton = new Rectangle(r.Left, r.Top, (int)(r.Width * 0.42), (int)(r.Height * 0.55));
            using (Brush blue = new SolidBrush(Color.FromArgb(60, 59, 110)))
                g.FillRectangle(blue, canton);

            g.SmoothingMode = old;
        }

        public static void DrawBr(Graphics g, Rectangle r)
        {
            SmoothingMode old = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (Brush green = new SolidBrush(Color.FromArgb(0, 151, 57)))
                g.FillRectangle(green, r);

            Point[] diamond = new Point[]
            {
                new Point(r.Left + r.Width / 2, r.Top + 1),
                new Point(r.Right - 1, r.Top + r.Height / 2),
                new Point(r.Left + r.Width / 2, r.Bottom - 1),
                new Point(r.Left + 1, r.Top + r.Height / 2),
            };
            using (Brush yellow = new SolidBrush(Color.FromArgb(254, 209, 0)))
                g.FillPolygon(yellow, diamond);

            int d = r.Height / 2;
            Rectangle circle = new Rectangle(r.Left + (r.Width - d) / 2, r.Top + (r.Height - d) / 2, d, d);
            using (Brush blue = new SolidBrush(Color.FromArgb(0, 39, 118)))
                g.FillEllipse(blue, circle);

            g.SmoothingMode = old;
        }
    }
}
