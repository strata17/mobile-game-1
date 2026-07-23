using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Reveal.UI
{
    /// <summary>
    /// Runtime texture/sprite generation for a polished casual-game look:
    /// rounded-rect surfaces, glossy "gem" tiles, vertical gradients and soft
    /// shadows. Everything is cached so repeated styles reuse one sprite.
    /// This is what turns flat squares into candy — depth, rounding, gloss.
    /// </summary>
    public static class Art
    {
        static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

        /// <summary>
        /// A rounded rectangle as a 9-sliced sprite (border = radius) so it
        /// scales to any size. Optional glossy vertical highlight for the
        /// candy-button / gem look.
        /// </summary>
        public static Sprite RoundedRect(int radius = 28, bool glossy = false, string key = null)
        {
            key ??= $"rr_{radius}_{glossy}";
            if (_cache.TryGetValue(key, out var s)) return s;

            int size = radius * 2 + 4;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float a = CornerAlpha(x, y, size, radius);
                float shade = 1f;
                if (glossy)
                {
                    // brighter at the top, subtle darken at the bottom
                    float t = 1f - (float)y / (size - 1);
                    shade = Mathf.Lerp(0.82f, 1.12f, t);
                }
                px[y * size + x] = new Color(shade, shade, shade, a);
            }
            tex.SetPixels32(px);
            tex.Apply();

            int b = radius; // 9-slice border
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
            _cache[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// Soft radial drop-shadow sprite (9-sliced) for elevation. maxAlpha
        /// defaults to a subtle value suited to elements sitting on a busy
        /// multi-hued backdrop (the board, buttons) -- a strong flat alpha
        /// there reads as a hard dark ring rather than a soft shadow. A
        /// floating modal card wants a visibly stronger shadow to read as
        /// properly elevated, so callers can request a higher value.
        /// </summary>
        public static Sprite SoftShadow(int radius = 40, float maxAlpha = 0.22f)
        {
            string key = $"shadow_{radius}_{maxAlpha}";
            if (_cache.TryGetValue(key, out var s)) return s;

            int size = radius * 2 + 4;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float a = CornerAlpha(x, y, size, radius);
                a = Mathf.SmoothStep(0f, 1f, a) * maxAlpha;
                px[y * size + x] = new Color(0f, 0f, 0f, a);
            }
            tex.SetPixels32(px);
            tex.Apply();
            int b = radius;
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
            _cache[key] = sprite;
            return sprite;
        }

        static Texture2D _topGloss;

        /// <summary>
        /// A soft white top-highlight gradient (opaque-ish at top, fully
        /// transparent by mid-height). Used as a small Simple-stretched
        /// overlay on buttons/bars instead of baking gloss into a 9-sliced
        /// base sprite -- 9-slicing only preserves a gradient within the
        /// border strip, so on a tall element (e.g. a full-height button)
        /// the stretched middle collapses to one flat colour and the shine
        /// all but disappears. A plain stretched gradient has no such
        /// limitation: it scales cleanly to any target size.
        /// </summary>
        public static Texture2D TopGloss()
        {
            if (_topGloss != null) return _topGloss;
            int h = 64;
            var tex = new Texture2D(2, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < h; y++)
            {
                float t = (float)y / (h - 1); // 0 bottom, 1 top
                float a = Mathf.Pow(Mathf.SmoothStep(0f, 1f, t), 1.3f) * 0.32f;
                var c = new Color(1f, 1f, 1f, a);
                tex.SetPixel(0, y, c);
                tex.SetPixel(1, y, c);
            }
            tex.Apply();
            _topGloss = tex;
            return tex;
        }

        /// <summary>
        /// A subtle grid-line texture: thin translucent lines every cellPx,
        /// forming (cells x cells) squares. Used so every board cell keeps a
        /// visible boundary even after its cover is revealed — without this,
        /// revealed cells blend seamlessly into the picture and any clue
        /// number on them looks like it's floating with no "home" cell.
        /// </summary>
        public static Texture2D GridTexture(int cells, int cellPx, Color lineColor, int thickness = 2)
        {
            int size = cells * cellPx;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color32[size * size];
            Color32 clear = new Color32(0, 0, 0, 0);
            Color32 line = lineColor;
            for (int y = 0; y < size; y++)
            {
                int ym = y % cellPx;
                for (int x = 0; x < size; x++)
                {
                    int xm = x % cellPx;
                    bool onLine = xm < thickness || ym < thickness;
                    px[y * size + x] = onLine ? line : clear;
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        /// <summary>A full-screen vertical gradient texture (top → bottom).</summary>
        public static Texture2D Gradient(Color top, Color bottom, int h = 256)
        {
            var tex = new Texture2D(2, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < h; y++)
            {
                Color c = Color.Lerp(bottom, top, (float)y / (h - 1));
                tex.SetPixel(0, y, c);
                tex.SetPixel(1, y, c);
            }
            tex.Apply();
            return tex;
        }

        static float CornerAlpha(int x, int y, int size, int radius)
        {
            // distance into a corner; 1 inside, feathered across a 1px edge
            float cx = Mathf.Clamp(x, radius, size - radius);
            float cy = Mathf.Clamp(y, radius, size - radius);
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
            return Mathf.Clamp01(radius - d + 0.5f);
        }

        /// <summary>Add a drop shadow behind a UI element (as a sibling image).</summary>
        public static void AddShadow(RectTransform target, float spread = 18f, float yOffset = -10f)
        {
            var go = new GameObject("Shadow", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(target.parent, false);
            go.transform.SetSiblingIndex(target.GetSiblingIndex()); // behind target
            var img = go.GetComponent<Image>();
            img.sprite = SoftShadow();
            img.type = Image.Type.Sliced;
            img.raycastTarget = false;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = target.anchorMin; rt.anchorMax = target.anchorMax;
            rt.pivot = target.pivot;
            rt.sizeDelta = target.sizeDelta + new Vector2(spread, spread);
            rt.anchoredPosition = target.anchoredPosition + new Vector2(0, yOffset);
        }
    }
}
