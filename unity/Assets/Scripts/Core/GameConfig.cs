using UnityEngine;

namespace Reveal.Core
{
    /// <summary>
    /// Central tuning constants for Reveal. Mirrors the balancing that was
    /// dialled in on the web prototype so the native game feels identical.
    /// Difficulty is deliberately gentle: bombs are visible and avoidable, so
    /// the bomb count is the whole difficulty dial — keep the ramp slow.
    /// </summary>
    public static class GameConfig
    {
        public const float WinRatio = 0.70f;   // fraction of safe tiles to clear a level
        public const int MaxHearts = 3;         // mistakes allowed per level before it fails
        public const float RevealSeconds = 0.24f; // per-tile cover dissolve duration

        public static readonly int[] StarTimes = { 30, 60 }; // <=30s -> 3 stars, <=60s -> 2 stars

        public const int HintCost = 50;         // coins to spend on a hint instead of an ad
        public const int BonusCoin = 25;        // coins from a surprise bonus tile
        public const int LevelCoin = 15;        // coins for clearing a level
        public const int ChestEvery = 5;        // a chapter chest every N levels
        public const int DailyBase = 40;        // base daily-reward coins (+streak bonus)
        public const int EndowTiles = 4;        // head-start tiles pre-revealed (endowed progress)
        public const int NudgeRemaining = 5;    // show "almost there" when this many safe tiles remain
        public const float GlowAt = 0.7f;       // progress fraction where the goal-gradient glow kicks in

        // Monetization pacing. Aligned with the CrazyLabs FTUE guidance: no ad
        // pressure in the first session. Interstitials are suppressed until the
        // player has cleared at least this many levels.
        public const int InterstitialEveryNLosses = 3;
        public const int NoAdsBeforeLevel = 2;

        public static int GridSizeForLevel(int level)
        {
            return Mathf.Min(8 + (level - 1) / 5, 12);
        }

        public static int BombCountForLevel(int level, int cells)
        {
            int bombs = 1 + (level - 1) / 5;
            return Mathf.Min(bombs, Mathf.FloorToInt(cells * 0.18f));
        }

        public static int ChapterOf(int level)
        {
            return (level - 1) / ChestEvery + 1;
        }

        public static int DailyAmount(int streak)
        {
            return DailyBase + Mathf.Min(streak, 7) * 10;
        }
    }
}
