using Reveal.Core;
using UnityEngine;

namespace Reveal.Game
{
    /// <summary>
    /// Paints the hidden picture for a scene onto a Texture2D: a vertical
    /// gradient background plus a clean flat-vector motif. Pure CPU pixel work
    /// so it needs no shaders, prefabs or imported art.
    /// </summary>
    public static class MotifPainter
    {
        const int Size = 512;

        public static Texture2D Paint(Scene scene)
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            var px = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                float t = 1f - (float)y / (Size - 1);
                Color bg = Color.Lerp(scene.BgBottom, scene.BgTop, t);
                for (int x = 0; x < Size; x++)
                    px[y * Size + x] = bg;
            }
            tex.SetPixels32(px);

            Color ink = new Color(1f, 1f, 1f, 0.92f);
            Color warm = new Color(1f, 0.86f, 0.4f, 1f);
            var c = new Vector2(Size / 2f, Size / 2f);
            float R = Size * 0.30f;

            switch (scene.Motif)
            {
                case "sun":
                    Rays(tex, c, R * 1.35f, R * 1.7f, 12, warm);
                    Disc(tex, c, R, warm);
                    break;
                case "star": Star(tex, c, R * 1.25f, R * 0.5f, 5, ink); break;
                case "heart": Heart(tex, c, R * 1.3f, new Color(1f, 0.5f, 0.62f, 1f)); break;
                case "diamond": Diamond(tex, c, R * 1.2f, ink); break;
                case "flower": Flower(tex, c, R, ink, warm); break;
                case "moon": Crescent(tex, c, R * 1.1f, ink); break;
                case "rocket": Rocket(tex, c, R, ink); break;
                case "balloon": Disc(tex, new Vector2(c.x, c.y - R * 0.15f), R, new Color(1f, 0.6f, 0.55f, 1f)); break;
                case "rainbow": Rainbow(tex, new Vector2(c.x, c.y + R * 0.5f), R * 1.6f); break;
                case "cloud": Cloud(tex, c, R, Color.white); break;
                case "planet":
                    Disc(tex, c, R, new Color(0.65f, 0.55f, 1f, 1f));
                    Ring(tex, c, R * 1.5f, R * 0.16f, ink);
                    break;
                case "bolt": Bolt(tex, c, R * 1.4f, warm); break;
                default: Disc(tex, c, R, ink); break;
            }

