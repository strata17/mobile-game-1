using System;
using System.Collections.Generic;
using UnityEngine;

namespace Reveal.Core
{
    /// <summary>
    /// Thin, typed wrapper over PlayerPrefs. All persistent state lives here so
    /// there is a single place that knows the storage keys. Mirrors the
    /// localStorage "reveal.*" keys from the web build.
    /// </summary>
    public static class SaveSystem
    {
        const string K = "reveal.";

        public static int Level
        {
            get => PlayerPrefs.GetInt(K + "level", 1);
            set { PlayerPrefs.SetInt(K + "level", value); PlayerPrefs.Save(); }
        }

        public static int Coins
        {
            get => PlayerPrefs.GetInt(K + "coins", 0);
            set { PlayerPrefs.SetInt(K + "coins", Mathf.Max(0, value)); PlayerPrefs.Save(); }
        }

        public static int Best
        {
            get => PlayerPrefs.GetInt(K + "best", 0);
            set { PlayerPrefs.SetInt(K + "best", value); PlayerPrefs.Save(); }
        }

        public static int Streak
        {
            get => PlayerPrefs.GetInt(K + "streak", 0);
            set { PlayerPrefs.SetInt(K + "streak", value); PlayerPrefs.Save(); }
        }

        public static string LastDailyDay
        {
            get => PlayerPrefs.GetString(K + "lastDaily", "");
            set { PlayerPrefs.SetString(K + "lastDaily", value); PlayerPrefs.Save(); }
        }

        public static bool TutorialDone
        {
            get => PlayerPrefs.GetInt(K + "tut", 0) == 1;
            set { PlayerPrefs.SetInt(K + "tut", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static bool SoundOn
        {
            get => PlayerPrefs.GetInt(K + "sound", 1) == 1;
            set { PlayerPrefs.SetInt(K + "sound", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static bool HapticsOn
        {
            get => PlayerPrefs.GetInt(K + "haptics", 1) == 1;
            set { PlayerPrefs.SetInt(K + "haptics", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        // Collection: which motif indices the player has revealed at least once.
        public static HashSet<int> Collection
        {
            get
            {
                var raw = PlayerPrefs.GetString(K + "collection", "");
                var set = new HashSet<int>();
                if (!string.IsNullOrEmpty(raw))
                    foreach (var p in raw.Split(','))
                        if (int.TryParse(p, out int v)) set.Add(v);
                return set;
            }
            set
            {
                PlayerPrefs.SetString(K + "collection", string.Join(",", value));
                PlayerPrefs.Save();
            }
        }

        // Daily missions blob (id|progress;...) plus the day they were rolled.
        public static string MissionsBlob
        {
            get => PlayerPrefs.GetString(K + "missions", "");
            set { PlayerPrefs.SetString(K + "missions", value); PlayerPrefs.Save(); }
        }

        public static string MissionsDay
        {
            get => PlayerPrefs.GetString(K + "missionsDay", "");
            set { PlayerPrefs.SetString(K + "missionsDay", value); PlayerPrefs.Save(); }
        }

        public static string Today => DateTime.UtcNow.ToString("yyyy-MM-dd");

        public static void ResetAll()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }
    }
}
