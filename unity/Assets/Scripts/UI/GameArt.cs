using UnityEngine;

namespace Reveal.UI
{
    /// <summary>
    /// Loads optional imported art (backgrounds, logo, mascot) from Resources.
    /// Every getter returns null if the asset is absent, so the game runs on
    /// procedural art until real assets are dropped into
    /// Assets/Resources/Art/. This keeps the project buildable at all times.
    /// </summary>
    public static class GameArt
    {
        static bool _loaded;
        static Texture2D _bg, _logo, _mascot;

        static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;
            _bg = Resources.Load<Texture2D>("Art/bg");
            _logo = Resources.Load<Texture2D>("Art/logo");
            _mascot = Resources.Load<Texture2D>("Art/mascot");
        }

        static readonly System.Collections.Generic.Dictionary<string, Texture2D> _pics
            = new System.Collections.Generic.Dictionary<string, Texture2D>();

        // Scenes without their own illustration reuse one of the finished five,
        // so every board shows real art (never the geometric placeholder).
        static readonly System.Collections.Generic.Dictionary<string, string> _alias
            = new System.Collections.Generic.Dictionary<string, string>
        {
            { "moon", "star" }, { "rocket", "diamond" }, { "balloon", "heart" },
            { "rainbow", "flower" }, { "cloud", "sun" }, { "planet", "diamond" },
            { "bolt", "star" },
        };

        /// <summary>The hidden picture for a motif, or null to fall back to procedural.</summary>
        public static Texture2D Picture(string motif)
        {
            if (_pics.TryGetValue(motif, out var t)) return t;
            t = Resources.Load<Texture2D>("Art/pics/" + motif);
            if (t == null && _alias.TryGetValue(motif, out var alt))
                t = Resources.Load<Texture2D>("Art/pics/" + alt);
            _pics[motif] = t;
            return t;
        }

        public static Texture2D Background { get { EnsureLoaded(); return _bg; } }
        public static Texture2D Logo { get { EnsureLoaded(); return _logo; } }
        public static Texture2D Mascot { get { EnsureLoaded(); return _mascot; } }

        public static Sprite SpriteFrom(Texture2D tex)
        {
            if (tex == null) return null;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
