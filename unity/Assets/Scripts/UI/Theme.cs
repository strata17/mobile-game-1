using UnityEngine;

namespace Reveal.UI
{
    /// <summary>
    /// The single source of truth for spacing, radius, elevation and type
    /// scale. Every prior round of "fix this button" / "fix this shadow"
    /// picked a fresh ad-hoc number, which is why fixing one screen kept
    /// breaking another -- there was no shared system underneath. Every new
    /// UI element should pull from here instead of inventing its own values.
    /// </summary>
    public static class Theme
    {
        // ---- Radius scale: exactly three sizes, nothing else ----
        public const int RadiusChip = 16;   // small pills, badges, mission rows
        public const int RadiusControl = 26; // buttons, tiles, gallery cells
        public const int RadiusCard = 40;   // top-level cards/panels

        // ---- Spacing scale (4/8pt rhythm) ----
        public const float Space1 = 8f;
        public const float Space2 = 16f;
        public const float Space3 = 24f;
        public const float Space4 = 32f;
        public const float Space5 = 48f;

        // ---- Elevation scale: exactly three tiers ----
        // (radius, maxAlpha) for Art.SoftShadow. Subtle = small elements on
        // busy/colourful backdrops (board, buttons). Card = a floating modal,
        // needs to read as clearly lifted. Never invent a fourth value.
        public const int ShadowRadiusSubtle = 40;
        public const float ShadowAlphaSubtle = 0.22f;
        public const int ShadowRadiusCard = 40;
        public const float ShadowAlphaCard = 0.32f;

        // ---- Type scale: five sizes, nothing between ----
        public const int TextCaption = 22;  // small labels, captions
        public const int TextBody = 26;     // mission rows, body copy
        public const int TextLabel = 32;    // buttons, stat values at rest
        public const int TextTitle = 44;    // section headers
        public const int TextDisplay = 64;  // hero titles ("Board Cleared!")

        // ---- Colour palette: one purple-dusk backdrop, one accent set ----
        // Every screen shares this exact gradient and card colour instead of
        // each screen/card inventing its own near-but-not-quite purple. That
        // was invisible in isolation but reads as "off" the moment two
        // screens are seen back to back.
        public static readonly Color BgTop = UIFactory.Hex("#4a3aa8");
        public static readonly Color BgBottom = UIFactory.Hex("#140f2e");
        public static readonly Color CardTop = UIFactory.Hex("#463a8f");
        public static readonly Color CardBottom = UIFactory.Hex("#150f30");
        public static readonly Color Scrim = new Color(0.02f, 0.03f, 0.06f, 0.88f);

        public static readonly Color AccentCoral = UIFactory.Hex("#ff5f7e");
        public static readonly Color AccentGold = UIFactory.Hex("#ffcb47");
        public static readonly Color AccentMint = UIFactory.Hex("#3ddc97");
        public static readonly Color AccentSky = UIFactory.Hex("#4fb0ff");

        public static readonly Color TextOnDark = Color.white;
        public static readonly Color TextOnDarkDim = new Color(1f, 1f, 1f, 0.62f);
    }
}
