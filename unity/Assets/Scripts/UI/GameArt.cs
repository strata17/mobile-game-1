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
        static Texture2D _bg, _logo, _mascot, _mascotSad, _mascotHappy, _coin, _heartIcon, _chest, _starIcon, _flame, _gear, _splash, _bomb;

        static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;
            _bg = Resources.Load<Texture2D>("Art/bg");
            _logo = Resources.Load<Texture2D>("Art/logo");
            _mascot = Resources.Load<Texture2D>("Art/mascot");
            _mascotSad = Resources.Load<Texture2D>("Art/mascot_sad");
            _mascotHappy = Resources.Load<Texture2D>("Art/mascot_happy");
            _bomb = Resources.Load<Texture2D>("Art/bomb");
            _coin = Resources.Load<Texture2D>("Art/coin");
            _heartIcon = Resources.Load<Texture2D>("Art/heart_icon");
            _chest = Resources.Load<Texture2D>("Art/chest");
            _starIcon = Resources.Load<Texture2D>("Art/star_icon");
            _flame = Resources.Load<Texture2D>("Art/flame");
            _gear = Resources.Load<Texture2D>("Art/gear");
            _splash = Resources.Load<Texture2D>("Art/splash");
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
        public static Texture2D MascotSad { get { EnsureLoaded(); return _mascotSad; } }
        public static Texture2D MascotHappy { get { EnsureLoaded(); return _mascotHappy; } }
        public static Texture2D Bomb { get { EnsureLoaded(); return _bomb; } }
        public static Texture2D Coin { get { EnsureLoaded(); return _coin; } }
        public static Texture2D HeartIcon { get { EnsureLoaded(); return _heartIcon; } }
        public static Texture2D Chest { get { EnsureLoaded(); return _chest; } }
        public static Texture2D StarIcon { get { EnsureLoaded(); return _starIcon; } }
        public static Texture2D Flame { get { EnsureLoaded(); return _flame; } }
        public static Texture2D Gear { get { EnsureLoaded(); return _gear; } }
        public static Texture2D Splash { get { EnsureLoaded(); return _splash; } }

        public static Sprite SpriteFrom(Texture2D tex)
        {
            if (tex == null) return null;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
