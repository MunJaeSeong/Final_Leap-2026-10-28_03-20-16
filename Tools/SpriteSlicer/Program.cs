using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

internal static class Program
{
    private static int W, H;
    private static byte[] Px;

    private static int Idx(int x, int y) { return (y * W + x) * 4; }

    private static void Main(string[] args)
    {
        string cfg = args.Length > 0 ? args[0] : "slice_paths.txt";
        var lines = File.ReadAllLines(cfg, Encoding.UTF8);
        string knight = lines[0];
        string owl = lines[1];
        string tiles = lines[2];
        string title = lines[3];
        string bg = lines[4];
        string outDir = lines[5];

        if (Directory.Exists(outDir)) Directory.Delete(outDir, true);
        Directory.CreateDirectory(outDir);

        Console.WriteLine("KNIGHT");
        SliceKnight(knight, Path.Combine(outDir, "Knight"), Path.Combine(outDir, "knight_preview.png"));
        Console.WriteLine("OWL");
        SliceAlphaSheet(owl, Path.Combine(outDir, "Owl"), Path.Combine(outDir, "owl_preview.png"), 24, 2, 70);
        Console.WriteLine("TILES");
        SliceAlphaSheet(tiles, Path.Combine(outDir, "Tiles"), Path.Combine(outDir, "tiles_preview.png"), 28, 0, 50);
        File.Copy(title, Path.Combine(outDir, "title.png"), true);
        File.Copy(bg, Path.Combine(outDir, "background.png"), true);
        Console.WriteLine("DONE");
    }

    private static void Load(string path)
    {
        using (var src = new Bitmap(path))
        {
            W = src.Width;
            H = src.Height;
            var rect = new Rectangle(0, 0, W, H);
            using (var bmp = new Bitmap(W, H, PixelFormat.Format32bppArgb))
            {
                using (var g = Graphics.FromImage(bmp))
                    g.DrawImage(src, rect);
                var data = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                Px = new byte[Math.Abs(data.Stride) * H];
                Marshal.Copy(data.Scan0, Px, 0, Px.Length);
                bmp.UnlockBits(data);
            }
        }
    }

    private static bool IsChecker(int x, int y)
    {
        int i = Idx(x, y);
        int b = Px[i], g = Px[i + 1], r = Px[i + 2];
        int max = Math.Max(r, Math.Max(g, b));
        int min = Math.Min(r, Math.Min(g, b));
        return (max - min) < 28 && min >= 186;
    }

    private static void SliceKnight(string path, string dir, string previewPath)
    {
        Load(path);
        var bg = new bool[W * H];
        var qx = new int[W * H];
        var qy = new int[W * H];
        int qs = 0, qe = 0;
        Action<int, int> push = (x, y) =>
        {
            if (x < 0 || y < 0 || x >= W || y >= H) return;
            int id = y * W + x;
            if (bg[id] || !IsChecker(x, y)) return;
            bg[id] = true;
            qx[qe] = x;
            qy[qe] = y;
            qe++;
        };
        for (int x = 0; x < W; x++) { push(x, 0); push(x, H - 1); }
        for (int y = 0; y < H; y++) { push(0, y); push(W - 1, y); }
        while (qs < qe)
        {
            int x = qx[qs], y = qy[qs];
            qs++;
            push(x + 1, y); push(x - 1, y); push(x, y + 1); push(x, y - 1);
        }

        for (int y = 0; y < H; y++)
        for (int x = 0; x < W; x++)
        {
            int i = Idx(x, y);
            if (bg[y * W + x]) Px[i + 3] = 0;
            else Px[i + 3] = 255;
        }

        var rects = Components(40, 2);
        rects.RemoveAll(r => r.Height <= 68);
        DumpAndSave(rects, dir, previewPath, "k", 8);
    }

    private static void SliceAlphaSheet(string path, string dir, string previewPath, int alphaMin, int mergeGap, int minArea)
    {
        Load(path);
        for (int y = 0; y < H; y++)
        for (int x = 0; x < W; x++)
        {
            int i = Idx(x, y);
            if (Px[i + 3] < alphaMin) Px[i + 3] = 0;
        }
        var rects = Components(minArea, mergeGap);
        int splitAt = dir.EndsWith("Tiles") ? 220 : 200;
        rects = SplitWide(rects, splitAt);
        DumpAndSave(rects, dir, previewPath, "s", 4);
    }

    private struct RectI
    {
        public int X, Y, R, B;
        public int Width { get { return R - X + 1; } }
        public int Height { get { return B - Y + 1; } }
        public int Area { get { return Width * Height; } }
        public int Cx { get { return (X + R) / 2; } }
        public int Cy { get { return (Y + B) / 2; } }
    }

