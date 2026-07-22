using UnityEngine;
using UnityEngine.UI;

namespace Reveal.UI
{
    /// <summary>
    /// Helpers for building UnityEngine.UI hierarchies from code. Keeping the UI
    /// programmatic (rather than in binary .prefab/.unity files) means the whole
    /// project is reviewable as text and reproducible from source.
    /// </summary>
    public static class UIFactory
    {
        public static Color Hex(string h)
        {
            ColorUtility.TryParseHtmlString(h, out Color c);
            return c;
        }

        public static RectTransform Panel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go.GetComponent<RectTransform>();
        }

        /// <summary>A rounded, optionally glossy filled panel (candy surface).</summary>
        public static Image RoundedPanel(Transform parent, string name, Color color, int radius = 28, bool glossy = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = Art.RoundedRect(radius, glossy);
            img.type = Image.Type.Sliced;
            img.color = color;
            return img;
        }

        public static RectTransform Container(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        public static Text Label(Transform parent, string name, string text, int size,
            Color color, TextAnchor anchor = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.text = text;
            t.font = DefaultFont;
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.fontStyle = style;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>
        /// A "candy" button: rounded, glossy face sitting on a darker extruded
        /// base, with a scale-down press animation. The 3D base is a sibling
        /// image behind the face so the button reads as a physical, tappable
        /// object rather than a flat rectangle.
        /// </summary>
        public static Button Button(Transform parent, string name, string label, Color bg, Color fg, int size = 34)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = Art.RoundedRect(30, true);
            img.type = Image.Type.Sliced;
            img.color = bg;
            var btn = go.GetComponent<Button>();
            btn.transition = Selectable.Transition.None; // handled by PressPop

            // Soft depth via a built-in Shadow effect (robust under layout groups).
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
            shadow.effectDistance = new Vector2(0, -5);

            AddGloss(go.transform);

            var txt = Label(go.transform, "Label", label, size, fg, TextAnchor.MiddleCenter, FontStyle.Bold);
            var rt = txt.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            go.AddComponent<PressPop>();
            return btn;
        }

        /// <summary>
        /// A soft top-highlight overlay, inset from the edges so its sharp
        /// rectangular corners stay comfortably inside the parent's rounded
        /// silhouette. See Art.TopGloss for why this beats baking gloss into
        /// a 9-sliced base sprite.
        /// </summary>
        public static void AddGloss(Transform parent, float lowerY = 0.42f, float upperY = 0.92f, float insetFrac = 0.05f)
        {
            var go = new GameObject("Gloss", typeof(RectTransform), typeof(RawImage));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<RawImage>();
            img.texture = Art.TopGloss();
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = new Vector2(insetFrac, lowerY);
            rt.anchorMax = new Vector2(1f - insetFrac, upperY);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        /// <summary>Stretch a RectTransform to fill its parent.</summary>
        public static void Stretch(RectTransform rt, float pad = 0f)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(pad, pad);
            rt.offsetMax = new Vector2(-pad, -pad);
        }

        public static void Anchor(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        static Font _font;
        public static Font DefaultFont
        {
            get
            {
                if (_font == null)
                {
                    // LegacyRuntime.ttf is the built-in font on modern Unity;
                    // fall back to Arial on older versions.
                    _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }
                return _font;
            }
        }

        public static Sprite SolidSprite()
        {
            if (_solid == null)
            {
                var tex = Texture2D.whiteTexture;
                _solid = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
            return _solid;
        }
        static Sprite _solid;
    }
}
