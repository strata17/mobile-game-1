using System;
using System.Collections.Generic;
using Reveal.Core;
using Reveal.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace Reveal.UI
{
    /// <summary>
    /// Builds and drives every screen: top HUD, in-level progress/hearts, the
    /// board host, and the menu / level-complete / game-over / settings
    /// overlays. GameManager wires the button callbacks and calls the Show/Set
    /// methods; this class owns no game logic.
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        // Callbacks (wired by GameManager)
        public Action OnPlay, OnNextLevel, OnRetry, OnDaily, OnHint, OnContinueAd, OnSettings, OnCloseSettings, OnReset, OnToggleSound;

        // HUD
        Text _coins, _level, _score, _best, _progressPct;
        Image _progressFill;
        RectTransform _heartsRow;
        RectTransform _boardHost;

        // Overlays
        RectTransform _menu, _levelComplete, _gameOver, _settings;
        Text _lcTitle, _lcLevel, _lcPoints, _lcCoins, _unlockNote;
        Text _goScore, _nearMiss;
        Text _chapterLabel, _streakLabel, _collectionCount;
        Image _journeyFill;
        Button _dailyBtn;
        RectTransform _missionsList, _collectionRow;
        Button _hintBtn;

        public RectTransform BoardHost => _boardHost;

        readonly Color _cardBg = UIFactory.Hex("#141726");
        readonly Color _accent = UIFactory.Hex("#ff8f3d");
        readonly Color _primary = UIFactory.Hex("#5f6dff");
        readonly Color _text = UIFactory.Hex("#f4f6ff");
        readonly Color _muted = UIFactory.Hex("#9aa3c7");

        RectTransform _root;

        public void Build(RectTransform root)
        {
            _root = root;
            BuildHud();
            BuildBoardHost();
            BuildControls();
            BuildMenu();
            BuildLevelComplete();
            BuildGameOver();
            BuildSettings();
        }

        // ---------------- HUD ----------------
        void BuildHud()
        {
            var top = UIFactory.Container(_root, "TopBar");
            top.anchorMin = new Vector2(0, 1); top.anchorMax = new Vector2(1, 1);
            top.pivot = new Vector2(0.5f, 1);
            top.sizeDelta = new Vector2(0, 120);
            top.anchoredPosition = new Vector2(0, -20);

            var coinPill = UIFactory.RoundedPanel(top, "CoinPill", _cardBg, 32, true).rectTransform;
            UIFactory.Anchor(coinPill, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-30, 0), new Vector2(200, 70));
            _coins = UIFactory.Label(coinPill, "Coins", "0", 40, UIFactory.Hex("#ffd76a"), TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Stretch(_coins.rectTransform);

            var gear = UIFactory.Button(top, "Gear", "•••", _cardBg, _text, 34);
            UIFactory.Anchor((RectTransform)gear.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(30, 0), new Vector2(72, 72));
            gear.onClick.AddListener(() => OnSettings?.Invoke());

            // Stats row
            var stats = UIFactory.Container(_root, "Stats");
            stats.anchorMin = new Vector2(0, 1); stats.anchorMax = new Vector2(1, 1);
            stats.pivot = new Vector2(0.5f, 1);
            stats.sizeDelta = new Vector2(0, 120);
            stats.anchoredPosition = new Vector2(0, -150);
            _level = Stat(stats, "LEVEL", -0.32f, out _);
            _score = Stat(stats, "SCORE", 0f, out _);
            _best = Stat(stats, "BEST", 0.32f, out _);

            // Progress
            var prow = UIFactory.Container(_root, "Progress");
            prow.anchorMin = new Vector2(0, 1); prow.anchorMax = new Vector2(1, 1);
            prow.pivot = new Vector2(0.5f, 1);
            prow.sizeDelta = new Vector2(-80, 40);
            prow.anchoredPosition = new Vector2(0, -300);
            var track = UIFactory.RoundedPanel(prow, "Track", UIFactory.Hex("#0c0e18"), 18).rectTransform;
            UIFactory.Stretch(track);
            _progressFill = UIFactory.RoundedPanel(track, "Fill", _accent, 18, true);
            _progressFill.rectTransform.anchorMin = new Vector2(0, 0);
            _progressFill.rectTransform.anchorMax = new Vector2(0, 1);
            _progressFill.rectTransform.pivot = new Vector2(0, 0.5f);
            _progressFill.rectTransform.sizeDelta = new Vector2(0, 0);
            _progressPct = UIFactory.Label(prow, "Pct", "0%", 26, _text);
            UIFactory.Stretch(_progressPct.rectTransform);

            _heartsRow = UIFactory.Container(_root, "Hearts");
            _heartsRow.anchorMin = new Vector2(0.5f, 1); _heartsRow.anchorMax = new Vector2(0.5f, 1);
            _heartsRow.pivot = new Vector2(0.5f, 1);
            _heartsRow.sizeDelta = new Vector2(300, 50);
            _heartsRow.anchoredPosition = new Vector2(0, -350);
            var hl = _heartsRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.childAlignment = TextAnchor.MiddleCenter; hl.spacing = 8;
        }

        Text Stat(RectTransform parent, string label, float xAnchor, out Text lbl)
        {
            var box = UIFactory.Container(parent, label);
            box.anchorMin = box.anchorMax = new Vector2(0.5f + xAnchor, 0.5f);
            box.pivot = new Vector2(0.5f, 0.5f);
            box.sizeDelta = new Vector2(200, 110);
            lbl = UIFactory.Label(box, "L", label, 22, _muted, TextAnchor.UpperCenter);
            UIFactory.Stretch(lbl.rectTransform);
            var val = UIFactory.Label(box, "V", "0", 52, _text, TextAnchor.LowerCenter, FontStyle.Bold);
            UIFactory.Stretch(val.rectTransform);
            return val;
        }

        void BuildBoardHost()
        {
            _boardHost = UIFactory.Container(_root, "BoardHost");
            _boardHost.anchorMin = new Vector2(0.5f, 0.5f);
            _boardHost.anchorMax = new Vector2(0.5f, 0.5f);
            _boardHost.pivot = new Vector2(0.5f, 0.5f);
            _boardHost.anchoredPosition = new Vector2(0, -40);
            _boardHost.sizeDelta = new Vector2(980, 980);
        }

        void BuildControls()
        {
            _hintBtn = UIFactory.Button(_root, "HintBtn", "Reveal a safe tile", _cardBg, _text, 32);
            var rt = (RectTransform)_hintBtn.transform;
            rt.anchorMin = new Vector2(0.5f, 0); rt.anchorMax = new Vector2(0.5f, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.anchoredPosition = new Vector2(0, 60);
            rt.sizeDelta = new Vector2(640, 96);
            _hintBtn.onClick.AddListener(() => OnHint?.Invoke());
        }

        // ---------------- overlays ----------------
        RectTransform Overlay(string name, out RectTransform card)
        {
            var ov = UIFactory.Panel(_root, name, new Color(0.02f, 0.03f, 0.06f, 0.6f));
            UIFactory.Stretch(ov);
            card = UIFactory.RoundedPanel(ov, "Card", _cardBg, 44).rectTransform;
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
            card.pivot = new Vector2(0.5f, 0.5f);
            card.sizeDelta = new Vector2(900, 1200);
            Art.AddShadow(card, 30f, -14f);
            var vg = card.gameObject.AddComponent<VerticalLayoutGroup>();
            vg.childAlignment = TextAnchor.UpperCenter;
            vg.spacing = 22; vg.padding = new RectOffset(50, 50, 50, 50);
            vg.childControlHeight = false; vg.childControlWidth = true;
            vg.childForceExpandHeight = false;
            return ov;
        }

        void BuildMenu()
        {
            _menu = Overlay("Menu", out var card);
            UIFactory.Label(card, "Title", "REVEAL", 96, _text, TextAnchor.MiddleCenter, FontStyle.Bold)
                .rectTransform.sizeDelta = new Vector2(0, 130);
            _streakLabel = UIFactory.Label(card, "Streak", "", 30, _accent);
            _streakLabel.rectTransform.sizeDelta = new Vector2(0, 40);

            _chapterLabel = UIFactory.Label(card, "Chapter", "Chapter 1", 34, _muted);
            _chapterLabel.rectTransform.sizeDelta = new Vector2(0, 46);
            var jtrack = UIFactory.RoundedPanel(card, "JTrack", UIFactory.Hex("#0c0e18"), 14).rectTransform;
            jtrack.sizeDelta = new Vector2(0, 22);
            _journeyFill = UIFactory.RoundedPanel(jtrack, "JFill", _primary, 14, true);
            _journeyFill.rectTransform.anchorMin = new Vector2(0, 0);
            _journeyFill.rectTransform.anchorMax = new Vector2(0, 1);
            _journeyFill.rectTransform.pivot = new Vector2(0, 0.5f);

            var play = UIFactory.Button(card, "Play", "PLAY", _primary, Color.white, 46);
            ((RectTransform)play.transform).sizeDelta = new Vector2(0, 130);
            play.onClick.AddListener(() => OnPlay?.Invoke());

            _dailyBtn = UIFactory.Button(card, "Daily", "Daily reward", _accent, Color.white, 32);
            ((RectTransform)_dailyBtn.transform).sizeDelta = new Vector2(0, 96);
            _dailyBtn.onClick.AddListener(() => OnDaily?.Invoke());

            UIFactory.Label(card, "MissHead", "DAILY MISSIONS", 26, _muted).rectTransform.sizeDelta = new Vector2(0, 40);
            _missionsList = UIFactory.Container(card, "Missions");
            _missionsList.sizeDelta = new Vector2(0, 220);
            var mv = _missionsList.gameObject.AddComponent<VerticalLayoutGroup>();
            mv.spacing = 10; mv.childControlHeight = false; mv.childForceExpandHeight = false;

            _collectionCount = UIFactory.Label(card, "ColHead", "Gallery 0/0", 26, _muted);
            _collectionCount.rectTransform.sizeDelta = new Vector2(0, 40);
            _collectionRow = UIFactory.Container(card, "Collection");
            _collectionRow.sizeDelta = new Vector2(0, 90);
            var cg = _collectionRow.gameObject.AddComponent<GridLayoutGroup>();
            cg.cellSize = new Vector2(64, 64); cg.spacing = new Vector2(8, 8);
            cg.constraint = GridLayoutGroup.Constraint.FixedRowCount; cg.constraintCount = 1;
        }

        void BuildLevelComplete()
        {
            _levelComplete = Overlay("LevelComplete", out var card);
            _levelComplete.gameObject.SetActive(false);
            UIFactory.Label(card, "Stars", "★ ★ ★", 60, UIFactory.Hex("#ffd76a")).rectTransform.sizeDelta = new Vector2(0, 90);
            _lcTitle = UIFactory.Label(card, "Title", "Board Cleared!", 60, _text, TextAnchor.MiddleCenter, FontStyle.Bold);
            _lcTitle.rectTransform.sizeDelta = new Vector2(0, 90);
            _lcLevel = UIFactory.Label(card, "Lvl", "Level 1 complete", 34, _muted);
            _lcLevel.rectTransform.sizeDelta = new Vector2(0, 50);
            _lcPoints = UIFactory.Label(card, "Pts", "+0 pts", 40, _accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            _lcPoints.rectTransform.sizeDelta = new Vector2(0, 60);
            _lcCoins = UIFactory.Label(card, "Coins", "+0 coins", 36, UIFactory.Hex("#ffd76a"));
            _lcCoins.rectTransform.sizeDelta = new Vector2(0, 50);
            _unlockNote = UIFactory.Label(card, "Unlock", "", 30, UIFactory.Hex("#7fe0a0"));
            _unlockNote.rectTransform.sizeDelta = new Vector2(0, 50);
            var next = UIFactory.Button(card, "Next", "NEXT LEVEL →", _primary, Color.white, 42);
            ((RectTransform)next.transform).sizeDelta = new Vector2(0, 130);
            next.onClick.AddListener(() => OnNextLevel?.Invoke());
        }

        void BuildGameOver()
        {
            _gameOver = Overlay("GameOver", out var card);
            _gameOver.gameObject.SetActive(false);
            UIFactory.Label(card, "Boom", "!", 110, UIFactory.Hex("#ff5f7e"), TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.sizeDelta = new Vector2(0, 120);
            UIFactory.Label(card, "Title", "Out of lives!", 60, _text, TextAnchor.MiddleCenter, FontStyle.Bold)
                .rectTransform.sizeDelta = new Vector2(0, 90);
            _nearMiss = UIFactory.Label(card, "Near", "", 34, _accent);
            _nearMiss.rectTransform.sizeDelta = new Vector2(0, 50);
            _goScore = UIFactory.Label(card, "Score", "Score: 0", 38, _muted);
            _goScore.rectTransform.sizeDelta = new Vector2(0, 60);
            var cont = UIFactory.Button(card, "Continue", "Continue — clear the bombs (AD)", _accent, Color.white, 30);
            ((RectTransform)cont.transform).sizeDelta = new Vector2(0, 120);
            cont.onClick.AddListener(() => OnContinueAd?.Invoke());
            var retry = UIFactory.Button(card, "Retry", "Retry level", _cardBg, _text, 34);
            ((RectTransform)retry.transform).sizeDelta = new Vector2(0, 100);
            retry.onClick.AddListener(() => OnRetry?.Invoke());
        }

        void BuildSettings()
        {
            _settings = Overlay("Settings", out var card);
            _settings.gameObject.SetActive(false);
            UIFactory.Label(card, "Title", "Settings", 56, _text, TextAnchor.MiddleCenter, FontStyle.Bold)
                .rectTransform.sizeDelta = new Vector2(0, 90);
            var snd = UIFactory.Button(card, "Sound", "Sound: On", _cardBg, _text, 34);
            ((RectTransform)snd.transform).sizeDelta = new Vector2(0, 100);
            snd.onClick.AddListener(() => { OnToggleSound?.Invoke(); snd.GetComponentInChildren<Text>().text = "Sound: " + (SaveSystem.SoundOn ? "On" : "Off"); });
            UIFactory.Label(card, "Ver", "Reveal 3.0 · Unity · clear 70%, avoid bombs", 26, _muted)
                .rectTransform.sizeDelta = new Vector2(0, 60);
            var done = UIFactory.Button(card, "Done", "Done", _primary, Color.white, 38);
            ((RectTransform)done.transform).sizeDelta = new Vector2(0, 110);
            done.onClick.AddListener(() => OnCloseSettings?.Invoke());
            var reset = UIFactory.Button(card, "Reset", "Reset all progress", _cardBg, UIFactory.Hex("#ff5f7e"), 30);
            ((RectTransform)reset.transform).sizeDelta = new Vector2(0, 90);
            reset.onClick.AddListener(() => OnReset?.Invoke());
        }

        // ---------------- update methods ----------------
        public void SetHud(int coins, int level, int score, int best)
        {
            _coins.text = coins.ToString();
            _level.text = level.ToString();
            _score.text = score.ToString();
            _best.text = best.ToString();
        }

        public void SetProgress(float frac, int remaining)
        {
            _progressFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(frac), 1);
            _progressPct.text = Mathf.RoundToInt(Mathf.Clamp01(frac) * 100) + "%";
            _progressFill.color = frac >= GameConfig.GlowAt ? UIFactory.Hex("#7fe0a0") : _accent;
        }

        public void SetHearts(int hearts)
        {
            foreach (Transform t in _heartsRow) Destroy(t.gameObject);
            for (int i = 0; i < GameConfig.MaxHearts; i++)
            {
                var h = UIFactory.Label(_heartsRow, "H", i < hearts ? "♥" : "♡", 40,
                    i < hearts ? UIFactory.Hex("#ff5f7e") : _muted);
                h.rectTransform.sizeDelta = new Vector2(44, 44);
            }
        }

        public void SetHintButton(int coins)
        {
            var t = _hintBtn.GetComponentInChildren<Text>();
            t.text = coins >= GameConfig.HintCost
                ? $"Reveal a safe tile · {GameConfig.HintCost} coins"
                : "Reveal a safe tile (AD)";
        }

        public void ShowInGame(bool show)
        {
            _hintBtn.gameObject.SetActive(show);
        }

        public void ShowMenu(bool show) => _menu.gameObject.SetActive(show);
        public void ShowSettings(bool show) => _settings.gameObject.SetActive(show);

        public void ShowLevelComplete(int level, int points, int coins, string unlock)
        {
            _lcLevel.text = $"Level {level} complete";
            _lcPoints.text = $"+{points} pts";
            _lcCoins.text = $"+{coins} coins";
            _unlockNote.text = unlock ?? "";
            _unlockNote.gameObject.SetActive(!string.IsNullOrEmpty(unlock));
            _levelComplete.gameObject.SetActive(true);
        }
        public void HideLevelComplete() => _levelComplete.gameObject.SetActive(false);

        public void ShowGameOver(int score, string nearMiss)
        {
            _goScore.text = $"Score: {score}";
            _nearMiss.text = nearMiss ?? "";
            _nearMiss.gameObject.SetActive(!string.IsNullOrEmpty(nearMiss));
            _gameOver.gameObject.SetActive(true);
        }
        public void HideGameOver() => _gameOver.gameObject.SetActive(false);

        public void SetMenuMeta(int streak, int level, HashSet<int> collection)
        {
            _streakLabel.text = streak > 0 ? $"{streak}-DAY STREAK" : "";
            _chapterLabel.text = $"Chapter {GameConfig.ChapterOf(level)} · Level {level}";
            int into = (level - 1) % GameConfig.ChestEvery;
            _journeyFill.rectTransform.anchorMax = new Vector2((float)into / GameConfig.ChestEvery, 1);
            _collectionCount.text = $"Gallery {collection.Count}/{Scenes.Count}";

            foreach (Transform t in _collectionRow) Destroy(t.gameObject);
            for (int i = 0; i < Scenes.Count; i++)
            {
                bool got = collection.Contains(i);
                UIFactory.RoundedPanel(_collectionRow, "g", got ? Scenes.All[i].BgTop : UIFactory.Hex("#20233a"), 16, got);
            }
        }

        public void SetDailyButton(bool available, int amount)
        {
            _dailyBtn.gameObject.SetActive(available);
            if (available) _dailyBtn.GetComponentInChildren<Text>().text = $"Daily reward · +{amount} coins";
        }

        public void SetMissions(List<Mission> missions)
        {
            foreach (Transform t in _missionsList) Destroy(t.gameObject);
            foreach (var m in missions)
            {
                var row = UIFactory.RoundedPanel(_missionsList, "m", UIFactory.Hex("#0c0e18"), 20).rectTransform;
                row.sizeDelta = new Vector2(0, 64);
                var label = UIFactory.Label(row, "t", $"{m.Text}", 28, m.Complete ? UIFactory.Hex("#7fe0a0") : _text, TextAnchor.MiddleLeft);
                label.rectTransform.anchorMin = new Vector2(0, 0); label.rectTransform.anchorMax = new Vector2(0.7f, 1);
                label.rectTransform.offsetMin = new Vector2(24, 0); label.rectTransform.offsetMax = Vector2.zero;
                var prog = UIFactory.Label(row, "p", $"{m.Progress}/{m.Goal}", 26, _muted, TextAnchor.MiddleRight);
                prog.rectTransform.anchorMin = new Vector2(0.7f, 0); prog.rectTransform.anchorMax = new Vector2(1, 1);
                prog.rectTransform.offsetMin = Vector2.zero; prog.rectTransform.offsetMax = new Vector2(-24, 0);
            }
        }
    }
}