            tex.Apply();
            return tex;
        }

        // ---------- primitives ----------
        static void Plot(Texture2D t, int x, int y, Color col)
        {
            if (x < 0 || y < 0 || x >= Size || y >= Size) return;
            Color d = t.GetPixel(x, y);
            t.SetPixel(x, y, Color.Lerp(d, col, col.a));
        }

        static void Disc(Texture2D t, Vector2 c, float r, Color col)
        {
            int minX = Mathf.FloorToInt(c.x - r), maxX = Mathf.CeilToInt(c.x + r);
            int minY = Mathf.FloorToInt(c.y - r), maxY = Mathf.CeilToInt(c.y + r);
            for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c);
                float a = Mathf.Clamp01(r - d);
                if (a > 0) Plot(t, x, y, new Color(col.r, col.g, col.b, col.a * a));
            }
        }

        static void Ring(Texture2D t, Vector2 c, float r, float w, Color col)
        {
            int minX = Mathf.FloorToInt(c.x - r), maxX = Mathf.CeilToInt(c.x + r);
            int minY = Mathf.FloorToInt(c.y - r), maxY = Mathf.CeilToInt(c.y + r);
            for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                float d = Mathf.Abs(Vector2.Distance(new Vector2(x, y), c) - r);
                float a = Mathf.Clamp01(w - d);
                if (a > 0) Plot(t, x, y, new Color(col.r, col.g, col.b, col.a * a));
            }
        }

        static void Rays(Texture2D t, Vector2 c, float r0, float r1, int n, Color col)
        {
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2f / n;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                for (float rr = r0; rr <= r1; rr += 0.5f)
                    Disc(t, c + dir * rr, 8f, col);
            }
        }

        static void Star(Texture2D t, Vector2 c, float outer, float inner, int points, Color col)
        {
            var pts = new Vector2[points * 2];
            for (int i = 0; i < points * 2; i++)
            {
                float rad = (i % 2 == 0) ? outer : inner;
                float a = -Mathf.PI / 2f + i * Mathf.PI / points;
                pts[i] = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rad;
            }
            FillPolygon(t, pts, col);
        }

        static void Diamond(Texture2D t, Vector2 c, float r, Color col)
        {
            var pts = new[]
            {
                new Vector2(c.x, c.y + r), new Vector2(c.x + r * 0.8f, c.y),
                new Vector2(c.x, c.y - r), new Vector2(c.x - r * 0.8f, c.y)
            };
            FillPolygon(t, pts, col);
        }

        static void Heart(Texture2D t, Vector2 c, float r, Color col)
        {
            for (int y = -Mathf.CeilToInt(r * 1.3f); y <= Mathf.CeilToInt(r); y++)
            for (int x = -Mathf.CeilToInt(r * 1.3f); x <= Mathf.CeilToInt(r * 1.3f); x++)
            {
                float fx = x / r, fy = -y / r;
                float v = Mathf.Pow(fx * fx + fy * fy - 1f, 3) - fx * fx * fy * fy * fy;
                if (v <= 0) Plot(t, (int)(c.x + x), (int)(c.y + y), col);
            }
        }

        static void Flower(Texture2D t, Vector2 c, float r, Color petal, Color center)
        {
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3f;
                Disc(t, c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r * 0.9f, r * 0.55f, petal);
            }
            Disc(t, c, r * 0.5f, center);
        }

        static void Crescent(Texture2D t, Vector2 c, float r, Color col)
        {
            Disc(t, c, r, col);
            Disc(t, new Vector2(c.x + r * 0.5f, c.y + r * 0.2f), r * 0.92f,
                Color.Lerp(col, Color.clear, 1f)); // punch-out via bg redraw is skipped; approximate
        }

        static void Rocket(Texture2D t, Vector2 c, float r, Color col)
        {
            var body = new[]
            {
                new Vector2(c.x, c.y + r * 1.3f), new Vector2(c.x + r * 0.45f, c.y),
                new Vector2(c.x + r * 0.35f, c.y - r), new Vector2(c.x - r * 0.35f, c.y - r),
                new Vector2(c.x - r * 0.45f, c.y)
            };
            FillPolygon(t, body, col);
            Disc(t, new Vector2(c.x, c.y + r * 0.15f), r * 0.22f, new Color(0.4f, 0.7f, 1f, 1f));
        }

        static void Rainbow(Texture2D t, Vector2 c, float r)
        {
            Color[] bands = {
                new Color(1f,0.4f,0.4f), new Color(1f,0.7f,0.3f), new Color(1f,0.9f,0.4f),
                new Color(0.5f,0.85f,0.5f), new Color(0.4f,0.7f,1f)
            };
            float w = r * 0.12f;
            for (int i = 0; i < bands.Length; i++)
                Arc(t, c, r - i * w, w * 0.9f, bands[i]);
        }

        static void Arc(Texture2D t, Vector2 c, float r, float w, Color col)
        {
            for (float a = 0; a <= Mathf.PI; a += 0.004f)
            {
                var p = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                Disc(t, p, w, col);
            }
        }

        static void Cloud(Texture2D t, Vector2 c, float r, Color col)
        {
            Disc(t, new Vector2(c.x - r * 0.6f, c.y - r * 0.1f), r * 0.6f, col);
            Disc(t, new Vector2(c.x + r * 0.6f, c.y - r * 0.1f), r * 0.6f, col);
            Disc(t, new Vector2(c.x, c.y + r * 0.35f), r * 0.75f, col);
            Disc(t, new Vector2(c.x, c.y - r * 0.2f), r * 0.7f, col);
        }

        static void Bolt(Texture2D t, Vector2 c, float r, Color col)
        {
            var pts = new[]
            {
                new Vector2(c.x + r * 0.15f, c.y + r), new Vector2(c.x - r * 0.35f, c.y),
                new Vector2(c.x, c.y), new Vector2(c.x - r * 0.15f, c.y - r),
                new Vector2(c.x + r * 0.35f, c.y * 1f), new Vector2(c.x, c.y)
            };
            FillPolygon(t, pts, col);
        }

        static void FillPolygon(Texture2D t, Vector2[] pts, Color col)
        {
            float minY = float.MaxValue, maxY = float.MinValue;
            foreach (var p in pts) { minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y); }
            for (int y = Mathf.FloorToInt(minY); y <= Mathf.CeilToInt(maxY); y++)
            {
                var xs = new System.Collections.Generic.List<float>();
                for (int i = 0; i < pts.Length; i++)
                {
                    var a = pts[i]; var b = pts[(i + 1) % pts.Length];
                    if ((a.y <= y && b.y > y) || (b.y <= y && a.y > y))
                        xs.Add(a.x + (y - a.y) / (b.y - a.y) * (b.x - a.x));
                }
                xs.Sort();
                for (int i = 0; i + 1 < xs.Count; i += 2)
                    for (int x = Mathf.FloorToInt(xs[i]); x <= Mathf.CeilToInt(xs[i + 1]); x++)
                        Plot(t, x, y, col);
            }
        }
    }
}