    private static List<RectI> Components(int minArea, int mergeGap)
    {
        var seen = new bool[W * H];
        var raw = new List<RectI>();
        var stack = new Stack<int>();
        for (int y = 0; y < H; y++)
        for (int x = 0; x < W; x++)
        {
            int start = y * W + x;
            if (seen[start] || Px[Idx(x, y) + 3] == 0) continue;
            int minx = x, maxx = x, miny = y, maxy = y, area = 0;
            stack.Push(start);
            seen[start] = true;
            while (stack.Count > 0)
            {
                int id = stack.Pop();
                int cx = id % W;
                int cy = id / W;
                area++;
                if (cx < minx) minx = cx;
                if (cy < miny) miny = cy;
                if (cx > maxx) maxx = cx;
                if (cy > maxy) maxy = cy;
                Try(cx + 1, cy); Try(cx - 1, cy); Try(cx, cy + 1); Try(cx, cy - 1);
            }
            if (area >= minArea)
                raw.Add(new RectI { X = minx, Y = miny, R = maxx, B = maxy });
        }

        void Try(int x, int y)
        {
            if (x < 0 || y < 0 || x >= W || y >= H) return;
            int id = y * W + x;
            if (seen[id] || Px[Idx(x, y) + 3] == 0) return;
            seen[id] = true;
            stack.Push(id);
        }

        bool changed = true;
        while (changed)
        {
            changed = false;
            for (int i = 0; i < raw.Count; i++)
            for (int j = i + 1; j < raw.Count; j++)
            {
                if (!Close(raw[i], raw[j], mergeGap)) continue;
                var a = raw[i];
                var b = raw[j];
                raw[i] = new RectI
                {
                    X = Math.Min(a.X, b.X),
                    Y = Math.Min(a.Y, b.Y),
                    R = Math.Max(a.R, b.R),
                    B = Math.Max(a.B, b.B)
                };
                raw.RemoveAt(j);
                changed = true;
                break;
            }
            if (changed) break;
        }
        // The loop above only merges once per outer pass because of break. Repeat properly.
        changed = true;
        var list = raw;
        while (changed)
        {
            changed = false;
            for (int i = 0; i < list.Count && !changed; i++)
            for (int j = i + 1; j < list.Count; j++)
            {
                if (!Close(list[i], list[j], mergeGap)) continue;
                var a = list[i];
                var b = list[j];
                list[i] = new RectI
                {
                    X = Math.Min(a.X, b.X),
                    Y = Math.Min(a.Y, b.Y),
                    R = Math.Max(a.R, b.R),
                    B = Math.Max(a.B, b.B)
                };
                list.RemoveAt(j);
                changed = true;
                break;
            }
        }
        return list;
    }

    private static List<RectI> SplitWide(List<RectI> rects, int maxWidth)
    {
        var result = new List<RectI>();
        foreach (var r in rects)
        {
            if (r.Width < maxWidth)
            {
                result.Add(r);
                continue;
            }
            int[] col = new int[r.Width];
            int peak = 0;
            for (int x = 0; x < r.Width; x++)
            {
                int c = 0;
                for (int y = r.Y; y <= r.B; y++)
                    if (Px[Idx(r.X + x, y) + 3] > 90) c++;
                col[x] = c;
                if (c > peak) peak = c;
            }
            int valley = Math.Max(6, peak / 8);
            var cuts = new List<int> { 0 };
            int run = 0;
            for (int x = 0; x < r.Width; x++)
            {
                if (col[x] <= valley) run++;
                else
                {
                    if (run >= 8)
                    {
                        int at = x - run / 2;
                        if (at - cuts[cuts.Count - 1] > 36)
                            cuts.Add(at);
                    }
                    run = 0;
                }
            }
            cuts.Add(r.Width);
            if (cuts.Count == 2)
            {
                result.Add(r);
                continue;
            }
            for (int i = 0; i < cuts.Count - 1; i++)
            {
                int x0 = r.X + cuts[i];
                int x1 = r.X + cuts[i + 1] - 1;
                if (x1 - x0 < 24) continue;
                int minY = r.B, maxY = r.Y;
                bool any = false;
                for (int y = r.Y; y <= r.B; y++)
                for (int x = x0; x <= x1; x++)
                {
                    if (Px[Idx(x, y) + 3] == 0) continue;
                    any = true;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
                if (!any || maxY - minY < 12) continue;
                result.Add(new RectI { X = x0, Y = minY, R = x1, B = maxY });
            }
        }
        return result;
    }

    private static bool Close(RectI a, RectI b, int gap)
    {
        int dx = 0;
        if (a.R < b.X) dx = b.X - a.R;
        else if (b.R < a.X) dx = a.X - b.R;
        int dy = 0;
        if (a.B < b.Y) dy = b.Y - a.B;
        else if (b.B < a.Y) dy = a.Y - b.B;
        if (dx > gap || dy > gap) return false;
        int overlapY = Math.Min(a.B, b.B) - Math.Max(a.Y, b.Y);
        int overlapX = Math.Min(a.R, b.R) - Math.Max(a.X, b.X);
        return overlapY > 8 || overlapX > 8 || (dx < gap / 2 && dy < gap / 2);
    }

    private static void DumpAndSave(List<RectI> rects, string dir, string previewPath, string prefix, int pad)
    {
        rects.Sort((a, b) =>
        {
            int rowA = a.Cy / 40;
            int rowB = b.Cy / 40;
            int c = rowA.CompareTo(rowB);
            if (c != 0) return c;
            return a.X.CompareTo(b.X);
        });

        var rows = new List<List<RectI>>();
        var ordered = new List<RectI>(rects);
        ordered.Sort((a, b) => a.Cy.CompareTo(b.Cy));
        foreach (var r in ordered)
        {
            int best = -1;
            int bestDist = int.MaxValue;
            for (int i = 0; i < rows.Count; i++)
            {
                int d = Math.Abs(rows[i][0].Cy - r.Cy);
                if (d < bestDist) { bestDist = d; best = i; }
            }
            if (best < 0 || bestDist > 48)
                rows.Add(new List<RectI> { r });
            else
                rows[best].Add(r);
        }
        foreach (var row in rows) row.Sort((a, b) => a.X.CompareTo(b.X));
        rows.Sort((a, b) => a[0].Y.CompareTo(b[0].Y));

        Directory.CreateDirectory(dir);
        int index = 0;
        var sb = new StringBuilder();
        for (int ri = 0; ri < rows.Count; ri++)
        {
            sb.AppendLine("ROW " + ri + " count=" + rows[ri].Count);
            int ci = 0;
            foreach (var r in rows[ri])
            {
                int x0 = Math.Max(0, r.X - pad);
                int y0 = Math.Max(0, r.Y - pad);
                int x1 = Math.Min(W - 1, r.R + pad);
                int y1 = Math.Min(H - 1, r.B + pad);
                int rw = x1 - x0 + 1;
                int rh = y1 - y0 + 1;
                string name = prefix + "_r" + ri.ToString("00") + "_" + ci.ToString("00") + ".png";
                SaveCrop(x0, y0, rw, rh, Path.Combine(dir, name));
                sb.AppendLine("  " + ci + " " + rw + "x" + rh + " @ " + r.X + "," + r.Y);
                ci++;
                index++;
            }
        }
        File.WriteAllText(Path.Combine(dir, "_layout.txt"), sb.ToString(), Encoding.UTF8);
        Console.WriteLine(sb.ToString());

        using (var preview = new Bitmap(W / 2, H / 2, PixelFormat.Format24bppRgb))
        using (var g = Graphics.FromImage(preview))
        {
            g.Clear(Color.FromArgb(12, 20, 28));
            g.ScaleTransform(0.5f, 0.5f);
            using (var src = BytesToBitmap())
                g.DrawImage(src, 0, 0);
            using (var pen = new Pen(Color.OrangeRed, 2f))
            using (var font = new Font("Consolas", 18f, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.Yellow))
            {
                for (int ri = 0; ri < rows.Count; ri++)
                {
                    int ci = 0;
                    foreach (var r in rows[ri])
                    {
                        g.DrawRectangle(pen, r.X, r.Y, r.Width, r.Height);
                        g.DrawString(ri + ":" + ci, font, brush, r.X, Math.Max(0, r.Y - 22));
                        ci++;
                    }
                }
            }
            preview.Save(previewPath, ImageFormat.Png);
        }
    }

    private static Bitmap BytesToBitmap()
    {
        var bmp = new Bitmap(W, H, PixelFormat.Format32bppArgb);
        var rect = new Rectangle(0, 0, W, H);
        var data = bmp.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        Marshal.Copy(Px, 0, data.Scan0, Px.Length);
        bmp.UnlockBits(data);
        return bmp;
    }

    private static void SaveCrop(int x, int y, int w, int h, string path)
    {
        using (var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb))
        {
            var rect = new Rectangle(0, 0, w, h);
            var data = bmp.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            var buf = new byte[Math.Abs(data.Stride) * h];
            for (int yy = 0; yy < h; yy++)
            {
                int src = Idx(x, y + yy);
                int dst = yy * data.Stride;
                Buffer.BlockCopy(Px, src, buf, dst, w * 4);
            }
            Marshal.Copy(buf, 0, data.Scan0, buf.Length);
            bmp.UnlockBits(data);
            bmp.Save(path, ImageFormat.Png);
        }
    }
}
